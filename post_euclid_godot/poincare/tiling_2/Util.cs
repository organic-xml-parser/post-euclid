
using System;
using Godot;

namespace PostEuclid.poincare.tiling_2;

public static class Util
{
    public static double NormalizeAngle(double angle)
    {
        if (angle < 0)
        {
            return 2.0 * Math.PI + angle;
        }

        return angle;
    }

    public static bool Clockwise(params Vector2[] vectors)
    {
        double sum = 0;
        for (int i = 0; i < vectors.Length; i++)
        {
            var ip = (i + 1) % vectors.Length;
            sum += (vectors[ip].X - vectors[i].X) * (vectors[ip].Y + vectors[i].Y);
        }

        return sum > 0;
    }
    
    public static string CreateRotatedPoint(Disk disk, string point, string origin, float angle)
    {
        var trsf = disk.PoincareTransform;

        // center on origin
        var originXy = disk.GetPointPosition(origin);
        disk.PoincareTranslate(-originXy.X, -originXy.Y);

        var pointXy = disk.GetPointPosition(point);
        var rotatedPointXy = new Vector2(
            (float)(Math.Cos(angle) * pointXy.X - Math.Sin(angle) * pointXy.Y),
            (float)(Math.Sin(angle) * pointXy.X + Math.Cos(angle) * pointXy.Y)
        );
        
        // center on the desired coordinates
        disk.PoincareTranslate(rotatedPointXy.X, rotatedPointXy.Y);

        // create the point
        var result = disk.AddPoint();
        
        // undo the transform changes
        disk.PoincareTransform = trsf;

        return result;
    }
    
    /**
     * Create mirrored version of point p about line segment defined by p0 -> p1
     */
    public static string CreateMirroredPoint(Disk disk, string p, string p0, string p1)
    {
        var originalTrsf = disk.PoincareTransform;
        
        var p0E = disk.GetPointPosition(p0);

        disk.PoincareTranslate(-p0E.X, -p0E.Y);

        var p0ENew = disk.GetPointPosition(p0);
        if (p0ENew.Length() > 10E-8)
        {
            throw new InvalidOperationException("Could not align p0 to x-axis");
        }

        var p1E = disk.GetPointPosition(p1);

        double angle = Math.Atan2(p1E.Y, p1E.X);
        disk.PoincareRotate(-(float)angle);

        p0E = disk.GetPointPosition(p0);
        if (p0E.Length() > 10E-8)
        {
            throw new InvalidOperationException();
        }

        p1E = disk.GetPointPosition(p1);
        if (p1E.Y > 10e-5)
        {
            throw new InvalidOperationException();
        }

        var pe = disk.GetPointPosition(p);
        disk.PoincareTranslate(-pe.X, pe.Y);

        var result = disk.AddPoint();
        
        disk.PoincareTransform = originalTrsf;

        return result;
    }

    public static string CreateAveragePoint(Disk disk, params string[] points)
    {
        var x = 0.0f;
        var y = 0.0f;

        foreach (var point in points)
        {
            var pos = disk.GetPointPosition(point);

            x += pos.X;
            y += pos.Y;
        }

        x /= points.Length;
        y /= points.Length;

        var trsf = disk.PoincareTransform;
        
        disk.PoincareTranslate(x, y);

        var result = disk.AddPoint();

        disk.PoincareTransform = trsf;

        return result;
    }
}

