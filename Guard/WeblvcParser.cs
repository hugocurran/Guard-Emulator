using System;
using System.Collections.Generic;
using System.Text;
using System.Json;

namespace Guard_Emulator
{
    public static class WeblvcParser
    {
        enum WebLvcOperation
        {
            Status = 0,
            Interaction = 1,
            Create = 2,
            Update = 3,
            Delete = 4
        }

    public static InternalMessage ParseMessage(byte[] message)
    {
        InternalMessage parsedMessage = new InternalMessage();

            // Dodgy characters at the end
            char[] crud = new char[] { '\x0000', '\x000a' };

            //Console.WriteLine("raw message: {0} len={1}", Encoding.ASCII.GetString(message), Encoding.ASCII.GetString(message).Length);
            //Console.WriteLine("new message: {0} len={1}", Encoding.ASCII.GetString(message).TrimEnd(crud), Encoding.ASCII.GetString(message).TrimEnd(crud).Length);
            JsonObject lvcMessage = (JsonObject)JsonValue.Parse(Encoding.ASCII.GetString(message).TrimEnd(crud));

            //Console.WriteLine("back from the parser");

            JsonObject cdsAdmin = (JsonObject)lvcMessage["cdsAdmin"];
            parsedMessage.SequenceNumber = cdsAdmin["Sequence"];
            //parsedMessage.TimeStamp = cdsAdmin["TimeStamp"];
            parsedMessage.TimeStamp = DateTimeOffset.FromUnixTimeMilliseconds((long)cdsAdmin["TimeStamp"]/1000);
            parsedMessage.SessionActive = true;
            parsedMessage.SessionName = cdsAdmin["Origin"];
            WebLvcOperation mesgType = (WebLvcOperation)Enum.Parse(typeof(WebLvcOperation), cdsAdmin["Operation"].ToString());

            switch (mesgType)
            {
                case WebLvcOperation.Create:
                    parsedMessage.Type = MessageType.ObjectCreate;
                    parsedMessage.Federate = cdsAdmin["Origin"];
                    parsedMessage.EntityID = cdsAdmin["ObjectId"];
                    parsedMessage.ObjectName = cdsAdmin["ObjectModelPath"];
                    break;

                case WebLvcOperation.Delete:
                    parsedMessage.Type = MessageType.ObjectDelete;
                    parsedMessage.Federate = cdsAdmin["Origin"];
                    parsedMessage.EntityID = cdsAdmin["ObjectId"];
                    parsedMessage.ObjectName = cdsAdmin["ObjectModelPath"];
                    break;

                case WebLvcOperation.Update:
                   
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

                case WebLvcOperation.Interaction:
                    parsedMessage.Type = MessageType.Interaction;
                    parsedMessage.Federate = cdsAdmin["Origin"];
                    parsedMessage.EntityID = cdsAdmin["ObjectId"];
                    parsedMessage.InteractionName = cdsAdmin["ObjectModelPath"];
                    break;

                case WebLvcOperation.Status:
                    parsedMessage.Type = MessageType.Status;
                    break;
            }
            return parsedMessage;
        }
    }
}
