from __future__ import annotations

import math
import typing
from dataclasses import dataclass

import numpy

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities
from post_euclid.euclidean_2d.entities import normalize_angle, Euclidean2D
from post_euclid.euclidean_2d.circle_inversion import CircleInversion
from post_euclid.hyperbolic_2d.hyperbolic_model_entity import HyperbolicModelEntity, HyperbolicModelTransformTool, T, \
    HyperbolicModelEntityFactory, HyperbolicModel, T_Point, T_Line

T_Transform = typing.Tuple[complex, complex, complex, complex]


class PoincareModelTransformTool(HyperbolicModelTransformTool[T_Transform]):

    def create_identity(self) -> T:
        return (1, 0,
                0, 1)

    def create_translation_like(self, dx: float, dy: float) -> T_Transform:
        b = complex(dx, dy)

        return (1,                      b,
                numpy.conjugate(b),     1)

    def create_rotation_like(self, angle: float) -> T_Transform:
        return (
            complex(math.cos(angle), math.sin(angle)),   0,
            0,                                           1
        )

    def gyro_mult(self, left: T_Transform, right: T_Transform) -> T_Transform:
        a = left[0]
        b = left[1]
        c = left[2]
        d = left[3]

        e = right[0]
        f = right[1]
        g = right[2]
        h = right[3]

        return (
            a * e + b * g,          a * f + b * h,
            c * e + d * g,          c * f + d * h
        )

    def get_inverse(self, trsf: T) -> T:
        a = trsf[0]
        b = trsf[1]
        c = trsf[2]
        d = trsf[3]

        det = a * d - b * c

        if det == 0:
            raise ValueError("Not invertible")

        det_inv = 1.0 / det

        return (
            d * det_inv,    -b * det_inv,
            -c * det_inv,   a * det_inv
        )



_TRANSFORM_TOOL = PoincareModelTransformTool()

class PoincareModelEntity(HyperbolicModelEntity[T_Transform]):

    def get_transform_tool(self) -> HyperbolicModelTransformTool[T]:
        return _TRANSFORM_TOOL

    def get_euclidean_representation(self) -> Euclidean2D:
        raise NotImplementedError()

    def apply_transform(self, model_transfrom: T):
        raise NotImplementedError()


class PoincareModelPoint(euclidean_2d.entities.Point, PoincareModelEntity):

    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)

        if numpy.hypot(self.x, self.y) > 1:
            raise ValueError("Invalid transformation")

    @property
    def xy(self):
        return self.x, self.y

    def euclidean_representation(self) -> euclidean_2d.entities.Euclidean2D:
        return self

    def apply_transform(self, model_transfrom: T_Transform):
        z = complex(self.x, self.y)
        p_new = (model_transfrom[0] * z + model_transfrom[1]) / (model_transfrom[2] * z + model_transfrom[3])

        self.x = p_new.real
        self.y = p_new.imag


class PoincareModelLineSegment(PoincareModelEntity):

    def __init__(self, p0: PoincareModelPoint, p1: PoincareModelPoint):
        self.p0 = p0
        self.p1 = p1

    def _get_direct_line_segment(self) -> euclidean_2d.entities.Euclidean2D:
        return euclidean_2d.entities.LineSegment(self.p0, self.p1)

    def apply_transform(self, model_transfrom: T):
        self.p0.apply_transform(model_transfrom)
        self.p1.apply_transform(model_transfrom)

    def get_euclidean_representation(self) -> euclidean_2d.entities.Euclidean2D:
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
        if abs(denom) < 10e-15:
            return self._get_direct_line_segment()

        ox = (qy * u - py * v) / denom
        oy = (-qx * u + px * v) / denom

        origin = euclidean_2d.entities.Point(ox, oy)
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

        return euclidean_2d.entities.CircleArc(
                euclidean_2d.entities.Circle(origin, radius),
                angle_min,
                angle_max
            )


class PoincareModelEntityFactory(HyperbolicModelEntityFactory[T_Transform, PoincareModelPoint, PoincareModelLineSegment]):

    def create_point(self) -> PoincareModelPoint:
        return PoincareModelPoint(0, 0)

    def create_line_segment(self, p0: PoincareModelPoint, p1: PoincareModelPoint) -> PoincareModelLineSegment:
        return PoincareModelLineSegment(p0, p1)


class PoincareHyperbolicModel(HyperbolicModel[T_Transform, PoincareModelPoint, PoincareModelLineSegment]):

    def __init__(self):
        self._factory = PoincareModelEntityFactory()
        self._tool = PoincareModelTransformTool()

    def get_factory(self) -> HyperbolicModelEntityFactory[T, T_Point, T_Line]:
        return self._factory

    def get_transform_tool(self) -> HyperbolicModelTransformTool[T]:
        return self._tool
