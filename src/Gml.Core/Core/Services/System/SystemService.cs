using System;
using System.Runtime.InteropServices;

namespace Gml.Core.Services.System;

public class SystemService
{

    public static string GetPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "windows";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "linux";
        }

        throw new PlatformNotSupportedException("This platform is not supported.");
    }

}
