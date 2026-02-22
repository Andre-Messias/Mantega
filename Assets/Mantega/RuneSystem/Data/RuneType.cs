namespace Mantega.RuneSystem
{
    using UnityEngine;

    using Mantega.AI;

    [CreateAssetMenu(fileName = "New Rune Type", menuName = "Runes/RuneType")]
    public class RuneType : ScriptableObject
    {
        [SerializeField] private string _name;
        public string Name => _name;
    }

}