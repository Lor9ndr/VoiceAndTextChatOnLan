using ReactiveUI;
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
using System.Windows.Input;

namespace VoiceChat.UI.ViewModels
{
    [DataContract]
    public class MainWindowViewModel : ViewModelBase,IScreen
    {


        [Reactive, DataMember]
        public string Message { get; set; }


        [Reactive,DataMember]
        public Client Client { get; set; }
        [Reactive, DataMember]
        public ClientUdpVoice VoiceClient { get; set; } 
        public ICommand StartCommand { get; set; }
        public ICommand StopCommand { get; set; }
        public ICommand SendMessageCommand { get; set; }

        [Reactive]
        public bool IsConnected { get; set; }

        public RoutingState Router => throw new NotImplementedException();

        public MainWindowViewModel()
        {
            var canStart = this.
                    WhenAnyValue
                    (x => x.IsConnected, (x) => x != true);
            var canStop = this.
            WhenAnyValue
                (x => x.IsConnected, (x) => x == true);

            Client = new Client();
            VoiceClient = new ClientUdpVoice();

            StartCommand = ReactiveCommand.Create(Connect, canStart);


            StopCommand = ReactiveCommand.Create(Disconnect,canStop);
            SendMessageCommand = ReactiveCommand.Create(SendMessage);

        }
        public void Connect()
        {
            Client.Connect();
            VoiceClient.Process();
            IsConnected = true;
        }


        private void SendMessage()
        {
            try
            {
                Client?.SendMessage(Message);
                Message = string.Empty;
            }
            catch(NullReferenceException e)
            {
                Client.Log.Add(e.Message);
            }
        }
        public void Disconnect()
        {
            Client.Dispose();
            VoiceClient.Dispose();
            IsConnected = false;

        }


    }
}