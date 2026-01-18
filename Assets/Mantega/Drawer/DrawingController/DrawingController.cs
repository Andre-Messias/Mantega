using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class DrawingController : MonoBehaviour
{
    [Header("Components")]
    public Camera DrawingCamera;

    [Header("Draw config")]
    public GameObject LinePrefab;
    public Transform DrawingParent;
    public float MinDistance = 0.1f;
    public bool EnableDrawing = true;

    [Header("Draw")]
    public List<LineRenderer> CreatedLines = new();
    [SerializeField] private bool _isDrawing = false;
    public bool IsDrawing
    {
        get => _isDrawing;
        set
        {
            if (value)
                StartDrawing();
            else
                StopDrawing();
        }
    }

    // Hidden in inspector
    [HideInInspector] public Vector2 MousePosition;

    // Private variables
    private LineRenderer _currentLine;
    private Vector2 _lastPosition;

    void Update()
    {
        HandleDrawing();
    }

    /// <summary>
    /// Handles user input for drawing lines based on mouse interactions.
    /// </summary>
    /// <remarks>This method responds to mouse input to create, update, and finalize lines.  - Pressing the
    /// left mouse button starts a new line. - Holding the left mouse button adds points to the current line, provided
    /// the mouse has moved a minimum distance. - Releasing the left mouse button finalizes the current line.</remarks>
    void HandleDrawing()
    {
        if (!EnableDrawing) return;

        // Draw line
        if (_isDrawing && _currentLine != null)
        {
            Vector2 mousePos = GetWorldPosition();
            if (Vector2.Distance(mousePos, _lastPosition) > MinDistance)
            {
                UpdateLine(mousePos);
            }
        }   
    }

    private void StartDrawing()
    {
        _isDrawing = true;
        if (!EnableDrawing) return;
        CreateNewLine();
    }

    private void StopDrawing()
    {
        _isDrawing = false;
        if (!EnableDrawing) return;
        _currentLine = null;
    }

    /// <summary>
    /// Creates a new line and initializes it with the first point at the current mouse position.
    /// </summary>
    /// <remarks>This method instantiates a new line object using the specified prefab and sets it as a child
    /// of the drawing parent. The line is initialized with zero points, and the first point is added based on the
    /// current mouse position.</remarks>
    private void CreateNewLine()
    {
        // Instantiate line object
        GameObject newLine = Instantiate(LinePrefab, Vector3.zero, Quaternion.identity, DrawingParent);
        _currentLine = newLine.GetComponent<LineRenderer>();
        CreatedLines.Add(_currentLine);

        // Initialize line
        _currentLine.positionCount = 0;

        // First point
        Vector2 mousePos = GetWorldPosition();
        UpdateLine(mousePos);
    }

    /// <summary>
    /// Updates the current line by adding a new point at the specified position.
    /// </summary>
    /// <param name="newPos">The position of the new point to add to the line.</param>
    void UpdateLine(Vector2 newPos)
    {
        _currentLine.positionCount++;
        _currentLine.SetPosition(_currentLine.positionCount - 1, newPos);
        _lastPosition = newPos;
    }

    /// <summary>
    /// Clears all lines from the canvas and releases associated resources.
    /// </summary>
    /// <remarks>This method removes all created lines from the canvas and destroys their associated game
    /// objects. After calling this method, the collection of created lines will be empty.</remarks>
    public void ClearCanvas()
    {
        foreach (var line in CreatedLines)
        {
            Destroy(line.gameObject);
        }
        CreatedLines.Clear();
    }

    /// <summary>
    /// Gets the world position of the mouse cursor based on the current screen position.
    /// </summary>
    /// <remarks>The method calculates the world position by projecting the mouse's screen position  onto a
    /// plane at a fixed distance from the camera. Ensure that the <c>drawingCamera</c>  is properly configured and
    /// active when calling this method.</remarks>
    /// <returns>A <see cref="Vector2"/> representing the world position of the mouse cursor.</returns>
    Vector2 GetWorldPosition()
    {
        return DrawingCamera.ScreenToWorldPoint(MousePosition);
    }

    // --- FUNÇÃO DE SALVAR IMAGEM ---
    public void SaveDrawingToFile()
    {
        // 1. Define o tamanho da imagem final (ex: 1024x1024 ou o tamanho da tela)
        int width = Screen.width;
        int height = Screen.height;

        // 2. Cria uma textura temporária para renderizar a câmera
        RenderTexture rt = new RenderTexture(width, height, 24);
        DrawingCamera.targetTexture = rt;

        // 3. Renderiza manualmente a câmera
        RenderTexture.active = rt;
        DrawingCamera.Render();

        // 4. Lê os pixels para uma Texture2D
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // 5. Limpa a sujeira
        DrawingCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // 6. Converte para PNG e Salva
        byte[] bytes = screenShot.EncodeToPNG();
        string filename = "Desenho_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        string filePath = Path.Combine(Application.persistentDataPath, filename);

        File.WriteAllBytes(filePath, bytes);

        Debug.Log("Imagem salva em: " + filePath);
    }

    public Texture2D GetDrawAsTexture()
    {
        return null;
    }
}
