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
    static List<DNSRecord>? records = JsonSerializer.Deserialize<List<DNSRecord>>("DNSrecords.json");

    public static async void start()
    {
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        IPHostEntry ipEntry = await Dns.GetHostEntryAsync(Dns.GetHostName());
        IPAddress ip = ipEntry.AddressList[1];

        IPEndPoint iPEndPoint = new(ip, 1234);
        
        using Socket server = new(
            iPEndPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        server.Bind(iPEndPoint);
        server.Listen();
        Console.WriteLine("Server is listening op port: 1234");

        var handler = await server.AcceptAsync();

        while (true)
        {
            var buffer = new byte[1024];
            // TODO:[Receive and print a received Message from the client]
            var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
            // Convert bytes to string
            var messageString = Encoding.UTF8.GetString(buffer, 0, received);

            if (messageString != null)
            {
                Console.WriteLine("Message from client: {0}", messageString);
                
                // TODO:[Receive and print Hello]
                // TODO:[Send Welcome to the client]
                // TODO:[Receive and print DNSLookup]
                // TODO:[Query the DNSRecord in Json file]
                // TODO:[If found Send DNSLookupReply containing the DNSRecord]
                // TODO:[If not found Send Error]
                // TODO:[Receive Ack about correct DNSLookupReply from the client]
                // TODO:[If no further requests receieved send End to the client]
                
                var response = "Message received";
                var responseByte = Encoding.UTF8.GetBytes(response);
                await handler.SendAsync(responseByte, SocketFlags.None);
            }
        }
    }
}
