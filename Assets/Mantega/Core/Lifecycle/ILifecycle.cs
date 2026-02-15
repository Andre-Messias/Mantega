namespace Mantega.Core.Lifecycle
{
    public interface ILifecycle
    {
        public enum LifecyclePhase
        {
            Uninitialized,
            Uninitializing,
            Restarting, 
            Initializing, 
            Initialized,
            Faulted,
            Fixing
        }
        public LifecyclePhase Status { get; }
        public void Initialize();
        public void Restart();
        public void Uninitialize();
    }
}