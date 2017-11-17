using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;

namespace UnitTests
{
    [TestClass]
    public class InternalMessageTests
    {


        [TestMethod]
        public void TestInternalMessageCreation()
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
    }
}
