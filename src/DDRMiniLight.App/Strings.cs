using System.Globalization;
using DDRMiniLight.Core;

namespace DDRMiniLight.App;

/// <summary>설정에 저장되는 언어 선택. Auto는 Windows 표시 언어를 따릅니다.</summary>
internal enum AppLanguage
{
    Auto,
    Korean,
    English,
    Japanese,
}

/// <summary>
/// 화면에 보이는 문자열 (한국어 / 영어 / 일본어).
/// 시작할 때 <see cref="Initialize"/>로 언어를 정하고, 바꾸면 앱을 다시 시작합니다.
/// </summary>
internal static class Strings
{
    private static AppLanguage _language = AppLanguage.English;

    /// <summary>실제로 쓰는 언어 (Auto가 아닌 값).</summary>
    public static AppLanguage Language => _language;

    public static void Initialize(AppLanguage setting) => _language = Resolve(setting);

    public static AppLanguage Resolve(AppLanguage setting)
    {
        if (setting != AppLanguage.Auto)
            return setting;

        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName switch
        {
            "ko" => AppLanguage.Korean,
            "ja" => AppLanguage.Japanese,
            _ => AppLanguage.English,
        };
    }

    private static string T(string ko, string en, string ja) => _language switch
    {
        AppLanguage.Korean => ko,
        AppLanguage.Japanese => ja,
        _ => en,
    };

    // ── 언어 선택 ───────────────────────────────

    /// <summary>언어 선택 목록. 언어 이름은 각 언어로 표기합니다.</summary>
    public static IReadOnlyList<(AppLanguage Language, string Label)> LanguageOptions =>
    [
        (AppLanguage.Auto, T("자동 (시스템 언어)", "Auto (system language)", "自動（システム言語）")),
        (AppLanguage.Korean, "한국어"),
        (AppLanguage.English, "English"),
        (AppLanguage.Japanese, "日本語"),
    ];

    public static string LanguageLabel => T("언어", "Language", "言語");
    public static string RestartPrompt => T(
        "언어를 바꾸려면 앱을 다시 시작해야 합니다. 지금 다시 시작할까요?",
        "The app needs to restart to change the language. Restart now?",
        "言語を変更するにはアプリの再起動が必要です。今すぐ再起動しますか？");
    public static string RestartLater => T(
        "다음에 시작할 때 새 언어가 적용됩니다.",
        "The new language will be used the next time the app starts.",
        "次回起動時に新しい言語が反映されます。");

    // ── LED 모드 ───────────────────────────────

    public static IReadOnlyList<(LightMode Mode, string Label)> LightModes =>
    [
        (LightMode.Mirror, T("누른 버튼만 켜기", "Light pressed buttons", "押したボタンのみ点灯")),
        (LightMode.AlwaysOn, T("항상 켜기", "Always on", "常時点灯")),
        (LightMode.Off, T("끄기", "Off", "消灯")),
    ];

    // ── 트레이 ─────────────────────────────────

    public static string OpenWindow => T("창 열기", "Open window", "ウィンドウを開く");
    public static string LedMode => T("LED 모드", "LED mode", "LED モード");
    public static string LedTest => T("LED 테스트", "LED test", "LED テスト");
    public static string StartWithWindows => T("Windows 시작 시 자동 실행", "Start with Windows", "Windows 起動時に自動実行");
    public static string Exit => T("종료", "Exit", "終了");
    public static string TrayHint => T(
        "트레이에서 계속 실행 중입니다. 종료하려면 트레이 아이콘을 우클릭하세요.",
        "Still running in the tray. Right-click the tray icon to exit.",
        "トレイで実行中です。終了するにはトレイアイコンを右クリックしてください。");
    public static string TrayConnected(string sides) => T($"{sides} 연결됨", $"{sides} connected", $"{sides} 接続中");
    public static string TrayNoPads => T("연결된 패드 없음", "No pads connected", "パッド未接続");

    // ── 메인 창 ────────────────────────────────

    public static string PadsGroup => T("컨트롤러", "Pads", "パッド");
    public static string LedGroup => "LED";
    public static string OptionsGroup => T("옵션", "Options", "オプション");
    public static string LogGroup => T("로그", "Log", "ログ");
    public static string CloseToTray => T("닫기 버튼을 누르면 트레이로 숨기기", "Hide to tray when closed", "閉じるとトレイに隠す");
    public static string SearchingPads => T(
        "DDR Mini 컨트롤러를 찾는 중입니다.\nUSB로 연결하면 자동으로 인식합니다.",
        "Looking for DDR Classic Mini pads.\nThey are detected automatically when connected via USB.",
        "DDR Classic Mini のパッドを探しています。\nUSB で接続すると自動的に認識します。");

