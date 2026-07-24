shader_type canvas_item;

uniform vec4 vignette_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float radius : hint_range(0.0, 1.5) = 0.75;
uniform float softness : hint_range(0.001, 1.0) = 0.45;
uniform float intensity : hint_range(0.0, 1.0) = 1.0;

void fragment() {
	vec2 centered = UV - vec2(0.5);
	float dist = length(centered);
	float mask = smoothstep(radius - softness, radius, dist) * intensity;
	COLOR = vec4(vignette_color.rgb, vignette_color.a * mask);
}
