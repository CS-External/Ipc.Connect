namespace Ipc.Connect.Server
{
    public class IpcServerOptions
    {
        public int ConnectionLimit { get; set; }

        public IpcServerOptions()
        {
            ConnectionLimit = -1;
        }
    }
}
