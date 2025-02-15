using System;
using System.Collections.Generic;
using System.Linq;

namespace PostEuclid.poincare.tiling;

public class MirrorEdgeTransform : IEdgeTransform
{

    public Polygon Generate(
        PolygonEdge polygonEdge, 
        Disk disk, 
        IIndexSource polygonIndexSource,
        IIndexSource edgeIndexSource)
    {
        var polygonEdges = polygonEdge.RootPolygonEdgesRelative();
        
        var result = new List<PolygonEdge>();
        
        result.Add(new PolygonEdge(
            polygonEdges[0].P1,
            polygonEdges[0].P0,
            polygonEdge.LayerIndex + 1,
            edgeIndexSource.GetNextIndex(),
            false,
            false,
            false));
        
        for (int i = polygonEdges.Count - 1; i >= 1; i--)
        {
            var edge = polygonEdges[i];

            // start of this edge is end of last edge
            var mP0 = result.Last().P1;
            
            // if the final edge, connect back to the start, otherwise generate a new point
            var mP1 = i == polygonEdges.Count - 1 ? 
                result.First().P0 : 
                Util.CreateMirroredPoint(disk, edge.P0, polygonEdges[0].P0, polygonEdges[0].P1);

            bool mp0IsCovered = mP0 == polygonEdge.P0 || mP0 == polygonEdge.P1;
            
            bool mp1IsCovered = mP1 == polygonEdge.P0 || mP1 == polygonEdge.P1;
            
            result.Add(new PolygonEdge(
                mP0,
                mP1,
                polygonEdge.LayerIndex + 1,
                edgeIndexSource.GetNextIndex(),
                (!mp0IsCovered && !mp1IsCovered),
                !mp0IsCovered && !(i == polygonEdges.Count - 1 || i == 1),
                !mp1IsCovered && !(i == polygonEdges.Count - 1 || i == 1)));
        }
        
        return new Polygon(result, polygonEdge.LayerIndex + 1, polygonIndexSource.GetNextIndex(),
            polygonEdge.Polygon.EdgeTransforms,
            polygonEdge.Polygon.VertexTransforms);
    }
}