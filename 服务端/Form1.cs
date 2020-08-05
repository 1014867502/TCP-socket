using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;  // IP，IPAddress, IPEndPoint，端口等；
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace _11111
{
    public partial class frm_server : Form
    {
        public frm_server()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }


        private int maxScoket = 2000;
        private Socket serviceSocket;
        //负责通信的socket
        Socket socketSend;
        //负责监听的socket
        Socket socketWatch = null;
        // 负责监听客户端连接请求的 线程；
        Thread threadWatch = null;
        //接受客户端发送消息的线程
        Thread threadReceive;
        //判断是否主动断开
        Boolean isClose = false;
        TcpListener listener=null;
        public static int x = 0;
        //将远程连接的客户端的IP地址和Socket存入集合中
        Dictionary<string, Socket> dict = new Dictionary<string, Socket>();
        Dictionary<string, Thread> dictThread = new Dictionary<string, Thread>();
        private static ManualResetEvent acceptDone = new ManualResetEvent(false);
        private static ManualResetEvent recevieDone = new ManualResetEvent(false);

        private delegate void append(String msg);

        private void btnBeginListen_Click(object sender, EventArgs e)
        {


            // 创建负责监听的套接字，注意其中的参数；

            serviceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 获得文本框中的IP对象；
            IPAddress address = IPAddress.Parse(txtIp.Text.Trim());
            // 创建包含ip和端口号的网络节点对象；
            IPEndPoint endPoint = new IPEndPoint(address, int.Parse(txtPort.Text.Trim()));
            //设置

            //启用keep-alive
            serviceSocket.IOControl(IOControlCode.KeepAliveValues, GetKeepAliveData(), null);
            serviceSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);


            try
            {
                // 将负责监听的套接字绑定到唯一的ip和端口上；
                serviceSocket.Bind(endPoint);
            }
            catch (SocketException se)
            {
                MessageBox.Show("异常：" + se.Message);
                return;
            }
            // 设置监听队列的长度；
            serviceSocket.Listen(1000);
            // 创建负责监听的线程；
            threadWatch = new Thread(WatchConnecting);         
            threadWatch.Start();
            ShowMsg("服务器启动监听成功！");
            //}
        }

        /// <summary>
        /// 监听客户端请求的方法；
        /// </summary>
        void WatchConnecting()
        {
            this.Invoke(new append(ShowMsg), "等待连接...");
            try
            {
                
                serviceSocket.BeginAccept(new AsyncCallback(acceptCallback), serviceSocket); //异步操作接入的连接请求
                acceptDone.WaitOne(); //阻止当前线程，等待连接回调函数的执行
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
      
        void ShowMsg(string str)
        {
            txtMsg.AppendText(str + "\r\n");
        }

        // 发送消息
        private void btnSend_Click(object sender, EventArgs e)
        {
            string strMsg = "服务器" + "\r\n" + "   -->" + txtMsgSend.Text.Trim() + "\r\n";
            byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(strMsg); // 将要发送的字符串转换成Utf-8字节数组；
            byte[] arrSendMsg = new byte[arrMsg.Length + 1];
            arrSendMsg[0] = 0; // 表示发送的是消息数据
            Buffer.BlockCopy(arrMsg, 0, arrSendMsg, 1, arrMsg.Length);
            string strKey = "";
            strKey = lbOnline.Text.Trim();
            if (string.IsNullOrEmpty(strKey))   // 判断是不是选择了发送的对象；
            {
                MessageBox.Show("请选择你要发送的好友！！！");
            }
            else
            {
                dict[strKey].Send(arrSendMsg);// 解决了 sokConnection是局部变量，不能再本函数中引用的问题；
                ShowMsg(strMsg);
                txtMsgSend.Clear();
            }
        }

        /// <summary>
        /// 群发消息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">消息</param>
        private void btnSendToAll_Click(object sender, EventArgs e)
        {
            string strMsg = "服务器" + "\r\n" + "   -->" + txtMsgSend.Text.Trim() + "\r\n";
            byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(strMsg); // 将要发送的字符串转换成Utf-8字节数组；
            foreach (Socket s in dict.Values)
            {
                s.Send(arrMsg);
            }
            ShowMsg(strMsg);
            txtMsgSend.Clear();
            ShowMsg(" 群发完毕～～～");
        }

        // 选择要发送的文件
        private void btnSelectFile_Click_1(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = "D:\\";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSelectFile.Text = ofd.FileName;
            }
        }

        // 文件的发送
        private void btnSendFile_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSelectFile.Text))
            {
                MessageBox.Show("请选择你要发送的文件！！！");
            }
            else
            {
                // 用文件流打开用户要发送的文件；
                using (FileStream fs = new FileStream(txtSelectFile.Text, FileMode.Open))
                {
                    string fileName = System.IO.Path.GetFileName(txtSelectFile.Text);
                    string fileExtension = System.IO.Path.GetExtension(txtSelectFile.Text);
                    string strMsg = "我给你发送的文件为： " + fileName + fileExtension + "\r\n";
                    byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(strMsg); // 将要发送的字符串转换成Utf-8字节数组；
                    byte[] arrSendMsg = new byte[arrMsg.Length + 1];
                    arrSendMsg[0] = 0; // 表示发送的是消息数据
                    Buffer.BlockCopy(arrMsg, 0, arrSendMsg, 1, arrMsg.Length);
                    bool fff = true;
                    string strKey = "";
                    strKey = lbOnline.Text.Trim();
                    if (string.IsNullOrEmpty(strKey))   // 判断是不是选择了发送的对象；
                    {
                        MessageBox.Show("请选择你要发送的好友！！！");
                    }
                    else
                    {
                        dict[strKey].Send(arrSendMsg);// 解决了 sokConnection是局部变量，不能再本函数中引用的问题；
                        byte[] arrFile = new byte[1024 * 1024 * 2];
                        int length = fs.Read(arrFile, 0, arrFile.Length);  // 将文件中的数据读到arrFile数组中；
                        byte[] arrFileSend = new byte[length + 1];
                        arrFileSend[0] = 1; // 用来表示发送的是文件数据；
                        Buffer.BlockCopy(arrFile, 0, arrFileSend, 1, length);
                        // 还有一个 CopyTo的方法，但是在这里不适合； 当然还可以用for循环自己转化；
                        //  sockClient.Send(arrFileSend);// 发送数据到服务端；
                        dict[strKey].Send(arrFileSend);// 解决了 sokConnection是局部变量，不能再本函数中引用的问题；
                        txtSelectFile.Clear();
                    }
                }
            }
            txtSelectFile.Clear();
        }

        private void frm_server_Load(object sender, EventArgs e)
        {

        }

        //设置IOControl设置的数据
        private byte[] GetKeepAliveData()
        {
            uint dummy = 0;
            byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)3000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));//keep-alive间隔
            BitConverter.GetBytes((uint)500).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);//尝试间隔
            return inOptionValues;
        }

        private void btnPause_Click(object sender, EventArgs e)
        {

            try
            {
                isClose = true;
                foreach (Socket socket in dict.Values)
                {
                    
                    ShowMsg(socket.RemoteEndPoint.ToString()+"已断开");
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
                foreach (Thread thread in dictThread.Values)
                {
                    thread.Interrupt();
                    thread.Abort();
                }
               lbOnline.Items.Clear();
               dict.Clear();
               dictThread.Clear();            
            }
            catch (Exception ex) { ShowMsg(ex.Message); }

        }

        //根据键值获取值
        public Thread GetValueByKey(Dictionary<string, Thread> dic, string key)
        {
            Thread result = null;
            foreach (KeyValuePair<string, Thread> kvp in dic)
            {
                if (kvp.Key.Equals(key))
                {
                    result = kvp.Value;
                    break;
                }
            }
            return result;
        }

        public Socket GetValueByKey(Dictionary<string, Socket> dic, string key)
        {
            Socket result = null;
            foreach (KeyValuePair<string, Socket> kvp in dic)
            {
                if (kvp.Key.Equals(key))
                {
                    result = kvp.Value;
                    break;
                }
            }
            return result;
        }

        //判断是否连接
        public bool isClientConnect(Socket client)
        {
            IPGlobalProperties iproperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = iproperties.GetActiveTcpConnections();
            try
            {
                foreach (TcpConnectionInformation c in tcpConnections)
                {
                    TcpState stateOfConnection = c.State;
                    if (c.LocalEndPoint.Equals(client.LocalEndPoint) && c.RemoteEndPoint.Equals(client.RemoteEndPoint))
                    {
                        if (stateOfConnection == TcpState.Established)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                
            }
            catch { }
            return false;
        }

        private void acceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket serviceSocket = (Socket)ar.AsyncState;
                acceptDone.Set();//结束当前进程的停止状态，继续接收下一链接请求
                Socket socket = serviceSocket.EndAccept(ar);//异步接受传入的连接请求
                ShowMsg("连接成功");
                StateObject state = new StateObject();
                state.socket = socket;
                socket.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), state);
                //阻止当前异步接收线程，等待接收回调函数返回
                recevieDone.WaitOne();
                if ((maxScoket--) > 0)
                {
                    serviceSocket.BeginAccept(new AsyncCallback(acceptCallback), serviceSocket); //等待接收下一个连接请求
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        //接收数据后的回调
        private void receiveCallback(IAsyncResult ar)
        {
            try
            {
                StateObject state = (StateObject)ar.AsyncState;
                int receiveBytes = state.socket.EndReceive(ar); //结束当前的接收数据请求
                if (receiveBytes > 0)
                {
                    state.sb.Append(System.Text.UnicodeEncoding.UTF8.GetString(state.buffer));
                    //String content = state.sb.ToString();
                    while (state.socket.Available > 0)
                    {
                        state.socket.BeginReceive(state.buffer, 0, StateObject.bufferSizes, SocketFlags.None, new AsyncCallback(receiveCallback), state);
                        state.sb.Append(System.Text.UnicodeEncoding.UTF8.GetString(state.buffer));
                        //content += state.sb.ToString();
                    }
                    if (state.sb.Length > 0)
                    {
                        this.Invoke(new append(ShowMsg), state.socket.RemoteEndPoint.ToString() + ":" + state.sb.ToString() + Environment.NewLine);
                        //this.msg_textBox.Text += state.socket.RemoteEndPoint.ToString() + ":" + content;
                    }
                    recevieDone.Set();//继续异步接收线程
                    StateObject newstate = new StateObject();
                    newstate.socket = state.socket;
                    //准备下一次接收数据请求
                    newstate.socket.BeginReceive(newstate.buffer, 0, StateObject.bufferSizes, SocketFlags.None, new AsyncCallback(receiveCallback), newstate);
                    //将数据返回
                    state.socket.BeginSend(state.buffer, 0, state.buffer.Length, SocketFlags.None, new AsyncCallback(sendCallback), state.socket); 
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                
            }
        }

        private void sendCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                int bytes = socket.EndSend(ar);
                StateObject state = new StateObject();
                state.socket = socket;
                state.socket.BeginReceive(state.buffer, 0, StateObject.bufferSizes, SocketFlags.None, new AsyncCallback(receiveCallback), state);

            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
