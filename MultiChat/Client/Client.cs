using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Text;
using System.Text.Json;


namespace Client
{
    public partial class Client : Form
    {
        private string clientName;
        public Client()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            clientName = Microsoft.VisualBasic.Interaction.InputBox("Nhập tên của bạn:", "Tên Client", "Client");
            Connect();
            client.Send(Serialize(clientName));
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            Send();
            AddMessage(txbMessage.Text);
        }

        IPEndPoint IP;
        Socket client;

        void Connect()
        {
            IP = new IPEndPoint(IPAddress.Parse("192.168.1.122"), 9999);
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);

            try
            {
                client.Connect(IP);
                client.Send(Serialize(clientName));

            }
            catch
            {
                MessageBox.Show("khong the ket noi", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Thread listen = new Thread(Receive);
            listen.IsBackground = true;
            listen.Start();
        }



        void Close()
        {
            client.Close();
        }

        void Send()
        {
            if (txbMessage.Text != string.Empty)
            {
                // Thêm tên client vào tin nhắn
                string message = $"{clientName}: {txbMessage.Text}";
                client.Send(Serialize(message));
            }
        }



        void Receive()
        {
            try
            {
                byte[] data = new byte[1024];

                while (true)
                {
                    int bytesRead = client.Receive(data);
                    if (bytesRead == 0)
                    {
                        break;
                    }

                    byte[] receivedData = new byte[bytesRead];
                    Array.Copy(data, receivedData, bytesRead);

                    string message = (string)Deserialize(receivedData);

                    // Kiểm tra thông điệp yêu cầu file
                    if (message == "SERVER_REQUEST_FILE")
                    {
                        // Hiển thị hộp thoại cho người dùng quyết định
                        DialogResult result = MessageBox.Show("Bạn có muốn nhận file không?", "Nhận File", MessageBoxButtons.YesNo);
                        if (result == DialogResult.Yes)
                        {
                            // Gửi phản hồi chấp nhận file
                            client.Send(Serialize("CLIENT_ACCEPT_FILE"));

                            // Bắt đầu nhận file từ server
                            ReceiveFile();
                        }
                        else
                        {
                            // Gửi phản hồi từ chối file
                            client.Send(Serialize("CLIENT_DECLINE_FILE"));
                        }
                    }
                    else
                    {
                        // Xử lý các tin nhắn khác
                        AddMessage(message);
                    }
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"IOException: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
            finally
            {
                Close();
            }
        }
        void ReceiveFile()
        {
            try
            {
                // Gọi hộp thoại từ luồng chính
                this.Invoke(new Action(() =>
                {
                    using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                    {
                        if (saveFileDialog.ShowDialog() == DialogResult.OK)
                        {
                            string filePath = saveFileDialog.FileName;

                            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                            {
                                byte[] buffer = new byte[1024];
                                int bytesRead;

                                // Nhận file từ server
                                while ((bytesRead = client.Receive(buffer)) > 0)
                                {
                                    fileStream.Write(buffer, 0, bytesRead);
                                }
                            }

                            MessageBox.Show("File đã được lưu thành công.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi nhận file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }




        void AddMessage(string s)
        {
            lsvMessage.Items.Add(new ListViewItem() { Text = s });
            txbMessage.Clear();
        }

        byte[] Serialize(object obj)
        {
            string jsonString = JsonSerializer.Serialize(obj);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        object Deserialize(byte[] data)
        {
            string jsonString = System.Text.Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<string>(jsonString);
        }

        private void Client_FormClosed(object sender, FormClosedEventArgs e)
        {
            Close();
        }

        private void lsvMessage_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}