using System;
using System.Collections.Generic;
using System.Linq;

namespace PostEuclid.poincare;

public static class Util
{
    
    public static string create_mirrored_point(Disk disk, string p, String p0, String p1)
    {
        var originalTrsf = disk.PoincareTransform;
        
        var p0E = disk.GetPoint(p0);

        disk.PoincareTranslate(-p0E.X, -p0E.Y);

        var p0ENew = disk.GetPoint(p0);
        if (p0ENew.Length() != 0)
        {
            throw new InvalidOperationException("Could not align p0 to x-axis");
        }

        var p1E = disk.GetPoint(p1);

        double angle = Math.Atan2(p1E.Y, p1E.X);
        disk.PoincareRotate(-(float)angle);

        p0E = disk.GetPoint(p0);
        if (p0E.Length() != 0)
        {
            throw new InvalidOperationException();
        }

        p1E = disk.GetPoint(p1);
        if (p1E.Y > 10e-5)
        {
            throw new InvalidOperationException();
        }

        var pe = disk.GetPoint(p);
        disk.PoincareTranslate(-pe.X, pe.Y);

        var result = disk.AddPoint();
        
        disk.PoincareTransform = originalTrsf;

        return result;
    }
}

public class EdgeTransform
{
    public enum Type
    {
        Rotation,
        Mirror
    }

    private readonly Type _type;

    public EdgeTransform(Type type)
    {
        _type = type;
    }

    public Polygon Generate(PolygonEdge polygonEdge, Disk disk)
    {
        if (_type == Type.Mirror)
        {
            return generate_mirror(polygonEdge, disk);
        }

        throw new InvalidOperationException();
    }

    private Polygon generate_mirror(PolygonEdge polygonEdge, Disk disk)
    {
        var p0 = polygonEdge.P0;
        var p1 = polygonEdge.P1;
        var points = (p0, p1);
        var polygonEdges = polygonEdge.Polygon.edges.ToList();
        var offsetIndex = polygonEdges.IndexOf(polygonEdge);

        if (offsetIndex == -1)
        {
            throw new InvalidOperationException();
        }
        
        var edges = new List<PolygonEdge>();
        int count = 0;

        for (int i = 0; i < polygonEdges.Count; i++)
        {
            var edge = polygonEdges[(offsetIndex + i) % polygonEdges.Count];

            if ((edge.P0 == points.p0 || edge.P0 == points.p1) && 
                (edge.P1 == points.p0 || edge.P1 == points.p1))
            {
                edges.Add(new PolygonEdge(
                    edge.P0, 
                    edge.P1, 
                    true, 
                    edge.Transforms));
            }
            else
            {
                var mP0 = count == 0 ? polygonEdge.P0 : edges.Last().P1;

                var mP1 = count == polygonEdges.Count - 1 ? 
                    polygonEdge.P0 : 
                    Util.create_mirrored_point(disk, edge.P1, p0, p1);
                
                edges.Add(new PolygonEdge(
                    mP0,
                    mP1,
                    false,
                    edge.Transforms.ToList()));
            }

            count++;
        }

        return new Polygon(edges);
    }
}

public class PolygonEdge
{
    private Polygon _polygon;
    
    private List<EdgeTransform> _transforms;

    public string P0 { get; }

    public string P1 { get; }

    public List<EdgeTransform> Transforms => _transforms.ToList();

    public bool IsRedundant { get; }

    public PolygonEdge(string p0, string p1, bool isRedundant, List<EdgeTransform> transforms)
    {
        P0 = p0;
        P1 = p1;

        if (p0 == p1)
        {
            throw new InvalidOperationException();
        }
        
        IsRedundant = isRedundant;
        _transforms = transforms;
    }

    public Polygon Polygon
    {
        get
        {
            if (_polygon == null)
            {
                throw new InvalidOperationException();
            }
            return _polygon;
        }

        set
        {
            if (_polygon != null)
            {
                throw new InvalidOperationException();
            }
            
            _polygon = value;
        }
    }
    
    public bool IsConnectedTo(PolygonEdge other)
    {
        return P1.Equals(other.P0);
    }
}

public class Polygon
{
    public List<PolygonEdge> edges;

