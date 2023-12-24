using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string host = "127.0.0.1";
        int port = 8080;
        using TcpClient client = new TcpClient();
        StreamReader? reader = null;
        StreamWriter? writer = null;

        try
        {
            await client.ConnectAsync(host, port);
            reader = new StreamReader(client.GetStream());
            writer = new StreamWriter(client.GetStream());
            if (writer is null || reader is null) return;

            Console.Write("Введите свое имя: ");
            string? userName = Console.ReadLine();
            Console.WriteLine("Вы ыошли в сеть");
            Console.WriteLine($"Что бы отправить сообщение пользователю пропишите: /message (Id собеседника) свое сообщение");
            Console.WriteLine($"Что бы отправить всем, напишите и отправьте сообщение без id человека )");

            await writer.WriteLineAsync(userName);
            await writer.FlushAsync();

            Task.Run(() => ReceiveMessageAsync(reader));

            while (true)
            {
                string? message = Console.ReadLine();

                if (message != null && message.StartsWith("/message"))
                {
                    string[] parts = message.Split(' ');
                    if (parts.Length >= 3)
                    {
                        await writer.WriteLineAsync(message);
                        await writer.FlushAsync();
                    }
                    else
                    {
                        Console.WriteLine("Неправильный формат отправленного сообщения, используйте: /message (Id собеседника) свое сообщение");
                    }
                }
                else
                {
                    await writer.WriteLineAsync(message);
                    await writer.FlushAsync();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            writer?.Close();
            reader?.Close();
        }
    }

    static async Task ReceiveMessageAsync(StreamReader reader)
    {
        while (true)
        {
            try
            {
                string? message = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(message)) continue;
                Print(message);
            }
            catch
            {
                break;
            }
        }
    }

    static void Print(string message)
    {
        if (OperatingSystem.IsWindows())
        {
            var position = Console.GetCursorPosition();
            int left = position.Left;
            int top = position.Top;
            Console.MoveBufferArea(0, top, left, 1, 0, top + 1);
            Console.SetCursorPosition(0, top);
            Console.WriteLine(message);
            Console.SetCursorPosition(left, top + 1);
        }
        else Console.WriteLine(message);
    }
}
