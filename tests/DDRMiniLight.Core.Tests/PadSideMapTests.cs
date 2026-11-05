using DDRMiniLight.Core;

namespace DDRMiniLight.Core.Tests;

public class PadSideMapTests
{
    private const string A = @"\\?\hid#vid_33dd&pid_0014#c&11791b0b&0&0000";
    private const string B = @"\\?\hid#vid_33dd&pid_0014#c&140959a&0&0000";
    private const string C = @"\\?\hid#vid_33dd&pid_0014#c&deadbeef&0&0000";

    [Fact]
    public void Assign_WithTwoConnected_AssignsOtherToOppositeSide()
    {
        var map = new PadSideMap();
        map.Assign(A, PadSide.P1, [A, B]);

        Assert.Equal(PadSide.P1, map.Get(A));
        Assert.Equal(PadSide.P2, map.Get(B));
    }

    [Fact]
    public void Assign_SameSideAsOther_SwapsSides()
    {
        var map = new PadSideMap();
        map.Assign(A, PadSide.P1, [A, B]);

        map.Assign(B, PadSide.P1, [A, B]);

        Assert.Equal(PadSide.P2, map.Get(A));
        Assert.Equal(PadSide.P1, map.Get(B));
    }

    [Fact]
    public void Assign_SingleConnected_DoesNotTouchOthers()
    {
        var map = new PadSideMap();
        map.Assign(A, PadSide.P2, [A]);

        Assert.Equal(PadSide.P2, map.Get(A));
        Assert.Equal(PadSide.Unassigned, map.Get(B));
    }

    [Fact]
    public void Assign_FromUnassigned_ClearsPreviousHolderOfThatSide()
    {
        var map = new PadSideMap();
        map.Assign(A, PadSide.P1, [A]);

        map.Assign(C, PadSide.P1, [A, B, C]);

        Assert.Equal(PadSide.P1, map.Get(C));
        Assert.Equal(PadSide.Unassigned, map.Get(A));
    }

    [Fact]
    public void Assign_Unassigned_RemovesEntry()
    {
        var map = new PadSideMap();
        map.Assign(A, PadSide.P1, [A, B]);

        map.Assign(A, PadSide.Unassigned, [A, B]);

        Assert.Equal(PadSide.Unassigned, map.Get(A));
        Assert.Equal(PadSide.P2, map.Get(B));
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var map = new PadSideMap();
        map.Assign(A, PadSide.P1, [A]);

        Assert.Equal(PadSide.P1, map.Get(A.ToUpperInvariant()));
    }

    [Fact]
    public void Constructor_DropsDuplicateSidesAndUnassigned()
    {
        var map = new PadSideMap(new Dictionary<string, PadSide>
        {
            [A] = PadSide.P1,
            [B] = PadSide.P1,
            [C] = PadSide.Unassigned,
        });

        Assert.Single(map.Entries);
        Assert.Equal(PadSide.P1, map.Get(A));
    }
}
