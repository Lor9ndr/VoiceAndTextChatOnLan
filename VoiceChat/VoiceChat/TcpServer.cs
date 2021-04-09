using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VoiceChat
{
    public class TcpServer:IDisposable
    {
        [Reactive]
        public Dictionary<VoiceTcpClient,NetworkStream> Clients { get; set; } = new Dictionary<VoiceTcpClient,NetworkStream>();
        private IPAddress _localaddr = IPAddress.Parse("235.5.5.1");
        private int _port = 8888;
        private TcpListener _server;
        public void Start()
        {
            _server = new TcpListener(_localaddr, _port);
            _server.Start();
            Task accept = new Task(() =>
            {
                while (true)
                {
                    TcpClient client = _server.AcceptTcpClient();
                    NetworkStream stream = client.GetStream();
                    byte[] data = new byte[64]; // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (stream.DataAvailable);
                    VoiceTcpClient voiceClient = new VoiceTcpClient
                    {
                        Name = builder.ToString(),
                        Client = client
                    };
                    Clients.Add(voiceClient, stream);
                    data = new byte[1]; // буфер для получаемых данных и отправляемых данных
                    while (true)
                    {
                        stream.Read(data, 0, data.Length);
                        foreach (var item in Clients)
                        {
                            if (item.Key.Name != voiceClient.Name)
                                item.Value.Write(data, 0, data.Length);
                        }
                    }

                }

            });
            accept.Start();
        }
        public void Dispose()
        {
            foreach (var client in Clients.Keys)
            {
                client.Disconnect();
            }
            _server.Stop();


        }
    }
}
