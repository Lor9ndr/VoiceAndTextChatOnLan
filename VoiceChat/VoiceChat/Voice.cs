using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace VoiceChat
{
    public class Voice
    {
        private WaveIn _sourceStream = null;
        private byte[] _data;
        private NetworkStream _ns;
        private WaveFileWriter _waveWriter = null;
        private Socket _connector, sc, sock = null;

        private string _path =  "../../../Voice.wav";

        private Thread recThread;
        public string Ip { get; set; }
        public int Port { get; set; }

        public void Recieve(int port)
        {
            Port = port;
            recThread = new Thread(new ThreadStart(VoiceRecieve));
            recThread.Start();

        }

        public void Send(string ip, int port)
        {
            Ip = ip;
            Port = port;
            RecordWav();
        }

        private void RecordWav()
        {
            _sourceStream = new WaveIn();
            int deviceNum = 0;
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                if (WaveIn.GetCapabilities(i).ProductName.Contains("icrophone"))
                {
                    deviceNum = i;
                }
                Console.WriteLine(WaveIn.GetCapabilities(i).ProductName);
            }
            _sourceStream.DeviceNumber = deviceNum;
            _sourceStream.WaveFormat = new WaveFormat(22000, WaveIn.GetCapabilities(deviceNum).Channels);
            _sourceStream.DataAvailable += new EventHandler<WaveInEventArgs>(SourceStream_DataAvailable);

            _waveWriter = new WaveFileWriter(_path, _sourceStream.WaveFormat);

            _sourceStream.StartRecording();

        }

        private void VoiceRecieve()
        {
            sc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ie = new IPEndPoint(0, this.Port);
            sc.Bind(ie);
            sc.Listen(0);

            sock = sc.Accept();

            _ns = new NetworkStream(sock);

            WriteBytes();

            sc.Close();

            while (true)
            {
                VoiceRecieve();
                //sock.BeginReceiveFrom(_data, 0, _data.Length, SocketFlags.Multicast, ref ie, new AsyncCallback());
                SendBytes();
            }

        }

        private void WriteBytes()
        {
            if(_ns != null)
            {
                SoundPlayer sp = new SoundPlayer(_ns);
                sp.Play();
            }
        }

        public void SendBytes()
        {
            _data = File.ReadAllBytes(_path);

            _connector = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ie = new IPEndPoint(IPAddress.Parse(Ip), Port);
            ie.Address = IPAddress.Loopback;
            _connector.Connect(ie);
            _connector.Send(_data, 0, _data.Length, 0);
            _connector.Close();

            RecordWav();

        }

        private void Dispose()
        {
            if (_sourceStream != null)
            {
                _sourceStream.StopRecording();
                _sourceStream.Dispose();
            }
            if (_waveWriter != null)
            {
                _waveWriter.Dispose();
            }
            GC.SuppressFinalize(this);
        }

        private void SourceStream_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (_waveWriter == null) return;
            _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            _waveWriter.Flush();
        }

    }
}
