using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PostEuclid.poincare.tiling_2;


public class Edge : Tuple<string, string>
{

    public string P0 => Item1;
    public string P1 => Item2;

    public string Label = "";

    public Edge(string item1, string item2) : base(item1, item2)
    {
        if (item1 == item2)
        {
            throw new ArgumentException("Edge points must be distinct.");
        }
    }

    public Edge Reversed()
    {
        return new Edge(P1, P0);
    }

    public Edge GetOrientedAwayFrom(string point)
    {
        if (point == P0)
        {
            return this;
        }
        else if (point == P1)
        {
            return new Edge(P1, P0);
        }
        else
        {
            throw new ArgumentException("Edge does not include the specified point");
        }
    }

    public bool ConnectsTo(Edge other)
    {
        return HasCommonPoint(other);
    }

    public bool HasCommonPoint(Edge other)
    {
        return P0 == other.P0 || P0 == other.P1 || P1 == other.P0 || P1 == other.P1;
    }

    public string CommonPoint(Edge other)
    {
        if (P0 == other.P0 || P0 == other.P1)
        {
            return P0;
        }
        
        if (P1 == other.P0 || P1 == other.P1)
        {
            return P1;
        }

        throw new ArgumentException("Edges do not have a common point.");
    }
}

public class Polygon
{
    private List<Edge> _edges;

    public List<Edge> Edges => _edges.ToList();

    public Polygon(params Edge[] edges)
    {
        if (edges.Length < 3)
        {
            throw new ArgumentException("Polygon must contain at least 3 edges.");
        }

        for (int i = 0; i < edges.Length; i++)
        {
            if (!edges[i].ConnectsTo(edges[(i + 1) % edges.Length]))
            {
                throw new ArgumentException("Edges are not contiguous.");
            }
        }
            
        _edges = edges.ToList();
    }
}

public class Graph
{
    private HashSet<Edge> _edges = new();
    private Dictionary<string, HashSet<Edge>> _pointsToEdges = new();
    private HashSet<string> _frontierPoints = new();
    private HashSet<string> _visitedPoints = new();

    public Graph(Polygon startPolygon)
    {
        foreach (var startEdge in startPolygon.Edges)
        {
            AddEdge(startEdge);
        }
    }

    public IEnumerable<string> FrontierPoints => _frontierPoints.ToList();

    public List<Edge> Edges => _edges.ToList();

    public bool ContainsPoint(string point)
    {
        return _pointsToEdges.ContainsKey(point);
    }
    
    public void MarkFrontierPoint(string point)
    {
        if (_frontierPoints.Contains(point))
        {
            throw new ArgumentException("Point is already marked as frontier");
        }
        
        if (_visitedPoints.Contains(point))
        {
            throw new ArgumentException("Point has already been visited");
        }

        if (!_pointsToEdges.ContainsKey(point))
        {
            throw new ArgumentException("AddEdge should be called before marking points");
        }
        
        _frontierPoints.Add(point);
    }
    
    public void MarkVisitedPoint(string point)
    {
        if (!_frontierPoints.Contains(point))
        {
            throw new ArgumentException("Point has not been marked as frontier");
        }
        
        _frontierPoints.Remove(point);
        _visitedPoints.Add(point);
    }

    public bool ContainsEdge(Edge edge)
    {
        return _edges.Contains(edge) || _edges.Contains(edge.Reversed());
    }

    public void AddEdge(Edge edge)
    {
        if (ContainsEdge(edge))
        {
            throw new ArgumentException("Graph already contains an equivalent edge.");
        }

        _edges.Add(edge);
        
        if (!_pointsToEdges.ContainsKey(edge.P0))
        {
            _pointsToEdges.Add(edge.P0, new HashSet<Edge>());
            _frontierPoints.Add(edge.P0);
        }
        
        if (!_pointsToEdges.ContainsKey(edge.P1))
        {
            _pointsToEdges.Add(edge.P1, new HashSet<Edge>());
            _frontierPoints.Add(edge.P1);
        }

        _pointsToEdges[edge.P0].Add(edge);
        _pointsToEdges[edge.P1].Add(edge);
    }

    public List<Edge> EdgesIncidentTo(string s)
    {
        return _pointsToEdges[s].ToList();
    }
}