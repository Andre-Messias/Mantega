using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Mantega.Core.Diagnostics;
using Mantega.Drawer; 
using Mantega.Geometry;

namespace Mantega.Runes
{
    #if UNITY_EDITOR
    using UnityEditor;
    #endif

    public class RuneGenerator : MonoBehaviour
    {
        [Header("Inputs")]
        [SerializeField] private DrawingController _drawingController;

        [Header("Output Settings")]
        [SerializeField] private Vector2Int _resolution = new(512, 512);
        [SerializeField] private Vector2 _padding = new(20f, 20f);

        [Header("Style")]
        [SerializeField] private TextureDrawer _drawer = new();

        [Header("Preview (Optional)")]
        [SerializeField] private RawImage _previewImage;

        private void Awake()
        {
            Validations.ValidateNotNull(_drawingController);
        }

        public (Texture2D, List<LineSegment>) BakeRune()
        {
            // Processing
            List<LineSegment> segments = RuneProcessor.ProcessDraw(
                _drawingController.Lines,     
                _resolution,         
                _padding,                       
                _drawingController.DrawingCamera 
            );

            if (segments.Count == 0)
            {
                Debug.LogWarning("No lines to draw the rune.");
                return (null, segments);
            }

            // Drawing
            _drawer.Texture = TextureDrawer.CreateSolidTexture(_resolution.x, _resolution.y, Color.white);
            _drawer.DrawLines(segments);

            // Finalizing
            Texture2D finalTexture = _drawer.Texture;

            if (_previewImage != null)
            {
                UpdatePreview(finalTexture);
            }

            return (finalTexture, segments);
        }

        private void UpdatePreview(Texture2D texture)
        {
            _previewImage.texture = texture;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(RuneGenerator))]
    public class RuneGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Desenha todas as variáveis padrão (Inputs, Settings, Style...)
            DrawDefaultInspector();

            RuneGenerator generator = (RuneGenerator)target;

            // Adiciona um espaço para o botão não ficar colado
            EditorGUILayout.Space(10);

            // Cria o botão com altura personalizada para destaque
            if (GUILayout.Button("Bake Rune Now", GUILayout.Height(30)))
            {
                generator.BakeRune();
            }
        }
    }
#endif
}