using System;
using System.Diagnostics;

namespace TCPServers.server
{
    class Configuration
    {
        public int ServerPort { get; set; }
        public int ShutdownPort { get; set; }
        public String ServerName { get; set; }
        public SourceLevels DebugLevel { get; set; }


    }
}