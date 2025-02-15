from __future__ import annotations

import math
import typing
from copy import copy
from enum import Enum

import numpy

from post_euclid.hyperbolic_2d.scene import Scene, SceneLineSegment, SceneItem


def _create_mirrored_point(scene: Scene, p: str, p0: str, p1: str) -> str:
    with scene:
        # to mirror about an edge
        # translate such that p0 at origin
        p0e = scene.point_value(p0).get_euclidean_representation()

        scene.translate(-p0e.x, -p0e.y)

        # rotate such that p1 is on x axis
        p0e_new = scene.point_value(p0).get_euclidean_representation()
        if math.hypot(p0e_new.x, p0e_new.y) != 0:
            raise ValueError()

        p1e = scene.point_value(p1).get_euclidean_representation()

        angle = math.atan2(p1e.y, p1e.x)
        scene.rotate(-angle)

        p0e = scene.point_value(p0).get_euclidean_representation()
        if math.hypot(p0e.x, p0e.y) != 0:
            raise ValueError()

        p1e = scene.point_value(p1).get_euclidean_representation()
        if p1e.y > 10e-15:
            raise ValueError()

        p = scene.point_value(p).get_euclidean_representation()
        scene.translate(-p.x, p.y)
        return scene.create_point_reference()


class EdgeTransform:
    """
    Transform applied to a polygon based on the points of an
    edge

    transform is applied to a polygon about the edge,
    either in the form of a rotation or a reflection

    - in the case of a reflection, all points of the
    polygon EXCEPT the edge points are transformed
    to generate new points.
        procedure:
            if edge shares no points, 2 new points generated
            if edge shares one point, 1 new point generated

    - in case of rotation, rotation is about p0, the resulting
    polygon has new vertices EXCEPT for p0
    """

    class Type(Enum):
        ROTATION = 0
        MIRROR = 1

    def __init__(self, type: EdgeTransform.Type):
        self._type = type

    def generate(self, polygon_edge: PolygonEdge, scene: Scene) -> Polygon:
        if self._type == EdgeTransform.Type.MIRROR:
            return self._generate_mirror(polygon_edge, scene)
        else:
            return self._generate_rotation(scene)

    def _generate_mirror(self, polygon_edge: PolygonEdge, scene: Scene) -> Polygon:
        p0 = polygon_edge.p0
        p1 = polygon_edge.p1
        points = (p0, p1)
        polygon_edges = [p for p in polygon_edge.polygon.edges]
        offset_index = polygon_edges.index(polygon_edge)
        # always start with the edge matching the current polygon_edge

        edges = []

        count = 0
        for i in range(0, len(polygon_edges)):
            edge = polygon_edges[(i + offset_index) % len(polygon_edges)]

            if edge.p0 in points and edge.p1 in points:
                # The edge is already inside the frontier that we are generating, prevent it from generating
                # further entities
                edges.append(PolygonEdge(edge.p0, edge.p1, True, *edge.transforms))
            else:
                # if edge has one common point, mirror the remaining point
                if count == 0:
                    m_p0 = polygon_edge.p0
                else:
                    m_p0 = edges[-1].p1

                if count == len(polygon_edges) - 1:
                    m_p1 = polygon_edge.p0
                else:
                    m_p1 = _create_mirrored_point(scene, edge.p1, p0, p1)

                # edge is now re-enabled if the transform has exposed it to the frontier
                edges.append(PolygonEdge(m_p0, m_p1, False, *edge.transforms))

            count += 1

        return Polygon(*edges)

    def _generate_rotation(self, scene: Scene) -> Polygon:
        raise NotImplementedError()


class PolygonEdge:

    def __init__(self, p0: str, p1: str, is_redundant: bool, *transforms: EdgeTransform):
        self.p0 = p0
        self.p1 = p1
        self.is_redundant = is_redundant

        self._polygon: typing.Optional[Polygon] = None

        if self.p0 == self.p1:
            raise ValueError("Points should be distinct")

        self.transforms = transforms

    @property
    def polygon(self) -> Polygon:
        if self._polygon is None:
            raise ValueError()

        return self._polygon

    def set_polygon(self, polygon: Polygon):
        if self._polygon is not None:
            raise ValueError()

        self._polygon = polygon

    def is_connected_to(self, next_edge: PolygonEdge):
        return self.p1 == next_edge.p0


