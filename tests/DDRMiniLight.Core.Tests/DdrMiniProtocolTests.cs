using DDRMiniLight.Core;

namespace DDRMiniLight.Core.Tests;

public class DdrMiniProtocolTests
{
    [Theory]
    [InlineData(0b0010_0000, 0b0000_0000, PadButtons.Left)]
    [InlineData(0b0001_0000, 0b0000_0000, PadButtons.Down)]
    [InlineData(0b0000_0000, 0b0000_0100, PadButtons.Up)]
    [InlineData(0b0000_0000, 0b0000_1000, PadButtons.Right)]
    [InlineData(0b0011_0000, 0b0000_1100, PadButtons.Arrows)]
    [InlineData(0b0000_0000, 0b0000_0000, PadButtons.None)]
    public void ParseInput_ReadsArrowBits(byte a, byte b, PadButtons expected)
    {
        byte[] report = [0x01, 0x00, 0x00, a, b];
        Assert.Equal(expected, DdrMiniProtocol.ParseInput(report));
    }

    // 실제 패드에서 기록한 리포트 (1P/2P 동일)
    [Theory]
    [InlineData("0100004000", PadButtons.MenuLeft)]
    [InlineData("0100000200", PadButtons.Cancel)]
    [InlineData("0100000100", PadButtons.Select)]
    [InlineData("0100008000", PadButtons.MenuRight)]
    [InlineData("010000C300", PadButtons.Menu)]
    public void ParseInput_ReadsMenuButtons(string hex, PadButtons expected)
    {
        Assert.Equal(expected, DdrMiniProtocol.ParseInput(Convert.FromHexString(hex)));
    }

    [Fact]
    public void ParseInput_IgnoresUnusedBits()
    {
        // 쓰지 않는 버튼 비트와 X/Y축은 무시해야 합니다.
        byte[] report = [0xFF, 0xFF, 0xFF, 0b0000_1100, 0b1111_0011];
        Assert.Equal(PadButtons.None, DdrMiniProtocol.ParseInput(report));
    }

    [Fact]
    public void WriteLightReport_IgnoresMenuButtons()
    {
        var buffer = new byte[5];
        DdrMiniProtocol.WriteLightReport(PadButtons.Menu | PadButtons.Up, buffer);
        Assert.Equal(new byte[] { 0x05, 0, 0, 0, 1 }, buffer);
    }

    [Fact]
    public void ParseInput_ShortReport_ReturnsNone()
    {
        Assert.Equal(PadButtons.None, DdrMiniProtocol.ParseInput(new byte[] { 0x01, 0x00, 0x00, 0xFF }));
    }

    [Theory]
    [InlineData(PadButtons.None, new byte[] { 0x05, 0, 0, 0, 0 })]
    [InlineData(PadButtons.Right, new byte[] { 0x05, 1, 0, 0, 0 })]
    [InlineData(PadButtons.Down, new byte[] { 0x05, 0, 1, 0, 0 })]
    [InlineData(PadButtons.Left, new byte[] { 0x05, 0, 0, 1, 0 })]
    [InlineData(PadButtons.Up, new byte[] { 0x05, 0, 0, 0, 1 })]
    [InlineData(PadButtons.Arrows, new byte[] { 0x05, 1, 1, 1, 1 })]
    public void WriteLightReport_MatchesDeviceLayout(PadButtons lights, byte[] expected)
    {
        var buffer = new byte[5];
        DdrMiniProtocol.WriteLightReport(lights, buffer);
        Assert.Equal(expected, buffer);
    }

    [Fact]
    public void WriteLightReport_ClearsTrailingBytes()
    {
        var buffer = Enumerable.Repeat((byte)0xAA, 9).ToArray();
        DdrMiniProtocol.WriteLightReport(PadButtons.Up, buffer);
        Assert.Equal(new byte[] { 0x05, 0, 0, 0, 1, 0, 0, 0, 0 }, buffer);
    }

    [Fact]
    public void WriteLightReport_BufferTooShort_Throws()
    {
        Assert.Throws<ArgumentException>(() => DdrMiniProtocol.WriteLightReport(PadButtons.Arrows, new byte[4]));
    }
}
