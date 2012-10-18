using System;
using Newtonsoft.Json;

namespace Esilog.Gelf4net.Appender
{
    [JsonObject( MemberSerialization.OptIn )]
    internal class GelfMessage
    {
        [JsonProperty( "facility" )]
        public String Facility
        {
            get;
            set;
        }

        [JsonProperty( "file" )]
        public String File
        {
            get;
            set;
        }

        [JsonProperty( "full_message" )]
        public String FullMesage
        {
            get;
            set;
        }

        [JsonProperty( "host" )]
        public String Host
        {
            get;
            set;
        }

        [JsonProperty( "level" )]
        public Int32 Level
        {
            get;
            set;
        }

        [JsonProperty( "line" )]
        public String Line
        {
            get;
            set;
        }

        [JsonProperty( "short_message" )]
        public String ShortMessage
        {
            get;
            set;
        }

        [JsonProperty( "timestamp" )]
        public Decimal TimeStamp
        {
            get;
            set;
        }

        [JsonProperty( "version" )]
        public String Version
        {
            get;
            set;
        }
    }
}