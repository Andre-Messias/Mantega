namespace Mantega.Core.Variables
{
    using System;
    using UnityEngine;

    using Mantega.Core.Reactive;

    /// <summary>
    /// Represents an integer value constrained within a specified range, with support for dynamic range adjustments
    /// and change notifications
    /// </summary>
    /// <remarks>The <see cref="ControlledInt"/> class provides a mechanism to manage an integer value
    /// that is constrained between a minimum and maximum value. The range can be dynamically adjusted, and the
    /// value is automatically clamped to remain within the specified bounds. The class also supports event-based
    /// notifications for internal changes, enabling observers to track modifications to the value, minimum, or
    /// maximum</remarks>
    [Serializable]
    public class ControlledInt : IInternalChange<ControlledInt>
    {
        #region Value
        /// <summary>
        /// Represents the serialized integer value
        /// </summary>
        /// <remarks>This field is serialized, making it visible and editable in the Unity
        /// Inspector. In the Unity Editor, changes to this value trigger the <see cref="OnEditorChangeValue(int, int)"/> method</remarks>
#if UNITY_EDITOR
        [CallOnChange(nameof(OnEditorChangeValue))]
#endif
        [SerializeField] private int _value;

        /// <summary>
        /// Gets or sets the integer value associated with this instance
        /// </summary>
        public int Value
        {
            get => _value;
            set => SetValue(value);
        }
        #endregion

        #region Max
        /// <summary>
        /// Represents the serialized maximum allowable value
        /// </summary>
        /// <remarks>This field is serialized, making it visible and editable in the Unity
        /// Inspector. In the Unity Editor, changes to this value trigger the <see cref="OnEditorChangeMax(int, int)"/> method</remarks>
#if UNITY_EDITOR
        [CallOnChange(nameof(OnEditorChangeMax))]
#endif
        [SerializeField] private int _max;

        /// <summary>
        /// Gets or sets the maximum allowable value
        /// </summary>
        public int Max
        {
            get => _max;
            set => SetMax(value);
        }
        #endregion

        #region Min
        /// <summary>
        /// Represents the serialized minimum allowable value
        /// </summary>
        /// <remarks>This field is serialized, making it visible and editable in the Unity
        /// Inspector. In the Unity Editor, changes to this value trigger the <see cref="OnEditorChangeMin(int, int)"/> method</remarks>
#if UNITY_EDITOR
        [CallOnChange(nameof(OnEditorChangeMin))]
#endif
        [SerializeField] private int _min;

        /// <summary>
        /// Gets or sets the minimum allowable value
        /// </summary>
        public int Min
        {
            get => _min;
            set => SetMin(value);
        }
        #endregion

        public event Action<ControlledInt, ControlledInt> OnInternalChange;

        #region Value Change Logic

        /// <summary>
        /// Sets the value, clamping it within the allowed range
        /// </summary>
        /// <param name="value">The value to set. It will be clamped to ensure it falls within the range defined by the <see cref="_min"/> and
        /// <see cref="_max"/> values</param>
        /// <returns>The clamped value that was set</returns>
        public int SetValue(int value) => _value = Mathf.Clamp(value, _min, _max);

        /// <summary>
        /// Sets the maximum value, ensuring it is not less than the current minimum value
        /// </summary>
        /// <remarks>If the specified <paramref name="max"/> is less than the current <see cref="_min"/>
        /// value, the <see cref="_min"/> value is used instead</remarks>
        /// <param name="max">The proposed maximum value to set</param>
        /// <returns>The resulting maximum value after applying the constraint</returns>
        public int SetMax(int max) => _max = Mathf.Max(_min, max);

        /// <summary>
        /// Sets the minimum value, ensuring it does not exceed the current maximum value
        /// </summary>
        /// <remarks>If the specified <paramref name="min"/> is greater than the current <see cref="_max"/>
        /// value, the <see cref="_max"/> value is used instead</remarks>
        /// <param name="min">The proposed minimum value</param>
        /// <returns>The updated minimum value, which will be the smaller of the proposed value and the current maximum</returns>
        public int SetMin(int min) => _min = Mathf.Min(_max, min);

        #endregion

        /// <summary>
        /// Returns a string representation of the current object, including the value and its defined range
        /// </summary>
        /// <returns>The exact details of the representation are unspecified and subject to change, but the
        /// following may be regarded as "ControlledInt: Value={value}, Min={min}, Max={max}"</returns>
        public override string ToString()
        {
            return $"{nameof(ControlledInt)}: Value={_value}, Min={_min}, Max={_max}";
        }

        /// <summary>
        /// Creates a new instance of <see cref="ControlledInt"/> with the same value, minimum, and maximum as the
        /// current instance
        /// </summary>
        /// <returns>A new <see cref="ControlledInt"/> instance that is a copy of the current instance</returns>
        public ControlledInt Clone()
        {
            return new ControlledInt
            {
                _value = this._value,
                _max = this._max,
                _min = this._min
            };
        }

#if UNITY_EDITOR

        #region EditorChangeHandlers
        /// <summary>
        /// Handles changes to the value in the Unity Editor
        /// </summary>
        /// <remarks>This method is intended for use in the Unity Editor to update the value
        /// and trigger the <see cref="OnInternalChange"/> event. It creates a clone of the current object
        /// with the old value, updates the value, and then invokes the event with the cloned and
        /// updated objects</remarks>
        /// <param name="oldV">The previous value before the change</param>
        /// <param name="newV">The new value after the change</param>
        private void OnEditorChangeValue(int oldV, int newV)
        {
            ControlledInt clone = Clone();
            clone._value = oldV;

            newV = SetValue(newV);
            OnInternalChange?.Invoke(clone, this);
        }

        /// <summary>
        /// Handles changes to the maximum value in the Unity Editor
        /// </summary>
        /// <remarks>This method is intended for use in the Unity Editor to update the maximum
        /// value and trigger the <see cref="OnInternalChange"/> event. It creates a clone of the current object
        /// with the old maximum value, updates the maximum value, and then invokes the event with the cloned and
        /// updated objects</remarks>
        /// <param name="oldV">The previous maximum value</param>
        /// <param name="newV">The new maximum value to be set</param>
        private void OnEditorChangeMax(int oldV, int newV)
        {
            ControlledInt clone = Clone();
            clone._max = oldV;

            _max = SetMax(newV);
            OnInternalChange?.Invoke(clone, this);
        }

        /// <summary>
        /// Handles changes to the minimum value of the controlled integer
        /// </summary>
        /// <remarks>This method updates the internal minimum value and triggers the <see
        /// cref="OnInternalChange"/> event with a clone of the previous state and the current state. Callers
        /// should ensure that the new value is valid within the context of the controlled integer's
        /// constraints</remarks>
        /// <param name="oldV">The previous minimum value before the change</param>
        /// <param name="newV">The new minimum value to be set</param>
        private void OnEditorChangeMin(int oldV, int newV)
        {
            ControlledInt clone = Clone();
            clone._min = oldV;

            _min = SetMin(newV);
            OnInternalChange?.Invoke(clone, this);
        }
        #endregion

#endif
    }
}