namespace Mantega.RuneSystem
{
    using UnityEngine;

    using Mantega.Core.Lifecycle;
    using Mantega.AI;
    using Mantega.Core.Diagnostics;

    public class RuneMatcher : LifecycleBehaviour
    {
        [SerializeField] private SiameseRuneMatcher _siameseRuneMatcher;

        #region Lifecycle
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Validations.ValidateNotNull(_siameseRuneMatcher, this);
            _siameseRuneMatcher.Initialize();
        }

        protected override void OnUninitialize()
        {
            base.OnUninitialize();
            if (_siameseRuneMatcher.CanUninitialize())
            {
                _siameseRuneMatcher.Uninitialize();
            }
        }

        protected override void OnRestart()
        {
            base.OnRestart();
            _siameseRuneMatcher.Restart();
        }

        protected override void OnFixFault()
        {
            base.OnFixFault();

            // SiameseRuneMatcher
            if(_siameseRuneMatcher == null)
            {
                _siameseRuneMatcher = Validations.ValidateComponentExists<SiameseRuneMatcher>(gameObject, this);
            }
            _siameseRuneMatcher.FixFault();
        }
        #endregion  
    }
}