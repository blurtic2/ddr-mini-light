using DDRMiniLight.Core;

namespace DDRMiniLight.Core.Tests;

public class DdrMiniProtocolTests
{
    [Theory]
    [InlineData(0b0010_0000, 0b0000_0000, PadButtons.Left)]
    [InlineData(0b0001_0000, 0b0000_0000, PadButtons.Down)]
    [InlineData(0b0000_0000, 0b0000_0100, PadButtons.Up)]
    [InlineData(0b0000_0000, 0b0000_1000, PadButtons.Right)]
    [InlineData(0b0011_0000, 0b0000_1100, PadButtons.All)]
    [InlineData(0b0000_0000, 0b0000_0000, PadButtons.None)]
    public void ParseInput_ReadsArrowBits(byte a, byte b, PadButtons expected)
    {
        byte[] report = [0x01, 0x00, 0x00, a, b];
        Assert.Equal(expected, DdrMiniProtocol.ParseInput(report));
    }

    [Fact]
    public void ParseInput_IgnoresOtherBits()
    {
        // 화살표가 아닌 비트(다른 버튼 등)는 무시해야 합니다.
        byte[] report = [0xFF, 0xFF, 0xFF, 0b1100_1111, 0b1111_0011];
        Assert.Equal(PadButtons.None, DdrMiniProtocol.ParseInput(report));
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
    [InlineData(PadButtons.All, new byte[] { 0x05, 1, 1, 1, 1 })]
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
        Assert.Throws<ArgumentException>(() => DdrMiniProtocol.WriteLightReport(PadButtons.All, new byte[4]));
    }
}
