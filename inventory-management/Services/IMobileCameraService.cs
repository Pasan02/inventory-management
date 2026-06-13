using System;

namespace inventory_management.Services
{
    public interface IMobileCameraService
    {
        event Action<byte[]>? ImageReceived;
        void Start();
        void Stop();
        string GetLocalIpAddress();
        string GetMobileCaptureUrl();
        bool IsRunning { get; }
    }
}
