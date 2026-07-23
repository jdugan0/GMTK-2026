shader_type canvas_item;
render_mode unshaded;

uniform float cone_half_angle_deg : hint_range(1.0, 90.0) = 25.0;
uniform float distance_falloff : hint_range(0.1, 8.0) = 1.5;
uniform float edge_softness : hint_range(0.01, 1.0) = 0.6;
uniform float beam_length : hint_range(0.05, 3) = 0.5;
uniform float intensity : hint_range(0.0, 2.0) = 1.0;
uniform float cone_offset_x : hint_range(-0.5, 0.5) = -0.05;

uniform float circle_radius : hint_range(0.02, 0.5) = 0.245;
uniform float circle_falloff : hint_range(0.1, 4.0) = 1.0;
uniform float circle_intensity : hint_range(0.0, 2.0) = 1.0;

uniform vec4 tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	vec2 p = UV - vec2(0.5, 0.5);
	float dist = length(p);

	vec2 cp = p - vec2(cone_offset_x, 0.0);
	float cone_dist = length(cp);
	float ang = abs(atan(cp.y, cp.x));
	float half_angle = radians(cone_half_angle_deg);
	float fall = pow(clamp(1.0 - cone_dist / beam_length, 0.0, 1.0), distance_falloff);
	float a = ang / half_angle;
	float edge = 1.0 - smoothstep(1.0 - edge_softness, 1.0, a);
	// zero behind the circle center, regardless of apex offset
	float cone = fall * edge * intensity * step(0.0, p.x);

	float circle = pow(clamp(1.0 - dist / circle_radius, 0.0, 1.0), circle_falloff) * circle_intensity;

	float v = max(cone, circle);
	COLOR = vec4(tint.rgb * v, 1.0);
}
