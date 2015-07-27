using System;
using System.Runtime.Serialization;

namespace com.garysieling.database
{
    [DataContract]
    public class TimingRecord
    {
        [DataMember]
        public String SessionId { get; set; }
        [DataMember]
        public int UserId { get; set; }
        [DataMember]
        public String DbId { get; set; }
        [DataMember]
        public DateTime RunDate { get; set; }
        [DataMember]
        public double? Duration { get; set; }
        [DataMember]
        public int Order { get; set; }
        [DataMember]
        public String Query { get; set; }
        [DataMember]
        public String QueryParms { get; set; }
        [DataMember]
        public String Description { get; set; }
        [DataMember]
        public String Domain { get; set; }
        [DataMember]
        public String IP { get; set; }
        [DataMember]
        public String ErrorMessage { get; set; }
    }
}
