# DDR Mini Light

DDR Classic Mini의 1P/2P 패드(PHOENIXWAN, VID `0x33DD` / PID `0x0014`)를 PC에 연결했을 때, 화살표 LED를 입력에 맞춰 켜 주는 Windows 트레이 앱입니다.

## 기능

- 누른 화살표의 LED 점등 (모드: **누른 버튼만 켜기** / **항상 켜기** / **끄기**)
- 패드 여러 대 동시 지원, USB 연결/분리 자동 감지
- 1P/2P 지정: **밟아서 1P/2P 지정** 버튼을 누르고 1P 패드를 밟거나, 카드의 1P/2P 버튼으로 지정
  - 지정한 패드는 화면에서 1P → 2P 순으로 표시되고, **식별** 버튼을 누르면 해당 패드의 LED가 깜빡임
- 창에서 패드별 입력 상태(화살표 4개 + 아래쪽 메뉴 좌/취소/선택/메뉴 우)와 LED 상태를 실시간으로 표시
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
│  │  ├─ PadSideMap.cs            장치 경로 → 1P/2P 지정 (맞바꾸기, 자동 지정)
│  │  ├─ PadSide.cs               1P / 2P / 미지정
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
   └─ DDRMiniLight.Core.Tests/    프로토콜, 1P/2P 지정 단위 테스트 (xUnit)
```

## 빌드

.NET 8 SDK가 필요합니다.

```bash
dotnet build -c Release
dotnet test
```

### 실행용 exe 만들기

저장소 루트의 `output` 폴더에 단일 exe(`DDRMiniLight.exe`, 약 0.5MB, .NET 8 Desktop Runtime 필요)를 만듭니다.

```bash
dotnet publish src/DDRMiniLight.App -p:PublishProfile=Local
```

Visual Studio에서는 프로젝트 우클릭 → **게시** → **Local**을 선택해도 같습니다.

- 평소에는 `output\DDRMiniLight.exe`를 실행해서 쓰고, "Windows 시작 시 자동 실행"도 이 exe에서 켜 두세요.
- 다시 게시하기 전에 트레이에서 앱을 종료하세요. 실행 중에는 exe가 잠겨서 덮어쓸 수 없습니다.
- 앱이 트레이에 떠 있으면 VS에서 디버그 실행해도 중복 실행 방지 때문에 기존 창만 열리고 종료됩니다.

## 프로토콜 메모

인덱스는 리포트 ID(`[0]`)를 포함한 버퍼 기준입니다.

| 버튼 | 입력 비트 | HID 버튼 번호 | LED 출력 바이트 |
|------|-----------|---------------|-----------------|
| ←    | `[3]` bit5 | 6  | `[3]` |
| ↓    | `[3]` bit4 | 5  | `[2]` |
| ↑    | `[4]` bit2 | 11 | `[4]` |
| →    | `[4]` bit3 | 12 | `[1]` |
| 메뉴 좌 | `[3]` bit6 | 7 | 없음 |
| 취소    | `[3]` bit1 | 2 | 없음 |
| 선택    | `[3]` bit0 | 1 | 없음 |
| 메뉴 우 | `[3]` bit7 | 8 | 없음 |

LED 출력 리포트: `[0] = 0x05`(리포트 ID), 나머지 바이트는 0(꺼짐) 또는 1(켜짐).

리포트 디스크립터 기준으로 입력 리포트(ID 1)는 `[1]` X축, `[2]` Y축, `[3..4]` 버튼 16개입니다.
1P/2P 패드 모두 같은 비트를 씁니다.

### 1P/2P 구분

1P/2P 패드는 디스크립터, 버전, 제품명이 모두 같고 시리얼 번호도 없어서 하드웨어로는 구분할 수 없습니다.
그래서 꽂힌 USB 포트마다 달라지는 HID 장치 경로를 키로 해서 사용자가 지정한 1P/2P를 설정 파일에 저장합니다.
같은 포트에 꽂으면 지정이 유지되고, 포트를 바꾸면 다시 지정해야 합니다.
