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
    static async Task Main(string[] args)
    {
        await ServerUDP.start();
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
    static string configFile = @"../Setting.json";
    static string dnsRecordsFile = @"DNSrecords.json";
    static Setting? setting;
    static List<DNSRecord>? records;

    static ServerUDP()
    {
        try
        {
            // Read and deserialize Setting.json
            string configContent = File.ReadAllText(configFile);
            setting = JsonSerializer.Deserialize<Setting>(configContent);
            if (setting == null)
                throw new Exception("Failed to deserialize Setting.json. Ensure it contains valid JSON.");

            // Read and deserialize DNSrecords.json
            string dnsRecordsContent = File.ReadAllText(dnsRecordsFile);
            records = JsonSerializer.Deserialize<List<DNSRecord>>(dnsRecordsContent);
            if (records == null)
                throw new Exception("Failed to deserialize DNSrecords.json. Ensure it contains valid JSON.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during initialization: {ex.Message}");
            Environment.Exit(1); // Exit the application if initialization fails
        }
    }

    public static async Task start()
    {
        // Use the IP address and port from the settings
        IPAddress ip = IPAddress.Parse(setting?.ServerIPAddress ?? "127.0.0.1");
        int port = setting?.ServerPortNumber ?? 1234;

        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        IPEndPoint iPEndPoint = new(ip, port);

        using Socket server = new(
            iPEndPoint.AddressFamily,
            SocketType.Stream,
            ProtocolType.Tcp
        );

        server.Bind(iPEndPoint);
        server.Listen();
        Console.WriteLine($"Server is listening on IP: {ip} and port: {port}");

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
