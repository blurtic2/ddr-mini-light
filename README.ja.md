# DDR Mini Light

[English](README.md) | [한국어](README.ko.md) | **日本語**

**DDR Classic Mini** の 1P/2P パッド（PHOENIXWAN、VID `0x33DD` / PID `0x0014`）を PC に接続したとき、入力に合わせて矢印の LED を点灯させる Windows 用タスクトレイアプリです。

> 非公式のファンメイドツールです。KONAMI およびハードウェアメーカーとは関係ありません。
> 現在、アプリの画面は韓国語のみです。

## 機能

- 押した矢印の LED を点灯（モード：**押したボタンのみ点灯** / **常時点灯** / **消灯**）
- 複数パッドの同時使用、USB の接続・切断を自動検出
- 1P/2P の割り当て：**「밟아서 1P/2P 지정」**（踏んで割り当て）ボタンを押してから 1P パッドの任意のボタンを押すか、各パッドのカードにある 1P/2P ボタンで割り当て
  - 割り当てたパッドは画面上で 1P → 2P の順に表示されます。**「식별」**（識別）ボタンを押すと、そのパッドの LED が点滅します
- パッドごとの入力状態（矢印 4 つ + 下部のメニュー左 / キャンセル / 決定 / メニュー右）と LED の状態をリアルタイム表示
- タスクトレイに常駐：閉じるボタンでトレイに隠れ、トレイメニューからモード変更、LED テスト、終了が可能
- Windows 起動時の自動実行（ウィンドウを表示せずトレイで起動）
- 多重起動の防止：もう一度起動すると既存のウィンドウが開きます
- 終了時にすべての LED を消灯

## ダウンロード

[Releases](https://github.com/blurtic2/ddr-mini-light/releases) から最新版を入手してください。

| ファイル | 説明 |
|---|---|
| `DDRMiniLight-vX.Y.Z-win-x64.zip` | 小さい実行ファイル。[.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0) が必要です。 |
| `DDRMiniLight-vX.Y.Z-win-x64-selfcontained.zip` | ランタイム同梱版。インストール不要でそのまま実行できます。 |

展開して `DDRMiniLight.exe` を実行してください。設定は `%AppData%\DDRMiniLight\settings.json` に保存されます。

- Windows 11 では、新しいトレイアイコンはタスクバーの隠れているインジケーター（^）の中に表示されます。常に表示するには、アイコンをタスクバーへドラッグしてください。
- 1P と 2P のパッドはハードウェアでは区別できないため、接続している USB ポートで区別します。ポートを差し替えた場合は、もう一度割り当ててください。

## 構成

```
DDRMiniLight.sln
├─ src/
│  ├─ DDRMiniLight.Core/          HID 通信ライブラリ（UI なし）
│  │  ├─ DdrMiniProtocol.cs       入力 / LED レポートの形式（ビット位置）
│  │  ├─ DdrMiniDevice.cs         パッド 1 台：読み取りスレッド、LED 出力
│  │  ├─ DeviceManager.cs         接続・切断の検出、デバイス一覧、LED テスト
│  │  ├─ PadSideMap.cs            デバイスパス → 1P/2P の割り当て（入れ替え、自動割り当て）
│  │  ├─ PadSide.cs               1P / 2P / 未割り当て
│  │  ├─ PadButtons.cs            ボタン / LED のフラグ
│  │  └─ LightMode.cs             LED モード
│  └─ DDRMiniLight.App/           WinForms トレイアプリ（DDRMiniLight.exe）
│     ├─ Program.cs               エントリポイント、多重起動の防止
│     ├─ TrayApplicationContext.cs  トレイアイコン / メニュー、アプリの寿命、設定の反映
│     ├─ MainForm.cs              メインウィンドウ
│     ├─ Controls/PadView.cs      パッドの描画
│     ├─ Controls/DeviceCard.cs   パッドごとのカード
│     ├─ AppSettings.cs           %AppData%\DDRMiniLight\settings.json
│     ├─ StartupRegistration.cs   Windows 起動時の自動実行（HKCU\...\Run）
│     └─ AppIcons.cs              アプリ / トレイアイコン（実行時に描画）
└─ tests/
   └─ DDRMiniLight.Core.Tests/    プロトコルと 1P/2P 割り当ての単体テスト（xUnit）
```

## ビルド

.NET 8 SDK が必要です。

```bash
dotnet build -c Release
dotnet test
```

### ローカル実行用 exe の作成

リポジトリ直下の `output` フォルダーに単一の実行ファイル（`DDRMiniLight.exe`、約 0.5MB、.NET 8 Desktop Runtime が必要）を作成します。

```bash
dotnet publish src/DDRMiniLight.App -p:PublishProfile=Local
```

Visual Studio では、プロジェクトを右クリック → **発行** → **Local** でも同じです。

- 普段は `output\DDRMiniLight.exe` を実行して使い、「Windows 起動時に自動実行」もこの exe から有効にしてください。
- 再発行する前に、トレイからアプリを終了してください。実行中は exe がロックされて上書きできません。
- アプリがトレイで動作中の場合、Visual Studio からデバッグ実行しても多重起動の防止により既存のウィンドウが開くだけで終了します。

## プロトコルメモ

インデックスはレポート ID（`[0]`）を含むバッファ基準です。

| ボタン | 入力ビット | HID ボタン番号 | LED 出力バイト |
|--------|------------|----------------|----------------|
| ←          | `[3]` bit5 | 6  | `[3]` |
| ↓          | `[3]` bit4 | 5  | `[2]` |
| ↑          | `[4]` bit2 | 11 | `[4]` |
| →          | `[4]` bit3 | 12 | `[1]` |
| メニュー左 | `[3]` bit6 | 7  | なし |
| キャンセル | `[3]` bit1 | 2  | なし |
| 決定       | `[3]` bit0 | 1  | なし |
| メニュー右 | `[3]` bit7 | 8  | なし |

LED 出力レポート：`[0] = 0x05`（レポート ID）。その他のバイトは 0（消灯）または 1（点灯）。

レポートディスクリプタによると、入力レポート（ID 1）は `[1]` X 軸、`[2]` Y 軸、`[3..4]` ボタン 16 個です。
1P と 2P のパッドは同じビットを使います。

### 1P と 2P の区別

1P と 2P のパッドはディスクリプタ、バージョン、製品名がすべて同じで、シリアル番号もないため、ハードウェアでは区別できません。
そこで USB ポートごとに異なる HID デバイスパスをキーにして、ユーザーが割り当てた 1P/2P を設定ファイルに保存します。
同じポートに接続している限り、割り当ては維持されます。
