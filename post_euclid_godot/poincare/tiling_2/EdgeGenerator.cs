using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Microsoft.VisualBasic.CompilerServices;

namespace PostEuclid.poincare.tiling_2;

public static class Extensions
{
    // stolen from https://stackoverflow.com/questions/15150147/all-permutations-of-a-list
    public static IEnumerable<IEnumerable<T>> Permute<T>(this IEnumerable<T> sequence)
    {
        if (sequence == null)
        {
            yield break;
        }

        var list = sequence.ToList();

        if (!list.Any())
        {
            yield return Enumerable.Empty<T>();
        }
        else
        {
            var startingElementIndex = 0;

            foreach (var startingElement in list)
            {
                var index = startingElementIndex;
                var remainingItems = list.Where((e, i) => i != index);

                foreach (var permutationOfRemainder in remainingItems.Permute())
                {
                    yield return permutationOfRemainder.Prepend(startingElement);
                }

                startingElementIndex++;
            }
        }
    }
}

public class EdgeGenerator
{
    public List<Edge> GenerateNewEdgesFor2Adjacent(Disk disk, 
        Graph graph, 
        string point, 
        List<Edge> incidentEdges, 
        int nSides,
        int nAdjacent)
    {
        disk.TranslateToPoint(point);

        if (disk.GetPointPosition(point).Length() != 0)
        {
            throw new InvalidOperationException();
        }
        
        var p0 = incidentEdges[0].GetOrientedAwayFrom(point).P1;
        var p1 = incidentEdges[1].GetOrientedAwayFrom(point).P1;

        bool isCw = Util.Clockwise(disk.GetPointPosition(p0), disk.GetPointPosition(point), disk.GetPointPosition(p1));
        if (!isCw)
        {
            incidentEdges.Reverse();
        }
        p0 = incidentEdges[0].GetOrientedAwayFrom(point).P1;
        p1 = incidentEdges[1].GetOrientedAwayFrom(point).P1;

        isCw = Util.Clockwise(disk.GetPointPosition(p0), disk.GetPointPosition(point), disk.GetPointPosition(p1));
        GD.Print("IS CW: ", isCw);

        disk.PoincareRotate(-Util.NormalizeAngle(disk.GetAngleToPoint(incidentEdges[0].GetOrientedAwayFrom(point).P1)));
        
        var edgeAngles = incidentEdges
            .Select(e => e.GetOrientedAwayFrom(point))
            .Select(p => p.P1)
            .Select(disk.GetAngleToPoint)
            .ToList();

        if (edgeAngles[0] > 10E-7)
        {
            throw new InvalidOperationException();
        }
        
        GD.PrintRaw("Edge angles: ");
        foreach (var e in edgeAngles)
        {
            GD.PrintRaw(e, " ");
        }
        GD.Print();
        
        GD.Print("P0: ", disk.GetPointPosition(incidentEdges[0].GetOrientedAwayFrom(point).P1));
        GD.Print("P: ", disk.GetPointPosition(point));
        GD.Print("P1: ", disk.GetPointPosition(incidentEdges[1].GetOrientedAwayFrom(point).P1));

        var polygonAngle = 2.0 * Math.PI / nAdjacent;
        var separationSpacing = Math.Abs(disk.GetPointPosition(incidentEdges[0].GetOrientedAwayFrom(point).P1).X);
        
        List<Edge> result = new List<Edge>();
        List<string> points = new List<string>();
    
        disk.PoincareRotate(-4.0f * polygonAngle);

        for (int i = 0; i < 3; i++)
        {
            var ttrsf = disk.PoincareTransform;
            disk.PoincareRotate(polygonAngle * i);
            disk.PoincareTranslate(-separationSpacing, 0);
            points.Add(disk.AddPoint());
            result.Add(new Edge(point, points.Last()));
            disk.PoincareTransform = ttrsf;
        }

        return result;
    }
    
    public List<Edge> GenerateNewEdgesFor3Adjacent(Disk disk, 
        Graph graph, 
        string point, 
        List<Edge> incidentEdges, 
        int nSides,
        int nAdjacent)
    {
        var trsf = disk.PoincareTransform;
        
        disk.TranslateToPoint(point);

        if (disk.GetPointPosition(point).Length() != 0)
        {
            throw new InvalidOperationException();
        }

        var points = new List<string>()
        {
            point,
            incidentEdges[0].GetOrientedAwayFrom(point).P1,
            incidentEdges[1].GetOrientedAwayFrom(point).P1,
            incidentEdges[2].GetOrientedAwayFrom(point).P1
        }
            .Permute()
            .Select(l => l.ToList())
            .First(l => 
                l[0] == point && Util.Clockwise(
                l.Select(disk.GetPointPosition).ToArray()));

        // points are now in cw order with point at 0, midpoint at 2
        // not sure why points[1] works but it does :)
        disk.PoincareRotate(-disk.GetAngleToPoint(points[1]));
        
        GD.Print("Midpoint after rotate: ", disk.GetPointPosition(points[2]));
        
        var separationSpacing = disk.GetPointPosition(points[2]).Length();
        var separationAngle = 2.0 * Math.PI / nAdjacent;

        var ttrsf = disk.PoincareTransform;
        disk.PoincareRotate(-separationAngle / 2);
        disk.PoincareTranslate(separationSpacing, 0);
        var p1 = disk.AddPoint();
        disk.PoincareTransform = ttrsf;
        
        
        disk.PoincareRotate(separationAngle / 2);
        disk.PoincareTranslate(separationSpacing, 0);
        var p2 = disk.AddPoint();
        
        disk.PoincareTransform = trsf;
        return new() { new Edge(point, p1), new Edge(point, p2) };
    }
    
    
    
    
    /**
     * polygonAngle: interior angle between two incident edges of the polygon 
     */
    public List<Edge> GenerateNewEdges(Disk disk, Graph graph, string point, List<Edge> incidentEdges, int nSides, int nAdjacent)
    {
        if (incidentEdges.Count != 2 && incidentEdges.Count != 3)
        {
            return new List<Edge>();
            throw new ArgumentException("Invalid incident edge number.");
        }
     
        var trsf = disk.PoincareTransform;

        var result = new List<Edge>();
        if (incidentEdges.Count == 2)
        {
            result.AddRange(GenerateNewEdgesFor2Adjacent(disk, graph, point, incidentEdges, nSides, nAdjacent));
        } 
        else if (incidentEdges.Count() == 3)
        {
            result.AddRange(GenerateNewEdgesFor3Adjacent(disk, graph, point, incidentEdges, nSides, nAdjacent));
        }

        disk.PoincareTransform = trsf;

        foreach (var edge in result)
        {
            graph.AddEdge(edge);
        }
        
        return result;
    }
}