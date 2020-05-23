using System.Management;

namespace NCompileBench
{
    public static class ManagementObjectExtensions
    {
        public static T GetValue<T>(this ManagementObject data, string propertyName)
        {
            var val = data[propertyName];
            if (val == null)
            {
                return default;
            }

            return (T) val;
        }
    }
}
