using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using CmlLib.Core;
using CmlLib.Core.Natives;
using CmlLib.Core.Rules;
using CmlLib.Core.Version;
using ICSharpCode.SharpZipLib.Zip;

namespace Gml.Core.Helpers.Game;

public class CustomNativeLibraryExtractor : INativeLibraryExtractor
{
    public CustomNativeLibraryExtractor(
        IRulesEvaluator rulesEvaluator)
    {
        this.rulesEvaluator = rulesEvaluator;
    }

    private readonly IRulesEvaluator rulesEvaluator;

    public string Extract(
        MinecraftPath path,
        IVersion version,
        RulesEvaluatorContext rulesContext)
    {
        var extractPath = path.GetNativePath(version.Id);
        Directory.CreateDirectory(extractPath);

        var nativeLibraries = version
            .ConcatInheritedCollection(v => v.Libraries)
            .Where(lib => lib.IsClientRequired)
            .Where(lib => lib.Rules == null || rulesEvaluator.Match(lib.Rules, rulesContext));

        foreach (var nativeLibrary in nativeLibraries)
        {
            var libPath = nativeLibrary.GetNativeLibraryPath(rulesContext.OS);
            if (string.IsNullOrEmpty(libPath))
                continue;

            SharpZipWrapper.Unzip(
                Path.Combine(path.Library, libPath),
                extractPath,
                nativeLibrary.ExtractExcludes,
                default);
        }

        return extractPath;
    }

    public void Clean(MinecraftPath path, IVersion version)
    {
        return;
    }
}


internal static class SharpZipWrapper
{
    public static void Unzip(
        string zipPath,
        string extractTo,
        IReadOnlyCollection<string> excludes,
        CancellationToken cancellationToken = default)
    {
        using var fs = File.OpenRead(zipPath);
        using var s = new ZipInputStream(fs);

        ZipEntry e;
        while ((e = s.GetNextEntry()) != null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (excludes.Any(e.Name.StartsWith))
                continue;

            var fullPath = Path.Combine(extractTo, e.Name);
            if (e.IsFile)
            {
                IOUtil.CreateParentDirectory(fullPath);
                var fileName = Path.GetFileName(fullPath);

                try
                {
                    using var output = File.Create(fullPath);
                    s.CopyTo(output);
                }
                catch (IOException)
                {
                    // just skip
                }
                catch (UnauthorizedAccessException)
                {
                    // just skip
                }
            }
            else
            {
                Directory.CreateDirectory(fullPath);
            }
        }
    }
}

internal static class IOUtil
{
    public static void CreateParentDirectory(string filePath)
    {
        var dirPath = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dirPath))
            Directory.CreateDirectory(dirPath);
    }

    public static string NormalizePath(string path)
    {
        return Path.GetFullPath(path)
            .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .TrimEnd(Path.DirectorySeparatorChar);
    }

    public static string CombinePath(IEnumerable<string> paths, string pathSeparator)
    {
        return string.Join(pathSeparator, paths.Select(Path.GetFullPath));
    }

    public static bool CheckFileValidation(string path, string? compareHash)
    {
        if (!File.Exists(path))
            return false;

        var fileHash = IOUtil.ComputeFileSHA1(path);
        return fileHash == compareHash;
    }

    public static string ComputeFileSHA1(string path)
    {
#pragma warning disable CS0618
#pragma warning disable SYSLIB0021

        using var file = File.OpenRead(path);
        using var hasher = new SHA1CryptoServiceProvider();

        var binaryHash = hasher.ComputeHash(file);
        return BitConverter.ToString(binaryHash).Replace("-", "").ToLowerInvariant();

#pragma warning restore SYSLIB0021
#pragma warning restore CS0618
    }
}
