using Newtonsoft.Json;

namespace Gml.Common.TextureService
{
    public class TextureReadDto
    {
        [JsonProperty("userName")]
        public string UserName { get; set; }

        [JsonProperty("hasCloak")]
        public bool HasCloak { get; set; }

        [JsonProperty("hasSkin")]
        public bool HasSkin { get; set; }

        [JsonProperty("skinUrl")]
        public string SkinUrl { get; set; }

        [JsonProperty("clockUrl")]
        public string ClockUrl { get; set; }

        [JsonProperty("texture")]
        public Texture Texture { get; set; }

        [JsonProperty("skinFormat")]
        public int SkinFormat { get; set; }
    }

    public class Texture
    {
        [JsonProperty("head")]
        public string Head { get; set; }

        [JsonProperty("front")]
        public string Front { get; set; }

        [JsonProperty("back")]
        public string Back { get; set; }

        [JsonProperty("cloakBack")]
        public string CloakBack { get; set; }

        [JsonProperty("cloak")]
        public string Cloak { get; set; }
    }
}
