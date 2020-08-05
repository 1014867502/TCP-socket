using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;

namespace _2222222
{
    public partial class frmClient : Form
    {
        public frmClient()
        {
            InitializeComponent();
            TextBox.CheckForIllegalCrossThreadCalls = false;
        }

        Thread threadClient = null; // 创建用于接收服务端消息的 线程；
        Socket sockClient = null;
        List<Socket> sockets = new List<Socket>();
        List<Thread> threads = new List<Thread>();
        private void btnConnect_Click(object sender, EventArgs e)
        {
            int j = Convert.ToInt32(txtNumber.Text.ToString());
            for (int i = 0; i < j; i++)
            {
                IPAddress ip = IPAddress.Parse(txtIp.Text.Trim());
                IPEndPoint endPoint = new IPEndPoint(ip, int.Parse(txtPort.Text.Trim()));
                sockClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


                sockClient.IOControl(IOControlCode.KeepAliveValues, GetKeepAliveData(), null);
                sockClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                try
                {
                    ShowMsg("与服务器连接中……");
                    sockClient.Connect(endPoint);
                    sockets.Add(sockClient);
                }
                catch (SocketException se)
                {
                    MessageBox.Show(se.Message);
                    return;
                    //this.Close();
                }
                ShowMsg("与服务器连接成功！！！");
                threadClient = new Thread(RecMsg);
                threadClient.IsBackground = true;
                threads.Add(threadClient);
                threadClient.Start(sockClient);
            }
            Thread keepAlive = new Thread(CheckAlive);
        }

        //监听线程，随时接受数据
        void RecMsg(Object o)
        {
            bool isRestart = false;
            Socket socket = o as Socket;
            while (true)
            {
                // 定义一个2M的缓存区；
                byte[] arrMsgRec = new byte[1024 * 2];
                // 将接受到的数据存入到输入  arrMsgRec中；
                int length = -1;
                bool bol = isClientConnect(socket);//判断是否断开

                if (bol == false)
                {//断开的情况
                    //ShowMsg("没连接,开始重连");
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    sockets.Remove(socket);
                    isRestart = true;//设置标记位，判断是否重连
                    while (isRestart)
                    {
                        IPAddress ip = IPAddress.Parse(txtIp.Text.Trim());
                        IPEndPoint endPoint = new IPEndPoint(ip, int.Parse(txtPort.Text.Trim()));
                        Socket newSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        newSocket.IOControl(IOControlCode.KeepAliveValues, GetKeepAliveData(), null);
                        newSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        try
                        {
                            ShowMsg("与服务器连接中……");
                            newSocket.Connect(endPoint);
                            sockets.Add(newSocket);
                            socket = newSocket;
                            isRestart = false;
                        }
                        catch (SocketException se)
                        {
                            ShowMsg("连接失败，继续重连……");
                            isRestart = true;
                            Thread.Sleep(3000);
                            continue;
                            //this.Close();
                        }
                        ShowMsg("与服务器连接成功！！！");
                    }
                }
                if (bol == true)
                {
                  
                    try
                    {
                        length = socket.Receive(arrMsgRec); // 接收数据，并返回数据的长度；
                    }
                    catch (SocketException se)
                    {
                        ShowMsg("异常；" + se.Message);
                    }
                    catch (Exception e)
                    {
                        ShowMsg("异常：" + e.Message);
                    }
                    if (arrMsgRec[0] == 0)  // 表示接收到的是数据；
                    {
                        try
                        {
                            string strMsg = System.Text.Encoding.UTF8.GetString(arrMsgRec, 1, length - 1);// 将接受到的字节数据转化成字符串；
                            ShowMsg(strMsg);
                        }
                        catch (Exception ex)
                        {
                            ShowMsg("length长度异常");
                        }
                    }
                    if (arrMsgRec[0] == 1) // 表示接收到的是文件数据；
                    {
                        try
                        {
                            SaveFileDialog sfd = new SaveFileDialog();
                            if (sfd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                            {// 在上边的 sfd.ShowDialog（） 的括号里边一定要加上 this 否则就不会弹出 另存为 的对话框，而弹出的是本类的其他窗口，，这个一定要注意！！！【解释：加了this的sfd.ShowDialog(this)，“另存为”窗口的指针才能被SaveFileDialog的对象调用，若不加thisSaveFileDialog 的对象调用的是本类的其他窗口了，当然不弹出“另存为”窗口。】

                                string fileSavePath = sfd.FileName;// 获得文件保存的路径；
                                                                   // 创建文件流，然后根据路径创建文件；
                                using (FileStream fs = new FileStream(fileSavePath, FileMode.Create))
                                {
                                    fs.Write(arrMsgRec, 1, length - 1);
                                    ShowMsg("文件保存成功：" + fileSavePath);
                                }
                            }
                        }
                        catch (Exception aaa)
                        {
                            MessageBox.Show(aaa.Message);
                        }
                    }
                }
            }
        }
        //显示数据
        void ShowMsg(string str)
        {
            txtMsg.AppendText(str + "\r\n");
        }

        // 发送消息；
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            int j = Convert.ToInt32(txtNumber.Text.ToString());
            if (sockets.Count <= 0)
            {
                ShowMsg("未连接，请连接后再发消息");
            }
            else
            {
                for (int i = 0; i < j; i++)
                {
                    string strMsg ="编号"+i + sockets[i].LocalEndPoint + txtName.Text.Trim() + "\r\n" + "    -->" + txtSendMsg.Text.Trim() + "\r\n";
                    byte[] arrMsg = Encoding.UTF8.GetBytes(strMsg);
                    byte[] arrSendMsg = new byte[arrMsg.Length + 1];
                    arrSendMsg[0] = 0; // 用来表示发送的是消息数据
                    Buffer.BlockCopy(arrMsg, 0, arrSendMsg, 1, arrMsg.Length);
                    sockets[i].BeginSend(arrMsg, 0, arrMsg.Length, SocketFlags.None, new AsyncCallback(sendCallback),sockets[i]); // 发送消息；
                    ShowMsg(strMsg);
                }
            }
        }

