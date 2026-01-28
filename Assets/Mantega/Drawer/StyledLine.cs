using UnityEngine;
using Mantega.Geometry;

namespace Mantega.Drawer
{
    /// <summary>
    /// Represents a straight line defined by its endpoints, thickness, and color.
    /// </summary>
    /// <remarks>The <see cref="StyledLine"/> structure is immutable and provides a way to define a line
    /// segment in 2D space. It includes the start and end points of the line, the line's thickness, and its
    /// color.</remarks>
    [System.Serializable]
    public readonly struct StyledLine
    {
        /// <summary>
        /// The geometry that define the line.
        /// </summary>
        /// <remarks>This field is read-only and provides the coordinates or data points that
        /// describe the line. It can be used to access the line's geometry or for calculations involving the
        /// line.</remarks>
        public readonly LineSegment Segment;

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
        /// Initializes a new instance of <see cref="StyledLine"/>.
        /// </summary>
        /// <param name="linePoints">The points that define the start and end of the line.</param>
        /// <param name="thickness">The thickness of the line. Must be greater than zero.</param>
        /// <param name="color">The color of the line.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if <paramref name="thickness"/> is less than or equal to zero.</exception>
        public StyledLine(LineSegment linePoints, int thickness, Color color)
        {
            if (thickness <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(thickness), thickness, $"{nameof(thickness)} must be greater than zero.");
            }

            this.Segment = linePoints;
            Thickness = thickness;
            Color = color;
        }

        /// <inheritdoc cref="StyledLine(LineSegment, int, Color)"/>
        /// <param name="start">The starting point of the line.</param>
        /// <param name="end">The ending point of the line.</param>
        public StyledLine(Vector2 start, Vector2 end, int thickness, Color color) : this(new LineSegment(start, end), thickness, color)
        {

        }
    }
}
