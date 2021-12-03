using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Launcher
{
    public partial class Form1 : Form
    {
        private Socket clientSocket;

        //ManualResetEvent instances signal completion.      
        private static ManualResetEvent connectDone = new ManualResetEvent(false);
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        private static ManualResetEvent receiveDone = new ManualResetEvent(false);

        //The response from the remote device.   
        private static String response = String.Empty;
        public Server server;
        private FormUpdate formUpdate;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                clientSocket.BeginConnect(new IPEndPoint(IPAddress.Loopback, 8888), new AsyncCallback(ConnectCallback), null);
                connectDone.WaitOne();

                //Create the state object.     
                server = new Server();
                server.workSocket = clientSocket;

                formUpdate = new FormUpdate();
                formUpdate.Visible = false;


                //Begin receiving the data from the remote device.     
                clientSocket.BeginReceive(server.receiveBuffer, 0, Server.receiveBufferSize, 0, new AsyncCallback(ReceiveCallback), server);
                //receiveDone.WaitOne();

                //MessageBox.Show(response);
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ConnectCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket.EndConnect(AR);
                connectDone.Set();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void SendCallback(IAsyncResult AR)
        {
            try
            {
                clientSocket.EndSend(AR);
                sendDone.Set();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                String content = String.Empty;
                //Retrieve the state object and the client socket      
                //from the asynchronous state object.     
                Server server = (Server)ar.AsyncState;
                Socket client = server.workSocket;

                //Read data from the remote device.      
                int bytesRead = client.EndReceive(ar);
                if (bytesRead > 0)
                {
                    //If the message length is not set, then read the first 4 bytes that should indicate it
                    if (server.messageLength == 0)
                    {
                        try
                        {
                            server.messageLength = BitConverter.ToUInt32(SubArray(server.receiveBuffer, 0, 4), 0);
                            server.sb.Append(Encoding.ASCII.GetString(server.receiveBuffer, 4, bytesRead - 4));
                            server.messageLength -= Convert.ToUInt32(bytesRead - 4);
                        }
                        catch   // 4asdf
                        {
                            return;
                        }
                    }
                    else
                    {
                        //There might be more data, so store the data received so far.     
                        server.sb.Append(Encoding.ASCII.GetString(server.receiveBuffer, 0, bytesRead));
                        server.messageLength -= Convert.ToUInt32(bytesRead);
                    }

                    //Check for end-of-file tag. If it is not there, read      
                    //more data.     
                    content = server.sb.ToString().Trim();
                    server.sb = new StringBuilder();

                    response = content;

                    if (server.messageLength == 0)
                    {
                        //All the data has been read from the      
                        //client. Display it on the console.     
                        textBox2.Invoke((MethodInvoker)delegate { textBox2.Text += content + "\r\n"; });
                        
                        
                        //receiveDone.Set();
                    }
                    client.BeginReceive(server.receiveBuffer, 0, Server.receiveBufferSize, 0, new AsyncCallback(ReceiveCallback), server);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void Send(string message)
        {
            byte[] buffer = new byte[4 + message.Length];
            BitConverter.GetBytes(message.Length).CopyTo(buffer, 0);
            Encoding.ASCII.GetBytes(message).CopyTo(buffer, 4);
            clientSocket.BeginSend(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(SendCallback), null);
            sendDone.WaitOne();
        }

        public static byte[] SubArray(byte[] data, int index, int length)
        {
            byte[] result = new byte[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                button1_Click(this, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Send("sendgmessage&msg=" + textBox1.Text);
            textBox1.Clear();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }
    }

    public class Server
    {
        public Socket workSocket = null;
        public const int receiveBufferSize = 1024;
        public byte[] receiveBuffer = new byte[receiveBufferSize];

        //Received data string.      
        public StringBuilder sb = new StringBuilder();

        public uint messageLength = 0;
    }
}
