using System;
using GmlCore.Interfaces.Sentry;
using SQLite;

namespace Gml.Models.Storage
{
    public class StorageItem
    {
        [PrimaryKey] public string Key { get; set; } = null!;
        public string? TypeName { get; set; }
        public string Value { get; set; } = null!;
    }

    public class UserStorageItem
    {
        [PrimaryKey] public string Login { get; set; } = null!;
        public string? Uuid { get; set; }
        public string? TypeName { get; set; }
        public string Value { get; set; } = null!;
        public string? AccessToken { get; set; }
        public string? SkinGuid { get; set; }
        public string? CloakGuid { get; set; }
    }

    public class BugItem : IStorageBug
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public Guid Guid { get; set; }
        public ProjectType ProjectType { get; set; }
        public DateTime Date { get; set; }
        public string? Attachment { get; set; }
        public string Value { get; set; } = null!;
    }

    public class BannedHardwareItem
    {
        public string? CpuIdentifier { get; set; }
        public string? MotherboardIdentifier { get; set; }
        public string? DiskIdentifiers { get; set; }
    }
}
