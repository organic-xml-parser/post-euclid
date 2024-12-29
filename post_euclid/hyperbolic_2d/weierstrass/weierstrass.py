import math
import typing
from math import cos, sin, cosh, sinh

import numpy

from post_euclid.euclidean_2d.entities import Euclidean2D, Point
from post_euclid.hyperbolic_2d.hyperbolic_model_entity import HyperbolicModelTransformTool, T, HyperbolicModelEntity, \
    HyperbolicModel, HyperbolicModelEntityFactory, T_Point, T_Line
from post_euclid.hyperbolic_2d.poincare.poincare import PoincareModelPoint, PoincareModelLineSegment


T_Transform = numpy.array


class WeierstrassModelTransformTool(HyperbolicModelTransformTool[T_Transform]):
    """
    See: https://martinlusblog.wordpress.com/2020/07/25/on-simulating-and-understanding-hyperbolic-space-with-no-previous-knowledge-on-the-subject/
    """

    def create_identity(self) -> T:
        return numpy.array([
            [1.0, 0.0, 0.0],
            [0.0, 1.0, 0.0],
            [0.0, 0.0, 1.0]
        ])

    def create_translation_like(self, dy: float, dz: float) -> T_Transform:
        # dy / dz is equivalent to rotation followed by dr
        angle = math.atan2(dy, dz)
        dr = math.hypot(dy, dz)

        a = self.create_rotation_like(angle)

        b = numpy.array([
            [cosh(dr),  sinh(dr),   0.0],
            [sinh(dr),  cosh(dr),   0.0],
            [0.0,       0.0,        1.0]
        ])

        c = self.create_rotation_like(-angle)

        return numpy.matmul(a, numpy.matmul(b, c))

    def create_rotation_like(self, angle: float) -> T_Transform:
        return numpy.array([
            [1.0, 0.0, 0.0],
            [0.0, cos(angle), -sin(angle)],
            [0.0, sin(angle), cos(angle)]
        ])

    def gyro_mult(self, left: T, right: T) -> T_Transform:
        return numpy.matmul(left, right)

    def get_inverse(self, trsf: T) -> T_Transform:
        return numpy.linalg.inv(trsf)


class WeierstrassHyperbolicModelEntity(HyperbolicModelEntity[T_Transform]):
    pass


class WeierstrassModelPoint(WeierstrassHyperbolicModelEntity):

    def __init__(self, y: float, z: float):
        self.x = math.sqrt(y * y + z * z + 1)
        self.y = y
        self.z = z

    def get_euclidean_representation(self) -> Euclidean2D:
        return self.as_poincare_point().get_euclidean_representation()

    def apply_transform(self, model_transfrom: T_Transform):
        original = numpy.array([self.x, self.y, self.z])
        vec = numpy.matmul(model_transfrom, original)

        self.x = vec[0]
        self.y = vec[1]
        self.z = vec[2]

        # x**2 == y** 2 + z** 2 + 1
        yy = self.y ** 2
        zz = self.z ** 2

        err = yy + zz + 1 - self.x * self.x

        if err > 10e-5:
            raise ValueError("x value deviation too high")

        self.x = math.sqrt(yy + zz + 1.0)

    def as_poincare_point(self) -> PoincareModelPoint:
        # distance from the "origin"
        frac = 1 / (self.x + 1.0)

        nx = self.y * frac
        ny = self.z * frac

        return PoincareModelPoint(ny, nx)


class WeierstrassModelLineSegment(HyperbolicModelEntity[T_Transform]):

    def __init__(self, p0: WeierstrassModelPoint, p1: WeierstrassModelPoint):
        self.p0 = p0
        self.p1 = p1

    def get_euclidean_representation(self) -> Euclidean2D:
        return PoincareModelLineSegment(
            self.p0.as_poincare_point(),
            self.p1.as_poincare_point()
        ).get_euclidean_representation()

    def apply_transform(self, model_transfrom: T):
        self.p0.apply_transform(model_transfrom)
        self.p1.apply_transform(model_transfrom)


class WeierstrassModelFactory(HyperbolicModelEntityFactory[
                                T_Transform,
                                WeierstrassModelPoint,
                                WeierstrassModelLineSegment]):

    def create_point(self) -> WeierstrassModelPoint:
        return WeierstrassModelPoint(0, 0)

    def create_line_segment(self, p0: WeierstrassModelPoint, p1: WeierstrassModelPoint) -> (
            WeierstrassModelLineSegment):
        return WeierstrassModelLineSegment(p0, p1)


class WeierstrassHyperbolicModel(HyperbolicModel[T_Transform, WeierstrassModelPoint, WeierstrassModelLineSegment]):

    def __init__(self):
        self._factory = WeierstrassModelFactory()
        self._tool = WeierstrassModelTransformTool()

    def get_factory(self) -> HyperbolicModelEntityFactory[
                                T_Transform,
                                WeierstrassModelPoint,
                                WeierstrassModelLineSegment]:
        return self._factory

    def get_transform_tool(self) -> WeierstrassModelTransformTool:
        return self._tool
