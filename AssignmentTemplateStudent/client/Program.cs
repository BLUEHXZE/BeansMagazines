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
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    // Add message declarations
    static readonly Message messageHello = new() 
    { 
        MsgId = 1, 
        MsgType = MessageType.Hello, 
        Content = "Hello!" 
    };

    static readonly Message DNSLookupMessage1 = new()
    {
        MsgId = 2,
        MsgType = MessageType.DNSLookup,
        Content = "www.outlook.com"
    };

    static readonly Message DNSLookupMessage2 = new()
    {
        MsgId = 3,
        MsgType = MessageType.DNSLookup,
        Content = "www.google.com"
    };

    static readonly Message DNSLookupMessage3 = new()
    {
        MsgId = 4,
        MsgType = MessageType.DNSLookup,
        Content = "www.test.com"
    };

    static readonly Message DNSLookupMessage4 = new()
    {
        MsgId = 5,
        MsgType = MessageType.DNSLookup,
        Content = "www.sample.com"
    };

    static readonly Message EndMessage = new()
    {
        MsgId = 6,
        MsgType = MessageType.End,
        Content = "Goodbye"
    };

    private static async Task SendMessage(Socket client, Message message, EndPoint endPoint)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
            Console.WriteLine($"Sending: {message.MsgType} - {message.Content}");
            var messageBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, options));
            await Task.Factory.FromAsync(
                client.BeginSendTo(messageBytes, 0, messageBytes.Length, SocketFlags.None, endPoint, null, null),
                client.EndSendTo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Send failed: {ex.Message}");
            throw;
        }
    }

    private static async Task<Message?> ReceiveMessage(Socket client, byte[] buffer, ref EndPoint remoteEP)
    {
        try
        {
            Array.Clear(buffer, 0, buffer.Length);
            var receiveTask = Task.Factory.FromAsync(
                client.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref remoteEP, null, null),
                ar => client.EndReceiveFrom(ar, ref remoteEP));

            var received = await receiveTask;
            if (received == 0) return null;

            var messageString = Encoding.UTF8.GetString(buffer, 0, received);
            try
            {
                return JsonSerializer.Deserialize<Message>(messageString);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to deserialize message: {ex.Message}");
                return null;
            }
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
        {
            Console.WriteLine("Receive timed out.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Receive failed: {ex.Message}");
            return null;
        }
    }

    public static async Task start()
    {
        IPAddress serverIp = IPAddress.Parse(setting?.ServerIPAddress ?? "127.0.0.1");
        int serverPort = setting?.ServerPortNumber ?? 1234;
        IPEndPoint serverEndPoint = new(serverIp, serverPort);

        IPEndPoint clientEndPoint = new(IPAddress.Any, setting?.ClientPortNumber ?? 5678); // Ensure different port
        using Socket client = new(
            AddressFamily.InterNetwork,
            SocketType.Dgram,
            ProtocolType.Udp
        );
        
        client.Bind(clientEndPoint); // Bind to receive responses
        Console.WriteLine($"Client bound to port {((IPEndPoint)client.LocalEndPoint).Port}");

        var buffer = new byte[1024];
        EndPoint remoteEP = serverEndPoint;

        try
        {
            foreach (var message in new[] { messageHello, DNSLookupMessage1, DNSLookupMessage2, 
                                          DNSLookupMessage3, DNSLookupMessage4, EndMessage })
            {
                int retryCount = 0;
                const int maxRetries = 3;

                while (retryCount < maxRetries)
                {
                    Console.WriteLine($"\nSending {message.MsgType} message... (Attempt {retryCount + 1})");
                    await SendMessage(client, message, serverEndPoint);

                    client.ReceiveTimeout = 5000; // 5-second timeout
                    var response = await ReceiveMessage(client, buffer, ref remoteEP);

                    if (response != null)
                    {
                        Console.WriteLine($"Received: {response.MsgType}, Content: {response.Content}");
                        if (response.MsgType == MessageType.Error)
                        {
                            Console.WriteLine($"Server returned error: {response.Content}");
                        }
                        break; // Successfully received response
                    }
                    else
                    {
                        retryCount++;
                        if (retryCount >= maxRetries)
                        {
                            Console.WriteLine($"Failed to receive response after {maxRetries} attempts.");
                        }
                        else
                        {
                            Console.WriteLine($"No response received, retrying... ({retryCount}/{maxRetries})");
                            await Task.Delay(1000 * retryCount); // Exponential backoff
                        }
                    }
                }

                if (message.MsgType == MessageType.End)
                {
                    Console.WriteLine("Ending session...");
                    break;
                }

                await Task.Delay(1000); // Delay between messages
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}