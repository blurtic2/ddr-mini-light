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
    private readonly Button _assignButton;
    private readonly Label _assignPrompt;
    private bool _updatingControls;

    // "밟아서 지정" 진행 상태: 지금 지정을 기다리는 쪽 (Unassigned면 진행 중 아님)
    private PadSide _assigningSide;
    private DdrMiniDevice? _assignedInThisRun;

    public MainForm(Icon icon)
    {
        // 아래의 모든 크기는 96 DPI 기준이며, ResumeLayout 시점에 현재 DPI로 자동 스케일됩니다.
        SuspendLayout();
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        Text = "DDR Mini Light";
        Icon = icon;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(640, 798);
        MinimumSize = new Size(520, 748);
        KeyPreview = true;

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
        var assignBar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false };
        _assignButton = new Button { Text = "밟아서 1P/2P 지정", AutoSize = true };
        _assignButton.Click += (_, _) =>
        {
            if (_assigningSide == PadSide.Unassigned)
                StartAssign();
            else
                EndAssign("지정을 취소했습니다.");
        };
        _assignPrompt = new Label
        {
            AutoSize = true,
            Margin = new Padding(8, 8, 3, 0),
            ForeColor = SystemColors.GrayText,
            Text = "1P/2P는 꽂힌 USB 포트로 구분합니다.",
        };
        assignBar.Controls.Add(_assignButton);
        assignBar.Controls.Add(_assignPrompt);

        deviceGroup.Controls.Add(_devicePanel);
        deviceGroup.Controls.Add(_emptyLabel);
        deviceGroup.Controls.Add(assignBar);

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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 424));
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

    /// <summary>사용자가 패드를 1P/2P로 지정했을 때 (카드 버튼 또는 밟아서 지정).</summary>
    public event EventHandler<(DdrMiniDevice Device, PadSide Side)>? SideAssignRequested;

    /// <summary>사용자가 패드 식별(LED 깜빡임)을 요청했을 때.</summary>
    public event EventHandler<DdrMiniDevice>? IdentifyRequested;

    private IEnumerable<DeviceCard> Cards => _devicePanel.Controls.OfType<DeviceCard>();

    public void AddDevice(DdrMiniDevice device, PadSide side)
    {
        if (Cards.Any(c => c.Device == device))
            return;

        // 폼의 자동 스케일 이후에 추가되는 컨트롤이라 현재 DPI를 직접 넘깁니다.
        var card = new DeviceCard(device, DeviceDpi) { Side = side };
        card.SideRequested += (_, s) => SideAssignRequested?.Invoke(this, (device, s));
        card.IdentifyRequested += (_, _) => IdentifyRequested?.Invoke(this, device);
        card.Pressed += (_, _) => OnCardPressed(card);
        _devicePanel.Controls.Add(card);
        SortCards();
        UpdateEmptyState();
        UpdateAssignHint();
    }

    public void RemoveDevice(DdrMiniDevice device)
    {
        var card = Cards.FirstOrDefault(c => c.Device == device);
        if (card is null)
            return;
        _devicePanel.Controls.Remove(card);
        card.Dispose();
        if (_assignedInThisRun == device)
            _assignedInThisRun = null;
        UpdateEmptyState();
        UpdateAssignHint();
    }

    /// <summary>각 카드의 1P/2P 표시를 갱신하고 1P → 2P → 미지정 순으로 정렬합니다.</summary>
    public void ApplySides(Func<DdrMiniDevice, PadSide> getSide)
    {
        foreach (var card in Cards)
            card.Side = getSide(card.Device);
        SortCards();
        UpdateAssignHint();
    }

    /// <summary>지정 진행 중이 아닐 때, 미지정 패드가 있으면 안내 문구를 보여줍니다.</summary>
    private void UpdateAssignHint()
    {
        if (_assigningSide != PadSide.Unassigned)
            return;

        if (Cards.Any(c => c.Side == PadSide.Unassigned))
        {
            _assignPrompt.ForeColor = SystemColors.HotTrack;
            _assignPrompt.Text = "1P/2P가 지정되지 않은 패드가 있습니다.";
        }
        else
        {
            _assignPrompt.ForeColor = SystemColors.GrayText;
            _assignPrompt.Text = "1P/2P는 꽂힌 USB 포트로 구분합니다.";
        }
    }

    private void SortCards()
    {
        var ordered = Cards
            .OrderBy(c => c.Side == PadSide.Unassigned ? int.MaxValue : (int)c.Side)
            .ToList();
        for (int i = 0; i < ordered.Count; i++)
            _devicePanel.Controls.SetChildIndex(ordered[i], i);
    }

    // ── 밟아서 지정 ────────────────────────────────

    private void StartAssign()
    {
        if (!Cards.Any())
        {
            AppendLog("1P/2P 지정: 연결된 패드가 없습니다.");
            return;
        }
        _assignedInThisRun = null;
        SetAssignStep(PadSide.P1);
    }

    private void SetAssignStep(PadSide side)
    {
        _assigningSide = side;
        _assignButton.Text = "취소";
        _assignPrompt.ForeColor = SideColors.For(side);
        _assignPrompt.Text = $"{(side == PadSide.P1 ? "1P" : "2P")} 패드의 아무 버튼이나 누르세요.  (Esc: 취소)";
    }

    private void EndAssign(string message)
    {
        _assigningSide = PadSide.Unassigned;
        _assignedInThisRun = null;
        _assignButton.Text = "밟아서 1P/2P 지정";
        _assignPrompt.ForeColor = SystemColors.GrayText;
        _assignPrompt.Text = message;
    }

    private void OnCardPressed(DeviceCard card)
    {
        if (_assigningSide == PadSide.Unassigned || card.Device == _assignedInThisRun)
            return;

        var side = _assigningSide;
        SideAssignRequested?.Invoke(this, (card.Device, side));

        // 두 대뿐이면 나머지는 자동으로 반대쪽이 되므로 1P만 받으면 끝납니다.
        int remaining = Cards.Count(c => c.Device != card.Device);
        if (side == PadSide.P1 && remaining > 1)
        {
            _assignedInThisRun = card.Device;
            SetAssignStep(PadSide.P2);
        }
        else
        {
            EndAssign("지정했습니다. 다음에도 같은 USB 포트에 꽂으면 그대로 유지됩니다.");
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape && _assigningSide != PadSide.Unassigned)
        {
            EndAssign("지정을 취소했습니다.");
            e.Handled = true;
        }
        base.OnKeyDown(e);
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
