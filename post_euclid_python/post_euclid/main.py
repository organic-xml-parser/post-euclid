import time
import typing

import pyglet
from pyglet.window.key import MOTION_LEFT, MOTION_RIGHT, MOTION_UP, MOTION_DOWN

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities
from post_euclid.hyperbolic_2d.poincare.poincare import PoincareHyperbolicModel
from post_euclid.hyperbolic_2d.scene import Scene, SceneLineSegment
from post_euclid.hyperbolic_2d.tiling import Tiling_3_7
from post_euclid.hyperbolic_2d.weierstrass.weierstrass import WeierstrassHyperbolicModel
from post_euclid.rendering.canvas import Canvas


def main():
    model = PoincareHyperbolicModel()
    scene = Scene(model)

    Tiling_3_7(scene).generate()

    window = pyglet.window.Window(
        caption="PostEuclid",
        width=800,
        height=800)

    @window.event
    def on_text_motion(motion):
        step = 0.01
        if motion == MOTION_LEFT:
            scene.rotate(-step * 10)

        if motion == MOTION_RIGHT:
            scene.rotate(+step * 10)

        if motion == MOTION_UP:
            scene.translate(0, +step * 10)

        if motion == MOTION_DOWN:
            scene.translate(0, -step * 10)

    canvas = Canvas(window)

    @window.event
    def on_draw():
        window.clear()
        canvas.update(window)
        canvas.scale *= 1

        batch = pyglet.graphics.Batch()

        #print(time.time() - timestamp)

        # draw the unit circle
        elements = [
            canvas.draw_circle(
                euclidean_2d.entities.Circle(
                    center=euclidean_2d.entities.Point(0, 0),
                    radius=1
                ),
                color=(50, 50, 50),
            batch=batch)
        ]
        #print(len(renderable_entities))
        timestamp = time.time()
        for renderable in scene.get_renderable_entities():
            elements.append(canvas.draw(renderable, batch=batch))

        print(time.time() - timestamp)
        batch.draw()

    pyglet.app.run()


if __name__ == '__main__':
    main()
