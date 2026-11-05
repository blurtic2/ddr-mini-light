namespace DDRMiniLight.Core;

/// <summary>패드가 1P용인지 2P용인지. 하드웨어로는 구분할 수 없어 사용자가 지정합니다.</summary>
public enum PadSide
{
    Unassigned = 0,
    P1 = 1,
    P2 = 2,
}
