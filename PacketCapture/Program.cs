using System;
using System.Linq;
using SharpPcap;
using SharpPcap.LibPcap;
using PacketDotNet;
using PacketCapture.Models;
using System.Net.Sockets;
using System.Net;

namespace PacketCapture
{
    class Program
    {
        private readonly int[] _targetPorts = { 80, 13000, 3389, 5900 };
        private TrafficData _trafficData;
        private Logger _logger;
        private Timer _saveTimer;
        private Dictionary<int, DateTime> _lastPopupTimes = new Dictionary<int, DateTime>();


        private void StartAutoSaveTimer()
        {
            _saveTimer = new Timer(state =>
            {
                _logger.SaveToJson(_trafficData);
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
        }

        public void Run()
        {
            string logPath = Logger.GetLogFilePath();
            _logger = new Logger(logPath);
            _trafficData = _logger.LoadFromJson();
            StartAutoSaveTimer();

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine($" [{DateTime.Now:HH:mm:ss}] Сохраняю перед выходом...");
                _logger.SaveToJson(_trafficData);
            };

            var devices = CaptureDeviceList.Instance;
            if (devices.Count < 1)
            {
                Console.WriteLine("Нет доступных сетевых устройств.");
                return;
            }

            var preferredDevice = devices
                .FirstOrDefault(d => d.Description.Contains("Realtek PCIe GBE Family Controller"))
                ?? devices[0];

 

            var device = preferredDevice as LibPcapLiveDevice;
            if (device == null)
            {
                Console.WriteLine("Выбранное устройство не поддерживает захват в промискуитетном режиме.");
                return;
            }

            device.OnPacketArrival += (sender, e) =>
            {
                var rawPacket = e.GetPacket();
                if (rawPacket?.Data == null) return;

                var packet = Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                var ipPacket = packet.Extract<IPv4Packet>();
                if (ipPacket == null) return;

                var tcpPacket = packet.Extract<TcpPacket>();
                var udpPacket = packet.Extract<UdpPacket>();


                var srcIP = ipPacket.SourceAddress.ToString();
                var dstIP = ipPacket.DestinationAddress.ToString();

                var srcHost = Utils.GetHostOrIP(srcIP);
                var dstHost = Utils.GetHostOrIP(dstIP);

                int srcPort = tcpPacket?.SourcePort ?? udpPacket?.SourcePort ?? 0;
                int dstPort = tcpPacket?.DestinationPort ?? udpPacket?.DestinationPort ?? 0;

                bool isIncoming = Utils.IsMyIp(dstIP);
                string arrow = isIncoming ? "<-in-" : "-out->";

                if (!Utils.IsMyIp(dstIP) && !Utils.IsMyIp(srcIP))
                {
                    return;
                }

                //Console.WriteLine(
                //    $"[{DateTime.Now:HH:mm:ss}]  {srcIP}-{srcHost}:{srcPort} {arrow} {dstIP}-{dstHost}:{dstPort} протокол {ipPacket.Protocol} пакет длина {rawPacket.Data.Length}");



                if (isIncoming)
                {

                    var Source = !string.IsNullOrEmpty(srcHost) ? $"{srcHost}:{srcPort}" : $"{srcIP}:{srcPort}";
                    var Destination = !string.IsNullOrEmpty(dstHost) ? $"{dstHost}" : $"{dstIP}:{dstPort}";

                    var existing = _trafficData.In.FirstOrDefault(p =>
                        p.Source == Source &&
                        p.Destination == Destination &&
                        p.Protocol == ipPacket.Protocol.ToString()
                    );

                    if (existing != null)
                    {
                        existing.Packets += 1;
                        existing.LastActivity = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        _trafficData.In.Add(new TrafficEntry
                        {
                            Source = Source,
                            Destination = Destination,
                            Protocol = ipPacket.Protocol.ToString(),
                            Packets = 1,
                            LastActivity = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }

                    _trafficData.TrafficTotal.In += rawPacket.Data.Length;
                }
                else
                {
                    var Source = !string.IsNullOrEmpty(srcHost) ? $"{srcHost}" : $"{srcIP}:{srcPort}";
                    var Destination = !string.IsNullOrEmpty(dstHost) ? $"{dstHost}:{dstPort}" : $"{dstIP}:{dstPort}";
                    var existing = _trafficData.Out.FirstOrDefault(p =>
                        p.Source == Source &&
                        p.Destination == Destination &&
                        p.Protocol == ipPacket.Protocol.ToString()
                    );

                    if (existing != null)
                    {
                        existing.Packets += 1;
                        existing.LastActivity = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    else
                    {
                        _trafficData.Out.Add(new TrafficEntry
                        {
                            Source = Source,
                            Destination = Destination,
                            Protocol = ipPacket.Protocol.ToString(),
                            Packets = 1,
                            LastActivity = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        });
                    }

                    _trafficData.TrafficTotal.Out += rawPacket.Data.Length;
                }


                if (isIncoming && _targetPorts.Contains(dstPort))
                {
                    var now = DateTime.Now;

                    if (!_lastPopupTimes.ContainsKey(dstPort) || (now - _lastPopupTimes[dstPort]).TotalMinutes >= 1)
                    {
                        _lastPopupTimes[dstPort] = now;
                        Utils.ShowPopup($"[{DateTime.Now:HH:mm:ss}] Входящий пакет от {srcIP} на порт{dstPort}");
                    }
                }
            };

            try
            {
                device.Open(DeviceModes.Promiscuous, 1000);
                Console.WriteLine("Захват запущен. Нажмите Enter для остановки...");



                device.StartCapture();
                Console.ReadLine();
                device.StopCapture();
                device.Close();
                Console.WriteLine($" [{DateTime.Now:HH:mm:ss}] Захват остановлен.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            var program = new Program();
            program.Run();
        }
    }
}
