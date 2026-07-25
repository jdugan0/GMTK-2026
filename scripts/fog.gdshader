shader_type canvas_item;

// Uniforms for customization
uniform sampler2D noise_texture : repeat_enable;
uniform vec3 smoke_color : source_color = vec3(0.8);
uniform float distortion_speed : hint_range(0.0, 5.0) = 0.2;
uniform float density : hint_range(0.0, 1.0) = 1.0;

// The flashlight's baked cone texture, plus the SCREEN_UV -> light-texture-UV
// transform, both pushed every frame by FogMask.cs. Rows of a 2x3 matrix.
uniform sampler2D light_texture : filter_linear, repeat_disable;
uniform vec3 screen_to_light_uv_x = vec3(0.0);
uniform vec3 screen_to_light_uv_y = vec3(0.0);
// The cone shader's peak output is well under 1.0, so remap before using it.
uniform float lit_min : hint_range(0.0, 1.0) = 0.0;
uniform float lit_max : hint_range(0.0, 1.0) = 0.4;
uniform float alpha_in_dark : hint_range(0.0, 1.0) = 1.0;
uniform float alpha_in_light : hint_range(0.0, 1.0) = 0.0;
uniform int debug_view : hint_enum("Off", "Lit Amount", "Light UV") = 0;

void fragment() {
    // Pre-calculate time-based offsets
    float time_offset_1 = TIME * distortion_speed * 0.1;
    float time_offset_2 = TIME * distortion_speed * 0.2;

    // Sample noise with distortion
    vec4 noise = texture(noise_texture, UV + time_offset_1);

    // Create distorted UV and sample final noise
    vec2 distorted_uv = UV + noise.rg * 0.1 - 0.05 + time_offset_2;
    float final_noise = texture(noise_texture, distorted_uv).r;

    // Where does this pixel land inside the flashlight's light texture?
    vec3 screen = vec3(SCREEN_UV, 1.0);
    vec2 light_uv = vec2(dot(screen_to_light_uv_x, screen), dot(screen_to_light_uv_y, screen));

    // repeat_disable clamps to the edge, which is black in the cone texture, so
    // anything outside the light's footprint reads as unlit.
    float lit = texture(light_texture, light_uv).r;
    float lit_amount = smoothstep(lit_min, max(lit_max, lit_min + 0.001), lit);
    float light_mask = mix(alpha_in_dark, alpha_in_light, lit_amount);

    float smoke_alpha = final_noise * density * light_mask;

    // Output color
    COLOR = vec4(smoke_color, smoke_alpha);

    if (debug_view == 1) {
        COLOR = vec4(vec3(lit_amount), 1.0);
    } else if (debug_view == 2) {
        COLOR = vec4(fract(light_uv), 0.0, 1.0);
    }
}
