from __future__ import annotations

import typing
from typing import TypeVar, Generic

from post_euclid.euclidean_2d.entities import Euclidean2D


T = TypeVar("T")


class HyperbolicModelTransformTool(Generic[T]):

    def create_identity(self) -> T:
        raise NotImplementedError()

    def create_translation_like(self, params: typing.Any) -> T:
        """
        Generate a translation transform according to the supplied parameters.
        The exact parameter values are implementation-dependent
        """
        raise NotImplementedError()

    def create_rotation_like(self, params: typing.Any) -> T:
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


class HyperbolicModelEntity(Generic[T]):
    """
    Base class for all model systems.
    """
    def get_transform_tool(self) -> HyperbolicModelTransformTool[T]:
        """
        :return: this system's utility class for managing and composing mobius transforms (gyrovector operations)
        """
        pass

    def get_euclidean_representation(self) -> Euclidean2D:
        """
        Consistent euclidean representation. E.g. for poincare disk model this would be the
        x,y coordinate point for a Point-like entity, or the circle arc representing a line segment.
        """

        raise NotImplementedError()

    def apply_transform(self, model_transfrom: T) -> HyperbolicModelEntity[T]:
        """
        :return: a copy of the entity with the specified transform applied.
        """
        raise NotImplementedError()

