using System;
using UnityEngine;

namespace Mantega.Stats
{
    using ControlledIntVariable = Beta.ControlledInt;
    
    public static partial class StatType
    {
        [Serializable]
        public class ControlledInt : IStatType<ControlledIntVariable, ControlledIntChange>
        {
            [SerializeField] private ControlledIntVariable _value;
            public ControlledIntVariable Value => _value;
            public void ApplyChange(ControlledIntChange change)
            {
                _value.Max = HandleIntChange(change.changeMax, _value.Max);
                _value.Min = HandleIntChange(change.changeMin, _value.Min);
                _value.Value = HandleIntChange(change.changeValue, _value.Value);
            }

            private int HandleIntChange(StatTypeChange.ChangeField<int> changeField, int value) => HandleIntChange(changeField, ref value);

            private int HandleIntChange(StatTypeChange.ChangeField<int> changeField, ref int value)
            {
                switch (changeField.Type)
                {
                    case StatTypeChange.ChangeType.None:
                        break;
                    case StatTypeChange.ChangeType.Set:
                        value = changeField.Value;
                        break;
                    case StatTypeChange.ChangeType.Change:
                        value += changeField.Value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return value;
            }

        }

        [Serializable]
        public class ControlledIntChange : StatTypeChange
        {
            public ChangeField<int> changeValue = new(ChangeType.None, 0);
            public ChangeField<int> changeMax = new(ChangeType.None, 0);
            public ChangeField<int> changeMin = new(ChangeType.None, 0);
        }
    }
}