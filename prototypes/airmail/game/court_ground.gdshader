shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;
uniform sampler2D setts : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D relief : hint_normal, filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D stone_arm : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D wear : filter_linear_mipmap, repeat_disable;
uniform sampler2D asphalt : source_color, filter_linear_mipmap_anisotropic, repeat_enable;
varying vec3 world;
void vertex() {
    world = (MODEL_MATRIX * vec4(VERTEX,1.)).xyz;
}
void fragment() {
    vec2 m = world.xz;
    vec2 uv = m / 2.;
    vec3 marks = texture(wear,(m+vec2(19.,26.))/vec2(39.,22.)).rgb;
    vec3 stone = texture(setts,uv).rgb;
    vec3 arm = texture(stone_arm,uv).rgb;
    vec3 repair = texture(asphalt,m/2.35).rgb * vec3(.55,.49,.43);
    float wet = smoothstep(.15,.78,marks.r);
    ALBEDO = mix(stone,repair,marks.b)*mix(1.,.57,wet)*(1.-.24*marks.g);
    AO = mix(.68+.32*arm.r,1.,marks.b);
    ROUGHNESS = clamp(mix(.37+.52*arm.g,.18,wet)-marks.g*.19+marks.b*.07,.15,.89);
    SPECULAR = mix(.38,.64,wet);
    NORMAL_MAP = texture(relief,uv).rgb;
    NORMAL_MAP_DEPTH = .75*(1.-.7*marks.b)*(1.-.52*wet);
}
