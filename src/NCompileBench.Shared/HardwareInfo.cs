using System.Collections.Generic;

namespace NCompileBench.Shared
{
    public class HardwareInfo
    {
        public string SystemFamily { get; set; }
        public string SystemSku { get; set; }
        public string Manufacturer { get; set; }
        public string Model { get; set; }
        public OsInfo OS { get; set; }
        public CpuInfo Cpu { get; set; }
        public string MotherBoard { get; set; }
        public ulong Memory { get; set; }
        public List<DriveInfo> Drives { get; set; }

        public class DriveInfo
        {
            public string Name { get; set; }
            public ulong Size { get; set; }
        }

        public class CpuInfo
        {
            public string Name { get; set; }
            public uint Count { get; set; }
            public uint SpeedMHz { get; set; }
            public uint NumberOfCores { get; set; }
            public uint NumberOfLogicalProcessors { get; set; }
            public string Architecture { get; set; }
        }

        public class OsInfo
        {
            public string Name { get; set; }
            public string Version { get; set; }
            public string BuildNumber { get; set; }
            public string Architecture { get; set; }
        }
    }
}
