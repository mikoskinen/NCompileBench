using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace NCompileBench
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

        public static HardwareInfo Get()
        {
            var result = new HardwareInfo();
            
            SetCpuInfo(result);
            SetSystemInfo(result);
            SetOsInfo(result);
            SetMotherBoardInfo(result);
            SetDriveInfo(result);

            return result;
        }

        private static void SetDriveInfo(HardwareInfo result)
        {
            result.Drives = new List<DriveInfo>();

            var drives =
                new ManagementObjectSearcher("select * from Win32_DiskDrive")
                    .Get()
                    .Cast<ManagementObject>()
                    .ToList();

            if (drives?.Any() != true)
            {
                return;
            }
            
            foreach (var managementObject in drives)
            {
                var drive = new DriveInfo
                {
                    Name = managementObject.GetValue<string>("Caption"),
                    Size = managementObject.GetValue<ulong>("Size")
                };

                result.Drives.Add(drive);
            }
        }
        
        private static void SetMotherBoardInfo(HardwareInfo result)
        {
            var baseBoardData =
                new ManagementObjectSearcher("select * from Win32_BaseBoard")
                    .Get()
                    .Cast<ManagementObject>()
                    .First();

            result.MotherBoard = baseBoardData.GetValue<string>("Product");
        }

        private static void SetOsInfo(HardwareInfo result)
        {
            var osData =
                new ManagementObjectSearcher("select * from Win32_OperatingSystem")
                    .Get()
                    .Cast<ManagementObject>()
                    .First();

            result.OS = new OsInfo()
            {
                Name = osData.GetValue<string>("Caption"),
                Version = osData.GetValue<string>("Version"),
                BuildNumber = osData.GetValue<string>("BuildNumber"),
                Architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString()
            };
        }

        private static void SetSystemInfo(HardwareInfo result)
        {
            var csData =
                new ManagementObjectSearcher("select * from Win32_ComputerSystem")
                    .Get()
                    .Cast<ManagementObject>()
                    .First();

            result.Cpu.Count = csData.GetValue<uint>("NumberOfProcessors");
            result.SystemFamily = csData.GetValue<string>("SystemFamily");
            result.SystemSku = csData.GetValue<string>("SystemSKUNumber");
            result.Memory = csData.GetValue<ulong>("TotalPhysicalMemory");
            result.Manufacturer = csData.GetValue<string>("Manufacturer");
            result.Model = csData.GetValue<string>("Model");

            if (string.Equals(result.Manufacturer, "lenovo", StringComparison.InvariantCultureIgnoreCase))
            {
                var csp =
                    new ManagementObjectSearcher("select * from Win32_ComputerSystem")
                        .Get()
                        .Cast<ManagementObject>()
                        .First();

                result.SystemFamily = csp.TryGetValue<string>("Name");
                result.SystemSku = csp.TryGetValue<string>("SKUNumber");
            }
        }

        private static void SetCpuInfo(HardwareInfo result)
        {
            var cpuData =
                new ManagementObjectSearcher("select * from Win32_Processor")
                    .Get()
                    .Cast<ManagementObject>()
                    .First();

            var name = cpuData.GetValue<string>("Name")?.Trim();
            var speedMHz = cpuData.GetValue<uint>("MaxClockSpeed");
            var cores = cpuData.GetValue<uint>("NumberOfCores");
            var threads = cpuData.GetValue<uint>("NumberOfLogicalProcessors");

            var cpu = new CpuInfo()
            {
                Name = name, SpeedMHz = speedMHz, NumberOfCores = cores, NumberOfLogicalProcessors = threads, Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
            };

            result.Cpu = cpu;
        }
    }
}
