using System;
using System.Collections.Generic;

namespace PacketCapture.Models
{
    public class TrafficData
    {
        public string Date { get; set; }
        public TrafficSummary TrafficTotal { get; set; } = new();
        public List<TrafficEntry> In { get; set; } = new();
        public List<TrafficEntry> Out { get; set; } = new();
    }

    public class TrafficSummary
    {
        public int In { get; set; }
        public int Out { get; set; }
    }

    public class TrafficEntry
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public string Protocol { get; set; }
        public int Packets { get; set; }
        public string LastActivity { get; set; }
    }
}
