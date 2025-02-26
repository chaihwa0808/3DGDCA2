using UnityEngine;
using System.Collections.Generic;

public class TrailGuide : MonoBehaviour
{
    public LineRenderer lineRenderer;  // Reference to the LineRenderer
    public Transform[] waypoints;      // The waypoints to create the path
    public int smoothness = 10;        // Higher value = smoother curves

    void Start()
    {
        if (lineRenderer == null || waypoints.Length < 2)
        {
            Debug.LogError("LineRenderer or waypoints not set properly!");
            return;
        }

        List<Vector3> smoothPoints = GenerateSmoothPath(waypoints, smoothness);
        lineRenderer.positionCount = smoothPoints.Count;
        lineRenderer.SetPositions(smoothPoints.ToArray());
    }

    List<Vector3> GenerateSmoothPath(Transform[] points, int subdivisions)
    {
        List<Vector3> smoothPath = new List<Vector3>();

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 p0 = (i == 0) ? points[i].position : points[i - 1].position;
            Vector3 p1 = points[i].position;
            Vector3 p2 = points[i + 1].position;
            Vector3 p3 = (i == points.Length - 2) ? points[i + 1].position : points[i + 2].position;

            for (int j = 0; j < subdivisions; j++)
            {
                float t = j / (float)subdivisions;
                smoothPath.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        // Ensure the last waypoint is added
        smoothPath.Add(points[points.Length - 1].position);

        return smoothPath;
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2 * p1) +
            (-p0 + p2) * t +
            (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 +
            (-p0 + 3 * p1 - 3 * p2 + p3) * t3
        );
    }
}
