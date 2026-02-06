namespace Mantega.AI.Example
{
    ///
    /// This script demonstrates how to use the SiameseRuneMatcher in Unity.
    ///

    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class SiameseRuneMatcher_Example : MonoBehaviour
    {
        [SerializeField] private SiameseRuneMatcher _runeMatcher;
        [SerializeField] private Texture2D _runeTexture1;
        [SerializeField] private Texture2D _runeTexture2;

        public float Compare()
        {
            return _runeMatcher.Compare(_runeTexture1, _runeTexture2);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SiameseRuneMatcher_Example))]
        public class SiameseRuneMatcherExampleEditor : Editor
        {
            const float SIMILARITY_THRESHOLD = 0.6f; 

            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                SiameseRuneMatcher_Example myScript = (SiameseRuneMatcher_Example)target;
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Compare Runes"))
                {
                    // Check play mode to avoid runtime errors in the editor
                    if (!Application.isPlaying)
                    {
                        Debug.LogWarning("Please enter Play mode to compare runes.");
                        return;
                    }

                    float similarity = myScript.Compare();
                    bool isMatch = similarity >= SIMILARITY_THRESHOLD;
                    string color = isMatch ? "green" : "red";
                    string resultText = isMatch ? "MATCH" : "NO MATCH";

                    Debug.Log($"(<color={color}><b>{resultText}</b></color>) Similarity Score: {similarity:P2}");
                }
            }
        }
#endif
    }

}