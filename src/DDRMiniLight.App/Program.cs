namespace DDRMiniLight.App;

internal static class Program
{
    private const string MutexName = @"Local\DDRMiniLight.SingleInstance";
    internal const string ShowWindowEventName = @"Local\DDRMiniLight.ShowWindow";

    /// <summary>윈도우 시작 시 자동 실행할 때 붙이는 인자. 창 없이 트레이로 시작합니다.</summary>
    internal const string MinimizedArgument = "--minimized";

    [STAThread]
    private static void Main(string[] args)
    {
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            // 이미 실행 중이면 기존 창을 띄우고 종료합니다.
            if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out var showEvent))
            {
                using (showEvent)
                    showEvent.Set();
            }
            return;
        }

        ApplicationConfiguration.Initialize();

        bool startMinimized = args.Contains(MinimizedArgument, StringComparer.OrdinalIgnoreCase);
        using var context = new TrayApplicationContext(startMinimized);
        Application.Run(context);
    }
}
