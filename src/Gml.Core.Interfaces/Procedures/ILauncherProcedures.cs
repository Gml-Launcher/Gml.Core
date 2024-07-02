using System;
using System.Threading.Tasks;
using Gml.Web.Api.Domains.System;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Storage;

namespace GmlCore.Interfaces.Procedures;

public interface ILauncherProcedures
{
    Task<string> CreateVersion(IVersionFile version, OsType osTypeEnum);
    Task Build(string version);
    IObservable<string> BuildLogs { get; }
}
