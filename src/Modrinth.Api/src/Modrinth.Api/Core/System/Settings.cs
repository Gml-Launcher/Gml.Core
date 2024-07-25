namespace Modrinth.Api.Core.System
{
    public class Settings
    {
        public RateLimit RateLimit { get; } = new RateLimit();
        public string UserAgent { get; set; }
        public int RequestTimeout { get; set; } = 60;
    }
}
