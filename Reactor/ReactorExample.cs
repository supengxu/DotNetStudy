
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ReactorPatternExample
{
    // 事件处理器接口
    public interface IEventHandler
    {
        void HandleEvent(Socket clientSocket);
    }

    // 接受连接的处理器
    public class AcceptEventHandler : IEventHandler
    {
        private readonly Reactor _reactor;

        public AcceptEventHandler(Reactor reactor)
        {
            _reactor = reactor;
        }

        public void HandleEvent(Socket serverSocket)
        {
            try
            {
                Socket clientSocket = serverSocket.Accept();
                Console.WriteLine($"新连接来自 {clientSocket.RemoteEndPoint}");
                
                // 为新客户端注册读事件处理器
                _reactor.RegisterHandler(clientSocket, new ReadEventHandler(_reactor));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"接受连接时出错: {ex.Message}");
            }
        }
    }

    // 读取数据的处理器
    public class ReadEventHandler : IEventHandler
    {
        private readonly Reactor _reactor;
        private readonly byte[] _buffer = new byte[1024];

        public ReadEventHandler(Reactor reactor)
        {
            _reactor = reactor;
        }

        public void HandleEvent(Socket clientSocket)
        {
            try
            {
                int bytesRead = clientSocket.Receive(_buffer);
                if (bytesRead > 0)
                {
                    string message = System.Text.Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                    Console.WriteLine($"从 {clientSocket.RemoteEndPoint} 收到: {message}");
                    
                    // 简单回显处理
                    string response = $"服务器收到: {message}";
                    byte[] responseBytes = System.Text.Encoding.UTF8.GetBytes(response);
                    clientSocket.Send(responseBytes);
                }
                else
                {
                    // 客户端关闭连接
                    Console.WriteLine($"连接关闭: {clientSocket.RemoteEndPoint}");
                    _reactor.RemoveHandler(clientSocket);
                    clientSocket.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"读取数据时出错: {ex.Message}");
                _reactor.RemoveHandler(clientSocket);
                clientSocket.Close();
            }
        }
    }

    // 反应器类
    public class Reactor
    {
        private readonly Dictionary<Socket, IEventHandler> _handlers = new();
        private readonly Socket _serverSocket;
        private readonly ManualResetEvent _stopEvent = new(false);
        private Thread _eventLoopThread;

        public Reactor(int port)
        {
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, port));
            _serverSocket.Listen(100);
            
            // 注册服务器套接字的接受连接处理器
            RegisterHandler(_serverSocket, new AcceptEventHandler(this));
        }

        // 注册事件处理器
        public void RegisterHandler(Socket socket, IEventHandler handler)
        {
            lock (_handlers)
            {
                _handlers[socket] = handler;
            }
        }

        // 移除事件处理器
        public void RemoveHandler(Socket socket)
        {
            lock (_handlers)
            {
                _handlers.Remove(socket);
            }
        }

        // 启动反应器
        public void Start()
        {
            _eventLoopThread = new Thread(EventLoop);
            _eventLoopThread.IsBackground = true;
            _eventLoopThread.Start();
            Console.WriteLine("Reactor 已启动，等待连接...");
        }

        // 停止反应器
        public void Stop()
        {
            _stopEvent.Set();
            if (_eventLoopThread != null && _eventLoopThread.IsAlive)
            {
                _eventLoopThread.Join();
            }
            
            lock (_handlers)
            {
                foreach (var socket in _handlers.Keys)
                {
                    socket.Close();
                }
                _handlers.Clear();
            }
            
            _serverSocket.Close();
            Console.WriteLine("Reactor 已停止");
        }

        // 事件循环
        private void EventLoop()
        {
            var readList = new List<Socket>();
            
            while (!_stopEvent.WaitOne(0))
            {
                try
                {
                    lock (_handlers)
                    {
                        readList.Clear();
                        readList.AddRange(_handlers.Keys);
                    }
                    
                    if (readList.Count > 0)
                    {
                        // 使用 Select 进行多路复用
                        Socket.Select(readList, null, null, 1000);
                        
                        foreach (var socket in readList)
                        {
                            IEventHandler handler;
                            lock (_handlers)
                            {
                                if (!_handlers.TryGetValue(socket, out handler))
                                    continue;
                            }
                            
                            // 处理事件
                            handler.HandleEvent(socket);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"事件循环出错: {ex.Message}");
                }
            }
        }
    }

    // 主程序
    class Program
    {
        static void Main(string[] args)
        {
            var reactor = new Reactor(8080);
            reactor.Start();
            
            Console.WriteLine("按 Enter 键停止服务器...");
            Console.ReadLine();
            
            reactor.Stop();
        }
    }
}