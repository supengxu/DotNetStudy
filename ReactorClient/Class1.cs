using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ReactorPatternExample.Client
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 创建 TCP 客户端
                using (TcpClient client = new TcpClient())
                {
                    // 连接到服务器
                    client.Connect(IPAddress.Loopback, 8080);
                    Console.WriteLine("已连接到服务器");

                    // 获取网络流
                    using (NetworkStream stream = client.GetStream())
                    {
                        // 启动一个线程接收服务器消息
                        Thread receiveThread = new Thread(() => ReceiveMessages(stream));
                        receiveThread.IsBackground = true;
                        receiveThread.Start();

                        // 从控制台读取消息并发送到服务器
                        string message;
                        while ((message = Console.ReadLine()) != null)
                        {
                            if (message.ToLower() == "exit")
                                break;

                            // 发送消息到服务器
                            byte[] data = Encoding.ASCII.GetBytes(message);
                            stream.Write(data, 0, data.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"客户端错误: {ex.Message}");
            }

            Console.WriteLine("客户端已关闭");
            Console.ReadKey();
        }

        static void ReceiveMessages(NetworkStream stream)
        {
            try
            {
                byte[] buffer = new byte[1024];
                while (true)
                {
                    // 读取服务器响应
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // 服务器关闭连接
                        Console.WriteLine("服务器已断开连接");
                        break;
                    }

                    // 显示服务器响应
                    string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"服务器: {response}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接收消息时出错: {ex.Message}");
            }
        }
    }
}