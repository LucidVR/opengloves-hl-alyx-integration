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

        static void Main(string[] args)
        {

            string path = Console.In.ReadLine();
            using var watcher = new FileSystemWatcher(@path);

            watcher.NotifyFilter = NotifyFilters.LastWrite
                                   | NotifyFilters.LastAccess;

            watcher.Changed += (object sender, FileSystemEventArgs e) =>
            {
                if (e.ChangeType != WatcherChangeTypes.Changed) return;

                Console.WriteLine($"Changed: {e.FullPath}");
            };

            var consoleCancellationToken = new CancellationTokenSource();
            ListenToConsole((string input) =>
            {
                switch (input)
                {
                    case "stop":
                        consoleCancellationToken.Cancel();
                        break;
                }

            }, consoleCancellationToken);
        }


    }
}
