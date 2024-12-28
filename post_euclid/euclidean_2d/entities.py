from __future__ import annotations

import typing
from dataclasses import dataclass

import numpy


class Euclidean2D:
    pass


@dataclass
class Point(Euclidean2D):
    x: float
    y: float

    @property
    def normalized(self) -> Point:
        if self.is_origin:
            raise ValueError("Cannot normalize origin point")

        mag = self.mag
        return Point(self.x / mag, self.y / mag)

    @property
    def mag(self):
        return numpy.hypot(self.x, self.y)

    @property
    def mag_sq(self):
        return self.x * self.x + self.y * self.y

    @property
    def is_origin(self):
        return self.x == 0 and self.y == 0

    def cross(self, other: Point) -> float:
        return self.x * other.y - self.y * other.x

    def dot(self, other: Point) -> float:
        return self.x * other.x + self.y * other.y

    def scaled(self, amount: float):
        return Point(self.x * amount, self.y * amount)

    def __add__(self, other):
        return Point(self.x + other.x, self.y + other.y)

    def __sub__(self, other):
        return Point(self.x - other.x, self.y - other.y)

    def __iter__(self):
        return iter((self.x, self.y))

    def __eq__(self, other: Point):
        return isinstance(other, Point) and (self.x, self.y) == (other.x, other.y)

    def __hash__(self):
        return hash((self.x, self.y))


@dataclass
class LineSegment(Euclidean2D):
    p0: Point
    p1: Point


@dataclass
class Circle(Euclidean2D):
    center: Point
    radius: float

    @property
    def arc(self) -> CircleArc:
        return CircleArc(self, 0, 2 * numpy.pi)

    @staticmethod
    def unit_circle():
        return Circle(Point(0, 0), 1)


@dataclass
class CircleArc(Euclidean2D):
    circle: Circle
    angle_0: float
    angle_1: float


def midpoint(p0: Point, p1: Point) -> Point:
    dx = p1.x - p0.x
    dy = p1.y - p0.y

    return Point(p0.x + dx * 0.5, p0.y + dy * 0.5)


def collinear(p0: Point, p1: Point, p2: Point):
    a = p1 - p0
    b = p2 - p1

    #print("Collinearity test: ", p0, p1, p2, a, b)
    #print(a.cross(b))

    return a.cross(b) == 0


def normalize_angle(angle: float) -> float:

    if angle < -numpy.pi or angle > numpy.pi:
        raise ValueError("Angle is outside normalizable range")

    #angle = numpy.fmod(angle, numpy.pi)

    if angle >= 0:
        result = angle
    else:
        result = 2 * numpy.pi + angle

    result = numpy.fmod(result + 2 * numpy.pi, 2 * numpy.pi)

    if result < 0:
        raise ValueError()

    return result


class Line(Euclidean2D):

    def __init__(self, origin: Point, delta: Point):
        delta_mag = numpy.hypot(*delta)

        if delta_mag == 0:
            raise ValueError("Cannot construct a line segment with zero dx, dy")

        self.origin = origin
        self.delta = Point(delta.x / delta_mag, delta.y / delta_mag)

    def closest_point(self, point: Point):
        dp = point - self.origin
        dist = dp.dot(point)
        return self.origin + self.delta.scale(dist)

    @property
    def reversed(self):
        return Line(self.origin, Point(-self.delta.x, -self.delta.y))

    def parallel(self, other: Line):
        return self.delta == other.delta or self.delta == other.reversed.delta

    @staticmethod
    def from_points(p0: Point, p1: Point):
        return Line(
            p0,
            Point(
                p1.x - p0.x,
                p1.y - p0.y
            )
        )


class LineIntersection:

    @staticmethod
    def get_intersection(line0: Line, line1: Line) -> typing.Optional[Point]:
        if line0.delta == line1.delta:
            raise ValueError("lines are parallel")

        p0x = line0.origin.x
        p0y = line0.origin.y

        p0Dx = line0.delta.x
        p0Dy = line0.delta.y

        p1x = line1.origin.x
        p1y = line1.origin.y

        p1Dx = line1.delta.x
        p1Dy = line1.delta.y

        denom = (p1Dx * p0Dy - p1Dy * p0Dx)
        if denom == 0:
            # sometimes lines are not exactly parallel, but due to floating point arithmetic they are "close enough"
            # that a solution cannot be found
            return None

        b = p0Dx * (p1y - p0y) + p0Dy * (p0x - p1x)
        b /= denom

        return Point(p1x + p1Dx * b, p1y + p1Dy * b)
