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


    public static async void start()
    {
        //TODO: [Create endpoints and socket]
        IPHostEntry ipEntry = await Dns.GetHostEntryAsync(Dns.GetHostName());
        setting.ClientIPAddress = ipEntry.AddressList[1].ToString();
        setting.ClientPortNumber = 1234;

        IPEndPoint endPoint = new((long)Convert.ToDouble(setting.ClientIPAddress), setting.ClientPortNumber);
    
        using Socket client = new (
            endPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

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

        var buffer = new byte[1_024];
        var receiveAnything = await client.ReceiveAsync(buffer, SocketFlags.None);

        await client.ConnectAsync(endPoint);
        while (true){
            var messageHelloByte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(messageHello));
            await client.SendAsync(messageHelloByte, SocketFlags.None);

        //TODO: [Receive and print Welcome from server]
            
            var receivedWelcome = receiveAnything;
            var receivedWelcomeMessage = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(buffer, 0, receivedWelcome));
            Console.WriteLine($"Received: {receivedWelcomeMessage.MsgType}, content: {receivedWelcomeMessage.Content}");

        // TODO: [Create and send DNSLookup Message]
            // for (int i = 0; i < dns)
        // TODO: [Receive and print DNSLookupReply from server]
        // TODO: [Send Acknowledgment to Server]
        // TODO: [Send next DNSLookup to server]
        // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply
            var DNS1byte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DNSLookupMessage1));
            await client.SendAsync(DNS1byte, SocketFlags.None);
            var receivedDNS1byte = receiveAnything;
            var receivedDNS1 = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(buffer, 0, receivedDNS1byte));
            Console.WriteLine($"Received: {receivedDNS1.MsgType}, content: {receivedDNS1.Content}");

            var DNS2byte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DNSLookupMessage2));
            await client.SendAsync(DNS2byte, SocketFlags.None);
            var receivedDNS2byte = receiveAnything;
            var receivedDNS2 = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(buffer, 0, receivedDNS2byte));
            Console.WriteLine($"Received: {receivedDNS2.MsgType}, content: {receivedDNS2.Content}");

            var DNS3byte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DNSLookupMessage3));
            await client.SendAsync(DNS3byte, SocketFlags.None);
            var receivedDNS3byte = receiveAnything;
            var receivedDNS3 = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(buffer, 0, receivedDNS3byte));
            Console.WriteLine($"Received: {receivedDNS3.MsgType}, content: {receivedDNS3.Content}");

            var DNS4byte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(DNSLookupMessage4));
            await client.SendAsync(DNS4byte, SocketFlags.None);
            var receivedDNS4byte = receiveAnything;
            var receivedDNS4 = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(buffer, 0, receivedDNS4byte));
            Console.WriteLine($"Received: {receivedDNS4.MsgType}, content: {receivedDNS4.Content}");
        //TODO: [Receive and print End from server]

            var Endbyte = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(EndMessage));
            await client.SendAsync(Endbyte, SocketFlags.None);
            var receivedEndbyte = receiveAnything;
            var receivedEnd = JsonSerializer.Deserialize<Message>(Encoding.UTF8.GetString(buffer, 0, receivedEndbyte));
            Console.WriteLine($"Received: {receivedEnd.MsgType}, content: {receivedEnd.Content}");
        }   
    }
}