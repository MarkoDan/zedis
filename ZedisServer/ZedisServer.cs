using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace Zedis
{
    public class ZedisServer
    {
        private int _port;
        private TcpListener ?_listener;
        private readonly DataStore _dataStore = new DataStore();
        private readonly Dictionary<string, string> _config = new();
        private bool _appendOnlyEnabled = true;
        

        
        public void Start() 
        {
            LoadConfig();
            int defaultPort = 6555;
            if (_config.TryGetValue("port", out var portValue) && int.TryParse(portValue, out var portFromConfig))
            {
                _port = portFromConfig;
            }
            else 
            {
                _port = defaultPort;
            }

            _listener = new TcpListener(IPAddress.Any, _port);
            _listener.Start();
            Console.WriteLine($"Zedis listening on port {_port}");
            
            

            if (_config.TryGetValue("loadstart", out var shouldLoad) && shouldLoad.ToLower() == "yes") 
            {
                _dataStore.Load();

                if (File.Exists("appendonly.aof"))
                {
                    foreach (var line in File.ReadLines("appendonly.aof"))
                    {
                        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
                        ProcessCommand(parts);
                    }
                }
            }
            
                    

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
                    // Appending commands in aof file
                    if (_appendOnlyEnabled) 
                    {
                        File.AppendAllText("appendonly.aof", $"{string.Join(" ", parts)}\r\n");
                    }
                    // Command logging to file and console
                    Console.WriteLine($"[{DateTime.Now}] Command: {string.Join(" ", parts)}");
                    File.AppendAllText("zedis.log", $"[{DateTime.Now}] Command: {string.Join(" ", parts)}{Environment.NewLine}");
                    if (parts == null || parts.Count == 0) 
                    {
                        continue;
                    }

                    var result = ProcessCommand(parts);
                    
                    if (result is string str)
                    {
                        if (str == "QUIT")
                        {
                            await writer.WriteAsync("+OK\r\n");
                            Console.WriteLine("Client disconnected");
                            break; // It will also dispose the stream 
                            
                        }

                        var response = ToRESP(str);
                        await writer.WriteAsync(response);
                        
                    }
                    else if (result is IEnumerable<string> list)
                    {
                        var response = ToRESP(list);
                        await writer.WriteAsync(response);
                    }

                   
                }
                catch (Exception ex) 
                {
                    writer.Write("-ERR " + ex.Message + "\r\n");
                    break;
                }
            }
        }

        private object ProcessCommand(List<string> parts)
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
                "BGSAVE" => _dataStore.BgSave(),
                "CONFIG" when parts.Count >= 2 => HandleConfig(parts),
                "APPEND" when parts.Count >=3 => _dataStore.Append(parts[1], string.Join(' ', parts.Skip(2))),
                "STRLEN" when parts.Count == 2 => _dataStore.Strlen(parts[1]),
                "MSET" when parts.Count >= 5 => _dataStore.MSet(parts.Skip(1)),
                "MGET" when parts.Count >= 2 => _dataStore.MGet(parts.Skip(1)),
                "SETEX" when parts.Count >= 4 => _dataStore.Setex(parts[1], parts[2], parts[3]),
                "SETNX" when parts.Count >= 3 => _dataStore.Setnx(parts[1], string.Join(' ', parts.Skip(2))),
                "INCRBY" when parts.Count >= 3 => _dataStore.IncrBy(parts[1], parts[2]),
                "DECR" when parts.Count == 2 => _dataStore.Decr(parts[1]),
                "DECRBY" when parts.Count >= 3 => _dataStore.DecrBy(parts[1], parts[2]),
                "LPUSH" when parts.Count >= 3 => _dataStore.LPush(parts.Skip(1).ToList()),
                "RPUSH" when parts.Count >= 3 => _dataStore.RPush(parts.Skip(1).ToList()),
                "LPOP" when parts.Count >= 2 => _dataStore.LPop(parts.Skip(1).ToList()),
                "RPOP" when parts.Count >= 2 => _dataStore.RPop(parts.Skip(1).ToList()),
                "LLEN" when parts.Count == 2 => _dataStore.LLen(parts[1]),
                "SADD" when parts.Count >= 2 => _dataStore.Sadd(parts.Skip(1).ToList()),
                "SREM" when parts.Count >= 2 => _dataStore.Sadd(parts.Skip(1).ToList()),
                "SMEMBERS" when parts.Count == 2 => _dataStore.Smembers(parts[1]),
                "SCARD" when parts.Count == 2 => _dataStore.Scard(parts[1]),
                
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

        private string ToRESP(IEnumerable<string> results) 
        {
            // Start an array of length N
            var sb = new StringBuilder();
            sb.Append($"*{results.Count()}\r\n");

            foreach (var item in results) 
            {
                if (item == "(nil)") 
                {
                    sb.Append("$-1\r\n");
                }
                else{
                   sb.Append($"${item.Length}\r\n{item}\r\n"); 
                }
            }

            return sb.ToString();
        }


        private void LoadConfig() 
        {
            if (!File.Exists("zedis.conf")) return;

            var lines = File.ReadLines("zedis.conf");

            foreach (var line in lines) 
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                var parts = line.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2) {
                    
                    _config[parts[0].ToLower()] = parts[1];
                }
            }
        }

        private string HandleConfig(List<string> parts)
        {  
            
            var cmd = parts[1].ToUpper();

            if (cmd == "GET" && parts.Count == 3) 
            {
                var key = parts[2].ToLower();
                return _config.TryGetValue(key, out var value)
                    ? $"{key}\n{value}"
                    : "(nil)";
            }

            if (cmd == "SET" && parts.Count == 4) 
            {
                var key = parts[2].ToLower();
                var value = parts[3];
                _config[key] = value;

                if (key == "appendonly"){
                    _appendOnlyEnabled = value.ToLower() == "yes";
                }
                SaveConfig();
                return "OK";
            }

            return "ERR invalid CONFIG syntax";
            
        }

        private void SaveConfig() 
        {
            var lines = _config.Select(kvp => $"{kvp.Key} {kvp.Value}");
            File.WriteAllLines("zedis.conf", lines);
        }

        
    }
}