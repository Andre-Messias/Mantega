namespace Mantega.AI.Example
{
    ///
    /// This script demonstrates how to use the PDollar recognizer in Unity.
    ///

    using UnityEngine;
    using System.Collections.Generic;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    public class PDollar_Example : MonoBehaviour
    {
        [Header("Symbols")]
        [SerializeField] private GameObject _drawParent1;
        [SerializeField] private GameObject _drawParent2;

        private List<LineRenderer> GetSymbolLineRenderers(GameObject gameObject)
        {
            List<LineRenderer> lineRenderers = new();
            foreach (Transform child in gameObject.transform)
            {
                if (child.TryGetComponent(out LineRenderer lr))
                {
                    lineRenderers.Add(lr);
                }
            }
            return lineRenderers;
        }

        public float CompareSymbolsFromGameObjects()
        {
            List<LineRenderer> symbol1 = GetSymbolLineRenderers(_drawParent1);
            List<LineRenderer> symbol2 = GetSymbolLineRenderers(_drawParent2);
            List<PDollar.PDollarPoint> convertedSymbol2 = PDollar.ToPoints(symbol2);
            List<PDollar.PDollarPoint> normalizedSymbol2 = PDollar.Normalize(convertedSymbol2);
            return CompareSymbols(symbol1, normalizedSymbol2);
        }

        public static float CompareSymbols(List<LineRenderer> symbol1, List<PDollar.PDollarPoint> normalizedSymbol2)
        {
            return PDollar.GetSimilarity(symbol1, normalizedSymbol2);
        }

#if UNITY_EDITOR
        [CustomEditor(typeof(PDollar_Example))]
        public class PDollarExampleEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();
                PDollar_Example myScript = (PDollar_Example)target;
                if (GUILayout.Button("Compare Symbols"))
                {
                    float similarity = myScript.CompareSymbolsFromGameObjects();
                    Debug.Log($"Symbols Similarity: {similarity}");
                }
            }
        }
#endif
    }
}