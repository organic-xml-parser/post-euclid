import typing

import pyglet
from pyglet.window.key import MOTION_LEFT, MOTION_RIGHT, MOTION_UP, MOTION_DOWN

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities
from post_euclid.hyperbolic_2d.poincare.poincare import PoincareHyperbolicModel
from post_euclid.hyperbolic_2d.scene import Scene, SceneLineSegment
from post_euclid.hyperbolic_2d.weierstrass.weierstrass import WeierstrassHyperbolicModel
from post_euclid.rendering.canvas import Canvas


def main():
    model = WeierstrassHyperbolicModel()
    scene = Scene(model)

    points: typing.List[str] = [
        scene.create_point_reference(),
        scene.create_point_reference()]

    (scene.point_reference(points[0])
     .apply_transform(model.get_transform_tool().create_translation_like(0.5, 0)))

    (scene.point_reference(points[1])
     .apply_transform(model.get_transform_tool().create_translation_like(0, 0.5)))

    scene.add_scene_item(SceneLineSegment(*points))

    window = pyglet.window.Window(
        caption="PostEuclid",
        width=800,
        height=800)

    @window.event
    def on_text_motion(motion):
        step = 0.01
        if motion == MOTION_LEFT:
            scene.rotate(-step* 10)

        if motion == MOTION_RIGHT:
            scene.rotate(+step* 10)

        if motion == MOTION_UP:
            scene.translate(0, +step)

        if motion == MOTION_DOWN:
            scene.translate(0, -step)

    @window.event
    def on_key_press(key, modifier):
        if key == pyglet.window.key.SPACE:
            points.append(scene.create_point_reference())

            scene.add_scene_item(SceneLineSegment(points[-2], points[-1]))

    canvas = Canvas(window)

    @window.event
    def on_draw():
        window.clear()
        canvas.update(window)
        canvas.scale *= 1
        # draw the unit circle
        canvas.draw_circle(
            euclidean_2d.entities.Circle(
                center=euclidean_2d.entities.Point(0, 0),
                radius=1
            ),
            color=(50, 50, 50)).draw()

        for renderable in scene.get_renderable_entities():
            canvas.draw(renderable).draw()

    pyglet.app.run()


if __name__ == '__main__':
    main()
