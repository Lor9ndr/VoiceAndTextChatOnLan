using NAudio.Wave;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    public class ClientUdpVoice:IDisposable
    {
        #region Public Properties
        public UdpClient client;
        [Reactive, DataMember]
        public int LOCALPORT { get; set; } = 1234; // порт для приема
        [Reactive, DataMember]
        public int REMOTEPORT { get; set; } = 1235; // порт для отправки
        [Reactive, DataMember]
        public string IP { get; set; } = "235.5.5.1"; // хост для групповой рассылки
        [Reactive]
        public float Threshold { get; set; } = 0.02f;
        #endregion

        #region Private Properties
        private bool alive = false; // будет ли работать поток для приема
      
        private const int TTL = 20;
      
        private IPAddress _groupAddress; // адрес для групповой рассылки
        private WaveIn _input = new WaveIn();
        //поток для речи собеседника
        private WaveOut _output = new WaveOut();
        //буфферный поток для передачи через сеть
        private BufferedWaveProvider _buffer;
        #endregion

        /// <summary>
        /// Иницилизация обьектов и их определение
        /// Запуск потока с прослушиванием порта
        /// </summary>
        public void Process()
        {
            //создаем поток для записи нашей речи
            //определяем его формат - частота дискретизации 8000 Гц, ширина сэмпла - 16 бит, 1 канал - моно
            _input.WaveFormat = new WaveFormat(44000, 16, 1);
            //добавляем код обработки нашего голоса, поступающего на микрофон
            _input.DataAvailable += Voice_Input;
            //создаем поток для прослушивания входящего звука
            //создаем поток для буферного потока и определяем у него такой же формат как и потока с микрофона
            _buffer = new BufferedWaveProvider(_input.WaveFormat);
            //привязываем поток входящего звука к буферному потоку
            _output.Init(_buffer);
            //сокет для отправки звука
            _groupAddress = IPAddress.Parse(IP);

            client = new UdpClient(LOCALPORT);
            // присоединяемся к групповой рассылке
            client.JoinMulticastGroup(_groupAddress, TTL);
            client.EnableBroadcast = true;
            _input.StartRecording();
            // запускаем задачу на прием сообщений
            Task receiveTask = new Task(Listening);
            receiveTask.Start();

        }

        /// <summary>
        /// Обработка голоса и отправка 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Voice_Input(object sender, WaveInEventArgs e)
        {
            try
            {
                if (SilenceDetection(e)) // Речь определена на отрезке
                    //посылаем байты, полученные с микрофона на удаленный адрес
                    client.Send(e.Buffer, e.Buffer.Length, IP, REMOTEPORT);
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Прослушивание входящих подключений
        /// </summary>
        private void Listening()
        {
            _output.Play();
            alive = true;
            while (alive)
            {
                try
                {
                    //получено данных
                    byte[] data = client.ReceiveAsync().Result.Buffer;
                    //добавляем данные в буфер, откуда output будет воспроизводить звук
                    _buffer.AddSamples(data, 0, data.Length);
                }
                catch (ObjectDisposedException)
                {
                    if (!alive)
                        return;
                    throw;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }
        public void Dispose()
        {
            _input.StopRecording();
            _output.Stop();
            alive = false;
            if (client != null)
                client.Close();
        }
        /// <summary>
        /// Определение тишины в микрофоне
        /// </summary>
        /// <param name="e">Собственно сам звук с микрофона</param>
        /// <returns></returns>
        private bool SilenceDetection(WaveInEventArgs e)
        {
            bool tr = false;
            double sum = 0;
            int count = e.BytesRecorded / 2;

            for (int i = 0; i < e.BytesRecorded; i+=2)
            {
                double tmp = (short)((e.Buffer[i+1] << 8) | e.Buffer[i]);
                tmp /= 32760.0f;
                sum += tmp * tmp ;
                if (tmp>Threshold)
                    tr = true; 
            }
            sum /= count;
            if (tr || sum > Threshold )
                return true;
            else return false;
        }
        
    }
}