
using PostEuclid.poincare;

namespace PostEuclid.poincare.tiling;


public interface IEdgeTransform
{
    public Polygon Generate(
        PolygonEdge polygonEdge, 
        Disk disk,
        IIndexSource polygonIndexSource,
        IIndexSource edgeIndexSource);
}