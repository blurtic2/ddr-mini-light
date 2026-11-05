using DDRMiniLight.Core;

namespace DDRMiniLight.App;

/// <summary>창과 트레이 메뉴에서 같이 쓰는 LED 모드 표시 이름.</summary>
internal static class LightModeText
{
    public static IReadOnlyList<(LightMode Mode, string Label)> All { get; } =
    [
        (LightMode.Mirror, "누른 버튼만 켜기"),
        (LightMode.AlwaysOn, "항상 켜기"),
        (LightMode.Off, "끄기"),
    ];
}
