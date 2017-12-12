using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Guard_Emulator
{
    public static class ProcessorFactory
    {
        public static Processor Create(string subscribe, string publish, OspProtocol osp, XDocument policy, CancellationToken token)
        {
            switch (osp)
            {
                case OspProtocol.HPSD_ZMQ:
                case OspProtocol.WebLVC_ZMQ:
                    return new ZmqProcessor(subscribe, publish, osp, policy, token);
                case OspProtocol.HPSD_TCP:
                case OspProtocol.WebLVC_TCP:
                    return new TcpProcessor(subscribe, publish, osp, policy, token);
                default:
                    return null;
            }
        }
    }
}
