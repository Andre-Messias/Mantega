
namespace Mantega.Geometry
{
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
}