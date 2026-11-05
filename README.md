# DDR Mini Light

DDR Mini 컨트롤러(PHOENIXWAN, VID `0x33DD` / PID `0x0014`)의 화살표 LED를 입력에 맞춰 켜 주는 Windows 트레이 앱입니다.

## 기능

- 누른 화살표의 LED 점등 (모드: **누른 버튼만 켜기** / **항상 켜기** / **끄기**)
- 컨트롤러 여러 대 동시 지원, USB 연결/분리 자동 감지
- 창에서 컨트롤러별 입력 상태와 LED 상태를 실시간으로 표시
- 시스템 트레이 상주: 닫기 버튼을 누르면 트레이로 숨기고, 트레이 메뉴에서 모드 변경, LED 테스트, 종료
- Windows 시작 시 자동 실행 (창 없이 트레이로 시작)
- 중복 실행 방지: 다시 실행하면 기존 창이 열림
- 종료할 때 LED를 모두 끔

## 구조

```
DDRMiniLight.sln
├─ src/
│  ├─ DDRMiniLight.Core/          HID 통신 라이브러리 (UI 없음)
│  │  ├─ DdrMiniProtocol.cs       입력/LED 리포트 형식 (비트 위치)
│  │  ├─ DdrMiniDevice.cs         장치 한 대: 읽기 스레드, LED 쓰기
│  │  ├─ DeviceManager.cs         연결/분리 감지, 장치 목록, LED 테스트
│  │  ├─ PadButtons.cs            버튼/LED 플래그
│  │  └─ LightMode.cs             LED 모드
│  └─ DDRMiniLight.App/           WinForms 트레이 앱 (DDRMiniLight.exe)
│     ├─ Program.cs               진입점, 단일 인스턴스
│     ├─ TrayApplicationContext.cs  트레이 아이콘/메뉴, 앱 수명, 설정 반영
│     ├─ MainForm.cs              메인 창
│     ├─ Controls/PadView.cs      화살표 패드 그리기
│     ├─ Controls/DeviceCard.cs   장치별 카드
│     ├─ AppSettings.cs           %AppData%\DDRMiniLight\settings.json
│     ├─ StartupRegistration.cs   HKCU\...\Run 자동 실행 등록
│     └─ AppIcons.cs              앱/트레이 아이콘 (런타임 생성)
└─ tests/
   └─ DDRMiniLight.Core.Tests/    프로토콜 단위 테스트 (xUnit)
```

## 빌드

.NET 8 SDK가 필요합니다.

```bash
dotnet build -c Release
dotnet test
```

실행 파일: `src/DDRMiniLight.App/bin/Release/net8.0-windows/DDRMiniLight.exe`

단일 exe로 배포하려면:

```bash
dotnet publish src/DDRMiniLight.App -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

## 프로토콜 메모

인덱스는 리포트 ID(`[0]`)를 포함한 버퍼 기준입니다.

| 방향 | 입력 비트 | LED 출력 바이트 |
|------|-----------|-----------------|
| ←    | `[3]` bit5 | `[3]` |
| ↓    | `[3]` bit4 | `[2]` |
| ↑    | `[4]` bit2 | `[4]` |
| →    | `[4]` bit3 | `[1]` |

LED 출력 리포트: `[0] = 0x05`(리포트 ID), 나머지 바이트는 0(꺼짐) 또는 1(켜짐).
