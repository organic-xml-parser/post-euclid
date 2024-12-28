import pyglet
from pyglet.window.key import MOTION_LEFT, MOTION_RIGHT, MOTION_UP, MOTION_DOWN

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities
from post_euclid.hyperbolic_2d.poincare_tiling import PoincareTiling
from post_euclid.rendering.canvas import Canvas
from post_euclid.hyperbolic_2d.coordinate import Axial
from post_euclid.hyperbolic_2d.poincare_scene import PoincareScene, PoincareSceneLineSegment, PoincareScenePoint


def main():

    scene = PoincareScene()

    PoincareTiling(5, 4, radius=0.3).generate(scene, 4)

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
            points.append(scene.create_point_reference(0, 0))

            scene.add_scene_item(PoincareSceneLineSegment(points[-2], points[-1]))


    canvas = Canvas(window)

    @window.event
    def on_draw():
        window.clear()
        canvas.update(window)
        canvas.scale *= 1
        batch = pyglet.graphics.Batch()
        # draw the unit circle
        entities = []
        entities.append(canvas.draw_circle(
            euclidean_2d.entities.Circle(
                center=euclidean_2d.entities.Point(0, 0),
                radius=1
            ),
            color=(50, 50, 50),
            batch=batch))

        for si, renderable in scene.get_renderable_entities().items():
            entities.append(canvas.draw(renderable, batch=batch))

        batch.draw()

    pyglet.app.run()


if __name__ == '__main__':
    main()
