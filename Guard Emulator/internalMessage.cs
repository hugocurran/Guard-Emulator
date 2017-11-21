using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Guard_Emulator
{
    public enum MessageType
    {
        Status,
        ObjectCreate,
        ObjectUpdate,
        ObjectDelete,
        Interaction
    }

    /// <summary>
    /// Internal message format
    /// </summary>
    public class InternalMessage
    {
        List<string> _attribs = new List<string>();
        internal string AddAttribute { set { _attribs.Add(value); } } // Here to support unit testing

        public DateTimeOffset TimeStamp { get; set; }
        public int SequenceNumber { get; set; }
        public MessageType Type { get; set; }
        public string Federate { get; set; }
        public string EntityID { get; set; }
        public string ObjectName { get; set; }
        public List<string> Attribute { get { return _attribs; } }
        public string InteractionName { get; set; }
        public bool SessionActive { get; set; }
        public string SessionName { get; set; }
    }
}
