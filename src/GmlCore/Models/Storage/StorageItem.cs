using SQLite;

namespace Gml.Models.Storage
{
    public class StorageItem
    {        
        [PrimaryKey] public string Key { get; set; } = null!;
        public string? TypeName { get; set; }
        public string Value { get; set; } = null!;
    }
}