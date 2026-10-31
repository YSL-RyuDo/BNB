# 서버 권한 전투 상태 연동

`feature/auth-hardening`에서 분기한 `feature/server-authoritative-combat` 변경이다.
서버의 동일 브랜치와 함께 배포한다. main 및 실제 계정 데이터는 변경하지 않는다.

`NetworkConnector`는 `GAME_START|room|matchId`에서 경기 번호를 저장하고 장면을 한 번 전환한다.
7종 무기 HIT와 물 충돌 신고에 경기 번호를 추가한다. 물 피해량은 클라이언트가 전송하지 않는다.
`CombatState`는 `DAMAGE|nickname|damage|remainingHP|matchId|revision`을 엄격하게 파싱하고
이전 경기, 중복, 역순 상태를 거부한다. 플레이어별로 상태 순번과 체력을 캐시한다.
`GameSystem::ApplyServerHealth`는 서버의 남은 체력을 표시한다. 로컬 피격 간격/체력 차감/사망 결정은 제거했다.
`PLAYER_DEAD|nickname|matchId`만 사망 표시로 사용한다.
서버가 알려준 본인 체력 0에 대한 DEAD 수신 확인을 2초 간격으로 보내 보상 파일 저장 재시도를 지원한다.
이 수신 확인은 서버의 체력을 변경할 권한이 없다.

다음 명령을 실제 실행하여 각각 전투 상태 11개 그룹, 기존 인증/프레이밍 9개 그룹이 통과했다.

```powershell
python tools/test_combat.py --out <new-isolated-directory>
python tools/test_auth.py --out <another-new-isolated-directory>
```

전투 검사는 실제 `CombatState.cs`를 .NET 9로 컴파일한다. UI 생성 전 상태 캐시,
두 플레이어의 교차 순번, 중복/역순, 이전 경기 재전송, 잘못된 숫자와 구버전 응답,
연결 재설정을 검증한다. `.github/workflows/combat.yml`에 등록했다.

실제 Unity Editor 플레이와 전체 클라이언트 컴파일은 아직 검증하지 않았다.
필요 버전은 2022.3.40f1이고, 이 PC에는 6000.3.1f1만 있어 프로젝트를 열거나 업그레이드하지 않았다.
병합 전 정확한 버전에서 개인전/협동전, 모든 무기, 물 피해, 장면 전환, 연속 경기,
체력/사망/결과 화면 및 보상 저장 실패 후 재시도를 확인해야 한다.

아직 충돌 자체는 Unity 신고에 의존한다. 서버는 경기·세션·캐릭터·팀·피격 간격·체력·사망을 검증하지만
공격 거리나 물 타일을 검증하지 않는다. TLS도 없으므로 실서비스 전투/인증 보안의 완성으로 표현하지 않는다.
