using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using NCompileBench.Shared;

namespace NCompileBench
{
    public class HardwareInfoProvider
    {
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
            result.Drives = new List<HardwareInfo.DriveInfo>();

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
                var drive = new HardwareInfo.DriveInfo { Name = managementObject.TryGetValue<string>("Caption"), Size = managementObject.TryGetValue<ulong>("Size") };

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

            result.MotherBoard = baseBoardData.TryGetValue<string>("Product");
        }

        private static void SetOsInfo(HardwareInfo result)
        {
            var osData =
                new ManagementObjectSearcher("select * from Win32_OperatingSystem")
                    .Get()
                    .Cast<ManagementObject>()
                    .First();

            result.OS = new HardwareInfo.OsInfo()
            {
                Name = osData.TryGetValue<string>("Caption"),
                Version = osData.TryGetValue<string>("Version"),
                BuildNumber = osData.TryGetValue<string>("BuildNumber"),
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

            result.Cpu.Count = csData.TryGetValue<uint>("NumberOfProcessors");
            result.SystemFamily = csData.TryGetValue<string>("SystemFamily");
            result.SystemSku = csData.TryGetValue<string>("SystemSKUNumber");
            result.Memory = csData.TryGetValue<ulong>("TotalPhysicalMemory");
            result.Manufacturer = csData.TryGetValue<string>("Manufacturer");
            result.Model = csData.TryGetValue<string>("Model");

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

            var name = cpuData.TryGetValue<string>("Name")?.Trim();
            var speedMHz = cpuData.TryGetValue<uint>("MaxClockSpeed");
            var cores = cpuData.TryGetValue<uint>("NumberOfCores");
            var threads = cpuData.TryGetValue<uint>("NumberOfLogicalProcessors");

            var cpu = new HardwareInfo.CpuInfo()
            {
                Name = name,
                SpeedMHz = speedMHz,
                NumberOfCores = cores,
                NumberOfLogicalProcessors = threads,
                Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString()
            };

            result.Cpu = cpu;
        }
    }
}
