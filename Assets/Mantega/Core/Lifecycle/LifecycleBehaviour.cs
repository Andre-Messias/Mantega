namespace Mantega.Core.Lifecycle
{
    using UnityEngine;

    using Mantega.Core.Diagnostics;
    using Mantega.Core.Reactive;
    using LifecyclePhase = ILifecycle.LifecyclePhase;
    using System;

#if UNITY_EDITOR
    using UnityEditor;
    using Editor;
#endif

    [Serializable]
    public abstract class LifecycleBehaviour : MonoBehaviour, ILifecycle
    {
        enum AutoInitializeOption
        {
            None,
            InitializeOnAwake,
            InitializeOnStart
        }

        #region Status
        [Header("Lifecycle")]
#if UNITY_EDITOR
        [CallOnChange(nameof(EditorTryResetInitializedEvent))]
#endif
        [SerializeField] protected Syncable<LifecyclePhase> _status = new(LifecyclePhase.Uninitialized);
        public IReadOnlySyncable<LifecyclePhase> SyncableStatus => _status;
        public LifecyclePhase Status => _status;
        #endregion

        [SerializeField] private AutoInitializeOption _autoInitializeOption = AutoInitializeOption.None;

        private readonly DeferredEvent _initialized = new();
        public IReadOnlyDeferredEvent Initialized => _initialized;


        #region Auto Initialization
        protected void Awake()
        {
            _status.OnValueChanged += HandleStatusChanged;
            if (_autoInitializeOption == AutoInitializeOption.InitializeOnAwake && CanInitialize())
            {
                Initialize();
            }

            OnAwake();
        }

        private void HandleStatusChanged(LifecyclePhase oldPhase, LifecyclePhase newPhase)
        {
            TryResetInitializedEvent(newPhase);
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
                _initialized.Reset();
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
                _initialized.Reset();
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
                _initialized.Reset();
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
        
        private bool TryResetInitializedEvent(LifecyclePhase lifecyclePhase)
        {
            if (CanResetInitializedEvent(lifecyclePhase))
            {
                _initialized.Reset();
                return true;
            }
            return false;
        }

        private bool TryResetInitializedEvent()
        {
            return TryResetInitializedEvent(_status);
        }

        private bool CanResetInitializedEvent(LifecyclePhase lifecyclePhase)
        {
            return CanInitialize(lifecyclePhase) && _initialized.HasFired;
        }

        private bool CanResetInitializedEvent()
        {
            return CanResetInitializedEvent(_status);
        }

#if UNITY_EDITOR
        private void EditorTryResetInitializedEvent(LifecyclePhase oldPhase, LifecyclePhase newPhase)
        {
            TryResetInitializedEvent(newPhase);
        }

        
#endif
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(LifecycleBehaviour), true)]
    public class LifecycleBehaviourEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            LifecycleBehaviour lifecycle = (LifecycleBehaviour)target;
            GUILayout.Space(25);
            GUILayout.Label($"Status: {lifecycle.Status}");
            bool buttonPressed = false;
            if (lifecycle.CanInitialize() && GUILayout.Button("Initialize"))
            {
                lifecycle.Initialize();
                buttonPressed = true;
            }
            if (lifecycle.CanRestart() && GUILayout.Button("Restart"))
            {
                lifecycle.Restart();
                buttonPressed = true;
            }
            if (lifecycle.CanUninitialize() && GUILayout.Button("Uninitialize"))
            {
                lifecycle.Uninitialize();
                buttonPressed = true;
            }
            if (lifecycle.CanFixFault() && GUILayout.Button("Fix Fault"))
            {
                lifecycle.FixFault();
                buttonPressed = true;
            }

            // Set gameobject as dirty to ensure changes are saved and reflected in the editor
            if (buttonPressed)
            {
                EditorUtility.SetDirty(lifecycle.gameObject);
            }
        }
    }
#endif
}