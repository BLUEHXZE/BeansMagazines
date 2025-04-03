using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

// SendTo();
class Program
{
    static void Main(string[] args)
    {
        ClientUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}

class ClientUDP
{

    //TODO: [Deserialize Setting.json]
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    private static async Task SendMessage(Socket client, Message message)
    {
        try
        {
            Console.WriteLine($"Sending: {message.MsgType} - {message.Content}");
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
            await client.SendAsync(messageBytes, SocketFlags.None);
            Console.WriteLine("Message sent successfully.");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Send failed: {ex.Message}");
        }
    }

    private static async Task ReceiveAndPrintResponse(Socket client)
    {
        var buffer = new byte[1024];
        var received = await client.ReceiveAsync(buffer, SocketFlags.None);
        
        if (received == 0) 
        {
            Console.WriteLine("Server closed connection.");
            client.Close();
            return;
        }

        var receivedMessage = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(buffer, 0, received));
        Console.WriteLine($"Received: {receivedMessage.MsgType}, Content: {receivedMessage.Content}");
    }


    public static async Task start()
    {
        //TODO: [Create and send HELLO]
        var messageHello = new Message
        {
            MsgId = 1,
            MsgType = MessageType.Hello,
            Content = "Hello!"
        };

        var DNSLookupMessage1 = new Message
        {
            MsgId = 2,
            MsgType = MessageType.DNSLookup,
            Content = "www.outlook.com"
        };

        var DNSLookupMessage2 = new Message
        {
            MsgId = 3,
            MsgType = MessageType.DNSLookup,
            Content = "www.google.com"
        };

        var DNSLookupMessage3 = new Message
        {
            MsgId = 4,
            MsgType = MessageType.DNSLookup,
            Content = "www.minecraft.net"
        };

        var DNSLookupMessage4 = new Message
        {
            MsgId = 5,
            MsgType = MessageType.DNSLookup,
            Content = "www.test.com"
        };

        var EndMessage = new Message
        {
            MsgId = 6,
            MsgType = MessageType.End,
            Content = "No lookups anymore"
        };

        //TODO: [Create endpoints and socket]
        IPAddress serverIp = IPAddress.Parse(setting?.ServerIPAddress ?? "127.0.0.1");
        int serverPort = setting?.ServerPortNumber ?? 1234;

        IPEndPoint endPoint = new(serverIp, serverPort);
    
        using Socket client = new (
            endPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );
        Console.WriteLine("connecting...");
        await client.ConnectAsync(endPoint);

        Console.WriteLine("Connected to server!");

        while (true){
            Console.WriteLine("Sending message 1");
            await SendMessage(client, messageHello);
            await ReceiveAndPrintResponse(client);
            await Task.Delay(500);

            Console.WriteLine("Sending message 2");
            await SendMessage(client, DNSLookupMessage1);
            await ReceiveAndPrintResponse(client);
            await Task.Delay(500);

            Console.WriteLine("Sending message 3");
            await SendMessage(client, DNSLookupMessage2);
            await ReceiveAndPrintResponse(client);
            await Task.Delay(500);

            Console.WriteLine("Sending message 4");
            await SendMessage(client, DNSLookupMessage3);
            await ReceiveAndPrintResponse(client);
            await Task.Delay(500);

            Console.WriteLine("Sending message 5");
            await SendMessage(client, DNSLookupMessage4);
            await ReceiveAndPrintResponse(client);
            await Task.Delay(500);

            Console.WriteLine("Sending message 6");
            await SendMessage(client, EndMessage);
            await ReceiveAndPrintResponse(client);
            await Task.Delay(500);

            Console.WriteLine("Disconnecting from server.");
            break;
        }   
    }
}