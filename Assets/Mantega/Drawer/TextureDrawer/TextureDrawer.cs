using System.Collections.Generic;
using UnityEngine;

namespace Mantega.Drawer.TextureDrawer
{
#if UNITY_EDITOR
    using Editor;
#endif

    /// <summary>
    /// Provides utility methods for creating and manipulating textures.
    /// </summary>
    /// <remarks>The <see cref="TextureDrawer"/> class includes methods for creating solid-color textures and
    /// drawing lines on textures. It operates directly on <see cref="Texture2D"/> objects, allowing for efficient pixel
    /// manipulation. This class is particularly useful for dynamically generating or modifying textures in real-time.</remarks>
    [System.Serializable]
    public class TextureDrawer
    {
        #region Line Structs
        /// <summary>
        /// Represents a line segment defined by two points in 2D space.
        /// </summary>
        /// <remarks>The <see cref="LinePoints"/> struct is immutable and provides a way to define a line
        /// segment using its start and end points. Both points are represented as <see cref="Vector2"/>
        /// instances.</remarks>
        [System.Serializable]
        public readonly struct LinePoints
        {
            /// <summary>
            /// The starting point of the line.
            /// </summary>
            public readonly Vector2 Start;

            /// <summary>
            /// The ending point of the line.
            /// </summary>
            public readonly Vector2 End;

            /// <summary>
            /// Initializes a new instance of the <see cref="LinePoints"/> class with the specified start and end
            /// points.
            /// </summary>
            /// <param name="start">The starting point of the line.</param>
            /// <param name="end">The ending point of the line.</param>
            public LinePoints(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }

            /// <summary>
            /// Defines an implicit conversion from a tuple of two <see cref="Vector2"/> points to a <see
            /// cref="LinePoints"/> instance.
            /// </summary>
            /// <param name="points">A tuple containing the start and end points of the line, represented as <see cref="Vector2"/> values.</param>
            public static implicit operator LinePoints((Vector2 start, Vector2 end) points)
            {
                return new LinePoints(points.start, points.end);
            }
        }

        /// <summary>
        /// Represents a straight line defined by its endpoints, thickness, and color.
        /// </summary>
        /// <remarks>The <see cref="Line"/> structure is immutable and provides a way to define a line
        /// segment in 2D space. It includes the start and end points of the line, the line's thickness, and its
        /// color.</remarks>
        [System.Serializable]
        public readonly struct Line
        {
            /// <summary>
            /// The points that define the line.
            /// </summary>
            /// <remarks>This field is read-only and provides the coordinates or data points that
            /// describe the line. It can be used to access the line's geometry or for calculations involving the
            /// line.</remarks>
            public readonly LinePoints LinePoints;

            /// <summary>
            /// The thickness of the line, measured as an integer value.
            /// </summary>
            /// <remarks>This field must be positive</remarks>
            public readonly int Thickness;

            /// <summary>
            /// The color of the line.
            /// </summary>
            public readonly Color Color;

            /// <summary>
            /// Initializes a new instance of <see cref="Line"/>.
            /// </summary>
            /// <param name="linePoints">The points that define the start and end of the line.</param>
            /// <param name="thickness">The thickness of the line. Must be greater than zero.</param>
            /// <param name="color">The color of the line.</param>
            /// <exception cref="System.ArgumentOutOfRangeException">Thrown if <paramref name="thickness"/> is less than or equal to zero.</exception>
            public Line(LinePoints linePoints, int thickness, Color color)
            {
                if (thickness <= 0)
                {
                    throw new System.ArgumentOutOfRangeException(nameof(thickness), thickness, $"{nameof(thickness)} must be greater than zero.");
                }

                this.LinePoints = linePoints;
                Thickness = thickness;
                Color = color;
            }

            /// <inheritdoc cref="Line(LinePoints, int, Color)"/>
            /// <param name="start">The starting point of the line.</param>
            /// <param name="end">The ending point of the line.</param>
            public Line(Vector2 start, Vector2 end, int thickness, Color color) : this(new LinePoints(start, end), thickness, color)
            {

            }
        }
        #endregion

