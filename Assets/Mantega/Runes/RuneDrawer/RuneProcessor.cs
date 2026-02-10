using System.Collections.Generic;
using UnityEngine;

using Mantega.Core.Diagnostics;
using Mantega.Geometry; 

namespace Mantega.Runes
{
    /// <summary>
    /// Provides methods for processing and normalizing drawn line renderers.
    /// </summary>
    /// <remarks>The <see cref="RuneProcessor"/> class includes functionality to extract, normalize, and
    /// transform collections of drawn lines represented by <see cref="LineRenderer"/> objects. It is designed to
    /// standardize the dimensions and positions of drawn lines to fit within a specified resolution and padding.</remarks>
    public static class RuneProcessor
    {
        /// <summary>
        /// Processes a collection of drawn line renderers, normalizing them to fit within a specified resolution and
        /// padding.
        /// </summary>
        /// <remarks>This method extracts the lines from the provided <paramref name="draw"/> collection,
        /// normalizes their positions and dimensions to fit within the specified <paramref name="targetResolution"/>
        /// and <paramref name="padding"/>, and returns the processed lines as a list of <see cref="LineSegment"/>
        /// objects.</remarks>
        /// <param name="draw">A list of <see cref="LineRenderer"/> objects representing the drawn lines to process.</param>
        /// <param name="targetResolution">The target resolution, specified as a <see cref="Vector2Int"/>, to which the lines will be normalized.</param>
        /// <param name="padding">The padding in pixels to apply around the normalized lines.</param>
        /// <param name="camera">An optional <see cref="Camera"/> used to extract the lines. If <see cref="null"/>, the default camera is used.</param>
        /// <returns>A list of <see cref="LineSegment"/> objects representing the normalized lines.</returns>
        public static List<LineSegment> ProcessDraw(List<LineRenderer> draw, Vector2Int targetResolution, Vector2 padding, Camera camera = null)
        {
            var extractedLines = ExtractLines(draw, camera);
            var normalizedLines = Normalize(extractedLines, targetResolution, padding);
            return normalizedLines;
        }

        /// <inheritdoc cref="ProcessDraw(List{LineRenderer}, Vector2Int, Vector2, Camera)"/>
        public static List<LineSegment> ProcessDraw(List<LineRenderer> draw, Vector2Int targetResolution, float padding, Camera camera = null)
        {
            return ProcessDraw(draw, targetResolution, new Vector2(padding, padding), camera);
        }

        /// <summary>
        /// Extracts a collection of <see cref="LineSegment"/> from the specified list of line renderers.
        /// </summary>
        /// <param name="renderers">A list of <see cref="LineRenderer"/> objects from which to extract line segments. This parameter cannot be
        /// <see langword="null"/>.</param>
        /// <param name="camera">An optional <see cref="Camera"/> used to transform the line segments. If <see langword="null"/>, the line
        /// segments are extracted without applying any camera-specific transformations.</param>
        /// <returns>A list of <see cref="LineSegment"/> objects representing the extracted line segments. The list will be
        /// empty if no line segments are extracted.</returns>
        public static List<LineSegment> ExtractLines(List<LineRenderer> renderers, Camera camera = null)
        {
            Validations.ValidateNotNull(renderers);

            var extractedLines = new List<LineSegment>();

            foreach (var lr in renderers)
            {
                extractedLines.AddRange(lr.ToLineSegments(camera));
            }

            return extractedLines;
        }
        
