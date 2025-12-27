using System.Security.Cryptography;
using System.Text;
using Gml.Common;
using Gml.Web.Api.Domains.System;

namespace GmlCore.Tests;

[TestFixture]
public class SystemTestsHelper
{
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
        if (File.Exists(_testFilePath)) File.Delete(_testFilePath);
    }

    private string _testFilePath;
    private string _testFileContent;

    [Test]
    public void CalculateFileHash_ValidFile_ReturnsCorrectHash()
    {
        // Arrange
        using var sha256 = SHA256.Create();
        var expectedHash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(_testFileContent)))
            .Replace("-", "").ToLowerInvariant();

        // Act
        var result = SystemHelper.CalculateFileHash(_testFilePath, sha256);

        // Assert
        Assert.That(result, Is.EqualTo(expectedHash));
    }

    [Test]
    public void CalculateFileHash_FileNotFound_ThrowsFileNotFoundException()
    {
        // Arrange
        using var sha256 = SHA256.Create();

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            SystemHelper.CalculateFileHash("nonexistentfile.txt", sha256));
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
        Assert.Throws<PlatformNotSupportedException>(() =>
            SystemHelper.GetStringOsType(OsType.Undefined));
    }

    [Test]
    public void GetStringOsType_InvalidValue_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SystemHelper.GetStringOsType((OsType)99));
    }
}
