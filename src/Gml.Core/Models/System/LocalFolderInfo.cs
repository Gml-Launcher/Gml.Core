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
}