        #region Texture
        /// <summary>
        /// The texture being drawn on.
        /// </summary>
        [Header("Texture")]
#if UNITY_EDITOR
        [CallOnChange(nameof(OnEditorChangeTexture))]
#endif
        [SerializeField] private Texture2D _texture;

        /// <summary>
        /// Gets or sets the texture being drawn on.
        /// </summary>
        public Texture2D Texture
        {
            get => _texture;
            set
            {
                ValidateTexture(value);
                _texture = value;
            }
        }
        #endregion

        #region Brush Settings
        /// <summary>
        /// The color of the brush used for drawing lines.
        /// </summary>
        [Header("Brush settings")]
        [SerializeField] private Color _brushColor = Color.black;

        /// <summary>
        /// Gets or sets the color of the brush used for drawing lines.
        /// </summary>
        public Color BrushColor
        {
            get => _brushColor;
            set => SetLineBrushColor(value);
        }

        /// <summary>
        /// Sets the color of the line brush used for drawing operations.
        /// </summary>
        /// <param name="color">The <see cref="Color"/> to set as the brush color.</param>
        /// <returns>The current <see cref="TextureDrawer"/> instance, allowing for method chaining.</returns>
        public TextureDrawer SetLineBrushColor(Color color)
        {
            _brushColor = color;
            return this;
        }

        /// <summary>
        /// The thickness of the brush used for drawing lines, measured in pixels.
        /// </summary>
        [SerializeField][Min(1)] private int _brushThickness = 1;

        /// <summary>
        /// Gets or sets the thickness of the line used for drawing.
        /// </summary>
        public int BrushThickness
        {
            get => _brushThickness;
            set => SetBrushThickness(value);
        }

