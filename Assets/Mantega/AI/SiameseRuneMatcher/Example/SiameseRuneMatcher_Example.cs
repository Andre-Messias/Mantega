namespace Mantega.AI.Example
{
    ///
    /// This script demonstrates how to use the SiameseRuneMatcher in Unity.
    ///

    using UnityEngine;
    using System.Threading.Tasks;

#if UNITY_EDITOR
    using UnityEditor;
    using Mantega.Core.Lifecycle;
#endif

    public class SiameseRuneMatcher_Example : MonoBehaviour
    {
        [SerializeField] private SiameseRuneMatcher _runeMatcher;
        [SerializeField] private Texture2D _runeTexture1;
        [SerializeField] private Texture2D _runeTexture2;

        public async Task<float> Compare()
        {
            await _runeMatcher.Initialized;
            return _runeMatcher.Compare(_runeTexture1, _runeTexture2);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(SiameseRuneMatcher_Example))]
        public class SiameseRuneMatcherExampleEditor : Editor
        {
            const float SIMILARITY_THRESHOLD = 0.6f; 

            public override async void OnInspectorGUI()
            {
                DrawDefaultInspector();
                SiameseRuneMatcher_Example runeMatcher = (SiameseRuneMatcher_Example)target;
                EditorGUILayout.Space(10);
                if (GUILayout.Button("Compare Runes"))
                {
                    // Check play mode to avoid runtime errors in the editor
                    if (runeMatcher._runeMatcher.Status != ILifecycle.LifecyclePhase.Initialized)
                    {
                        Debug.LogWarning("Please initialize RuneMatcher to compare runes.");
                        return;
                    }

                    float similarity = await runeMatcher.Compare();
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