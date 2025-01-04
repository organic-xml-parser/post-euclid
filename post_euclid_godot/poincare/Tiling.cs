using Godot;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.NetworkInformation;

public class Util
{
    
    public static String create_mirrored_point(Disk disk, String p, String p0, String p1)
    {
        var original_trsf = disk.PoincareTransform;
        
        var p0e = disk.GetPoint(p0);

        disk.PoincareTranslate(-p0e.X, -p0e.Y);

        var p0e_new = disk.GetPoint(p0);
        if (p0e_new.Length() != 0)
        {
            throw new InvalidOperationException("Could not align p0 to x-axis");
        }

        var p1e = disk.GetPoint(p1);

        double angle = Math.Atan2(p1e.Y, p1e.X);
        disk.PoincareRotate(-(float)angle);

        p0e = disk.GetPoint(p0);
        if (p0e.Length() != 0)
        {
            throw new InvalidOperationException();
        }

        p1e = disk.GetPoint(p1);
        if (p1e.Y > 10e-5)
        {
            throw new InvalidOperationException();
        }

        var pe = disk.GetPoint(p);
        disk.PoincareTranslate(-pe.X, pe.Y);

        var result = disk.AddPoint();
        
        disk.PoincareTransform = original_trsf;

        return result;
    }
}

public class EdgeTransform
{
    public enum Type
    {
        ROTATION,
        MIRROR
    }

    private Type type;

    public EdgeTransform(Type type)
    {
        this.type = type;
    }

    public Polygon generate(PolygonEdge polygonEdge, Disk disk)
    {
        if (this.type == Type.MIRROR)
        {
            return generate_mirror(polygonEdge, disk);
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    private Polygon generate_mirror(PolygonEdge polygonEdge, Disk disk)
    {
        var p0 = polygonEdge.p0;
        var p1 = polygonEdge.p1;
        var points = (p0, p1);
        var polygon_edges = polygonEdge.polygon.edges.ToList();
        var offset_index = polygon_edges.IndexOf(polygonEdge);

        if (offset_index == -1)
        {
            throw new InvalidOperationException();
        }
        
        var edges = new List<PolygonEdge>();
        int count = 0;

        for (int i = 0; i < polygon_edges.Count; i++)
        {
            var edge = polygon_edges[(offset_index + i) % polygon_edges.Count];

            if ((edge.p0 == points.p0 || edge.p0 == points.p1) && 
                (edge.p1 == points.p0 || edge.p1 == points.p1))
            {
                edges.Add(new PolygonEdge(
                    edge.p0, 
                    edge.p1, 
                    true, 
                    edge.transforms));
            }
            else
            {
                String m_p0;
                if (count == 0)
                {
                    m_p0 = polygonEdge.p0;
                }
                else
                {
                    m_p0 = edges.Last().p1;
                }

                String m_p1;
                if (count == polygon_edges.Count - 1)
                {
                    m_p1 = polygonEdge.p0;
                }
                else
                {
                    m_p1 = Util.create_mirrored_point(disk, edge.p1, p0, p1);
                }
                
                edges.Add(new PolygonEdge(
                    m_p0,
                    m_p1,
                    false,
                    edge.transforms.ToList()));
            }

            count++;
        }

        return new Polygon(edges);
    }
}

public class PolygonEdge
{
    private String _p0;
    private String _p1;
    private bool _isRedundant;
    private Polygon _polygon = null;
    private List<EdgeTransform> _transforms;

    public String p0
    {
        get { return _p0; }
    }
    
    public String p1
    {
        get { return _p1; }
    }

    public List<EdgeTransform> transforms
    {
        get { return _transforms.ToList(); }
    }

    public bool isRedundant
    {
        get { return _isRedundant; }
    }

    public PolygonEdge(string p0, string p1, bool isRedundant, List<EdgeTransform> transforms)
    {
        _p0 = p0;
        _p1 = p1;

        if (p0 == p1)
        {
            throw new InvalidOperationException();
        }
        
        _isRedundant = isRedundant;
        _transforms = transforms;
    }

    public Polygon polygon
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
    
    public bool isConnectedTo(PolygonEdge other)
    {
        return _p1.Equals(other._p0);
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
            e.polygon = this;
        }

        for (int i = 0; i < edges.Count; i++)
        {
            var e0 = edges[i];
            var e1 = edges[(i + 1) % edges.Count];

            if (!e0.isConnectedTo(e1))
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

    public void createEdge(String p0, String p1)
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
    private PolygonEdge _polygonEdge;

    public SpanningTreeNode parent
    {
        set { _parent = value; }
    }
    
    public SpanningTreeNode(PolygonEdge polygonEdge)
    {
        _polygonEdge = polygonEdge;
    }

    public PolygonEdge polygonEdge
    {
        get
        {
            return _polygonEdge;
        }
    }

    public IEnumerable<SpanningTreeNode> walk()
    {
        yield return this;
        
        foreach (var n in childNodes)
        {
            foreach (var nn in n.walk())
            {
                yield return nn;
            }
        }
    }

    public void generate(Disk disk, int depth)
    {
        if (depth == 0 || _polygonEdge.isRedundant)
        {
            return;
        }
        
        if (childNodes.Count == 0)
        {
            foreach (var t in _polygonEdge.transforms)
            {
                var childPolygon = t.generate(_polygonEdge, disk);
                foreach (var edge in childPolygon.edges)
                {
                    var node = new SpanningTreeNode(edge);
                    node.parent = this;
                    node.generate(disk, depth - 1);
                    childNodes.Add(node);
                }
            }
        }

    }
}


public class Tiling
{

    public class Tiling_3_7
    {
        public static void generate(Disk disk, int depth)
        {
            if (depth < 0)
            {
                throw new ArgumentException();
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

            for (int i = 0; i < n; i++)
            {
                var angle = Math.PI * 2 * i / n;
                var trsf = disk.PoincareTransform;

                disk.PoincareRotate((float)angle);
                disk.PoincareTranslate(0.0f, (float)radius);
                points.Add(disk.AddPoint());

                disk.PoincareTransform = trsf;
            }

            var root_shape = new Polygon(
                new List<PolygonEdge>()
                {
                    new(
                        points[0],
                        points[1],
                        false,
                        new List<EdgeTransform>
                        {
                            new(EdgeTransform.Type.MIRROR)
                        }),
                    new(
                        points[1],
                        points[2],
                        false,
                        new List<EdgeTransform>
                        {
                            new(EdgeTransform.Type.MIRROR)
                        }),
                    new(
                        points[2],
                        points[3],
                        false,
                        new List<EdgeTransform>
                        {
                            new(EdgeTransform.Type.MIRROR)
                        }),
                    new(
                        points[3],
                        points[0],
                        false,
                        new List<EdgeTransform>
                        {
                            new(EdgeTransform.Type.MIRROR)
                        })
                });

            var edgeGenerator = new EdgeGenerator(disk);
            var tree = new List<SpanningTreeNode>();
            foreach (var e in root_shape.edges)
            {
                var tree_root = new SpanningTreeNode(e);
                tree.Add(tree_root);
            }

            foreach (var t in tree)
            {
                t.generate(disk, depth);

                var subnodes = t.walk().ToList();
                foreach (var subNode in subnodes)
                {
                    edgeGenerator.createEdge(subNode.polygonEdge.p0, subNode.polygonEdge.p1);
                }
            }
        }
    }
}