namespace PostEuclid.poincare.tiling;

public interface IVertexTransform
{
    public Polygon Generate(
        string polygonVertex,
        Polygon sourcePolygon,
        Disk disk,
        IIndexSource polygonIndexSource,
        IIndexSource edgeIndexSource);
}