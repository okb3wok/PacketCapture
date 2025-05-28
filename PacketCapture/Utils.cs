using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace PacketCapture
{
    public static class Utils
    {


        public static bool IsMyIp(string IP)
        {
            var ipList = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList
                .Where(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Select(ip => ip.ToString())
                .ToList();

            return ipList.Contains(IP);
        }


        public static bool IsLocalAddress(string ip)
        {
            return ip.StartsWith("192.168.") ||
                   ip.StartsWith("10.") ||
                   ip.StartsWith("172.16.") ||
                   ip.StartsWith("127.") ||
                   ip.StartsWith("169.254.");
        }

        public static string GetHostOrIP(string ip)
        {
            try
            {
                var hostEntry = Dns.GetHostEntry(ip);
                return hostEntry.HostName;
            }
            catch (SocketException)
            {
                return ip; // если имя не разрешилось, вернуть сам IP
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при разрешении имени для {ip}: {ex.Message}");
                return ip;
            }
        }

        public static void ShowPopup(string message)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"Add-Type -AssemblyName PresentationFramework;[System.Windows.MessageBox]::Show('{message}')\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Не удалось показать окно: " + ex.Message);
            }
        }
    }
}
