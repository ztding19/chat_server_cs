using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace chat_server_cs
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource _cts = new CancellationTokenSource();
        Socket serverSocket;
        IPEndPoint endpoint = new IPEndPoint(IPAddress.Loopback, 6100);
        List<Socket> clients = new List<Socket>();
        List<String> clientNames = new List<String>();
        bool first = true;
        bool flag = false;
        public MainWindow()
        {
            InitializeComponent();
            chat.Text = "-----Start-----\n";
            txtMembers.Text = "Online Members:\n";
        }


        private void AcceptClients()
        {
            //while (!_cts.Token.IsCancellationRequested)
            try
            {
                while (flag)
                {
                    Socket clientSocket = serverSocket.Accept(); // This will block until a client connects
                    clients.Add(clientSocket);
                    
                    HandleClientSocket(clientSocket);
                }
            } catch (SocketException ex)
            {
                MessageBox.Show("close server");
                foreach (Socket clientSocket in clients)
                {
                    MessageBox.Show("Close : " + clientSocket.ToString());
                    clientSocket.Close();
                }
                clients.Clear();
                clientNames.Clear();
                RefreshMemberList();
                // Clean up resources


            }
        }

        private void HandleClientSocket(Socket clientSocket)
        {
            string clientIp = "";
            Task.Run(() =>
            {
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    while (flag)
                    {
                        byte[] buffer = new byte[4096]; // adjust buffer size as needed
                        int bytesRead = clientSocket.Receive(buffer);

                        // Check if the client has disconnected
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string showedMessage = "";
                        chat.Dispatcher.Invoke(() =>
                        {
                            mMessage msg = new mMessage(receivedMessage);
                            if (msg.getDictMsg()["status"] == "connect") {
                                clientNames.Add(msg.getDictMsg()["name"]);
                                clientIp = msg.getDictMsg()["message"];
                                showedMessage = msg.getDictMsg()["name"] + " joined!\n";
                                chat.Text += showedMessage;
                                RefreshMemberList();
                            }else if (msg.getDictMsg()["status"] == "disconnect")
                            {
                                clientNames.Remove(msg.getDictMsg()["name"]);
                                clients.Remove(clientSocket);
                                showedMessage = msg.getDictMsg()["name"] + " left!\n";
                                chat.Text += showedMessage;
                                RefreshMemberList();
                            }
                            else
                            {
                                showedMessage = msg.getDictMsg()["name"] + " : " + msg.getDictMsg()["message"] + "\n";
                                chat.Text += msg.getDictMsg()["name"] + "(" + clientIp + ") : " + msg.getDictMsg()["message"] + "\n";
                            }
                        });
                        byte[] messageBytes = Encoding.UTF8.GetBytes(showedMessage);
                        foreach(Socket clientSocket in clients)
                        {
                            clientSocket.Send(messageBytes);
                        }
                        

                        // TODO: Further processing of received message, if necessary
                    }
                }
                catch (SocketException ex)
                {
                    // Handle any socket-specific errors, like if the client forcibly closes the connection

                    Console.WriteLine($"Socket Exception: {ex.Message}");
                }
                
            });
        }

        class mMessage
        {
            Dictionary<String, String> dictMsg = new Dictionary<String, String>();
            public mMessage(Dictionary<String,String> dict)
            {
                this.dictMsg = dict;
            }

            public mMessage(String str) 
            {
                this.dictMsg = JsonConvert.DeserializeObject<Dictionary<String, String>>(str);
            }

            public String getJsonString()
            {
                return JsonConvert.SerializeObject(this.dictMsg);
            }

            public Dictionary<String, String> getDictMsg() {  return this.dictMsg; }
        }

        void RefreshMemberList()
        {
            txtMembers.Dispatcher.Invoke((() =>
            {
                txtMembers.Text = "Online Members :\n";
                foreach(string name in clientNames)
                {
                    txtMembers.Text += name + "\n";
                }
            }));
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = false;
            btnClose.IsEnabled = true;
            txtOnline.Text = "Online";
            flag = true;
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (!serverSocket.IsBound)
            {
                serverSocket.Bind(endpoint);
                serverSocket.Listen(10); // Setting a backlog of 10 connections
            }
            
            _cts = new CancellationTokenSource();
            Thread acceptThread = new Thread(() => AcceptClients());
            acceptThread.Start();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            btnStart.IsEnabled = true;
            btnClose.IsEnabled = false;
            _cts.Cancel();
            flag = false;
            serverSocket.Close();
            

            txtOnline.Text = "Offline";
            btnStart.IsEnabled=true;
            btnClose.IsEnabled=false;
        }
    }
}
