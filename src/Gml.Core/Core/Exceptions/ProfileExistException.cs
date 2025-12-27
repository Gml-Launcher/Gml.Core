using System;
using GmlCore.Interfaces.Launcher;

namespace Gml.Core.Exceptions;

public class ProfileExistException : Exception
{
    public ProfileExistException(IGameProfile profile,
        string message = "A profile with the specified name already exists!") : base(message)
    {
        Profile = profile;
    }

    public IGameProfile Profile { get; }
}
