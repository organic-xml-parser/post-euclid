using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PostEuclid.poincare.tiling;


public static class PolygonCompleter
{

    /**
     * Given two incident edges, fill the rest of the polygon.
     */
    public static List<Edge> GenerateRemainingEdges(Disk disk, int nSides, Edge e0, Edge e1)
    {
        if (nSides != 4)
        {
            throw new InvalidOperationException();
        }

        var commonPoint = e0.CommonPoint(e1);
        
        e0 = e0.GetOrientedAwayFrom(commonPoint);
        e1 = e1.GetOrientedAwayFrom(commonPoint);

        var remainingPoint = Util.CreateMirroredPoint(disk, commonPoint, e0.P1, e1.P1);

        return new List<Edge>()
        {
            new(e0.P1, remainingPoint),
            new(remainingPoint, e1.P1)
        };
    }
}

/**
 * Given a graph and a set of new edges, "stitches" the edges together to create a new set of polygons.
 */
public class EdgeStitcher
{
    public List<Polygon> StitchNewEdges(Disk disk, int nSides, Graph graph, List<Edge> newEdges)
    {
        var result = new List<Polygon>();
        
        for (int i = 0; i < newEdges.Count; i++)
        {
            var e0 = newEdges[i];
            var e1 = newEdges[(i + 1) % newEdges.Count];

            List<Edge> remainingEdges = new();
            if (e0.HasCommonPoint(e1))
            {
                GD.Print("Stitching corner edge");
                remainingEdges.Add(e1);
                remainingEdges.Add(e0);
                remainingEdges.AddRange(PolygonCompleter.GenerateRemainingEdges(disk, nSides, e0, e1));
            }
            else
            {
                GD.Print("Stitching square edge");
                try
                {
                    var connectedEdge = new List<Edge>()
                    {
                        new(e0.P0, e1.P0),
                        new(e0.P0, e1.P1),
                        new(e0.P1, e1.P0),
                        new(e0.P1, e1.P1),
                    }.Single(e => graph.ContainsEdge(e));
                    
                    
                    // try combinations of edges
                    remainingEdges.Add(e0);
                    remainingEdges.Add(new Edge(
                        e0.GetOrientedAwayFrom(connectedEdge.P0).P1,
                        e1.GetOrientedAwayFrom(connectedEdge.P1).P1
                    ));
                    remainingEdges.Add(e1);
                    remainingEdges.Add(new Edge(
                        e0.GetOrientedAwayFrom(connectedEdge.P0).P0,
                        e1.GetOrientedAwayFrom(connectedEdge.P1).P0
                    ));
                }
                catch (Exception e)
                {
                    continue;
                }
                
                
                
            }

            foreach (var edge in remainingEdges)
            {
                if (!graph.ContainsEdge(edge))
                {
                    GD.Print("Adding edge");
                    graph.AddEdge(edge);
                }
            }
            
            result.Add(new Polygon(remainingEdges.ToArray()));
        }
        
        return result;
    }
}