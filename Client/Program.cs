using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Serverlesson1
{
    class Client
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("------------\nFILE CLIENT\n-----------");

            var ipAddress = IPAddress.Parse("192.168.100.248");
            var port = 27001;
            var ep = new IPEndPoint(ipAddress, port);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    await socket.ConnectAsync(ep);
                    Console.WriteLine($"{socket.RemoteEndPoint}: Servere qosuldunuz...");

                    while (true)
                    {
                        Console.WriteLine("_______File Transferi_______");
                        Console.WriteLine("exit - Cixis");
                        Console.WriteLine("---------------------------------");
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write("Faylin yolunu daxil edin: \n");
                        var filePath = Console.ReadLine()?.Trim();
                        Console.ResetColor();

                        if (filePath?.Trim() == "exit")
                        {
                            Console.WriteLine("Emeliyyat sonlandirildi");
                            break;
                        }
                        else if (!File.Exists(filePath))
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Bele bir fayl tapilmadi...! Yeniden daxil edin\n");
                            Console.ResetColor();
                            continue;
                        }

                        string fileName = Path.GetFileName(filePath);
                        byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
                        long fileSize = new FileInfo(filePath).Length;
                        int min = 1024;
                        string result = ((fileSize / min) > 1024)
                            ? $"{(fileSize / min / min)} MB"
                            : $"{(fileSize / min)} KB";

                        Console.WriteLine($"Gonderilir: {fileName} : {result}...");

                        await socket.SendAsync(BitConverter.GetBytes(nameBytes.Length), SocketFlags.None);
                        int sent = 0;
                        while (sent < nameBytes.Length)
                        {
                            sent += await socket.SendAsync(
                                nameBytes.AsMemory(sent),
                                SocketFlags.None);
                        }

                        byte[] sizeBytes = BitConverter.GetBytes(fileSize);
                        sent = 0;

                        while (sent < sizeBytes.Length)
                        {
                            sent += await socket.SendAsync(
                                sizeBytes.AsMemory(sent),
                                SocketFlags.None);
                        }

                        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead;

                            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                int totalSent = 0;

                                while (totalSent < bytesRead)
                                {
                                    totalSent += await socket.SendAsync(
                                        buffer.AsMemory(totalSent, bytesRead - totalSent),
                                        SocketFlags.None);
                                }
                            }
                        }
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Fayl ugurla gonderildi...\n");
                        Console.ResetColor();
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Xeta: {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
    }
}