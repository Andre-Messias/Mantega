using System;
using UnityEngine;

namespace Mantega
{
    public static class Mantega
    {
        public const string Version = "0.0.1";
        
        [Serializable]
        public class ControlledInt
        {
            [SerializeField] private int _value;
            public int Value
            {
                get => _value;
                set => Set(value);
            }
            public int Max;
            public int Min;

            public void Set(int newValue)
            {
                _value = Mathf.Clamp(newValue, Min, Max);
            }
        }
    }

}