        /// <summary>
        /// Sets the thickness of the brush used for drawing.
        /// </summary>
        /// <param name="thickness">The desired brush thickness. Must be a positive integer. Values less than 1 will be clamped to 1.</param>
        /// <returns>The current <see cref="TextureDrawer"/> instance, allowing for method chaining.</returns>
        public TextureDrawer SetBrushThickness(int thickness)
        {
            _brushThickness = Mathf.Max(1, thickness);
            return this;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of <see cref="TextureDrawer"/>.
        /// </summary>
        /// <remarks>The created texture is initialized as a solid color texture based on the specified
        /// <paramref name="backgroundColor"/> so only call this within Unity lifecycle methods.</remarks>
        /// <param name="width">The width of the texture, in pixels. Must be greater than 0.</param>
        /// <param name="height">The height of the texture, in pixels. Must be greater than 0.</param>
        /// <param name="backgroundColor">The background color of the texture. If null, the texture will be initialized with a transparent color.</param>
        /// <param name="filterMode">The filter mode to apply to the texture. Defaults to <see cref="FilterMode.Point"/>.</param>
        public TextureDrawer(int width, int height, Color? backgroundColor = null, FilterMode filterMode = FilterMode.Point)
        {
            Color bgFinalColor = backgroundColor ?? Color.clear;
            _texture = CreateSolidTexture(width, height, bgFinalColor, filterMode);
        }

        /// <summary>
        /// Initializes a new instance of <see cref="TextureDrawer"/>.
        /// </summary>
        /// <remarks>This constructor creates a default instance of the <see cref="TextureDrawer"/> class.
        /// Use this class to manage and render textures in your application.</remarks>
        public TextureDrawer() { }
        #endregion

        #region Draw Methods
        /// <summary>
        /// Draws a line between two points on the texture using the specified brush settings.
        /// </summary>
        /// <param name="start">The starting point of the line, represented as a <see cref="Vector2"/>.</param>
        /// <param name="end">The ending point of the line, represented as a <see cref="Vector2"/>.</param>
        /// <returns>The current <see cref="TextureDrawer"/> instance, allowing for method chaining.</returns>
        public TextureDrawer DrawLine(Vector2 start, Vector2 end)
        {
            DrawLine(_texture, start, end, _brushThickness, _brushColor);
            return this;
        }

        /// <inheritdoc cref="DrawLine(Vector2, Vector2)"/>
        /// <param name="line">The line to be drawn, defined by its start and end points.</param>
        public TextureDrawer DrawLine(Line line)
        {
            DrawLine(_texture, line);
            return this;
        }

        /// <summary>
        /// Draws the specified lines onto the texture.
        /// </summary>
        /// <param name="lines">A list of <see cref="Line"/> objects representing the lines to be drawn. Cannot be null.</param>
        /// <returns>The current <see cref="TextureDrawer"/> instance, allowing for method chaining.</returns>
        public TextureDrawer DrawLines(List<Line> lines)
        {
            DrawLines(_texture, lines);
            return this;
        }

        /// <inheritdoc cref="DrawLines(List{Line})"/>
        /// <param name="linePoints">A list of <see cref="LinePoints"/> objects representing the start and end points of each line to be drawn.</param>
        public TextureDrawer DrawLines(List<LinePoints> linePoints)
        {
            DrawLines(_texture, linePoints, _brushThickness, _brushColor);
            return this;
        }
        #endregion

        #region Static Methods

        /// <summary>
        /// Validates the specified texture to ensure it meets the required conditions for processing.
        /// </summary>
        /// <param name="texture">The <see cref="Texture2D"/> to validate.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="texture"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.ArgumentException">Thrown if <paramref name="texture"/> is not readable. Ensure the texture is marked as readable in its import
        /// settings.</exception>
        private static void ValidateTexture(Texture2D texture)
        {
            if (texture == null)
            {
                throw new System.ArgumentNullException(nameof(texture));
            }

            if (!texture.isReadable)
            {
                throw new System.ArgumentException($"{nameof(texture)} must be readable. Ensure the texture is marked as readable in its import settings.", nameof(texture));
            }
        }

        /// <summary>
        /// Validates the specified width and height.
        /// </summary>
        /// <param name="width">The width to validate. Must be greater than zero.</param>
        /// <param name="height">The height to validate. Must be greater than zero.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if <paramref name="width"/> or <paramref name="height"/> is less than or equal to zero.</exception>
        private static void ValidateSize(int width, int height)
        {
            if (width <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(width), width, $"{nameof(width)} must be greater than zero.");
            }
            if (height <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(height), height, $"{nameof(height)} must be greater than zero.");
            }
        }

        /// <inheritdoc cref="CreateTransparentTexture(int, int, FilterMode)"/>
        /// <summary>
        /// Creates a solid color texture.
        /// </summary>
        /// <remarks>The created texture uses the RGBA32 format and does not include mipmaps. The <see
        /// cref="Texture2D.Apply"/> method is called internally to finalize the texture after setting its
        /// pixels.</remarks>
        /// <param name="color">The color to fill the texture with.</param>
        /// <returns>A <see cref="Texture2D"/> object filled with the specified solid color.</returns>
        public static Texture2D CreateSolidTexture(int width, int height, Color color, FilterMode filter = FilterMode.Point)
        {
            Texture2D tex = CreateTransparentTexture(width, height, filter);

            Color32[] pixels = new Color32[width * height];
            Color32 color32 = color;

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color32;
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            return tex;
        }

        /// <summary>
        /// Creates a new transparent texture.
        /// </summary>
        /// <param name="width">The width of the texture, in pixels. Must be greater than 0.</param>
        /// <param name="height">The height of the texture, in pixels. Must be greater than 0.</param>
        /// <param name="filter">The filter mode to apply to the texture. Defaults to <see cref="FilterMode.Point"/>.</param>
        /// <returns>A new <see cref="Texture2D"/> instance with the specified dimensions and filter mode, initialized with a
        /// transparent RGBA32 format.</returns>
        private static Texture2D CreateTransparentTexture(int width, int height, FilterMode filter = FilterMode.Point)
        {
            // Validation
            ValidateSize(width, height);

            return new(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = filter
            };
        }

