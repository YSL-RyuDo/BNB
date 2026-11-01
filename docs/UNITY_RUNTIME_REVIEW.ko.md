# Unity 2022.3.40f1 실제 실행 검수

2026-11-01, `feature/server-authoritative-collision`에서 진행했다. 서버 기반 커밋은
`d375a689a0105d62b687015a0d1cadf4ceb41175`, 클라이언트 기반 커밋은
`624ed6a561d3bbb2fce334f8cb8a5b9c3687deb6`이다. main 병합, 실제 계정 변경,
Unity 프로젝트 버전 업그레이드는 하지 않았다. 설치·검수 프로젝트·캐시·빌드는 D 드라이브에 두었다.

## 발견과 수정

| 파일 / 속성 | 변경 전 | 변경 후 |
| --- | --- | --- |
| `Assets/Scenes/LobbyScene.unity`, CreateRoomPasswordText의 `TMP_InputField.m_GlobalFontAsset` | 존재하지 않는 폰트 GUID | 해당 입력란 자식 텍스트가 사용하는 기존 폰트 자산 |
| `Assets/Scenes/ShopScene.unity`, Video의 `VideoPlayer.m_VideoClip` | 존재하지 않는 영상 GUID | 기존 characterVideo[0]에 등록된 Knight 영상 |
| `Assets/FBX/Weapon/지팡이2.fbx.meta`, `isReadable` | 0, King 생성 시 Outline의 CPU 메시 접근 오류 | 1, 해당 지팡이 메시의 읽기/쓰기 허용 |

새 자산을 추가하거나 상점 콘텐츠 배열을 바꾸지 않았다. 함수·게임 규칙·데이터 저장 형식·
네트워크 프로토콜은 이번 수정에서 변경하지 않았다. Hybrid Winsock + IOCP 서버도 그대로다.
초기 빌드는 성공했지만 자산 검사에서 두 참조가 실패했으며, 같은 검사로 수정 후 0건을 확인했다.
또한 실제 Map2/캐릭터 4·5·6·0 조합에서 King의 지팡이 `Cylinder009`, `Sphere003` 메시
접근 오류가 클라이언트마다 8건씩 발생했다. 진행 검사 자체가 끝나도 종료 코드는 1로 실패했다.
오류를 무시하거나 외곽선을 끄지 않고 해당 모델의 import 설정을 수정했다. 이 설정은 해당
메시의 CPU 사본을 유지하므로 메모리 사용량이 증가할 수 있다. 증가량은 정량 측정하지 않았다.

## 검수 방법

`tools/unity_review/ReviewBuild.cs`의 `Inspect/Run`은 활성 빌드 씬과 게임 프리팹을 Unity
SerializedObject API로 검사하고 Windows Development 플레이어를 빌드한다. 최초에는
Assets/Prefab의 72개를 검사했고, 이후 씬 의존성을 포함한 86개로 확대해 Assets/FBX 아래의
실제 캐릭터도 검사한다. 동적으로 추가하는 Outline에 필요한 메시 읽기 가능 여부도 확인한다.
`ReviewAgent.Start`는 실제 LoginUI 및 RoomUI 버튼, LocalPlayerController 입력 전송 경로를
호출하고 실제 TCP 수신기와 씬·게임 오브젝트 상태를 확인한다. `run_players.py`는 합성 계정
4개를 별도 작업 디렉터리에 만들고 서버와 실제 Unity 플레이어 4개를 실행한다.

원본 체크아웃 대신 Assets/Packages/ProjectSettings 사본을 사용했다. 검사 에이전트만 사본에
주입하고 게임 스크립트를 대역으로 교체하지 않았다. 검사용 company/product 이름으로 실제
사용자의 PlayerPrefs를 분리했다. 서버는 `tools/benchmark/build.py`의 loopback 빌드이며
계정 CSV는 전부 합성 데이터다. 저장소에서 복사한 CSV는 맵 지형뿐이다.

## 명령과 결과

정식 반복 실행 명령은 [검수 도구 설명](../tools/unity_review/README.md)에 있다. 이번 검수에서는
동일한 `ReviewBuild.Run`을 사본 프로젝트에 직접 실행하면서 에이전트를 보완하고 반복 빌드했다.

```text
Unity.exe -batchmode -nographics -quit -projectPath <copy> -logFile <import-log>
Unity.exe -batchmode -nographics -projectPath <copy> -executeMethod ReviewBuild.Run -logFile <build-log>
python tools/benchmark/build.py --out <server-build> --port 9000
python tools/unity_review/run_players.py --server-repo <server-repo> --server-build <server-build> --player <player> --out <case> --map <map> --weapons <four-indices> --balloons <four-indices>
python tools/test_auth.py --out <auth-results>
python tools/test_combat.py --out <combat-results>
python tools/test_world.py --out <world-results>
python tools/benchmark/build.py --out <network-build> --port 39092
python tools/benchmark/regression.py --build <network-build> --out <network-results>
```

