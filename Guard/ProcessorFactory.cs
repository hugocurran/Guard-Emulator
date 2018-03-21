using FPDL.Deploy;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Guard_Emulator
{
    internal static class ProcessorFactory
    {
        internal static Processor Create(FpdlParser deploy, string path, CancellationToken token)
        {
            string input, output;
            ModuleOsp.OspProtocol osp = ModuleOsp.OspProtocol.INVALID;
            XElement policy;
            switch (path)
            {
                case "Export":
                    policy = deploy.ExportPolicy;
                    input = deploy.ExportIn;
                    output = deploy.ExportOut;
                    osp = deploy.Protocol;
                    break;
                case "Import":
                    policy = deploy.ImportPolicy;
                    input = deploy.ImportIn;
                    output = deploy.ImportOut;
                    osp = deploy.Protocol;
                    break;
                default:
                    throw new ApplicationException("Path variable incorrectly set");
            }
            switch (osp)
            {
                case ModuleOsp.OspProtocol.HPSD_ZMQ:
                case ModuleOsp.OspProtocol.WebLVC_ZMQ:
                    return new ZmqProcessor(input, output, osp, policy, token);
                case ModuleOsp.OspProtocol.HPSD_TCP:
                case ModuleOsp.OspProtocol.WebLVC_TCP:
                    return new TcpProcessor(input, output, osp, policy, token);
                default:
                    return null;
            }
        }

        /// <summary>
        /// Test interface
        /// </summary>
        /// <param name="input"></param>
        /// <param name="output"></param>
        /// <param name="osp"></param>
        /// <param name="policy"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        internal static Processor Create(string input, string output, ModuleOsp.OspProtocol osp, XElement policy, CancellationToken token)
        {
            switch (osp)
            {
                case ModuleOsp.OspProtocol.HPSD_ZMQ:
                case ModuleOsp.OspProtocol.WebLVC_ZMQ:
                    return new ZmqProcessor(input, output, osp, policy, token);
                case ModuleOsp.OspProtocol.HPSD_TCP:
                case ModuleOsp.OspProtocol.WebLVC_TCP:
                    return new TcpProcessor(input, output, osp, policy, token);
                default:
                    return null;
            }
        }
    }
}
