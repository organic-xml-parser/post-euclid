from __future__ import annotations

import math
import typing
from dataclasses import dataclass

import numpy

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities
from post_euclid.euclidean_2d.entities import LineIntersection, normalize_angle, collinear
from post_euclid.hyperbolic_2d.circle_inversion import CircleInversion


class Poincare:
    """
    Base class for Poincare projections
    """
    pass


class PoincarePoint(euclidean_2d.entities.Point, Poincare):

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

        #if numpy.hypot(self.x, self.y) > 1:
        #    raise ValueError("Invalid transformation")

    @property
    def xy(self):
        return self.x, self.y


class PoincareLineSegment(Poincare):

    @dataclass
    class CircleArcConstruction:
        circle_arc: euclidean_2d.entities.Euclidean2D
        extra_entities: typing.List[euclidean_2d.entities.Euclidean2D]

    def __init__(self, p0: PoincarePoint, p1: PoincarePoint):
        self.p0 = p0
        self.p1 = p1

    def _get_direct_line_segment(self) -> CircleArcConstruction:
        return PoincareLineSegment.CircleArcConstruction(
            circle_arc=euclidean_2d.entities.LineSegment(self.p0, self.p1),
            extra_entities=[])

    def get_circle_arc(self) -> CircleArcConstruction:
        # arc through p0, p1, and tangent to unit circle
        # returned is the radius and midpoint of the circle

        if self.p0.is_origin or self.p1.is_origin:
            return self._get_direct_line_segment()

        px = self.p0.x
        py = self.p0.y

        qx = self.p1.x
        qy = self.p1.y

        px2 = px * px
        py2 = py * py

        qx2 = qx * qx
        qy2 = qy * qy

        u = (px2 + py2 + 1)
        v = (qx2 + qy2 + 1)

        denom = 2 * (px * qy - py * qx)

        # basically equivalent to collinear with origin
        if denom < 10e-15:
            return self._get_direct_line_segment()

        ox = (qy * u - py * v) / denom
        oy = (-qx * u + px * v) / denom

        origin = PoincarePoint(ox, oy)
        radius = math.sqrt(ox * ox + oy * oy - 1)

        # calculate the angle between the two points:
        circ_to_p0 = euclidean_2d.entities.Point(origin.x - self.p0.x, origin.y - self.p0.y)
        circ_to_p1 = euclidean_2d.entities.Point(origin.x - self.p1.x, origin.y - self.p1.y)
        angle_0 = numpy.atan2(circ_to_p0.y, circ_to_p0.x)
        angle_1 = numpy.atan2(circ_to_p1.y, circ_to_p1.x)

        # normalize the -pi -> pi range to: 0 -> 2pi
        angle_0_prev = angle_0
        angle_1_prev = angle_1
        angle_0 = normalize_angle(angle_0_prev)
        angle_1 = normalize_angle(angle_1_prev)

        # now both angles are in the range 0 --> 2pi
        angle_min = min(angle_0, angle_1)
        angle_max = max(angle_0, angle_1)

        delta_angle = angle_max - angle_min
        if delta_angle > numpy.pi:
            delta_angle = 2 * numpy.pi - delta_angle

            angle_max = angle_min - delta_angle

        if numpy.abs(angle_max - angle_min) > numpy.pi:
            raise ValueError()

        return PoincareLineSegment.CircleArcConstruction(
            euclidean_2d.entities.CircleArc(
                euclidean_2d.entities.Circle(origin, radius),
                angle_min,
                angle_max
            ),
            extra_entities=[]
        )
