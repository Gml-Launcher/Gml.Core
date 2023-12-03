using Gml.Models.Enums;
using GmlCore.Interfaces.Enums;
using GmlCore.Interfaces.Launcher;

namespace Gml.Models
{
    public class GameProfile : BaseProfile
    {
        public GameProfile()
        {
            
        }
        
        internal GameProfile(string name, string gameVersion, GameLoader gameLoader) : base(name, gameVersion, gameLoader)
        {
        }

        public static IGameProfile Empty { get; set; } = new GameProfile("Empty", "0.0.0", GmlCore.Interfaces.Enums.GameLoader.Undefined);
    }
}