shader_type spatial;
render_mode unshaded, cull_back, depth_test_inverted, depth_draw_never, shadows_disabled, fog_disabled;

uniform vec4 ink : source_color = vec4(0.16, 0.32, 0.31, 1.0);
uniform vec4 paper : source_color = vec4(1.0, 0.91, 0.72, 1.0);

void vertex() {
    // In plan the projection is orthographic. Bias depth alone so the figure's
    // own coat and limbs cannot trigger its behind-cover ink.
    vec4 view_position = MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
    view_position.z += 0.78;
    POSITION = PROJECTION_MATRIX * view_position;
}

void fragment() {
    float facing = smoothstep(0.04, 0.32, abs(dot(normalize(NORMAL), VIEW)));
    ALBEDO = mix(ink.rgb, paper.rgb, facing);
    ALPHA = mix(0.90, 0.72, facing);
}
