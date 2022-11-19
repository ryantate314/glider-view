namespace GliderView.API.Models
{
    public class IgcdPayload
    {
        public string Type { get; set; }
        public string Origin { get; set; }
        public string Airfield { get; set; }
        /// <summary>
        /// The tracker ID
        /// </summary>
        public string Id { get; set; }

        public const string TYPE_TESTHOOK = "testhook";
        public const string TYPE_LANDING = "landing";
        public const string TYPE_TAKEOFF = "takeoff";
        public const string TYPE_UNDEFINEDTAKEOFF = "udtakeoff";
        public const string TYPE_UNDEFINEDLANDING = "udlanding";
    }
}
