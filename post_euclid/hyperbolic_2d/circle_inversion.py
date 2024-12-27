import math
import typing

import numpy

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities


class CircleInversion:

    @staticmethod
    def _to_outside_dist(x):
        base_triangle_length = numpy.sqrt(1 - x * x)

        outer_point_acute_angle = numpy.asin(x)

        outer_point_dist = base_triangle_length / numpy.tan(outer_point_acute_angle)

        return x + outer_point_dist

    @staticmethod
    def to_outside(xy: euclidean_2d.entities.Point) -> euclidean_2d.entities.Point:
        r = math.hypot(*xy)
        r1 = CircleInversion._to_outside_dist(r)

        return euclidean_2d.entities.Point(
           xy.x * r1 / r,
           xy.y * r1 / r
        )

