import pyglet
from pyglet.window.key import MOTION_LEFT, MOTION_RIGHT, MOTION_UP, MOTION_DOWN

from post_euclid import euclidean_2d
from post_euclid.euclidean_2d import entities
from post_euclid.rendering.canvas import Canvas
from post_euclid.hyperbolic_2d.coordinate import Axial
from post_euclid.hyperbolic_2d.poincare_scene import PoincareScene, PoincareSceneLineSegment, PoincareScenePoint


def main():
    window = pyglet.window.Window(
        caption="PostEuclid",
        width=800,
        height=800)

    scene = PoincareScene()
    points = []
    pt_count = 5
    for x in range(-pt_count, pt_count + 1):
        points.append([])
        for y in range(-pt_count, pt_count + 1):
            try:
                pt = Axial(x / pt_count, y / pt_count).as_hyperbolic_beltrami().as_hyperbolic_poincare()
                points[-1].append(scene.create_point_reference(pt.x, pt.y))
            except:
                pass

    for i in range(0, len(points)):
        for j in range(0, len(points[i]) - 1):
            p0 = points[i][j]
            p1 = points[i][j + 1]

            #if i == 2 and j == 5:
            scene.add_scene_item(PoincareSceneLineSegment(p0, p1))

            if i < len(points) - 1 and len(points[i + 1]) > j:
                scene.add_scene_item(PoincareSceneLineSegment(p0, points[i + 1][j]))

    for pp in points:
        for p in pp:
            scene.add_scene_item(PoincareScenePoint(p))

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
