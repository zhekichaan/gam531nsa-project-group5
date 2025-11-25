using OpenTK.Mathematics;

namespace FinalProject.Common;

public class BoundingBox
{
    public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public BoundingBox(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        // Check if this bounding box intersects with another
        public bool Intersects(BoundingBox other)
        {
            return (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
                   (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
                   (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);
        }

        // Check if a point is inside this bounding box
        public bool Contains(Vector3 point)
        {
            return point.X >= Min.X && point.X <= Max.X &&
                   point.Y >= Min.Y && point.Y <= Max.Y &&
                   point.Z >= Min.Z && point.Z <= Max.Z;
        }

        // Create a bounding box from center, size and scale
        public static BoundingBox FromCenterAndSize(Vector3 center, Vector3 size, Vector3 scale)
        {
            Vector3 halfSize = size * scale * 0.5f;
            return new BoundingBox(center - halfSize, center + halfSize);
        }

        // Transform bounding box by position, scale, and rotation
        public static BoundingBox Transform(Vector3 originalMin, Vector3 originalMax, Vector3 position, Vector3 scale, float rotation)
        {
            // Scale the original bounds
            Vector3 scaledMin = originalMin * scale;
            Vector3 scaledMax = originalMax * scale;

            // For rotation around Y-axis, we need to consider all corners
            Vector3[] corners = new Vector3[8];
            corners[0] = new Vector3(scaledMin.X, scaledMin.Y, scaledMin.Z);
            corners[1] = new Vector3(scaledMax.X, scaledMin.Y, scaledMin.Z);
            corners[2] = new Vector3(scaledMin.X, scaledMax.Y, scaledMin.Z);
            corners[3] = new Vector3(scaledMax.X, scaledMax.Y, scaledMin.Z);
            corners[4] = new Vector3(scaledMin.X, scaledMin.Y, scaledMax.Z);
            corners[5] = new Vector3(scaledMax.X, scaledMin.Y, scaledMax.Z);
            corners[6] = new Vector3(scaledMin.X, scaledMax.Y, scaledMax.Z);
            corners[7] = new Vector3(scaledMax.X, scaledMax.Y, scaledMax.Z);

            // Rotate corners around Y-axis
            for (int i = 0; i < 8; i++)
            {
                float x = corners[i].X;
                float z = corners[i].Z;
                corners[i].X = x * MathF.Cos(rotation) - z * MathF.Sin(rotation);
                corners[i].Z = x * MathF.Sin(rotation) + z * MathF.Cos(rotation);
            }

            // Find new min/max after rotation
            Vector3 min = corners[0];
            Vector3 max = corners[0];

            for (int i = 1; i < 8; i++)
            {
                min = Vector3.ComponentMin(min, corners[i]);
                max = Vector3.ComponentMax(max, corners[i]);
            }

            // Apply position offset
            return new BoundingBox(min + position, max + position);
        }
}