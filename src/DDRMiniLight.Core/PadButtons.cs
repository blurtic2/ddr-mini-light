namespace DDRMiniLight.Core;

/// <summary>
/// DDR Classic Mini 패드의 버튼. 화살표 4개는 LED도 같은 비트로 표현합니다.
/// 패드 아래쪽에 메뉴 좌, 취소, 선택, 메뉴 우 버튼이 있습니다 (LED 없음).
/// </summary>
[Flags]
public enum PadButtons : byte
{
    None = 0,

    Left = 1 << 0,
    Down = 1 << 1,
    Up = 1 << 2,
    Right = 1 << 3,

    MenuLeft = 1 << 4,
    Cancel = 1 << 5,
    Select = 1 << 6,
    MenuRight = 1 << 7,

    /// <summary>화살표 4개 (LED가 있는 버튼).</summary>
    Arrows = Left | Down | Up | Right,

    /// <summary>아래쪽 버튼 4개.</summary>
    Menu = MenuLeft | Cancel | Select | MenuRight,
}
