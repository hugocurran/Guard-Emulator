using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;
using System.Xml.Linq;
using System;
using System.Json;
using Google.Protobuf;
using System.Text;
using System.Collections.Generic;

namespace UnitTests
{
    [TestClass]
    public class WeblvcParserUnitTests
    {

        [TestMethod]
        public void WeblvcObjectCreateMessageParsing()
        {
            string jsonString = "{ \"cdsAdmin\":{ \"Format\":\"JSON\",\"ObjectId\":\"VRF262147:167\",\"ObjectModelId\":\"6237bd4c-0601-40cd-9a03-44bc247d8498\",\"ObjectModelPath\":\"HLAobjectRoot.BaseEntity.PhysicalEntity.Platform\",\"Operation\":2,\"Origin\":\"Test\",\"PolicyId\":\"f9771db7-20b0-4e4b-8e78-26cbe6dab1d0\",\"Sequence\":4744} }";

            InternalMessage parsed = WeblvcParser.ParseMessage(Encoding.ASCII.GetBytes(jsonString));

            Assert.AreEqual(4744, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.ObjectCreate, parsed.Type);
            Assert.AreEqual("Test", parsed.Federate);
            Assert.AreEqual("HLAobjectRoot.BaseEntity.PhysicalEntity.Platform", parsed.ObjectName);
            Assert.AreEqual("VRF262147:167", parsed.EntityID);
        }

        [TestMethod]
        public void WeblvcObjectDeleteMessageParsing()
        {
            string jsonString = "{ \"cdsAdmin\":{ \"Format\":\"JSON\",\"ObjectId\":\"VRF262147:167\",\"ObjectModelId\":\"6237bd4c-0601-40cd-9a03-44bc247d8498\",\"ObjectModelPath\":\"HLAobjectRoot.BaseEntity.PhysicalEntity.Platform\",\"Operation\":4,\"Origin\":\"Test\",\"PolicyId\":\"f9771db7-20b0-4e4b-8e78-26cbe6dab1d0\",\"Sequence\":4744} }";

            InternalMessage parsed = WeblvcParser.ParseMessage(Encoding.ASCII.GetBytes(jsonString));

            Assert.AreEqual(4744, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.ObjectDelete, parsed.Type);
            Assert.AreEqual("Test", parsed.Federate);
            Assert.AreEqual("HLAobjectRoot.BaseEntity.PhysicalEntity.Platform", parsed.ObjectName);
            Assert.AreEqual("VRF262147:167", parsed.EntityID);
        }

        [TestMethod]
        public void weblvcObjectUpdateMessageParsing()
        {
            string jsonString = 
                "{ \"Attributes\":" +
                "{ \"DamageState\":0,\"EngineSmokeOn\":false,\"EntityIdentifier\":[1,3001,378],\"EntityType\":[2,6,225,3,8,4,0],\"FirePowerDisabled\":0,\"FlamesPresent\":false,\"ForceIdentifier\":1,\"Immobilized\":0,\"Marking\":\"Mk-65 19\",\"SmokePlumePresent\":false,\"Spatial\":{\"AccelerationVector\":[0.0,0.0,0.0],\"AngularVelocity\":[0.0,0.0,0.0],\"DeadReckoningAlgorithm\":2,\"IsFrozen\":false,\"Orientation\":[-2.0945155620574951,-1.3958266973495483,-3.1163771152496298],\"Velocity\":[0.0,0.0,0.0],\"WorldLocation\":[3141364.4598075533,5439446.9609799786,1102972.4341654007] } }," +
                "\"cdsAdmin\":" +
                "{\"Format\":\"JSON\",\"ObjectId\":\"VRF262147:167\",\"ObjectModelId\":\"6237bd4c-0601-40cd-9a03-44bc247d8498\",\"ObjectModelPath\":\"HLAobjectRoot.BaseEntity.PhysicalEntity.Platform\",\"Operation\":3,\"Origin\":\"Test\",\"PolicyId\":\"f9771db7-20b0-4e4b-8e78-26cbe6dab1d0\",\"Sequence\":4743}}";

            InternalMessage parsed = WeblvcParser.ParseMessage(Encoding.ASCII.GetBytes(jsonString));

            Assert.AreEqual(4743, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.ObjectUpdate, parsed.Type);
            Assert.AreEqual("Test", parsed.Federate);
            Assert.AreEqual("HLAobjectRoot.BaseEntity.PhysicalEntity.Platform", parsed.ObjectName);
            Assert.AreEqual("VRF262147:167", parsed.EntityID);
            Assert.AreEqual(11, parsed.Attribute.Count);
            Assert.AreEqual("EngineSmokeOn", parsed.Attribute[1]);
        }
    }
}