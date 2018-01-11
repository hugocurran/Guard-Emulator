using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;
using System.Xml.Linq;
using System.IO;

namespace UnitTests
{
    [TestClass]
    public class fpdlParserUnitTests
    {
        string solution_dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;

        #region schema tests
        /*
        string fpdlSchema = @"C:\Users\peter\Source\Repos\Guard Emulator\Tests\Sample test data\FpdlPolicyTypes.xsd";
        
        [TestMethod]
        public void TestSchemaLoading1()    // Invalid file
        {
            fpdlParser parser = new fpdlParser();
            if (!parser.LoadSchema("http://www.niteworks.net/fpdl", @"C:\Users\peter\Source\Repos\Guard Emulator\Guard Emulator\Schema\BADFILE.xsd"))
            {
                Assert.AreEqual(@"Could not find file 'C:\Users\peter\Source\Repos\Guard Emulator\Guard Emulator\Schema\BADFILE.xsd'.", parser.errorMsg);
            }
        }

        [TestMethod]
        public void TestSchemaLoading2()    // Invalid schema
        {
            fpdlParser parser = new fpdlParser();
            if (!parser.LoadSchema("http://www.niteworks.net/fpdl", @"C:\Users\peter\Source\Repos\Guard Emulator\Tests\Sample test data\FpdlPolicyTypes.xsd"))
            {
                Assert.AreEqual(@"Schema load error: Could not find file 'C:\Users\peter\Source\Repos\Guard Emulator\Guard Emulator\Schema\BADFILE.xsd'.", parser.errorMsg);
            }
        }

        [TestMethod]
        public void TestSchemaLoading3()    // Valid schema
        {
            fpdlParser parser = new fpdlParser();
            if (!parser.LoadSchema("http://www.niteworks.net/fpdl", fpdlSchema))
            {
                Assert.Fail(parser.errorMsg);
            }
        }
        */
        #endregion

        [TestMethod]
        public void DeployLoading1()    // Valid deploy file
        {
            FpdlParser parser = new FpdlParser();

            if (!parser.LoadDeployDocument(solution_dir + @"\..\Sample test data\Deploy1.xml"))
            {
                Assert.Fail(parser.ErrorMsg);
            }
            Assert.AreEqual("127.0.0.1:5556", parser.ExportSub);
            Assert.AreEqual(OspProtocol.HPSD_ZMQ, parser.Protocol);
            Assert.AreEqual("10.10.10.12", parser.SyslogServerIp);
        }

        [TestMethod]
        public void ExportPolicyGeneration1()
        {
            FpdlParser parser = new FpdlParser();

            if (!parser.LoadDeployDocument(solution_dir + @"\..\Sample test data\Deploy1.xml"))
            {
                Assert.Fail(parser.ErrorMsg);
            }

            XDocument policy = parser.ExportPolicy;
            Assert.IsNotNull(policy);

            Assert.AreEqual("1", policy.Element("exportPolicy").Element("rule").Attribute("ruleNumber").Value);
        }

        [TestMethod]
        public void ImportPolicyGeneration1()
        {
            FpdlParser parser = new FpdlParser();

            if (!parser.LoadDeployDocument(solution_dir + @"\..\Sample test data\Deploy1.xml"))
            {
                Assert.Fail(parser.ErrorMsg);
            }

            XDocument policy = parser.ImportPolicy;
            Assert.IsNotNull(policy);

            Assert.AreEqual("1", policy.Element("importPolicy").Element("rule").Attribute("ruleNumber").Value);
        }
    }
}
