namespace NewTCPServer
{
    class NewServerWorker
    {
        public void Start()
        {
            MyNewServer server = new MyNewServer(@"C:\Logfiler");
            server.Start();
        }
    }
}