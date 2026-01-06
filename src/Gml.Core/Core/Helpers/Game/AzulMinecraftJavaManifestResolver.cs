using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core.Files;
using CmlLib.Core.Rules;

namespace Gml.Core.Helpers.Game;

public class AzulMinecraftJavaManifestResolver
{
    private readonly HttpClient _httpClient;

    public AzulMinecraftJavaManifestResolver(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string ManifestServer { get; set; } = "https://api.azul.com/metadata/v1/zulu/packages/";
    public static string? MajorVersion { get; set; }

    public static string GetOSNameForJava(LauncherOSRule os)
    {
        return (os.Name) switch
        {
            LauncherOSRule.Windows => "windows",
            LauncherOSRule.Linux => "linux",
            LauncherOSRule.OSX => "macos",
        };
    }

    public static string GetOSNameForAzulArchiveJava(LauncherOSRule os)
    {
        return (os.Name, os.Arch) switch
        {
            (LauncherOSRule.Windows, "64") => "windows-x64",
            (LauncherOSRule.Windows, "32") => "windows-x86",
            (LauncherOSRule.Windows, "arm64") => "windows-arm64",
            (LauncherOSRule.Windows, _) => $"windows-{os.Arch}",
            (LauncherOSRule.Linux, "64") => "linux",
            (LauncherOSRule.Linux, "32") => "linux-i386",
            (LauncherOSRule.Linux, _) => $"linux-{os.Arch}",
            (LauncherOSRule.OSX, "64") => "mac-os",
            (LauncherOSRule.OSX, "32") => "mac-os",
            (LauncherOSRule.OSX, "arm") => "mac-os-arm64",
            (LauncherOSRule.OSX, "arm64") => "mac-os-arm64",
            (LauncherOSRule.OSX, _) => $"mac-os-{os.Arch}",
            (_, _) => $"{os.Name}-{os.Arch}"
        };
    }

    public static string GetOSArchForJava(LauncherOSRule os)
    {
        return os.Arch switch
        {
            "32" => "x86",
            "64" => "x64",
            "amd64" => "amd64",
            "i686" => "i686",
            "arm" => "arm",
            "arm64" => "aarch64",
            "aarch64" => "aarch64",
            "aarch32" => "aarch32",
            "aarch32sf" => "aarch32sf",
            "aarch32hf" => "aarch32hf",
            "ppc" => "ppc",
            "ppc64" => "ppc64",
            "ppc64hf" => "ppc64hf",
            "ppc32" => "ppc32",
            "ppc32spe" => "ppc32spe",
            "ppc32hf" => "ppc32hf",
            "sparc" => "sparc",
            "sparc32" => "sparc32",
            "sparcv9" => "sparcv9",
            "sparcv9-64" => "sparcv9-64",
            _ => throw new ArgumentException($"Unknown arch: {os.Arch}")
        };
    }

    public async Task<IEnumerable<MinecraftJavaManifestMetadata>> GetAllManifests()
    {
        return null;
    }

    public async Task<IEnumerable<MinecraftJavaManifestMetadata>> GetManifestsForOS(string os, string arch,
        string? majorVersion = null)
    {
        using var json = await requestJsonManifest(os, arch, majorVersion);

        if (json.RootElement.ValueKind != JsonValueKind.Array)
            return Enumerable.Empty<MinecraftJavaManifestMetadata>();

        var components = json.RootElement.EnumerateArray();

        return enumerateComponents(os, components).ToArray();
    }

    private async Task<JsonDocument> requestJsonManifest(string os, string arch, string? majorVersion = null)
    {
        var httpQuery = new
        {
            os = os,
            arch = arch,
            availability_types = "CA",
            certifications = "tck"
        };

        var queryString = $"?os={httpQuery.os}" +
                          $"&arch={httpQuery.arch}" +
                          $"&availability_types={httpQuery.availability_types}" +
                          $"&java_package_type=jre" +
                          $"&certifications={httpQuery.certifications}";
        if (!string.IsNullOrEmpty(majorVersion))
        {
            queryString += $"&java_version={majorVersion}";
        }

        var url = ManifestServer + queryString;

        using var stream = await _httpClient.GetStreamAsync(url);
        return await JsonDocument.ParseAsync(stream);
    }

    private IEnumerable<MinecraftJavaManifestMetadata> enumerateComponents(string osName,
        JsonElement.ArrayEnumerator components)
    {
        foreach (var componentProp in components)
        {
            yield return parseManifest(osName, componentProp);
        }
    }

    private MinecraftJavaManifestMetadata parseManifest(string os, JsonElement component)
    {
        if (!component.TryGetProperty("download_url", out var downloadUrl))
        {
            throw new Exception("Manifest is missing download_url");
        }

        if (!component.TryGetProperty("name", out var name))
        {
            throw new Exception("Manifest is missing name");
        }

        int majorVersion = 0;
        if (component.TryGetProperty("java_version", out var javaVersion) &&
            javaVersion.ValueKind == JsonValueKind.Array)
        {
            var versionArray = javaVersion.EnumerateArray();
            if (versionArray.Any())
            {
                majorVersion = versionArray.First().GetInt32();
            }
        }


        return new MinecraftJavaManifestMetadata(os, component.ToString())
        {
            Metadata = new MFileMetadata
            {
                Url = downloadUrl.GetString()
            },
            VersionName = name.GetString(),
            Component = majorVersion.ToString(),
            VersionReleased = string.Empty
        };
    }

    public async Task<IEnumerable<MinecraftJavaFile>> GetFilesFromManifest(
        MinecraftJavaManifestMetadata manifest,
        CancellationToken cancellationToken)
    {
        var url = manifest.Metadata?.Url;
        if (string.IsNullOrEmpty(url))
            throw new ArgumentException("Url was null");
        return await GetFilesFromManifest(url, cancellationToken);
    }

    public async Task<IEnumerable<MinecraftJavaFile>> GetFilesFromManifest(
        string manifestUrl,
        CancellationToken cancellationToken)
    {
        return
        [
            new MinecraftJavaFile(Path.GetFileName(manifestUrl))
            {
                Url = manifestUrl,
                Type = "file"
            }
        ];
    }

    private IEnumerable<MinecraftJavaFile> parseJavaFilesAndDispose(JsonDocument _json)
    {
        using var json = _json;

        // if (!json.RootElement.TryGetProperty("files", out var files))
        //     yield break;
        //
        // var objects = files.EnumerateObject();
        // foreach (var prop in objects)
        // {
        //     var name = prop.Name;
        //     var value = prop.Value;
        //
        //     var downloadObj = value.GetPropertyOrNull("downloads")?.GetPropertyOrNull("raw");
        //     yield return new MinecraftJavaFile(name)
        //     {
        //         Type = value.GetPropertyValue("type"),
        //         Executable = value.GetPropertyOrNull("executable")?.GetBoolean() ?? false,
        //         Sha1 = downloadObj?.GetPropertyValue("sha1"),
        //         Size = downloadObj?.GetPropertyOrNull("size")?.GetInt64() ?? 0,
        //         Url = downloadObj?.GetPropertyValue("url")
        //     };
        // }

        return [];
    }
}
