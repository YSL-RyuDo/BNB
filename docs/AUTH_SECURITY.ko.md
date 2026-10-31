# 인증 응답 변경 — 2026-11-01

동반 서버: `YSL-RyuDo/BnBServer`, 양쪽 브랜치 `feature/auth-hardening`.
기준 커밋 `d98ea1da298f3c31713407db6875702ed8383a96`에서 변경했다.

## 변경

| 파일 | 변경 함수/내용 |
| --- | --- |
| `Assets/Script/Login/AuthProtocol.cs`, `.meta` | `TryParseLoginSuccess`, `Command`: 성공 응답의 ID/닉네임 2개 필드와 오류 명령 파싱 |
| `Assets/Script/Login/AuthReceiver.cs` | `HandleMessage`: 새 성공 응답, 구분자가 있는 오류 응답, 중복 로그인 및 `AUTH_ERROR` 표시 |
| `Assets/Script/Login/LoginSystem.cs` | `HandleLoginMessage`: 대체 로그인 경로에도 같은 파서와 오류 처리 적용 |
| `Assets/Script/Login/RegisterSystem.cs` | `HandleRegisterMessage`: 오류/성공 응답의 명령을 구분자로 분리 |
| `Assets/Script/Network/NetworkConnector.cs` | `HandleServerMessage`: 신규 오류 전달; `StartListening`: 디코더 예외의 원문 로그 제거; 알 수 없는 명령의 내용 로그 제거 |
| `Assets/Script/Lobby/LobbyRoom.cs`, `LobbySystem.cs` | `OnConfirmPasswordClicked`: 방 비밀번호 로그 제거 |
| `Assets/Sprites/LobbyIllustration.png`, `.meta` | 기존 이미지 이름 정리. 파일 내용과 GUID 보존, 재생성 없음 |
| `tools/AuthTests.cs`, `AuthUiDoubles.cs`, `test_auth.py` | 실제 인증 스크립트와 UI 대체 객체를 이용하는 격리 C# 검사 |

요청 형식, 기존 입력 Trim, 닉네임 저장, LobbyScene 전환은 유지한다.
비밀번호를 PlayerPrefs 등에 저장하지 않는다. 두 인증 경로의 성공 시 닉네임 로그도 제거했다.

```text
요청 유지: LOGIN|id,password\n
요청 유지: REGISTER|id,password,nickname\n
성공 이전: LOGIN_SUCCESS|id,password,nickname\n
성공 이후: LOGIN_SUCCESS|id,nickname\n
추가 오류: AUTH_ERROR|\n
```

서버와 클라이언트를 함께 배포해야 한다. 세 필드 구형 응답, 빈 ID/닉네임,
추가 구분자/제어 문자는 거부하며 장면을 바꾸지 않는다. 기존 구버전 클라이언트는
새 서버의 성공 응답을 읽지 못한다. 게임 콘텐츠/장면/네트워크 프레이밍은 변경하지 않았다.

## 실행 결과

```powershell
python tools/test_auth.py --out ../validation/client-auth-final
```

Python 3.14, .NET SDK 9.0.318에서 exit 0, PASS 9개 그룹:

- `AuthReceiver`: 성공·구형/비정상 응답 거부·오류 표시·닉네임만 저장.
- `LoginSystem`: 성공·구형/비정상 응답 거부·오류 표시.
- `RegisterSystem`: 구분자 포함 응답 처리.
- UTF-8 인증 성공 응답의 바이트 단위 분할 수신.
- 기존 프레이밍: 분할·병합·잔여 프레임·조기 dispatch 방지.
- 기존 프레이밍: UTF-8의 모든 분할 위치.
- 기존 프레이밍: 한계 크기 및 초과 거부.
- 기존 프레이밍: 잘못된 UTF-8 거부.
- 인증 로그에 응답 본문과 테스트 비밀번호가 없는지 확인.

실제 인증 C# 소스를 별도 UTF-8 빌드 디렉터리로 복사하여 테스트한다.
테스트 UI 대체 객체는 `tools`에만 있으며 `Assets`나 플레이어 빌드에 포함되지 않는다.
이 검사는 실제 Unity 런타임/장면 실행을 대체하지 않는다. 수정한 전체 NetworkConnector 및
게임 스크립트의 Unity 컴파일도 이 검사 범위에 포함되지 않는다.

프로젝트는 Unity **2022.3.40f1**, 설치된 Editor는 **6000.3.1f1**이다.
정확한 Editor가 없어 EditMode/PlayMode/전체 플레이어 빌드는 실행하지 않았다.
검증 목적으로 프로젝트를 자동 업그레이드하지 않았다.

서버의 동일 변경 전후 인증 검사 18개는 13/18에서 18/18로 개선되었고,
동시 로그인/프레이밍 6개와 기존 네트워크·송신 큐·게임 상태 회귀 검사도 통과했다.
상세 결과와 저장소 전환/복구 절차는 동반 서버의 `docs/engineering/AUTH_SECURITY.ko.md`를 참고한다.

## 제한

TCP는 여전히 TLS가 아니다. 서버의 저장 해시화와 응답/로그 변경만으로 전송 중 비밀번호가
보호되는 것은 아니다. 인증서 검증을 포함한 전송 계층 보안은 후속 과제다.
기존 평문 계정 파일/이력은 삭제하지 않았다. 실제 운영 데이터 전환과 배포는 수행하지 않았다.
Unity 2022.3.40f1에서 로그인·회원가입·중복 로그인·장면 전환을 추가 확인해야 한다.
