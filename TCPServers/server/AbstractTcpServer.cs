using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace TCPServers.server
{
    public abstract class AbstractTcpServer
    {
        /*
         * Constants - here name of configfile
         */
        /// <summary>
        /// The name og the config file - 'TcpServerConfig.xml'
        /// </summary>
        public const string CONFIG_FILE = "TcpServerConfig.xml";

        /*
         * Fields to operate the server
         */
        protected readonly int Port = 0;
        protected readonly int ShutdownPort = 0;
        protected readonly string Name = "";
        private bool running = true;
        private readonly List<Task> startingTasks;

        /// <summary>
        /// Initilalize the server with port numbers, name etc. 
        /// </summary>
        /// <param name="port">The server port</param>
        /// <param name="name">The name of the server</param>
        protected AbstractTcpServer(int port, string name)
        {
            Port = port;
            ShutdownPort = Port + 1;
            Name = name;
            SetupTracing(SourceLevels.All);

            startingTasks = new List<Task>();
        }

        /// <summary>
        /// Initilalize the server with port numbers, name etc. 
        /// </summary>
        /// <param name="configFilePath">The path to find the configuration file</param>
        protected AbstractTcpServer(string configFilePath)
        {
            Configuration conf = ReadConfiguration(configFilePath);
            Port = conf.ServerPort;
            ShutdownPort = conf.ShutdownPort;
            Name = conf.ServerName;
            SetupTracing(conf.DebugLevel);

            startingTasks = new List<Task>();
        }



        /// <summary>
        /// Starts the server at server port, can be stopped at stop-port
        /// </summary>
        public void Start()
        {
            // start stop server separately
            Task.Run(() => StopServer(ShutdownPort) );
            Trace.TraceInformation($"The stop server started at {ShutdownPort}");

            // start Main server
            TcpListener server = new TcpListener(IPAddress.Loopback, Port);
            server.Start();
            Trace.TraceInformation($"The Server '{Name}' is started at {Port}");

            while (running)
            {
                if (server.Pending())
                {
                    // A client is connected
                    TcpClient socket = server.AcceptTcpClient();
                    Trace.TraceInformation($"New client connected");

                    startingTasks.Add(Task.Run(
                        () =>
                        {
                            TcpClient tmpsocket = socket;
                            DoClient(tmpsocket);
                        }
                    ));
                }
                else
                {
                    // no client
                    Thread.Sleep(2000); // wait 2 sec
                }
            }

            Trace.TraceInformation($"The server is stopping");
            // wait for all task to finished
            foreach (Task task in startingTasks)
            {
                task.Wait();
            }

            foreach (TraceListener l in Trace.Listeners)
            {
                l.Close();
            }

        }

        /// <summary>
        /// Maintain one client i.e. do all communication with the specific client
        /// </summary>
        /// <param name="socket">The socket to the client</param>
        private void DoClient(TcpClient socket)
        {
            using (StreamReader sr = new StreamReader(socket.GetStream()))
            using (StreamWriter sw = new StreamWriter(socket.GetStream()))
            {
                sw.AutoFlush = true;

                TcpWorkerTemplate(sr, sw);
            }

            socket?.Close();
        }

        /// <summary>
        /// The template method i.e. insert code here that handle one client 
        /// </summary>
        /// <param name="sr">The network input stream to read from</param>
        /// <param name="sw">The network output stream to write to</param>
        protected abstract void TcpWorkerTemplate(StreamReader sr, StreamWriter sw);

        /*
         * For stooping the server softly
         */

        private void StopTheServer()
        {
            running = false;
        }

        private void StopServer(int stopPort)
        {
            TcpListener stopServer = new TcpListener(IPAddress.Loopback, stopPort);
            stopServer.Start();
            bool stop = false;

            while (!stop)
            {
                using (TcpClient client = stopServer.AcceptTcpClient())
                using (StreamReader sr = new StreamReader(client.GetStream()))
                {
                    String str = sr.ReadLine();
                    if (str == "KillMe")
                    {
                        StopTheServer();
                        stop = true;
                    }
                    else
                    {
                        Trace.TraceWarning($"Someone try illegal to stop the Server - use {str}");
                    }
                }
            }

            stopServer.Stop();
        }

        /*
         * Configuration
         */
        private Configuration ReadConfiguration(string configFilePath)
        {
            Configuration conf = new Configuration();

            XmlDocument configDoc = new XmlDocument(); 
            configDoc.Load(configFilePath + @"\" + CONFIG_FILE);

            /*
             * Read Serverport
             */
            XmlNode portNode = configDoc.DocumentElement.SelectSingleNode("ServerPort");
            if (portNode != null)
            {
                String str = portNode.InnerText.Trim(); 
                conf.ServerPort = Convert.ToInt32(str);
            }

            /*
             * Read Shutdown port
             */
            XmlNode sdportNode = configDoc.DocumentElement.SelectSingleNode("ShutdownPort");
            if (sdportNode != null)
            {
                String str = sdportNode.InnerText.Trim();
                conf.ShutdownPort = Convert.ToInt32(str);
            }

            /*
             * Read server name
             */
            XmlNode nameNode = configDoc.DocumentElement.SelectSingleNode("ServerName");
            if (nameNode != null)
            {
                conf.ServerName = nameNode.InnerText.Trim();
            }

            /*
             * Read Debug Level
             */
            XmlNode debugNode = configDoc.DocumentElement.SelectSingleNode("DebugLevel");
            if (debugNode != null)
            {
                string str  = debugNode.InnerText.Trim();
                SourceLevels level = SourceLevels.All;
                SourceLevels.TryParse(str, true, out level);
                conf.DebugLevel = level;
            }

            return conf;
        }

        /*
         * Setup Tracing
         */
        private void SetupTracing(SourceLevels confDebugLevel)
        {
            // Consol
            TraceListener tl1 = new TextWriterTraceListener(Console.Out)
                {Filter = new EventTypeFilter(confDebugLevel) };
            // File
            TraceListener tl2 = new TextWriterTraceListener(new StreamWriter(@"C:\Logfiler\TcpServer.txt"))
                {Filter = new EventTypeFilter(confDebugLevel) };
            // XML file
            TraceListener tl3 = new XmlWriterTraceListener(new StreamWriter(@"C:\Logfiler\TcpServer.xml"))
                { Filter = new EventTypeFilter(confDebugLevel) };

            Trace.Listeners.Add(tl1);
            Trace.Listeners.Add(tl2);
            Trace.Listeners.Add(tl3);
            
        }

    }
}
