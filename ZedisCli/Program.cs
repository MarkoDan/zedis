using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

public class Program
{
    const string LOCALHOST = "127.0.0.1";
    const int PORT = 6444;

    public static void Main(string[] args)
    {
        var client = new TcpClient();
        client.Connect(LOCALHOST, PORT);

        var stream = client.GetStream();
        var writer = new StreamWriter(stream) { AutoFlush = true };
        var reader = new StreamReader(stream);

        while (true)
        {
            Console.Write("zedis-cli> ");
            var command = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(command))
                continue;

            if (command.ToLower() == "exit")
                break;

            var result = ToRESP(command);
            writer.Write(result);
            writer.Flush();

            if (command.Trim().StartsWith("SUBSCRIBE", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Subscribed. Waiting for messages... (Press Ctrl+C to exit)");

                while (true)
                {
                    var response = ParseRESPResponse(reader);
                    if (response == null)
                    {
                        Console.WriteLine("Connection closed by server.");
                        break;
                    }

                    Console.WriteLine(response);
                }

                break; // Exit after SUBSCRIBE loop (optional)
            }
            else
            {
                var response = ParseRESPResponse(reader);
                if (response == null)
                {
                    Console.WriteLine("Connection closed by server.");
                    break;
                }

                Console.WriteLine(response);

                if (command.Trim().ToUpper() == "QUIT" && response == "OK")
                    break;
            }
        }

        writer.Close();
        reader.Close();
        client.Close();
    }


    public static string ToRESP(string command) 
    {
       var parts = command.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
       var sb = new StringWriter();

       sb.WriteLine($"*{parts.Length}");
       foreach (var part in parts) 
       {
            sb.WriteLine($"${part.Length}");
            sb.WriteLine(part);    
       }

       return sb.ToString();
    }

    public static string? ParseRESPResponse(StreamReader reader)
    {
        string? line = reader.ReadLine();
        if (line == null) return null;

        switch (line[0])
        {
            case '+': return line.Substring(1);
            case ':': return line.Substring(1);
            case '-': return "ERR " + line.Substring(1);
            case '$':
                if (line == "$-1") return "(nil)";
                if (int.TryParse(line.Substring(1), out int dataLen))
                {
                    string? data = reader.ReadLine();
                    return data ?? "(nil)";
                }
                return "(invalid bulk)";
            case '*':
                if (!int.TryParse(line.Substring(1), out int count))
                {
                    return "(invalid array)";
                }
                var results = new List<string>();
                for (int i = 0; i < count; i++) 
                {
                    var typeLine = reader.ReadLine();
                    if (typeLine == null) return "(incomplete array)";
                    if (typeLine == "$-1")
                    {
                        results.Add($"{i + 1}) (nil)");
                    }
                    else if (typeLine.StartsWith("$") && int.TryParse(typeLine.Substring(1), out int len))
                    {
                        string? val = reader.ReadLine();
                        results.Add($"{i + 1}) {(val ?? "(nil)")}");
                    }

                }
                return string.Join("\n", results);
            default:
                return "(unknown RESP format)";
        }

    }

    public static List<string> ParseCommandLine(string input) 
    {
        var matches = Regex.Matches(input, @"[\""].+?[\""]|[^ ]+");
        return matches.Select(m => m.Value.Trim('"')).ToList();
    }

}
