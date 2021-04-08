using NAudio.Wave;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace VoiceChat
{
    public class VoiceTcpClient
    {
        [Reactive]
        public string Name { get; set; } = "Admin";
        public TcpClient Client { get; set; } = new TcpClient();

        private NetworkStream _stream;


        #region NaudioThings
        private WaveIn _input;
        private WaveOut _output;
        private BufferedWaveProvider _buffer;
        #endregion
        public void ConnectToServer(string ip, int port)
        {
            Client.Connect(ip, port);
            _stream = Client.GetStream();
            byte[] name = Encoding.Unicode.GetBytes(Name);
            _stream.Write(name, 0, name.Length);
            _input = new WaveIn();
            _output = new WaveOut();
            EnableVoice();
            Task outputVoice = new Task(() =>
            {
                GetOutputVoice();
            });
            outputVoice.Start();

        }
        private void EnableVoice()
        {
            _input.DeviceNumber = 0;
            _input.WaveFormat = new WaveFormat(44000, 16, 1);
            _input.DataAvailable += InputDataAvailable;

            _buffer = new BufferedWaveProvider(_input.WaveFormat);
            _output.Init(_buffer);

        }

        private void InputDataAvailable(object sender, WaveInEventArgs e)
        {
            _stream.Write(e.Buffer, 0, e.Buffer.Length);
        }
        private void GetOutputVoice()
        {
            byte[] data = new byte[3000];
            do
            {
                int bytes = _stream.Read(data, 0, data.Length);
                _buffer.AddSamples(data, 0, bytes);
            }
            while (_stream.DataAvailable);
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
