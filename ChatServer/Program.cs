using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Threading.Tasks;
class Program
{
    static async Task Main(string[] args)
    {
        ServerObject server = new ServerObject();
        await server.ListenAsync();
    }
}
class ServerObject
{
    TcpListener tcpListener = new TcpListener(IPAddress.Any, 8080);
    List<ClientObject> clients = new List<ClientObject>();
    protected internal Dictionary<int, string> clientUsernames = new Dictionary<int, string>();
    int clientIdCounter = 1;

    protected internal void RemoveConnection(int id)
    {
        ClientObject client = clients.FirstOrDefault(c => c.Id == id);
        if (client != null)
        {
            clients.Remove(client);
            client.Close();
        }
    }

    protected internal async Task ListenAsync()
    {
        try
        {
            tcpListener.Start();
            Console.WriteLine("Сервер запущен. Ожидание подключений...");

            while (true)
            {
                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

                ClientObject clientObject = new ClientObject(tcpClient, clientIdCounter++, this);
                clients.Add(clientObject);
                Task.Run(clientObject.ProcessAsync);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        finally
        {
            Disconnect();
        }
    }

    protected internal async Task BroadcastMessageAsync(string message, int senderId, int? receiverId = null)
    {
        foreach (var client in clients)
        {
            if (client.Id == senderId)
            {
                continue;
            }

            if (receiverId != null && client.Id != receiverId)
            {
                continue;
            }

            await client.Writer.WriteLineAsync(message);
            await client.Writer.FlushAsync();
        }
    }

    protected internal void Disconnect()
    {
        foreach (var client in clients)
        {
            client.Close();
        }
        tcpListener.Stop();
    }
}

class ClientObject
{
    protected internal int Id { get; }
    protected internal StreamWriter Writer { get; }
    protected internal StreamReader Reader { get; }

    TcpClient client;
    ServerObject server;

    public ClientObject(TcpClient tcpClient, int clientId, ServerObject serverObject)
    {
        client = tcpClient;
        Id = clientId;
        server = serverObject;
        var stream = client.GetStream();
        Reader = new StreamReader(stream);
        Writer = new StreamWriter(stream);
    }

    public async Task ProcessAsync()
    {
        try
        {
            string? userName = await Reader.ReadLineAsync();
            server.clientUsernames.Add(Id, userName);

            string? message = $"[{Id}] | {userName} вошел в чат";
            await server.BroadcastMessageAsync(message, Id);
            Console.WriteLine(message);

            while (true)
            {
                try
                {
                    message = await Reader.ReadLineAsync();
                    if (message == null) continue;

                    if (message.StartsWith("/message"))
                    {
                        string[] parts = message.Split(' ');
                        if (parts.Length >= 3)
                        {
                            int targetUserId = int.Parse(parts[1]);
                            message = $"[{Id}] | {userName} (личное сообщение): {string.Join(' ', parts.Skip(2))}";
                            await server.BroadcastMessageAsync(message, Id, targetUserId);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        message = $"[{Id}] | {userName}: {message}";
                        await server.BroadcastMessageAsync(message, Id);
                    }
                }
                catch
                {
                    message = $"[{Id}] | {userName} покинул чат";
                    Console.WriteLine(message);
                    await server.BroadcastMessageAsync(message, Id);
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        finally
        {
            server.RemoveConnection(Id);
        }
    }

    protected internal void Close()
    {
        Writer.Close();
        Reader.Close();
        client.Close();
    }
}
