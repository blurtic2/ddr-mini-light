namespace DDRMiniLight.Core;

/// <summary>DDR Mini 컨트롤러의 화살표 버튼 (LED도 같은 비트로 표현).</summary>
[Flags]
public enum PadButtons : byte
{
    None = 0,
    Left = 1 << 0,
    Down = 1 << 1,
    Up = 1 << 2,
    Right = 1 << 3,

    All = Left | Down | Up | Right,
}
