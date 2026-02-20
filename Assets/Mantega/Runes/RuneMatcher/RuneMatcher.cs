namespace Mantega.Runes
{
    using UnityEngine;

    using Mantega.Core.Lifecycle;
    using Mantega.AI;

    public class RuneMatcher : LifecycleBehaviour
    {
        [SerializeField] private SiameseRuneMatcher _siameseRuneMatcher;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _siameseRuneMatcher.Initialize();
        }

        protected override void OnUninitialize()
        {
            base.OnUninitialize();
            _siameseRuneMatcher.Uninitialize();
        }
    }
}