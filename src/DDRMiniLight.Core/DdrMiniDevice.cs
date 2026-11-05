using HidSharp;

namespace DDRMiniLight.Core;

/// <summary>
/// 연결된 DDR Mini 컨트롤러 한 대. 전용 스레드에서 입력을 읽고, 모드에 따라 LED를 갱신합니다.
/// 이벤트는 읽기 스레드에서 발생하므로 UI에서는 마샬링이 필요합니다.
/// </summary>
public sealed class DdrMiniDevice : IDisposable
{
    private const int ReadTimeoutMs = 250;

    private readonly HidDevice _device;
    private readonly HidStream _stream;
    private readonly byte[] _inputBuffer;
    private readonly byte[] _outputBuffer;
    private readonly object _writeLock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Thread _readThread;

    private PadButtons _buttons;
    private PadButtons? _lastWrittenLights;
    private PadButtons? _lightOverride;
    private LightMode _lightMode;
    private int _disposed;

    private DdrMiniDevice(HidDevice device, HidStream stream, LightMode lightMode)
    {
        _device = device;
        _stream = stream;
        _lightMode = lightMode;
        _inputBuffer = new byte[device.GetMaxInputReportLength()];
        _outputBuffer = new byte[Math.Max(device.GetMaxOutputReportLength(), 0)];

        _stream.ReadTimeout = ReadTimeoutMs;

        Path = device.DevicePath;
        DisplayName = BuildDisplayName(device);

        _readThread = new Thread(ReadLoop)
        {
            IsBackground = true,
            Name = $"DDRMini reader ({DisplayName})",
            Priority = ThreadPriority.AboveNormal,
        };
    }

    /// <summary>장치를 구분하는 고유 경로.</summary>
    public string Path { get; }

    public string DisplayName { get; }

    /// <summary>장치가 LED 출력 리포트를 받을 수 있는지 여부.</summary>
    public bool SupportsLights => _outputBuffer.Length >= DdrMiniProtocol.LightReportLength;

    /// <summary>현재 눌린 버튼.</summary>
    public PadButtons Buttons => _buttons;

    /// <summary>마지막으로 장치에 보낸 LED 상태.</summary>
    public PadButtons Lights => _lastWrittenLights ?? PadButtons.None;

    public LightMode LightMode
    {
        get => _lightMode;
        set
        {
            _lightMode = value;
            UpdateLights();
        }
    }

    /// <summary>버튼 상태가 바뀌었을 때 (읽기 스레드).</summary>
    public event EventHandler<PadButtons>? ButtonsChanged;

    /// <summary>LED 상태를 장치에 보냈을 때.</summary>
    public event EventHandler<PadButtons>? LightsChanged;

    /// <summary>장치가 분리되었거나 통신에 실패했을 때. 한 번만 발생합니다.</summary>
    public event EventHandler<Exception?>? Disconnected;

    /// <summary>HID 장치가 DDR Mini 입력 인터페이스인지 확인합니다.</summary>
    public static bool IsCandidate(HidDevice device)
    {
        try
        {
            return device.VendorID == DdrMiniProtocol.VendorId
                && device.ProductID == DdrMiniProtocol.ProductId
                && device.GetMaxInputReportLength() >= DdrMiniProtocol.MinInputReportLength;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 장치를 엽니다. 실패하면 <c>null</c>과 오류 메시지를 반환합니다.
    /// 이벤트를 구독한 뒤 <see cref="Start"/>를 호출해야 입력을 읽기 시작합니다.
    /// </summary>
    public static DdrMiniDevice? TryOpen(HidDevice device, LightMode lightMode, out string? error)
    {
        HidStream? stream = null;
        try
        {
            if (!device.TryOpen(out stream))
            {
                error = "장치를 열 수 없습니다. 다른 프로그램이 사용 중일 수 있습니다.";
                return null;
            }

            error = null;
            return new DdrMiniDevice(device, stream, lightMode);
        }
        catch (Exception ex)
        {
            stream?.Dispose();
            error = ex.Message;
            return null;
        }
    }

    /// <summary>입력 읽기 스레드를 시작하고 현재 모드의 LED 상태를 적용합니다.</summary>
    public void Start()
    {
        if (_readThread.ThreadState.HasFlag(ThreadState.Unstarted))
            _readThread.Start();
        UpdateLights();
    }

    /// <summary>모드와 관계없이 일정 시간 동안 LED를 직접 지정합니다. <c>null</c>이면 해제.</summary>
    public void SetLightOverride(PadButtons? lights)
    {
        _lightOverride = lights;
        UpdateLights();
    }

    /// <summary>이 패드의 LED만 깜빡여서 어느 패드인지 알려줍니다.</summary>
    public async Task BlinkAsync(int times = 3, CancellationToken cancellationToken = default)
    {
        try
        {
            for (int i = 0; i < times; i++)
            {
                SetLightOverride(PadButtons.Arrows);
                await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                SetLightOverride(PadButtons.None);
                await Task.Delay(150, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (_disposed == 0)
                SetLightOverride(null);
        }
    }

    private void ReadLoop()
    {
        var token = _cts.Token;
        Exception? failure = null;

        while (!token.IsCancellationRequested)
        {
            int count;
            try
            {
                count = _stream.Read(_inputBuffer, 0, _inputBuffer.Length);
            }
            catch (TimeoutException)
            {
                continue;
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                    failure = ex;
                break;
            }

            if (count <= 0)
                continue;

            var buttons = DdrMiniProtocol.ParseInput(_inputBuffer.AsSpan(0, count));
            if (buttons == _buttons)
                continue;

            _buttons = buttons;
            ButtonsChanged?.Invoke(this, buttons);
            UpdateLights();
        }

        if (!token.IsCancellationRequested)
            Disconnected?.Invoke(this, failure);
    }

    private PadButtons DesiredLights() =>
        _lightOverride ?? _lightMode switch
        {
            // LED는 화살표에만 있으므로 아래쪽 버튼은 제외합니다.
            LightMode.Mirror => _buttons & PadButtons.Arrows,
            LightMode.AlwaysOn => PadButtons.Arrows,
            _ => PadButtons.None,
        };

    private void UpdateLights() => WriteLights(DesiredLights(), force: false);

    private void WriteLights(PadButtons lights, bool force)
    {
        if (!SupportsLights)
            return;

        lock (_writeLock)
        {
            if (!force && _lastWrittenLights == lights)
                return;

            try
            {
                DdrMiniProtocol.WriteLightReport(lights, _outputBuffer);
                _stream.Write(_outputBuffer, 0, _outputBuffer.Length);
                _lastWrittenLights = lights;
            }
            catch
            {
                // 쓰기 실패는 읽기 루프에서 분리로 감지됩니다.
                return;
            }
        }

        LightsChanged?.Invoke(this, lights);
    }

    private static string BuildDisplayName(HidDevice device)
    {
        string name;
        try { name = device.GetProductName(); }
        catch { name = "DDR Mini"; }

        try
        {
            var serial = device.GetSerialNumber();
            if (!string.IsNullOrWhiteSpace(serial))
                name += $" ({serial})";
        }
        catch
        {
            // 시리얼 번호가 없는 장치
        }

        return string.IsNullOrWhiteSpace(name) ? "DDR Mini" : name.Trim();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _cts.Cancel();

        // 종료 시 LED를 끕니다 (이미 분리된 경우 무시).
        _lightOverride = null;
        WriteLights(PadButtons.None, force: true);

        if (Thread.CurrentThread != _readThread && _readThread.IsAlive)
            _readThread.Join(ReadTimeoutMs * 4);

        _stream.Dispose();
        _cts.Dispose();
    }
}
