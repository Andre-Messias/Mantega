namespace Mantega.Core.Lifecycle.Example
{
    ///
    /// This script demonstrates how to use the LifecycleBehaviour class to create a component that follows a specific lifecycle.
    ///

    using System;
    using UnityEngine;

    public class LifecycleBehaviour_Example : LifecycleBehaviour
    {
        // Fault simulation fields for testing the lifecycle's fault handling capabilities.
        [SerializeField] private bool _imaginarySimpleFault = false;
        [SerializeField] private bool _imaginaryComplexFault = false;

        // A counter to track how many times the OnInitialize method has been called, demonstrating the lifecycle's initialization process.
        [SerializeField] private int _initializations = 0;

        protected override void OnAwake()
        {
            base.OnAwake();
            Debug.Log("OnAwake");
            DebugInitialize();
        }

        protected override void OnStart()
        {
            base.OnStart();
            Debug.Log("OnStart");
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            if(_imaginarySimpleFault)
            {
                throw new Exception("OnInitialize: An simple fault has occurred.");
            }
            if(_imaginaryComplexFault)
            {
                throw new Exception("OnInitialize: An complex fault has occurred.");
            }

            _initializations++;
            Debug.Log($"OnInitialize: {_initializations} time(s) initialized.");
        }

        protected override void OnUninitialize()
        {
            base.OnUninitialize();

            Debug.Log("OnUninitialize");
        }

        protected override void OnRestart()
        {
            base.OnRestart();

            _initializations = 0;
            _imaginarySimpleFault = false;
            Debug.Log("OnRestart");
        }

        protected override void OnFixFault()
        {
            base.OnFixFault();
            _imaginarySimpleFault = false;
            _imaginaryComplexFault = false;
        }

        private async void DebugInitialize()
        {
            await Initialized;
            Debug.Log("Debug Initialize called.");
        }
    }
}