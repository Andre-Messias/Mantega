namespace Mantega.Core.Lifecycle
{
    public interface ILifecycle
    {
        public enum LifecyclePhase
        {
            Uninitialized,
            Uninitializing,
            Resetting, 
            Initializing, 
            Initialized,
            Faulted,
            Fixing
        }
        public LifecyclePhase Status { get; }
        public void Initialize();
        public void Reset();
        public void Uninitialize();
    }
}