namespace Mantega.RuneSystem
{
    using UnityEngine;
    using System.Collections.Generic;

    [CreateAssetMenu(fileName = "New Rune Registry", menuName = "Runes/RuneRegistry")]
    public class RuneRegistry : ScriptableObject
    {
        [SerializeField] private RuneSet[] _runeSets;
        public IReadOnlyList<RuneSet> RuneSets => _runeSets;
    }

}