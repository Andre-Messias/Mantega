using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DrawingProcessor : MonoBehaviour
{
    [Header("Referências")]
    public DrawingManager drawingManager;
    public Camera processingCamera;

    [Header("Configuração de Saída")]
    public int outputSize = 105;   // 105x105
    public float padding = 1.2f;   // Margem de segurança

    [Header("Debug")]
    public bool saveToDisk = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            ProcessAndSaveImage();
        }
    }

    public Texture2D ProcessAndSaveImage()
    {
        // 1. Validação básica
        if (drawingManager.createdLines.Count == 0)
        {
            Debug.LogWarning("Nenhuma linha para processar.");
            return null;
        }

        // 2. Calcula os limites do desenho original para saber onde posicionar a câmera
        Bounds bounds = CalculateBounds();

        // 3. Cria o "Clone Fantasma" (O desenho preto temporário)
        GameObject ghostRoot = CreateGhostDrawing();

        // 4. Configura a câmera e o visual
        SetupProcessingCamera(bounds);

        // Garante fundo branco
        Color originalBg = processingCamera.backgroundColor;
        processingCamera.backgroundColor = Color.white;

        // 5. Renderiza
        RenderTexture rt = new RenderTexture(outputSize, outputSize, 24);
        rt.filterMode = FilterMode.Point;
        processingCamera.targetTexture = rt;

        RenderTexture.active = rt;
        processingCamera.Render();

        // 6. Lê os pixels
        Texture2D resultTex = new Texture2D(outputSize, outputSize, TextureFormat.RGB24, false);
        resultTex.ReadPixels(new Rect(0, 0, outputSize, outputSize), 0, 0);
        resultTex.Apply();

        // 7. Limpeza
        processingCamera.targetTexture = null;
        RenderTexture.active = null;
        processingCamera.backgroundColor = originalBg;

        Destroy(rt);
        Destroy(ghostRoot); // <--- Destrói a cópia preta, o desenho original fica intacto

        // 8. Salvar
        if (saveToDisk)
        {
            SaveTexture(resultTex);
        }

        return resultTex;
    }

    /// <summary>
    /// Cria uma cópia exata do desenho atual, mas com linhas pretas.
    /// </summary>
    GameObject CreateGhostDrawing()
    {
        // Cria um objeto pai para organizar
        GameObject root = new GameObject("TEMP_GHOST_DRAWING");

        // Material preto simples
        Material blackMaterial = new Material(Shader.Find("Sprites/Default"));

        foreach (var originalLine in drawingManager.createdLines)
        {
            // Cria um novo GameObject para a linha
            GameObject ghostLineObj = new GameObject("GhostLine");
            ghostLineObj.transform.SetParent(root.transform);

            // Copia a posição e rotação (embora world space resolva, é bom garantir)
            ghostLineObj.transform.position = originalLine.transform.position;
            ghostLineObj.transform.rotation = originalLine.transform.rotation;
            ghostLineObj.transform.localScale = originalLine.transform.localScale;

            // Adiciona e configura o LineRenderer
            LineRenderer ghostLine = ghostLineObj.AddComponent<LineRenderer>();

            // --- Cópia das Propriedades Visuais ---
            ghostLine.useWorldSpace = originalLine.useWorldSpace;
            ghostLine.loop = originalLine.loop;
            ghostLine.widthMultiplier = originalLine.widthMultiplier; // Mantém a espessura original
            ghostLine.startWidth = originalLine.startWidth;
            ghostLine.endWidth = originalLine.endWidth;
            ghostLine.numCapVertices = originalLine.numCapVertices;
            ghostLine.numCornerVertices = originalLine.numCornerVertices;

            // --- Define Cor Preta ---
            ghostLine.material = blackMaterial;
            ghostLine.startColor = Color.black;
            ghostLine.endColor = Color.black;

            // --- Cópia dos Pontos (Geometria) ---
            Vector3[] positions = new Vector3[originalLine.positionCount];
            originalLine.GetPositions(positions);
            ghostLine.positionCount = originalLine.positionCount;
            ghostLine.SetPositions(positions);
        }

        return root;
    }

    Bounds CalculateBounds()
    {
        var lines = drawingManager.createdLines;
        if (lines.Count == 0) return new Bounds(Vector3.zero, Vector3.one);

        Vector3 firstPoint = lines[0].GetPosition(0);
        Bounds b = new Bounds(firstPoint, Vector3.zero);

        foreach (var line in lines)
        {
            for (int i = 0; i < line.positionCount; i++)
            {
                b.Encapsulate(line.GetPosition(i));
            }
        }
        return b;
    }

    void SetupProcessingCamera(Bounds bounds)
    {
        Vector3 center = bounds.center;
        center.z = -10f; // Afasta a câmera
        processingCamera.transform.position = center;

        float maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y);
        if (maxExtent == 0) maxExtent = 1f;

        processingCamera.orthographicSize = maxExtent * padding;
        processingCamera.orthographic = true;
    }

    void SaveTexture(Texture2D tex)
    {
        byte[] bytes = tex.EncodeToPNG();
        string filename = "Processado_Clone_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllBytes(filePath, bytes);
        Debug.Log($"Imagem processada salva em: {filePath}");
    }
}