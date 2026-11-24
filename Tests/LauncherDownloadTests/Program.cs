using System.Net;
using System.Net.Sockets;
using System.Text;
using Launcher;

// Exercise real HTTP transport on loopback; no live patch server or credentials.
await Scenario(["HTTP/1.1 503 Service Unavailable\r\nContent-Length: 0\r\nConnection: close\r\n\r\n",
    "HTTP/1.1 200 OK\r\nContent-Length: 4\r\nConnection: close\r\n\r\nlist"], true);
await Scenario(["HTTP/1.1 200 OK\r\nContent-Length: 20\r\nConnection: close\r\n\r\nshort",
    "HTTP/1.1 200 OK\r\nContent-Length: 4\r\nConnection: close\r\n\r\nlist"], true);
await Scenario(["HTTP/1.1 404 Not Found\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"], false);
await Scenario(Enumerable.Repeat("HTTP/1.1 503 Service Unavailable\r\nContent-Length: 0\r\nConnection: close\r\n\r\n", 4).ToArray(), false);
Console.WriteLine("Launcher HTTP retry, truncation and permanent failure checks passed.");

static async Task Scenario(string[] responses, bool success)
{
    using var listener = new TcpListener(IPAddress.Loopback, 0);
    listener.Start();
    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(25));
    var server = Serve();
    using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    var uri = new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}/PList.gz");
    bool completed = false;
    try
    {
        byte[] data = await PatchDownload.FetchAsync(client, uri, cancellationToken: deadline.Token);
        if (!data.SequenceEqual("list"u8.ToArray())) throw new Exception("Unexpected patch payload.");
        completed = true;
    }
    catch (HttpRequestException) when (!success) { }
    if (completed != success) throw new Exception("Unexpected launcher download outcome.");
    await server;

    async Task Serve()
    {
        foreach (string response in responses)
        {
            using var connection = await listener.AcceptTcpClientAsync(deadline.Token);
            using var stream = connection.GetStream();
            using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
            string? line;
            do { line = await reader.ReadLineAsync(deadline.Token); } while (!string.IsNullOrEmpty(line));
            await stream.WriteAsync(Encoding.ASCII.GetBytes(response), deadline.Token);
        }
    }
}
