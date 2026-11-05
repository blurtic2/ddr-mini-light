namespace DDRMiniLight.Core;

/// <summary>장치 관리자가 알리는 사건의 종류. 문장은 UI 쪽에서 언어에 맞게 만듭니다.</summary>
public enum DeviceLogKind
{
    /// <summary>장치 목록을 가져오지 못함. <see cref="DeviceLogEventArgs.Detail"/>에 원인.</summary>
    EnumerationFailed,

    /// <summary>장치를 열지 못함. <see cref="DeviceLogEventArgs.Detail"/>이 null이면 다른 프로그램이 사용 중일 가능성.</summary>
    OpenFailed,

    /// <summary>장치가 연결됨.</summary>
    Connected,

    /// <summary>장치가 분리됨.</summary>
    Disconnected,

    /// <summary>통신 오류로 연결이 끊김. <see cref="DeviceLogEventArgs.Detail"/>에 원인.</summary>
    ConnectionLost,
}

public sealed class DeviceLogEventArgs(DeviceLogKind kind, DdrMiniDevice? device = null, string? detail = null) : EventArgs
{
    public DeviceLogKind Kind { get; } = kind;

    /// <summary>관련 장치 (목록 조회 실패, 열기 실패일 때는 null).</summary>
    public DdrMiniDevice? Device { get; } = device;

    /// <summary>OS나 예외에서 온 추가 정보.</summary>
    public string? Detail { get; } = detail;
}
