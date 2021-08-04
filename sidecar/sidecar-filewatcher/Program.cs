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

    class NamedPipeProvider : IDisposable
    {
        private NamedPipeClientStream _pipe;
        public NamedPipeProvider(bool isRightHand)
        {
            _pipe = new NamedPipeClientStream("vrapplication/ffb/curl/" + (isRightHand ? "right" : "left"));
        }

        public void Connect()
        {
            _pipe.Connect();
        }

        public void Dispose()
        {
            Disconnect();
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
        private string _path;
        private FileStream _fs;
        private StreamReader _sr;

        public FileChecker(string path)
        {
            _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            _sr = new StreamReader(_fs);
        }

        public String GetNextLine()
        {
            try
            {
                return _sr.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine("Err Reading: " + e.Message);
                return "";
            }
        }

        public void Dispose()
        {
            _fs.Dispose();
            _sr.Dispose();
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

        public static void ListenAndSend(string path, ref NamedPipeProvider pipeProvider, bool isRightHand, CancellationToken ct)
        {
            using FileChecker fileChecker = new FileChecker(path);

            Console.WriteLine("Awaiting connection for a pipe...");
            pipeProvider.Connect();
            Console.WriteLine("Pipe connected successfully");
            Console.WriteLine((isRightHand ? "Right" : "Left") + " (in-game) ready and listening for input");
            string line;

            while (!ct.IsCancellationRequested)
            {
                while ((line = fileChecker.GetNextLine()) != null)
                {
                    if (line != null && line.Contains("[OpenGlovesParse]"))
                    {
                        if ((isRightHand && line.Contains("{Right}")) || (!isRightHand && line.Contains("{Left}")))
                        {
                            string[] split = line.Split('(', ')');
                            if (split.Length > 0)
                            {
                                short[] ffb = Array.ConvertAll(split[1].Split(','), short.Parse);

                                VRFFBInput ffbInput = new VRFFBInput(ffb[0], ffb[1], ffb[2], ffb[3], ffb[4]);

                                pipeProvider.Send(ffbInput);

                                Console.WriteLine("Sent force feedback message");
                            }
                        }

                    }
                };
                Thread.Sleep(10);
            }
        }
        static void Main(string[] args)
        {
            string input = Console.ReadLine();

            ConsoleIn dataIn = JsonSerializer.Deserialize<ConsoleIn>(input);

            Console.WriteLine("Data in: Path: " + dataIn.path + ", Inverted hands: " + (dataIn.invertHands ? "yes" : "no"));

            Console.WriteLine("Pipes initialised");
            CancellationTokenSource right_cts = new CancellationTokenSource();
            CancellationTokenSource left_cts = new CancellationTokenSource();

            NamedPipeProvider right_pipeProvider = new NamedPipeProvider(true);
            NamedPipeProvider left_pipeProvider = new NamedPipeProvider(false);

            Thread listenAndSendThread_right = new Thread(new ThreadStart(() => ListenAndSend(dataIn.path, ref right_pipeProvider, dataIn.invertHands, right_cts.Token)));
            Thread listenAndSendThread_left = new Thread(new ThreadStart(() => ListenAndSend(dataIn.path, ref left_pipeProvider , !dataIn.invertHands, left_cts.Token)));

            Console.WriteLine("Awaiting connection to pipes... Try opening SteamVR with driver active if this process is hanging");
            listenAndSendThread_right.Start();
            listenAndSendThread_left.Start();


            var consoleCancellationToken = new CancellationTokenSource();
            ListenToConsole((string input) =>
            {
                switch (input)
                {
                    case "stop":
                        Console.WriteLine("Exiting...");
                        right_cts.Cancel();
                        left_cts.Cancel();
                        consoleCancellationToken.Cancel();
                        break;
                }

            }, consoleCancellationToken);

            listenAndSendThread_left.Join();
            listenAndSendThread_right.Join();

            Console.WriteLine("Listener exiting...");
        }

    }
}
