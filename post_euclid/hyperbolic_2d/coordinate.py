from __future__ import annotations

import math

import numpy

from post_euclid.hyperbolic_2d.poincare import PoincarePoint


class Polar:

    def __init__(self, r: float, theta: float):
        self.r = r
        self.theta = theta


    def __add__(self, other: Polar):
        # distance between two points: r1, given by:
        r = numpy.arccosh(
            numpy.cosh(self.r) * numpy.cosh(other.r) -
            numpy.sinh(self.r) * numpy.sinh(other.r) * numpy.cos(other.theta - self.theta)
        )


    def as_hyperbolic_axial(self):
        return Axial(
            numpy.arctanh(numpy.tanh(self.r) * numpy.cos(self.theta)),
            numpy.arctanh(numpy.tanh(self.r) * numpy.sin(self.theta))
        )


class Axial:
    """
    x axis through the origin
    y axis constructed perpendicular to x
    """

    def __init__(self, x: float, y: float):
        self.x: float = x
        self.y: float = y

    def __sub__(self, other: Axial):
        return Axial(self.x - other.x, self.y - other.y)

    def __add__(self, other: Axial):
        return Axial(self.x + other.x, self.y + other.y)

    def is_valid(self) -> bool:
        return math.tanh(self.x) ** 2 + math.tanh(self.y) ** 2 <= 1

    def as_hyperbolic_polar(self) -> Polar:
        v = numpy.hypot(numpy.tanh(self.x) ** 2, numpy.tanh(self.y) ** 2)

        r = numpy.arctanh(v)

        theta = 2 * numpy.arctan(numpy.tanh(self.y) / (
            numpy.tanh(self.x) + v
        ))

        return Polar(r, theta)

    def as_hyperbolic_beltrami(self) -> Beltrami:
        return Beltrami(
            numpy.tanh(self.x), numpy.tanh(self.y)
        )


class Beltrami:

    def __init__(self, x: float, y: float):
        self.x = x
        self.y = y

        if numpy.hypot(x, y) > 1:
            raise ValueError("Beltrami model coordinates should be inside unit circle")

    def as_hyperbolic_poincare(self):
        xx = self.x * self.x
        yy = self.y * self.y

        return PoincarePoint(
            self.x / (1 + numpy.sqrt(1 - xx - yy)),
            self.y / (1 + numpy.sqrt(1 - xx - yy))
        )
