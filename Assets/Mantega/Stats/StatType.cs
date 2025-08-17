using System;
using UnityEngine;

namespace Mantega.Stats
{
    public interface IStatType<T>
    {
        public abstract T Value { get; }

        public abstract bool TryApplyChange(object change);

    }

    public abstract class StatTypeChange
    {
        public enum ChangeType
        {
            None,
            Set,
            Change
        }

        [Serializable]
        public class ChangeField<T>
        {
            [SerializeField] private ChangeType _type = ChangeType.None;
            public ChangeType Type => _type;

            [SerializeField] private T _value;
            public T Value => _value;

            public ChangeField(ChangeType type, T value)
            {
                _type = type;
                _value = value;
            }
        }
    }
}
