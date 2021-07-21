using System;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

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
            _pipe = new NamedPipeClientStream("vrapplication/ffb/curl/" + (isRightHand ? "right" : "left"));
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

    class FileChecker : IDisposable
    {
        private string path;
        private FileStream fs;
        private StreamReader sr;

        public FileChecker(string path)
        {
            fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            sr = new StreamReader(fs);
        }

        public String GetNextLine()
        {
            try
            {
                return sr.ReadLine();
            }
            catch (Exception e)
            {
                return "";
            }
        }

        public void Dispose()
        {
            sr.Dispose();
        }
    }

    class ConsoleIn
    {
        public string path { get; set; }
        public bool invertHands { get; set; }
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
            string input = Console.ReadLine();

            ConsoleIn dataIn = JsonSerializer.Deserialize<ConsoleIn>(input);

            using FileChecker checker = new FileChecker(dataIn.path);
            Console.WriteLine("Initalised File Watcher");

            Console.WriteLine("Awaiting server. Try opening SteamVR with driver active if this process is hanging");
            NamedPipesProvider rightNamedPipeProvider = new NamedPipesProvider(true);
            rightNamedPipeProvider.Connect();
            Console.WriteLine("Created Right Pipe");

            NamedPipesProvider leftNamedPipeProvider = new NamedPipesProvider(false);
            leftNamedPipeProvider.Connect();
            Console.WriteLine("Created Left Pipe");

            string line;
            Console.WriteLine("Connected successfully");

            TimerCallback tCallback = (x) =>
            {
                while ((line = checker.GetNextLine()) != null)
                {
                    if (line != null && line.Contains("[OpenGlovesParse]"))
                    {
                        string[] split = line.Split('(', ')');
                        if(split.Length > 0)
                        {
                            short[] ffb = Array.ConvertAll(split[1].Split(','), short.Parse);

                            VRFFBInput ffbInput = new VRFFBInput(ffb[0], ffb[1], ffb[2], ffb[3], ffb[4]);

                            NamedPipesProvider pipe = line.Contains(dataIn.invertHands ? "[Right]" : "[Left]") ? ref rightNamedPipeProvider : ref leftNamedPipeProvider;

                            pipe.Send(ffbInput);

                            Console.WriteLine("Sent force feedback message: " + ffb.ToString());
                        }
                    }
                };
            };

            var checkTimer = new Timer(tCallback);
            checkTimer.Change(0, 10);

            var consoleCancellationToken = new CancellationTokenSource();

            bool isRunning = true;
            ListenToConsole((string input) =>
            {
                switch (input)
                {
                    case "stop":
                        Console.WriteLine("Exiting...");
                        checkTimer.Dispose();
                        consoleCancellationToken.Cancel();
                        isRunning = false;
                        break;
                }

            }, consoleCancellationToken);


            Task t = Task.Run(async () => {
                do
                {
                    await Task.Delay(10);
                } while (isRunning);
            });

            t.Wait();
        }

    }
}
