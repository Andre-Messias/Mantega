using System.Collections.Generic;
using UnityEngine;

using Mantega.Geometry; 
using Mantega.Diagnostics;

namespace Mantega.Runes
{
    public static class RuneProcessor
    {
        public static List<LineSegment> ProcessDraw(List<LineRenderer> draw, Vector2 targetResolution, float padding, Camera camera = null)
        {
            var extractedLines = ExtractLines(draw, camera);
            var normalizedLines = Normalize(extractedLines, targetResolution, padding);
            return normalizedLines;
        }

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
        
        public static List<LineSegment> Normalize(List<LineSegment> originalLines, Vector2 targetResolution, float padding)
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
            float drawableWidth = targetResolution.x - (padding * 2);
            float drawableHeight = targetResolution.y - (padding * 2);

            float scaleX = drawableWidth / bounds.size.x;
            float scaleY = drawableHeight / bounds.size.y;

            // Avoid distortion: use the smaller scale factor
            float finalScale = Mathf.Min(scaleX, scaleY);

            // Centering
            Vector2 currentCenter = bounds.center;
            Vector2 targetCenter = targetResolution / 2f;

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
        
        private static Bounds CreateLinesBounds(List<LineSegment> lines)
        {
            if (lines.Count == 0) return new Bounds(Vector2.zero, Vector2.zero);

            Vector2 firstPoint = lines[0].Start;
            Bounds bounds = new (firstPoint, Vector3.zero);

            foreach (var line in lines)
            {
                bounds.Encapsulate(line.Start);
                bounds.Encapsulate(line.End);
            }

            return bounds;
        }

        private static Vector2 TransformPoint(Vector2 point, Vector2 oldCenter, Vector2 newCenter, float scale)
        {
            return ((point - oldCenter) * scale) + newCenter;
        }
    }
}