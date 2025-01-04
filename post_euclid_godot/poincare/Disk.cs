using Godot;
using System;
using Complex = System.Numerics.Complex;
using System.Collections.Generic;
using System.Linq;

namespace PostEuclid.poincare;


public class MobiusTransform {
        
    public Complex a;
    public Complex b;
    public Complex c;
    public Complex d;
    
    public static MobiusTransform identity() {
        return new MobiusTransform(1, 0,
                            0, 1);
    }
    
    public static MobiusTransform translation(float dx, float dy) {
        Complex v = new Complex(dx, dy);
        
        return new MobiusTransform(1,           v,
                                   Complex.Conjugate(v), 1);
    }
    
    public static MobiusTransform rotation(float angle) {
        return new MobiusTransform(
            new Complex(Math.Cos(angle), Math.Sin(angle)),   0,
            0,                                           1);

    }

    public MobiusTransform(MobiusTransform other)
    {
        this.a = other.a;
        this.b = other.b;
        this.c = other.c;
        this.d = other.d;
    }
    
    public MobiusTransform(Complex a, Complex b, Complex c, Complex d) {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
    
    public static MobiusTransform multiply(MobiusTransform left, MobiusTransform right) {
        Complex a = left.a;
        Complex b = left.b;
        Complex c = left.c;
        Complex d = left.d;
        Complex e = right.a;
        Complex f = right.b;
        Complex g = right.c;
        Complex h = right.d;
        
        return new MobiusTransform(
            a * e + b * g,          a * f + b * h,
            c * e + d * g,          c * f + d * h
        );
    }

    public Vector2 transform_point(Vector2 point)
    {
        var z = new Complex(point.X, point.Y);
        var p_new = (a * z + b) / (c * z + d);

        return new Vector2((float)p_new.Real, (float)p_new.Imaginary);
    }

    public MobiusTransform inverse()
    {
        var determinant = a * d - b * c;

        if (determinant == 0)
        {
            throw new InvalidOperationException("Not Invertible");
        }

        var inverse = 1.0 / determinant;

        return new MobiusTransform(
            d * inverse, -b * inverse,
            -c * inverse, a * inverse);
    }
}


public partial class Disk : Sprite2D
{
    private MobiusTransform trsf = MobiusTransform.identity();
    
    // point identifiers to their coordinates
    private Dictionary<String, Vector2> points = new();
    
    private Dictionary<Tuple<String, String>, Node2D> edges = new();

    private HashSet<Node> added_nodes = new();

    private PackedScene edge_scene = ResourceLoader.Load<PackedScene>("res://poincare/poincare_edge.tscn");

    private static double step = 0.5f;

    public MobiusTransform PoincareTransform
    {
        get { return new MobiusTransform(trsf); }
        set
        {
            trsf = new MobiusTransform(value);
        }
    }
    
    public void PoincareTranslate(float x, float y)
    {
        trsf = MobiusTransform.multiply(
            MobiusTransform.translation(x, y),
            trsf);
    }
    
    public void PoincareRotate(float angle)
    {
        trsf = MobiusTransform.multiply(
            MobiusTransform.rotation(angle),
            trsf);
    }
    
    public String AddPoint()
    {
        var id = Guid.NewGuid().ToString();

        points[id] = trsf.inverse().transform_point(new Vector2(0, 0));

        return id;
    }

    public Vector2 GetPoint(String id)
    {
        return trsf.transform_point(points[id]);
    }

    public void AddEdge(String a, String b)
    {
        var e0 = new Tuple<String, String>(a, b);
        var e1 = new Tuple<String, String>(b, a);

        if (edges.Keys.Contains(e0) || edges.Keys.Contains(e1))
        {
            return;
        }

        edges.Add(e0, null);
    }
    
    public override void _Ready()
    {
        Tiling.Tiling_3_7.Generate(this, 2);
        recomputeEdges();

        GD.Print("Generated ", edges.Count, "Edges and ", points.Count, " Points");
    }

    private void recomputeEdges()
    {
        
        foreach (var n in added_nodes)
        {
            RemoveChild(n);
        }
        added_nodes.Clear();

        foreach (var edge in edges.Keys)
        {

            var node = edge_scene.Instantiate<Node2D>();

            var p0 = trsf.transform_point(points[edge.Item1]);
            var p1 = trsf.transform_point(points[edge.Item2]);

            node.Material = (Material)node.Material.Duplicate();

            ((ShaderMaterial)node.Material).SetShaderParameter("p0", p0);
            ((ShaderMaterial)node.Material).SetShaderParameter("p1", p1);

            AddChild(node);
            added_nodes.Add(node);
            edges[edge] = node;
        }
    }

    public override void _Process(double delta)
    {
        
        double current_step = delta * step;

        double dx = 0;
        double dy = 0;

        if (Input.IsActionPressed("ui_left"))
        {
            dx += current_step;
        }
        
        if (Input.IsActionPressed("ui_right"))
        {
            dx -= current_step;
        }
        
        if (Input.IsActionPressed("ui_up"))
        {
            dy += current_step;
        }
        
        if (Input.IsActionPressed("ui_down"))
        {
            dy -= current_step;
        }

        PoincareTranslate((float)dx, (float)dy);
        
        foreach (var node in edges.Values)
        {
            ((ShaderMaterial)node.Material).SetShaderParameter("mb_a", 
                new Vector2((float)trsf.a.Real, (float)trsf.a.Imaginary));
            ((ShaderMaterial)node.Material).SetShaderParameter("mb_b", 
                new Vector2((float)trsf.b.Real, (float)trsf.b.Imaginary));
            ((ShaderMaterial)node.Material).SetShaderParameter("mb_c", 
                new Vector2((float)trsf.c.Real, (float)trsf.c.Imaginary));
            ((ShaderMaterial)node.Material).SetShaderParameter("mb_d", 
                new Vector2((float)trsf.d.Real, (float)trsf.d.Imaginary));
        }
    }
}
