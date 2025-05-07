using GmlCore.Interfaces.System;

namespace Gml.Models.System;

public class LocalFolderInfo : IFolderInfo
{

    public LocalFolderInfo()
    {

    }

    public LocalFolderInfo(string path)
    {
        Path = path;
    }

    public string Path { get; set; }

    public override string ToString()
    {
        return Path;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Path == ((LocalFolderInfo)obj).Path;
    }
}
