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

            // writer.WriteLine("Welcome to Zedis!");

            string? line;
            while ((line = reader.ReadLine()) != null) 
            {
                var response = ProcessCommand(line);
                Console.WriteLine($"Received: {line}");
                writer.WriteLine(response);
            }
        }

        private string ProcessCommand(string line)
        {
            var parts = line.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "ERR uknown command";

            var cmd = parts[0].ToUpper();

            return cmd switch
            {
                "SET" when parts.Length == 3 => _dataStore.Set(parts[1], parts[2]),
                "GET" when parts.Length == 2 => _dataStore.Get(parts[1]),
                _ => "ERR unknown or invalid command"
            };
        }
    }
}