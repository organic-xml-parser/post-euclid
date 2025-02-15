using System;
using System.Collections.Generic;
using System.Linq;

namespace PostEuclid.poincare.tiling;

public class Polygon
{
    public List<PolygonEdge> Edges;

    public readonly int LayerIndex;
    public readonly int IndexInLayer;
    
    private readonly List<IEdgeTransform> _edgeTransforms;
    private readonly List<IVertexTransform> _vertexTransforms;
    
    public List<IEdgeTransform> EdgeTransforms => _edgeTransforms.ToList();
    public List<IVertexTransform> VertexTransforms => _vertexTransforms.ToList();

    public List<string> Points
    {
        get
        {
            var result = new List<string>();

            foreach (var edge in Edges)
            {
                result.Add(edge.P0);
            }
            
            return result;
        }
    }
    
    public Polygon(List<PolygonEdge> edges, 
        int layerIndex,
        int indexInLayer,
        List<IEdgeTransform> edgeTransforms, 
        List<IVertexTransform> vertexTransforms)
    {
        if (layerIndex < 0 || indexInLayer < 0)
        {
            throw new ArgumentException();
        }
        
        IndexInLayer = indexInLayer;
        
        _edgeTransforms = edgeTransforms.ToList();
        _vertexTransforms = vertexTransforms.ToList();
        
        LayerIndex = layerIndex;
        Edges = new List<PolygonEdge>(edges);

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

    public bool ContainsVertex(string vertex)
    {
        return Edges.Any(p => p.P0 == vertex);
    }

    public List<PolygonEdge> EdgesRelativeTo(PolygonEdge originEdge)
    {
        List<PolygonEdge> result = new();
        
        var offsetIndex = Edges.IndexOf(originEdge);

        if (offsetIndex == -1)
        {
            throw new InvalidOperationException();
        }
        
        for (var i = 0; i < Edges.Count; i++)
        {
            result.Add(Edges[(offsetIndex + i) % Edges.Count]);
        }

        return result;
    }
    
    public List<PolygonEdge> EdgesRelativeTo(string originVertex)
    {
        if (!ContainsVertex(originVertex))
        {
            throw new InvalidOperationException("Polygon does not contain specified vertex.");
        }
        
        List<PolygonEdge> result = new();
        
        var offsetIndex = Edges.FindIndex(p => p.P0 == originVertex);
        for (var i = 0; i < Edges.Count; i++)
        {
            result.Add(Edges[(offsetIndex + i) % Edges.Count]);
        }

        return result;
    }
}