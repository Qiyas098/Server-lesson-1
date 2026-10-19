using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Serverlesson1
{
    class Server
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("------------\nFILE SERVER\n------------");

            var ipAddress = IPAddress.Parse("192.168.100.248");
            var port = 27001;

            using (var serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                var ep = new IPEndPoint(ipAddress, port);
                serverSocket.Bind(ep);
                serverSocket.Listen(5);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Server ise salindi...");
                Console.ResetColor();

                while (true)
                {
                    var socket = await serverSocket.AcceptAsync();

                    _ = Task.Run(async () =>
                    {
                        Console.WriteLine($"\nMusteri ugurla qosuldu: {socket.RemoteEndPoint}\n");

                        try
                        {
                            while (true)
                            {
                                byte[] nameLenBuffer = new byte[4];
                                int received = 0;

                                while (received < 4)
                                {
                                    int count = await socket.ReceiveAsync(nameLenBuffer.AsMemory(received), SocketFlags.None);
                                    if (count == 0) return;
                                    received += count;
                                }
                                int nameLen = BitConverter.ToInt32(nameLenBuffer, 0);

                                byte[] nameBuffer = new byte[nameLen];
                                received = 0;
                                while (received < nameLen)
                                {
                                    int count = await socket.ReceiveAsync(nameBuffer.AsMemory(received), SocketFlags.None);
                                    if (count == 0) return;
                                    received += count;
                                }

                                string fileName = Encoding.UTF8.GetString(nameBuffer);
                                byte[] sizeBuffer = new byte[8];
                                received = 0;

                                while (received < 8)
                                {
                                    int count = await socket.ReceiveAsync(sizeBuffer.AsMemory(received), SocketFlags.None);
                                    if (count == 0) return;
                                    received += count;
                                }
                                long fileSize = BitConverter.ToInt64(sizeBuffer, 0);
                                string savePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                                using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                                {
                                    byte[] buffer = new byte[8192];
                                    long totalReceived = 0;

                                    while (totalReceived < fileSize)
                                    {
                                        int bytesRead = await socket.ReceiveAsync(
                                            buffer.AsMemory(0, (int)Math.Min(buffer.Length, fileSize - totalReceived)),
                                            SocketFlags.None);

                                        if (bytesRead == 0) return;

                                        await fs.WriteAsync(buffer, 0, bytesRead);
                                        totalReceived += bytesRead;
                                    }
                                }

                                Console.ForegroundColor = ConsoleColor.Green;
                                Console.Write("[QEBUL EDILDI]   ");
                                Console.ResetColor();
                                Console.Write(fileName);

                                int min = 1024;
                                string result = ((fileSize / min) > 1024)
                                    ? $"{(fileSize / min / min)}MB"
                                    : $"{(fileSize / min)}KB";

                                Console.Write($"  {result}");
                                Console.Write($"  {DateTime.Now}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Xeta: {ex.Message}");
                        }
                        finally
                        {
                            socket.Dispose();
                        }
                    });
                }
            }
        }
    }
}