using DDRMiniLight.App.Controls;
using DDRMiniLight.Core;

namespace DDRMiniLight.App;

/// <summary>
/// 연결된 컨트롤러의 입력/LED 상태와 설정, 로그를 보여주는 메인 창.
/// 설정 변경은 이벤트로 알리고, 실제 처리는 <see cref="TrayApplicationContext"/>가 합니다.
/// </summary>
internal sealed class MainForm : Form
{
    private const int MaxLogEntries = 500;

    private readonly FlowLayoutPanel _devicePanel;
    private readonly Label _emptyLabel;
    private readonly Dictionary<LightMode, RadioButton> _modeButtons = new();
    private readonly CheckBox _startupCheck;
    private readonly CheckBox _closeToTrayCheck;
    private readonly ListBox _logList;
    private bool _updatingControls;

    public MainForm(Icon icon)
    {
        // 아래의 모든 크기는 96 DPI 기준이며, ResumeLayout 시점에 현재 DPI로 자동 스케일됩니다.
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "DDR Mini Light";
        Icon = icon;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 660);
        MinimumSize = new Size(520, 600);

        // ── 장치 영역 ─────────────────────────────
        var deviceGroup = new GroupBox { Text = "컨트롤러", Dock = DockStyle.Fill, Padding = new Padding(8) };
        _devicePanel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoScroll = true, WrapContents = true };
        _emptyLabel = new Label
        {
            Text = "DDR Mini 컨트롤러를 찾는 중입니다.\nUSB로 연결하면 자동으로 인식합니다.",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = SystemColors.GrayText,
        };
        deviceGroup.Controls.Add(_devicePanel);
        deviceGroup.Controls.Add(_emptyLabel);

        // ── LED 모드 ─────────────────────────────
        var lightGroup = new GroupBox { Text = "LED", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var lightLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        foreach (var (mode, label) in LightModeText.All)
        {
            var radio = new RadioButton { Text = label, AutoSize = true, Tag = mode };
            radio.CheckedChanged += OnModeRadioChanged;
            _modeButtons[mode] = radio;
            lightLayout.Controls.Add(radio);
        }
        var testButton = new Button { Text = "LED 테스트", AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
        testButton.Click += (_, _) => LightTestRequested?.Invoke(this, EventArgs.Empty);
        lightLayout.Controls.Add(testButton);
        lightGroup.Controls.Add(lightLayout);

        // ── 옵션 ─────────────────────────────────
        var optionGroup = new GroupBox { Text = "옵션", Dock = DockStyle.Fill, Padding = new Padding(8) };
        var optionLayout = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
        _startupCheck = new CheckBox { Text = "Windows 시작 시 자동 실행", AutoSize = true };
        _startupCheck.CheckedChanged += (_, _) =>
        {
            if (!_updatingControls) StartupChanged?.Invoke(this, _startupCheck.Checked);
        };
        _closeToTrayCheck = new CheckBox { Text = "닫기 버튼을 누르면 트레이로 숨기기", AutoSize = true };
        _closeToTrayCheck.CheckedChanged += (_, _) =>
        {
            if (!_updatingControls) CloseToTrayChanged?.Invoke(this, _closeToTrayCheck.Checked);
        };
        optionLayout.Controls.Add(_startupCheck);
        optionLayout.Controls.Add(_closeToTrayCheck);
        optionGroup.Controls.Add(optionLayout);

        // ── 로그 ─────────────────────────────────
        var logGroup = new GroupBox { Text = "로그", Dock = DockStyle.Fill, Padding = new Padding(8) };
        _logList = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            HorizontalScrollbar = true,
            BorderStyle = BorderStyle.None,
        };
        logGroup.Controls.Add(_logList);

        // ── 전체 배치 ─────────────────────────────
        var settingsRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = Padding.Empty };
        settingsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        settingsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        settingsRow.Controls.Add(lightGroup, 0, 0);
        settingsRow.Controls.Add(optionGroup, 1, 0);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(deviceGroup, 0, 0);
        root.Controls.Add(settingsRow, 0, 1);
        root.Controls.Add(logGroup, 0, 2);

        Controls.Add(root);
        UpdateEmptyState();

        ResumeLayout(false);
        PerformLayout();
    }

    public event EventHandler<LightMode>? LightModeSelected;
    public event EventHandler? LightTestRequested;
    public event EventHandler<bool>? StartupChanged;
    public event EventHandler<bool>? CloseToTrayChanged;

    /// <summary>닫기 버튼을 눌렀을 때 숨기기만 할지 여부. 컨텍스트가 설정합니다.</summary>
    public bool HideOnClose { get; set; } = true;

    /// <summary>창이 트레이로 숨겨질 때.</summary>
    public event EventHandler? HiddenToTray;

    public void SetOptions(LightMode mode, bool startup, bool closeToTray)
    {
        _updatingControls = true;
        try
        {
            _modeButtons[mode].Checked = true;
            _startupCheck.Checked = startup;
            _closeToTrayCheck.Checked = closeToTray;
        }
        finally
        {
            _updatingControls = false;
        }
    }

    public void AddDevice(DdrMiniDevice device)
    {
        if (_devicePanel.Controls.OfType<DeviceCard>().Any(c => c.Device == device))
            return;
        // 폼의 자동 스케일 이후에 추가되는 컨트롤이라 현재 DPI를 직접 넘깁니다.
        _devicePanel.Controls.Add(new DeviceCard(device, DeviceDpi));
        UpdateEmptyState();
    }

    public void RemoveDevice(DdrMiniDevice device)
    {
        var card = _devicePanel.Controls.OfType<DeviceCard>().FirstOrDefault(c => c.Device == device);
        if (card is null)
            return;
        _devicePanel.Controls.Remove(card);
        card.Dispose();
        UpdateEmptyState();
    }

    public void AppendLog(string message)
    {
        _logList.BeginUpdate();
        _logList.Items.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        while (_logList.Items.Count > MaxLogEntries)
            _logList.Items.RemoveAt(0);
        _logList.EndUpdate();
        _logList.TopIndex = _logList.Items.Count - 1;
    }

    public void ShowAndActivate()
    {
        if (!Visible)
            Show();
        if (WindowState == FormWindowState.Minimized)
            WindowState = FormWindowState.Normal;
        Activate();
    }

    private void OnModeRadioChanged(object? sender, EventArgs e)
    {
        if (_updatingControls || sender is not RadioButton { Checked: true, Tag: LightMode mode })
            return;
        LightModeSelected?.Invoke(this, mode);
    }

    private void UpdateEmptyState()
    {
        bool hasDevices = _devicePanel.Controls.Count > 0;
        _devicePanel.Visible = hasDevices;
        _emptyLabel.Visible = !hasDevices;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (e.CloseReason == CloseReason.UserClosing && HideOnClose)
        {
            e.Cancel = true;
            Hide();
            HiddenToTray?.Invoke(this, EventArgs.Empty);
            return;
        }
        base.OnFormClosing(e);
    }
}
