using System;
using System.Collections.Generic;
using System.Text;
using System.Json;

namespace Guard_Emulator
{
    public static class WeblvcParser
    {
        enum webLvcOperation
        {
            NOOP = 0,
            Interaction = 1,
            Create = 2,
            Update = 3,
            Delete = 4
        }

    public static InternalMessage ParseMessage(byte[] message)
    {
        InternalMessage parsedMessage = new InternalMessage();

            JsonObject lvcMessage = (JsonObject)JsonValue.Parse(Encoding.ASCII.GetString(message).TrimEnd(Convert.ToChar(0x00)));

            JsonObject cdsAdmin = (JsonObject)lvcMessage["cdsAdmin"];
            parsedMessage.SequenceNumber = cdsAdmin["Sequence"];
            parsedMessage.TimeStamp = DateTimeOffset.UtcNow;
            webLvcOperation mesgType = (webLvcOperation)Enum.Parse(typeof(webLvcOperation), cdsAdmin["Operation"].ToString());

            switch (mesgType)
            {
                case webLvcOperation.Create:
                    parsedMessage.Type = MessageType.ObjectCreate;
                    parsedMessage.Federate = cdsAdmin["Origin"];
                    parsedMessage.EntityID = cdsAdmin["ObjectId"];
                    parsedMessage.ObjectName = cdsAdmin["ObjectModelPath"];
                    break;

                case webLvcOperation.Delete:
                    parsedMessage.Type = MessageType.ObjectDelete;
                    parsedMessage.Federate = cdsAdmin["Origin"];
                    parsedMessage.EntityID = cdsAdmin["ObjectId"];
                    parsedMessage.ObjectName = cdsAdmin["ObjectModelPath"];
                    break;

                case webLvcOperation.Update:
                   
                    parsedMessage.Type = MessageType.ObjectUpdate;
                    parsedMessage.Federate = cdsAdmin["Origin"];
                    parsedMessage.EntityID = cdsAdmin["ObjectId"];
                    parsedMessage.ObjectName = cdsAdmin["ObjectModelPath"];
                    JsonObject attributes = (JsonObject)lvcMessage["Attributes"];
                    foreach (var attrib in attributes)
                    {
                            parsedMessage.Attribute.Add(attrib.Key);
                    }
                    break;

                case webLvcOperation.Interaction:
                    parsedMessage.Type = MessageType.Interaction;
                    parsedMessage.Federate = cdsAdmin["Origin"];
                    parsedMessage.EntityID = cdsAdmin["ObjectId"];
                    parsedMessage.InteractionName = cdsAdmin["ObjectModelPath"];
                    break;

                case webLvcOperation.NOOP:
                    parsedMessage.Type = MessageType.Status;
                    break;
            }
            return parsedMessage;
        }
    }
}
