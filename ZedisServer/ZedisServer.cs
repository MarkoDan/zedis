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

                Thread thread = new Thread(() => HandleClient(client));
                thread.Start();


            }
        }

        private void HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            var reader = new StreamReader(stream, Encoding.UTF8);
            var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            while (true) 
            {
                try
                {
                    var parts = ParseRESP(reader);
                    if (parts == null || parts.Count == 0) 
                    {
                        continue;
                    }

                    var result = ProcessCommand(parts);
                    var response = ToRESP(result);
                    writer.Write(response);
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
                "PING" => "PONG",
                _ => "ERR unknown or invalid command"
            };
        }
        
        //*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$5\r\nMarko\r\n
        private async Task<List<string>> ParseRESP(StreamReader reader)
        {
            string ?line = await reader.ReadLineAsync();

            List<string> parts = new();
            
            if (line == null || line.Length == 0 || line[0] != '*') 
            {
                throw new Exception("Invalid RESP format - expected array");
            }
            
            

            foreach (var item in line) 
            {

            }




            return [];
        }

        private string ToRESP(string result)
        {
            return result;
        }
    }
}