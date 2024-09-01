using System.Collections.Generic;

namespace Gml.Core.Helpers.Mirrors;

public class MirrorsHelper
{
    public static Dictionary<string, string[]> JavaMirrors = new()
    {
        {
            "linux", [
                "https://mirror.recloud.tech/openjdk-22_linux-x64_bin.zip",
                "https://mirror.recloud.host/openjdk-22_linux-x64_bin.zip",
                "https://mr-1.recloud.tech/openjdk-22_linux-x64_bin.zip",
                "https://mr-2.recloud.tech/openjdk-22_linux-x64_bin.zip",
                "https://mr-3.recloud.tech/openjdk-22_linux-x64_bin.zip",
            ]
        },
        {
            "windows", [
                "https://mirror.recloud.tech/openjdk-22_windows-x64_bin.zip",
                "https://mirror.recloud.host/openjdk-22_windows-x64_bin.zip",
                "https://mr-1.recloud.tech/openjdk-22_windows-x64_bin.zip",
                "https://mr-2.recloud.tech/openjdk-22_windows-x64_bin.zip",
                "https://mr-3.recloud.tech/openjdk-22_windows-x64_bin.zip",
            ]
        },
    };

    public static Dictionary<string, string[]> DotnetMirrors = new()
    {
        {
            "linux", [
                "https://mirror.recloud.tech/dotnet-sdk-8.0.302-linux-x64.zip",
                "https://mirror.recloud.host/dotnet-sdk-8.0.302-linux-x64.zip",
                "https://mr-1.recloud.tech/dotnet-sdk-8.0.302-linux-x64.zip",
                "https://mr-2.recloud.tech/dotnet-sdk-8.0.302-linux-x64.zip",
                "https://mr-3.recloud.tech/dotnet-sdk-8.0.302-linux-x64.zip",
            ]
        },
        {
            "windows", [
                "https://mirror.recloud.tech/dotnet-sdk-8.0.302-win-x64.zip",
                "https://mirror.recloud.host/dotnet-sdk-8.0.302-win-x64.zip",
                "https://mr-1.recloud.tech/dotnet-sdk-8.0.302-win-x64.zip",
                "https://mr-2.recloud.tech/dotnet-sdk-8.0.302-win-x64.zip",
                "https://mr-3.recloud.tech/dotnet-sdk-8.0.302-win-x64.zip",
            ]
        },
    };
}
