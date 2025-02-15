using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using PostEuclid.poincare.tiling;

namespace PostEuclid.poincare;


public class Batcher<T>
{
    private Dictionary<T, int> _elementsToBatchNumber = new();
    private List<List<T>> _batches = new();

    private int _batchSize;
    
    public IEnumerable<IEnumerable<T>> Batches => _batches.AsEnumerable();

    public Batcher(int batchSize)
    {
        _batchSize = batchSize;
    }
    
    public void add(T element)
    {
        if (_elementsToBatchNumber.ContainsKey(element))
        {
            throw new ArgumentException();
        }

        if (_batches.Count == 0 || _batches.Last().Count == _batchSize)
        {
            _batches.Add(new List<T>());
        }

        _batches.Last().Add(element);
    }
}

public partial class Disk : Sprite2D
{
    private const int BATCH_SIZE = 16;
    
    public struct RenderedEdge
    {
        public string P0;
        public string P1;
        public string Label = "";

        public RenderedEdge(string p1, string p0, string label)
        {
            P1 = p1;
            P0 = p0;
            Label = label;
        }
    }
    
    private MobiusTransform trsf = MobiusTransform.identity();
    
    /**
     * point identifiers to their coordinates
     */
    private Dictionary<String, Vector2> points = new();
    
    /**
     * point identifiers to their text labels, if any
     */
    private Dictionary<String, String> pointLabels = new();

    /**
     * Point identifiers to their corresponding label sprites.
     */
    private Dictionary<String, Node2D> pointLabelNodes = new();

    private Batcher<RenderedEdge> edgeBatcher = new(BATCH_SIZE);
    
    private Dictionary<RenderedEdge, Node2D> edges = new();

    private HashSet<Node> added_nodes = new();

    private PackedScene edge_scene = ResourceLoader.Load<PackedScene>("res://poincare/poincare_edge.tscn");

    private static double _translationStep = 0.8f;
    private static double _rotationStep = 1.5f;

    private double _timeOffset;
    
    public MobiusTransform PoincareTransform
    {
        get { return new MobiusTransform(trsf); }
        set
        {
            trsf = new MobiusTransform(value);
        }
    }
    
    public void PoincareTranslate(double x, double y)
    {
        trsf = MobiusTransform.Multiply(
            MobiusTransform.translation(x, y),
            trsf);
    }

    public void TranslateToPoint(string point)
    {
        var xy = GetPointPosition(point);
        PoincareTranslate(-xy.X, -xy.Y);
    }

    public double GetAngleToPoint(string point)
    {
        var xy = GetPointPosition(point);

        return Math.Atan2(xy.Y, xy.X);
    }
    
    public void PoincareRotate(double angle)
    {
        trsf = MobiusTransform.Multiply(
            MobiusTransform.rotation(angle),
            trsf);
    }
    
    public String AddPoint()
    {
        var id = Guid.NewGuid().ToString();

        points[id] = trsf.Inverse().transform_point(new Vector2(0, 0));
        pointLabels[id] = "";

        return id;
    }

    public void SetPointLabel(string pointId, string label)
    {
        pointLabels[pointId] = label;
    }

    public Vector2 GetPointPosition(String id)
    {
        return trsf.transform_point(points[id]);
    }

    public void AddEdge(RenderedEdge edge)
    {
        edges[edge] = null;
        edgeBatcher.add(edge);
    }
    
    public override void _Ready()
    {
        var tiling = new Tiling();
        tiling.Generate(this);
        RecomputeEdges();

        GD.Print("Disk: Generated ", edges.Count, "Edges and ", points.Count, " Points");
    }

