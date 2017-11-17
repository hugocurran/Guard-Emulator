using System;
using System.Collections.Generic;
using System.Text;

namespace Guard_Emulator
{
    public static class HpsdParser
    {
        public static InternalMessage ParseMessage(HpsdMessage message)
        {
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
                    parsedMessage.ObjectName = message.ObjectCreate.ObjectClassName;
                    break;

                case HpsdMessage.Types.MessageType.ObjectUpdate:
                    parsedMessage.Type = MessageType.ObjectUpdate;
                    parsedMessage.Federate = message.ObjectUpdate.ProducingFederate;
                    parsedMessage.EntityID = message.ObjectUpdate.InstanceId;
                    parsedMessage.ObjectName = message.ObjectUpdate.ObjectClassName;
                    foreach (var attrib in message.ObjectUpdate.Attributes)
                    {
                        parsedMessage.Attribute.Add(attrib.Name);
                    }
                    break;

                case HpsdMessage.Types.MessageType.ObjectDelete:
                    parsedMessage.Type = MessageType.ObjectDelete;
                    parsedMessage.Federate = message.ObjectDelete.ProducingFederate;
                    parsedMessage.EntityID = message.ObjectDelete.InstanceId;
                    parsedMessage.ObjectName = message.ObjectDelete.ObjectClassName;
                    break;

                case HpsdMessage.Types.MessageType.Interaction:
                    parsedMessage.Type = MessageType.Interaction;
                    parsedMessage.Federate = message.Interaction.ProducingFederate;
                    //parsedMessage.EntityID = message.Interaction.InstanceId;
                    parsedMessage.InteractionName = message.Interaction.InteractionClassName;
                    break;
            }
            return parsedMessage;
        }
    }
}
