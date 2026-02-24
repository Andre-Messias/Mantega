namespace Mantega.AI
{
    using System.Collections.Generic;
    using UnityEngine;

    using Mantega.Geometry;
    using Mantega.Core.Diagnostics;

    /// <summary>
    /// Provides functionality for the $P Point-Cloud Recognizer algorithm.
    /// </summary>
    /// <remarks>The <see cref="PDollar"/> class implements the $P Point-Cloud Recognizer algorithm.
    /// It includes methods for normalizing point clouds, calculating similarity scores, and converting various input formats into the required point representation.</remarks>
    public static class PDollar
    {
        #region PDollarPoint Struct
        /// <summary>
        /// Represents a point in 2D space with an associated stroke identifier.
        /// </summary>
        /// <remarks>This structure is used to represent points for the $P Point-Cloud Recognizer algorithm.</remarks>
        [System.Serializable]
        public struct PDollarPoint
        {
            /// <summary>
            /// Represents the coordinates of the point.
            /// </summary>
            public Vector2 Point;

            /// <summary>
            /// Represents the identifier of the stroke to which this point belongs.
            /// </summary>
            public int StrokeID;

            /// <summary>
            /// Initializes a new instance of the <see cref="PDollarPoint"/> class.
            /// </summary>
            /// <param name="point">The coordinates of the point.</param>
            /// <param name="strokeId">The identifier of the stroke to which this point belongs.</param>
            public PDollarPoint(Vector2 point, int strokeId)
            {
                this.Point = point;
                this.StrokeID = strokeId;
            }

            /// <inheritdoc cref="PDollarPoint(Vector2, int)"/>
            /// <param name="x">Represents the x coordinate of the point.</param>
            /// <param name="y">Represents the y coordinate of the point.</param>
            public PDollarPoint(float x, float y, int strokeId) : this(new Vector2(x, y), strokeId)
            {

            }

            /// <summary>
            /// Gets the X coordinate of the point.
            /// </summary>
            public readonly float X => Point.x;

            /// <summary>
            /// Gets the Y coordinate of the point.
            /// </summary>
            public readonly float Y => Point.y;
        }
        #endregion

        #region Constants
        /// <summary>
        /// Represents the number of points used in the normalization process.
        /// </summary>
        private const int NUM_POINTS = 32;

        /// <summary>
        /// Represents the precision factor used in the greedy cloud-matching algorithm.
        /// </summary>
        private const float EPSILON = 0.50f;
        
        /// <summary>
        /// Represents the origin point used for normalization and translation of points.
        /// </summary>
        private static readonly PDollarPoint ORIGIN = new(0, 0, 0);
        #endregion

        #region Similarity Score
        /// <summary>
        /// Calculates the similarity score between a set of raw points and a template of predefined points.
        /// </summary>
        /// <remarks>This method normalizes the points before calculating
        /// the similarity. The similarity score is computed using a greedy cloud-matching algorithm and is adjusted to
        /// produce a final distance-based score.</remarks>
        /// <param name="points">A list of <see cref="PDollarPoint"/> representing the raw input points to be compared.</param>
        /// <param name="normalizedTemplate">A list of <see cref="PDollarPoint"/> representing the template points to compare against. It must be pre-normalized.</param>
        /// <returns>A <see cref="float"/> value representing the similarity score, where higher values indicate greater
        /// similarity.</returns>
        public static float GetSimilarity(List<PDollarPoint> points, List<PDollarPoint> normalizedTemplate)
        {
            ValidatePointsList(normalizedTemplate);
            ValidatePointsList(points);

            // Normalize
            List<PDollarPoint> candidate = Normalize(points);

            // Calculate distance
            float dist = GreedyCloudMatch(candidate, normalizedTemplate);

            return CalculateDistanceScore(dist);
        }

        /// <inheritdoc cref="GetSimilarity(List{PDollarPoint}, List{PDollarPoint})"/>
        /// <param name="rawPoints">A list of raw points to be compared.</param>
        public static float GetSimilarity(List<Vector2> rawPoints, List<PDollarPoint> normalizedTemplate)
        {
            return GetSimilarity(ToPoints(rawPoints), normalizedTemplate);
        }

        /// <inheritdoc cref="GetSimilarity(List{Vector2}, List{PDollarPoint})"/>
        public static float GetSimilarity(List<LineRenderer> rawPoints, List<PDollarPoint> normalizedTemplate)
        {
            return GetSimilarity(ToPoints(rawPoints), normalizedTemplate);
        }

        /// <inheritdoc cref="GetSimilarity(List{Vector2}, List{PDollarPoint})"/>
        public static float GetSimilarity(List<List<LineSegment>> rawPoints, List<PDollarPoint> normalizedTemplate)
        {
            return GetSimilarity(ToPoints(rawPoints), normalizedTemplate);
        }

        /// <summary>
        /// Calculates a similarity score based on the given distance, where smaller distances yield higher scores.
        /// </summary>
        /// <param name="distance">The distance value to evaluate. Must be a non-negative number.</param>
        /// <returns>A score representing the evaluated distance. The score is a non-negative value, with a maximum of 1.0 for
        /// distances less than or equal to 2.0, and 0.0 for distances greater than or equal to 4.0.</returns>
        public static float CalculateDistanceScore(float distance)
        {
            Validations.ValidateNotNegative(distance);
            return Mathf.Max((distance - 2.0f) / -2.0f, 0.0f);
        }
        #endregion

        #region Cloud Match Algorithm

        /// <summary>
        /// Computes the minimum distance between two point clouds using a greedy matching algorithm.
        /// </summary>
        /// <remarks>This method uses a greedy approach to calculate the distance between two point clouds
        /// by iterating through the points with a step size determined by the size of the first point cloud. The
        /// distance is computed in both directions (from <paramref name="points"/> to <paramref name="template"/> and
        /// vice versa), and the minimum distance is returned.</remarks>
        /// <param name="points">The list of points representing the first point cloud.</param>
        /// <param name="template">The list of points representing the second point cloud.</param>
        /// <returns>The minimum distance between the two point clouds as a <see cref="float"/> value.</returns>
        private static float GreedyCloudMatch(List<PDollarPoint> points, List<PDollarPoint> template)
        {
            int step = Mathf.FloorToInt(Mathf.Pow(points.Count, 1.0f - EPSILON));
            float min = float.MaxValue;

            for (int i = 0; i < points.Count; i += step)
            {
                float d1 = CloudDistance(points, template, i);
                float d2 = CloudDistance(template, points, i);
                min = Mathf.Min(min, Mathf.Min(d1, d2));
            }
            return min;
        }

        /// <summary>
        /// Calculates the weighted distance between two point clouds, starting from a specified index.
        /// </summary>
        /// <remarks>The method computes the distance by iterating through the points in <paramref
        /// name="pts1"/> starting  at the specified index and finding the closest unmatched point in <paramref
        /// name="pts2"/>. The distance is weighted based on the position of the point in the iteration, with weights
        /// decreasing from 1 to 0.</remarks>
        /// <param name="pts1">The list of points representing the first point cloud.</param>
        /// <param name="pts2">The list of points representing the second point cloud.</param>
        /// <param name="start">The starting index in <paramref name="pts1"/> to begin the distance calculation.</param>
        /// <returns>The weighted sum of distances between the points in the two clouds.</returns>
        private static float CloudDistance(List<PDollarPoint> pts1, List<PDollarPoint> pts2, int start)
        {
            bool[] matched = new bool[pts1.Count];
            float sum = 0;
            int i = start;

            do
            {
                int index = -1;
                float min = float.MaxValue;
                for (int j = 0; j < matched.Length; j++)
                {
                    if (!matched[j])
                    {
                        float d = SqrEuclideanDistance(pts1[i], pts2[j]);
                        if (d < min)
                        {
                            min = d;
                            index = j;
                        }
                    }
                }
                matched[index] = true;

                // Weight decreases from 1 to 0
                float weight = 1.0f - ((i - start + pts1.Count) % pts1.Count) / (float)pts1.Count;
                sum += weight * Mathf.Sqrt(min); 

                i = (i + 1) % pts1.Count;
            } while (i != start);

            return sum;
        }
        #endregion

        #region Normalization

        /// <summary>
        /// Normalizes a list of points by resampling, scaling, and translating them to a common origin.
        /// </summary>
        /// <remarks>This method performs the following steps to normalize the points: <list
        /// type="number"> <item>Resamples the points to the specified number of points.</item> <item>Scales the points
        /// to fit within a unit bounding box.</item> <item>Translates the points so that their centroid aligns
        /// with the origin.</item> </list></remarks>
        /// <param name="points">The list of points to normalize. Cannot be <see cref="null"/>.</param>
        /// <param name="numPoints">The number of points to resample to. Defaults to <see langword="NUM_POINTS"/>.</param>
        /// <returns>A new list of normalized points.</returns>
        public static List<PDollarPoint> Normalize(List<PDollarPoint> points, int numPoints = NUM_POINTS)
        {
            ValidatePointsList(points);
            Validations.ValidateGreaterThan(numPoints, 1);

            points = Resample(points, numPoints);
            points = Scale(points);
            points = TranslateTo(points, ORIGIN);
            return points;
        }

        /// <summary>
        /// Resamples a list of points to a specified number of evenly spaced points while preserving the original
        /// stroke structure.
        /// </summary>
        /// <remarks>This method ensures that the resampled points are distributed evenly along the path
        /// defined by the input points. Points within the same stroke are resampled together, preserving the stroke
        /// structure.The first and last points of the original path are always included in the resampled list.</remarks>
        /// <param name="points">The list of <see cref="PDollarPoint"/> to be resampled. Points must
        /// be ordered by their stroke sequence.</param>
        /// <param name="numPoints">The desired number of points in the resampled list. Must be greater than 1.</param>
        /// <returns>A new list of <see cref="PDollarPoint"/> objects containing <paramref name="numPoints"/> evenly spaced
        /// points along the original path.</returns>
        private static List<PDollarPoint> Resample(List<PDollarPoint> points, int numPoints)
        {
            float SegmentLength = PathLength(points) / (numPoints - 1);
            float DistanceAccumulator = 0;
            List<PDollarPoint> newPoints = new(){ points[0] };

            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].StrokeID == points[i - 1].StrokeID)
                {
                    float d = EuclideanDistance(points[i - 1], points[i]);
                    if ((DistanceAccumulator + d) >= SegmentLength)
                    {
                        float qx = points[i - 1].X + ((SegmentLength - DistanceAccumulator) / d) * (points[i].X - points[i - 1].X);
                        float qy = points[i - 1].Y + ((SegmentLength - DistanceAccumulator) / d) * (points[i].Y - points[i - 1].Y);
                        PDollarPoint q = new(qx, qy, points[i].StrokeID);
                        newPoints.Add(q);
                        points.Insert(i, q);
                        DistanceAccumulator = 0;
                    }
                    else DistanceAccumulator += d;
                }
            }

            // If fall a rounding-error short of adding the last point
            if (newPoints.Count == numPoints - 1)
            {
                newPoints.Add(new PDollarPoint(points[points.Count - 1].X, points[points.Count - 1].Y, points[points.Count - 1].StrokeID));
            }

            return newPoints;
        }

        /// <summary>
        /// Scales a list of points to fit within a unit square.
        /// </summary>
        /// <remarks>The scaling operation ensures that the points maintain their relative positions 
        /// while fitting within a unit square. The <see cref="PDollarPoint.StrokeID"/> of each point is preserved. If all points have the same X
        /// and Y coordinates, the method avoids division by zero by scaling the points to a default size of 1.</remarks>
        /// <param name="points">The list of <see cref="PDollarPoint"/> objects to scale. The list must contain at least one point.</param>
        /// <returns>A new list of <see cref="PDollarPoint"/> objects, where the X and Y coordinates are normalized to the range
        /// [0, 1].</returns>
        private static List<PDollarPoint> Scale(List<PDollarPoint> points)
        {
            PDollarPoint firstPoint = points[0];
            float minX = firstPoint.X;
            float maxX = firstPoint.X;
            float minY = firstPoint.Y;
            float maxY = firstPoint.Y;

            foreach (PDollarPoint p in points)
            {
                minX = Mathf.Min(minX, p.X); maxX = Mathf.Max(maxX, p.X);
                minY = Mathf.Min(minY, p.Y); maxY = Mathf.Max(maxY, p.Y);
            }

            float size = Mathf.Max(maxX - minX, maxY - minY);
            // Prevent division by zero
            if (Mathf.Approximately(size, 0f))
            {
                size = 1.0f;
            }

            List<PDollarPoint> newPoints = new();
            foreach (PDollarPoint p in points)
            {
                newPoints.Add(new PDollarPoint((p.X - minX) / size, (p.Y - minY) / size, p.StrokeID));
            }

            return newPoints;
        }

        /// <summary>
        /// Translates a collection of points to a new position based on the specified target point.
        /// </summary>
        /// <remarks>The translation is performed by calculating the centroid of the input points and
        /// adjusting each point's position so that the centroid aligns with the specified target point.</remarks>
        /// <param name="points">The collection of points to be translated.</param>
        /// <param name="to">The target point to which the collection of points will be translated.</param>
        /// <returns>A new list of <see cref="PDollarPoint"/> objects representing the translated points.</returns>
        private static List<PDollarPoint> TranslateTo(List<PDollarPoint> points, PDollarPoint to)
        {
            PDollarPoint centroid = CalculateCentroid(points);
            List<PDollarPoint> newPoints = new();
            foreach (PDollarPoint p in points)
            {
                newPoints.Add(new PDollarPoint(p.X + to.X - centroid.X, p.Y + to.Y - centroid.Y, p.StrokeID));
            }

            return newPoints;
        }
        #endregion

        #region Math Helpers

        /// <summary>
        /// Calculates the centroid of a given set of points.
        /// </summary>
        /// <param name="points">A list of <see cref="PDollarPoint"/> to calculate the centroid for. The list
        /// must not be <see cref="null"/> or empty.</param>
        /// <returns>A <see cref="PDollarPoint"/> representing the centroid of the input points.</returns>
        private static PDollarPoint CalculateCentroid(List<PDollarPoint> points)
        {
            float x = 0, y = 0;
            foreach (PDollarPoint p in points) 
            { 
                x += p.X; 
                y += p.Y; 
            }

            return new PDollarPoint(x / points.Count, y / points.Count, 0);
        }

        /// <summary>
        /// Calculates the total length of a path represented by a list of points.
        /// </summary>
        /// <remarks>The method calculates the length by summing the Euclidean distances between
        /// consecutive points that belong to the same stroke. Points with different <see
        /// cref="PDollarPoint.StrokeID"/> values are treated as belonging to separate strokes and do not contribute to
        /// the total length.</remarks>
        /// <param name="points">A list of <see cref="PDollarPoint"/> objects representing the path.</param>
        /// <returns>The total length of the path as a floating-point value. Only consecutive points within the same stroke
        /// contribute to the length.</returns>
        private static float PathLength(List<PDollarPoint> points)
        {
            float d = 0;
            for (int i = 1; i < points.Count; i++)
            {
                if (points[i].StrokeID == points[i - 1].StrokeID)
                {
                    d += EuclideanDistance(points[i - 1], points[i]);
                }
            }

            return d;
        }

        /// <summary>
        /// Calculates the squared Euclidean distance between two points.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        /// <returns>The Euclidean distance between <paramref name="p1"/> and <paramref name="p2"/> as a floating-point value.</returns>
        private static float SqrEuclideanDistance(PDollarPoint p1, PDollarPoint p2)
        {
            float dx = p2.X - p1.X;
            float dy = p2.Y - p1.Y;
            return (dx * dx) + (dy * dy);
        }

        /// <inheritdoc cref="SqrEuclideanDistance(PDollarPoint, PDollarPoint)"/>
        /// <summary>
        /// Calculates the Euclidean distance between two points.
        /// </summary>
        private static float EuclideanDistance(PDollarPoint p1, PDollarPoint p2)
        {
            return Mathf.Sqrt(SqrEuclideanDistance(p1, p2));
        }
        #endregion

        #region Validations

        /// <summary>
        /// Validates the specified list of points to ensure it meets the required conditions.
        /// </summary>
        /// <param name="points">The list of <see cref="PDollarPoint"/> objects to validate.</param>
        private static void ValidatePointsList(List<PDollarPoint> points)
        {
            Validations.ValidateNotNull(points);
            Validations.ValidateGreaterThan(points.Count, 1);
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Converts a list of Unity <see cref="Vector2"/> points to a list of <see cref="PDollarPoint"/> objects.
        /// </summary>
        /// <param name="unityPoints">The list of <see cref="Vector2"/> points to convert. Cannot be <see cref="null"/> or empty.</param>
        /// <returns>A list of <see cref="PDollarPoint"/> objects, where each point corresponds to a <see cref="Vector2"/> in the
        /// input list.</returns>
        public static List<PDollarPoint> ToPoints(List<Vector2> unityPoints)
        {
            Validations.ValidateNotNullOrEmpty(unityPoints);

            List<PDollarPoint> list = new();
            for (int i = 0; i < unityPoints.Count; i++)
            {
                list.Add(new PDollarPoint(unityPoints[i].x, unityPoints[i].y, 0));
            }

            return list;
        }

        /// <summary>
        /// Converts a collection of <see cref="LineRenderer"/> objects into a list of <see cref="PDollarPoint"/> instances.
        /// </summary>
        /// <remarks>This method processes each <see cref="LineRenderer"/> in the input list, extracting
        /// its positions and converting them into <see cref="PDollarPoint"/> instances. If a <see cref="Camera"/> is
        /// provided, the positions are transformed to screen space using the camera; otherwise, the positions remain in
        /// world space. <para> If an error occurs while processing a specific <see cref="LineRenderer"/>, it is
        /// skipped, and a warning is logged. </para></remarks>
        /// <param name="renderers">A list of <see cref="LineRenderer"/> objects to convert. Cannot be <see cref="null"/> or empty.</param>
        /// <param name="camera">An optional <see cref="Camera"/> used to transform the positions of the points from world space to screen
        /// space. If null, the positions are used as-is in world space.</param>
        /// <returns>A list of <see cref="PDollarPoint"/> instances representing the points from the provided <see
        /// cref="LineRenderer"/> objects. Each point is assigned a unique <see cref="PDollarPoint.StrokeID"/> corresponding to its originating
        /// <see cref="LineRenderer"/>.</returns>
        public static List<PDollarPoint> ToPoints(List<LineRenderer> renderers, Camera camera = null)
        {
            Validations.ValidateNotNullOrEmpty(renderers);

            // Initial capacity estimation
            List<PDollarPoint> cloud = new(renderers.Count * 50);

            int strokeID = 0;

            foreach (var lineRenderer in renderers)
            {
                try
                {
                    Validations.ValidateNotNullOrEmpty(lineRenderer);

                    Vector3[] positions = new Vector3[lineRenderer.positionCount];
                    lineRenderer.GetPositions(positions);

                    for (int i = 0; i < positions.Length; i++)
                    {
                        Vector2 pos;
                        if (camera != null)
                        {
                            pos = camera.WorldToScreenPoint(positions[i]);
                        }
                        else
                        {
                            pos = positions[i];
                        }

                        cloud.Add(new PDollarPoint(pos, strokeID));
                    }

                    strokeID++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Skipping LineRenderer due to error: {ex.Message}");
                }
            }

            return cloud;
        }

        /// <summary>
        /// Converts a collection of <see cref="List{List{LineSegment}"/> objects into a list of <see cref="PDollarPoint"/> instances.
        /// </summary>
        /// <remarks>This method processes each stroke in the input collection and converts its line
        /// segments into points. Each line segment contributes two points: one for its start and one for its end. The
        /// resulting points are assigned a <see cref="PDollarPoint.StrokeID"/> based on their originating stroke's index in the input list.
        /// <para> If a stroke in the input list is null or empty, it is skipped, and a warning is logged. The method
        /// ensures that the output list contains only valid points from valid strokes.</para></remarks>
        /// <param name="strokes">A list of strokes, where each stroke is a list of <see cref="LineSegment"/> objects. Each stroke represents
        /// a sequence of connected line segments.</param>
        /// <returns>A list of <see cref="PDollarPoint"/> objects representing the points in the input strokes. Each point is
        /// associated with a <see cref="PDollarPoint.StrokeID"/> corresponding to its originating stroke.</returns>
        public static List<PDollarPoint> ToPoints(List<List<LineSegment>> strokes)
        {
            Validations.ValidateNotNull(strokes);

            // Initial capacity estimation
            List<PDollarPoint> cloud = new(strokes.Count * 20);

            for (int strokeIndex = 0; strokeIndex < strokes.Count; strokeIndex++)
            {
                List<LineSegment> currentStroke = strokes[strokeIndex];

                try
                {
                    Validations.ValidateNotNullOrEmpty(currentStroke);
                    int strokeID = strokeIndex;

                    for (int i = 0; i < currentStroke.Count; i++)
                    {
                        LineSegment seg = currentStroke[i];

                        cloud.Add(new PDollarPoint(seg.Start, strokeID));
                        cloud.Add(new PDollarPoint(seg.End, strokeID));
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Skipping stroke at index {strokeIndex} due to error: {ex.Message}");
                }
            }

            return cloud;
        }
        #endregion
    }
}