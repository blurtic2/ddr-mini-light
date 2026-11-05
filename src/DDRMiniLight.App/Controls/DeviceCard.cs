using DDRMiniLight.Core;

namespace DDRMiniLight.App.Controls;

/// <summary>장치 한 대의 1P/2P 지정, 패드 상태, LED 상태를 보여주는 카드.</summary>
internal sealed class DeviceCard : Panel
{
    private readonly DdrMiniDevice _device;
    private readonly PadView _pad;
    private readonly Label _badge;
    private readonly Label _status;
    private readonly RadioButton _p1Button;
    private readonly RadioButton _p2Button;
    private PadSide _side;
    private PadButtons _lastButtons;

    /// <param name="dpi">카드가 들어갈 창의 현재 DPI (96 기준으로 크기를 환산).</param>
    public DeviceCard(DdrMiniDevice device, int dpi)
    {
        _device = device;
        int S(int logical) => (int)Math.Round(logical * dpi / 96.0);

        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(S(8));
        Margin = new Padding(S(6));
        Size = new Size(S(190), S(296));

        // ── 머리글: 1P/2P 배지 + 제품명 ─────────────
        var header = new Panel { Dock = DockStyle.Top, Height = S(28) };
        _badge = new Label
        {
            Dock = DockStyle.Left,
            Width = S(40),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(Font.FontFamily, Font.Size + 2, FontStyle.Bold),
            ForeColor = Color.White,
        };
        var name = new Label
        {
            Text = device.DisplayName,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(S(6), 0, 0, 0),
            ForeColor = SystemColors.GrayText,
            AutoEllipsis = true,
        };
        header.Controls.Add(name);
        header.Controls.Add(_badge);

        _pad = new PadView { Dock = DockStyle.Fill };

        _status = new Label
        {
            Dock = DockStyle.Bottom,
            Height = S(20),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText,
        };

        // ── 하단 버튼: 1P / 2P 지정, 식별 ────────────
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = S(32),
            WrapContents = false,
            Padding = Padding.Empty,
        };
        _p1Button = SideButton("1P", PadSide.P1);
        _p2Button = SideButton("2P", PadSide.P2);
        var identify = new Button { Text = "식별", Size = new Size(S(56), S(26)), Margin = new Padding(S(6), S(2), 0, 0) };
        identify.Click += (_, _) => IdentifyRequested?.Invoke(this, EventArgs.Empty);
        buttons.Controls.Add(_p1Button);
        buttons.Controls.Add(_p2Button);
        buttons.Controls.Add(identify);

        Controls.Add(_pad);
        Controls.Add(_status);
        Controls.Add(buttons);
        Controls.Add(header);

        _device.ButtonsChanged += OnDeviceStateChanged;
        _device.LightsChanged += OnDeviceStateChanged;
        Side = PadSide.Unassigned;
        RefreshState();

        RadioButton SideButton(string text, PadSide side)
        {
            var button = new RadioButton
            {
                Text = text,
                Appearance = Appearance.Button,
                AutoCheck = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(S(44), S(26)),
                Margin = new Padding(0, S(2), S(4), 0),
            };
            button.Click += (_, _) => SideRequested?.Invoke(this, side);
            return button;
        }
    }

    public DdrMiniDevice Device => _device;

    /// <summary>사용자가 1P/2P 버튼을 눌렀을 때.</summary>
    public event EventHandler<PadSide>? SideRequested;

    /// <summary>사용자가 식별 버튼을 눌렀을 때.</summary>
    public event EventHandler? IdentifyRequested;

    /// <summary>아무것도 안 밟은 상태에서 화살표를 밟았을 때 (UI 스레드).</summary>
    public event EventHandler? Pressed;

    public PadSide Side
    {
        get => _side;
        set
        {
            _side = value;
            _badge.Text = value switch { PadSide.P1 => "1P", PadSide.P2 => "2P", _ => "?" };
            _badge.BackColor = SideColors.For(value);
            _p1Button.Checked = value == PadSide.P1;
            _p2Button.Checked = value == PadSide.P2;
        }
    }

    private void OnDeviceStateChanged(object? sender, PadButtons e)
    {
        // 장치 이벤트는 읽기 스레드에서 오므로 UI 스레드로 넘깁니다.
        if (IsHandleCreated && !IsDisposed)
            BeginInvoke(RefreshState);
    }

    private void RefreshState()
    {
        if (IsDisposed)
            return;

        var buttons = _device.Buttons;
        _pad.Buttons = buttons;
        _pad.Lights = _device.Lights;
        _status.Text = _device.SupportsLights
            ? $"입력: {Describe(buttons)}"
            : "LED 출력 미지원";

        if (_lastButtons == PadButtons.None && buttons != PadButtons.None)
            Pressed?.Invoke(this, EventArgs.Empty);
        _lastButtons = buttons;
    }

    private static string Describe(PadButtons buttons)
    {
        if (buttons == PadButtons.None)
            return "-";

        var parts = new List<string>(4);
        if (buttons.HasFlag(PadButtons.Left)) parts.Add("←");
        if (buttons.HasFlag(PadButtons.Down)) parts.Add("↓");
        if (buttons.HasFlag(PadButtons.Up)) parts.Add("↑");
        if (buttons.HasFlag(PadButtons.Right)) parts.Add("→");
        if (buttons.HasFlag(PadButtons.MenuLeft)) parts.Add("메뉴◀");
        if (buttons.HasFlag(PadButtons.Cancel)) parts.Add("취소");
        if (buttons.HasFlag(PadButtons.Select)) parts.Add("선택");
        if (buttons.HasFlag(PadButtons.MenuRight)) parts.Add("메뉴▶");
        return string.Join(" ", parts);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        RefreshState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _device.ButtonsChanged -= OnDeviceStateChanged;
            _device.LightsChanged -= OnDeviceStateChanged;
        }
        base.Dispose(disposing);
    }
}

internal static class SideColors
{
    public static Color For(PadSide side) => side switch
    {
        PadSide.P1 => Color.FromArgb(46, 120, 230),
        PadSide.P2 => Color.FromArgb(225, 70, 70),
        _ => Color.FromArgb(150, 150, 150),
    };
}
