using System;
using System.IO;
using System.Security.Cryptography;
using Gml.Web.Api.Domains.System;

namespace Gml.Common
{
    public static class SystemHelper
    {
        public static string CalculateFileHash(string filePath, HashAlgorithm algorithm)
        {
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var hashBytes = algorithm.ComputeHash(fileStream);

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        public static string GetStringOsType(OsType osType)
        {
            return osType switch
            {
                OsType.Undefined => throw new PlatformNotSupportedException(),
                OsType.Linux => "linux",
                OsType.OsX => "osx",
                OsType.Windows => "windows",
                _ => throw new ArgumentOutOfRangeException(nameof(osType), osType, null)
            };
        }
    }
}
