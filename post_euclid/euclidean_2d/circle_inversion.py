import math
import typing

import numpy

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities


class CircleInversion:

    @staticmethod
    def invert(arc_entity: euclidean_2d.entities.Euclidean2D, point: euclidean_2d.entities.Point):
        if isinstance(arc_entity, euclidean_2d.entities.LineSegment):
            return CircleInversion.invert(euclidean_2d.entities.Line.from_points(arc_entity.p0, arc_entity.p1), point)
        if isinstance(arc_entity, euclidean_2d.entities.Line):
            closest_point = arc_entity.closest_point(point)
            delta = point - closest_point
            return closest_point - delta
        elif isinstance(arc_entity, euclidean_2d.entities.CircleArc):
            dr = (point - arc_entity.circle.center)
            center_dist = (point - arc_entity.circle.center).mag
            return dr.normalized.scaled(1 / center_dist)

    @staticmethod
    def _to_outside_dist(x):
        base_triangle_length = numpy.sqrt(1 - x * x)

        outer_point_acute_angle = numpy.asin(x)

        outer_point_dist = base_triangle_length / numpy.tan(outer_point_acute_angle)

        return x + outer_point_dist
