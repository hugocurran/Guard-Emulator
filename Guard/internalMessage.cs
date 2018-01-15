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

        public override string ToString()
        {
            string common = String.Format("Type: {0} Time: {1} Sequence: {2}", Type, TimeStamp.ToUniversalTime(), SequenceNumber);
            switch (Type)
            {
                case MessageType.Status:
                    return common + String.Format(" SessionName: {0}", SessionName);
                case MessageType.ObjectCreate:
                case MessageType.ObjectDelete:
                    return common + String.Format(" Federate: {0} Entity: {1} ObjectName: {2}", Federate, EntityID, ObjectName);
                case MessageType.ObjectUpdate:
                    common = common + String.Format(" Federate: {0} Entity: {1} ObjectName: {2} Attributes: ", Federate, EntityID, ObjectName);
                    foreach (string attrib in _attribs)
                        common = common + attrib + ", ";
                    return common;
                case MessageType.Interaction:
                    return common + String.Format(" InteractionName: {0}", InteractionName);
            }
            return "";
        }
    }
}
