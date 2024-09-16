using System.Net.Sockets;
using System.Net;
using System.Text.Json;
using System.Windows.Forms;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;

namespace Server
{

    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            Connect();
            this.btnSendFile.Click += new System.EventHandler(this.btnSendFile_Click);

        }

        private void Server_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            //gửi tin nhắn đến tất cả client
            foreach (Socket item in clientList)
            {
                Send(item);
            }

            AddMessage(txbMessage.Text);

        }

        IPEndPoint IP;
        Socket server;
        List<Socket> clientList;

        void Connect()
        {
            clientList = new List<Socket>(); //list client
            IP = new IPEndPoint(IPAddress.Any, 9999); //IP
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP); //sotket

            server.Bind(IP); //gán ip và cổng cho socket
            Thread Listen = new Thread(() =>
            {
                try
                {
                    while (true)
                    {
                        server.Listen(100); //lắng nghe
                        Socket client = server.Accept(); //chấp nhận
                        clientList.Add(client); //add->list

                        // Nhận tên client từ client
                        byte[] nameBuffer = new byte[1024];
                        int nameBytes = client.Receive(nameBuffer);
                        string clientName = Encoding.UTF8.GetString(nameBuffer, 0, nameBytes);

                        // Thêm tên client vào ListBox
                        Invoke((MethodInvoker)(() =>
                        {
                            ListClient.Items.Add(clientName);
                        }));

                        //tạo luồng nhận data từ client
                        Thread receive = new Thread(Receive);
                        receive.IsBackground = true;
                        receive.Start(client);
                    }
                }
                catch
                {
                    //xử lí lỗi kết nối
                    IP = new IPEndPoint(IPAddress.Any, 9999);
                    server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
                }
            });
            Listen.IsBackground = true;
            Listen.Start();
        }


        


        void Close()
        {
            server.Close();
        }

        void Send(Socket client)
        {
            if (txbMessage.Text != string.Empty)
            {
                //gửi tin nhắn
                string message = $"SERVER: {txbMessage.Text}";
                client.Send(Serialize(message));
            }
            txbMessage.Clear();
        }


        void Receive(object obj)
        {
            Socket client = obj as Socket;
            string clientName = string.Empty;

            try
            {
                // Đọc tên client từ client 
                byte[] nameBuffer = new byte[1024];
                int nameBytes = client.Receive(nameBuffer);
                clientName = Encoding.UTF8.GetString(nameBuffer, 0, nameBytes);

                while (true)
                {
                    //nhận dữ liệu
                    byte[] data = new byte[1024 * 5000];
                    int bytesReceived = client.Receive(data);

                    if (bytesReceived == 0)
                    {
                        // Client đã ngắt kết nối
                        break;
                    }

                    string message = (string)Deserialize(data);
                    AddMessage(message);
                }
            }
            catch
            {
                // Xử lý lỗi ngắt kết nối không mong muốn
            }
            finally
            {
                // Xóa client khỏi danh sách và cập nhật giao diện người dùng
                clientList.Remove(client);
                UpdateClientList();

                client.Close();
            }
        }

        private void btnSendFile_Click(object sender, EventArgs e)
        {
            //hộp thoại
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = openFileDialog.FileName;

                foreach (Socket client in clientList)
                {
                    // Gửi yêu cầu gửi file đến client
                    string requestMessage = "SERVER_REQUEST_FILE";
                    client.Send(Serialize(requestMessage));

                    // Gửi file nếu client chấp nhận
                    Thread receiveResponse = new Thread(() => ReceiveFileResponse(client, filePath));
                    receiveResponse.IsBackground = true;
                    receiveResponse.Start();
                }
            }
        }

        void ReceiveFileResponse(Socket client, string filePath)
        {
            // Nhận phản hồi từ client
            byte[] buffer = new byte[1024];
             int receivedBytes = client.Receive(buffer);
            string response = Encoding.UTF8.GetString(buffer, 0, receivedBytes);

            if (response == "CLIENT_ACCEPT_FILE")
            {
                SendFile(client, filePath);
            }
        }

        void SendFile(Socket client, string filePath)
        {
            //đọc dữ liệu 
            byte[] fileData = File.ReadAllBytes(filePath);
            client.Send(fileData);
        }


        void UpdateClientList()
        {
            // Xóa tất cả các tên client hiện có trong ListBox
            ListClient.Items.Clear();

            // Thêm lại tất cả các tên client từ danh sách client
            foreach (Socket client in clientList)
            {
                // Đọc tên client từ client (có thể bạn đã làm điều này ở nơi khác)
                byte[] nameBuffer = new byte[1024];
                int nameBytes = client.Receive(nameBuffer);
                string clientName = Encoding.UTF8.GetString(nameBuffer, 0, nameBytes);

                ListClient.Items.Add(clientName);
            }
        }



        void AddMessage(string s)
        {
            lsvMessage.Items.Add(new ListViewItem() { Text = s });
        }

        byte[] Serialize(object obj)
        {
            //chuyển đối tượng thành chuỗi json và mã hoá thành mảng byte
            string jsonString = JsonSerializer.Serialize(obj);
            return System.Text.Encoding.UTF8.GetBytes(jsonString);
        }

        object Deserialize(byte[] data)
        {
            //giải mã mảng byte và chuyển chuỗi json thành đối tượng
            string jsonString = System.Text.Encoding.UTF8.GetString(data).TrimEnd('\0');
            return JsonSerializer.Deserialize<string>(jsonString);
        }

        private void txbMessage_TextChanged(object sender, EventArgs e)
        {

        }

        private void ListClient_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}