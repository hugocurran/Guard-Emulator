using System;
using System.Collections.Generic;
using System.Text;

namespace Guard_Emulator
{
    /// <summary>
    /// Parser for OSP messages using HPSD
    /// </summary>
    public static class HpsdParser
    {
        /// <summary>
        /// Parse an HPSD message
        /// </summary>
        /// <param name="message">OSP message to parse</param>
        /// <returns>Parsed message using internal message format</returns>
        public static InternalMessage ParseMessage(HpsdMessage message)
        {
            // Convert message timestamp to DateTime
            InternalMessage parsedMessage = new InternalMessage();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            parsedMessage.TimeStamp = start.AddMilliseconds(message.Timestamp).ToLocalTime();
            parsedMessage.SequenceNumber = message.SequenceNumber;

            switch(message.MessageType)
            {
                case HpsdMessage.Types.MessageType.SessionStatus:
                    parsedMessage.Type = MessageType.Status;
                    parsedMessage.SessionActive = message.SessionStatus.Active;
                    parsedMessage.SessionName = message.SessionStatus.SessionName;
                    break;

                case HpsdMessage.Types.MessageType.ObjectCreate:
                    parsedMessage.Type = MessageType.ObjectCreate;
                    parsedMessage.Federate = message.ObjectCreate.ProducingFederate;
                    parsedMessage.EntityID = message.ObjectCreate.InstanceId;
                    // HPSD omits the HLAobjectRoot
                    parsedMessage.ObjectName = "HLAobjectRoot." + message.ObjectCreate.ObjectClassName;
                    break;

                case HpsdMessage.Types.MessageType.ObjectUpdate:
                    parsedMessage.Type = MessageType.ObjectUpdate;
                    parsedMessage.Federate = message.ObjectUpdate.ProducingFederate;
                    parsedMessage.EntityID = message.ObjectUpdate.InstanceId;
                    parsedMessage.ObjectName = "HLAobjectRoot." + message.ObjectUpdate.ObjectClassName;
                    foreach (var attrib in message.ObjectUpdate.Attributes)
                    {
                        parsedMessage.Attribute.Add(attrib.Name);
                    }
                    break;

                case HpsdMessage.Types.MessageType.ObjectDelete:
                    parsedMessage.Type = MessageType.ObjectDelete;
                    parsedMessage.Federate = message.ObjectDelete.ProducingFederate;
                    parsedMessage.EntityID = message.ObjectDelete.InstanceId;
                    parsedMessage.ObjectName = "HLAobjectRoot." + message.ObjectDelete.ObjectClassName;
                    break;

                case HpsdMessage.Types.MessageType.Interaction:
                    parsedMessage.Type = MessageType.Interaction;
                    parsedMessage.Federate = message.Interaction.ProducingFederate;
                    //parsedMessage.EntityID = message.Interaction.InstanceId;
                    parsedMessage.InteractionName = "HLAinteractionRoot." + message.Interaction.InteractionClassName;
                    break;
            }
            return parsedMessage;
        }
    }
}
