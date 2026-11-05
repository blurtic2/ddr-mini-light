using HidSharp;

namespace DDRMiniLight.Core;

/// <summary>
/// DDR Mini 컨트롤러의 연결/분리를 감시하고, 연결된 장치를 관리합니다.
/// 이벤트는 백그라운드 스레드에서 발생할 수 있습니다.
/// </summary>
public sealed class DeviceManager : IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, DdrMiniDevice> _devices = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _failedPaths = new(StringComparer.OrdinalIgnoreCase);
    private LightMode _lightMode;
    private bool _started;
    private bool _disposed;
    private int _testRunning;

    public DeviceManager(LightMode lightMode = LightMode.Mirror)
    {
        _lightMode = lightMode;
    }

    public event EventHandler<DdrMiniDevice>? DeviceConnected;
    public event EventHandler<DdrMiniDevice>? DeviceDisconnected;
    public event EventHandler<string>? Log;

    public IReadOnlyList<DdrMiniDevice> Devices
    {
        get
        {
            lock (_sync)
                return _devices.Values.ToList();
        }
    }

    public LightMode LightMode
    {
        get => _lightMode;
        set
        {
            _lightMode = value;
            foreach (var device in Devices)
                device.LightMode = value;
        }
    }

    public void Start()
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_started)
                return;
            _started = true;
        }

        DeviceList.Local.Changed += OnDeviceListChanged;
        Refresh();
    }

    /// <summary>장치 목록을 다시 확인해서 새 장치는 열고, 사라진 장치는 정리합니다.</summary>
    public void Refresh()
    {
        List<HidDevice> present;
        try
        {
            present = DeviceList.Local
                .GetHidDevices(DdrMiniProtocol.VendorId, DdrMiniProtocol.ProductId)
                .Where(DdrMiniDevice.IsCandidate)
                .ToList();
        }
        catch (Exception ex)
        {
            RaiseLog($"장치 목록을 가져오지 못했습니다: {ex.Message}");
            return;
        }

        var connected = new List<DdrMiniDevice>();
        var removed = new List<DdrMiniDevice>();

        lock (_sync)
        {
            if (_disposed)
                return;

            var presentPaths = present.Select(d => d.DevicePath).ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (path, device) in _devices.ToList())
            {
                if (presentPaths.Contains(path))
                    continue;
                _devices.Remove(path);
                removed.Add(device);
            }

            // 사라진 장치는 다시 꽂으면 재시도할 수 있게 실패 기록에서 지웁니다.
            _failedPaths.IntersectWith(presentPaths);

            foreach (var hid in present)
            {
                if (_devices.ContainsKey(hid.DevicePath) || _failedPaths.Contains(hid.DevicePath))
                    continue;

                var device = DdrMiniDevice.TryOpen(hid, _lightMode, out var error);
                if (device is null)
                {
                    _failedPaths.Add(hid.DevicePath);
                    RaiseLog($"장치 열기 실패: {error}");
                    continue;
                }

                device.Disconnected += OnDeviceDisconnected;
                _devices.Add(device.Path, device);
                connected.Add(device);
            }
        }

        foreach (var device in connected)
            device.Start();

        foreach (var device in removed)
            CloseDevice(device, "분리됨");

        foreach (var device in connected)
        {
            RaiseLog($"연결됨: {device.DisplayName}" + (device.SupportsLights ? "" : " (LED 출력 미지원)"));
            DeviceConnected?.Invoke(this, device);
        }
    }

    /// <summary>모든 장치의 LED를 순서대로 켜 보는 테스트.</summary>
    public async Task RunLightTestAsync(CancellationToken cancellationToken = default)
    {
        if (Interlocked.Exchange(ref _testRunning, 1) != 0)
            return;

        PadButtons[] sequence =
        [
            PadButtons.Left, PadButtons.Down, PadButtons.Up, PadButtons.Right,
            PadButtons.All, PadButtons.None, PadButtons.All, PadButtons.None,
        ];

        try
        {
            foreach (var step in sequence)
            {
                foreach (var device in Devices)
                    device.SetLightOverride(step);
                await Task.Delay(180, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            foreach (var device in Devices)
                device.SetLightOverride(null);
            Interlocked.Exchange(ref _testRunning, 0);
        }
    }

    private void OnDeviceListChanged(object? sender, DeviceListChangedEventArgs e) => Refresh();

    private void OnDeviceDisconnected(object? sender, Exception? error)
    {
        if (sender is not DdrMiniDevice device)
            return;

        lock (_sync)
        {
            if (!_devices.Remove(device.Path))
                return;
        }

        // 읽기 스레드에서 호출되므로 정리는 다른 스레드에서 합니다.
        ThreadPool.QueueUserWorkItem(_ => CloseDevice(device, error is null ? "분리됨" : $"연결 끊김 ({error.Message})"));
    }

    private void CloseDevice(DdrMiniDevice device, string reason)
    {
        device.Disconnected -= OnDeviceDisconnected;
        device.Dispose();
        RaiseLog($"{reason}: {device.DisplayName}");
        DeviceDisconnected?.Invoke(this, device);
    }

    private void RaiseLog(string message) => Log?.Invoke(this, message);

    public void Dispose()
    {
        List<DdrMiniDevice> devices;
        lock (_sync)
        {
            if (_disposed)
                return;
            _disposed = true;
            devices = _devices.Values.ToList();
            _devices.Clear();
        }

        if (_started)
            DeviceList.Local.Changed -= OnDeviceListChanged;

        foreach (var device in devices)
        {
            device.Disconnected -= OnDeviceDisconnected;
            device.Dispose();
        }
    }
}
