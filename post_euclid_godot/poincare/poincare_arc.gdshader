shader_type canvas_item;

/*
uniform vec2 mb_a = vec2(1, 0);
uniform vec2 mb_b = vec2(0, 0);
uniform vec2 mb_c = vec2(0, 0);
uniform vec2 mb_d = vec2(1, 0);
*/


/*
// arc coordinates on the poincare disk
uniform vec2 p0 = vec2(0.5, 0);
uniform vec2 p1 = vec2(0.6, 0.1);
*/

const int BATCH_SIZE=24;

// arc coordinates, transformed according to mobius transform
uniform int elements_in_batch = 0;
uniform float[BATCH_SIZE] line_thickness;
uniform vec3[BATCH_SIZE] base_color;
uniform vec2[BATCH_SIZE] local_p0;
uniform vec2[BATCH_SIZE] local_p1;
uniform int[BATCH_SIZE] edgeIndices;
uniform float timeOffset;

// line thickness

/*
Properties of the arc used to draw the edge
Radius < 0 indicates straight line segment.
*/
varying flat vec2[BATCH_SIZE] origin;
varying flat float[BATCH_SIZE] radius;


vec2 uv_to_poincare(vec2 input) {
	return (input * 2.0f) + vec2(-1, -1);
}

/*

vec2 complex_mult(vec2 a, vec2 b) {
    return vec2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x);
}

vec2 complex_conj(vec2 a) {
    return vec2(a.x, -a.y);
}

vec2 complex_div(vec2 a, vec2 b) {
    float factor = 1.0 / (b.x * b.x + b.y * b.y);

    return complex_mult(a, complex_conj(b)) * factor;
}

vec2 apply_mobius_trsf(vec2 point) {
    vec2 p_new = complex_div(
        complex_mult(mb_a, point) + mb_b,
        complex_mult(mb_c, point) + mb_d
    );

    return p_new;
}


float normalize_angle(float angle) {
	float result = angle;
	if (result < 0.0) {
		result = 2.0 * PI + result;
	}

	return mod(result + 2.0 * PI, 2.0 * PI);
}
*/

bool are_points_clockwise(vec2 a, vec2 b, vec2 c) {
	// sum (x2 - x1) * (y2 - y1) for edges
	vec2 e0 = b - a;
	vec2 e1 = c - b;
	vec2 e2 = a - c;

	return (b.x - a.x) * (b.y + a.y) +
		(c.x - b.x) * (c.y + b.y) +
		(a.x - c.x) * (a.y + c.y) > 0.0;
}

/*
float _intensity_as_line_segment(vec2 uv) {
	vec2 line_vec = p1 - p0;
	vec2 p0_to_UV = uv - p0;

	float line_length = length(line_vec);

	line_vec = normalize(line_vec);

	float dl = dot(line_vec, p0_to_UV);

	if (dl < 0.0 || dl > line_length) {
		float p0_dist = length(p0 - uv);
		float p1_dist = length(p1 - uv);
		return clamp(
			1.0 - min(p0_dist, p1_dist) / line_thickness,
			0.0,
			1.0
		);
	}

	//return max(0.0, min(1.0, 1.0 - abs(dl) / line_thickness));
	vec2 line_point = p0 + (normalize(line_vec) * dl);

	if (length(line_point - uv) < line_thickness) {
		return 1.0 - length(line_point - uv) / line_thickness;
	}

	return 0.0f;


	if (dl < 0.0f || dl > line_length) {
		// point does not lie on the line
		return 0.0f;
	}


	float point_dist = distance(p0, line_point);
	if (point_dist > line_thickness) {
		return 0.0f;
	} else {
		return point_dist / line_thickness;
	}
}
*/

bool _is_colinear_with_origin(int i) {
    float dy = local_p1[i].y - local_p0[i].y;
    float dx = local_p1[i].x - local_p0[i].x;

    if (dy == 0.0f) {
        // horizontal line
        return local_p0[i].x == 0.0f;
    } else if (dx == 0.0f) {
        return local_p0[i].y == 0.0f;
    }

    float dy_dx = dy / dx;

    return local_p0[i].x * dy_dx == local_p0[i].y;
}

float _distance_from_straight_line_segment(vec2 uv, int i) {
    vec2 delta_p = local_p1[i] - local_p0[i];
    vec2 delta_uv = uv - local_p0[i];

    float dot_prod = dot(uv, delta_p);
    vec2 closest_point = local_p0[i] + normalize(delta_uv) * dot_prod;

    return length(uv - closest_point);
}

vec2 _circle_invert(vec2 xy) {
    float invert_rad = 1.0f / length(xy);

    return normalize(xy) * invert_rad;
}

/*
void _circle_from_3_points(vec2 a, vec2 b, vec2 c, out vec2 origin, out float radius) {
    // find midpoint line from a -> b
    vec2 ab = b - a;
    vec2 ab_line_origin = a + 0.5 * ab;
    vec2 ab_line_dir = normalize(vec2(-ab.y, ab.x));

    vec2 ac = c - a;
    vec2 ac_line_origin = a + 0.5 * ac;
    vec2 ac_line_dir = normalize(vec2(-ac.y, ac.x));

    vec2 o0 = ab_line_origin;
    vec2 d0 = ab_line_dir;
    vec2 o1 = ac_line_origin;
    vec2 d1 = ac_line_dir;

    float alpha = (d1.x * o0.y - o1.y * d1.x) / (d0.x * d1.y - d0.y * d1.x);

    origin = o0 + alpha * d0;
    radius = length(origin - a);
}

*/

