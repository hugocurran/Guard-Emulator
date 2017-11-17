using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;
using System.Xml.Linq;

namespace UnitTests
{
    [TestClass]
    public class fpdlParserUnitTests
    {
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

        [TestMethod]
        public void TestDeployLoading1()    // Valid deploy file
        {
            //string fpdlSchemaLocation = @"C:\Users\peter\Source\Repos\Guard Emulator\Guard Emulator\Schema\";

            fpdlParser parser = new fpdlParser();
            /*
            if (!parser.LoadSchema("http://www.niteworks.net/fpdl", fpdlSchemaLocation))
            {
                Assert.Fail(parser.errorMsg);
            }
            */

            if (!parser.LoadDeployDocument(@"C:\Users\peter\Source\Repos\Guard Emulator\Tests\Sample test data\Deploy1.xml"))
            {
                Assert.Fail(parser.errorMsg);
            }
        }

        [TestMethod]
        public void TestExportPolicyGeneration1()
        {
            fpdlParser parser = new fpdlParser();

            if (!parser.LoadDeployDocument(@"C:\Users\peter\Source\Repos\Guard Emulator\Tests\Sample test data\Deploy1.xml"))
            {
                Assert.Fail(parser.errorMsg);
            }

            XDocument foo = parser.ExportPolicy();
        }
    }
}
