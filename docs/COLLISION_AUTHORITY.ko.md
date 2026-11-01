# 서버 충돌 판정에 대응하는 Unity 클라이언트

2026-11-01, `feature/server-authoritative-collision`.
대응 서버 브랜치도 같은 이름이다. 서버·클라이언트를 함께 업데이트해야 한다.
이전 HIT/WATER_HIT 기반 클라이언트는 새 서버에서 정상적인 전투를 수행할 수 없다.

## 변경 파일과 동작

| 파일 | 변경 함수/역할 |
| --- | --- |
| `Assets/Script/Network/WorldProtocol.cs` | `Parse`, `Move`, `Attack`: 경기 번호, 유한 숫자, 필드 수, 순서 번호, 문화권 독립 숫자 직렬화/파싱 |
| `Assets/Script/Network/NetworkConnector.cs` | `SendWorld`: 순서 있는 비동기 송신, 대기 32건/1초 제한. `HandleServerMessage`: 서버 상태 이벤트 전달, 맵/캐릭터 생성 후 MATCH_READY 전송. `WorldStarted`로 준비 전 조작 차단 |
| `Assets/Script/Game/LocalPlayerController.cs` | `HandleAttackOrBalloonInput`: 공격 방향/풍선 설치 의도만 전송. `FixedUpdate/TrySendPosition`: 방향 입력 전송과 로컬 이동 예측. `Reconcile`: 서버와 0.25 이상 위치 차이를 보정. 물 피해 보고 제거 |
| `Assets/Script/Game/WorldView.cs` | `Receive/Update`: 서버 무기 ID별 기존 prefab 표시, 위치/회전 보간, 기존 무기 스크립트·collider 비활성화, 서버 타일 제거 적용. 원격 캐릭터 위치 보간 |
| `Assets/Script/Game/BalloonSystem.cs` | `HandleBalloonResult`, `HandleWaterSpread`: 로컬 폭발 타이머/블록 파괴 주장 제거, 서버가 보내는 물 타일만 표시 |
| `Assets/Script/Game/MapSystem.cs` | `RemoveServerTile`: 검증된 좌표의 Block/Wall만 제거 |
| `Assets/Script/Game/GameSystem.cs` | 각 클라이언트가 맵 요청, 원격 위치 문화권 독립 파싱 |
| `tools/WorldProtocolTests.cs`, `tools/test_world.py` | 순수 프로토콜 테스트와 실제 production C# 구문 검사 |
| `.github/workflows/combat.yml` | 신규 테스트 CI 등록 |

새 스크립트의 `.meta`도 함께 추적한다. prefab, scene, ProjectSettings, 계정 데이터는 변경하지 않았다.
기존 무기 스크립트는 자산 참조를 유지하기 위해 남겼으나 새 WorldView가 생성하는 오브젝트에서는
비활성화한다. 해당 스크립트의 이전 결과 주장 패킷을 보내더라도 서버는 상태 변경에 사용하지 않는다.

## 프로토콜

클라이언트 입력은 `MATCH_READY|match`, `MOVE_INPUT|match|seq|dx,dz`,
`ATTACK|match|seq|yaw`, `BALLOON|match|seq`다. LF로 끝난다.
공격자·대상·피해량·무기 선택·생성 좌표·폭발 시각을 전송하지 않는다.
서버가 경기 시작에 확정한 캐릭터와 장착 풍선을 사용한다.

서버의 `COMBAT_BEGIN`, `PLAYER_STATE`, `WEAPON_SPAWN/STATE/REMOVE`, `TILE_REMOVE`는
현재 경기 번호와 일치해야 적용한다. 기존 DAMAGE/사망/승패/재경기와 풍선 표시 이벤트는 유지한다.
풍선/물 이벤트 자체에는 아직 경기 번호가 없으며 연결 수명 고도화 때 함께 보강해야 한다.
계정 저장 형식 및 비밀번호 없는 로그인 성공 형식은 이전 인증 작업과 같다.

## 실제 실행 검증

```powershell
python tools/test_world.py --out <world-results>
python tools/test_combat.py --out <combat-results>
python tools/test_auth.py --out <auth-results>
```

.NET 9에서 새 프로토콜 9개 그룹(잘못된/이전 경기 이벤트 12종과 프랑스어 숫자 문화권 포함),
기존 전투 상태 11개 그룹, 인증 9개 그룹이 통과했다.
`test_world.py`는 55개 실제 production 스크립트를 C# 9 문법으로 검사한다.
이는 Unity 참조를 연결한 타입 검사나 Editor 빌드가 아니다.
기존 `LineMessageBuffer.cs`와 `tools/LineMessageBufferTests.cs`를 .NET Framework C# 컴파일러로
빌드한 수신 프레이밍 4개 그룹도 통과했다. 실행 명령은 `.github/workflows/receive-framing.yml`과 같다.
경로가 없는 결과 파일을 `measurements/2026-11-01-collision/`에 보관했다.

첫 [GitHub 검사](https://github.com/YSL-RyuDo/BNB/actions/runs/41256607150)는 실패했다.
CI에 미리 설치된 SDK 10의 Roslyn이 Runtime 10을 요구하여 net9.0 검사에서 CS1705가 발생했다.
`test_world.py`가 설치된 정식 SDK 9를 선택하고 격리 출력 폴더의 global.json과 작업 디렉터리로
선택을 고정하도록 수정했다. 로컬 재검증은 9개 그룹 통과했다. 이 실패를 게임 플레이 통과로
해석하거나 숨기지 않는다.

## 호환성 영향과 미검증 범위

- 실제 Unity 2022.3.40f1 Editor PlayMode/전체 빌드는 실행하지 못했다. 필요한 버전이 설치되어
  있지 않으며 설치된 6000.3.1f1로 원본 프로젝트를 업그레이드하지 않았다.
- 맵·캐릭터·무기·풍선 자산과 UI 흐름을 유지했지만 판정은 서버의 XZ 평면 근사다.
  Unity PhysX와 완전히 같은 결과 또는 이전 조작감을 보장하지 않는다.
- Laser의 중복 길이 배율 판정을 최대 15의 표시 범위로 수정했다. Melody의 전체 수명은
  최대 10초다. 이 변경은 기존보다 제한적이다.
- 이동 예측은 간단한 위치 보정이며 입력 재실행/지연 보상은 없다. 실제 RTT에서 조작감,
  벽·풍선 접촉, 두 맵/7종 무기/10종 풍선, 공격 애니메이션 시점, 개인전/협동전 및 연속
  재경기를 동일 Editor 버전으로 확인한 후 병합 여부를 판단해야 한다.
- TCP TLS, 기존 평문 계정 이력 처리, 인증 시도 제한은 여전히 후속 과제다.
  이번 변경으로 외부 네트워크의 자격 증명 보호가 완성된 것은 아니다.
- 별도 feature 브랜치에서 작업했고 main 병합은 하지 않았다.

서버 구현·규칙·재현 결과는 BnBServer의 `docs/engineering/COLLISION_AUTHORITY.ko.md`를 참조한다.
