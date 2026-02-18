namespace Mantega.Core.Lifecycle.Editor
{
    using UnityEngine;
    using UnityEditor;
    using System;

    using Mantega.Core.Lifecycle;

    /// <summary>
    /// Draws the custom inspector GUI for a LifecycleBehaviour in the Unity Editor.
    /// </summary>
    /// <remarks>This method adds status display and action buttons (Initialize, Restart, Uninitialize, Fix
    /// Fault) to the inspector for a LifecycleBehaviour component. The availability of each button depends on the
    /// current state of the LifecycleBehaviour. When an action button is pressed, the corresponding method is invoked
    /// and the GameObject is marked as dirty to ensure changes are saved in the editor.</remarks>
    [CustomEditor(typeof(LifecycleBehaviour), true)]
    public class LifecycleBehaviourEditor : Editor
    {
        /// <summary>
        /// Draws the custom inspector GUI for a LifecycleBehaviour.
        /// </summary>
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LifecycleBehaviour lifecycle = (LifecycleBehaviour)target;

            GUILayout.Space(15);
            EditorGUILayout.LabelField("Lifecycle Controls", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.EnumPopup("Current Phase", lifecycle.Status);
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            if (lifecycle.CanInitialize())
            {
                if (GUILayout.Button("Initialize"))
                {
                    ExecuteAction(lifecycle, "Initialize Lifecycle", l => l.Initialize());
                }
            }

            if (lifecycle.CanRestart())
            {
                if (GUILayout.Button("Restart"))
                {
                    ExecuteAction(lifecycle, "Restart Lifecycle", l => l.Restart());
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();

            if (lifecycle.CanUninitialize())
            {
                if (GUILayout.Button("Uninitialize"))
                {
                    ExecuteAction(lifecycle, "Uninitialize Lifecycle", l => l.Uninitialize());
                }
            }

            if (lifecycle.CanFixFault())
            {
                if (GUILayout.Button("Fix Fault"))
                {
                    ExecuteAction(lifecycle, "Fix Fault", l => l.FixFault());
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Executes the specified action on the given <see cref="LifecycleBehaviour"/> and records the operation for undo support.
        /// </summary>
        /// <remarks>This method ensures that changes made by the action can be undone via the Unity
        /// editor's undo system. After the action is executed, the lifecycle's initialization event is reset and the
        /// object is marked as dirty to ensure changes are saved.</remarks>
        /// <param name="lifecycle">The lifecycle behaviour instance to operate on. Cannot be <see langword="null"/>.</param>
        /// <param name="undoName">The name to use for the undo operation. This name appears in the Unity editor's undo history and should
        /// describe the action performed.</param>
        /// <param name="action">The action to execute on the <see cref="LifecycleBehaviour"/>. Cannot be <see langword="null"/>.</param>
        private void ExecuteAction(LifecycleBehaviour lifecycle, string undoName, Action<LifecycleBehaviour> action)
        {
            // Necessary to ensure that the action can be undone 
            Undo.RecordObject(lifecycle, undoName);

            action(lifecycle);

            EditorUtility.SetDirty(lifecycle);
        }
    }
}