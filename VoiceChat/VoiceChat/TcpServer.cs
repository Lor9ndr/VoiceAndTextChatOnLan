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
    public class TcpServer
    {
        [Reactive]
        public ObservableCollection<VoiceTcpClient> Clients { get; set; } = new ObservableCollection<VoiceTcpClient>();
        private IPAddress _localaddr = IPAddress.Parse("127.0.0.1");
        private int _port = 8888;
        private TcpListener _server;
        private NetworkStream _stream;
        public void Start()
        {
            _server = new TcpListener(_localaddr, _port);
            _server.Start();
            Task accept = new Task(() =>
            {
                while (true)
                {
                    TcpClient client = _server.AcceptTcpClient();
                    _stream = client.GetStream();
                    byte[] data = new byte[256]; // буфер для получаемых данных
                    StringBuilder builder = new StringBuilder();
                    int bytes = 0;
                    do
                    {
                        bytes = _stream.Read(data, 0, data.Length);
                        builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                    }
                    while (_stream.DataAvailable);
                    VoiceTcpClient voiceClient = new VoiceTcpClient
                    {
                        Name = builder.ToString(),
                        Client = client
                    };
                    Clients.Add(voiceClient);
                    data = new byte[3000]; // буфер для получаемых данных
                    do
                    {
                        bytes = _stream.Read(data, 0, data.Length);
                        _stream.Write(data, 0, data.Length);
                    }
                    while (_stream.DataAvailable);
                }

            });
            accept.Start();
        }
    }
}
