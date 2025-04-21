using System;
using System.IO;
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

            writer.WriteLine(command);
            var response = reader.ReadLine();
            Console.WriteLine(response);
        }

        writer.Close();
        reader.Close();
        client.Close();
    }
}
