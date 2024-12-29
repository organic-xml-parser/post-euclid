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
from post_euclid.hyperbolic_2d.hyperbolic_model_entity import HyperbolicModelEntity, HyperbolicModelTransformTool, \
    HyperbolicModel
from post_euclid.hyperbolic_2d.poincare.poincare import PoincareModelEntity, PoincareModelPoint, \
    PoincareModelLineSegment, PoincareModelTransformTool


class SceneItem:

    def __init__(self, keys: typing.Set[str]):
        self._keys = keys

    @property
    def keys(self):
        for k in self._keys:
            yield k

    def get_concrete_geometry(self, scene: Scene) -> HyperbolicModelEntity:
        raise NotImplementedError()


class ScenePoint(SceneItem):

    def __init__(self, p: str):
        super().__init__({p})
        self.p = p

    def get_concrete_poincare_geometry(self, scene: Scene) -> HyperbolicModelEntity:
        return scene.point_value(self.p)


class SceneLineSegment(SceneItem):

    def __init__(self, p0: str, p1: str):
        super().__init__({p0, p1})
        self._p0 = p0
        self._p1 = p1

    def get_concrete_geometry(self, scene: Scene) -> PoincareModelLineSegment:
        return scene.model.get_factory().create_line_segment(
            scene.point_value(self._p0),
            scene.point_value(self._p1)
        )


class Scene:

    def __init__(self, model: HyperbolicModel):
        self._points: typing.Dict[str, HyperbolicModelEntity] = {}
        self._scene_items: typing.List[SceneItem] = []
        self._model = model
        self._transform = self._model.get_transform_tool().create_identity()

    @property
    def model(self) -> HyperbolicModel:
        return self._model

    def translate(self, dx: float, dy: float):
        self._transform = self._model.get_transform_tool().gyro_mult(
            self._model.get_transform_tool().create_translation_like(dx, dy), self._transform)

    def rotate(self, angle: float):
        self._transform = self._model.get_transform_tool().gyro_mult(
            self._model.get_transform_tool().create_rotation_like(angle), self._transform)

    def add_scene_item(self, poincare_scene_item: SceneItem):
        if any(k not in self._points for k in poincare_scene_item.keys):
            raise ValueError("Scene item references points outside the scene")

        self._scene_items.append(poincare_scene_item)

    def get_renderable_entities(self) -> typing.Iterator[euclidean_2d.entities.Euclidean2D]:
        for item in self._scene_items:
            geom = item.get_concrete_geometry(self)
            geom.apply_transform(self._transform)
            yield geom.get_euclidean_representation()

    def create_point_reference(self) -> str:
        key = str(uuid.uuid4())

        # remove any coordinate offset to store the underlying point value
        point = self._model.get_factory().create_point()

        point.apply_transform(self._model.get_transform_tool().get_inverse(self._transform))

        self._points[key] = point

        return key

    def point_reference(self, key: str) -> HyperbolicModelEntity:
        return self._points[key]

    def point_value(self, key: str) -> HyperbolicModelEntity:
        return copy(self._points[key])

    def underlying_point_coordinates(self, key: str) -> HyperbolicModelEntity:
        """
        :return: the point coordinate as stored, with no transform applied to it
        """

        return self._points[key]
