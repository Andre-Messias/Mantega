namespace Mantega.RuneSystem
{
    using UnityEngine;
    using System.Collections.Generic;

    [CreateAssetMenu(fileName = "New Rune Set", menuName = "Runes/RuneSet")]
    public class RuneSet : ScriptableObject
    {
        [SerializeField] private RuneType _type;
        public RuneType Type => _type;

        [SerializeField] private Rune[] _runes;
        public IReadOnlyList<Rune> Runes => _runes;
    }

}