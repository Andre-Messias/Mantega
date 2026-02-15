namespace Mantega.Core.Lifecycle
{
    using UnityEngine;

    using Mantega.Core.Diagnostics;
    using Mantega.Core.Reactive;
    using LifecyclePhase = ILifecycle.LifecyclePhase;
    using System;

    [Serializable]
    public abstract class LifecycleBehaviour : MonoBehaviour, ILifecycle
    {
        [Header("Lifecycle")]
        [SerializeField] protected Syncable<LifecyclePhase> _status = new(LifecyclePhase.Uninitialized);
        public IReadOnlySyncable<LifecyclePhase> SyncableStatus => _status;
        public LifecyclePhase Status => _status;

        private readonly DeferredEvent _initialized = new();
        public IReadOnlyDeferredEvent Initialized => _initialized;

        #region Initialization

        public void Initialize()
        {
            LifecyclePhase status = _status;
            if (!CanInitialize(status))
            {
                Log.Warning($"Cannot initialize while in status {status}.", this);
                return;
            }

            _status.Value = LifecyclePhase.Initializing;
            try
            {
                OnInitialize();
                _status.Value = LifecyclePhase.Initialized;
                _initialized.Fire();
            }
            catch (Exception ex)
            {
                Log.Error($"Initialization failed with exception: {ex}", this);
                _status.Value = LifecyclePhase.Faulted;
            }
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
            LifecyclePhase status = _status;
            if (!CanRestart(status))
            {
                Log.Warning($"Cannot reset while in status {status}.", this);
                return;
            }
    
            _status.Value = LifecyclePhase.Restarting;
            try
            {
                OnRestart();
                _status.Value = LifecyclePhase.Uninitialized;
            }
            catch (Exception ex)
            {
                Log.Error($"Resetting failed with exception: {ex}", this);
                _status.Value = LifecyclePhase.Faulted;
            }
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
            LifecyclePhase status = _status;
            if (!CanUninitialize(status))
            {
                Log.Warning($"Cannot uninitialize while in status {status}.", this);
                return;
            }

            _status.Value = LifecyclePhase.Uninitializing;
            try
            {
                OnUninitialize();
                _status.Value = LifecyclePhase.Uninitialized;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Uninitialization failed with exception: {ex}", this);
                _status.Value = LifecyclePhase.Faulted;
            }
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
            LifecyclePhase status = _status;
            if (!CanFixFault(status))
            {
                Log.Warning($"Cannot fix fault while in status {status}.", this);
                return;
            }

            _status.Value = LifecyclePhase.Fixing;
            try
            { 
                OnFixFault();
                _status.Value = LifecyclePhase.Uninitialized;
            }
            catch (System.Exception ex)
            {
                Log.Error($"Fixing fault failed with exception: {ex}", this);
                _status.Value = LifecyclePhase.Faulted;
            }
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
    
    }
}