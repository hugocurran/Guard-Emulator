using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;
using System.Xml.Linq;
using System;
using Google.Protobuf;
using System.Text;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class HpsdParserUnitTests
    {
        
        [TestMethod]
        public void HPSDStatusMessageParsing()
        {
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();


            HpsdMessage statusMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = 1,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.SessionStatus,
                SessionStatus = new SessionStatus()
                {
                    Active = true,
                    SessionName = "ThisSession"
                }
            };

            InternalMessage parsed = HpsdParser.ParseMessage(statusMessage);

            // Message header
            Assert.AreEqual(calcTime, parsed.TimeStamp);
            Assert.AreEqual(1, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.Status, parsed.Type);

            // Message body
            Assert.IsTrue(parsed.SessionActive);
            Assert.AreEqual("ThisSession", parsed.SessionName);
        }

        [TestMethod]
        public void HPSDObjectCreateMessageParsing()
        {
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            string instanceId = "388AED46-4033-49C5-BA0D-8B6F8865D8C1";

            HpsdMessage objectCreateMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = 1,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.ObjectCreate,
                ObjectCreate = new ObjectCreate()
                {
                    ProducingFederate = "CGF",
                    ObjectClassName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.Aircraft",
                    InstanceId = instanceId,
                    InstanceName = "Eric"
                }
            };

            InternalMessage parsed = HpsdParser.ParseMessage(objectCreateMessage);

            // Message header
            Assert.AreEqual(calcTime, parsed.TimeStamp);
            Assert.AreEqual(1, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.ObjectCreate, parsed.Type);

            // Message body
            Assert.AreEqual("CGF", parsed.Federate);
            Assert.AreEqual("HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.Aircraft", parsed.ObjectName);
            Assert.AreEqual(instanceId, parsed.EntityID);
        }

        [TestMethod]
        public void HPSDObjectDeleteMessageParsing()
        {
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            string instanceId = "388AED46-4033-49C5-BA0D-8B6F8865D8C1";

            HpsdMessage objectDeleteMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = 1,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.ObjectDelete,
                ObjectDelete = new ObjectDelete()
                {
                    ProducingFederate = "CGF",
                    ObjectClassName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.Aircraft",
                    InstanceId = instanceId,
                }
            };

            InternalMessage parsed = HpsdParser.ParseMessage(objectDeleteMessage);

            // Message header
            Assert.AreEqual(calcTime, parsed.TimeStamp);
            Assert.AreEqual(1, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.ObjectDelete, parsed.Type);

            // Message body
            Assert.AreEqual("CGF", parsed.Federate);
            Assert.AreEqual("HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.Aircraft", parsed.ObjectName);
            Assert.AreEqual(instanceId, parsed.EntityID);
        }

        [TestMethod]
        public void HPSDObjectUpdateMessageParsing()
        {
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            string instanceId = "388AED46-4033-49C5-BA0D-8B6F8865D8C1";

            List<NamedValue> attribs = new List<NamedValue>();
            attribs.Add(new NamedValue { Name = "DamageState", Value = ByteString.CopyFrom("0", Encoding.Unicode) });
            attribs.Add(new NamedValue { Name = "EngineSmokeOn", Value = ByteString.CopyFrom("false", Encoding.Unicode) });
            attribs.Add(new NamedValue { Name = "AccelerationVector", Value = ByteString.CopyFrom("0.0,0.0,0.0", Encoding.Unicode) });

            HpsdMessage objectUpdateMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = 1,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.ObjectUpdate,
                ObjectUpdate = new ObjectUpdate()
                {
                    ProducingFederate = "CGF",
                    ObjectClassName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.Aircraft",
                    InstanceId = instanceId
                }
            };
            objectUpdateMessage.ObjectUpdate.Attributes.Add(attribs);
                
            InternalMessage parsed = HpsdParser.ParseMessage(objectUpdateMessage);

            // Message header
            Assert.AreEqual(calcTime, parsed.TimeStamp);
            Assert.AreEqual(1, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.ObjectUpdate, parsed.Type);

            // Message body
            Assert.AreEqual("CGF", parsed.Federate);
            Assert.AreEqual("HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.Aircraft", parsed.ObjectName);
            Assert.AreEqual(instanceId, parsed.EntityID);
            Assert.AreEqual(3, parsed.Attribute.Count);
            Assert.AreEqual("EngineSmokeOn", parsed.Attribute[1]);
        }

        [TestMethod]
        public void HPSDInteractionMessageParsing()
        {
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            //string instanceId = "388AED46-4033-49C5-BA0D-8B6F8865D8C1";

            /*
            List<NamedValue> parameters = new List<NamedValue>();
            parameters.Add(new NamedValue { Name = "DamageState", Value = ByteString.CopyFrom("0", Encoding.Unicode) });
            parameters.Add(new NamedValue { Name = "EngineSmokeOn", Value = ByteString.CopyFrom("false", Encoding.Unicode) });
            parameters.Add(new NamedValue { Name = "AccelerationVector", Value = ByteString.CopyFrom("0.0,0.0,0.0", Encoding.Unicode) });
            */

            HpsdMessage interactionMessage = new HpsdMessage()
            {
                ProtocolVersion = 81,
                SequenceNumber = 1,
                Timestamp = timeStamp,
                MessageType = HpsdMessage.Types.MessageType.Interaction,
                Interaction = new Interaction()
                {
                    ProducingFederate = "CGF",
                    InteractionClassName = "HLAinteractionRoot.Happening",
                    //InstanceId = instanceId,
                }
            };
            interactionMessage.Interaction.Parameters = new NamedValue { Name = "A Thing", Value = ByteString.CopyFrom("42", Encoding.Unicode) };

            InternalMessage parsed = HpsdParser.ParseMessage(interactionMessage);

            // Message header
            Assert.AreEqual(calcTime, parsed.TimeStamp);
            Assert.AreEqual(1, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.Interaction, parsed.Type);

            // Message body
            Assert.AreEqual("CGF", parsed.Federate);
            Assert.AreEqual("HLAinteractionRoot.Happening", parsed.InteractionName);
            //Assert.AreEqual(instanceId, parsed.EntityID);
        }
    }
}