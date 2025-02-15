using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PostEuclid.poincare.tiling;

public class PolygonEdge
{
    /**
     * Which layer this edge belongs to.
     */
    public readonly int LayerIndex;
    
    /**
     * The index of this edge in the layer.
     */
    public readonly int IndexInLayer;
    
    private Polygon _polygon;
    
    public string P0 { get; set; }

    public string P1 { get; set; }

    public readonly bool IsActive;
    public readonly bool IsP0Active;
    public readonly bool IsP1Active;
    
    public PolygonEdge(
        string p0,
        string p1,
        int layerIndex,
        int indexInLayer, 
        bool isActive, 
        bool isP0Active, 
        bool isP1Active)
    {
        if (layerIndex < 0 || indexInLayer < 0)
        {
            throw new ArgumentException();
        }

        LayerIndex = layerIndex;
        IndexInLayer = indexInLayer;
        this.IsActive = isActive;
        this.IsP0Active = isP0Active;
        this.IsP1Active = isP1Active;

        P0 = p0;
        P1 = p1;

        if (p0 == p1)
        {
            throw new InvalidOperationException();
        }
    }

    public List<PolygonEdge> RootPolygonEdgesRelative()
    {
        return _polygon.EdgesRelativeTo(this);
    }

    public Polygon Polygon
    {
        get
        {
            if (_polygon == null)
            {
                throw new InvalidOperationException("Polygon has not been set.");
            }
            return _polygon;
        }

        set
        {
            if (_polygon != null)
            {
                throw new InvalidOperationException("Polygon has already been set.");
            }
            
            _polygon = value;
        }
    }
    
    public bool IsConnectedTo(PolygonEdge other)
    {
        return P1.Equals(other.P0);
    }
}