using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

class Program
{
    static void Main(string[] args)
    {
        ServerConfiguration serverConfig = LoadServerConfiguration();
        WebServer server = new WebServer(serverConfig);

        try
        {
            server.Start();
            Console.WriteLine("Press Enter to stop the server.");
            Console.ReadLine();
        }
        finally
        {
            server.Stop();
        }
    }

    static ServerConfiguration LoadServerConfiguration()
    {
        string baseUrl = "http://localhost:8080/";
        string rootDirectory = Path.Combine(Directory.GetCurrentDirectory(), "static");

        if (!Directory.Exists(rootDirectory))
        {
            Directory.CreateDirectory(rootDirectory);
        }

        return new ServerConfiguration(baseUrl, rootDirectory);
    }
}


class ServerConfiguration
{
    public string BaseUrl { get; set; }
    public string RootDirectory { get; set; }

    public ServerConfiguration(string baseUrl, string rootDirectory)
    {
        BaseUrl = baseUrl;
        RootDirectory = rootDirectory;
    }
}

class WebServer
{
    private HttpListener listener;
    private ServerConfiguration serverConfig;

    public WebServer(ServerConfiguration config)
    {
        serverConfig = config;
        listener = new HttpListener();
        listener.Prefixes.Add(serverConfig.BaseUrl);
    }

    public void Start()
    {
        try
        {
            listener.Start();
            Console.WriteLine("Server is listening at " + serverConfig.BaseUrl);

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                ThreadPool.QueueUserWorkItem((ctx) =>
                {
                    HandleRequest((HttpListenerContext)ctx, serverConfig.RootDirectory);
                }, context);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }
    }

    public void Stop()
    {
        if (listener != null && listener.IsListening)
        {
            listener.Stop();
            listener.Close();
        }
    }

    private void HandleRequest(HttpListenerContext context, string rootDirectory)
      {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        try
        {
            string requestedPath = request.Url.LocalPath;
            string filePath = Path.Combine(rootDirectory, requestedPath.TrimStart('/'));

            if (File.Exists(filePath))
            {
                byte[] buffer = File.ReadAllBytes(filePath);
                response.ContentType = GetContentType(filePath);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                string errorMessage = "404 (Not Found): " + requestedPath;
                byte[] errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                response.ContentType = "text/plain";
                response.ContentLength64 = errorBytes.Length;
                response.OutputStream.Write(errorBytes, 0, errorBytes.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error handling request: " + ex.Message);
            response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {
            response.Close();
        }
    }
    
    private string GetContentType(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLower();

        switch (ext)
        {
            case ".html":
                return "text/html";
            case ".css":
                return "text/css";
            case ".js":
                return "text/javascript";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".png":
                return "image/png";
            default:
                return "application/octet-stream";
        }
    }
}
