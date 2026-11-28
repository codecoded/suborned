shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;
#include "res://irradiance.gdshaderinc"

uniform sampler2D wardrobe : source_color, filter_linear_mipmap_anisotropic, repeat_disable;
uniform sampler2D poses : filter_linear, repeat_disable;
uniform bool bounced = true;

void vertex() {
    // One texture column belongs to one real vertex. Consecutive rows are
    // samples of its rigged motion; the lower half holds the posed normals.
    float texel = 1.0 / float(textureSize(poses, 0).y);
    float base = round(UV2.y * 64.0) * 47.0 + 0.5;
    vec2 coord = vec2(UV2.x, (base + INSTANCE_CUSTOM.g * 64.0) * texel);
    vec3 pos = textureLod(poses,coord,0.).rgb;
    vec3 n = textureLod(poses,coord+vec2(0.,.5),0.).rgb;
    if (INSTANCE_CUSTOM.b < .999) {
        coord.y = (base + INSTANCE_CUSTOM.r * 64.0) * texel;
        pos = mix(textureLod(poses,coord,0.).rgb,pos,INSTANCE_CUSTOM.b);
        n = mix(textureLod(poses,coord+vec2(0.,.5),0.).rgb,n,INSTANCE_CUSTOM.b);
    }
    VERTEX = vec3(-1.05,-.04,-.95) + pos * vec3(2.10,2.15,1.90);
    NORMAL = normalize(n*2.-1.);
    surface_world = (MODEL_MATRIX * vec4(VERTEX,1.)).xyz;
    surface_normal = MODEL_NORMAL_MATRIX * NORMAL;
}

void fragment() {
    ALBEDO = texture(wardrobe,UV).rgb;
    ROUGHNESS = .73;
    if (bounced) {
        vec4 field = local_irradiance(surface_world,surface_normal);
        AO = mix(.28,1.,field.a);
        EMISSION = ALBEDO * field.rgb * vec3(1.37,1.06,.79);
    }
}
