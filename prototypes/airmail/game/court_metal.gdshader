shader_type spatial;
render_mode cull_disabled;
uniform vec4 coat : source_color;
uniform sampler2D finish : filter_linear_mipmap_anisotropic, repeat_enable;
uniform float rough = .52;
uniform float metal = .5;
varying vec3 pos;
varying vec3 axis;
void vertex(){
    pos = (MODEL_MATRIX*vec4(VERTEX,1.)).xyz;
    axis = normalize(MODEL_NORMAL_MATRIX*NORMAL);
}
void fragment(){
    vec3 a = abs(axis);
    vec2 m = a.y > max(a.x,a.z) ? pos.xz : vec2(a.x>a.z ? pos.z : pos.x,-pos.y);
    vec4 detail = texture(finish,m/vec2(.63,.49));
    float foot = (1.-smoothstep(.15,.68,pos.y)) * smoothstep(.36,.65,texture(finish,m*.57).b);
    ALBEDO = coat.rgb*(.64+.73*detail.b)*mix(1.,.52,foot);
    METALLIC = metal*(1.-foot*.55);
    ROUGHNESS = clamp(rough+(detail.a-.30)*1.2+foot*.28,.18,.9);
    NORMAL_MAP = vec3(detail.rg,1.);
    NORMAL_MAP_DEPTH = .36;
}
