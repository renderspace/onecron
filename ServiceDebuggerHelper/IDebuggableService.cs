using System.ServiceProcess;

namespace ServiceDebuggerHelper
{
    public interface IDebuggableService
    {
        void Start(string[] args);
        void StopService();
        void Pause();
        void Continue();
    }
}