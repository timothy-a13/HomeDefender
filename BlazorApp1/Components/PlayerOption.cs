using System.Text.Json.Serialization;

namespace BlazorApp1.Components
{
   
    public class PlayerOption
    {
        [JsonPropertyName("width")]
        public int Width { get; set; } = 300;

        [JsonPropertyName("height")]
        public int Height { get; set; } = 200;

        [JsonPropertyName("controls")]
        public bool Controls { get; set; } = true;

        [JsonPropertyName("autoplay")]
        public bool Autoplay { get; set; } = true;

        [JsonPropertyName("muted")]
        public bool Muted { get; set; } = true;

        [JsonPropertyName("preload")]
        public string Preload { get; set; } = "auto";

        /// 播放資源
        [JsonPropertyName("src")]
        public string? SrcUrl { get; set; }

        [JsonPropertyName("type")]
        public string? SrcType { get; set; }

        [JsonPropertyName("class")]
        public string? Class { get; set; }
    }
}
