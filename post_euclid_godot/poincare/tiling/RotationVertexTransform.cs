using System;
using System.Collections.Generic;
using System.Linq;

namespace PostEuclid.poincare.tiling;

public class RotationVertexTransform : IVertexTransform
{
    private double _angle;
    
    public RotationVertexTransform(double angle)
    {
        _angle = angle;
    }
    
    public Polygon Generate(
        string vertex,
        Polygon sourcePolygon, 
        Disk disk,
        IIndexSource polygonIndexSource,
        IIndexSource edgeIndexSource)
    {
        // starting with P0 == origin vertex
        var relativeEdges = sourcePolygon.EdgesRelativeTo(vertex);

        var origin = relativeEdges[0].P0;

        var result = new List<PolygonEdge>();
        
        // for first edge, rotate about P0 to produce a new p1
        result.Add(new PolygonEdge(
            origin,
            Util.CreateRotatedPoint(disk, relativeEdges[0].P1, origin, (float)_angle),
            sourcePolygon.LayerIndex + 1,
            edgeIndexSource.GetNextIndex(),
            false,
            false,
            false)); // only end point is uncovered
        
        // create mirrors of the subsequent edges until the last edge
        for (var i = 1; i < relativeEdges.Count - 1; i++)
        {
            result.Add(new PolygonEdge(
                result.Last().P1,
                Util.CreateRotatedPoint(disk, relativeEdges[i].P1, origin, (float)_angle),
                sourcePolygon.LayerIndex + 1,
                edgeIndexSource.GetNextIndex(),
                true, 
                true, 
                false));
        }
        
        // add the last edge, also hidden
        result.Add(new PolygonEdge(
            result.Last().P1,
            origin,
            sourcePolygon.LayerIndex + 1,
            edgeIndexSource.GetNextIndex(),
            false,
            false, 
            false));
        
        return new Polygon(result, 
            sourcePolygon.LayerIndex + 1, 
            polygonIndexSource.GetNextIndex(),
            sourcePolygon.EdgeTransforms,
            sourcePolygon.VertexTransforms);
    }
}