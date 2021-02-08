using System;
using System.Management;

namespace NCompileBench
{
    public static class ManagementObjectExtensions
    {
        public static T GetValue<T>(this ManagementObject managementObject, string propertyName)
        {
            var val = managementObject[propertyName];
            if (val == null)
            {
                return default;
            }

            return (T) val;
        }

        public static T TryGetValue<T>(this ManagementObject managementObject, string propertyName)
        {
            try
            {
                return managementObject.GetValue<T>(propertyName);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}
