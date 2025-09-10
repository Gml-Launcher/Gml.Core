using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gml.Core.User
{
    public class AuthUser : User
    {
        public List<AuthUserHistory> AuthHistory { get; set; } = new();
        public List<ServerJoinHistory> ServerJoinHistory { get; set; } = new();

        public override async Task Block(bool isPermanent)
        {
            if (isPermanent)
            {
                var hwids = AuthHistory.Select(c => c.Hwid).Distinct();

                await Manager.Users.BlockHardware(hwids);
            }

            await base.Block(isPermanent);
        }
    }
}
