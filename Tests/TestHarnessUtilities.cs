using Google.Protobuf;
using Guard_Emulator;
using System;
using System.Collections.Generic;
using System.Json;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Tests
{
    static class Harness
    {
        /// <summary>
        /// Policy that permits anything
        /// </summary>
        /// <returns></returns>
        public static XElement CreateEmptyPolicy()
        {
            // Create an empty policy file
            XElement emptyPolicy =
                new XElement("exportPolicy",
                    new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*"))
            );
            return emptyPolicy;
        }

        /// <summary>
        /// Create an IPEndpoint from a string
        /// </summary>
        /// <param name="addrPort">IpAddress:Port</param>
        /// <returns>IPEndpoint</returns>
        public static IPEndPoint EndPoint(string addrPort)
        {
            // Server comes from FPDL in the form <IP Address>:<Port>
            string[] parts = addrPort.Split(":");
            IPAddress ipAddress = IPAddress.Parse(parts[0]);
            Int32 port = Convert.ToInt32(parts[1]);
            return new IPEndPoint(ipAddress, port);
        }

        public static byte[] HPSD_StatusMessage(int sequence)
        {
            // Create an HPSD Status message for testing
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            HpsdMessage statusMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = sequence,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.SessionStatus,
                SessionStatus = new SessionStatus()
                {
                    Active = true,
                    SessionName = "ThisSession"
                }
            };
            return statusMessage.ToByteArray();
        }

        public static byte[] WebLVC_StatusMessage(int sequence)
        {
            // Create a WebLVC 'status' message - note this is not defined, so we simply send a NOOP message

            //"{ "cdsAdmin":{ "Format":"JSON","ObjectId":"VRF262147:167","ObjectModelId":"6237bd4c-0601-40cd-9a03-44bc247d8498","ObjectModelPath":"","Operation":0,"Origin":"Test","PolicyId":"f9771db7-20b0-4e4b-8e78-26cbe6dab1d0","Sequence":1} }"

            JsonObject cdsAdmin = new JsonObject();
            cdsAdmin.Add("Format", new JsonPrimitive("JSON"));
            cdsAdmin.Add("ObjectId", new JsonPrimitive("VRF262147:167"));
            cdsAdmin.Add("ObjectModelId", new JsonPrimitive("6237bd4c-0601-40cd-9a03-44bc247d8498"));
            cdsAdmin.Add("ObjectModelPath", new JsonPrimitive(""));
            cdsAdmin.Add("Operation", new JsonPrimitive(0));    // NOOP
            cdsAdmin.Add("Origin", new JsonPrimitive("Test"));
            cdsAdmin.Add("PolicyId", new JsonPrimitive("f9771db7-20b0-4e4b-8e78-26cbe6dab1d0"));
            cdsAdmin.Add("Sequence", new JsonPrimitive(sequence));

            JsonObject mesg = new JsonObject();
            mesg.Add("cdsAdmin", cdsAdmin);

            Console.WriteLine("StatusMessage: {0}", mesg);
            return Encoding.ASCII.GetBytes(mesg.ToString());           
        }

        public static byte[] WebLVC_UpdateMessage(int sequence)
        {
            // Create a WebLVC update message

            JsonObject attributes = new JsonObject
            {
                { "DamageState", new JsonPrimitive(0) },
                { "EngineSmokeOn", new JsonPrimitive(false) },
                { "EntityIdentifier", new JsonArray(new JsonPrimitive(1), new JsonPrimitive(3001), new JsonPrimitive(258)) },
//\"EntityType\":[3,1,44,1,32,1,0],\"FirePowerDisabled\":0,\"FlamesPresent\":false,\"ForceIdentifier\":2,\"Immobilized\":0,\"Marking\":\"R 2\",\"SmokePlumePresent\":false,\"Spatial\":{\"AccelerationVector\":[0.0,0.0,0.0],\"AngularVelocity\":[0.0,0.0,0.0],\"DeadReckoningAlgorithm\":2,\"IsFrozen\":false,\"Orientation\":[-0.48604518175125067,0.20816335082054138,1.7494438886642456],\"Velocity\":[1.3002797365188599,-0.67837601900100708,-0.30837112665176392],\"WorldLocation\":[3139561.6843168521,5441061.288272094,1101651.3013419574]}
            };

            JsonObject cdsAdmin = new JsonObject {
                { "Format", new JsonPrimitive("JSON") },
                { "ObjectId", new JsonPrimitive("VRF262147:167") },
                { "ObjectModelId", new JsonPrimitive("6237bd4c-0601-40cd-9a03-44bc247d8498") },
                { "ObjectModelPath", new JsonPrimitive("") },
                { "Operation", new JsonPrimitive(3) },
                { "Origin", new JsonPrimitive("Test") },
                { "PolicyId", new JsonPrimitive("f9771db7-20b0-4e4b-8e78-26cbe6dab1d0") },
                { "Sequence", new JsonPrimitive(sequence) }
        };

            JsonObject mesg = new JsonObject();
            mesg.Add("Attributes", attributes);
            mesg.Add("cdsAdmin", cdsAdmin);

            Console.WriteLine("updateMessage: {0}", mesg);
            return Encoding.ASCII.GetBytes(mesg.ToString());
        }
    }
}