    private void RecomputeEdges()
    {
        
        foreach (var n in added_nodes)
        {
            RemoveChild(n);
        }
        added_nodes.Clear();

        foreach (var pointId in points.Keys)
        {
            /*
            var node = new Node2D();
            node.ZIndex = 10;
            var labelNode = new Label();
            labelNode.Text = pointLabels[pointId];
            node.AddChild(labelNode);
            AddChild(node);
            pointLabelNodes[pointId] = node;
                        */

        }

        int edgeIndex = 0;

        foreach (var edgeBatch in edgeBatcher.Batches)
        {
            var edgeBatchList = edgeBatch.ToList();
            
            var node = edge_scene.Instantiate<Node2D>();
            node.GetChild<Node2D>(0).GetChild<Label>(0).QueueFree();
            
            node.Material = (Material)node.Material.Duplicate();

            var local_p0 = new Vector2[BATCH_SIZE];
            var local_p1 = new Vector2[BATCH_SIZE];
            var baseColor = new Color[BATCH_SIZE];
            var lineThickness = new float[BATCH_SIZE];
            var edgeIndices = new int[BATCH_SIZE];
            
            ((ShaderMaterial)node.Material).SetShaderParameter("elements_in_batch", edgeBatchList.Count);

            for (int i = 0; i < edgeBatchList.Count; i++)
            {
                local_p0[i] = trsf.transform_point(points[edgeBatchList[i].P0]);
                local_p1[i] = trsf.transform_point(points[edgeBatchList[i].P1]);
                baseColor[i] = Color.FromHsv((float)edgeIndex / edges.Count, 0.8f, 0.7f);
                lineThickness[i] = 0.04f;
                edgeIndices[i] = edgeIndex;
                
                edges[edgeBatchList[i]] = node;

                edgeIndex++;
            }
            
            //var color = Color.FromHsv((float)1 / edges.Count, 0.8f, 0.7f);

            //((ShaderMaterial)node.Material).SetShaderParameter("p0", points[edge.P0]);
            //((ShaderMaterial)node.Material).SetShaderParameter("p1", points[edge.P1]);
            ((ShaderMaterial)node.Material).SetShaderParameter("local_p0", local_p0);
            ((ShaderMaterial)node.Material).SetShaderParameter("local_p1", local_p1);
            ((ShaderMaterial)node.Material).SetShaderParameter("base_color", baseColor);
            ((ShaderMaterial)node.Material).SetShaderParameter("line_thickness", lineThickness);
            ((ShaderMaterial)node.Material).SetShaderParameter("edgeIndices", edgeIndices);
            ((ShaderMaterial)node.Material).SetShaderParameter("timeOffset", _timeOffset);
            AddChild(node);
            added_nodes.Add(node);
            

        }
        
        /*
        var edgeIndex = 0;
        foreach (var edge in edges.Keys)
        {

            var node = edge_scene.Instantiate<Node2D>();
            var label = node.GetChild<Node2D>(0).GetChild<Label>(0);
            
            label.SetText(edge.Label);
            label.Position = new Vector2(0.0f, 0.0f);
            label.QueueFree();
            
            node.Material = (Material)node.Material.Duplicate();

            var color = Color.FromHsv((float)edgeIndex / edges.Count, 0.8f, 0.7f);

            //((ShaderMaterial)node.Material).SetShaderParameter("p0", points[edge.P0]);
            //((ShaderMaterial)node.Material).SetShaderParameter("p1", points[edge.P1]);
            ((ShaderMaterial)node.Material).SetShaderParameter("local_p0", trsf.transform_point(points[edge.P0]));
            ((ShaderMaterial)node.Material).SetShaderParameter("local_p1", trsf.transform_point(points[edge.P1]));
            ((ShaderMaterial)node.Material).SetShaderParameter("base_color", color);
            ((ShaderMaterial)node.Material).SetShaderParameter("line_thickness", 0.04);
            AddChild(node);
            added_nodes.Add(node);
            edges[edge] = node;
            edgeIndex++;
    
        }
        */
    }
    
    private void PoincareCircleArcProperties(Vector2 p0, Vector2 p1, out Vector2 origin, out float radius) {

        // https://math.stackexchange.com/questions/1503466/algebraic-solutions-for-poincar%C3%A9-disk-arcs

        var dx = p1.X - p0.X;
        var dy = p1.Y - p0.Y;

        var px = p0.X;
        var py = p0.Y;

        var qx = p1.X;
        var qy = p1.Y;

        var px2 = px * px;
        var py2 = py * py;

        var qx2 = qx * qx;
        var qy2 = qy * qy;

        float u = (px2 + py2 + 1.0f);
        float v = (qx2 + qy2 + 1.0f);

        float denom = 2.0f * (px * qy - py * qx);

        float ox;
        float oy;

        if (dx == 0.0f) {
            oy = 0.0f;
        } else {
            oy = (-qx * u + px * v) / denom;
        }

        if (dy == 0.0f) {
            ox = 0.0f;
        } else {
            ox = (qy * u - py * v) / denom;
        }

        origin = new Vector2(ox, oy);
        radius = (p0 - origin).Length();
    }

