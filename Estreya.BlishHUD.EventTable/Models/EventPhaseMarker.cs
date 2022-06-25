namespace Estreya.BlishHUD.EventTable.Models;

using Blish_HUD;
using Estreya.BlishHUD.EventTable.UI.Views.Controls;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventPhaseMarker
{
    private static readonly Logger Logger = Logger.GetLogger<EventPhaseMarker>();

    /// <summary>
    /// Describes the time the marker occures after event start.
    /// </summary>
    [JsonProperty("time"), TypeOverride(typeof(string)), Description("Specifies the time in minutes after the event started.")]
    public float Time { get; set; }

    [JsonIgnore]
    private string _colorCode;

    [JsonProperty("color")]
    public string ColorCode
    {
        get => this._colorCode;
        set
        {
            this._colorCode = value;
            this._color = null;
        }
    }

    [JsonIgnore]
    private Color? _color;

    [JsonIgnore]
    public Color Color
    {
        get
        {
            if (this._color == null)
            {
                try
                {
                    System.Drawing.Color color = string.IsNullOrWhiteSpace(this.ColorCode) ? System.Drawing.Color.White : System.Drawing.ColorTranslator.FromHtml(this.ColorCode);
                    this._color = new Color(color.R, color.G, color.B);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed generating color:");
                    this._color = Microsoft.Xna.Framework.Color.White;
                }
            }

            return _color ?? Color.White;
        }
    }

    [JsonProperty("description")]
    public string Description { get; set; }

    public EventPhaseMarker() { }

    public EventPhaseMarker(float time, string colorCode)
    {
        this.Time = time;
        this.ColorCode = colorCode;
    }

    public EventPhaseMarker(float time, string colorCode, string description) : this(time, colorCode)
    {
        this.Description = description;
    }
}