    public Polygon(List<PolygonEdge> edges)
    {
        this.edges = new List<PolygonEdge>(edges);

        foreach (var e in edges)
        {
            e.Polygon = this;
        }

        for (int i = 0; i < edges.Count; i++)
        {
            var e0 = edges[i];
            var e1 = edges[(i + 1) % edges.Count];

            if (!e0.IsConnectedTo(e1))
            {
                throw new InvalidOperationException("Edges are not contiguous.");
            }
        }
    }
}

public class EdgeGenerator
{
    private HashSet<Tuple<String, String>> edges = new();
    
    private Disk disk;
    
    public EdgeGenerator(Disk disk)
    {
        this.disk = disk;
    }

    public void CreateEdge(String p0, String p1)
    {
        var key = new Tuple<string, string>(p0, p1);
        var keyInv = new Tuple<string, string>(p1, p0);

        if (edges.Contains(key) || edges.Contains(keyInv))
        {
            return;
        }
        
        disk.AddEdge(p0, p1);
        edges.Add(key);
    }
}


class SpanningTreeNode
{
    private SpanningTreeNode _parent = null;
    private List<SpanningTreeNode> childNodes = new();

    public SpanningTreeNode parent
    {
        set => _parent = value;
    }
    
    public SpanningTreeNode(PolygonEdge polygonEdge)
    {
        this.PolygonEdge = polygonEdge;
    }

    public PolygonEdge PolygonEdge { get; }

    public IEnumerable<SpanningTreeNode> Walk()
    {
        yield return this;
        
        foreach (var n in childNodes)
        {
            foreach (var nn in n.Walk())
            {
                yield return nn;
            }
        }
    }

    public void Generate(Disk disk, int depth)
    {
        if (depth == 0 || PolygonEdge.IsRedundant)
        {
            return;
        }

        if (childNodes.Count != 0)
        {
            return;
        }
        
        foreach (var t in PolygonEdge.Transforms)
        {
            var childPolygon = t.Generate(PolygonEdge, disk);
            foreach (var edge in childPolygon.edges)
            {
                var node = new SpanningTreeNode(edge);
                node.parent = this;
                node.Generate(disk, depth - 1);
                childNodes.Add(node);
            }
        }

    }
}


public static class Tiling
{

    public static class Tiling_3_7
    {
        public static void Generate(Disk disk, int depth)
        {
            if (depth <= 0)
            {
                throw new ArgumentException("Depth must be > 0");
            }
            
            var n = 4;
            var k = 6;

            var a = Math.PI / n;
            var b = Math.PI / k;
            var c = Math.PI / 2;

            var sinA = Math.Sin(a);
            var sinB = Math.Sin(b);

            var radius = Math.Sin(c - b - a) /
                         Math.Sqrt(1 - sinB * sinB - sinA * sinA);

            var points = new List<String>();

            for (var i = 0; i < n; i++)
            {
                var angle = Math.PI * 2 * i / n;
                var trsf = disk.PoincareTransform;

                disk.PoincareRotate((float)angle);
                disk.PoincareTranslate(0.0f, (float)radius);
                points.Add(disk.AddPoint());

                disk.PoincareTransform = trsf;
            }

            var rootShape = new Polygon(
                new List<PolygonEdge>()
                {
                    new(
                        points[0],
                        points[1],
                        false,
                        new List<EdgeTransform>
                        {
                            new(EdgeTransform.Type.Mirror)
                        }),
                    new(
                        points[1],
                        points[2],
                        false,
                        new List<EdgeTransform>
                        {
                            new(EdgeTransform.Type.Mirror)
                        }),
                    new(
                        points[2],
                        points[3],
                        false,
                        new List<EdgeTransform>
                        {
                            new(EdgeTransform.Type.Mirror)
                        }),
                    new(
                        points[3],
                        points[0],
                        false,
                        new List<EdgeTransform>
                        {
                            new(EdgeTransform.Type.Mirror)
                        })
                });

            var edgeGenerator = new EdgeGenerator(disk);
            var tree = new List<SpanningTreeNode>();
            foreach (var e in rootShape.edges)
            {
                var treeRoot = new SpanningTreeNode(e);
                tree.Add(treeRoot);
            }

            foreach (var t in tree)
            {
                t.Generate(disk, depth);

                var subnodes = t.Walk().ToList();
                foreach (var subNode in subnodes)
                {
                    edgeGenerator.CreateEdge(subNode.PolygonEdge.P0, subNode.PolygonEdge.P1);
                }
            }
        }
    }
}