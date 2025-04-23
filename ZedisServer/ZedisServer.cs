using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Zedis
{
    public class ZedisServer
    {
        private readonly int _port;
        private TcpListener ?_listener;
        private readonly DataStore _dataStore = new DataStore();
        

        public ZedisServer(int port) {
            
            _port = port;
        }

        public void Start() 
        {
            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Zedis listening on port {_port}");

            while (true) 
            {
                var client = _listener.AcceptTcpClient();
                Console.WriteLine("Client connected");

                Thread thread = new Thread(async () => await HandleClient(client));
                thread.Start();


            }
        }

        private async Task HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            while (true) 
            {
                try
                {
                    List<string> parts = await ParseRESP(reader);
                    // Command logging to file and console
                    Console.WriteLine($"[{DateTime.Now}] Command: {string.Join(" ", parts)}");
                    File.AppendAllText("zedis.log", $"[{DateTime.Now}] Command: {string.Join(" ", parts)}{Environment.NewLine}");
                    if (parts == null || parts.Count == 0) 
                    {
                        continue;
                    }

                    var result = ProcessCommand(parts);

                    if (result == "QUIT")
                    {
                        await writer.WriteAsync("+OK\r\n");
                        Console.WriteLine("Client disconnected");
                        break; // It will also dispose the stream 
                        
                    }

                    var response = ToRESP(result);
                    await writer.WriteAsync(response);
                }
                catch (Exception ex) 
                {
                    writer.Write("-ERR " + ex.Message + "\r\n");
                    break;
                }
            }
        }

        private string ProcessCommand(List<string> parts)
        {
            
            if (parts.Count == 0) return "ERR uknown command";

            var cmd = parts[0].ToUpper();

            return cmd switch
            {
                "SET" when parts.Count >= 3 => _dataStore.Set(parts[1], string.Join(' ', parts.Skip(2))),
                "GET" when parts.Count == 2 => _dataStore.Get(parts[1]),
                "DEL" when parts.Count >= 2 => _dataStore.Del(parts.Skip(1)),
                "EXISTS" when parts.Count >= 2 => _dataStore.Exists(parts.Skip(1)),
                "INCR" when parts.Count == 2 => _dataStore.Incr(parts[1]),
                "TYPE" when parts.Count == 2 => _dataStore.Type(parts[1]),
                "EXPIRE" when parts.Count == 3 => _dataStore.Expire(parts.Skip(1)),
                "TTL" when parts.Count == 2 => _dataStore.Ttl(parts[1]),
                "ECHO" when parts.Count == 2 => _dataStore.Echo(parts[1]),
                "QUIT" => "QUIT",
                "PING" => "PONG",
                "SAVE" => _dataStore.Save(),
                _ => "ERR unknown or invalid command"
            };
        }
        
        //*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$5\r\nMarko\r\n
        private async Task<List<string>> ParseRESP(StreamReader reader)
        {
            string? firstLine = await reader.ReadLineAsync();

            List<string> parts = new();
            
            if (string.IsNullOrWhiteSpace(firstLine) || firstLine[0] != '*') 
            {
                throw new Exception("Invalid RESP format: expected array");
            }

            if (!int.TryParse(firstLine.Substring(1), out int count))
            {
                throw new Exception("Invalid array length");
            }

            for (int i = 0; i < count; i++) 
            {
                string? sizeLine = await reader.ReadLineAsync();
                if (sizeLine == null || sizeLine[0] != '$') 
                {
                    throw new Exception("invalid bulk string length");
                }
                if (!int.TryParse(sizeLine.Substring(1), out int length)) 
                {
                    throw new Exception("Invalid bulk string length");
                }

                string? dataLine = await reader.ReadLineAsync();
                if (dataLine == null || dataLine.Length != length) 
                {
                    throw new Exception("String length mismatch");
                }

                parts.Add(dataLine);

            }
            return parts;

        }

        //*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$5\r\nMarko\r\n
        private string ToRESP(string result)
        {
            if (result == "(nil)") 
            {
                return "$-1\r\n";
            }
            if (result.StartsWith("ERR")) 
            {
                return $"-{result}\r\n";
            }
            if (result == "OK" || result == "PONG") 
            {
                return $"+{result}\r\n";
            }
            if (int.TryParse(result, out _)) 
            {
                return $":{result}\r\n";
            }

            return $"${result.Length}\r\n{result}\r\n";
        }
    }
}