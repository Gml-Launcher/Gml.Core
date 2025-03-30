using System;

namespace Gml.Core;

public class VersionNotLoadedException(string message) : Exception
{
    public string InnerExceptionMessage { get; set; } = message;
}

public class NewsProviderNotFoundException(string message) : Exception
{
    public string ExceptionMessage { get; set; } = message;
}
