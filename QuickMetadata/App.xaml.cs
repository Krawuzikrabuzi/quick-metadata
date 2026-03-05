using System.IO;
using System.IO.Pipes;
using System.Windows;

namespace QuickMetadata
{
    public partial class App : Application
    {
        private const string PipeName = "QuickMetadataPipe";
        private static Mutex? _mutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _mutex = new Mutex(true, "QuickMetadataMutex", out bool isFirst);

            if (isFirst)
            {
                var window = new MainWindow(e.Args);
                _ = ListenForFiles(window);
                window.Show();
            }
            else
            {
                var arg = e.Args.Length > 0 ? e.Args[0] : null;
                if (arg != null) SendToRunningInstance(arg);
                Shutdown();
            }
        }

        private static void SendToRunningInstance(string message)
        {
            for (int attempt = 0; attempt < 10; attempt++)
            {
                try
                {
                    using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                    pipe.Connect(300);
                    using var writer = new StreamWriter(pipe);
                    writer.WriteLine(message);
                    return;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
        }

        private static async Task ListenForFiles(MainWindow window)
        {
            while (true)
            {
                using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.In);
                await pipe.WaitForConnectionAsync();
                using var reader = new StreamReader(pipe);
                var message = await reader.ReadLineAsync();
                if (message == null) continue;
                window.Dispatcher.Invoke(() => window.AddFiles(message));
            }
        }
    }
}