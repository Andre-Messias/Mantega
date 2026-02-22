namespace Mantega.RuneSystem
{
    using UnityEngine;

    using Mantega.AI;

    [CreateAssetMenu(fileName = "New Rune", menuName = "Runes/Rune")]
    public class Rune : ScriptableObject
    {
        [SerializeField] private Texture2D _texture;
        public Texture2D Texture => _texture;

        [SerializeField] private PDollar.PDollarPoint[] _points;
        public PDollar.PDollarPoint[] Points => _points;

        [SerializeField] private RuneType _type;
        public RuneType Type => _type;
    }

}