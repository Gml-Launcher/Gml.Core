using GmlCore.Interfaces.System;

namespace Gml.Models.System
{
    public class LocalFileInfo : IFileInfo
    {
        public string Name { get; set; }
        public string Directory { get; set; }
        public long Size { get; set; }
        public string Hash { get; set; }
        public string FullPath { get; set; }
    }
}
