using Gml.Core.Helpers.Mirrors;

namespace GmlCore.Tests;

[TestFixture]
public class MirrorsHelperTests
{
    [Test]
    public void JavaMirrors_HasExpectedPlatformsAndUrls_InOrder()
    {
        // has exactly three platforms
        Assert.That(MirrorsHelper.JavaMirrors.Count, Is.EqualTo(3));

        // linux
        Assert.That(MirrorsHelper.JavaMirrors.ContainsKey("linux"), Is.True);
        var linux = MirrorsHelper.JavaMirrors["linux"];
        var expectedLinux = new[]
        {
            "https://mirror.recloud.tech/openjdk-22_linux-x64_bin.zip",
            "https://mirror.recloud.host/openjdk-22_linux-x64_bin.zip",
            "https://mr-1.recloud.tech/openjdk-22_linux-x64_bin.zip",
            "https://mr-2.recloud.tech/openjdk-22_linux-x64_bin.zip",
            "https://mr-3.recloud.tech/openjdk-22_linux-x64_bin.zip"
        };
        Assert.That(linux, Is.EqualTo(expectedLinux));

        // windows
        Assert.That(MirrorsHelper.JavaMirrors.ContainsKey("windows"), Is.True);
        var windows = MirrorsHelper.JavaMirrors["windows"];
        var expectedWindows = new[]
        {
            "https://mirror.recloud.tech/openjdk-22_windows-x64_bin.zip",
            "https://mirror.recloud.host/openjdk-22_windows-x64_bin.zip",
            "https://mr-1.recloud.tech/openjdk-22_windows-x64_bin.zip",
            "https://mr-2.recloud.tech/openjdk-22_windows-x64_bin.zip",
            "https://mr-3.recloud.tech/openjdk-22_windows-x64_bin.zip"
        };
        Assert.That(windows, Is.EqualTo(expectedWindows));

        // osx
        Assert.That(MirrorsHelper.JavaMirrors.ContainsKey("osx"), Is.True);
        var osx = MirrorsHelper.JavaMirrors["osx"];
        var expectedOsx = new[]
        {
            "https://mirror.recloud.tech/openjdk-22_macos-aarch64.zip",
            "https://mirror.recloud.host/openjdk-22_macos-aarch64.zip",
            "https://mr-1.recloud.tech/openjdk-22_macos-aarch64.zip",
            "https://mr-2.recloud.tech/openjdk-22_macos-aarch64.zip",
            "https://mr-3.recloud.tech/openjdk-22_macos-aarch64.zip"
        };

        Assert.That(osx, Is.EqualTo(expectedOsx).AsCollection);
    }

    [Test]
    public void DotnetMirrors_HasExpectedPlatformsAndUrls_InOrder()
    {
        // has exactly three platforms
        Assert.That(MirrorsHelper.DotnetMirrors.Count, Is.EqualTo(3));

        // linux
        Assert.That(MirrorsHelper.DotnetMirrors.ContainsKey("linux"), Is.True);
        var linux = MirrorsHelper.DotnetMirrors["linux"];
        var expectedLinux = new[]
        {
            "https://mirror.recloud.tech/dotnet-sdk-8.0.302-linux-x64.zip",
            "https://mirror.recloud.host/dotnet-sdk-8.0.302-linux-x64.zip",
            "https://mr-1.recloud.tech/dotnet-sdk-8.0.302-linux-x64.zip",
            "https://mr-2.recloud.tech/dotnet-sdk-8.0.302-linux-x64.zip",
            "https://mr-3.recloud.tech/dotnet-sdk-8.0.302-linux-x64.zip"
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(linux, Is.EqualTo(expectedLinux).AsCollection);

            // windows
            Assert.That(MirrorsHelper.DotnetMirrors.ContainsKey("windows"), Is.True);
        }

        var windows = MirrorsHelper.DotnetMirrors["windows"];
        var expectedWindows = new[]
        {
            "https://mirror.recloud.tech/dotnet-sdk-8.0.302-win-x64.zip",
            "https://mirror.recloud.host/dotnet-sdk-8.0.302-win-x64.zip",
            "https://mr-1.recloud.tech/dotnet-sdk-8.0.302-win-x64.zip",
            "https://mr-2.recloud.tech/dotnet-sdk-8.0.302-win-x64.zip",
            "https://mr-3.recloud.tech/dotnet-sdk-8.0.302-win-x64.zip"
        };
        using (Assert.EnterMultipleScope())
        {
            Assert.That(windows, Is.EqualTo(expectedWindows).AsCollection);

            // osx
            Assert.That(MirrorsHelper.DotnetMirrors.ContainsKey("osx"), Is.True);
        }

        var osx = MirrorsHelper.DotnetMirrors["osx"];
        var expectedOsx = new[]
        {
            "https://mirror.recloud.tech/dotnet-sdk-8.0.302-macos-aarch64.zip",
            "https://mirror.recloud.host/dotnet-sdk-8.0.302-macos-aarch64.zip",
            "https://mr-1.recloud.tech/dotnet-sdk-8.0.302-macos-aarch64.zip",
            "https://mr-2.recloud.tech/dotnet-sdk-8.0.302-macos-aarch64.zip",
            "https://mr-3.recloud.tech/dotnet-sdk-8.0.302-macos-aarch64.zip"
        };
        Assert.That(osx, Is.EqualTo(expectedOsx).AsCollection);
    }
}
