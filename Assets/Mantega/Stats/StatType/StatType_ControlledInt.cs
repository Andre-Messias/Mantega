using System;
using UnityEngine;

namespace Mantega.Stats
{
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

    [Serializable]
    public class StatType_ControlledInt : IStatType<ControlledInt>
    {
        [SerializeField] private ControlledInt _value;
        public ControlledInt Value => _value;
        public bool TryApplyChange(object change)
        {
            change.GetType();
            if (change is not StatTypeChange_ControlledInt intChange) return false;

            HandleControlledIntChange(intChange);

            return true;
        }

        private void HandleControlledIntChange(StatTypeChange_ControlledInt change)
        {
            HandleIntChange(change.changeMax, ref _value.Max);
            HandleIntChange(change.changeMin, ref _value.Min);
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
    public class StatTypeChange_ControlledInt : StatTypeChange
    {
        public ChangeField<int> changeValue = new(ChangeType.None, 0);
        public ChangeField<int> changeMax = new(ChangeType.None, 0);
        public ChangeField<int> changeMin = new(ChangeType.None, 0);
    }
}