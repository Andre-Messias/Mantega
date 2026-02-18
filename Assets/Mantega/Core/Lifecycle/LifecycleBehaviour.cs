using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Mantega.Core.Editor")]
namespace Mantega.Core.Lifecycle
{
    using UnityEngine;

    using Mantega.Core.Diagnostics;
    using Mantega.Core.Reactive;
    using LifecyclePhase = ILifecycle.LifecyclePhase;
    using System;
    using System.Threading.Tasks;

#if UNITY_EDITOR
    using Mantega.Core;
#endif

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
        /// be aware that the specific lifecycle event (or <see cref="DeferredEvent.HasFired"/>) will <b>not</b> have been processed yet
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

        #region Auto Initialize Option

        /// <summary>
        /// Specifies options for automatically initializing the component.
        /// </summary>
        private enum AutoInitializeOption
        {
            None,
            InitializeOnAwake,
            InitializeOnStart
        }

        /// <summary>
        /// Specifies when the component should automatically initialize itself.
        /// </summary>
        [Header("Lifecycle Options")]
        [SerializeField] private AutoInitializeOption _autoInitializeOption = AutoInitializeOption.None;
        #endregion

        private readonly DeferredEvent _initialized = new();
        public IReadOnlyDeferredEvent Initialized => _initialized;


        #region Auto Initialization
        protected void Awake()
        {
            OnAwake();

            if (_autoInitializeOption == AutoInitializeOption.InitializeOnAwake && CanInitialize())
            {
                SafeInitializeAsync();
            }
        }

        protected virtual void OnAwake() { }

        protected void Start()
        {
            OnStart();

            if (_autoInitializeOption == AutoInitializeOption.InitializeOnStart && CanInitialize())
            {
                SafeInitializeAsync();
            }
        }

        protected virtual void OnStart() { }

        private void SafeInitializeAsync()
        {
            InitializeAsync().ContinueWith(t =>
            {
                if (t.IsFaulted) Log.Error(t.Exception.ToString(), this);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }
        #endregion

        #region Initialization
        public async Task InitializeAsync()
        {
            await ExecuteLifecycleTransitionAsync(CanInitialize, LifecyclePhase.Initializing, OnInitializeAsync, LifecyclePhase.Initialized, "Async Initialization");
        }

        public void Initialize()
        {
            ExecuteLifecycleTransition(CanInitialize, LifecyclePhase.Initializing, OnInitialize, LifecyclePhase.Initialized, "Initialization");
        }

        protected virtual async Task OnInitializeAsync() { OnInitialize(); await Task.CompletedTask; }
        protected virtual void OnInitialize() { }
        public bool CanInitialize() => CanInitialize(_status);
        protected virtual bool CanInitialize(LifecyclePhase status) => status == LifecyclePhase.Uninitialized;
        #endregion

        #region Restarting
        /// <summary>
        /// Restarts the component asynchronously.
        /// </summary>
        public async Task RestartAsync()
        {
            await ExecuteLifecycleTransitionAsync(CanRestart, LifecyclePhase.Restarting, OnRestartAsync, LifecyclePhase.Uninitialized, "Async Restarting");
        }

        public void Restart()
        {
            ExecuteLifecycleTransition(CanRestart, LifecyclePhase.Restarting, OnRestart, LifecyclePhase.Uninitialized, "Restarting");
        }

        protected virtual async Task OnRestartAsync() { OnRestart(); await Task.CompletedTask; }
        protected virtual void OnRestart() { }
        public bool CanRestart() => CanRestart(_status);
        protected virtual bool CanRestart(LifecyclePhase status) => status == LifecyclePhase.Initialized || status == LifecyclePhase.Uninitialized || status == LifecyclePhase.Faulted;
        #endregion

        #region Uninitialization
        /// <summary>
        /// Uninitializes the component asynchronously. Essential for saving data or closing network connections gracefully.
        /// </summary>
        public async Task UninitializeAsync()
        {
            await ExecuteLifecycleTransitionAsync(CanUninitialize, LifecyclePhase.Uninitializing, OnUninitializeAsync, LifecyclePhase.Uninitialized, "Async Uninitializing");
        }

        public void Uninitialize()
        {
            ExecuteLifecycleTransition(CanUninitialize, LifecyclePhase.Uninitializing, OnUninitialize, LifecyclePhase.Uninitialized, "Uninitializing");
        }

        protected virtual async Task OnUninitializeAsync() { OnUninitialize(); await Task.CompletedTask; }
        protected virtual void OnUninitialize() { }
        public bool CanUninitialize() => CanUninitialize(_status);
        protected virtual bool CanUninitialize(LifecyclePhase status) => status == LifecyclePhase.Initialized || status == LifecyclePhase.Faulted;
        #endregion

        #region Fault Fixing
        /// <summary>
        /// Attempts to fix a fault asynchronously (e.g., retrying a download or reconnection).
        /// </summary>
        public async Task FixFaultAsync()
        {
            await ExecuteLifecycleTransitionAsync(CanFixFault, LifecyclePhase.Fixing, OnFixFaultAsync, LifecyclePhase.Uninitialized, "Async Fixing Fault");
        }

        public void FixFault()
        {
            ExecuteLifecycleTransition(CanFixFault, LifecyclePhase.Fixing, OnFixFault, LifecyclePhase.Uninitialized, "Fixing Fault");
        }

        protected virtual async Task OnFixFaultAsync() { OnFixFault(); await Task.CompletedTask; }
        protected virtual void OnFixFault() { }
        public bool CanFixFault() => CanFixFault(_status);
        protected virtual bool CanFixFault(LifecyclePhase status) => status == LifecyclePhase.Faulted;
        #endregion

        #region Lifecycle Engines
        private void ExecuteLifecycleTransition(
            Func<LifecyclePhase, bool> canExecutePredicate,
            LifecyclePhase transitionPhase,
            Action action,
            LifecyclePhase successPhase,
            string operationName)
        {
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

        private async Task ExecuteLifecycleTransitionAsync(
            Func<LifecyclePhase, bool> canExecutePredicate,
            LifecyclePhase transitionPhase,
            Func<Task> asyncAction,
            LifecyclePhase successPhase,
            string operationName)
        {
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

        private void CompleteTransition(LifecyclePhase successPhase)
        {
            _status.Value = successPhase;

            if (CanResetInitializedEvent(successPhase)) _initialized.Reset();
            if (successPhase == LifecyclePhase.Initialized) _initialized.Fire();
        }

        private void HandleTransitionFailure(string operationName, Exception ex)
        {
            Log.Error($"{operationName} failed with exception: {ex}", this);
            _status.Value = LifecyclePhase.Faulted;
        }

        private void CannotExecuteWarning(string operationName, LifecyclePhase currentStatus)
        {
            Log.Warning($"Cannot perform '{operationName}' while in status {currentStatus}.", this);
        }

        #endregion

        private bool CanResetInitializedEvent(LifecyclePhase lifecyclePhase)
        {
            return CanInitialize(lifecyclePhase) && _initialized.HasFired;
        }

#if UNITY_EDITOR
        private void EditorTryResetInitializedEvent(LifecyclePhase phase)
        {
            if (CanResetInitializedEvent(phase))
            {
                _initialized.Reset();
            }
        }

        internal void EditorForceUpdateEventState()
        {
            EditorTryResetInitializedEvent(_status);
        }
#endif
    }
}