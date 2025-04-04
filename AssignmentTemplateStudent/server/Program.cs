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

    private static async Task SendResponse(Socket server, Message response, EndPoint remoteEP)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            var responseBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, options));
            await Task.Factory.FromAsync(
                server.BeginSendTo(responseBytes, 0, responseBytes.Length, SocketFlags.None, remoteEP, null, null),
                server.EndSendTo);
            Console.WriteLine($"Sent response: {response.MsgType}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending response: {ex.Message}");
        }
    }

    public static async Task start()
    {
        IPAddress ip = IPAddress.Parse(setting?.ServerIPAddress ?? "127.0.0.1");
        int port = setting?.ServerPortNumber ?? 1234;
        IPEndPoint iPEndPoint = new(ip, port);

        using Socket server = new(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );

        try
        {
            server.Bind(iPEndPoint);
            Console.WriteLine($"Server is listening on IP: {ip} and port: {port}");

            var buffer = new byte[1024];
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

            while (true)
            {
                try
                {
                    Array.Clear(buffer, 0, buffer.Length);
                    Console.WriteLine("\nWaiting for client message...");
                    
                    int received = await Task.Factory.FromAsync(
                        server.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, null, null),
                        ar => server.EndReceiveFrom(ar, ref remoteEP));

                    var messageString = Encoding.UTF8.GetString(buffer, 0, received);
                    Console.WriteLine($"Received {received} bytes from {remoteEP}");

                    Message? message = null;
                    try
                    {
                        message = JsonSerializer.Deserialize<Message>(messageString);
                        Console.WriteLine($"Message type: {message?.MsgType}, Content: {message?.Content}");
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Failed to deserialize message: {ex.Message}");
                        continue;
                    }

                    if (message == null) continue;

                    Message response;
                    switch (message.MsgType)
                    {
                        case MessageType.Hello:
                            response = new Message
                            {
                                MsgId = message.MsgId,
                                MsgType = MessageType.Welcome,
                                Content = "Welcome to the DNS Server!"
                            };
                            break;

                        case MessageType.DNSLookup:
                            var domainName = message.Content?.ToString();
                            var record = records.FirstOrDefault(r => r.Name.Equals(domainName, StringComparison.OrdinalIgnoreCase));
                            
                            if (record != null)
                            {
                                response = new Message
                                {
                                    MsgId = message.MsgId,
                                    MsgType = MessageType.DNSLookupReply,
                                    Content = record
                                };
                            }
                            else
                            {
                                response = new Message
                                {
                                    MsgId = message.MsgId,
                                    MsgType = MessageType.Error,
                                    Content = $"Domain {domainName} not found"
                                };
                            }
                            break;

                        case MessageType.End:
                            response = new Message
                            {
                                MsgId = message.MsgId,
                                MsgType = MessageType.End,
                                Content = "Goodbye!"
                            };
                            break;

                        default:
                            response = new Message
                            {
                                MsgId = message.MsgId,
                                MsgType = MessageType.Error,
                                Content = "Unknown message type"
                            };
                            break;
                    }

                    await Task.Delay(100); // Small delay before sending response
                    await SendResponse(server, response, remoteEP);

                    if (message.MsgType == MessageType.End)
                    {
                        Console.WriteLine($"Client {remoteEP} ended session");
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Socket error: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error handling message: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Server error: {ex.Message}");
        }
    }
}