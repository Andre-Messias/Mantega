using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Mantega.Core.Diagnostics;

namespace Mantega.Drawer
{
    /// <summary>
    /// Provides functionality for drawings using line-based rendering.
    /// </summary>
    /// <remarks>The <see cref="DrawingController"/> class enables users to draw lines interactively, manage
    /// drawing configurations, and export the resulting drawings as textures or files. It supports customizable
    /// drawing parameters, such as the  minimum drawing distance and the prefab used for line rendering. The class also
    /// provides methods for clearing the canvas, saving drawings, and handling user input for drawing operations. 
    /// This class is designed to work with Unity's MonoBehaviour and requires a properly configured camera, line
    /// prefab, and parent transform for drawing operations. Ensure that all required fields are set before using the
    /// drawing functionality.</remarks>
    public class DrawingController : MonoBehaviour
    {
        #region DrawingCamera
        /// <summary>
        /// The camera used for rendering drawing-related visuals.
        /// </summary>
        [Header("DrawingCamera")]
        [SerializeField] private Camera _drawingCamera;

        /// <summary>
        /// Gets or sets the camera used for rendering drawings.
        /// </summary>
        public Camera DrawingCamera
        {
            get => _drawingCamera;
            set
            {
                Validations.ValidateNotNull(value, this);
                _drawingCamera = value;
            }
        }
        #endregion

        #region Draw Config
        /// <summary>
        /// The prefab used to create new line objects in the drawing.
        /// </summary>
        [Header("Draw config")]
        [SerializeField] private GameObject _linePrefab;

        /// <summary>
        /// Gets or sets the prefab used to create new line objects.
        /// </summary>
        public GameObject LinePrefab
        {
            get => _linePrefab;
            set
            {
                ValidateLinePrefab(value);
                _linePrefab = value;
            }
        }

        /// <summary>
        /// The parent <see cref="Transform"/> under which drawing objects are instantiated.
        /// </summary>
        [SerializeField] private Transform _drawingParent;

        /// <summary>
        /// Gets or sets the parent <see cref="Transform"/> used for drawing operations.
        /// </summary>
        public Transform DrawingParent
        {
            get => _drawingParent;
            set
            {
                Validations.ValidateNotNull(value, this);
                _drawingParent = value;
            }
        }

        /// <summary>
        /// The minimum distance value used to determine proximity thresholds for the drawing functionality.
        /// </summary>
        [SerializeField, Min(0)] private float _minDrawingDistance = 0.1f;

        /// <summary>
        /// Gets or sets the minimum allowable drawing distance.
        /// </summary>
        public float MinDrawingDistance
        {
            get => _minDrawingDistance;
            set
            {
                Validations.ValidateNotNegative(value, this);
                _minDrawingDistance = value;
            }
        }

        /// <summary>
        /// Indicates whether drawing functionality is enabled.
        /// </summary>
        public bool EnableDrawing = true;
        #endregion

        #region Draw
        /// <summary>
        /// The <see cref="LineRenderer"/> objects that represent the lines drawn on the canvas.
        /// </summary>
        [Header("Draw")]
        [SerializeField] private List<LineRenderer> _lines = new();

        /// <summary>
        /// Gets the collection of <see cref="LineRenderer"/> objects that represent the drawn lines.
        /// </summary>
        public List<LineRenderer> Lines => _lines;

        /// <summary>
        /// Indicates whether the object is currently in drawing mode.
        /// </summary>
        [SerializeField] private bool _isDrawing = false;

        /// <summary>
        /// Gets or sets a value indicating whether the drawing operation is active.
        /// </summary>
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
        #endregion

        // Hidden in inspector
        /// <summary>
        /// Represents the current position of the mouse cursor in screen coordinates.
        /// </summary>
        [HideInInspector] public Vector2 MousePosition;

        // Private variables
        /// <summary>
        /// Represents the current line being rendered by the <see cref="LineRenderer"/>.
        /// </summary>
        private LineRenderer _currentLine;

        /// <summary>
        /// Stores the last recorded position.
        /// </summary>
        private Vector2 _lastPosition;

        private void Awake()
        {
            // Validations
            Validations.ValidateNotNull(_drawingCamera, this);
            ValidateLinePrefab(_linePrefab);
            Validations.ValidateNotNull(_drawingParent, this);
        }

        private void Update()
        {
            HandleDrawing();
        }

        #region Drawing Methods
        /// <summary>
        /// Handles user input for drawing lines based on mouse interactions.
        /// </summary>
        /// <remarks>This method responds to mouse input to create, update, and finalize lines.  - Pressing the
        /// left mouse button starts a new line. - Holding the left mouse button adds points to the current line, provided
        /// the mouse has moved a minimum distance. - Releasing the left mouse button finalizes the current line.</remarks>
        private void HandleDrawing()
        {
            if (!EnableDrawing) return;

            // Draw line
            if (_isDrawing && _currentLine != null)
            {
                Vector2 mousePos = GetPointerWorldPosition();
                if (Vector2.Distance(mousePos, _lastPosition) > _minDrawingDistance)
                {
                    ExtendLine(mousePos);
                }
            }   
        }

