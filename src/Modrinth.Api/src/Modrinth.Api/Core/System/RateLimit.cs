namespace Modrinth.Api.Core.System
{
    public class RateLimit
    {
        public int Limit { get; set; }
        public int Remaining { get; set; }
        public int Reset { get; set; }
    }
}