        // 选择要发送的文件；
        private void btnSelectFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = "D:\\";
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSelectFile.Text = ofd.FileName;
            }
        }

        //向服务器端发送文件
        private void btnSendFile_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtSelectFile.Text))
            {
                MessageBox.Show("请选择要发送的文件！！！");
            }
            else
            {
                // 用文件流打开用户要发送的文件；
                using (FileStream fs = new FileStream(txtSelectFile.Text, FileMode.Open))
                {
                    //在发送文件以前先给好友发送这个文件的名字+扩展名，方便后面的保存操作；
                    string fileName = System.IO.Path.GetFileName(txtSelectFile.Text);
                    string fileExtension = System.IO.Path.GetExtension(txtSelectFile.Text);
                    string strMsg = "我给你发送的文件为： " + fileName + "\r\n";
                    byte[] arrMsg = System.Text.Encoding.UTF8.GetBytes(strMsg);
                    byte[] arrSendMsg = new byte[arrMsg.Length + 1];
                    arrSendMsg[0] = 0; // 用来表示发送的是消息数据
                    Buffer.BlockCopy(arrMsg, 0, arrSendMsg, 1, arrMsg.Length);
                    sockClient.Send(arrSendMsg); // 发送消息；

                    byte[] arrFile = new byte[1024 * 1024 * 2];
                    int length = fs.Read(arrFile, 0, arrFile.Length);  // 将文件中的数据读到arrFile数组中；
                    byte[] arrFileSend = new byte[length + 1];
                    arrFileSend[0] = 1; // 用来表示发送的是文件数据；
                    Buffer.BlockCopy(arrFile, 0, arrFileSend, 1, length);
                    // 还有一个 CopyTo的方法，但是在这里不适合； 当然还可以用for循环自己转化；
                    sockClient.Send(arrFileSend);// 发送数据到服务端；
                    txtSelectFile.Clear();
                }
            }
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void frmClient_Load(object sender, EventArgs e)
        {

        }

        //暂停所有连接
        private void btnPause_Click(object sender, EventArgs e)
        {
            try
            {
                int j = Convert.ToInt32(txtNumber.Text.ToString());
                for (int i = 0; i < j; i++)
                {
                    sockets[i].Shutdown(SocketShutdown.Both);
                    sockets[i].Close();
                    threads[i].Interrupt();
                    threads[i].Abort();
                }
                sockets.Clear();
                threads.Clear();
            }
            catch (Exception ex)
            {
                ShowMsg("已经清空连接");
            }

        }

        private void btnTest_Click(object sender, EventArgs e)
        {
            if (sockets.Count <= 0)
            {
                ShowMsg("未连接，请连接后再发送消息");
            }
            else
            {
                int number = 0;
                int j = Convert.ToInt32(txtMsgNum.Text.ToString());
                int k = Convert.ToInt32(txtNumber.Text.ToString());
                for (int z = 0; z < j; z++)
                {
                    for (int i = 0; i < k; i++)
                    {
                        number++;
                        //Thread.Sleep(300);                      
                        string strMsg = "编号" + i + sockets[i].LocalEndPoint + txtName.Text.Trim() + "\r\n" + "    -->" + txtSendMsg.Text.Trim() + "\r\n";
                        byte[] arrMsg = Encoding.UTF8.GetBytes(strMsg);
                        byte[] arrSendMsg = new byte[arrMsg.Length + 1];
                        arrSendMsg[0] = 0; // 用来表示发送的是消息数据
                        Buffer.BlockCopy(arrMsg, 0, arrSendMsg, 1, arrMsg.Length);
                        sockets[i].BeginSend(arrMsg, 0, arrMsg.Length, SocketFlags.None, new AsyncCallback(sendCallback), sockets[i]); // 发送消息；
                        ShowMsg(strMsg);
                    }
                }
                ShowMsg("发送数据数量："+number);
            }



        }

        //断线重连模式
        private void btnRestart_Click(object sender, EventArgs e)
        {
            int j = Convert.ToInt32(txtNumber.Text.ToString());

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

        //判断是否连接
        public bool isClientConnect(Socket client)
        {
            IPGlobalProperties iproperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnections = iproperties.GetActiveTcpConnections();
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
            return false;
        }

        private void CheckAlive()
        {
            Thread.Sleep(10000);
            while (true)
            {
                try
                {

                    if (sockets.Count > 0)
                    {
                        foreach (Socket item in sockets)
                        {
                            //if (item.Client.Client.Poll(500, System.Net.Sockets.SelectMode.SelectRead) && (item.Client.Client.Available == 0))

                            if (!isClientConnect(item))
                            {
                                ShowMsg("CheckAlive移除链");
                                sockets.Remove(item);
                                continue;
                            }
                        }
                    }
                    else
                    {
                        ShowMsg("sockets列表为空");
                    }
                }
                catch (Exception e)
                {
                    ShowMsg("CheckAlive异常." + e.Message);
                }

                Thread.Sleep(500);
            }

        }

        //异步发送回调函数
        private void sendCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket2 = null;
                socket2 = (Socket)ar.AsyncState;
                socket2.EndSend(ar); //结束异步发送
                //state.socket.BeginReceive(state.buffer, 0, state.buffer.Length, SocketFlags.None, new AsyncCallback(receiveCallback), state); //开始异步接收
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
