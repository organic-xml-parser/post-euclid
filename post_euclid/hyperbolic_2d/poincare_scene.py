"""
Represents a scene of entities placed on the poincare disk
"""
from __future__ import annotations

import typing
import uuid
from dataclasses import dataclass

import numpy

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d.entities import Euclidean2D
from post_euclid.hyperbolic_2d.poincare import PoincarePoint, Poincare, PoincareLineSegment


class PoincareSceneItem:

    def __init__(self, keys: typing.Set[str]):
        self._keys = keys

    @property
    def keys(self):
        for k in self._keys:
            yield k

    def get_concrete_poincare_geometry(self, scene: PoincareScene) -> Poincare:
        raise NotImplementedError()


class PoincareScenePoint(PoincareSceneItem):

    def __init__(self, p: str):
        super().__init__({p})
        self.p = p

    def get_concrete_poincare_geometry(self, scene: PoincareScene) -> Poincare:
        return scene.point_value(self.p)


class PoincareSceneLineSegment(PoincareSceneItem):

    def __init__(self, p0: str, p1: str):
        super().__init__({p0, p1})
        self._p0 = p0
        self._p1 = p1

    def get_concrete_poincare_geometry(self, scene: PoincareScene) -> Euclidean2D:
        return PoincareLineSegment(
            scene.point_value(self._p0),
            scene.point_value(self._p1)
        ).get_circle_arc().circle_arc


class PoincareScene:

    def __init__(self):
        self._points: typing.Dict[str, PoincarePoint] = {}
        self._scene_items: typing.List[PoincareSceneItem] = []

        self._transformation_matrix = numpy.matrix([
            [1, 0],
            [0, 1]
        ], dtype=complex)

    def translate(self, dx: float, dy: float):
        b = dx + dy * 1j

        mat = numpy.matrix([
            [1, b],
            [numpy.conjugate(b), 1]
        ], dtype=complex)

        self._transformation_matrix = mat * self._transformation_matrix

    def rotate(self, angle: float):
        mat = numpy.matrix([
            [numpy.cos(angle) + numpy.sin(angle) * 1j, 0],
            [0, 1]
        ], dtype=complex)

        self._transformation_matrix = mat * self._transformation_matrix

    def add_scene_item(self, poincare_scene_item: PoincareSceneItem):
        if any(k not in self._points for k in poincare_scene_item.keys):
            raise ValueError("Scene item references points outside the scene")

        self._scene_items.append(poincare_scene_item)

    def get_renderable_entities(self) -> typing.Dict[PoincareSceneItem, Poincare]:
        return {si: si.get_concrete_poincare_geometry(self) for si in self._scene_items}

    def create_point_reference(self, x: float, y: float) -> str:
        key = str(uuid.uuid4())
        x, y = PoincareScene._apply_transform_matrix(numpy.linalg.inv(self._transformation_matrix), (x, y))

        self._points[key] = PoincarePoint(x, y)
        return key

    def point_value(self, key: str) -> PoincarePoint:
        x, y = PoincareScene._apply_transform_matrix(self._transformation_matrix, self._points[key].xy)

        return PoincarePoint(x, y)

    def transformed_coordinate(self, x, y) -> typing.Tuple[float, float]:
        return PoincareScene._apply_transform_matrix(self._transformation_matrix, (x, y))

    def underlying_coordinate(self, x, y) -> typing.Tuple[float, float]:
        return PoincareScene._apply_transform_matrix(
            numpy.linalg.inv(self._transformation_matrix),
            (x, y))

    def underlying_point_coordinates(self, key: str) -> PoincarePoint:
        """
        :return: the point coordinate as stored, with no transform applied to it
        """

        return self._points[key]

    @staticmethod
    def _apply_transform_matrix(transform_matrix, xy) -> typing.Tuple[float, float]:
        z = xy[0] + xy[1] * 1j

        num = (transform_matrix[0, 0] * z + transform_matrix[0, 1])
        denom = (transform_matrix[1, 0] * z + transform_matrix[1, 1])

        p_new = num / denom

        return numpy.real(p_new), numpy.imag(p_new)

    @staticmethod
    def translation_mobius(dx: float) -> typing.Tuple[float, float, float, float]:
        e_p = numpy.pow(numpy.e, dx)

        return (e_p + 1, e_p - 1,
                e_p - 1, e_p + 1)

    @staticmethod
    def rotation_mobius(angle) -> typing.Tuple[float, float, float, float]:
        return numpy.cos(angle) + numpy.sin(angle) * 1j, 0, 0, 1

    def mobius_transformation(self, a, b, c, d) -> None:
        # for translation
        # z - k / -abs(k)z + 1

        # giving
        #  1   -k
        # -|k|   1

        if a * d == b * c:
            raise ValueError()

        # assume coordinate is x + iy
        for k in self._points.keys():
            existing_point = self._points[k]

            z = (existing_point.x + existing_point.y * 1j)

            result = (a * z + b) / (c * z + d)

            self._points[k] = PoincarePoint(numpy.real(result), numpy.imag(result))
