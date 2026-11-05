namespace DDRMiniLight.Core;

/// <summary>
/// DDR Mini 컨트롤러(PHOENIXWAN)의 HID 리포트 형식.
/// 모든 인덱스는 리포트 ID 바이트(<c>[0]</c>)를 포함한 버퍼 기준입니다.
/// </summary>
public static class DdrMiniProtocol
{
    // 모든 제품이 같은 ID를 쓰는지는 확인되지 않음
    public const int VendorId = 0x33DD;  // 13277
    public const int ProductId = 0x0014; // 20

    /// <summary>입력 리포트에서 버튼 상태를 읽는 데 필요한 최소 길이.</summary>
    public const int MinInputReportLength = 5;

    /// <summary>LED 출력 리포트 길이.</summary>
    public const int LightReportLength = 5;

    public const byte LightReportId = 0x05;

    // 입력 리포트 (ID 1): [1] X축, [2] Y축, [3..4] HID 버튼 1~16
    //   byte[3]: bit0 = 선택(1), bit1 = 취소(2), bit4 = Down(5), bit5 = Left(6), bit6 = 메뉴 좌(7), bit7 = 메뉴 우(8)
    //   byte[4]: bit2 = Up(11), bit3 = Right(12)
    private const int InputIndexA = 3;
    private const int InputIndexB = 4;
    private const int SelectBit = 0;
    private const int CancelBit = 1;
    private const int DownBit = 4;
    private const int LeftBit = 5;
    private const int MenuLeftBit = 6;
    private const int MenuRightBit = 7;
    private const int UpBit = 2;
    private const int RightBit = 3;

    // 출력 리포트: [0]=0x05, [1]=Right, [2]=Down, [3]=Left, [4]=Up (0 또는 1)
    private const int LightIndexRight = 1;
    private const int LightIndexDown = 2;
    private const int LightIndexLeft = 3;
    private const int LightIndexUp = 4;

    /// <summary>입력 리포트를 버튼 상태로 변환합니다. 길이가 부족하면 <see cref="PadButtons.None"/>.</summary>
    public static PadButtons ParseInput(ReadOnlySpan<byte> report)
    {
        if (report.Length < MinInputReportLength)
            return PadButtons.None;

        byte a = report[InputIndexA];
        byte b = report[InputIndexB];

        var result = PadButtons.None;
        if ((a & (1 << LeftBit)) != 0) result |= PadButtons.Left;
        if ((a & (1 << DownBit)) != 0) result |= PadButtons.Down;
        if ((b & (1 << UpBit)) != 0) result |= PadButtons.Up;
        if ((b & (1 << RightBit)) != 0) result |= PadButtons.Right;
        if ((a & (1 << MenuLeftBit)) != 0) result |= PadButtons.MenuLeft;
        if ((a & (1 << CancelBit)) != 0) result |= PadButtons.Cancel;
        if ((a & (1 << SelectBit)) != 0) result |= PadButtons.Select;
        if ((a & (1 << MenuRightBit)) != 0) result |= PadButtons.MenuRight;
        return result;
    }

    /// <summary>
    /// LED 출력 리포트를 <paramref name="buffer"/>에 씁니다.
    /// 버퍼는 장치의 최대 출력 리포트 길이에 맞춰야 하며, 나머지 바이트는 0으로 채웁니다.
    /// </summary>
    public static void WriteLightReport(PadButtons lights, Span<byte> buffer)
    {
        if (buffer.Length < LightReportLength)
            throw new ArgumentException($"버퍼는 최소 {LightReportLength}바이트여야 합니다.", nameof(buffer));

        buffer.Clear();
        buffer[0] = LightReportId;
        buffer[LightIndexRight] = Bit(lights, PadButtons.Right);
        buffer[LightIndexDown] = Bit(lights, PadButtons.Down);
        buffer[LightIndexLeft] = Bit(lights, PadButtons.Left);
        buffer[LightIndexUp] = Bit(lights, PadButtons.Up);
    }

    private static byte Bit(PadButtons value, PadButtons flag) => (value & flag) != 0 ? (byte)1 : (byte)0;
}
