using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace AnserU2_cSharp
{
    /// <summary>
    /// TCP driver cho máy in Anser SmartU2.
    /// Kết nối qua TCP thay COM port, tự động reconnect khi mất kết nối.
    /// Protocol binary: STX(0x02) ... ETX(0x03), có checksum.
    /// </summary>
    public class AnserU2TcpDriver : IDisposable
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }

        /// <summary>Fired khi nhận được 1 frame hoàn chỉnh (STX...ETX).</summary>
        public event Action<byte[]> DataReceived;

        /// <summary>Fired khi trạng thái kết nối thay đổi. Value: "Connected" | "Disconnected" | "Error: ..."</summary>
        public event Action<string> ConnectionStatusChanged;

        public string ConnectionStatus { get; private set; } = "Disconnected";
        public bool IsConnected => _client?.Connected == true && _stream != null;

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _cts;
        private Task _receiveTask;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
        private bool _disposed;

        private const int ConnectTimeoutMs = 5000;
        private const int ReconnectDelayMs = 5000;
        private const int ReceiveBufferSize = 1024;

        public void Connect()
        {
            if (_cts != null && !_cts.IsCancellationRequested) return;

            _cts = new CancellationTokenSource();
            _receiveTask = Task.Run(() => ConnectAndReceiveLoopAsync(_cts.Token));
        }

        private async Task ConnectAndReceiveLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _client?.Dispose();
                    _client = new TcpClient();

                    var connectTask = _client.ConnectAsync(IpAddress, Port);
                    if (await Task.WhenAny(connectTask, Task.Delay(ConnectTimeoutMs, ct)) != connectTask)
                    {
                        if (ct.IsCancellationRequested) break;
                        throw new TimeoutException($"TCP connect timeout ({ConnectTimeoutMs}ms) to {IpAddress}:{Port}");
                    }
                    await connectTask;

                    _stream = _client.GetStream();
                    ConnectionStatus = "Connected";
                    ConnectionStatusChanged?.Invoke("Connected");

                    await ReceiveLoopAsync(ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    ConnectionStatus = $"Error: {ex.Message}";
                    ConnectionStatusChanged?.Invoke(ConnectionStatus);
                }

                if (ct.IsCancellationRequested) break;

                ConnectionStatus = "Disconnected";
                ConnectionStatusChanged?.Invoke("Disconnected");

                try { await Task.Delay(ReconnectDelayMs, ct); }
                catch (OperationCanceledException) { break; }
            }

            ConnectionStatus = "Disconnected";
            ConnectionStatusChanged?.Invoke("Disconnected");
        }

        private async Task ReceiveLoopAsync(CancellationToken ct)
        {
            var buffer = new byte[ReceiveBufferSize];
            var frameBuffer = new List<byte>(64);
            bool inFrame = false;

            while (!ct.IsCancellationRequested)
            {
                int read;
                try
                {
                    read = await _stream.ReadAsync(buffer, 0, buffer.Length, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch { break; }

                if (read == 0) break;

                for (int i = 0; i < read; i++)
                {
                    byte b = buffer[i];

                    if (b == 0x02) // STX
                    {
                        frameBuffer.Clear();
                        frameBuffer.Add(b);
                        inFrame = true;
                    }
                    else if (b == 0x03 && inFrame) // ETX
                    {
                        frameBuffer.Add(b);
                        DataReceived?.Invoke(frameBuffer.ToArray());
                        frameBuffer.Clear();
                        inFrame = false;
                    }
                    else if (inFrame)
                    {
                        frameBuffer.Add(b);
                    }
                }
            }
        }

        /// <summary>Ghi dữ liệu xuống máy in. Thread-safe.</summary>
        public void Write(byte[] data)
        {
            if (data == null || data.Length == 0 || !IsConnected) return;

            _writeLock.Wait();
            try
            {
                _stream.Write(data, 0, data.Length);
                _stream.Flush();
            }
            catch { }
            finally
            {
                _writeLock.Release();
            }
        }

        public void Disconnect()
        {
            _cts?.Cancel();
            try { _receiveTask?.Wait(3000); } catch { }
            _client?.Dispose();
            _client = null;
            _stream = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Disconnect();
                _writeLock?.Dispose();
                _cts?.Dispose();
            }
        }
    }
}
