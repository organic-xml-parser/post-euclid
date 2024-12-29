"""
Represents a scene of entities placed on the poincare disk
"""
from __future__ import annotations

import typing
import uuid
from copy import copy
from dataclasses import dataclass

import numpy

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d.entities import Euclidean2D
from post_euclid.hyperbolic_2d.poincare.poincare import PoincareModelEntity, PoincareModelPoint, \
    PoincareModelLineSegment, PoincareModelTransformTool


class PoincareSceneItem:

    def __init__(self, keys: typing.Set[str]):
        self._keys = keys

    @property
    def keys(self):
        for k in self._keys:
            yield k

    def get_concrete_poincare_geometry(self, scene: PoincareScene) -> PoincareModelEntity:
        raise NotImplementedError()


class PoincareScenePoint(PoincareSceneItem):

    def __init__(self, p: str):
        super().__init__({p})
        self.p = p

    def get_concrete_poincare_geometry(self, scene: PoincareScene) -> PoincareModelPoint:
        return scene.point_value(self.p)


class PoincareSceneLineSegment(PoincareSceneItem):

    def __init__(self, p0: str, p1: str):
        super().__init__({p0, p1})
        self._p0 = p0
        self._p1 = p1

    def get_concrete_poincare_geometry(self, scene: PoincareScene) -> PoincareModelLineSegment:
        return PoincareModelLineSegment(
            scene.point_value(self._p0),
            scene.point_value(self._p1)
        )


class PoincareScene:

    def __init__(self):
        self._points: typing.Dict[str, PoincareModelPoint] = {}
        self._scene_items: typing.List[PoincareSceneItem] = []
        self._tool = PoincareModelTransformTool()
        self._transform = self._tool.create_identity()

    def translate(self, dx: float, dy: float):
        self._transform = self._tool.gyro_mult(
            self._tool.create_translation_like((dx, dy)), self._transform)

    def rotate(self, angle: float):
        self._transform = self._tool.gyro_mult(
            self._tool.create_rotation_like(angle), self._transform)

    def add_scene_item(self, poincare_scene_item: PoincareSceneItem):
        if any(k not in self._points for k in poincare_scene_item.keys):
            raise ValueError("Scene item references points outside the scene")

        self._scene_items.append(poincare_scene_item)

    def get_renderable_entities(self) -> typing.Iterator[euclidean_2d.entities.Euclidean2D]:
        for item in self._scene_items:
            poincare_geom = item.get_concrete_poincare_geometry(self)
            poincare_geom.apply_transform(self._transform)
            yield poincare_geom.get_euclidean_representation()

    def create_point_reference(self, x: float, y: float) -> str:
        key = str(uuid.uuid4())

        point = PoincareModelPoint(x, y)

        # remove any coordinate offset to store the underlying point value
        point.apply_transform(self._tool.get_inverse(self._transform))

        self._points[key] = point

        return key

    def point_value(self, key: str) -> PoincareModelPoint:
        return copy(self._points[key])

    def underlying_point_coordinates(self, key: str) -> PoincareModelPoint:
        """
        :return: the point coordinate as stored, with no transform applied to it
        """

        return self._points[key]
