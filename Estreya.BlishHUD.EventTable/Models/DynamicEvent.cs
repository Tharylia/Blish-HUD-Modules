namespace Estreya.BlishHUD.EventTable.Models;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Newtonsoft.Json;
using System;

public class DynamicEvent
{
    [JsonProperty("id")] public string ID { get; set; }

    [JsonProperty("name")] public string Name { get; set; }

    [JsonProperty("level")] public int Level { get; set; }

    [JsonProperty("map_id")] public int MapId { get; set; }

    [JsonProperty("flags")] public string[] Flags { get; set; }

    [JsonProperty("location")] public DynamicEventLocation Location { get; set; }

    [JsonProperty("icon")] public DynamicEventIcon Icon { get; set; }

    [JsonProperty("custom")] public bool IsCustom { get; set; }

    //[JsonProperty("defaultDisabled")] public bool DefaultDisabled { get; set; }

    [JsonProperty("color")] public string ColorCode { get; set; }

    public Color GetColorAsXnaColor()
    {
        var defaultColor = Color.White;

        if (string.IsNullOrWhiteSpace(this.ColorCode)) return defaultColor;

        try
        {
            System.Drawing.Color parsedColor = System.Drawing.ColorTranslator.FromHtml(this.ColorCode);
            return new Color(parsedColor.R, parsedColor.G, parsedColor.B);
        }
        catch (Exception)
        {
            return defaultColor;
        }
    }

    public class DynamicEventLocation
    {
        [JsonProperty("type")] public string Type { get; set; }

        [JsonProperty("center")] public float[] Center { get; set; }

        [JsonProperty("radius")] public float Radius { get; set; }

        /// <summary>
        ///     Height defines the total height in inches.
        /// </summary>
        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("rotation")] public float Rotation { get; set; }

        /// <summary>
        ///     Z Ranges defines the top and bottom boundaries offset from the center z.
        /// </summary>
        [JsonProperty("z_range")]
        public float[] ZRange { get; set; }

        [JsonProperty("points")] public float[][] Points { get; set; }
    }

    public class DynamicEventIcon
    {
        [JsonProperty("file_id")] public int FileID { get; set; }

        [JsonProperty("signature")] public string Signature { get; set; }
    }
}
