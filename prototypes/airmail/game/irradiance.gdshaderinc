// XYZ axes match the 1 m ray-baked grid in author/bake_arcade.py.
uniform sampler3D arcade_field : filter_linear, repeat_disable;
varying vec3 surface_world;
varying vec3 surface_normal;

vec4 local_irradiance(vec3 position, vec3 normal) {
    vec3 p = position + normalize(normal) * 0.38 - vec3(-24.0, 0.22, -8.0);
    vec3 size = vec3(50.0, 12.0, 20.0);
    // Fade at the sides and top. Ground sits directly on the bottom row.
    vec3 edge = min(p, size - 1.0 - p);
    float interior = smoothstep(0.0, 2.0, min(min(edge.x,edge.z),size.y-1.0-p.y));
    vec4 field = texture(arcade_field,(p+.5)/size);
    return mix(vec4(0.0,0.0,0.0,1.0),field,interior);
}
