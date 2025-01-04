shader_type canvas_item;

uniform vec2 mb_a = vec2(1, 0);
uniform vec2 mb_b = vec2(0, 0);
uniform vec2 mb_c = vec2(0, 0);
uniform vec2 mb_d = vec2(1, 0);

uniform vec3 base_color = vec3(1.0, 0.5, 0.5);

// arc coordinates on the poincare disk
uniform vec2 p0 = vec2(0.5, 0);
uniform vec2 p1 = vec2(0.6, 0.1);

// line thickness
uniform float line_thickness = 0.5f;

vec2 uv_to_poincare(vec2 input) {
	return (input * 2.0f) + vec2(-1, -1);
}

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

bool are_points_clockwise(vec2 a, vec2 b, vec2 c) {
	// sum (x2 - x1) * (y2 - y1) for edges
	vec2 e0 = b - a;
	vec2 e1 = c - b;
	vec2 e2 = a - c;

	return (b.x - a.x) * (b.y + a.y) +
		(c.x - b.x) * (c.y + b.y) +
		(a.x - c.x) * (a.y + c.y) > 0.0;
}

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


float _intensity_as_circle_arc(vec2 uv, vec2 local_p0, vec2 local_p1) {

	float dp0 = length(p0 - uv);
	float dp1 = length(p1 - uv);

	float px = local_p0.x;
	float py = local_p0.y;

	float qx = local_p1.x;
	float qy = local_p1.y;

	float px2 = px * px;
	float py2 = py * py;

	float qx2 = qx * qx;
	float qy2 = qy * qy;

	float u = (px2 + py2 + 1.0);
	float v = (qx2 + qy2 + 1.0);

	float denom = 2.0 * (px * qy - py * qx);

	float ox = (qy * u - py * v) / denom;
	float oy = (-qx * u + px * v) / denom;

	vec2 origin = vec2(ox, oy);
	float radius = length(local_p0 - origin);

	float dl = abs(length(uv - origin) - radius);

	bool is_cw = are_points_clockwise(origin, local_p0, local_p1);
	bool a_is_cw = are_points_clockwise(origin, local_p0, uv);
	bool b_is_cw = are_points_clockwise(origin, uv, local_p1);

	if (is_cw) {
		if (!are_points_clockwise(origin, local_p0, uv)) {
			return 0.0;
		}

		if (!are_points_clockwise(origin, uv, local_p1)) {
			return 0.0;
		}
	} else {
		if (are_points_clockwise(origin, local_p0, uv)) {
			return 0.0;
		}

		if (are_points_clockwise(origin, uv, local_p1)) {
			return 0.0;
		}
	}

	return clamp(1.0 - dl / line_thickness, 0.0, 1.0);
}

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	// Called for every pixel the material is visible on.
	vec2 uv_poincare = uv_to_poincare(UV);
    vec2 local_p0 = apply_mobius_trsf(p0);
    vec2 local_p1 = apply_mobius_trsf(p1);
    
    if (length(local_p0 - local_p1) < 0.05) {
	    COLOR = vec4(base_color, 0.0);
    } else {
    
        float intensity = _intensity_as_circle_arc(uv_poincare, local_p0, local_p1);
        float p0_intensity = 1.0 - length(local_p0 - uv_poincare) / line_thickness;
        float p1_intensity = 1.0 - length(local_p1 - uv_poincare) / line_thickness;

        intensity = max(intensity, p0_intensity);
        intensity = max(intensity, p1_intensity);

        COLOR = vec4(base_color, intensity);
    }
}

//void light() {
	// Called for every pixel for every light affecting the CanvasItem.
	// Uncomment to replace the default light processing function with this one.
//}