| 검사 | 실제 결과 |
| --- | --- |
| Unity 2022.3.40f1 원본 사본 import/스크립트 컴파일 | 종료 0 |
| 수정 전 씬·프리팹 검사와 Windows 빌드 | 빌드는 성공, 참조 2건 때문에 검사 종료 1 |
| 수정 후 확대 자산 검사와 Windows 빌드 | 7개 씬·86개 프리팹, 문제 0건, 빌드 오류 0건·경고 7건, 종료 0 |
| C++ 서버 Release x64 loopback 빌드 | 포트 9000/39092 빌드 모두 종료 0 |
| 기존 TCP 프레이밍·방 격리·잘못된 입력 회귀 | 8/8 통과, 종료 0 |
| 인증/수신 프레이밍 .NET 검사 | 9개 그룹 통과, 종료 0 |
| 전투 상태 .NET 검사 | 11개 그룹 통과, 종료 0 |
| 월드 프로토콜·C# 문법 검사 | 9개 그룹 통과, 종료 0 |

빌드 경고가 없다고 주장하지 않는다. 최초 빌드에는 미사용 변수/필드 경고도 있었으며,
증분 빌드의 경고 수는 전체 소스의 경고 개수를 의미하지 않는다. 원본 빌드 보고서와 마지막
보고서를 함께 보관한다.

최종 빌드의 실제 4인 실행 결과:

| 맵 | 캐릭터/무기 인덱스 | 풍선 인덱스 | 결과 |
| --- | --- | --- | --- |
| Map1 | 0,1,2,3 | 0,1,2,3 | 4/4 완료, 오류 0, 모두 종료 0 |
| Map2 | 4,5,6,0 | 4,5,6,7 | 4/4 완료, 오류 0, 모두 종료 0 |
| Map1 | 0,1,2,3 | 8,9,0,1 | 4/4 완료, 오류 0, 모두 종료 0 |

각 실행은 로그인 → 방 입장 → 캐릭터 선택 → 경기 진입 → 이동/위치 변조 보정 → 공격
→ 무기 표시 수명 종료 → 물풍선 설치/자동 폭발/피해 10 → HP UI 반영 → 물 정리 →
이모티콘 표시를 확인했다. 이동과 변조 보정은 각 실행의 0번 클라이언트에서 수행했다.
모든 클라이언트가 피해 신고 없이 서버 피해를 수신했다. 모든 피격·승패·보상 경로를 이
3개 시나리오에서 검사했다는 의미는 아니다.

검토 가능한 원본 요약은 [measurements/2026-11-01-unity-review](../measurements/2026-11-01-unity-review/)
에 있다. 실패한 외곽선 실행과 수정 후 결과를 모두 포함한다. 최종 플레이어와 실제 검사한
게임 소스/수정 자산의 해시는 `manifest.json`에 기록했다. 실제 실행 사본과 저장소의 해당
58개 파일이 바이트 단위로 같은지도 확인했다. 원시 로그의 개인 경로·네트워크 주소·라이선스
정보는 올리지 않았다. 이 기록은 로컬 실행 결과이며 새 GitHub Actions 실행 성공으로 표시하지 않는다.

초기 검사 에이전트의 실패도 제품 실패와 구별한다. 첫 시도는 씬에 연결되지 않은 구형
LoginSystem/RoomSystem을 찾았다. 실제 LoginUI/RoomUI로 수정했다. 다음 시도에서는 다른
플레이어의 풍선 생성과 본인의 설치 응답을 구분하지 않았다. 본인의 쿨타임 이벤트를 기다리도록
수정했다. 또 이전 무기 단계에서 이미 체력이 감소한 플레이어를 발견했다. 물풍선 직전 체력을
기준으로 추가 10 피해를 검사하고, 별도 물 생성 이벤트를 기다리도록 수정했다. 이 실패 시도를
통과로 집계하지 않으며, 대기 중인 소유 테스트 프로세스는 중단하고 새 합성 경기에서 재실행했다.
피해를 한 번도 받지 않은 플레이어의 캐시는 아직 비어 있으므로 초기 체력은 실제 HP 슬라이더에서
읽으며, 피해를 받은 뒤에는 서버 캐시와 UI 값이 일치하는지도 확인한다.

## 해석 범위

- 실제 Unity 엔진이 컴파일하고 실제 Windows 플레이어에서 PhysX, MonoBehaviour, 네트워크,
  씬 전환과 프리팹 생명주기를 실행한 검사다. .NET 테스트만으로 Unity 성공을 주장하지 않는다.
- 플레이어는 `-batchmode -nographics`로 실행했다. Null graphics의 셰이더·비디오 디코더
  메시지는 화면 품질 검증이 아니다. Editor PlayMode, 렌더링, 소리, 애니메이션 타이밍,
  사람이 느끼는 조작감은 이번 결과로 통과 처리하지 않는다.
- 자동 입력 에이전트는 게임 진입 후 키보드 Update를 멈추고 실제 입력 전송 메서드를 호출한다.
  서버 판정 패킷을 주입하거나 피해 신고를 보내지 않는다. 검증은 loopback에서 수행했다.
- 무기와 풍선 인덱스를 모두 실행해도 모든 맵·무기 조합이나 충돌 경계 상황을 검증한 것은 아니다.
  협동전·재접속·연속 경기·WAN 지연·다수 방 부하는 별도 검수가 필요하다.
- 이모티콘은 정상 응답의 표시만 검사한다. 소유권/인덱스/서버 쿨타임의 모든 악용 경로를
  검증한 것으로 해석하지 않는다.
- TLS, 기존 평문 계정의 과거 이력 처리, 인증 시도 제한은 여전히 후속 과제다.
- 기존 CI는 이 검수 에이전트를 자동 실행하지 않는다. Unity 라이선스/Editor가 없는 CI를
  전체 Unity 검증으로 표시하지 않는다.
