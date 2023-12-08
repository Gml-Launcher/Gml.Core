using System.Threading.Tasks;
using GmlCore.Interfaces.System;

namespace GmlCore.Interfaces.Procedures
{
    public interface IFileStorageProcedures
    {
        Task<IFileInfo?> GetFileInfo(string fileHash);
    }
}
