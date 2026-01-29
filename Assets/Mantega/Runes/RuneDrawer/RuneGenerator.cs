using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Opcional, para preview
using Mantega.Drawer; // Onde mora o TextureDrawer e DrawingController
using Mantega.Geometry; // Onde mora o LineSegment
using static UnityEngine.GraphicsBuffer;



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
        [Tooltip("Resolução final da textura da runa (ex: 512x512 ou 256x512)")]
        [SerializeField] private Vector2Int _resolution = new(512, 512);

        [Tooltip("Espaço vazio nas bordas para a runa não encostar no limite")]
        [SerializeField] private Vector2 _padding = new(20f, 20f);

        [Header("Style")]
        [SerializeField] private TextureDrawer _drawer = new();

        [Header("Preview (Optional)")]
        [SerializeField] private RawImage _previewImage;

        /// <summary>
        /// Captura o desenho atual, normaliza e gera uma textura pronta para uso.
        /// </summary>
        /// <returns>A Texture2D gerada.</returns>
        public Texture2D BakeRune()
        {
            if (_drawingController == null)
            {
                Debug.LogError("DrawingController não atribuído no RuneGenerator!");
                return null;
            }

            // 1. PROCESSAMENTO (Geometry Phase)
            // Extrai as linhas do mundo 3D, limpa, centraliza e escala para caber na resolução alvo.
            // O RuneProcessor retorna apenas dados matemáticos (LineSegment), sem cor ou pixels.
            List<LineSegment> segments = RuneProcessor.ProcessDraw(
                _drawingController.Lines,           // As linhas cruas do LineRenderer
                _resolution,                        // O tamanho do alvo (Vector2Int converte implícito para Vector2)
                _padding,                           // Margem de segurança
                _drawingController.DrawingCamera    // Necessário para converter World -> Screen
            );

            if (segments.Count == 0)
            {
                Debug.LogWarning("Nenhuma linha para gerar a runa.");
                return null;
            }

            // 2. PINTURA (Texture Phase)
            _drawer.Texture = TextureDrawer.CreateSolidTexture(_resolution.x, _resolution.y, Color.white);
            _drawer.DrawLines(segments);

            // 3. FINALIZAÇÃO
            Texture2D finalTexture = _drawer.Texture;

            // Se tivermos uma imagem de UI para preview, atualizamos ela agora
            if (_previewImage != null)
            {
                UpdatePreview(finalTexture);
            }

            return finalTexture;
        }

        /// <summary>
        /// Atualiza o componente de Image da UI com a nova textura.
        /// </summary>
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