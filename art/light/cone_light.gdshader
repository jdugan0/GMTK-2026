shader_type canvas_item;
render_mode unshaded;

// --- Cone beam (apex at texture center, pointing +X) ---
// Half-angle of the beam in degrees (50 deg full cone = 25 here)
uniform float cone_half_angle_deg : hint_range(1.0, 90.0) = 25.0;
// Higher = light dies off faster with distance
uniform float distance_falloff : hint_range(0.1, 8.0) = 1.5;
// 0 = razor-sharp beam edges, 1 = fully feathered
uniform float edge_softness : hint_range(0.01, 1.0) = 0.6;
// Beam reach as a fraction of texture width (apex is centered, so 0.5 = edge)
uniform float beam_length : hint_range(0.05, 3) = 0.5;
// Brightness near the apex before falloff
uniform float intensity : hint_range(0.0, 2.0) = 1.0;

// --- Circle glow around the player ---
// Radius as a fraction of texture width
uniform float circle_radius : hint_range(0.02, 0.5) = 0.245;
// Higher = glow dies off faster (1.0 = linear, like a plain gradient)
uniform float circle_falloff : hint_range(0.1, 4.0) = 1.0;
uniform float circle_intensity : hint_range(0.0, 2.0) = 1.0;

uniform vec4 tint : source_color = vec4(1.0, 1.0, 1.0, 1.0);

void fragment() {
	vec2 p = UV - vec2(0.5, 0.5);
	float dist = length(p);

	// Cone: bright at apex, 0 at beam_length, feathered sides
	float ang = abs(atan(p.y, p.x));
	float half_angle = radians(cone_half_angle_deg);
	float fall = pow(clamp(1.0 - dist / beam_length, 0.0, 1.0), distance_falloff);
	float a = ang / half_angle;
	float edge = 1.0 - smoothstep(1.0 - edge_softness, 1.0, a);
	float cone = fall * edge * intensity * step(0.0, p.x);

	// Circle glow centered on the player, rotation-invariant
	float circle = pow(clamp(1.0 - dist / circle_radius, 0.0, 1.0), circle_falloff) * circle_intensity;

	// max = whichever is brighter wins; they can never sum into overbright
	float v = max(cone, circle);
	COLOR = vec4(tint.rgb * v, 1.0);
}
