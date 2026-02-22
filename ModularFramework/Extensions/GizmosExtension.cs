using System.Collections.Generic;
using UnityEngine;

namespace ModularFramework
{
    public static class GizmosExtension
    {
        public static void DrawCircle(Vector3 center, float radius, int segments = 20)
        {
            List<Vector3> points = new List<Vector3>();
            var normal = Vector3.up; // Assuming a flat circle on the XZ plane
            for (int i = 0; i <= segments; i++)
            {
                float currentAngle = (float)i / segments * 360;
                Quaternion rotation = Quaternion.AngleAxis(currentAngle, normal);
                Vector3 currentPoint = center + rotation * Vector3.forward * radius;
                points.Add(currentPoint);
            }
            DrawPolyline(points);
        }

        public static void DrawPolygons(List<List<Vector3>> pointsCollection)
        {
            foreach (var points in pointsCollection)
            {
                DrawPolyline(points, true);
            }
        }
        
        public static void DrawPolyline(List<Vector3> points, bool isClosed = false)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Gizmos.DrawLine(points[i], points[i + 1]);
            }

            if (isClosed)
            {
                Gizmos.DrawLine(points[^1], points[0]);
            }
        }
    }
}