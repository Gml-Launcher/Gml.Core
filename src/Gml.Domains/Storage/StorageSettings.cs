using System;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Storage;

namespace Gml.Domains.Storage;

public class StorageSettings : IStorageSettings
{
    public StorageType StorageType { get; set; }
    public string? StorageHost { get; set; }
    public string? StorageLogin { get; set; }
    public string? StoragePassword { get; set; }
    public TextureProtocol TextureProtocol { get; set; }
    public bool SentryNeedAutoClear { get; set; }
    public TimeSpan SentryAutoClearPeriod { get; set; }
}
