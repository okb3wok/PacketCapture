using System;
using System.IO;
using Newtonsoft.Json;
using PacketCapture.Models;

namespace PacketCapture
{
    public class Logger
    {
        private readonly string _logFilePath;

        public Logger(string filePath)
        {
            _logFilePath = filePath;
        }

        public void SaveToJson(TrafficData trafficData)
        {
            try
            {
                trafficData.Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string json = JsonConvert.SerializeObject(trafficData, Formatting.Indented);
                File.WriteAllText(_logFilePath, json);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Данные успешно сохранены.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        public TrafficData LoadFromJson()
        {
            if (!File.Exists(_logFilePath))
            {
                return new TrafficData { Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            }

            try
            {
                string json = File.ReadAllText(_logFilePath);
                return JsonConvert.DeserializeObject<TrafficData>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                return new TrafficData { Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") };
            }
        }

        public static string GetLogFilePath()
        {
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);
            return Path.Combine(logsDir, $"{DateTime.Now:yyyy-MM-dd}.json");
        }
    }
}
