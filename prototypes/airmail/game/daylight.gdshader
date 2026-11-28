shader_type sky;

uniform sampler2D daylight : source_color, filter_linear_mipmap, repeat_enable;
uniform sampler2D arcade : source_color, filter_linear_mipmap, repeat_enable;

void sky() {
    vec3 light = texture(daylight, SKY_COORDS).rgb;
    if (AT_CUBEMAP_PASS) {
        // Keep direct daylight overhead and include the facing shops in metal and glazing.
        float frontage = 0.67 * (1.0 - smoothstep(0.08, 0.58, EYEDIR.y));
        light = mix(light, texture(arcade, SKY_COORDS).rgb, frontage);
    }
    COLOR = light;
}
