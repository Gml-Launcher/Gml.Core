using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Storage;

namespace Gml.Models.Storage;

public class StorageSettings : IStorageSettings
{
    public StorageType StorageType { get; set; }
    public string StorageHost { get; set; }
    public string StorageLogin { get; set; }
    public string StoragePassword { get; set; }
}
