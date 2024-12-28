import typing

import numpy

from post_euclid.hyperbolic_2d.poincare import PoincarePoint
from post_euclid.hyperbolic_2d.poincare_scene import PoincareScene, PoincareSceneItem, PoincareSceneLineSegment


class PoincareTiling:

    def __init__(self, n_sides: int, n_per_vertex: int, radius: float, rounding_precision: int = 20):
        self._n_sides = n_sides
        self._n_per_vertex = n_per_vertex
        self._rounding_precision = rounding_precision
        self._radius = radius

        self._angle = 2 * numpy.pi / self._n_sides

    def _rounded_point(self, x, y) -> typing.Tuple[float, float]:
        return (
            round(x, self._rounding_precision),
            round(y, self._rounding_precision)
        )

    def _create_point(self, x, y,
                      generated_points: typing.Dict[typing.Tuple[float, float], PoincarePoint],
                      generated_lines: typing.Set[typing.Tuple[str, str]],
                      scene: PoincareScene) -> str:
        underlying_xy = scene.underlying_coordinate(x, y)
        rounded = self._rounded_point(*underlying_xy)

        if rounded in generated_points:
            return generated_points[rounded]
        else:
            return scene.create_point_reference(x, y)

    def generate(self, scene: PoincareScene, depth: int):
        generated_points = dict()
        generated_lines = set()

        self._generate(
            scene,
            generated_points,
            generated_lines,
            depth)

    def _generate(self,
                  scene: PoincareScene,
                  generated_points: typing.Dict[typing.Tuple[float, float], str],
                  generated_lines: typing.Set[typing.Tuple[str, str]],
                  depth: int):
        if depth == 0:
            return

        points: typing.List[str] = []

        for i in range(0, self._n_sides):
            x = self._radius * numpy.cos(i * self._angle)
            y = self._radius * numpy.sin(i * self._angle)

            underlying_xy = scene.underlying_coordinate(x, y)
            rounded = self._rounded_point(*underlying_xy)

            if rounded in generated_points:
                point = generated_points[rounded]
            else:
                point = scene.create_point_reference(x, y)
                generated_points[rounded] = point

                scene.translate(x, y)
                scene.translate(x, y)
                scene.rotate(self._angle / 2)
                self._generate(scene, generated_points, generated_lines, depth - 1)
                scene.rotate(-self._angle / 2)
                scene.translate(-x, -y)
                scene.translate(-x, -y)

            points.append(point)

        # add edges for subsquent points
        for i in range(0, len(points)):
            p0 = points[i]
            p1 = points[(i + 1) % len(points)]

            if (p0, p1) in generated_lines or (p1, p0) in generated_lines:
                continue

            scene.add_scene_item(PoincareSceneLineSegment(p0, p1))
