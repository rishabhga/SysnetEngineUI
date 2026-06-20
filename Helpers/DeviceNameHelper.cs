using System;
using System.Linq;

namespace ManageEngineWebApp.Helpers
{

    public static class DeviceNameHelper
    {
        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            var value = raw.Trim();
            if (value.Contains('\\'))
                value = value.Split('\\').Last();
            if (value.Contains('.'))
                value = value.Split('.')[0];

            return value.ToUpperInvariant();
        }
    }
}