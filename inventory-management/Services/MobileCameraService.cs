using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class MobileCameraService : IMobileCameraService, IDisposable
    {
        private TcpListener? _listener;
        private bool _isRunning;
        private CancellationTokenSource? _cts;
        private const int Port = 5050;

        public event Action<byte[]>? ImageReceived;

        public bool IsRunning => _isRunning;

        public string GetLocalIpAddress()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    if (socket.LocalEndPoint is IPEndPoint endPoint)
                    {
                        var ipStr = endPoint.Address.ToString();
                        if (!ipStr.StartsWith("127.") && ipStr != "0.0.0.0")
                        {
                            return ipStr;
                        }
                    }
                }
            }
            catch { }

            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up) continue;
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                    
                    var name = ni.Name.ToLower();
                    var desc = ni.Description.ToLower();
                    if (name.Contains("virtual") || name.Contains("vethernet") || name.Contains("wsl") || name.Contains("loopback") || name.Contains("pseudo") ||
                        desc.Contains("virtual") || desc.Contains("vmware") || desc.Contains("virtualbox") || desc.Contains("host-only") || desc.Contains("vpn") || desc.Contains("wireguard"))
                    {
                        continue;
                    }

                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            var ipStr = addr.Address.ToString();
                            if (ipStr.StartsWith("192.168.") || ipStr.StartsWith("10.") || ipStr.StartsWith("172."))
                            {
                                return ipStr;
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    {
                        return ip.ToString();
                    }
                }
            }
            catch { }

            return "localhost";
        }

        public string GetMobileCaptureUrl()
        {
            var ip = GetLocalIpAddress();
            return $"http://{ip}:{Port}";
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                _cts = new CancellationTokenSource();
                // Listen on IPAddress.Any (0.0.0.0) so it binds to all network interfaces without UAC restriction
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                _isRunning = true;

                // Start background listening task
                var token = _cts.Token;
                Task.Run(() => ListenAsync(token), token);
                System.Diagnostics.Debug.WriteLine($"MobileCameraService started on port {Port} at: {GetMobileCaptureUrl()}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to start MobileCameraService: {ex.Message}");
                _isRunning = false;
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            try
            {
                _isRunning = false;
                _cts?.Cancel();
                _listener?.Stop();
                _listener = null;
                System.Diagnostics.Debug.WriteLine("MobileCameraService stopped.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping MobileCameraService: {ex.Message}");
            }
        }

        private async Task ListenAsync(CancellationToken token)
        {
            while (_isRunning && _listener != null)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync(token);
                    _ = Task.Run(() => HandleClientAsync(client), token);
                }
                catch (ObjectDisposedException)
                {
                    break; // Listener stopped, normal exit
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error receiving connection: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            {
                try
                {
                    stream.ReadTimeout = 10000; // 10 seconds timeout

                    // Read request headers
                    var headerBytes = new List<byte>();
                    int b;
                    bool doubleCrlfFound = false;

                    while ((b = stream.ReadByte()) != -1)
                    {
                        headerBytes.Add((byte)b);
                        if (headerBytes.Count >= 4 &&
                            headerBytes[headerBytes.Count - 4] == '\r' &&
                            headerBytes[headerBytes.Count - 3] == '\n' &&
                            headerBytes[headerBytes.Count - 2] == '\r' &&
                            headerBytes[headerBytes.Count - 1] == '\n')
                        {
                            doubleCrlfFound = true;
                            break;
                        }

                        // Prevent buffer overflow or oversized headers
                        if (headerBytes.Count > 8192) break;
                    }

                    if (!doubleCrlfFound) return;

                    var headerString = Encoding.UTF8.GetString(headerBytes.ToArray());
                    var headerLines = headerString.Split(new[] { "\r\n" }, StringSplitOptions.None);
                    if (headerLines.Length == 0) return;

                    var requestLine = headerLines[0].Split(' ');
                    if (requestLine.Length < 2) return;

                    string method = requestLine[0].ToUpper();
                    string path = requestLine[1];

                    // Find Content-Length
                    int contentLength = 0;
                    foreach (var line in headerLines)
                    {
                        if (line.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase))
                        {
                            int.TryParse(line.Substring("Content-Length:".Length).Trim(), out contentLength);
                        }
                    }

                    if (method == "OPTIONS")
                    {
                        await SendResponseAsync(stream, "", "text/plain", 200);
                        return;
                    }

                    if (method == "GET")
                    {
                        var html = GetHtmlContent();
                        await SendResponseAsync(stream, html, "text/html; charset=utf-8", 200);
                    }
                    else if (method == "POST" && path == "/upload")
                    {
                        if (contentLength > 0 && contentLength < 15 * 1024 * 1024) // 15MB limit
                        {
                            byte[] bodyBuffer = new byte[contentLength];
                            int bytesRead = 0;
                            while (bytesRead < contentLength)
                            {
                                int read = await stream.ReadAsync(bodyBuffer, bytesRead, contentLength - bytesRead);
                                if (read <= 0) break;
                                bytesRead += read;
                            }

                            if (bytesRead == contentLength)
                            {
                                // Fire event on background or UI thread
                                _ = Task.Run(() => ImageReceived?.Invoke(bodyBuffer));
                                await SendResponseAsync(stream, "Success", "text/plain", 200);
                            }
                            else
                            {
                                await SendResponseAsync(stream, "Incomplete data", "text/plain", 400);
                            }
                        }
                        else
                        {
                            await SendResponseAsync(stream, "Invalid Content-Length", "text/plain", 400);
                        }
                    }
                    else
                    {
                        await SendResponseAsync(stream, "Not Found", "text/plain", 404);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error handling HTTP request: {ex.Message}");
                }
            }
        }

        private async Task SendResponseAsync(NetworkStream stream, string content, string contentType, int statusCode)
        {
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var headerBuilder = new StringBuilder();
            headerBuilder.AppendLine($"HTTP/1.1 {statusCode} {GetStatusDescription(statusCode)}");
            headerBuilder.AppendLine($"Content-Type: {contentType}");
            headerBuilder.AppendLine($"Content-Length: {contentBytes.Length}");
            headerBuilder.AppendLine("Access-Control-Allow-Origin: *");
            headerBuilder.AppendLine("Access-Control-Allow-Methods: GET, POST, OPTIONS");
            headerBuilder.AppendLine("Access-Control-Allow-Headers: Content-Type");
            headerBuilder.AppendLine("Connection: close");
            headerBuilder.AppendLine();

            var headerBytes = Encoding.UTF8.GetBytes(headerBuilder.ToString());
            await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
            if (contentBytes.Length > 0)
            {
                await stream.WriteAsync(contentBytes, 0, contentBytes.Length);
            }
            await stream.FlushAsync();
        }

        private string GetStatusDescription(int code)
        {
            return code switch
            {
                200 => "OK",
                400 => "Bad Request",
                404 => "Not Found",
                _ => "Internal Server Error"
            };
        }

        private string GetHtmlContent()
        {
            return @"<!DOCTYPE html>
<html>
<head>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Alpine Camera Capture</title>
    <style>
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: #0f172a;
            color: #f8fafc;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            min-height: 100vh;
            margin: 0;
            padding: 20px;
            box-sizing: border-box;
        }
        .card {
            background: rgba(30, 41, 59, 0.7);
            backdrop-filter: blur(12px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 24px;
            padding: 30px;
            width: 100%;
            max-width: 400px;
            box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.5);
            text-align: center;
        }
        h1 {
            font-size: 24px;
            margin-top: 0;
            margin-bottom: 10px;
            font-weight: 700;
            background: linear-gradient(to right, #38bdf8, #818cf8);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        p {
            color: #94a3b8;
            font-size: 14px;
            margin-bottom: 30px;
        }
        .btn {
            background: linear-gradient(135deg, #0284c7 0%, #4f46e5 100%);
            color: white;
            border: none;
            border-radius: 16px;
            padding: 16px 24px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            width: 100%;
            box-shadow: 0 10px 15px -3px rgba(79, 70, 229, 0.4);
            transition: transform 0.2s, box-shadow 0.2s;
        }
        .btn:active {
            transform: scale(0.98);
        }
        #preview {
            width: 100%;
            border-radius: 16px;
            margin-top: 25px;
            display: none;
            border: 2px solid rgba(255, 255, 255, 0.1);
            max-height: 250px;
            object-fit: contain;
            background: #020617;
        }
        #status {
            margin-top: 20px;
            font-size: 14px;
            font-weight: 500;
        }
        .success { color: #10b981; }
        .error { color: #ef4444; }
        .uploading { color: #38bdf8; }
    </style>
</head>
<body>
    <div class='card'>
        <h1>Alpine Mobile Camera</h1>
        <p>Take a photo to upload directly to the desktop app.</p>
        <button class='btn' onclick='triggerCamera()'>Take Photo</button>
        <input type='file' id='cameraInput' accept='image/*' capture='camera' style='display:none;' onchange='handlePhoto(event)'>
        <img id='preview' alt='Preview'>
        <div id='status'></div>
    </div>
    <script>
        function triggerCamera() {
            document.getElementById('cameraInput').click();
        }
        function handlePhoto(event) {
            const file = event.target.files[0];
            if (!file) return;

            // Show preview
            const preview = document.getElementById('preview');
            preview.src = URL.createObjectURL(file);
            preview.style.display = 'block';

            // Status
            const statusDiv = document.getElementById('status');
            statusDiv.className = 'uploading';
            statusDiv.innerText = 'Uploading photo to desktop app...';

            // Send raw binary file directly as request body
            fetch('/upload', {
                method: 'POST',
                body: file,
                headers: {
                    'Content-Type': file.type
                }
            })
            .then(response => {
                if (response.ok) {
                    statusDiv.className = 'success';
                    statusDiv.innerText = 'Upload complete! Check your desktop screen to approve.';
                } else {
                    throw new Error('Server returned error');
                }
            })
            .catch(error => {
                statusDiv.className = 'error';
                statusDiv.innerText = 'Upload failed. Check your WiFi connection and try again.';
            });
        }
    </script>
</body>
</html>";
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
