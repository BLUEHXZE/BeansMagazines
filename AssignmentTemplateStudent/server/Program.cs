using System;
using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LibData;

// ReceiveFrom();
class Program
{
    static void Main(string[] args)
    {
        ServerUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}


class ServerUDP
{
    static string configFile = "../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    // TODO: [Read the JSON file and return the list of DNSRecords]
    static List<DNSRecord>? records = JsonSerializer.Deserialize<List<DNSRecord>>(File.ReadAllText("../DNSrecords.json"));

    public static void start()
    {
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        using Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        serverSocket.Bind(serverEndPoint);
        Console.WriteLine("Server started...");

        byte[] buffer = new byte[1024];
        EndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            // TODO:[Receive and print a received Message from the client]
            int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
            string receivedMessage = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
            Message message = JsonSerializer.Deserialize<Message>(receivedMessage);

            Console.WriteLine($"Received: {message.MsgType}, Content: {message.Content}");

            switch (message.MsgType)
            {
                // TODO:[Receive and print Hello]
                case MessageType.Hello:
                    // TODO:[Send Welcome to the client]
                    SendResponse(serverSocket, clientEndPoint, new Message { MsgId = 1, MsgType = MessageType.Welcome, Content = "Welcome!" });
                    break;
                
                // TODO:[Receive and print DNSLookup]
                case MessageType.DNSLookup:
                    // TODO:[Query the DNSRecord in Json file]
                    DNSRecord? record = records?.Find(r => r.Name == message.Content.ToString());
                    if (record != null)
                    {
                        // TODO:[If found Send DNSLookupReply containing the DNSRecord]
                        SendResponse(serverSocket, clientEndPoint, new Message { MsgId = message.MsgId, MsgType = MessageType.DNSLookupReply, Content = record });
                    }
                    else
                    {
                        // TODO:[If not found Send Error]
                        SendResponse(serverSocket, clientEndPoint, new Message { MsgId = message.MsgId, MsgType = MessageType.Error, Content = "Record not found" });
                    }
                    break;
                
                // TODO:[Receive Ack about correct DNSLookupReply from the client]
                case MessageType.Ack:
                    Console.WriteLine("Acknowledgment received.");
                    break;
                
                // TODO:[If no further requests receieved send End to the client]
                case MessageType.End:
                    SendResponse(serverSocket, clientEndPoint, new Message { MsgId = message.MsgId, MsgType = MessageType.End, Content = "Session ended." });
                    Console.WriteLine("Ending session...");
                    return;
            }
        }
    }

    static void SendResponse(Socket socket, EndPoint clientEndPoint, Message message)
    {
        string jsonMessage = JsonSerializer.Serialize(message);
        byte[] responseBytes = Encoding.UTF8.GetBytes(jsonMessage);
        socket.SendTo(responseBytes, clientEndPoint);
        Console.WriteLine($"Sent: {message.MsgType}, Content: {message.Content}");
    }
}
