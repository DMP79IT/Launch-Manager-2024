using System;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LaunchManager.Models
{
    [Serializable]
    public class AppEntry
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Path { get; set; }
        public string Arguments { get; set; }
        public string Mode { get; set; } = "LM"; // "LM" o "MSFS"
        public string Timing { get; set; } = "After";
        public int DelaySeconds { get; set; } = 0;
        public bool StartMinimized { get; set; } = false;
        public int StartMinimizedDelaySeconds { get; set; } = 0;
        public bool CloseWindow { get; set; } = false;
        public int CloseWindowDelaySeconds { get; set; } = 0;
        public bool CloseMSFS { get; set; } = false;
        public bool Active { get; set; } = true;
    }
}
