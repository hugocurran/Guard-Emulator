using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Guard_Emulator
{
    public enum messageType
    {
        ObjectUpdate,
        ObjectDelete,
        Interaction
    }

    /// <summary>
    /// Internal message format
    /// </summary>
    public class internalMessage
    {
        List<string> _attribs = new List<string>();

        public messageType type { get; set; }
        public string federate { get; set; }
        public string entityID { get; set; }
        public string objectName { get; set; }
        public List<string> attribute { get { return _attribs; } }
        public string interactionName { get; set; }
    }
}
