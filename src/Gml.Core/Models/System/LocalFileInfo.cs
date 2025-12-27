using System.IO;
using GmlCore.Interfaces.System;

namespace Gml.Models.System;

public class LocalFileInfo : IFileInfo
{
    private string _directory;

    public LocalFileInfo()
    {
    }

    public LocalFileInfo(string directory)
    {
        Directory = directory;
        Name = Path.GetFileName(directory);
    }

    public string Name { get; set; }

    public string Directory
    {
        get => _directory.Replace("\t", string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
        set => _directory = value;
    }

    public long Size { get; set; }
    public string Hash { get; set; }
    public string FullPath { get; set; }

    public override string ToString()
    {
        return Directory;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Directory == ((LocalFileInfo)obj).Directory;
    }
}
