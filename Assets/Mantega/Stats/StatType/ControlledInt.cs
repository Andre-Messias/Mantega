using System;
using UnityEngine;

namespace Mantega.Stats
{
    using ControlledIntVariable = Core.Variables.ControlledInt;

    public static partial class StatType
    {
        [Serializable]
        public class ControlledInt : StatTypeBase<ControlledIntVariable, ControlledIntChange>
        {
            [SerializeField] private ControlledIntVariable _value;
            public override ControlledIntVariable Value => _value;
            protected override void ApplyChangeLogic(ControlledIntChange change)
            {
                _value.Max = HandleIntChange(change.changeMax, _value.Max);
                _value.Min = HandleIntChange(change.changeMin, _value.Min);
                _value.Value = HandleIntChange(change.changeValue, _value.Value);
            }

            private int HandleIntChange(StatTypeChange.ChangeField<int> changeField, int value)
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

            public override string ToString()
            {
                return $"{_value.Value} (Min: {_value.Min}, Max: {_value.Max})";
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