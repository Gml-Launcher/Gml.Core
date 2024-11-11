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
            using var file = File.OpenRead(filePath);

            var binaryHash = algorithm.ComputeHash(file);
            return BitConverter.ToString(binaryHash).Replace("-", "").ToLowerInvariant();
        }

        public static string GetStringOsType(OsType osType)
        {
            switch (osType)
            {
                case OsType.Undefined:
                    throw new PlatformNotSupportedException();
                case OsType.Linux:
                    return "linux";
                case OsType.OsX:
                    return "osx";
                case OsType.Windows:
                    return "windows";
                default:
                    throw new ArgumentOutOfRangeException(nameof(osType), osType, null);
            }
        }
    }
}
