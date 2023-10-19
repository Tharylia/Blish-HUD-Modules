namespace Estreya.BlishHUD.EventTable.Contexts;

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using static Estreya.BlishHUD.EventTable.Models.DynamicEvent;

public struct AddDynamicEvent
{
    public AddDynamicEvent()
    {
    }

    public Guid? Id { get; set; } = null;
    public string Name { get; set; } = null;

    public int Level { get; set; } = 0;

    public int MapId { get; set; } = 0;

    public string[] Flags { get; set; } = null;

    public AddDynamicEventLocation? Location { get; set; } = null;

    public AddDynamicEventIcon? Icon { get; set; } = null;

    public string ColorCode { get; set; } = null;

    public struct AddDynamicEventLocation
    {
        public const string TYPE_POLY = "poly";
        public const string TYPE_SPHERE = "sphere";
        public const string TYPE_CYLINDER = "cylinder";

        public AddDynamicEventLocation()
        {
        }

        public string Type { get; set; } = null;

        public float[] Center { get; set; } = null;

        public float Radius { get; set; } = 0;

        /// <summary>
        ///     Height defines the total height in inches.
        /// </summary>
        public float Height { get; set; } = 0;

        public float Rotation { get; set; } = 0;

        /// <summary>
        ///     Z Ranges defines the top and bottom boundaries offset from the center z.
        /// </summary>
        public float[] ZRange { get; set; } = null;

        public float[][] Points { get; set; } = null;
    }

    public struct AddDynamicEventIcon
    {
        public AddDynamicEventIcon()
        {
        }

        public int FileID { get; set; } = 0;

        public string Signature { get; set; } = null;
    }
}
