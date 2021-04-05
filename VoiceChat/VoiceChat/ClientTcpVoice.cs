using NAudio.Wave;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceChat
{
    [DataContract]
    public class ClientUdpVoice
    {
        [Reactive]
        public ObservableCollection<string> Log { get; set; } = new ObservableCollection<string>();
        bool alive = false; // будет ли работать поток для приема
        public UdpClient client;
        [Reactive, DataMember]
        public int LOCALPORT { get; set; }= 1234; // порт для приема сообщений
        [Reactive, DataMember]
        public int REMOTEPORT { get; set; } = 1235; // порт для отправки сообщений
        const int TTL = 20;
        [Reactive, DataMember]
        public string IP {get;set;} = "235.5.5.1"; // хост для групповой рассылки
        IPAddress groupAddress; // адрес для групповой рассылки
        WaveIn input = new WaveIn();
        //поток для речи собеседника
        WaveOut output = new WaveOut();
        //буфферный поток для передачи через сеть
        BufferedWaveProvider bufferStream;

        public ClientUdpVoice() { }
        public ClientUdpVoice(Client chatClient)
        {
            Log = chatClient.Log;
        }

        public void Process()
        {


            //создаем поток для записи нашей речи
            //определяем его формат - частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
            input.WaveFormat = new WaveFormat(44000, 16, 1);
            //добавляем код обработки нашего голоса, поступающего на микрофон
            input.DataAvailable += Voice_Input;
            //создаем поток для прослушивания входящего звука
            //создаем поток для буферного потока и определяем у него такой же формат как и потока с микрофона
            bufferStream = new BufferedWaveProvider(new WaveFormat(44000, 16, 1));
            //привязываем поток входящего звука к буферному потоку
            output.Init(bufferStream);
            //сокет для отправки звука
            groupAddress = IPAddress.Parse(IP);

            client = new UdpClient(LOCALPORT);
            // присоединяемся к групповой рассылке
            client.JoinMulticastGroup(groupAddress, TTL);
            client.EnableBroadcast = true;
            input.StartRecording();
            // запускаем задачу на прием сообщений
            Task receiveTask = new Task(Listening);
            receiveTask.Start();

        }

        private void Voice_Input(object sender, WaveInEventArgs e)
        {

            try
            {
                //посылаем байты, полученные с микрофона на удаленный адрес
                client.Send(e.Buffer,e.Buffer.Length, IP,REMOTEPORT);
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);
            }
        }
        //Прослушивание входящих подключений
        private void Listening()
        {
            output.Play();
            alive = true;
            //бесконечный цикл
            while (alive)
            {
                try
                {
                    //получено данных
                    byte[] data = client.ReceiveAsync().Result.Buffer;
                    //добавляем данные в буфер, откуда output будет воспроизводить звук
                    bufferStream.AddSamples(data, 0, data.Length);
                }
                catch (ObjectDisposedException)
                {
                    if (!alive)
                        return;
                    throw;
                }
                catch (Exception ex)
                {
                    Log.Add(ex.Message);
                }
            }
        }
        private static string LocalIPAddress()
        {
            string localIP = "";
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }
        public void Dispose()
        {
            input.StopRecording();
            output.Stop();
            alive = false;
            if (client != null)
            {
                client.Close();
            }

        }
    }
}