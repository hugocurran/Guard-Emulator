using System;
using System.Collections.Generic;
using System.Text;
using Guard_Emulator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    [TestClass]
    public class ProcessorFactoryUnitTests
    {
        [TestMethod]
        public void CreateZMQProcessor()
        {
            string subscriber = "127.0.0.1:5556";
            string publisher = "127.0.0.1:5555";

            // Create an empty policy file
            XDocument testPolicy = new XDocument();
            XElement emptyPolicy =
                new XElement("exportPolicy",
                    new XElement("rule",
                        new XAttribute("ruleNumber", "1"),
                        new XElement("federate", "*"),
                        new XElement("entity", "*"),
                        new XElement("objectName", "*"),
                        new XElement("attributeName", "*"))
            );
            testPolicy.Add(emptyPolicy);

            // Processor runs in a task so needs a token
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            Processor processorObj = ProcessorFactory.Create(subscriber, publisher, OspProtocol.HPSD_ZMQ, testPolicy, token);
            Assert.IsInstanceOfType(processorObj, typeof(ZmqProcessor));

            processorObj = ProcessorFactory.Create(subscriber, publisher, OspProtocol.HPSD_TCP, testPolicy, token);
            Assert.IsInstanceOfType(processorObj, typeof(TcpProcessor));

            tokenSource.Dispose();

        }
    }
}
