using System.Diagnostics;

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
        var mutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            mutex.Dispose();
            // 이미 실행 중이면 기존 창을 띄우고 종료합니다.
            if (EventWaitHandle.TryOpenExisting(ShowWindowEventName, out var showEvent))
            {
                using (showEvent)
                    showEvent.Set();
            }
            return;
        }

        bool restart;
        try
        {
            ApplicationConfiguration.Initialize();

            var settings = AppSettings.Load();
            Strings.Initialize(settings.Language);

            bool startMinimized = args.Contains(MinimizedArgument, StringComparer.OrdinalIgnoreCase);
            using var context = new TrayApplicationContext(settings, startMinimized);
            Application.Run(context);
            restart = context.RestartRequested;
        }
        finally
        {
            // 다시 시작할 새 프로세스가 단일 인스턴스 검사를 통과하도록 먼저 놓아 줍니다.
            mutex.ReleaseMutex();
            mutex.Dispose();
        }

        if (restart && Environment.ProcessPath is { } path)
            Process.Start(path);
    }
}
