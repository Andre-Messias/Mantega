using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Mantega.Core.Editor")]
namespace Mantega.Core.Lifecycle
{
    using UnityEngine;

    using Mantega.Core.Diagnostics;
    using Mantega.Core.Reactive;
    using LifecyclePhase = ILifecycle.LifecyclePhase;
    using System;

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
        /// <remarks>Changes to this field may trigger lifecycle-related events or behaviors, depending on the implementation. </remarks>
        [Header("Lifecycle")]
#if UNITY_EDITOR
        [CallOnChange(nameof(EditorTryResetInitializedEvent))]
#endif
        [SerializeField] protected Syncable<LifecyclePhase> _status = new(LifecyclePhase.Uninitialized);

        /// <summary>
        /// Gets the current lifecycle phase status as a syncable value.
        /// </summary>
        public IReadOnlySyncable<LifecyclePhase> SyncableStatus => _status;

        /// <inheritdoc/>
        public LifecyclePhase Status => _status;
        #endregion

        #region Auto Initialize Option

        /// <summary>
        /// Specifies options for automatically initializing the component.
        /// </summary>
        enum AutoInitializeOption
        {
            None,
            InitializeOnAwake,
            InitializeOnStart
        }

        /// <summary>
        /// Specifies when the component should automatically initialize itself.
        /// </summary>
        [SerializeField] private AutoInitializeOption _autoInitializeOption = AutoInitializeOption.None;
        #endregion

        private readonly DeferredEvent _initialized = new();
        public IReadOnlyDeferredEvent Initialized => _initialized;


        #region Auto Initialization
        protected void Awake()
        {
            if (_autoInitializeOption == AutoInitializeOption.InitializeOnAwake && CanInitialize())
            {
                Initialize();
            }

            OnAwake();
        }

        protected virtual void OnAwake() { }

        protected void Start()
        {
            if (_autoInitializeOption == AutoInitializeOption.InitializeOnStart && CanInitialize())
            {
                Initialize();
            }

            OnStart();
        }

        protected virtual void OnStart() { }
        #endregion

        #region Initialization
        public void Initialize()
        {
            ExecuteLifecycleTransition(
                CanInitialize,
                LifecyclePhase.Initializing,
                OnInitialize,
                LifecyclePhase.Initialized,
                "Initialization"
            );
        }

        protected virtual void OnInitialize() { }

        protected virtual bool CanInitialize(LifecyclePhase status)
        {
            return status == LifecyclePhase.Uninitialized;
        }

        public bool CanInitialize()
        {
            return CanInitialize(_status);
        }
        #endregion

        #region Restarting
        public void Restart()
        {
            ExecuteLifecycleTransition(
                CanRestart,
                LifecyclePhase.Restarting,
                OnRestart,
                LifecyclePhase.Uninitialized,
                "Restarting"
            );
        }

        protected virtual void OnRestart() { }

        protected virtual bool CanRestart(LifecyclePhase status)
        {
            return status == LifecyclePhase.Initialized || status == LifecyclePhase.Uninitialized || status == LifecyclePhase.Faulted;
        }

        public bool CanRestart()
        {
            return CanRestart(_status);
        }
        #endregion

        #region Uninitialization
        public void Uninitialize()
        {
            ExecuteLifecycleTransition(
                CanUninitialize,
                LifecyclePhase.Uninitializing,
                OnUninitialize,
                LifecyclePhase.Uninitialized,
                "Uninitializing"
            );
        }

        protected virtual void OnUninitialize() { }

        protected virtual bool CanUninitialize(LifecyclePhase status)
        {
            return status == LifecyclePhase.Initialized || status == LifecyclePhase.Faulted;
        }

        public bool CanUninitialize()
        {
            return CanUninitialize(_status);
        }
        #endregion

        #region Fault Fixing
        public void FixFault()
        {
            ExecuteLifecycleTransition(
                CanFixFault,
                LifecyclePhase.Fixing,
                OnFixFault,
                LifecyclePhase.Uninitialized,
                "Fixing Fault"
            );
        }

        protected virtual void OnFixFault() { }

        protected virtual bool CanFixFault(LifecyclePhase status)
        {
            return status == LifecyclePhase.Faulted;
        }

        public bool CanFixFault()
        {
            return CanFixFault(_status);
        }
        #endregion

        private void ExecuteLifecycleTransition(
            Func<LifecyclePhase, bool> canExecutePredicate,
            LifecyclePhase transitionPhase,
            Action action,
            LifecyclePhase successPhase,
            string operationName)
        {
            LifecyclePhase currentStatus = _status;

            // Validate
            if (!canExecutePredicate(currentStatus))
            {
                Log.Warning($"Cannot perform '{operationName}' while in status {currentStatus}.", this);
                return;
            }

            _status.Value = transitionPhase;

            try
            {
                action();

                _status.Value = successPhase;

                if (CanResetInitializedEvent(successPhase))
                {
                    _initialized.Reset();
                }

                if(successPhase == LifecyclePhase.Initialized)
                {
                    _initialized.Fire();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{operationName} failed with exception: {ex}", this);
                _status.Value = LifecyclePhase.Faulted;
            }
        }


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