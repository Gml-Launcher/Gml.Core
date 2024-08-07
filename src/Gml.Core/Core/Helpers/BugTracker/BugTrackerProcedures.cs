using System;
using Gml.Core.Launcher;
using Gml.Core.Services.Storage;
using GmlCore.Interfaces.Launcher;
using GmlCore.Interfaces.Procedures;

namespace Gml.Core.Helpers.BugTracker;

public class BugTrackerProcedures(IStorageService storage) : IBugTrackerProcedures
{
    private readonly IStorageService _storage = storage;
    public void CaptureException(IBugInfo bugInfo)
    {
        throw new NotImplementedException();
    }
}
