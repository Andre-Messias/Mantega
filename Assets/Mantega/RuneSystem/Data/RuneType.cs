namespace Mantega.RuneSystem
{
    using UnityEngine;

    /// <summary>
    /// Represents a type of rune.
    /// </summary>
    /// <remarks>Use this class to create and configure different rune types within the Unity Editor.</remarks>
    [CreateAssetMenu(fileName = "New Rune Type", menuName = "Runes/RuneType")]
    public class RuneType : ScriptableObject
    {
        /// <summary>
        /// The name of the rune type. This is used for display purposes and should be unique among all rune types.
        /// </summary>
        [SerializeField] private string _name;

        /// <summary>
        /// Gets the name associated with this instance.
        /// </summary>
        public string Name => _name;
    }
}