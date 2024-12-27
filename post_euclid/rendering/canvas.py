import math

import pyglet

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities


class Canvas:

    def __init__(self, window):
        self.scale = 0.0
        self._origin = 0.0, 0.0
        self.update(window)

        self._draw_function_map = {
            euclidean_2d.entities.Point: self.draw_point,
            euclidean_2d.entities.Circle: self.draw_circle,
            euclidean_2d.entities.CircleArc: self.draw_circle_arc,
            euclidean_2d.entities.LineSegment: self.draw_line_segment,
            euclidean_2d.entities.Line: self.draw_line
        }

    def draw(self, euclidean_entity: euclidean_2d.entities.Euclidean2D, *args, **kwargs):
        if euclidean_entity.__class__ in self._draw_function_map:
            return self._draw_function_map[euclidean_entity.__class__](euclidean_entity, *args, **kwargs)
        else:
            for k, v in self._draw_function_map.items():
                if isinstance(euclidean_entity, k):
                    return v(euclidean_entity, *args, **kwargs)

        raise ValueError("No draw function for entity")

    def draw_line(self, line: euclidean_2d.entities.Line, *args, **kwargs):
        x0 = -1 * self.scale
        x1 = 1 * self.scale

        y0 = -1 * self.scale
        y1 = 1 * self.scale

        if line.delta.x == 0:
            # draw vertical line
            line_bottom = self._to_render_coords(line.origin.x, y1)
            line_top = self._to_render_coords(line.origin.x, -y0)
            pyglet.shapes.Line(*line_bottom, *line_top, *args, **kwargs).draw()
        elif line.delta.y == 0:
            # draw horizontal line
            line_left = self._to_render_coords(x0, line.origin.y)
            line_right = self._to_render_coords(x1, line.origin.y)
            pyglet.shapes.Line(*line_left, *line_right, *args, **kwargs).draw()
        else:
            dy_dx = line.delta.y / line.delta.x
            dx0 = x0 - line.origin.x
            dx1 = x1 - line.origin.x

            line_0 = self._to_render_coords(x0, line.origin.y + dy_dx * dx0)
            line_1 = self._to_render_coords(x1, line.origin.y + dy_dx * dx1)
            return pyglet.shapes.Line(*line_0, *line_1, *args, **kwargs)

    def draw_circle(self, circle: euclidean_2d.entities.Circle, *args, **kwargs):
        return pyglet.shapes.Circle(*self._to_render_coords(*circle.center),
                             circle.radius * self.scale,
                             *args,
                             **kwargs)

    def draw_circle_arc(self,
                 circle_arc: euclidean_2d.entities.CircleArc,
                 *args,
                 **kwargs):

        a0 = circle_arc.angle_0
        delta_angle = circle_arc.angle_1 - circle_arc.angle_0

        return pyglet.shapes.Arc(*self._to_render_coords(*circle_arc.circle.center),
                          circle_arc.circle.radius * self.scale,
                          *args,
                          **kwargs,
                          start_angle=a0,
                          angle=delta_angle,
                          closed=False,
                          segments=8)

    def draw_line_segment(self, line_segment: euclidean_2d.entities.LineSegment, *args, **kwargs):
        return pyglet.shapes.Line(
            *self._to_render_coords(*line_segment.p0),
            *self._to_render_coords(*line_segment.p1),
            *args,
            **kwargs)

    def update(self, window):
        self.scale = min(window.width, window.height) * 0.5 - 5
        self._origin = window.width / 2, window.height / 2

    def draw_point(self, point: euclidean_2d.entities.Point, *args, **kwargs):
        return pyglet.shapes.Circle(*self._to_render_coords(*point),
                              radius=5,
                              color=(50, 50, 0),
                              *args,
                              **kwargs)

    def _to_render_coords(self, x: float, y: float):
        return (-x * self.scale + self._origin[0],
                -y * self.scale + self._origin[1])
