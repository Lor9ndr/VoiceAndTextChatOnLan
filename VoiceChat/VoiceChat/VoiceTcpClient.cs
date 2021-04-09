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

        public bool IsAdmin { get; set; } = false;

        private NetworkStream _stream;

        Task _outputVoiceTask;


        #region NaudioThings
        private WaveIn _input;
        private WaveOut _output;
        private BufferedWaveProvider _buffer;
        #endregion
        public void ConnectToServer(string ip, int port)
        {
            Client.Connect(ip, port);
            _stream = Client.GetStream();
            _input = new WaveIn();
            _output = new WaveOut();
            byte[] name = Encoding.Unicode.GetBytes(Name);
            _stream.Write(name, 0, name.Length);

            EnableVoice();
            _outputVoiceTask = new Task(() =>
            {
                GetOutputVoice();
            });
            _outputVoiceTask.Start();

        }
        private void EnableVoice()
        {
            _input.DeviceNumber = 0;
            _input.WaveFormat = new WaveFormat(44000, 16, 1);
            _input.DataAvailable += InputDataAvailable;
            _input.StartRecording();

            _buffer = new BufferedWaveProvider(_input.WaveFormat);
            _output.Init(_buffer);

        }

        private void InputDataAvailable(object sender, WaveInEventArgs e)
        {
            byte[] name = Encoding.Unicode.GetBytes(Name);
            _stream.Write(name, 0, name.Length);
            _stream.Write(e.Buffer, 0, e.Buffer.Length);
        }
        private void GetOutputVoice()
        {
            _output.Play();
            while (true)
            {
                byte[] buf = new byte[1];
                _stream.Read(buf);
                _buffer.AddSamples(buf, 0, buf.Length);
                buf = new byte[1];
            }
        }
        public void Disconnect()
        {
            _output.Dispose();
            _input.Dispose();
            Client.Dispose();
            _outputVoiceTask.Dispose();
        }
        public override string ToString()
        {
            return Name;
        }
    }
}
