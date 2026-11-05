using DDRMiniLight.Core;

namespace DDRMiniLight.App.Controls;

/// <summary>장치 한 대의 이름, 패드 상태, LED 상태를 보여주는 카드.</summary>
internal sealed class DeviceCard : Panel
{
    private readonly DdrMiniDevice _device;
    private readonly PadView _pad;
    private readonly Label _status;

    /// <param name="dpi">카드가 들어갈 창의 현재 DPI (96 기준으로 크기를 환산).</param>
    public DeviceCard(DdrMiniDevice device, int dpi)
    {
        _device = device;
        int S(int logical) => (int)Math.Round(logical * dpi / 96.0);

        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(S(8));
        Margin = new Padding(S(6));
        Size = new Size(S(180), S(220));

        var title = new Label
        {
            Text = device.DisplayName,
            Dock = DockStyle.Top,
            Height = S(22),
            Font = new Font(Font, FontStyle.Bold),
            AutoEllipsis = true,
        };

        _pad = new PadView { Dock = DockStyle.Fill };

        _status = new Label
        {
            Dock = DockStyle.Bottom,
            Height = S(20),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText,
        };

        Controls.Add(_pad);
        Controls.Add(_status);
        Controls.Add(title);

        _device.ButtonsChanged += OnDeviceStateChanged;
        _device.LightsChanged += OnDeviceStateChanged;
        RefreshState();
    }

    public DdrMiniDevice Device => _device;

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

        _pad.Buttons = _device.Buttons;
        _pad.Lights = _device.Lights;
        _status.Text = _device.SupportsLights
            ? $"입력: {Describe(_device.Buttons)}"
            : "LED 출력 미지원";
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
