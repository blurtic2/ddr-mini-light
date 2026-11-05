namespace DDRMiniLight.Core;

/// <summary>LED 점등 방식.</summary>
public enum LightMode
{
    /// <summary>누르고 있는 버튼의 LED만 켭니다.</summary>
    Mirror,

    /// <summary>모든 LED를 항상 켭니다.</summary>
    AlwaysOn,

    /// <summary>모든 LED를 끕니다.</summary>
    Off,
}
