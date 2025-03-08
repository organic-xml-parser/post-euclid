using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using Godot;

namespace PostEuclid.poincare.tiling;


/**
 * Major source for understanding hyperbolic tiling:
 *      GENERATING HYPERBOLIC PATTERNS FOR REGULAR AND NON-REGULAR P-GONS, Ajit V. Datar
 * General idea (my approach differs a bit from the paper):
 * 1) start with core polygon points.
 * 2) mark all "exposed" points (as they are referred to in the Datar paper), or "frontier" as I call them
 * (loosely borrowing the term from https://github.com/Geoplexity/Frontier).
 * 3) perform generation on those points. Incident edges are used to calculate the resulting new edges
 * 4) perform a stitch pass, to generate new polygons by joining together adjacent edges.
 * 5) repeat to generate new outer layers.
 */
public class Tiling
{
    private int CompareFrontierPoints(Disk disk, string p0, string p1)
    {
        return disk.GetAngleToPoint(p1).CompareTo(disk.GetAngleToPoint(p0));
    }
    
    public void Generate(Disk disk)
    {
        
        var nSides = 4;
        var nAdjacent = 5;
        
        // ===================================================================
        //                      Generate New Edges
        // ===================================================================
        var gen = new EdgeGenerator();
        var stitch = new EdgeStitcher();
        var graph = new Graph(GenerateStartPolygon(disk, nSides, nAdjacent));

        for (int layerIndex = 0; layerIndex < 5; layerIndex++)
        {
            var newEdges = new List<Edge>();
            int pointIndex = 0;
            var fp = graph.FrontierPoints.ToList();
            fp.Sort((p0, p1) => CompareFrontierPoints(disk, p0, p1));
            foreach (var p in fp)
            {
                GD.PrintRaw("Visiting frontier point... ", pointIndex);
                disk.SetPointLabel(p, pointIndex.ToString());
                newEdges.AddRange(gen.GenerateNewEdges(
                    disk, 
                    graph, 
                    p, 
                    graph.EdgesIncidentTo(p), 
                    nSides, 
                    nAdjacent));
                
                
                GD.PrintRaw("    Generated ", newEdges.Count, " new edges.");
                GD.Print();

                pointIndex++;
            }
            
            var newPolygons = stitch.StitchNewEdges(disk, nSides, graph, newEdges);
            GD.PrintRaw("Generated ", newPolygons.Count, " new Polygons.");
            GD.Print();

            for (int i = 0; i < newEdges.Count; i++)
            {
                newEdges[i].Label = i.ToString();
            }

            foreach (var f in fp)
            {
                graph.MarkVisitedPoint(f);
            }
        }


        // ===================================================================
        //                      Create the rendered edges
        // ===================================================================
        foreach (var edge in graph.Edges)
        {
            disk.AddEdge(new Disk.RenderedEdge(edge.P0, edge.P1, edge.Label));
        }
    }
    
    
    private Polygon GenerateStartPolygon(Disk disk, int nSides, int nAdjacent)
    {
        // ===================================================================
        //                      Generate starting shape
        // ===================================================================
        var startPoints = GenerateCenterPolygonPoints(disk, nSides, nAdjacent);
        var edges = new List<Edge>();
        for (int i = 0; i < startPoints.Count; i++)
        {
            var p0 = startPoints[i];
            var p1 = startPoints[(i + 1) % startPoints.Count];
            edges.Add(new Edge(p0, p1));
            
        }

        return new Polygon(edges.ToArray());
    }
    
    private static List<string> GenerateCenterPolygonPoints(Disk disk, int nSides, int nAdjacent)
    {
        var initialTransform = disk.PoincareTransform;
            
        var n = nSides;
        var k = nAdjacent;

        var a = Math.PI / n;
        var b = Math.PI / k;
        var c = Math.PI / 2;

        var sinA = Math.Sin(a);
        var sinB = Math.Sin(b);

        var radius = Math.Sin(c - b - a) /
                     Math.Sqrt(1 - sinB * sinB - sinA * sinA);

        var result = new List<String>();

        for (var i = 0; i < n; i++)
        {
            var angle = Math.PI * 2 * i / n;
            var trsf = disk.PoincareTransform;

            disk.PoincareRotate((float)angle);
            disk.PoincareTranslate(0.0f, (float)radius);
            result.Add(disk.AddPoint());

            disk.PoincareTransform = trsf;
        }

        disk.PoincareTransform = initialTransform;

        return result;
    }
}