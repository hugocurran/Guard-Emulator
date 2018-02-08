using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;
using System;

namespace UnitTests
{
    [TestClass]
    public class InternalMessageTests
    {


        [TestMethod]
        public void InternalMessageCreation()
        {
            InternalMessage imesg = new InternalMessage();
            imesg.Type = MessageType.ObjectUpdate;
            imesg.Federate = "foo";
            imesg.EntityID = "bar";
            imesg.ObjectName = "some.object";
            imesg.Attribute.Add("attrib1");
            imesg.Attribute.Add("attrib2");

            Assert.AreEqual(MessageType.ObjectUpdate, imesg.Type);
            Assert.AreEqual("foo", imesg.Federate);
            Assert.AreEqual("bar", imesg.EntityID);
            Assert.AreEqual("some.object", imesg.ObjectName);
            Assert.AreEqual("attrib1", imesg.Attribute[0]);
            Assert.AreEqual("attrib2", imesg.Attribute[1]);
        }

        [TestMethod]
        public void InternalMessageToString()
        {
            DateTime now = DateTime.Now;
            DateTimeOffset time = new DateTimeOffset(now);

            InternalMessage imesg = new InternalMessage();
            imesg.TimeStamp = new DateTimeOffset(now);
            imesg.SequenceNumber = 42;
            imesg.Type = MessageType.ObjectUpdate;
            imesg.Federate = "foo";
            imesg.EntityID = "bar";
            imesg.ObjectName = "some.object";
            imesg.Attribute.Add("attrib1");
            imesg.Attribute.Add("attrib2");

            string expected = "Type: ObjectUpdate TimeStamp: " + time.ToUnixTimeMilliseconds().ToString() + " Sequence: 42 Federate: foo Entity: bar ObjectName: some.object Attributes: attrib1, attrib2, ";

            Assert.AreEqual(expected, imesg.ToString());
        }
    }
}
