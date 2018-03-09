using FPDL.Deploy;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Guard_Emulator
{
    public static class ProcessorFactory
    {
        public static Processor Create(string subscribe, string publish, ModuleOsp.OspProtocol osp, XElement policy, CancellationToken token)
        {
            switch (osp)
            {
                case ModuleOsp.OspProtocol.HPSD_ZMQ:
                case ModuleOsp.OspProtocol.WebLVC_ZMQ:
                    return new ZmqProcessor(subscribe, publish, osp, policy, token);
                case ModuleOsp.OspProtocol.HPSD_TCP:
                case ModuleOsp.OspProtocol.WebLVC_TCP:
                    // 'subscribe' for TCP processor is the (upstream) addr:port the processor listens on
                    // 'publish' fopr the TCP processor is (downstream) addr:port the process connects to
                    return new TcpProcessor(subscribe, publish, osp, policy, token);
                default:
                    return null;
            }
        }
    }
}
