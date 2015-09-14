using System.ServiceProcess;

namespace ServiceDebuggerHelper
{
    public class DebuggableService : ServiceBase, IDebuggableService
    {
        public void Start(string[] args)
        {
            OnStart(args);
        }

        public void StopService()
        {
            OnStop();
        }

        public void Pause()
        {
            OnPause();
        }

        public void Continue()
        {
            OnContinue();
        }
    }
}