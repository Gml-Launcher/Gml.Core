using System;
using System.IO;
using System.Security.Cryptography;
using Gml.Common;
using Gml.Web.Api.Domains.System;
using NUnit.Framework;

namespace GmlCore.Tests;

[TestFixture]
public class SystemHelperTest
{
    private string _testFilePath = string.Empty;
    private string _testFileContent = string.Empty;

    [SetUp]
    public void Setup()
    {
        _testFileContent = "test content";
        _testFilePath = Path.GetTempFileName();
        File.WriteAllText(_testFilePath, _testFileContent);
    }

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(_testFilePath))
        {
            try { File.Delete(_testFilePath); } catch { /* ignore */ }
        }
    }

    [Test]
    public void CalculateFileHash_ValidFile_ReturnsCorrectSha256()
    {
        using var sha256 = SHA256.Create();
        var expected = BitConverter
            .ToString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(_testFileContent)))
            .Replace("-", "").ToLowerInvariant();

        var actual = SystemHelper.CalculateFileHash(_testFilePath, sha256);

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void CalculateFileHash_FileNotFound_ThrowsFileNotFoundException()
    {
        using var sha256 = SHA256.Create();
        Assert.Throws<FileNotFoundException>(() => SystemHelper.CalculateFileHash("nonexistent_file.xyz", sha256));
    }

    [Test]
    public void GetStringOsType_Linux_ReturnsLinux()
    {
        Assert.That(SystemHelper.GetStringOsType(OsType.Linux), Is.EqualTo("linux"));
    }

    [Test]
    public void GetStringOsType_Windows_ReturnsWindows()
    {
        Assert.That(SystemHelper.GetStringOsType(OsType.Windows), Is.EqualTo("windows"));
    }

    [Test]
    public void GetStringOsType_OsX_ReturnsOsx()
    {
        Assert.That(SystemHelper.GetStringOsType(OsType.OsX), Is.EqualTo("osx"));
    }

    [Test]
    public void GetStringOsType_Undefined_ThrowsPlatformNotSupportedException()
    {
        Assert.Throws<PlatformNotSupportedException>(() => SystemHelper.GetStringOsType(OsType.Undefined));
    }

    [Test]
    public void GetStringOsType_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SystemHelper.GetStringOsType((OsType)99));
    }
}
