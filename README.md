# Post-Euclid

Messing about with rendering non-euclidean geometry.

## Resources
http://math.geometryof.org/MNEG/MNEG.html
https://www.researchgate.net/publication/268710591_Gyrovector_spaces_and_their_differential_geometry
https://www.youtube.com/c/CodeParade
https://www.youtube.com/@ZenoRogue
https://martinlusblog.wordpress.com/2020/07/25/on-simulating-and-understanding-hyperbolic-space-with-no-previous-knowledge-on-the-subject/
https://math.stackexchange.com/questions/4486960/translating-points-on-the-poincar%C3%A9-disk

## Notes:
(Mostly for myself, but may be useful to others)

Fundamentally to deal with non-euclidean geometry on a computer we must 
interface with it via Euclidean geometry. This is typically accomplished by
projecting the hyperbolic space into the standard 2D space we are used to.

Note that this even applies to how we store the coordinate system (I think)
before any rendering takes place. As in Euclidean 2D space there are two
numbers needed to uniquely identify a point, and grid based systems fit uniformly 
onto the plane. Axial coordinates exist, but are not 
well-behaved as in Euclidean Geometry. A similar problem arises in Spherical 
Geometry: if you use polar coordinates then the coordinates towards the 
"poles" will spatially become closer together. In the land of pure maths 
where all numbers are infinitely divisible this is fine, but computers
using finite precision arithmetic will run into problems at these extremes.

For the poincare disk model, we represent all of hyperbolic space in a finite
region: the unit circle (excluding edges, sometimes referred to as "open" - 
as opposed to closed, where the points lying on the edges also belong to the
region). It is important to remember that the poincare disk lives in plain 2d
Euclidean space, so if we define a Point(x, y) in memory it will refer to a
coordinate somewhere in that region such that `x * x + y * y < 1`.

As far as I know the only way to represent hyperbolic coordinates are using
a projection into a more well-behaved geometry along with the coordinates in
that projection. There aren't any "pure" hyperbolic coordinates, but we can
translate between different projections, for example between 
Poincare/Beltrami Klien model. So a "pure" coordinate could be thought of
as the name of the projection + parameter values.

Of course, in cramming an infinite space into the unit circle - and indeed 
with any projection into Euclidean space - there is always a breaking of
some property: e.g. lines not appearing as parallel or straight/angles not 
being preserved.

The distortions which are inevitably introduced via such projections also
change how the operations we typically consider as translation/rotation 
affect the points in our model. For one thing, if we want to "translate"
a point in our model, it should always stay inside the disk no matter the
amount we move it (it mostly goes over my head, but the above paper
applies this to relativity in keeping velocity values below _c_).

The paper and Code Parade video go over the concept of gyrovectors, 
operations on which are more general versions of things we think of
as transformations (e.g. translation, or movement).

Again, all of this is happening in Euclidean space: we just take our disk
and the things on it, apply a transformation, and the result is a disk which 
is altered to represent the hyperbolic coordinates after the transform is
applied. Because of this, the different projections all have different 
operators that preserve their individual constraints.

Another thing to note, that confused me initially is that there does not 
appear to be a "special" point in hyperbolic space. The various projections,
in particular the crochet/saddle give the illusion of a magical "origin",
but this is just as arbitrary as an origin in the Euclidean geometry, 
or poles in spherical, and "translations" can be applied to move it about.

### Precision

Due to the intense distortion caused by the Poincare disk model it is pretty
easy to run into floating point precision issues: far away objects are squished
towards the edge of the circle. If their coordinates are "saved" in this position
and then the reverse transformation applied to bring them back to the center, they
can quite easily shift.

One mitigation is to use high floating point precision values, which for limited
areas will keep the resulting distortion unnoticable.
