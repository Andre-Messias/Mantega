namespace Mantega.Core.Syncables.Example
{
    ///
    /// This script demonstrates how to use the Syncable and IInternalChange interfaces to create a synchronizable value that notifies subscribers of changes, including internal changes. 
    /// 

    using System;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
    using Mantega.Editor;
#endif

    public class Syncables_Example : MonoBehaviour
    {
        [Serializable]
        private class InternalChange_Example : IInternalChange<InternalChange_Example>
        {
            [SerializeField] private int _value1; 
            public int Value1
            {
                get => _value1;
                set
                {
                    if (_value1 == value) return;
                    ChangeValue1WithEvent(_value1, value);
                }
            }

            [SerializeField] private Color _value2;
            public Color Value2
            {
                get => _value2;
                set
                {
                    if (_value2 == value) return;
                    ChangeValue2WithEvent(_value2, value);
                }
            }

            public event Action<InternalChange_Example, InternalChange_Example> OnInternalChange;

            public InternalChange_Example(int value1, Color value2)
            {
                _value1 = value1;
                _value2 = value2;
            }

            private void ChangeValue1WithEvent(int oldValue, int newValue)
            {
                InternalChange_Example clone = Clone();
                clone._value1 = oldValue;
                _value1 = newValue;
                OnInternalChange?.Invoke(clone, this);
            }

            private void ChangeValue2WithEvent(Color oldValue, Color newValue)
            {
                InternalChange_Example clone = Clone();
                clone._value2 = oldValue;
                _value2 = newValue;
                OnInternalChange?.Invoke(clone, this);
            }

            public InternalChange_Example Clone()
            {
                return new InternalChange_Example(_value1, _value2);
            }
        }

        // Syncable - Can be public if you want to edit it from other scripts.
#if UNITY_EDITOR
        [CallOnChange(nameof(OnEditorChangeColor))]
#endif
        [SerializeField] private Syncable<Color> _colorSyncable = new(Color.white);
#if UNITY_EDITOR
        [CallOnChange(nameof(OnEditorChangeInternalChange))]
#endif
        [SerializeField] private Syncable<InternalChange_Example> _internalChangeSyncable = new(new(0, Color.white));

        // IReadOnlySyncable - Allows you to read the value and subscribe to the event, but not modify it. Useful for exposing Syncables without allowing external modification.
        public IReadOnlySyncable<Color> ColorSyncable => _colorSyncable;

        private void OnEnable()
        {
            _colorSyncable.OnValueChanged += OnColorChange;
            _internalChangeSyncable.OnValueChanged += OnInternalChange;
        }

        private void OnDisable()
        {
            _colorSyncable.OnValueChanged -= OnColorChange;
            _internalChangeSyncable.OnValueChanged -= OnInternalChange;
        }

        private void OnColorChange()
        {
            Debug.Log($"Color Syncable Changed: {_colorSyncable.Value}");
        }

        private void OnColorChange(Color oldValue, Color newValue)
        {
            Debug.Log($"Color Syncable Changed: {oldValue} -> {newValue}");
        }

        private void OnInternalChange()
        {
            Debug.Log($"Internal Change Syncable Changed: Value1: {_internalChangeSyncable.Value.Value1}, Value2: {_internalChangeSyncable.Value.Value2}");
        }

        private void OnInternalChange(InternalChange_Example oldValue, InternalChange_Example newValue)
        {
            Debug.Log($"Internal Change Syncable Changed: Value1: {oldValue.Value1} -> {newValue.Value1}, Value2: {oldValue.Value2} -> {newValue.Value2}");
        }

#if UNITY_EDITOR
        private void OnEditorChangeColor()
        {
            OnColorChange();
        }

        private void OnEditorChangeInternalChange()
        {
            OnInternalChange();
        }
        
        [CustomEditor(typeof(Syncables_Example))]
        public class Syncables_ExampleEditor : Editor
        {
            private Color GenerateRandomColor() => new(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);

            public override void OnInspectorGUI()
            {
                base.OnInspectorGUI();

                // Title
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Changes Example", EditorStyles.boldLabel);
                Syncables_Example example = (Syncables_Example)target;

                // Changing syncable values
                bool buttonPressed = true;
                if (GUILayout.Button("Change Color Syncable"))
                {
                    example._colorSyncable.Value = GenerateRandomColor();
                }
                else if (GUILayout.Button("Change Internal Change Syncable Value1"))
                {
                    example._internalChangeSyncable.Value.Value1 += (int)(UnityEngine.Random.value * 100);
                }
                else if (GUILayout.Button("Change Internal Change Syncable Value2"))
                {
                    example._internalChangeSyncable.Value.Value2 = GenerateRandomColor();
                }
                else
                {
                    buttonPressed = false;
                }

                if (buttonPressed)
                {
                    // Mark the object as dirty to ensure changes are saved and reflected in the editor
                    EditorUtility.SetDirty(example);

                    if (!Application.isPlaying)
                    {
                        Debug.Log("Enter Play Mode to see the changes in action.");
                    }
                }
            }
        }
#endif
    }

}