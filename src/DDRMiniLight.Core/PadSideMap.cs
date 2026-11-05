namespace DDRMiniLight.Core;

/// <summary>
/// 장치 경로(USB 포트마다 고유) → 1P/2P 지정 정보.
/// 1P/2P 패드는 펌웨어가 같아서 꽂힌 포트로만 구분할 수 있습니다.
/// </summary>
public sealed class PadSideMap
{
    private readonly Dictionary<string, PadSide> _map = new(StringComparer.OrdinalIgnoreCase);

    public PadSideMap(IEnumerable<KeyValuePair<string, PadSide>>? entries = null)
    {
        if (entries is null)
            return;

        foreach (var (path, side) in entries)
        {
            if (side != PadSide.Unassigned && !_map.ContainsValue(side))
                _map[path] = side;
        }
    }

    public IReadOnlyDictionary<string, PadSide> Entries => _map;

    public PadSide Get(string path) => _map.TryGetValue(path, out var side) ? side : PadSide.Unassigned;

    /// <summary>
    /// <paramref name="path"/>를 <paramref name="side"/>로 지정합니다.
    /// 같은 쪽으로 지정돼 있던 다른 패드는 서로 맞바꾸고,
    /// 연결된 패드가 두 대이고 다른 한 대가 미지정이면 반대쪽으로 자동 지정합니다.
    /// </summary>
    public void Assign(string path, PadSide side, IReadOnlyCollection<string> connectedPaths)
    {
        var previous = Get(path);

        if (side == PadSide.Unassigned)
        {
            _map.Remove(path);
            return;
        }

        var conflicts = _map
            .Where(e => e.Value == side && !string.Equals(e.Key, path, StringComparison.OrdinalIgnoreCase))
            .Select(e => e.Key)
            .ToList();
        foreach (var other in conflicts)
        {
            if (previous != PadSide.Unassigned)
                _map[other] = previous;
            else
                _map.Remove(other);
        }

        _map[path] = side;

        var others = connectedPaths
            .Where(p => !string.Equals(p, path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (others.Count == 1 && Get(others[0]) == PadSide.Unassigned)
            _map[others[0]] = Opposite(side);
    }

    public static PadSide Opposite(PadSide side) => side switch
    {
        PadSide.P1 => PadSide.P2,
        PadSide.P2 => PadSide.P1,
        _ => PadSide.Unassigned,
    };
}
