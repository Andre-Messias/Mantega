namespace Mantega.RuneSystem
{
    using UnityEngine;
    using System.Collections.Generic;

#if UNITY_EDITOR
    using System.Linq;

    using Mantega.Core;
    using Mantega.Core.Diagnostics;
#endif

    [CreateAssetMenu(fileName = "New Rune Set", menuName = "Runes/RuneSet")]
    public class RuneSet : ScriptableObject
    {
#if UNITY_EDITOR
        [CallOnChange(nameof(OnTypeChange))]
#endif
        [SerializeField] private RuneType _type;
        public RuneType Type => _type;

#if UNITY_EDITOR
        [CallOnChange(nameof(SetRune))]
#endif
        [SerializeField] private Rune[] _runes;
        public IReadOnlyList<Rune> Runes => _runes;

#if UNITY_EDITOR
        private void ValidateRune(Rune rune)
        {
            Validations.ValidateNotNull(rune, this);
            Validations.ValidateNotNull(rune.Type, this);
            if(rune.Type != _type)
            {
                throw new System.Exception($"Rune type {rune.Type.name} does not match set type {Type.name}");
            }
        }

        private void SetRune(Rune[] oldRunes, Rune[] newRunes)
        {
            Debug.Log("old:" + oldRunes);
            Debug.Log("new:" + newRunes);
            _runes = new Rune[0];
        }
        
        private void OnTypeChange(RuneType _, RuneType newType)
        {
            ResetRunes();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void ResetRunes()
        {
            _runes = new Rune[0];
        }
#endif
    }
}