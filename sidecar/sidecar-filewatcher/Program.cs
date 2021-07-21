using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace sidecar_filewatcher
{
    [StructLayout(LayoutKind.Sequential)]
    public struct VRFFBInput
    {
        //Curl goes between 0-1000
        public VRFFBInput(short thumbCurl, short indexCurl, short middleCurl, short ringCurl, short pinkyCurl)
        {
            this.thumbCurl = thumbCurl;
            this.indexCurl = indexCurl;
            this.middleCurl = middleCurl;
            this.ringCurl = ringCurl;
            this.pinkyCurl = pinkyCurl;
        }
        public short thumbCurl;
        public short indexCurl;
        public short middleCurl;
        public short ringCurl;
        public short pinkyCurl;
    };
    class NamedPipesProvider
    {
        private NamedPipeClientStream _pipe;
        public NamedPipesProvider(bool isRightHand)
        {
            _pipe = new NamedPipeClientStream("vrapplication/ffb/curl/" + isRightHand);
        }

        public void Connect()
        {
            _pipe.Connect();
        }

        public void Disconnect()
        {
            if (_pipe.IsConnected)
            {
                _pipe.Dispose();
            }
        }

        public void Send(VRFFBInput input)
        {
            if (_pipe.IsConnected)
            {
                int size = Marshal.SizeOf(input);
                byte[] arr = new byte[size];

                IntPtr ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(input, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
                Marshal.FreeHGlobal(ptr);

                _pipe.Write(arr, 0, size);
            }
        }
    }

    class Program
    {
        public static void ListenToConsole(Action<string> onMessageFromParent, CancellationTokenSource cancellationToken)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var text = await Console.In.ReadLineAsync();
                    onMessageFromParent(text);
                }
            }, cancellationToken.Token);
        }
        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }

        static void Main(string[] args)
        {
            String path = @"D:\Steam\steamapps\common\Half-Life Alyx\game\hlvr\console.log";
            var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using (var sr = new StreamReader(fs))
            {
                string line;
                // Read and display lines from the file until the end of
                // the file is reached.
                while ((line = sr.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                }

                using var watcher = new FileSystemWatcher(@"D:\Steam\steamapps\common\Half-Life Alyx\game\hlvr");

                watcher.NotifyFilter = NotifyFilters.Attributes
                                     | NotifyFilters.CreationTime
                                     | NotifyFilters.DirectoryName
                                     | NotifyFilters.FileName
                                     | NotifyFilters.LastAccess
                                     | NotifyFilters.LastWrite
                                     | NotifyFilters.Security
                                     | NotifyFilters.Size;

                watcher.Filter = "*.log";

                watcher.Error += OnError;
                watcher.Changed += (object sender, FileSystemEventArgs e) =>
                {
                    Console.WriteLine($"Event: {e.FullPath}");
                    if (e.ChangeType != WatcherChangeTypes.Changed) return;

                    while ((line = sr.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }
                };

                watcher.EnableRaisingEvents = true;

                var consoleCancellationToken = new CancellationTokenSource();
                ListenToConsole((string input) =>
                {
                    switch (input)
                    {
                        case "stop":
                            Console.WriteLine("Received Stop");
                            consoleCancellationToken.Cancel();
                            break;
                    }

                }, consoleCancellationToken);

                Console.ReadKey(false);

            }
        }
        

    }
}