        #region Draw Lines Static Methods

        /// <inheritdoc cref="DrawLines(Texture2D, List{Line})"/>
        /// <param name="tex">The <see cref="Texture2D"/> on which the lines will be drawn. This texture will be modified directly.</param>
        /// <param name="linePoints">A list of <see cref="LinePoints"/> objects, each defining the start and end points of a line to be drawn.</param>
        /// <param name="thickness">The thickness of the lines to be drawn, in pixels. Must be a positive integer.</param>
        /// <param name="color">The color of the lines to be drawn.</param>
        public static void DrawLines(Texture2D tex, List<LinePoints> linePoints, int thickness, Color color)
        {
            // Validation
            ValidateTexture(tex);

            Color32[] pixels = tex.GetPixels32();
            int width = tex.width;
            int height = tex.height;
            foreach (var points in linePoints)
            {
                DrawLineInMemory(pixels, width, height, points.Start, points.End, thickness, color);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
        }

        /// <summary>
        /// Draws multiple lines on the specified texture.
        /// </summary>
        /// <remarks>This method modifies the provided texture directly by drawing the specified lines
        /// onto it. After drawing, the texture is updated to reflect the changes.</remarks>
        /// <param name="tex">The <see cref="Texture2D"/> on which the lines will be drawn. The texture must be writable.</param>
        /// <param name="lines">A list of <see cref="Line"/> objects representing the lines to be drawn.</param>
        public static void DrawLines(Texture2D tex, List<Line> lines)
        {
            // Validation
            ValidateTexture(tex);

            Color32[] pixels = tex.GetPixels32();
            int width = tex.width;
            int height = tex.height;

            foreach (var line in lines)
            {
                DrawLineInMemory(pixels, width, height, line.LinePoints.Start, line.LinePoints.End, line.Thickness, line.Color);
            }

            tex.SetPixels32(pixels);
            tex.Apply();
        }

        /// <summary>
        /// Draws a single line on the specified texture.
        /// </summary>
        /// <remarks>This method modifies the provided texture by drawing the specified line directly onto
        /// it. Ensure the texture is writable and properly initialized before calling this method.</remarks>
        /// <param name="start">The start point.</param>
        /// <param name="end">The end point.</param>
        /// <param name="thickness">Thickness in pixels.</param>
        /// <param name="color">Color of the line.</param>
        public static void DrawLine(Texture2D tex, Vector2 start, Vector2 end, int thickness, Color color)
        {
            // Validation
            ValidateTexture(tex);

            Color32[] pixels = tex.GetPixels32();
            DrawLineInMemory(pixels, tex.width, tex.height, start, end, thickness, color);

            tex.SetPixels32(pixels);
            tex.Apply();
        }

        /// <inheritdoc cref="DrawLine(Texture2D, Vector2, Vector2, int, Color)"/>
        /// <param name="tex">The texture on which the line will be drawn. Cannot be null.</param>
        /// <param name="line">The line to draw, defined by its start and end points. Cannot be null.</param>
        public static void DrawLine(Texture2D tex, Line line)
        {
            DrawLine(tex, line.LinePoints.Start, line.LinePoints.End, line.Thickness, line.Color);
        }

        /// <summary>
        /// Draws a line between two points in a pixel buffer.
        /// </summary>
        /// <remarks>This method modifies the <paramref name="pixels"/> array directly to render the line.
        /// The line is drawn using a brush-like approach to ensure smooth rendering, and the thickness is applied
        /// symmetrically around the line path. The method assumes that the pixel buffer is large enough to accommodate
        /// the specified dimensions (<paramref name="width"/> × <paramref name="height"/>).</remarks>
        /// <param name="pixels">The pixel buffer to draw into, represented as a one-dimensional array of <see cref="Color32"/> values.</param>
        /// <param name="width">The width of the pixel buffer, in pixels.</param>
        /// <param name="height">The height of the pixel buffer, in pixels.</param>
        /// <param name="start">The starting point of the line, specified as a <see cref="Vector2"/>.</param>
        /// <param name="end">The ending point of the line, specified as a <see cref="Vector2"/>.</param>
        /// <param name="thickness">The thickness of the line, in pixels. Must be a positive value.</param>
        /// <param name="color">The color of the line, specified as a <see cref="Color"/>.</param>
        private static void DrawLineInMemory(Color32[] pixels, int width, int height, Vector2 start, Vector2 end, int thickness, Color color)
        {
            float distance = Vector2.Distance(start, end);
            float step = Mathf.Max(1f, thickness * 0.25f);

            Color32 color32 = color;

            for (float i = 0; i <= distance; i += step)
            {
                float t = i / distance;
                float x = Mathf.Lerp(start.x, end.x, t);
                float y = Mathf.Lerp(start.y, end.y, t);

                DrawBrush(pixels, width, height, x, y, thickness, color32);
            }

            DrawBrush(pixels, width, height, end.x, end.y, thickness, color32);
        }
        #endregion

        /// <summary>
        /// Draws a circular brush on a 2D pixel array, filling the specified area with the given color.
        /// </summary>
        /// <remarks>This method modifies the <paramref name="pixels"/> array directly, setting the color
        /// of pixels within the circular area defined by the brush. Pixels outside the bounds of the image (<paramref
        /// name="width"/> and <paramref name="height"/>) are ignored.</remarks>
        /// <param name="pixels">A 1D array representing the pixel data of a 2D image. The array is organized in row-major order, where each
        /// row is contiguous in memory.</param>
        /// <param name="width">The width of the image, in pixels.</param>
        /// <param name="height">The height of the image, in pixels.</param>
        /// <param name="cx">The x-coordinate of the center of the brush, in pixels.</param>
        /// <param name="cy">The y-coordinate of the center of the brush, in pixels.</param>
        /// <param name="diameter">The diameter of the circular brush, in pixels. Must be a positive value.</param>
        /// <param name="color">The color to apply to the pixels within the brush area.</param>
        private static void DrawBrush(Color32[] pixels, int width, int height, float cx, float cy, int diameter, Color32 color)
        {
            float raio = diameter / 2f;
            float raioQuadrado = raio * raio;

            int minX = Mathf.RoundToInt(cx - raio);
            int maxX = Mathf.RoundToInt(cx + raio);
            int minY = Mathf.RoundToInt(cy - raio);
            int maxY = Mathf.RoundToInt(cy + raio);

            for (int y = minY; y <= maxY; y++)
            {
                // Inside bounds check for Y
                if (y < 0 || y >= height) continue;

                // Since we are using a 1D array, calculate the Y offset once per row
                int yOffset = y * width;

                for (int x = minX; x <= maxX; x++)
                {
                    // Inside bounds check for X
                    if (x < 0 || x >= width) continue;

                    float dx = x - cx;
                    float dy = y - cy;

                    // Check if the pixel is within the brush circle
                    if ((dx * dx + dy * dy) <= raioQuadrado)
                    {
                        // Set the pixel color directly in the 1D array
                        int index = yOffset + x;

                        // If the color is fully opaque, set it directly; otherwise, blend it
                        if (color.a == 255)
                        {
                            pixels[index] = color;
                        }
                        else
                        {
                            Color32 corFundo = pixels[index];
                            pixels[index] = Color32.Lerp(corFundo, color, color.a / 255f);
                        }
                    }
                }
            }
        }

        #endregion

        #region Editor Methods
#if UNITY_EDITOR
        private void OnEditorChangeTexture(Texture2D _, Texture2D newTex)
        {
            ValidateTexture(newTex);
        }
#endif
        #endregion
    }
}