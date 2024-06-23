using System;

namespace Gml.Core;

public class VersionNotLoadedException(string message) : Exception
{
    public string InnerExceptionMessage { get; set; } = message;
}
