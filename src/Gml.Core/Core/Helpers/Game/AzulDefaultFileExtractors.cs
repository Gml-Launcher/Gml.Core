using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using CmlLib.Core.FileExtractors;
using CmlLib.Core.Java;
using CmlLib.Core.Rules;
using CmlLib.Core.Version;

namespace Gml.Core.Helpers.Game;

public class AzulDefaultFileExtractors
{
    public LibraryFileExtractor? Library { get; set; }
    public AssetFileExtractor? Asset { get; set; }
    public ClientFileExtractor? Client { get; set; }
    public AzulJavaFileExtractor? Java { get; set; }
    public LogFileExtractor? Log { get; set; }

    public IEnumerable<IFileExtractor> ExtraExtractors { get; set; } =
        Enumerable.Empty<IFileExtractor>();

    public static AzulDefaultFileExtractors CreateDefault(
        HttpClient httpClient,
        IRulesEvaluator rulesEvaluator,
        IJavaPathResolver javaPathResolver)
    {
        var extractors = new AzulDefaultFileExtractors();
        extractors.Library = new LibraryFileExtractor(JsonVersionParserOptions.ClientSide, rulesEvaluator);
        extractors.Asset = new AssetFileExtractor(httpClient);
        extractors.Client = new ClientFileExtractor();
        extractors.Java = new AzulJavaFileExtractor(httpClient, javaPathResolver);
        extractors.Log = new LogFileExtractor();
        return extractors;
    }

    public FileExtractorCollection ToExtractorCollection()
    {
        var extractors = new FileExtractorCollection();
        if (Library != null)
            extractors.Add(Library);
        if (Asset != null)
            extractors.Add(Asset);
        if (Client != null)
            extractors.Add(Client);
        if (Log != null)
            extractors.Add(Log);
        if (Java != null)
            extractors.Add(Java);
        extractors.AddRange(ExtraExtractors);
        return extractors;
    }
}
