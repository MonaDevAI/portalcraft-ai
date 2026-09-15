using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ContextIq.RepoTool;

public static class DashboardServer
{
    public static void Run(string generatedDirectory, int port)
    {
        var dashboard = Path.Combine(
            Path.GetFullPath(generatedDirectory),
            "dashboard",
            "index.html");
        if (!File.Exists(dashboard))
        {
            throw new FileNotFoundException(
                "Generated dashboard not found. Run the generate command first.",
                dashboard);
        }

        var html = File.ReadAllBytes(dashboard);
        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        Console.WriteLine($"Context IQ dashboard: http://127.0.0.1:{port}");
        Console.WriteLine("Press Ctrl+C to stop.");

        while (true)
        {
            using var client = listener.AcceptTcpClient();
            try
            {
                using var stream = client.GetStream();
                ReadRequestHeaders(stream);
                var headers = Encoding.ASCII.GetBytes(
                    "HTTP/1.1 200 OK\r\n" +
                    "Content-Type: text/html; charset=utf-8\r\n" +
                    $"Content-Length: {html.Length}\r\n" +
                    "Cache-Control: no-store\r\n" +
                    "Connection: close\r\n\r\n");
                stream.Write(headers);
                stream.Write(html);
            }
            catch (IOException exception) when (IsClientDisconnect(exception))
            {
                // Browsers can close speculative or superseded requests before the response completes.
            }
        }
    }

    private static bool IsClientDisconnect(IOException exception)
    {
        return exception.InnerException is SocketException socketException
            && socketException.SocketErrorCode is SocketError.ConnectionAborted
                or SocketError.ConnectionReset
                or SocketError.Shutdown;
    }

    private static void ReadRequestHeaders(NetworkStream stream)
    {
        var state = 0;
        while (state < 4)
        {
            var value = stream.ReadByte();
            if (value < 0)
            {
                break;
            }
            state = (state, value) switch
            {
                (0, 13) => 1,
                (1, 10) => 2,
                (2, 13) => 3,
                (3, 10) => 4,
                (_, 13) => 1,
                _ => 0
            };
        }
    }
}
