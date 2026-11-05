using System.Text.Json;
using System.Text.Json.Serialization;
using DDRMiniLight.Core;

namespace DDRMiniLight.App;

/// <summary>%AppData%\DDRMiniLight\settings.json 에 저장되는 사용자 설정.</summary>
internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "DDRMiniLight",
        "settings.json");

    public LightMode LightMode { get; set; } = LightMode.Mirror;

    /// <summary>창의 닫기 버튼을 누르면 종료하지 않고 트레이로 숨깁니다.</summary>
    public bool CloseToTray { get; set; } = true;

    /// <summary>장치 경로(USB 포트마다 고유) → 1P/2P.</summary>
    public Dictionary<string, PadSide> PadSides { get; set; } = new();

    /// <summary>트레이로 숨겼다는 알림을 이미 보여줬는지.</summary>
    public bool TrayHintShown { get; set; }

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions) ?? new();
        }
        catch
        {
            // 손상된 설정 파일은 무시하고 기본값을 씁니다.
        }
        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch
        {
            // 설정 저장 실패는 동작에 영향이 없으므로 무시합니다.
        }
    }
}
