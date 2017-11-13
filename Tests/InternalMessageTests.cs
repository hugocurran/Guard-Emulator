using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;

namespace UnitTests
{
    [TestClass]
    public class InternalMessageTests
    {


        [TestMethod]
        public void TestCreation()
        {
            internalMessage imesg = new internalMessage();
            imesg.type = messageType.ObjectUpdate;
            imesg.federate = "foo";
            imesg.entityID = "bar";
            imesg.objectName = "some.object";
            imesg.attribute.Add("attrib1");
            imesg.attribute.Add("attrib2");

            Assert.AreEqual(messageType.ObjectUpdate, imesg.type);
            Assert.AreEqual("foo", imesg.federate);
            Assert.AreEqual("bar", imesg.entityID);
            Assert.AreEqual("some.object", imesg.objectName);
            Assert.AreEqual("attrib1", imesg.attribute[0]);
            Assert.AreEqual("attrib2", imesg.attribute[1]);
        }
    }
}
