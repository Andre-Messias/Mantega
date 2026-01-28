namespace Mantega.Geometry
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Represents a line segment defined by two points in 2D space.
    /// </summary>
    /// <remarks>The <see cref="LineSegment"/> struct is immutable and provides a way to define a line
    /// segment using its start and end points. Both points are represented as <see cref="Vector2"/>
    /// instances.</remarks>
    [System.Serializable]
    public readonly struct LineSegment
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
        /// Initializes a new instance of the <see cref="LineSegment"/> class with the specified start and end
        /// points.
        /// </summary>
        /// <param name="start">The starting point of the line.</param>
        /// <param name="end">The ending point of the line.</param>
        public LineSegment(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Defines an implicit conversion from a tuple of two <see cref="Vector2"/> points to a <see
        /// cref="LineSegment"/> instance.
        /// </summary>
        /// <param name="points">A tuple containing the start and end points of the line, represented as <see cref="Vector2"/> values.</param>
        public static implicit operator LineSegment((Vector2 start, Vector2 end) points)
        {
            return new LineSegment(points.start, points.end);
        }

        /// <summary>
        /// Gets the length of the line segment.
        /// </summary>
        public float Length => Vector2.Distance(Start, End);

        /// <summary>
        /// Gets the normalized direction vector of the line segment.
        /// </summary>
        public Vector2 Direction => (End - Start).normalized;
    }

    /// <summary>
    /// Provides extension methods for the <see cref="LineRenderer"/> class.
    /// </summary>
    /// <remarks>This class includes utility methods to simplify working with <see cref="LineRenderer"/>
    /// objects, such as converting their positions into other representations. These methods are designed to enhance
    /// the functionality of <see cref="LineRenderer"/> without modifying its core behavior.</remarks>
    public static class LineRendererExtensions
    {
        /// <summary>
        /// Converts the positions of a <see cref="LineRenderer"/> into a collection of line segments.
        /// </summary>
        /// <remarks>Each line segment is defined by two consecutive positions in the <paramref
        /// name="lineRenderer"/>. If a <paramref name="camera"/> is provided, the positions are transformed to screen
        /// space using the camera's perspective.</remarks>
        /// <param name="lineRenderer">The <see cref="LineRenderer"/> whose positions will be converted to line segments. Must not be <see
        /// langword="null"/>.</param>
        /// <param name="camera">An optional <see cref="Camera"/> used to transform the positions from world space to screen space. If <see
        /// langword="null"/>, the positions are used as-is in world space.</param>
        /// <returns>A list of <see cref="LineSegment"/> objects representing the line segments defined by the positions of the
        /// <paramref name="lineRenderer"/>. Returns an empty list if the <paramref name="lineRenderer"/> is <see
        /// langword="null"/> or contains fewer than two positions.</returns>
        public static List<LineSegment> ToLineSegments(this LineRenderer lineRenderer, Camera camera = null)
        {
            var segments = new List<LineSegment>();

            if (lineRenderer == null || lineRenderer.positionCount < 2)
                return segments;

            Vector3[] positions = new Vector3[lineRenderer.positionCount];
            lineRenderer.GetPositions(positions);

            for (int i = 0; i < positions.Length - 1; i++)
            {
                Vector2 start;
                Vector2 end;

                if (camera != null)
                {
                    start = camera.WorldToScreenPoint(positions[i]);
                    end = camera.WorldToScreenPoint(positions[i + 1]);
                }
                else
                {
                    start = positions[i];
                    end = positions[i + 1];
                }

                segments.Add(new LineSegment(start, end));
            }

            return segments;
        }
    }
}