    // ── 1P/2P 지정 ─────────────────────────────

    public static string Unassigned => T("미지정", "Unassigned", "未割り当て");
    public static string AssignByPressing => T("밟아서 1P/2P 지정", "Assign 1P/2P by pressing", "踏んで 1P/2P を割り当て");
    public static string Cancel => T("취소", "Cancel", "キャンセル");
    public static string PortHint => T(
        "1P/2P는 꽂힌 USB 포트로 구분합니다.",
        "1P/2P is identified by the USB port.",
        "1P/2P は接続した USB ポートで区別します。");
    public static string UnassignedHint => T(
        "1P/2P가 지정되지 않은 패드가 있습니다.",
        "Some pads have no 1P/2P assignment.",
        "1P/2P が割り当てられていないパッドがあります。");
    public static string AssignNoPads => T(
        "1P/2P 지정: 연결된 패드가 없습니다.",
        "Assign 1P/2P: no pads connected.",
        "1P/2P 割り当て：接続されたパッドがありません。");
    public static string PressPrompt(string side) => T(
        $"{side} 패드의 아무 버튼이나 누르세요.  (Esc: 취소)",
        $"Press any button on the {side} pad.  (Esc: cancel)",
        $"{side} パッドの任意のボタンを押してください。（Esc：キャンセル）");
    public static string AssignCanceled => T("지정을 취소했습니다.", "Assignment canceled.", "割り当てをキャンセルしました。");
    public static string AssignDone => T(
        "지정했습니다. 다음에도 같은 USB 포트에 꽂으면 그대로 유지됩니다.",
        "Assigned. It is kept as long as the pad stays in the same USB port.",
        "割り当てました。同じ USB ポートに接続している限り維持されます。");
    public static string AssignedAs(string side) => T($"{side}로 지정했습니다.", $"Assigned as {side}.", $"{side} に割り当てました。");

    // ── 장치 카드 ──────────────────────────────

    public static string Identify => T("식별", "Identify", "識別");
    public static string Input(string buttons) => T($"입력: {buttons}", $"Input: {buttons}", $"入力：{buttons}");
    public static string NoLedOutput => T("LED 출력 미지원", "No LED output", "LED 出力非対応");
    public static string MenuLeft => T("메뉴◀", "Menu◀", "メニュー◀");
    public static string CancelButton => T("취소", "Cancel", "キャンセル");
    public static string SelectButton => T("선택", "Select", "決定");
    public static string MenuRight => T("메뉴▶", "Menu▶", "メニュー▶");

    // ── 로그 ──────────────────────────────────

    public static string SearchStarted => T("장치 검색을 시작합니다.", "Searching for pads.", "パッドを検索しています。");
    public static string LedTestNoPads => T(
        "LED 테스트: 연결된 패드가 없습니다.",
        "LED test: no pads connected.",
        "LED テスト：接続されたパッドがありません。");
    public static string StartupFailed(string detail) => T(
        $"자동 실행 설정 실패: {detail}",
        $"Could not change start with Windows: {detail}",
        $"自動実行の設定に失敗しました：{detail}");

    /// <summary>장치 관리자 로그를 현재 언어의 문장으로 만듭니다.</summary>
    public static string DeviceLog(DeviceLogEventArgs e)
    {
        string name = e.Device?.DisplayName ?? "";
        return e.Kind switch
        {
            DeviceLogKind.EnumerationFailed => T(
                $"장치 목록을 가져오지 못했습니다: {e.Detail}",
                $"Could not list devices: {e.Detail}",
                $"デバイス一覧を取得できませんでした：{e.Detail}"),
            DeviceLogKind.OpenFailed => T(
                $"패드를 열지 못했습니다: {e.Detail ?? "다른 프로그램이 사용 중일 수 있습니다."}",
                $"Could not open a pad: {e.Detail ?? "it may be in use by another program."}",
                $"パッドを開けませんでした：{e.Detail ?? "他のプログラムが使用中の可能性があります。"}"),
            DeviceLogKind.Connected => T($"연결됨: {name}", $"Connected: {name}", $"接続：{name}")
                + (e.Device is { SupportsLights: false } ? T(" (LED 출력 미지원)", " (no LED output)", "（LED 出力非対応）") : ""),
            DeviceLogKind.Disconnected => T($"분리됨: {name}", $"Disconnected: {name}", $"切断：{name}"),
            DeviceLogKind.ConnectionLost => T(
                $"연결 끊김 ({e.Detail}): {name}",
                $"Connection lost ({e.Detail}): {name}",
                $"接続が切れました（{e.Detail}）：{name}"),
            _ => e.Kind.ToString(),
        };
    }
}
