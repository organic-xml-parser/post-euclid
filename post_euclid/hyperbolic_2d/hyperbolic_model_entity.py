from __future__ import annotations

import typing
from typing import TypeVar, Generic

from post_euclid.euclidean_2d.entities import Euclidean2D


T = TypeVar("T")


class HyperbolicModelTransformTool(Generic[T]):

    def create_identity(self) -> T:
        raise NotImplementedError()

    def create_translation_like(self, dx: float, dy: float) -> T:
        """
        Generate a translation transform according to the supplied parameters.
        The exact parameter values are implementation-dependent
        """
        raise NotImplementedError()

    def create_rotation_like(self, angle: float) -> T:
        """
        As w. create_translation_like, produce a Transform analogous to a rotation.
        """
        raise NotImplementedError()

    def gyro_mult(self, left: T, right: T) -> T:
        """
        Represents mobius transfroms analogous to a transformation matrix in a 2d Euclidean system.
        """
        raise NotImplementedError()

    def get_inverse(self, trsf: T) -> T:
        """
        :return the inverse of the specified transform, such that gyro_mult(Tinv, T) == identity
        Note that inverse may not always be calculable. In this case exceptions may be thrown.
        """
        raise NotImplementedError()


T_Point = TypeVar("T_Point")
T_Line = TypeVar("T_Line")


class HyperbolicModelEntity(Generic[T]):

    def get_euclidean_representation(self) -> Euclidean2D:
        """
        Consistent euclidean representation. E.g. for poincare disk model this would be the
        x,y coordinate point for a Point-like entity, or the circle arc representing a line segment.
        """

        raise NotImplementedError()

    def apply_transform(self, model_transfrom: T):
        """
        Apply the transform to the underlying representation.
        """
        raise NotImplementedError()


class HyperbolicModelEntityFactory(Generic[T, T_Point, T_Line]):

    def create_point(self) -> T_Point:
        raise NotImplementedError()

    def create_line_segment(self, p0: T_Point, p1: T_Point) -> T_Line:
        raise NotImplementedError()


class HyperbolicModel(Generic[T, T_Point, T_Line]):
    """
    Base class for all model systems.
    """

    def get_transform_tool(self) -> HyperbolicModelTransformTool[T]:
        """
        :return: this system's utility class for managing and composing mobius transforms (gyrovector operations)
        """
        raise NotImplementedError()

    def get_factory(self) -> HyperbolicModelEntityFactory[T]:
        """
        :return: this system's utility class for creating new entities
        """
        raise NotImplementedError()
