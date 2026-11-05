using DDRMiniLight.Core;

namespace DDRMiniLight.App;

/// <summary>
/// 앱 전체 수명을 관리합니다: 장치 관리자, 트레이 아이콘, 메인 창, 설정.
/// 창을 닫아도 트레이에 남아 LED 제어를 계속합니다.
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly PadSideMap _sides;
    private readonly DeviceManager _manager;
    private readonly MainForm _form;
    private readonly NotifyIcon _trayIcon;
    private readonly Icon _activeIcon;
    private readonly Icon _inactiveIcon;
    private readonly Dictionary<LightMode, ToolStripMenuItem> _modeMenuItems = new();
    private readonly ToolStripMenuItem _startupMenuItem;
    private readonly EventWaitHandle _showWindowEvent;
    private readonly RegisteredWaitHandle _showWindowRegistration;
    private bool _exiting;
    private bool _shutdownStarted;

    public TrayApplicationContext(bool startMinimized)
    {
        _settings = AppSettings.Load();
        _sides = new PadSideMap(_settings.PadSides);

        _activeIcon = AppIcons.Create(SystemInformation.SmallIconSize.Width, active: true);
        _inactiveIcon = AppIcons.Create(SystemInformation.SmallIconSize.Width, active: false);

        // ── 메인 창 ──────────────────────────────
        _form = new MainForm(AppIcons.Create(32, active: true)) { HideOnClose = _settings.CloseToTray };
        _form.SetOptions(_settings.LightMode, StartupRegistration.IsEnabled, _settings.CloseToTray);
        _form.LightModeSelected += (_, mode) => SetLightMode(mode);
        _form.LightTestRequested += (_, _) => RunLightTest();
        _form.StartupChanged += (_, enabled) => SetStartup(enabled);
        _form.CloseToTrayChanged += (_, enabled) => SetCloseToTray(enabled);
        _form.HiddenToTray += (_, _) => OnHiddenToTray();
        _form.SideAssignRequested += (_, e) => AssignSide(e.Device, e.Side);
        _form.IdentifyRequested += (_, device) => _ = device.BlinkAsync();
        _form.FormClosed += (_, _) => ExitThread();
        _ = _form.Handle; // 숨긴 상태에서도 BeginInvoke를 쓸 수 있도록 핸들을 미리 만듭니다.

        // ── 트레이 메뉴 ───────────────────────────
        var menu = new ContextMenuStrip();
        var openItem = new ToolStripMenuItem("창 열기", null, (_, _) => _form.ShowAndActivate())
        {
            Font = new Font(menu.Font, FontStyle.Bold),
        };
        var modeMenu = new ToolStripMenuItem("LED 모드");
        foreach (var (mode, label) in LightModeText.All)
        {
            var item = new ToolStripMenuItem(label, null, (_, _) => SetLightMode(mode));
            _modeMenuItems[mode] = item;
            modeMenu.DropDownItems.Add(item);
        }
        _startupMenuItem = new ToolStripMenuItem("Windows 시작 시 자동 실행", null, (_, _) => SetStartup(!_startupMenuItem!.Checked))
        {
            Checked = StartupRegistration.IsEnabled,
        };

        menu.Items.Add(openItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(modeMenu);
        menu.Items.Add(new ToolStripMenuItem("LED 테스트", null, (_, _) => RunLightTest()));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(_startupMenuItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("종료", null, (_, _) => Exit()));

        _trayIcon = new NotifyIcon
        {
            Icon = _inactiveIcon,
            Text = "DDR Mini Light",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
                _form.ShowAndActivate();
        };
        UpdateModeMenu();

        // ── 다른 인스턴스가 실행되면 창을 띄웁니다 ─────
        _showWindowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ShowWindowEventName);
        _showWindowRegistration = ThreadPool.RegisterWaitForSingleObject(
            _showWindowEvent,
            (_, _) => RunOnUi(_form.ShowAndActivate),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);

        // ── 장치 관리자 ───────────────────────────
        _manager = new DeviceManager(_settings.LightMode);
        _manager.Log += (_, message) => RunOnUi(() => _form.AppendLog(message));
        _manager.DeviceConnected += (_, device) => RunOnUi(() =>
        {
            _form.AddDevice(device, _sides.Get(device.Path));
            UpdateTrayStatus();
        });
        _manager.DeviceDisconnected += (_, device) => RunOnUi(() =>
        {
            _form.RemoveDevice(device);
            UpdateTrayStatus();
        });

        _form.AppendLog("장치 검색을 시작합니다.");
        _manager.Start();
        UpdateTrayStatus();

        if (!startMinimized)
            _form.ShowAndActivate();
    }

    private void AssignSide(DdrMiniDevice device, PadSide side)
    {
        var connected = _manager.Devices.Select(d => d.Path).ToList();
        _sides.Assign(device.Path, side, connected);
        _settings.PadSides = new Dictionary<string, PadSide>(_sides.Entries, StringComparer.OrdinalIgnoreCase);
        _settings.Save();

        _form.ApplySides(d => _sides.Get(d.Path));
        _form.AppendLog($"{SideText(side)}로 지정했습니다.");
        UpdateTrayStatus();
        _ = device.BlinkAsync(times: 1);
    }

    private static string SideText(PadSide side) => side switch
    {
        PadSide.P1 => "1P",
        PadSide.P2 => "2P",
        _ => "미지정",
    };

    private void SetLightMode(LightMode mode)
    {
        _manager.LightMode = mode;
        _settings.LightMode = mode;
        _settings.Save();
        _form.SetOptions(mode, StartupRegistration.IsEnabled, _settings.CloseToTray);
        UpdateModeMenu();
    }

    private void SetStartup(bool enabled)
    {
        try
        {
            StartupRegistration.SetEnabled(enabled);
        }
        catch (Exception ex)
        {
            _form.AppendLog($"자동 실행 설정 실패: {ex.Message}");
        }

        bool actual = StartupRegistration.IsEnabled;
        _startupMenuItem.Checked = actual;
        _form.SetOptions(_settings.LightMode, actual, _settings.CloseToTray);
    }

    private void SetCloseToTray(bool enabled)
    {
        _settings.CloseToTray = enabled;
        _settings.Save();
        _form.HideOnClose = enabled;
    }

    private void RunLightTest()
    {
        if (_manager.Devices.Count == 0)
        {
            _form.AppendLog("LED 테스트: 연결된 장치가 없습니다.");
            return;
        }
        _ = _manager.RunLightTestAsync();
    }

    private void OnHiddenToTray()
    {
        if (_settings.TrayHintShown)
            return;

        _trayIcon.ShowBalloonTip(
            3000,
            "DDR Mini Light",
            "트레이에서 계속 실행 중입니다. 종료하려면 트레이 아이콘을 우클릭하세요.",
            ToolTipIcon.Info);
        _settings.TrayHintShown = true;
        _settings.Save();
    }

    private void UpdateModeMenu()
    {
        foreach (var (mode, item) in _modeMenuItems)
            item.Checked = mode == _settings.LightMode;
    }

    private void UpdateTrayStatus()
    {
        var sides = _manager.Devices
            .Select(d => _sides.Get(d.Path))
            .OrderBy(s => s == PadSide.Unassigned ? int.MaxValue : (int)s)
            .Select(SideText)
            .ToList();
        _trayIcon.Icon = sides.Count > 0 ? _activeIcon : _inactiveIcon;
        _trayIcon.Text = sides.Count > 0
            ? $"DDR Mini Light - {string.Join(", ", sides)} 연결됨"
            : "DDR Mini Light - 연결된 장치 없음";
    }

    private void RunOnUi(Action action)
    {
        if (_exiting || _form.IsDisposed || !_form.IsHandleCreated)
            return;
        try
        {
            _form.BeginInvoke(action);
        }
        catch (InvalidOperationException)
        {
            // 종료 중 핸들이 사라진 경우
        }
    }

    private void Exit()
    {
        _exiting = true;
        ExitThread();
    }

    protected override void ExitThreadCore()
    {
        if (_shutdownStarted)
            return;
        _shutdownStarted = true;
        _exiting = true;
        _trayIcon.Visible = false;
        _manager.Dispose(); // 모든 LED를 끄고 장치를 닫습니다.
        if (!_form.IsDisposed && _form.Visible)
        {
            _form.HideOnClose = false;
            _form.Close();
        }
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _showWindowRegistration.Unregister(null);
            _showWindowEvent.Dispose();
            _trayIcon.Dispose();
            _form.Dispose();
        }
        base.Dispose(disposing);
    }
}
