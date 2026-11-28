shader_type spatial;
uniform sampler2D rooms : source_color, filter_linear_mipmap_anisotropic, repeat_disable;
varying vec3 world;
varying vec3 outward;
void vertex(){
    world = (MODEL_MATRIX*vec4(VERTEX,1.)).xyz;
    outward = normalize(MODEL_NORMAL_MATRIX*NORMAL);
}
void fragment(){
    vec2 quadrant = floor(UV*2.);
    vec2 opening = fract(UV*2.);
    vec3 dir = PROJECTION_MATRIX[3][3]>.5 ? INV_VIEW_MATRIX[2].xyz : normalize(CAMERA_POSITION_WORLD-world);
    float head_on = max(.25,dot(outward,dir));
    vec3 side = normalize(cross(vec3(0.,1.,0.),outward));
    vec2 shift = vec2(dot(dir,side)*.17,-dir.y*.19)/head_on;
    // The far wall moves behind the sill; the work surface at the bottom stays close to the glass.
    vec2 inside = opening-clamp(shift,vec2(-.29),vec2(.29))*mix(.92,.25,smoothstep(.50,.92,opening.y));
    vec2 margin = min(inside,1.-inside);
    float reveal = smoothstep(-.035,.012,min(margin.x,margin.y));
    vec3 contents = texture(rooms,(quadrant+clamp(inside,.005,.995))*.5).rgb;
    float lit = smoothstep(.25,.88,dot(contents,vec3(.21,.72,.07)));
    ALBEDO = mix(vec3(.042,.039,.030),contents*.42,reveal);
    ROUGHNESS = mix(.25,.55,head_on);
    METALLIC = .055;
    SPECULAR = .45;
    EMISSION = contents*(.18+.31*lit)*reveal;
}
