using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Godot;
using PostEuclid.poincare.tiling;
using Range = System.Range;

namespace PostEuclid.poincare.tiling;

/**
 * Note: implementation based on: GENERATING HYPERBOLIC PATTERNS FOR REGULAR AND NON-REGULAR P-GONS, Ajit V. Datar
 *
 * Notes from paper:
 *
 * Symmetry Groups:
 *  - symmetry of a pattern defined as an operation which preserves the tiling pattern
 *    (e.g. rotation or mirroring about origin)
 *
 *  Fundamental Region:
 *  - portion of the hyperbolic plane which when transformed by all transformations
 *    in symmetry group completely covers the plane.
 *
 *  Motif/Fundamental Pattern:
 *  - pattern within the fundamental region (think those MC Escher lizards that tesselate)
 *  - if the pattern completely covers the fundamental region, then the whole plane is
 *    covered (since the fundamental region covers the plane).
 *
 *  Regular Tesselations of the form {p, q}:
 *  - p-gon with p sides, with q meeting at each vertex
 *
 *  Symmetry Groups and their Transformations:
 *  - [p, q]:
 *      - reflection across p-gon edge
 *      - reflection across perpendicular bisector of p-gon edge
 *      - reflection across radius of p-gon
 *
 *      Fundamental Region:
 *      - hyperbolic right-triangle angles pi/p, pi/q formed by reflection axes
 *
 * - [p, q]+
 *      - rotation order p about p-gon center
 *      - rotation order q about p-gon vertices
 *      - rotation order 2 about center of p-gon edges
 *
 *      Fundamental Region:
 *      - Isosceles triangle angles 2pi/p, pi/q, pi/q (join 2 f-regions of [p, q])
 *      - Lack of mirror operations means no fundamental boundary..
 *
 * - [p+, q]
 *      - rotation order p about p-gon center
 *      - reflection across p-gon edge
 *      Fundamental Region:
 *      - same as [p, q]+
 *      - boundary is formed by the mirror, q must be even
 *
 * - [p, q+]
 *      - rotation order q about p-gon vertex
 *      - reflection across p-gon edge bisector
 *      Fundamental Region:
 *      - joining of two f-regions from [p, q] along p-gon radius (center to vertex line)
 *
 * P-Gon Layers and edge exposures:
 *    next layer is all p-gons which share edge/vertex with previous layer
 *    exposure of a vertex is number of p-gons joined to it
 */
public static class Tiling
{
    /**
     * Tiling process:
     * each polygon edge has a set of transforms associated with it.
     * parallel to the polygon data structure we have a spanning tree, giving each edge/transform an index
     * some indices will be skipped
     */



    public static class TreeLayerPrune
    {
        public static bool ShouldRemove(int layerIndex, int polygonIndex)
        {
            return false;
            //return layerIndex > 1 && polygonIndex % 12 == 1;
        }
    }
    
    

    public static class Tiling_4_5
    {
        private static List<string> GenerateCenterPolygonPoints(Disk disk)
        {
            var initialTransform = disk.PoincareTransform;
            
            var n = 4;
            var k = 5;

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
        
        public static void Generate(Disk disk, int depth)
        {
            if (depth < 0)
            {
                throw new ArgumentException("Depth must be >= 0");
            }

            var points = GenerateCenterPolygonPoints(disk);
            var edgeTransforms = new List<IEdgeTransform>
            {
                new MirrorEdgeTransform(),
            };
            
            var vertexTransforms = new List<IVertexTransform>
            {
                new RotationVertexTransform(+0.5 * 2.0 * Math.PI / 5.0),
                new RotationVertexTransform(-0.5 * 2.0 * Math.PI / 5.0)
            };
            
            
            var rootShape = new Polygon(
                new List<PolygonEdge>()
                {
                    new(
                        points[0],
                        points[1],
                        0,
                        0,
                        true,
                        true,
                        false),
                    new(
                        points[1],
                        points[2],
                        0,
                        1,
                        true,
                        true,
                        false),
                    new(
                        points[2],
                        points[3],
                        0,
                        2,
                        true,
                        true,
                        false),
                    new(
                        points[3],
                        points[0],
                        0,
                        3,
                        true,
                        true,
                        false)
                }, 0, 0,
                edgeTransforms,
                vertexTransforms);

            var tree = new List<List<Polygon>>();
            tree.Add(new List<Polygon> { rootShape });

            GD.Print("Layers Generated");
            for (var i = 1; i <= depth; i++)
            {
                var nextLayer = new List<Polygon>();
                var polygonIndexSource = new IndexSource();
                var edgeIndexSource = new IndexSource();

                var polygonsToParents = new Dictionary<Polygon, Polygon>();

                for (var polygonIndex = 0; polygonIndex < tree.Last().Count; polygonIndex++)
                {
                    var polygon = tree.Last()[polygonIndex];
                    
                    foreach (var edge in polygon.Edges)
                    {
                        if (edge.IsActive)
                        {
                            foreach (var trsf in polygon.EdgeTransforms)
                            {
                                var next = trsf.Generate(edge, disk, polygonIndexSource, edgeIndexSource);
                                polygonsToParents[next] = polygon;
                                nextLayer.Add(next);
                            }
                        }

                        if (edge.IsP0Active)
                        {
                            foreach (var trsf in polygon.VertexTransforms)
                            {
                                var next = trsf.Generate(edge.P0, polygon, disk, polygonIndexSource, edgeIndexSource);
                                polygonsToParents[next] = polygon;
                                nextLayer.Add(next);
                            }
                        }

                        if (edge.IsP1Active)
                        {
                            foreach (var trsf in polygon.VertexTransforms)
                            {
                                var next = trsf.Generate(edge.P0, polygon, disk, polygonIndexSource, edgeIndexSource);
                                polygonsToParents[next] = polygon;
                                nextLayer.Add(next);
                            }
                        }
                    }
                }
                GD.Print("Current Layer: (", nextLayer.Count, ")", i);

                var indicesToRemove = Enumerable.Range(0, nextLayer.Count)
                    .Where(pi => TreeLayerPrune.ShouldRemove(i, pi))
                    .OrderByDescending(pi => pi);

                foreach (var indexToRemove in indicesToRemove)
                {
                    nextLayer.RemoveAt(indexToRemove);
                }
                
                foreach (var polygon in nextLayer)
                {
                    GD.PrintRaw(polygon.IndexInLayer, " ");
                    
                    /*
                    disk.AddEdge(new(
                        Util.CreateAveragePoint(disk, polygon.Points.ToArray()),
                        Util.CreateAveragePoint(disk, polygonsToParents[polygon].Points.ToArray()),
                        polygonsToParents[polygon].LayerIndex,
                        0,
                        true,
                        false,
                        false
                    ));
                    */
                }
                
                GD.Print();
                
                tree.Add(nextLayer);
            }
            
            foreach (var layer in tree)
            {
                foreach (var polygon in layer)
                {
                    foreach (var edge in polygon.Edges)
                    {
                        //disk.AddEdge(edge);
                    }
                }
            }
        }
    }
}