    private bool IsStraightLineSegment(Vector2 p0, Vector2 p1)
    {
        // straight line if O -> p0 and O -> p1 are perpendicular
        return p0.Cross(p1) != 0;
    }

    public override void _Process(double delta)
    {
        _timeOffset += delta;
        
        double currentTranslationStep = delta * _translationStep;
        double currentRotationStep = delta * _rotationStep;

        double dx = 0;
        double dy = 0;

        if (Input.IsActionPressed("ui_left"))
        {
            dx += currentRotationStep;
        }
        
        if (Input.IsActionPressed("ui_right"))
        {
            dx -= currentRotationStep;
        }
        
        if (Input.IsActionPressed("ui_up"))
        {
            dy += currentTranslationStep;
        }
        
        if (Input.IsActionPressed("ui_down"))
        {
            dy -= currentTranslationStep;
        }

        PoincareRotate(dx);
        PoincareTranslate(0f, dy);

        //foreach (var pointId in points.Keys)
        //{
            /*
            var p = trsf.transform_point(points[pointId]);
            var labelNode = pointLabelNodes[pointId];
            
            var textScale = 1.0f - p.Length();
            labelNode.Scale = new Vector2(textScale, textScale);
            labelNode.Position = (p * Texture.GetWidth() * 0.5f) - 0.5f * textScale * labelNode.GetChild<Label>(0).Size;
            */
        //}
        
        /*
        foreach (var keyNode in edges)
        {
            var p0 = trsf.transform_point(points[keyNode.Key.P0]);
            var p1 = trsf.transform_point(points[keyNode.Key.P1]);
            
            var node = keyNode.Value;
            //var labelNode = node.GetChild<Node2D>(0);
            //var textPosition = p0 + 0.5f * (p1 - p0);
            
            //var textScale = 1.0f - textPosition.Length();
            //labelNode.Scale = new Vector2(textScale, textScale);
            //labelNode.Position = (textPosition * Texture.GetWidth() * 0.5f) - 0.5f * textScale * labelNode.GetChild<Label>(0).Size;

            /*
            ((ShaderMaterial)node.Material).SetShaderParameter("mb_a", 
                new Vector2((float)trsf.a.Real, (float)trsf.a.Imaginary));
            ((ShaderMaterial)node.Material).SetShaderParameter("mb_b", 
                new Vector2((float)trsf.b.Real, (float)trsf.b.Imaginary));
            ((ShaderMaterial)node.Material).SetShaderParameter("mb_c", 
                new Vector2((float)trsf.c.Real, (float)trsf.c.Imaginary));
            ((ShaderMaterial)node.Material).SetShaderParameter("mb_d", 
                new Vector2((float)trsf.d.Real, (float)trsf.d.Imaginary));
            *
            
            //((ShaderMaterial)node.Material).SetShaderParameter("p0", points[keyNode.Key.P0]);
            //((ShaderMaterial)node.Material).SetShaderParameter("p1", points[keyNode.Key.P1]);

            ((ShaderMaterial)node.Material).SetShaderParameter("local_p0", p0);
            ((ShaderMaterial)node.Material).SetShaderParameter("local_p1", p1);

        }
        */
    
        
        foreach (var edgeBatch in edgeBatcher.Batches)
        {
            var edgeBatchList = edgeBatch.ToList();

            var node = edges[edgeBatchList.First()];
            
            var local_p0 = new Vector2[BATCH_SIZE];
            var local_p1 = new Vector2[BATCH_SIZE];
            
            for (int i = 0; i < edgeBatchList.Count; i++)
            {
                local_p0[i] = trsf.transform_point(points[edgeBatchList[i].P0]);
                local_p1[i] = trsf.transform_point(points[edgeBatchList[i].P1]);
            }
            
            ((ShaderMaterial)node.Material).SetShaderParameter("local_p0", local_p0);
            ((ShaderMaterial)node.Material).SetShaderParameter("local_p1", local_p1);
            ((ShaderMaterial)node.Material).SetShaderParameter("timeOffset", _timeOffset);
            

        }
    }
}
