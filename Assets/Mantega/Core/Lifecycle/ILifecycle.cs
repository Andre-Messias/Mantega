namespace Mantega.Core.Lifecycle
{
    /// <summary>
    /// Defines the contract for components that support explicit lifecycle management.
    /// </summary>
    /// <remarks>Implement this interface to provide standardized control over the operational state of a
    /// component. The lifecycle phases represented by the interface allow consumers to monitor and manage the
    /// component's readiness and recovery states. Typical usage includes initializing resources, resetting state, and
    /// releasing resources in a controlled manner.</remarks>
    public interface ILifecycle
    {
        /// <summary>
        /// Specifies the various phases in the lifecycle.
        /// </summary>
        /// <remarks>Use this enumeration to track or control the current operational state of a
        /// component.</remarks>
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

        /// <summary>
        /// Gets the current lifecycle phase of the object.
        /// </summary>
        public LifecyclePhase Status { get; }

        /// <summary>
        /// Initializes the component and prepares it for use.
        /// </summary>
        public void Initialize();

        /// <summary>
        /// Performs a hard reset on the component, restoring it to its factory default configuration.
        /// </summary>
        /// <remarks>This effectively wipes any current state or progress. It is equivalent to a "Factory Reset".</remarks>
        public void Restart();

        /// <summary>
        /// Deactivates the component and releases heavy resources, but retains configuration for future re-initialization.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Restart"/>, this method does <b>not</b> revert the component to factory defaults. 
        /// It acts as a "Soft Stop", allowing the component to be <see cref="Initialize"/> again with its previous context.
        /// </remarks>
        public void Uninitialize();
    }
}