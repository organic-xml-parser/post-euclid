using System;

namespace PostEuclid.poincare.tiling;

/**
 * Port of Ajit Datar Thesis on Hyperbolic Tilings, 3.1.2
 */
public class ExposureParameterTable
{
    private int p;
    private int q;

    private int maxExposure;
    private int minExposure;

    public ExposureParameterTable(int p, int q)
    {
        this.p = p;
        this.q = q;
        
        // todo: exception for allowed values of p, q
        
        // maximum exposure is number of polygons meeting at a vertex - 1
        maxExposure = q - 1;

        // todo: not sure this is correct
        minExposure = 1;
    }

    public int Exposure(int layer, int vertexIndex, int pgonIndex)
    {
        if (layer == 0)
        {
            if (p == 3)
            {
                return pgonIndex == 0 ? minExposure : maxExposure;
            }

            if (q == 3)
            {
                return maxExposure;
            }
        }
        
        if (q == 3)
        {
            return vertexIndex == 0 ? minExposure : maxExposure;
        }
        
        return pgonIndex == 0 ? minExposure : maxExposure;
    }

    public int VertexesToSkip(int exposure)
    {
        if (q == 3)
        {
            return exposure == minExposure ? 3 : 2;
        }

        if (p == 3)
        {
            return 1;
        }

        return exposure == minExposure ? 1 : 0;
    }

    public int PolygonsToSkip(int exposure, int vertexIndex)
    {
        if (q == 3)
        {
            return 0;
        }

        if (p == 3)
        {
            return exposure == minExposure ? -1 : 0;
        }

        return vertexIndex == 0 ? -1 : 0;
    }

    public int VerticesToVisit(int exposure)
    {
        if (p == 3)
        {
            return 1;
        }

        if (q == 3)
        {
            return exposure == minExposure ? p - 5 : p - 4;
        }

        return exposure == minExposure ? p - 3 : p - 2;
    }

    public int PolygonsToGenerate(int exposure, int vertexIndex)
    {
        if (q == 3)
        {
            return 1;
        }

        if (p == 3)
        {
            return exposure == minExposure ? q - 4 : q - 3;
        }

        throw new InvalidOperationException();
    }
}