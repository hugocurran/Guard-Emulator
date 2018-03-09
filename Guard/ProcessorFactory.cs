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
                    break;
                case "Import":
                    policy = deploy.ImportPolicy;
                    input = deploy.ImportIn;
                    output = deploy.ImportOut;
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
    }
}
