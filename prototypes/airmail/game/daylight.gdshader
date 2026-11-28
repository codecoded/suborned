shader_type sky;

uniform sampler2D daylight : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D arcade : source_color, filter_linear_mipmap, repeat_enable;
uniform bool low_sun = false;
uniform float reflection_mix = 0.67;

void sky() {
    vec2 sky_uv = SKY_COORDS;
    if (low_sun) {
        // The measured disc in this HDR is at 214.28 degrees, 47.99 degrees above the horizon.
        // Move it to the directional light's azimuth and elevation, keeping the horizon level.
        sky_uv.x -= 0.20479;
        float y = max(0.0, EYEDIR.y);
        if (y > 0.0) {
            float k = 2.18;
            sky_uv.y = acos(clamp(y*k*inversesqrt(1.0+(k*k-1.0)*y*y), 0.0, 1.0)) / PI;
        }
    }
    vec3 light = texture(daylight, sky_uv).rgb;
    if (AT_CUBEMAP_PASS) {
        // Keep direct daylight overhead and include the facing shops in metal and glazing.
        float frontage = reflection_mix * (1.0 - smoothstep(0.08, 0.58, EYEDIR.y));
        light = mix(light, texture(arcade, SKY_COORDS).rgb, frontage);
    }
    COLOR = light;
}