        /// <summary>
        /// Initiates the drawing process by setting the drawing state and creating a new line if drawing is enabled.
        /// </summary>
        /// <remarks>This method sets the internal drawing state to active. If <see cref="EnableDrawing"/>
        /// is <see langword="true"/>, it creates a new line to begin the drawing operation. If drawing is not enabled,
        /// the method exits without further action.</remarks>
        private void StartDrawing()
        {
            _isDrawing = true;
            if (!EnableDrawing) return;
            CreateNewLine();
        }

        /// <summary>
        /// Stops the current drawing operation and resets the drawing state.
        /// </summary>
        /// <remarks>This method sets the drawing state to inactive and clears the current line being
        /// drawn. If drawing is not enabled, the method exits without making further changes.</remarks>
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
            GameObject newLine = Instantiate(_linePrefab, Vector3.zero, Quaternion.identity, _drawingParent);
            _currentLine = newLine.GetComponent<LineRenderer>();
            _lines.Add(_currentLine);

            // Initialize line
            _currentLine.positionCount = 0;

            // First point
            Vector2 mousePos = GetPointerWorldPosition();
            ExtendLine(mousePos);
        }

        /// <summary>
        /// Updates the current line by adding a new point at the specified position.
        /// </summary>
        /// <param name="newPos">The position of the new point to add to the line.</param>
        void ExtendLine(Vector2 newPos)
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
            foreach (var line in _lines)
            {
                Destroy(line.gameObject);
            }
            _lines.Clear();
        }
        #endregion

        /// <summary>
        /// Gets the world position of the pointer based on the current screen position.
        /// </summary>
        /// <remarks>The method calculates the world position by projecting the mouse's screen position  onto a
        /// plane at a fixed distance from the camera. Ensure that the <c><see cref="_drawingCamera"/></c>  is properly configured and
        /// active when calling this method.</remarks>
        /// <returns>A <see cref="Vector2"/> representing the world position of the pointer.</returns>
        Vector2 GetPointerWorldPosition()
        {
            return _drawingCamera.ScreenToWorldPoint(MousePosition);
        }

        #region Saving and Exporting Methods

        /// <summary>
        /// Captures the current view of the drawing camera and exports it as a 2D texture. 
        /// </summary>
        /// <remarks>This method renders the current view of the camera to a temporary render texture, 
        /// reads the contents into a new <see cref="Texture2D"/>, and returns the resulting texture. The texture is
        /// created with the dimensions of the screen and uses the RGB24 format.</remarks>
        /// <returns>A <see cref="Texture2D"/> containing the captured view of the drawing camera.</returns>
        public Texture2D ExportDrawingAsTexture()
        {
            // Size
            int width = Screen.width;
            int height = Screen.height;

            // Temporarily render the camera's view to a RenderTexture
            RenderTexture rt = RenderTexture.GetTemporary(width, height, 24);

            // Save previous settings
            RenderTexture previousTarget = _drawingCamera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;

            try
            {
                _drawingCamera.targetTexture = rt;
                _drawingCamera.Render();
                RenderTexture.active = rt;

                // Create a new Texture2D and read the RenderTexture contents into it
                Texture2D screenShot = new(width, height, TextureFormat.RGB24, false);
                screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                screenShot.Apply();
                return screenShot;
            }
            finally
            {
                // Cleanup
                _drawingCamera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>
        /// Saves the current drawing to a file at the specified path in PNG format.
        /// </summary>
        /// <remarks>This method exports the current drawing as a texture, encodes it as a PNG, and writes
        /// it to the specified file. If an error occurs during the save operation, an error message is logged to the
        /// console.</remarks>
        /// <param name="filePath">The full path, including the file name, where the drawing will be saved. The path must be valid and
        /// writable.</param>
        public void SaveDrawingToFile(string filePath)
        {
            try
            {
                ValidateFilePath(filePath);

                Texture2D draw = ExportDrawingAsTexture();

                try 
                {
                    byte[] bytes = draw.EncodeToPNG();
                    string directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    File.WriteAllBytes(filePath, bytes);
                }
                finally
                {
                    if (draw != null) Destroy(draw);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error while saving the image: {e.Message}");
            }
        }

        /// <summary>
        /// Saves the current drawing to a file with a timestamped filename in the application's persistent data path.
        /// </summary>
        /// <remarks>The file is saved in PNG format, and the filename is automatically generated using
        /// the current date and time. The generated filename follows the pattern "Draw_yyyyMMdd_HHmmss.png".</remarks>
        public void SaveDrawingToFile()
        {
            string filename = "Draw_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            string filePath = Path.Combine(Application.persistentDataPath, filename);
            SaveDrawingToFile(filePath);
            Debug.Log("File path: " + filePath);
        }

        #endregion

        #region Validations
        /// <summary>
        /// Validates the specified file path to ensure it is not null or empty.
        /// </summary>
        /// <param name="filePath">The file path to validate.</param>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="filePath"/> is null or an empty string.</exception>
        private void ValidateFilePath(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new System.ArgumentException("File path cannot be null or empty.", nameof(filePath));
            }
        }

        /// <summary>
        /// Validates that the specified line prefab is not null and contains a <see cref="LineRenderer"/> component.
        /// </summary>
        /// <param name="linePrefab">The line prefab to validate. Must not be <see langword="null"/> and must contain a <see
        /// cref="LineRenderer"/> component.</param>
        private void ValidateLinePrefab(GameObject linePrefab)
        {
            Validations.ValidateNotNull(linePrefab, this);
            Validations.ValidateComponentExists<LineRenderer>(linePrefab, this);
        }
        #endregion
    }
}