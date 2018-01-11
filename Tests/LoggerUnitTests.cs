using Microsoft.VisualStudio.TestTools.UnitTesting;
using Guard_Emulator;
using System.Xml.Linq;
using System.IO;

namespace UnitTests
{
    [TestClass]
    public class loggerUnitTests
    {
        string solution_dir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;


        [TestMethod]
        public void LoggerInitialisation()    // 
        {
            Logger logger = Logger.Instance;
            logger.Initialise("127.0.0.1");

            Assert.IsTrue(logger.IsInitialised);

            logger.Stop();
        }

        [TestMethod]
        public void LoggerSingletonConfirmation()    // 
        {
            Logger logger = Logger.Instance;
            Logger xlogger = Logger.Instance;
            Logger ylogger = Logger.Instance;

            Assert.AreSame(logger, xlogger);
            Assert.AreSame(logger, ylogger);
            Assert.AreSame(ylogger, xlogger);

            logger.Stop();
        }
    }
}


