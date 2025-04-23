using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.Sockets;

public class Program
{
    const string LOCALHOST = "127.0.0.1";
    const int PORT = 6555;

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
            var response = ParseRESPResponse(reader);
            Console.WriteLine(response);
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

    public static string ParseRESPResponse(StreamReader reader) 
    {
        string? line = reader.ReadLine();
        if (line == null) return "(no response)";

        switch (line[0]) 
        {
            case '+': return line.Substring(1);
            case ':': return line.Substring(1);
            case '-': return "ERR " + line.Substring(1);
            case '$':
                if (line == "$-1") return "(nil)";
                if (int.TryParse(line.Substring(1), out int len)) 
                {
                    string? data = reader.ReadLine();
                    return data ?? "(nil)";
                }
                return "(invalid bulk)";
            default:
                return "(unknown RESP format)";
        }
    }
}
