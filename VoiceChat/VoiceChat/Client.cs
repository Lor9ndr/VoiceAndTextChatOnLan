using NAudio.Wave;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceChat
{
    [DataContract]
    public class Client : IDisposable
    {
        [Reactive,DataMember]
        public ObservableCollection<string> Log { get; set; } = new ObservableCollection<string>();
        bool alive = false; // будет ли работать поток для приема
        public UdpClient client;
        const int LOCALPORT = 8001; // порт для приема сообщений
        const int REMOTEPORT = 8001; // порт для отправки сообщений
        const int TTL = 20;
        const string HOST = "235.5.5.1"; // хост для групповой рассылки
        IPAddress groupAddress; // адрес для групповой рассылки
        private bool _disposed = false;

        [Reactive,DataMember]
        public string UserName { get; set; }
        [DataMember]
        private string _currentMessage { get; set; }
        public Client()
        {
            if (UserName == null)
                UserName = "UserName";
        }
        public void Connect()
        {
            try
            {
                _disposed = false;
                groupAddress = IPAddress.Parse(HOST);

                client = new UdpClient(LOCALPORT);
                // присоединяемся к групповой рассылке
                client.JoinMulticastGroup(groupAddress, TTL);
                client.EnableBroadcast = true;

                // запускаем задачу на прием сообщений
                Task receiveTask = new Task(ReceiveMessages);
                receiveTask.Start();

                // отправляем первое сообщение о входе нового пользователя
                _currentMessage = " вошел в чат";
                byte[] data = Encoding.Unicode.GetBytes(_currentMessage);
                client.Send(data, data.Length, HOST, REMOTEPORT);
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);
            }
        }
        public void SendMessage(string message)
        {
            _currentMessage = message;
            if (_currentMessage != null && client != null && !_disposed)
            {
                byte[] data = Encoding.Unicode.GetBytes(_currentMessage);
                client.Send(data, data.Length, HOST, REMOTEPORT);
            }
        }
        private void ReceiveMessages()
        {
            alive = true;
            try
            {
                while (alive)
                {
                    //IPEndPoint remoteIp = null;
                    byte[] data = client.ReceiveAsync().Result.Buffer;//(ref remoteIp);
                    string message = Encoding.Unicode.GetString(data);

                    // добавляем полученное сообщение в текстовое поле
                   
                        string time = DateTime.Now.ToShortTimeString();
                        Log.Add(time +':'+ UserName + ':' + message + "\r\n");
                }
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
       
        public void Dispose()
        {
            _currentMessage = " покидает чат";
            SendMessage(_currentMessage);
            if (client != null)
            {
                client.DropMulticastGroup(groupAddress);
                client.Close();
            }

            alive = false;
            _disposed = true;


        }
    }
}
