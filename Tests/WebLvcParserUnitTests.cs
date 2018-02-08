using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;
using System.Xml.Linq;
using System;
//using System.Json;
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
            string jsonString = "{ \"cdsAdmin\":{ \"Format\":\"JSON\",\"ObjectId\":\"VRF131073:2\",\"ObjectModelId\":\"00000000-0000-0000-0000-000000000000\",\"ObjectModelPath\":\"HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.GroundVehicle\",\"Operation\":3,\"Origin\":\"Test\",\"PolicyId\":\"52769b0b-a240-45e2-972c-ed3b6307f71d\",\"Sequence\":2}}";
        //string jsonString = "{ \"cdsAdmin\":{ \"Format\":\"JSON\",\"ObjectId\":\"VRF262147:167\",\"ObjectModelId\":\"6237bd4c-0601-40cd-9a03-44bc247d8498\",\"ObjectModelPath\":\"HLAobjectRoot.BaseEntity.PhysicalEntity.Platform\",\"Operation\":2,\"Origin\":\"Test\",\"PolicyId\":\"f9771db7-20b0-4e4b-8e78-26cbe6dab1d0\",\"Sequence\":4744} }";

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
            //string jsonString = 
            //    "{\"Attributes\":" +
            //    "{ \"DamageState\":0,\"EngineSmokeOn\":false,\"EntityIdentifier\":[1,3001,378],\"EntityType\":[2,6,225,3,8,4,0],\"FirePowerDisabled\":0,\"FlamesPresent\":false,\"ForceIdentifier\":1,\"Immobilized\":0,\"Marking\":\"Mk-65 19\",\"SmokePlumePresent\":false,\"Spatial\":{\"AccelerationVector\":[0.0,0.0,0.0],\"AngularVelocity\":[0.0,0.0,0.0],\"DeadReckoningAlgorithm\":2,\"IsFrozen\":false,\"Orientation\":[-2.0945155620574951,-1.3958266973495483,-3.1163771152496298],\"Velocity\":[0.0,0.0,0.0],\"WorldLocation\":[3141364.4598075533,5439446.9609799786,1102972.4341654007]}}," +
            //    "\"cdsAdmin\":" +
            //    "{\"Format\":\"JSON\",\"ObjectId\":\"VRF262147:167\",\"ObjectModelId\":\"6237bd4c-0601-40cd-9a03-44bc247d8498\",\"ObjectModelPath\":\"HLAobjectRoot.BaseEntity.PhysicalEntity.Platform\",\"Operation\":3,\"Origin\":\"Test\",\"PolicyId\":\"f9771db7-20b0-4e4b-8e78-26cbe6dab1d0\",\"Sequence\":4743}}";


            string jsonString = "{\"Attributes\":{\"DamageState\":0,\"EngineSmokeOn\":false,\"EntityIdentifier\":[1,3001,258],\"EntityType\":[3,1,44,1,32,1,0],\"FirePowerDisabled\":0,\"FlamesPresent\":false,\"ForceIdentifier\":2,\"Immobilized\":0,\"Marking\":\"R 2\",\"SmokePlumePresent\":false,\"Spatial\":{\"AccelerationVector\":[0.0,0.0,0.0],\"AngularVelocity\":[0.0,0.0,0.0],\"DeadReckoningAlgorithm\":2,\"IsFrozen\":false,\"Orientation\":[-0.48604518175125067,0.20816335082054138,1.7494438886642456],\"Velocity\":[1.3002797365188599,-0.67837601900100708,-0.30837112665176392],\"WorldLocation\":[3139561.6843168521,5441061.288272094,1101651.3013419574]}},\"cdsAdmin\":{\"Format\":\"JSON\",\"ObjectId\":\"VRF262147:115\",\"ObjectModelId\":\"572a7bdd-e48a-4910-a927-c1183516ce2a\",\"ObjectModelPath\":\"HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.GroundVehicle\",\"Operation\":3,\"Origin\":\"Test\",\"PolicyId\":\"37a7cc03-a212-41ee-821e-afb59896f043\",\"Sequence\":23229}}";
 

            InternalMessage parsed = WeblvcParser.ParseMessage(Encoding.ASCII.GetBytes(jsonString));

            Assert.AreEqual(23229, parsed.SequenceNumber);
            Assert.AreEqual(MessageType.ObjectUpdate, parsed.Type);
            Assert.AreEqual("Test", parsed.Federate);
            Assert.AreEqual("HLAobjectRoot.BaseEntity.PhysicalEntity.Platform.GroundVehicle", parsed.ObjectName);
            Assert.AreEqual("VRF262147:115", parsed.EntityID);
            Assert.AreEqual(11, parsed.Attribute.Count);
            Assert.AreEqual("EngineSmokeOn", parsed.Attribute[1]);
        }
    }
}