// returns -1 if distance is effectively infinite
// (i.e. uv is outside disk, so circle segment should be ignored)
float _distance_from_poincare_circle_arc(vec2 uv, int i) {

    bool is_cw = are_points_clockwise(origin[i], local_p0[i], local_p1[i]);
	bool a_is_cw = are_points_clockwise(origin[i], local_p0[i], uv);
	bool b_is_cw = are_points_clockwise(origin[i], uv, local_p1[i]);

	if (is_cw) {
		if (!are_points_clockwise(origin[i], local_p0[i], uv)) {
			return -1.0f;
		}

		if (!are_points_clockwise(origin[i], uv, local_p1[i])) {
			return -1.0f;
		}
	} else {
		if (are_points_clockwise(origin[i], local_p0[i], uv)) {
			return -1.0f;
		}

		if (are_points_clockwise(origin[i], uv, local_p1[i])) {
			return -1.0f;
		}
	}

	return abs(length(uv - origin[i]) - radius[i]);
}

float _calculate_intensity(vec2 uv, float adjusted_line_thickness, int i) {

    float dl = 0.0f;
    if (radius[i] < 0.0) {
        dl = 10.0;
    } else {
        dl = _distance_from_poincare_circle_arc(uv, i);
    }

    if (dl < 0.0f) {
        return 0.0f;
    }

	return clamp(1.0 - dl / adjusted_line_thickness, 0.0, 1.0);
}

void vertex() {

    for (int i = 0; i < elements_in_batch; i++) {

        vec2 span = local_p1[i] - local_p0[i];

        // approximate with straight line if object is small
        if (_is_colinear_with_origin(i) || dot(span, span) < 0.001) {
            radius[i] = -1.0;
        } else {
            // https://math.stackexchange.com/questions/1503466/algebraic-solutions-for-poincar%C3%A9-disk-arcs

            float dx = local_p1[i].x - local_p0[i].x;
            float dy = local_p1[i].y - local_p0[i].y;

           	float px = local_p0[i].x;
           	float py = local_p0[i].y;

           	float qx = local_p1[i].x;
           	float qy = local_p1[i].y;

           	float px2 = px * px;
           	float py2 = py * py;

           	float qx2 = qx * qx;
           	float qy2 = qy * qy;

           	float u = (px2 + py2 + 1.0);
           	float v = (qx2 + qy2 + 1.0);

           	float denom = 2.0 * (px * qy - py * qx);

           	float ox;
           	float oy;

            if (dx == 0.0f) {
                oy = 0.0f;
            } else {
                oy = (-qx * u + px * v) / denom;
            }

            if (dy == 0.0f) {
                ox = 0.0f;
            } else {
                ox = (qy * u - py * v) / denom;
            }

           	origin[i] = vec2(ox, oy);
           	radius[i] = length(local_p0[i] - origin[i]);
        }
    }
}

vec4 _fragment(int i, vec2 uv_poincare) {

    // for a circle centered on p0 -> p1, if the uv is outside that
    // circle, it should not be rendered.
    vec2 local_span = (local_p1[i] - local_p0[i]);
    vec2 local_cen = local_p0[i] + 0.5 * local_span;
    vec2 local_cen_to_uv = uv_poincare - local_cen;

    // point is outside circle or outside circle containing the two points
    if (dot(uv_poincare, uv_poincare) >= 1.0f ||
        dot(local_cen_to_uv, local_cen_to_uv) > dot(local_span, local_span) * 0.25) {
        return vec4(base_color[i], 0.0f);
    }

    float adjusted_line_thickness = line_thickness[i] * (1.0 - length(uv_poincare));

    float intensity = _calculate_intensity(uv_poincare, adjusted_line_thickness, i);
    //float p0_intensity = 1.0 - length(local_p0 - uv_poincare) / adjusted_line_thickness;
    //float p1_intensity = 1.0 - length(local_p1 - uv_poincare) / adjusted_line_thickness;

    //intensity = max(intensity, p0_intensity);
    //intensity = max(intensity, p1_intensity);

    //if (intensity > 0.0) {
    //    intensity = 1.0;
    //}

    //intensity = intensity * (0.5 + 0.5 * (sin(timeOffset * 2.0 + float(edgeIndices[i])) + 1.0));

    vec4 color = vec4(base_color[i], intensity);

    color[0] *= (0.5 + 0.25 * (sin(timeOffset * 2.0 + float(edgeIndices[i]) * timeOffset * 0.002) + 1.0));
    color[1] *= (0.5 + 0.25 * (cos(timeOffset * 4.0 + float(edgeIndices[i]) * timeOffset * 0.001) + 1.0));
    color[2] *= (0.5 + 0.25 * (sin(timeOffset * 8.0 + float(edgeIndices[i]) * timeOffset * 0.03) + 1.0));

    return color;
}


void fragment() {
    // Called for every pixel the material is visible on.
	vec2 uv_poincare = uv_to_poincare(UV);

    COLOR = vec4(0.0, 0.0, 0.0, 0.0);

    for (int i = 0; i < elements_in_batch; i++) {
        vec4 result = _fragment(i, uv_poincare);
        if (result[3] > 0.0) {
            COLOR = result;
            break;
        }
    }

}


//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
