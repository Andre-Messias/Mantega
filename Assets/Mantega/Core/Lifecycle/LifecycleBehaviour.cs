namespace Mantega.Core.Lifecycle
{
    using UnityEngine;
    using System;
    using System.Threading.Tasks;

    using Mantega.Core.Diagnostics;
    using Mantega.Core.Reactive;
    using LifecyclePhase = ILifecycle.LifecyclePhase;

    /// <summary>
    /// Provides a base class for Unity components that implement a managed, multi-phase lifecycle.
    /// </summary>
    /// <remarks>For thread safety and correct event sequencing, prefer subscribing to the provided lifecycle events 
    /// rather than directly monitoring status changes. Derived classes should override the provided
    /// virtual methods to implement custom behavior for each phase, and can customize the conditions under which
    /// transitions are permitted by overriding the relevant Can methods.</remarks>
    [Serializable]
    public abstract class LifecycleBehaviour : MonoBehaviour, ILifecycle
    {
        #region Status
        /// <summary>
        /// Represents the current lifecycle phase of the object.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Execution Order and State Consistency:</b>
        /// </para>
        /// <para>
        /// During a transition (e.g., <see cref="Initialize"/>), the <see cref="Status"/> property is updated to the target phase
        /// (e.g., <see cref="LifecyclePhase.Initialized"/>) <b>immediately before</b> the corresponding event (e.g., <see cref="Initialized"/>) is fired.
        /// </para>
        /// <para>
        /// <alert class="warning">
        /// <b>Warning:</b> If you subscribe to <see cref="Syncable{T}.OnValueChanged"/> on <see cref="SyncableStatus"/>,
        /// be aware that the specific lifecycle event (or <see cref="Promise.IsResolved"/>) will <b>not</b> have been processed yet
        /// at the exact moment of the callback.
        /// </alert>
        /// </para>
        /// <para>
        /// To ensure the initialization flow is fully complete, prefer subscribing directly to the specific event (e.g., <see cref="Initialized"/>).
        /// </para>
        /// </remarks>
        [SerializeField, HideInInspector] protected Syncable<LifecyclePhase> _status = new(LifecyclePhase.Uninitialized);

        /// <summary>
        /// Gets the current lifecycle phase status as a syncable value.
        /// </summary>
        /// <inheritdoc cref="_status"/>
        public IReadOnlySyncable<LifecyclePhase> SyncableStatus => _status;

        /// <inheritdoc/>
        public LifecyclePhase Status => _status;
        #endregion

        #region Lifecycle Options
        #region Auto Initialize Option
        /// <summary>
        /// Specifies options for automatically initializing the component.
        /// </summary>
        private enum AutoInitializeOption
        {
            /// <summary>
            /// The component will not initialize automatically. 
            /// You must call <see cref="Initialize"/> or <see cref="InitializeAsync"/> manually.
            /// </summary>
            None,
            /// <summary>
            /// Automatically initializes during the <see cref="Awake"/> message.
            /// </summary>
            InitializeOnAwake,
            /// <summary>
            /// Automatically initializes during the <see cref="Start"/> message.
            /// </summary>
            InitializeOnStart
        }

        /// <summary>
        /// Specifies when the component should automatically initialize itself.
        /// </summary>
        [Header("Lifecycle Options")]
        [SerializeField] private AutoInitializeOption _autoInitializeOption = AutoInitializeOption.None;
        #endregion
        #region Runtime Only Option
#if UNITY_EDITOR
        /// <summary>
        /// Specifies whether lifecycle controls should only be available during play mode.
        /// </summary>
        [SerializeField] protected bool _runtimeOnly = false;
#endif
        #endregion
        #endregion

        #region Initialized Event
        /// <summary>
        /// Event triggered when the component successfully completes the <see cref="LifecyclePhase.Initialized"/> phase.
        /// </summary>
        [SerializeField, HideInInspector] private Promise _initialized = new();

        /// <summary>
        /// Gets a read-only view of the initialization event. Resolved after <see cref="OnInitialize"/> completes.
        /// </summary>
        public IReadOnlyPromise Initialized => _initialized;
        #endregion

        #region Auto Initialize
        /// <summary>
        /// Unity's Awake method.
        /// </summary>
        /// <remarks>
        /// Calls <see cref="OnAwake"/> <b>before</b> attempting to auto-initialize.
        /// </remarks>
        protected void Awake()
        {
            OnAwake();

            if (_autoInitializeOption == AutoInitializeOption.InitializeOnAwake && CanInitialize())
            {
                SafeInitializeAsync();
            }
        }

        /// <summary>
        /// Virtual hook for <see cref="Awake"/>.
        /// </summary>
        /// <remarks>Override this instead of adding a new Awake method 
        /// to ensure proper lifecycle ordering.</remarks>
        protected virtual void OnAwake() { }

        /// <summary>
        /// Unity's Start method.
        /// </summary>
        /// <remarks>
        /// Calls <see cref="OnStart"/> <b>before</b> attempting to auto-initialize.
        /// </remarks>
        protected void Start()
        {
            OnStart();

            if (_autoInitializeOption == AutoInitializeOption.InitializeOnStart && CanInitialize())
            {
                SafeInitializeAsync();
            }
        }

        /// <summary>
        /// Virtual hook for <see cref="Start"/>.
        /// </summary>
        /// <remarks>Override this instead of adding a new Start method 
        /// to ensure proper lifecycle ordering.</remarks>
        protected virtual void OnStart() { }

        /// <summary>
        /// Safely executes initialization in a "Fire-and-Forget" manner.
        /// </summary>
        /// <remarks>
        /// Any exceptions thrown during this process are caught and logged to the Unity Console 
        /// preventing silent failures in async void contexts.
        /// </remarks>
        private void SafeInitializeAsync()
        {
            InitializeAsync().ContinueWith(t =>
            {
                if (t.IsFaulted) Log.Error(t.Exception.ToString(), this);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Asynchronously transitions the component to the <see cref="LifecyclePhase.Initialized"/> state.
        /// </summary>
        /// <remarks>
        /// Use this for operations involving I/O, Network, or heavy asset loading.
        /// Exceptions thrown during execution will be re-thrown to the caller.
        /// </remarks>
        /// <returns>A Task representing the initialization operation.</returns>
        public async Task InitializeAsync()
        {
            await ExecuteLifecycleTransitionAsync(CanInitialize, LifecyclePhase.Initializing, OnInitializeAsync, LifecyclePhase.Initialized, "Async Initialization");
        }

        /// <summary>
        /// Synchronously transitions the component to the <see cref="LifecyclePhase.Initialized"/> state.
        /// </summary>
        /// <remarks>
        /// Use this for simple setups that must complete in a single frame.
        /// </remarks>
        public void Initialize()
        {
            ExecuteLifecycleTransition(CanInitialize, LifecyclePhase.Initializing, OnInitialize, LifecyclePhase.Initialized, "Initialization");
        }

        /// <summary>
        /// Override this method to implement asynchronous initialization logic.
        /// </summary>
        /// <remarks>
        /// This method is called during the initialization process. The default implementation calls the synchronous <see cref="OnInitialize"/> method and completes immediately.
        /// </remarks>
        /// <returns>A Task representing the async operation.</returns>
        protected virtual async Task OnInitializeAsync() { OnInitialize(); await Task.CompletedTask; }

        /// <summary>
        /// Override this method to implement synchronous initialization logic.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Checks if the component is in a valid state to be initialized.
        /// </summary>
        /// <returns><see langword="true"/> if initialization is allowed; otherwise, <see langword="false"/>.</returns>
        public bool CanInitialize() => CanInitialize(_status);

        /// <summary>
        /// Determines whether initialization can proceed based on the specified lifecycle phase.
        /// </summary>
        /// <remarks>Override this method to customize the conditions under which initialization is permitted for derived types.</remarks>
        /// <param name="status">The current lifecycle phase to evaluate. Typically represents the state of the component or service.</param>
        /// <inheritdoc cref="CanInitialize()"/>
        protected virtual bool CanInitialize(LifecyclePhase status) => status == LifecyclePhase.Uninitialized;
        #endregion

        #region Restarting
        /// <summary>
        /// Asynchronously restarts the component. Useful for resetting state with heavy cleanup.
        /// </summary>
        /// <remarks>
        /// This method transitions through the <see cref="LifecyclePhase.Restarting"/> phase, returning the component to factory settings.
        /// </remarks>
        /// <returns>A Task representing the restart operation.</returns>
        public async Task RestartAsync()
        {
            await ExecuteLifecycleTransitionAsync(CanRestart, LifecyclePhase.Restarting, OnRestartAsync, LifecyclePhase.Uninitialized, "Async Restarting");
        }

        /// <summary>
        /// Synchronously restarts the component.
        /// </summary>
        /// <remarks>
        /// Use this for quick resets that can complete within a single frame returning the component to factory settings.
        /// </remarks>
        public void Restart()
        {
            ExecuteLifecycleTransition(CanRestart, LifecyclePhase.Restarting, OnRestart, LifecyclePhase.Uninitialized, "Restarting");
        }

        /// <summary>
        /// Override this method to implement asynchronous restart logic.
        /// </summary>
        /// <remarks>
        /// This method is called during the restart process. The default implementation calls the synchronous <see cref="OnRestart"/> method and completes immediately.
        /// </remarks>
        protected virtual async Task OnRestartAsync() { OnRestart(); await Task.CompletedTask; }

        /// <summary>
        /// Override this method to implement synchronous restart logic.
        /// </summary>
        protected virtual void OnRestart() { }

        /// <summary>
        /// Checks if the component is in a valid state to be restarted.
        /// </summary>
        /// <returns><see langword="true"/> if restart is allowed; otherwise, <see langword="false"/>.</returns>
        public bool CanRestart() => CanRestart(_status);

        /// <summary>
        /// Determines whether restart can proceed based on the specified lifecycle phase.
        /// </summary>
        /// <remarks>Override this method to customize the conditions under which restarting is permitted for derived types.</remarks>
        /// <param name="status">The current lifecycle phase to evaluate. Typically represents the state of the component or service.</param>
        /// <inheritdoc cref="CanRestart()"/>
        protected virtual bool CanRestart(LifecyclePhase status) => status == LifecyclePhase.Initialized || status == LifecyclePhase.Uninitialized || status == LifecyclePhase.Faulted;
        #endregion

        #region Uninitialization
        /// <summary>
        /// Asynchronously unitializes the component. Useful for uninitializing with heavy cleanup.
        /// </summary>
        /// <remarks>
        /// This method transitions through the <see cref="LifecyclePhase.Uninitializing"/> phase, turning the component back to an uninitialized state without resetting to factory settings.
        /// </remarks>
        /// <returns>A Task representing the restart operation.</returns>
        public async Task UninitializeAsync()
        {
            await ExecuteLifecycleTransitionAsync(CanUninitialize, LifecyclePhase.Uninitializing, OnUninitializeAsync, LifecyclePhase.Uninitialized, "Async Uninitializing");
        }

        /// <summary>
        /// Synchronously unitializes the component.
        /// </summary>
        /// <remarks>
        /// Use this for quick uninitialization that can complete within a single frame, turning the component back to an uninitialized state without resetting to factory settings.
        /// </remarks>
        public void Uninitialize()
        {
            ExecuteLifecycleTransition(CanUninitialize, LifecyclePhase.Uninitializing, OnUninitialize, LifecyclePhase.Uninitialized, "Uninitializing");
        }

        /// <summary>
        /// Override this method to implement asynchronous uninitialize logic.
        /// </summary>
        /// <remarks>
        /// This method is called during the uninitialize process. The default implementation calls the synchronous <see cref="OnUninitialize"/> method and completes immediately.
        /// </remarks>
        protected virtual async Task OnUninitializeAsync() { OnUninitialize(); await Task.CompletedTask; }

        /// <summary>
        /// Override this method to implement synchronous uninitialize logic.
        /// </summary>
        protected virtual void OnUninitialize() { }

        /// <summary>
        /// Checks if the component is in a valid state to be uninitialized.
        /// </summary>
        /// <returns><see langword="true"/> if uninitialize is allowed; otherwise, <see langword="false"/>.</returns>
        public bool CanUninitialize() => CanUninitialize(_status);

        /// <summary>
        /// Determines whether uninitialize can proceed based on the specified lifecycle phase.
        /// </summary>
        /// <remarks>Override this method to customize the conditions under which uninitializing is permitted for derived types.</remarks>
        /// <param name="status">The current lifecycle phase to evaluate. Typically represents the state of the component or service.</param>
        /// <inheritdoc cref="CanUninitialize()"/>
        protected virtual bool CanUninitialize(LifecyclePhase status) => status == LifecyclePhase.Initialized || status == LifecyclePhase.Faulted;
        #endregion

        #region Fault Fixing
        /// <summary>
        /// Asynchronously fix the component fault. Useful for recovering from faults that require heavy cleanup or external operations.
        /// </summary>
        /// <returns>A Task representing the restart operation.</returns>
        public async Task FixFaultAsync()
        {
            await ExecuteLifecycleTransitionAsync(CanFixFault, LifecyclePhase.Fixing, OnFixFaultAsync, LifecyclePhase.Uninitialized, "Async Fixing Fault");
        }

        /// <summary>
        /// Synchronously fix the component fault.
        /// </summary>
        /// <remarks>
        /// Use this for quick fixing that can complete within a single frame.
        /// </remarks>
        public void FixFault()
        {
            ExecuteLifecycleTransition(CanFixFault, LifecyclePhase.Fixing, OnFixFault, LifecyclePhase.Uninitialized, "Fixing Fault");
        }

        /// <summary>
        /// Override this method to implement asynchronous fix fault logic.
        /// </summary>
        /// <remarks>
        /// This method is called during the fixing process. The default implementation calls the synchronous <see cref="OnFixFault"/> method and completes immediately.
        /// </remarks>
        protected virtual async Task OnFixFaultAsync() { OnFixFault(); await Task.CompletedTask; }

        /// <summary>
        /// Override this method to implement synchronous fix fault logic.
        /// </summary>
        protected virtual void OnFixFault() { }

        /// <summary>
        /// Checks if the component is in a valid state to be fixed.
        /// </summary>
        /// <returns><see langword="true"/> if fixing is allowed; otherwise, <see langword="false"/>.</returns>
        public bool CanFixFault() => CanFixFault(_status);

        /// <summary>
        /// Determines whether fixing can proceed based on the specified lifecycle phase.
        /// </summary>
        /// <remarks>Override this method to customize the conditions under which fixing is permitted for derived types.</remarks>
        /// <param name="status">The current lifecycle phase to evaluate. Typically represents the state of the component or service.</param>
        /// <inheritdoc cref="CanFixFault()"/>
        protected virtual bool CanFixFault(LifecyclePhase status) => status == LifecyclePhase.Faulted;
        #endregion

        #region Lifecycle Engines
        /// <summary>
        /// Core engine for executing synchronous lifecycle transitions.
        /// </summary>
        /// <param name="canExecutePredicate">Function to validate if the transition is allowed.</param>
        /// <param name="transitionPhase">The intermediate phase during execution.</param>
        /// <param name="action">The synchronous action to execute for the lifecycle transition.</param>
        /// <param name="successPhase">The final phase upon success.</param>
        /// <param name="operationName">Name of operation for logging.</param>
        /// <exception cref="Exception">Catches, logs, and re-throws any exception occurring during the action.</exception>
        private void ExecuteLifecycleTransition(
            Func<LifecyclePhase, bool> canExecutePredicate,
            LifecyclePhase transitionPhase,
            Action action,
            LifecyclePhase successPhase,
            string operationName)
        {
#if UNITY_EDITOR
            RuntimeOnlyValidation(operationName);
#endif

            LifecyclePhase currentStatus = _status;
            if (!canExecutePredicate(currentStatus))
            {
                CannotExecuteWarning(operationName, currentStatus);
                return;
            }

            _status.Value = transitionPhase;
            try
            {
                action();
                CompleteTransition(successPhase);
            }
            catch (Exception ex)
            {
                HandleTransitionFailure(operationName, ex);
                throw;
            }
        }

        /// <summary>
        /// Core engine for executing asynchronous lifecycle transitions.
        /// </summary>
        /// <param name="asyncAction">The asynchronous action to execute for the lifecycle transition.</param>
        /// <returns>The task representing the asynchronous operation.</returns>
        /// <inheritdoc cref="ExecuteLifecycleTransition(Func{LifecyclePhase, bool}, LifecyclePhase, Action, LifecyclePhase, string)"/>
        private async Task ExecuteLifecycleTransitionAsync(
            Func<LifecyclePhase, bool> canExecutePredicate,
            LifecyclePhase transitionPhase,
            Func<Task> asyncAction,
            LifecyclePhase successPhase,
            string operationName)
        {
#if UNITY_EDITOR
            RuntimeOnlyValidation(operationName);
#endif

            LifecyclePhase currentStatus = _status;
            if (!canExecutePredicate(currentStatus))
            {
                CannotExecuteWarning(operationName, currentStatus);
                return;
            }

            _status.Value = transitionPhase;
            try
            {
                await asyncAction();
                CompleteTransition(successPhase);
            }
            catch (Exception ex)
            {
                HandleTransitionFailure(operationName, ex);
                throw;
            }
        }

        /// <summary>
        /// Handles the successful completion of a transition.
        /// </summary>
        /// <param name="successPhase">The lifecycle phase to set upon successful completion.</param>
        private void CompleteTransition(LifecyclePhase successPhase)
        {
            _status.Value = successPhase;

            if (ShouldResetInitializedEvent(successPhase)) _initialized.Reset();
            if (successPhase == LifecyclePhase.Initialized) _initialized.Resolve();
        }

        /// <summary>
        /// Handles transition failures.
        /// </summary>
        /// <param name="operationName">The name of the operation that failed, used for logging.</param>
        /// <param name="ex">The exception that was thrown during the transition.</param>
        private void HandleTransitionFailure(string operationName, Exception ex)
        {
            Log.Error($"{operationName} failed with exception: {ex}", this);
            _status.Value = LifecyclePhase.Faulted;
        }

        /// <summary>
        /// Logs a warning when a transition cannot be executed due to invalid state.
        /// </summary>
        /// <param name="operationName">The name of the operation that was attempted, used for logging.</param>
        /// <param name="currentStatus">The current lifecycle phase that prevented the operation from executing.</param>
        private void CannotExecuteWarning(string operationName, LifecyclePhase currentStatus)
        {
            Log.Warning($"Cannot perform '{operationName}' while in status {currentStatus}.", this);
        }

#if UNITY_EDITOR
        private void RuntimeOnlyValidation(string operationName)
        {
            if (_runtimeOnly && !Application.isPlaying)
            {
                RuntimeOnlyWarning(operationName);
                throw new InvalidOperationException($"Cannot perform '{operationName}' in edit mode when runtime-only mode is enabled.");
            }
        }

        /// <summary>
        /// Logs a warning indicating that a lifecycle operation was attempted in edit mode, which is only permitted at
        /// runtime.
        /// </summary>
        /// <param name="operationName">The name of the lifecycle operation that was attempted in edit mode.</param>
        private void RuntimeOnlyWarning(string operationName)
        {
            Log.Warning($"Attempted to perform '{operationName}' in edit mode, but lifecycle operations are restricted to runtime only.", this);
        }
#endif

        #endregion

        /// <summary>
        /// Determines if the Initialized event should be reset.
        /// </summary>
        private bool ShouldResetInitializedEvent(LifecyclePhase lifecyclePhase)
        {
            return CanInitialize(lifecyclePhase) && _initialized.IsResolved;
        }

        /// <summary>
        /// Unity's OnDestroy method.
        /// </summary>
        /// <remarks>
        /// Acts as a fail-safe to ensure uninitialization occurs if the object is destroyed.
        /// </remarks>
        protected virtual void OnDestroy()
        {
            if (CanUninitialize())
            {
                Uninitialize();
            }

            if (!_initialized.IsResolved)
            {
                _initialized.Cancel();
            }
        }
    }
}