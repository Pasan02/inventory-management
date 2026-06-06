using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace inventory_management.Services
{
    public class MobileCameraService : IMobileCameraService, IDisposable
    {
        private HttpListener? _listener;
        private bool _isRunning;
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
                    var endPoint = socket.LocalEndPoint as IPEndPoint;
                    return endPoint?.Address.ToString() ?? "localhost";
                }
            }
            catch
            {
                // Fallback to local machine hostname lookup
                try
                {
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.ToString();
                        }
                    }
                }
                catch
                {
                    // Ignore
                }
                return "localhost";
            }
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
                _listener = new HttpListener();
                var localIp = GetLocalIpAddress();
                
                // Add prefixes for local IP, localhost, and 127.0.0.1
                _listener.Prefixes.Add($"http://localhost:{Port}/");
                _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
                
                if (localIp != "localhost")
                {
                    _listener.Prefixes.Add($"http://{localIp}:{Port}/");
                }

                _listener.Start();
                _isRunning = true;
                
                // Start background listening task
                Task.Run(() => ListenAsync());
                System.Diagnostics.Debug.WriteLine($"MobileCameraService started at: {GetMobileCaptureUrl()}");
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
                if (_listener != null && _listener.IsListening)
                {
                    _listener.Stop();
                    _listener.Close();
                }
                _listener = null;
                System.Diagnostics.Debug.WriteLine("MobileCameraService stopped.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping MobileCameraService: {ex.Message}");
            }
        }

        private async Task ListenAsync()
        {
            while (_isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => HandleRequestAsync(context));
                }
                catch (HttpListenerException)
                {
                    // Listening stopped, normal exit
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error receiving request: {ex.Message}");
                }
            }
        }

        private async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Add CORS headers so mobile phone can connect without issues
                response.AddHeader("Access-Control-Allow-Origin", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
                response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = (int)HttpStatusCode.OK;
                    response.Close();
                    return;
                }

                if (request.HttpMethod == "GET")
                {
                    // Serve the camera capture page
                    var html = GetHtmlContent();
                    var buffer = Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html; charset=utf-8";
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    response.StatusCode = (int)HttpStatusCode.OK;
                }
                else if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/upload")
                {
                    // Read raw binary image bytes directly from request stream
                    using (var ms = new MemoryStream())
                    {
                        await request.InputStream.CopyToAsync(ms);
                        var imageBytes = ms.ToArray();
                        
                        if (imageBytes.Length > 0)
                        {
                            // Trigger the event to the WPF UI thread
                            ImageReceived?.Invoke(imageBytes);
                            response.StatusCode = (int)HttpStatusCode.OK;
                            
                            var statusBuffer = Encoding.UTF8.GetBytes("Success");
                            await response.OutputStream.WriteAsync(statusBuffer, 0, statusBuffer.Length);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.BadRequest;
                        }
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling HTTP request: {ex.Message}");
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                try
                {
                    response.Close();
                }
                catch
                {
                    // Ignore
                }
            }
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
