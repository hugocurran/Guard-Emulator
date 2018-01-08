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

            JsonObject lvcMessage = (JsonObject)JsonObject.Parse(System.Text.Encoding.UTF8.GetString(message));

            JsonObject cdsAdmin = (JsonObject)lvcMessage["cdsAdmin"];
            parsedMessage.Federate = cdsAdmin["Origin"];
            parsedMessage.EntityID = cdsAdmin["ObjectId"];
            parsedMessage.SequenceNumber = cdsAdmin["Sequence"];
            webLvcOperation bar = (webLvcOperation)Enum.Parse(typeof(webLvcOperation), cdsAdmin["Operation"].ToString());

            switch(bar)
            {
                case webLvcOperation.Create:
                    parsedMessage.Type = MessageType.ObjectCreate;
                    parsedMessage.ObjectName = cdsAdmin["ObjectModelPath"];
                    break;

                case webLvcOperation.Delete:
                    parsedMessage.Type = MessageType.ObjectDelete;
                    parsedMessage.ObjectName = cdsAdmin["ObjectModelPath"];
                    break;

                case webLvcOperation.Update:
                    parsedMessage.Type = MessageType.ObjectUpdate;
                    parsedMessage.ObjectName = cdsAdmin["ObjectModelPath"];
                    JsonObject attributes = (JsonObject)lvcMessage["Attributes"];
                    foreach (var attrib in attributes)
                    {
                            parsedMessage.Attribute.Add(attrib.Key);
                    }
                    break;

                case webLvcOperation.Interaction:
                    parsedMessage.Type = MessageType.Interaction;
                    parsedMessage.InteractionName = cdsAdmin["ObjectModelPath"];
                    break;
            }
            return parsedMessage;
        }
    }
}
