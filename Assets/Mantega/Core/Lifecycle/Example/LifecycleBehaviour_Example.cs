namespace Mantega.Core.Lifecycle.Example
{
    using Mantega.Core.Reactive;
    using System;
    using UnityEngine;
    using Mantega.Core.Variables;

    public class LifecycleBehaviour_Example : LifecycleBehaviour
    {
        [SerializeField] private Syncable<ControlledInt> _controlledInt;

    }
}