class Polygon:

    def __init__(self, *edges: PolygonEdge):
        # polygon edges consist of points
        for i in range(0, len(edges)):
            e0 = edges[i]
            e1 = edges[(i + 1) % len(edges)]

            if not e0.is_connected_to(e1):
                raise ValueError("Edges should be connected")

        self._edges = edges
        for e in self._edges:
            e.set_polygon(self)

    @property
    def edges(self) -> typing.Iterator[PolygonEdge]:
        for e in self._edges:
            yield e


class SceneEdgeGenerator:
    """
    Ensures the uniqueness of generated edges
    """

    def __init__(self, scene: Scene):
        self._scene = scene
        self._generated_edges: typing.Dict[typing.Tuple[str, str], SceneItem] = {}

    def create_edge_scene_item(self, p0: str, p1: str):
        key = (p0, p1)
        key_inv = (p1, p0)

        if key in self._generated_edges:
            return self._generated_edges[key]

        if key_inv in self._generated_edges:
            return self._generated_edges[key_inv]

        ls = SceneLineSegment(p0, p1)

        self._scene.add_scene_item(ls)

        self._generated_edges[key] = ls


class SpanningTreeNode:

    def __init__(self, parent: typing.Optional[SpanningTreeNode], polygon_edge: PolygonEdge):
        self._parent = parent
        self.polygon_edge = polygon_edge
        self._child_nodes: typing.List[SpanningTreeNode] = []

    def walk(self, callback: typing.Callable[[SpanningTreeNode], []]):
        callback(self)
        for n in self._child_nodes:
            callback(n)

        for n in self._child_nodes:
            n.walk(callback)

    def generate(self, scene: Scene, depth: int):
        if depth == 0:
            return

        if self.polygon_edge.is_redundant:
            # edge is already shared with another polygon, performing transform would generate overlaps
            return

        if len(self._child_nodes) == 0:
            # for each edge of the polygon edges generate a child node
            for t in self.polygon_edge.transforms:
                child_polygon = t.generate(self.polygon_edge, scene)
                for edge in child_polygon.edges:
                    self._child_nodes.append(SpanningTreeNode(self, edge))

        for child_node in self._child_nodes:
            child_node.generate(scene, depth - 1)


class Tiling_3_7:
    """
    Tiling of the hyperbolic plane,
    data is stored as a spanning tree linked by edges.

    Polygons are identified by a set of point pairs (edges)
    """

    def __init__(self, scene: Scene):
        self._scene = scene

    def generate(self):
        # based off "constructCenterPolygon" defined in http://aleph0.clarku.edu/~djoyce/poincare/Polygon.java

        n = 4
        k = 6

        points = []

        a = math.pi / n
        b = math.pi / k
        c = math.pi / 2

        sin_a = math.sin(a)
        sin_b = math.sin(b)

        radius = math.sin(c - b - a) / math.sqrt(1 - sin_b * sin_b - sin_a * sin_a)

        for i in range(0, n):
            angle = math.radians(i * 360 / n)

            with self._scene:
                self._scene.rotate(angle)
                self._scene.translate(0, radius)

                points.append(self._scene.create_point_reference())

        root_shape = Polygon(
            PolygonEdge(points[0], points[1], False, EdgeTransform(EdgeTransform.Type.MIRROR)),
            PolygonEdge(points[1], points[2], False, EdgeTransform(EdgeTransform.Type.MIRROR)),
            PolygonEdge(points[2], points[3], False, EdgeTransform(EdgeTransform.Type.MIRROR)),
            PolygonEdge(points[3], points[0], False, EdgeTransform(EdgeTransform.Type.MIRROR))
        )


        scene_items = []

        se = SceneEdgeGenerator(self._scene)

        tree = [SpanningTreeNode(None, e) for e in root_shape.edges]
        for t in tree:
            t.generate(self._scene, 2)
            t.walk(lambda node: se.create_edge_scene_item(node.polygon_edge.p0, node.polygon_edge.p1))

        for i in scene_items:
            self._scene.add_scene_item(i)
