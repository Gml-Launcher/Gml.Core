using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CmlLib.Core;
using CmlLib.Core.FileExtractors;
using CmlLib.Core.Files;
using CmlLib.Core.Java;
using CmlLib.Core.Rules;
using CmlLib.Core.Tasks;
using CmlLib.Core.Version;

namespace Gml.Core.Helpers.Game;

public class AzulJavaFileExtractor(
    HttpClient httpClient,
    IJavaPathResolver javaPathResolver)
    : IFileExtractor
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IJavaPathResolver _javaPathResolver = javaPathResolver;
    public string JavaManifestServer { get; set; } = "https://api.azul.com/metadata/v1/zulu/packages/";

    public new async ValueTask<IEnumerable<GameFile>> Extract(
        MinecraftPath path,
        IVersion version,
        RulesEvaluatorContext rulesContext,
        CancellationToken cancellationToken)
    {
        JavaVersion javaVersion;
        if (string.IsNullOrEmpty(version.JavaVersion?.Component))
            javaVersion = MinecraftJavaPathResolver.JreLegacyVersion;
        else
            javaVersion = new JavaVersion($"azul-{version.JavaVersion.Component}", version.JavaVersion.MajorVersion);

        var manifestResolver = new AzulMinecraftJavaManifestResolver(_httpClient);
        manifestResolver.ManifestServer = JavaManifestServer;

        var extractor = new Extractor(
            _javaPathResolver,
            rulesContext,
            manifestResolver);
        return await extractor.ExtractFromJavaVersion(javaVersion, cancellationToken);
    }

    public class Extractor
    {
        private readonly IJavaPathResolver _javaPathResolver;
        private readonly AzulMinecraftJavaManifestResolver _manifestResolver;
        private readonly RulesEvaluatorContext _rulesContext;

        public Extractor(
            IJavaPathResolver javaPathResolver,
            RulesEvaluatorContext rulesContext,
            AzulMinecraftJavaManifestResolver manifestResolver)
        {
            _javaPathResolver = javaPathResolver;
            _rulesContext = rulesContext;
            _manifestResolver = manifestResolver;
        }

        public async ValueTask<IEnumerable<GameFile>> ExtractFromJavaVersion(
            JavaVersion javaVersion,
            CancellationToken cancellationToken)
        {
            var manifestUrl = await findManifestUrl(javaVersion);
            if (string.IsNullOrEmpty(manifestUrl))
                return Enumerable.Empty<GameFile>();

            var installPath = _javaPathResolver.GetJavaDirPath(javaVersion, _rulesContext);
            var files = await _manifestResolver.GetFilesFromManifest(manifestUrl, cancellationToken);
            return extractFiles(installPath, files);
        }

        private async ValueTask<string?> findManifestUrl(JavaVersion javaVersion)
        {
            var osName = AzulMinecraftJavaManifestResolver.GetOSNameForJava(_rulesContext.OS);
            var osArch = AzulMinecraftJavaManifestResolver.GetOSArchForJava(_rulesContext.OS);
            var manifests = await _manifestResolver.GetManifestsForOS(osName, osArch,
                AzulMinecraftJavaManifestResolver.MajorVersion ?? javaVersion.MajorVersion);
            var manifestUrl = findManifestUrlFromMetadatas(manifests,
                AzulMinecraftJavaManifestResolver.MajorVersion ?? javaVersion.MajorVersion);

            if (string.IsNullOrEmpty(manifestUrl) &&
                javaVersion.Component != MinecraftJavaPathResolver.JreLegacyVersion.Component)
                manifestUrl =
                    findManifestUrlFromMetadatas(manifests, MinecraftJavaPathResolver.JreLegacyVersion.Component);

            return manifestUrl;
        }

        private string? findManifestUrlFromMetadatas(IEnumerable<MinecraftJavaManifestMetadata> metadatas,
            string version)
        {
            return metadatas.FirstOrDefault(v => v.Component == version)?.Metadata?.Url;
        }

        private IEnumerable<GameFile> extractFiles(
            string path,
            IEnumerable<MinecraftJavaFile> files)
        {
            foreach (var javaFile in files)
            {
                if (javaFile.Type == "file")
                {
                    var gameFile = extractFile(path, javaFile);
                    yield return gameFile;
                }
            }
        }

        private GameFile extractFile(string path, MinecraftJavaFile javaFile)
        {
            var filePath = Path.Combine(path, javaFile.Name);
            filePath = IOUtil.NormalizePath(filePath);

            return new GameFile(javaFile.Name)
            {
                Hash = javaFile.Sha1,
                Path = filePath,
                Url = javaFile.Url,
                Size = javaFile.Size,
                UpdateTask =
                [
                    new ZuluJavaExtractionTask(path)
                ]
            };
        }
    }
}

public class ZuluJavaExtractionTask : IUpdateTask
{
    public ZuluJavaExtractionTask(string extractTo) => this.ExtractTo = extractTo;

    public string ExtractTo { get; }

    public ValueTask Execute(GameFile file, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(file.Path))
            throw new InvalidOperationException("file.Path was null");
        SharpZipWrapper.Unzip(file.Path, ExtractTo, [], cancellationToken);
        File.Delete(file.Path);
        return new ValueTask();
    }
}
