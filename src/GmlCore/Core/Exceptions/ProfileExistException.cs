using System;
using System.Threading;
using GmlCore.Interfaces.Launcher;

namespace Gml.Core.Exceptions
{
    public class ProfileExistException : Exception
    {
        public IGameProfile Profile { get; }


        public ProfileExistException(IGameProfile profile, string message = "A profile with the specified name already exists!") : base(message)
        {
            Profile = profile;
        }
        
    }
}