        /// <summary>
        /// Normalizes a collection of line segments to fit within a specified resolution.
        /// </summary>
        /// <remarks>This method ensures that the original aspect ratio of the line segments is preserved
        /// by using the smaller of the horizontal and vertical scale factors. If the bounding box of the original lines
        /// has zero width or height, the method returns a new <see cref="List{LineSegment}"/>.</remarks>
        /// <param name="originalLines">The list of <see cref="LineSegment"/> objects to normalize. If null or empty, an empty list is returned.</param>
        /// <param name="targetResolution">The target resolution, specified as a <see cref="Vector2Int"/> representing the width and height in pixels.
        /// Must be non-negative.</param>
        /// <param name="padding">The padding in pixels to apply around the normalized content.</param>
        /// <returns>A new list of <see cref="LineSegment"/> objects that have been scaled and centered to fit within the
        /// specified resolution and padding.</returns>
        public static List<LineSegment> Normalize(List<LineSegment> originalLines, Vector2Int targetResolution, Vector2 padding)
        {
            Validations.ValidateNotNegative(targetResolution);

            if (originalLines == null || originalLines.Count == 0)
                return new List<LineSegment>();

            // Draw bounding box of the original content
            Bounds bounds = CreateLinesBounds(originalLines);

            // Avoid division by zero
            if (bounds.size.x == 0 || bounds.size.y == 0)
                return new List<LineSegment>(originalLines);

            // Scale
            float drawableWidth = targetResolution.x - (padding.x * 2);
            float drawableHeight = targetResolution.y - (padding.y * 2);

            float scaleX = drawableWidth / bounds.size.x;
            float scaleY = drawableHeight / bounds.size.y;

            // Avoid distortion: use the smaller scale factor
            float finalScale = Mathf.Min(scaleX, scaleY);

            // Centering
            Vector2 currentCenter = bounds.center;
            Vector2 targetCenter = targetResolution / 2;

            // Construct new lines
            var processedLines = new List<LineSegment>(originalLines.Count);

            foreach (var line in originalLines)
            {
                Vector2 newStart = TransformPoint(line.Start, currentCenter, targetCenter, finalScale);
                Vector2 newEnd = TransformPoint(line.End, currentCenter, targetCenter, finalScale);

                processedLines.Add(new LineSegment(newStart, newEnd));
            }

            return processedLines;
        }

        /// <inheritdoc cref="Normalize(List{LineSegment}, Vector2Int, Vector2)"/>
        public static List<LineSegment> Normalize(List<LineSegment> originalLines, Vector2Int targetResolution, float padding)
        {
            return Normalize(originalLines, targetResolution, new Vector2(padding, padding));
        }

        /// <summary>
        /// Creates a bounding box that encapsulates the start and end points of the specified line segments.
        /// </summary>
        /// <param name="lines">A list of <see cref="LineSegment"/> objects representing the line segments to include in the bounds.</param>
        /// <returns>A <see cref="Bounds"/> object that represents the smallest axis-aligned bounding box containing all the
        /// start and end points of the provided line segments. If the list is empty, a bounds object with a center at
        /// <see cref="Vector2.zero"/> and size <see cref="Vector2.zero"/> is returned.</returns>
        private static Bounds CreateLinesBounds(List<LineSegment> lines)
        {
            if (lines.Count == 0) return new Bounds(Vector2.zero, Vector2.zero);

            Vector2 firstPoint = lines[0].Start;
            Bounds bounds = new (firstPoint, Vector2.zero);

            foreach (var line in lines)
            {
                bounds.Encapsulate(line.Start);
                bounds.Encapsulate(line.End);
            }

            return bounds;
        }

        /// <summary>
        /// Transforms a point from one coordinate space to another by applying a scale and translation.
        /// </summary>
        /// <param name="point">The point to transform, represented as a <see cref="Vector2"/>.</param>
        /// <param name="oldCenter">The origin of the old coordinate space, represented as a <see cref="Vector2"/>.</param>
        /// <param name="newCenter">The origin of the new coordinate space, represented as a <see cref="Vector2"/>.</param>
        /// <param name="scale">The scaling factor to apply to the point during the transformation.</param>
        /// <returns>A <see cref="Vector2"/> representing the transformed point in the new coordinate space.</returns>
        private static Vector2 TransformPoint(Vector2 point, Vector2 oldCenter, Vector2 newCenter, float scale)
        {
            return ((point - oldCenter) * scale) + newCenter;
        }
    }
}