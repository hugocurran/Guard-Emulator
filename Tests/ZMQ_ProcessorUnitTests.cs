using System;
using System.Text;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetMQ;
using NetMQ.Sockets;
using System.Threading.Tasks;
using System.Threading;
using Guard_Emulator;
using Google.Protobuf;
using System.Xml.Linq;
using Tests;
using Hugo.Utility.Syslog;

namespace UnitTests
{
    [TestClass]
    public class ZMQ_ProcessorUnitTests
    {
        // All testing done on loopback if
        //string subscriber = "127.0.0.1:5556";  //Socket that guard will subscribe to for upstream traffic
        //string publisher = "127.0.0.1:5555";   //Socket guard will publish to for downstream traffic

        [TestMethod]
        public void ObjectCreatePolicyProcessing1()
        {
            // Create a matching policy file
            XDocument testPolicy = Harness.CreateEmptyPolicy();

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                Type = MessageType.ObjectCreate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");
            Processor processor = new ZmqProcessor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "1");
        }

        [TestMethod]
        public void ObjectCreatePolicyProcessing2()
        {
            // Create a matching policy file
            XDocument testPolicy = Harness.CreateEmptyPolicy();

            XElement rule =
                new XElement("rule",
                    new XAttribute("ruleNumber", "2"),
                    new XElement("federate", "CGF"),
                    new XElement("entity", "*"),
                    new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                    new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                Type = MessageType.ObjectCreate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");
            Processor processor = new TcpProcessor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "2");
        }

        [TestMethod]
        public void ObjectCreatePolicyProcessing3()
        {
            // Create a matching policy file
            XDocument testPolicy = Harness.CreateEmptyPolicy();

            XElement rule =
                new XElement("rule",
                    new XAttribute("ruleNumber", "2"),
                    new XElement("federate", "CGF"),
                    new XElement("entity", "*"),
                    new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                    new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "*")
);
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                Type = MessageType.ObjectCreate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");
            Processor processor = new TcpProcessor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "3");
        }

        [TestMethod]
        public void ObjectUpdatePolicyProcessing1()
        {
            // Create a matching policy file
            XDocument testPolicy = Harness.CreateEmptyPolicy();

            XElement rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "2"),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                Type = MessageType.ObjectUpdate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };
            intMessage.Attribute.Add("Marking");

            // Create a 'null' processor for testing
            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");
            Processor processor = new ZmqProcessor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "3");
        }

        [TestMethod]
        public void ObjectUpdatePolicyProcessing2()
        {
            // Create a matching policy file
            XDocument testPolicy = Harness.CreateEmptyPolicy();

            XElement rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "2"),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft"),
                new XElement("attributeName", "Marking")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Object Update
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                ObjectName = "HLAobjectRoot.BaseEntity.PhysicalEntity.Aircraft",
                Type = MessageType.ObjectUpdate,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };
            intMessage.Attribute.Add("Type");

            // Create a 'null' processor for testing
            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");
            Processor processor = new TcpProcessor();

            Assert.IsFalse(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "NOMATCH");
        }

        [TestMethod]
        public void InteractionPolicyProcessing1()
        {
            // Create a matching policy file
            XDocument testPolicy = Harness.CreateEmptyPolicy();

            XElement rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "2"),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAinteractionRoot.WeaponFire"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAinteractionRoot.Detonation"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Interaction
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                InteractionName = "HLAinteractionRoot.Detonation",
                Type = MessageType.Interaction,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");
            Processor processor = new ZmqProcessor();

            Assert.IsTrue(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "3");
        }

        [TestMethod]
        public void InteractionPolicyProcessing2()
        {
            // Create a matching policy file
            XDocument testPolicy = Harness.CreateEmptyPolicy();

            XElement rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "2"),
                new XElement("federate", "CGF"),
                new XElement("entity", "*"),
                new XElement("objectName", "HLAinteractionRoot.WeaponFire"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            rule =
                new XElement("rule",
                new XAttribute("ruleNumber", "3"),
                new XElement("federate", "CGF"),
                new XElement("entity", "2B93915C-116C-43F4-BF61-5295FFD5F82A"),
                new XElement("objectName", "HLAinteractionRoot.Detonation"),
                new XElement("attributeName", "*")
            );
            testPolicy.Element("exportPolicy").Add(rule);

            // Create an internal message for an Interaction
            long timeStamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime calcTime = start.AddMilliseconds(timeStamp).ToLocalTime();
            InternalMessage intMessage = new InternalMessage()
            {
                Federate = "CGF",
                EntityID = "2B93915C-116C-43F4-BF61-5295FFD5F82A",
                InteractionName = "HLAinteractionRoot.WeaponFire",
                Type = MessageType.Interaction,
                TimeStamp = calcTime,
                SequenceNumber = 1
            };

            // Create a 'null' processor for testing
            // We need an initialised logger object
            Logger logger = Logger.Instance;
            logger.Initialise(Facility.Local1, "127.0.0.1", "testGuard");
            Processor processor = new TcpProcessor();

            Assert.IsFalse(processor.ApplyPolicy(intMessage, testPolicy));
            Assert.AreEqual(processor.RuleNumber, "NOMATCH");
        }



        }
    }

