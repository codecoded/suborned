shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;
#include "res://irradiance.gdshaderinc"
#include "res://postal_finishes.gdshaderinc"

uniform sampler2D base_map : source_color, hint_default_white, filter_linear_mipmap_anisotropic;
uniform sampler2D surface_map : hint_default_white, filter_linear_mipmap_anisotropic;
uniform sampler2D bump : hint_normal, filter_linear_mipmap_anisotropic;
uniform sampler2D lamps : source_color, hint_default_black, filter_linear_mipmap_anisotropic;
uniform float metal = 0.0;
uniform float rough = 0.72;
uniform float bump_depth = 0.0;
uniform float lamp_energy = 0.0;
uniform vec4 tint : source_color = vec4(1.0);
uniform bool paving = false;
uniform bool packed_surface = false;
uniform bool rough_map = false;
uniform float paving_scale = 1.0;

void vertex() {
    surface_world = (MODEL_MATRIX * vec4(VERTEX,1.0)).xyz;
    surface_normal = MODEL_NORMAL_MATRIX * NORMAL;
}

void fragment() {
    // The pavement and road receive the same world-space tiling as the original slab tops.
    vec2 uv = paving ? surface_world.xz*paving_scale : UV;
    ALBEDO = texture(base_map,uv).rgb * tint.rgb;
    vec3 data = texture(surface_map,uv).rgb;
    METALLIC = metal * (packed_surface ? data.b : 1.);
    ROUGHNESS = rough * (packed_surface ? data.g : (rough_map ? data.r : 1.));
    if (paving && rough_map) {
        ROUGHNESS = max(.72,ROUGHNESS);
        SPECULAR = .23;
    }
    float contact = packed_surface ? data.r : 1.0;
    float macro_bump = 1.;
    vec3 room_light = vec3(0.);
    if (dressed && (architecture || (paving && !rough_map))) {
        vec3 tilt;
        postal_finish(surface_world,surface_normal,UV,UV2,paving,ALBEDO,ROUGHNESS,METALLIC,
            contact,tilt,macro_bump);
        NORMAL = normalize(NORMAL + mat3(VIEW_MATRIX)*tilt);
        if (architecture && UV.y >= .75 && UV.x < .5) {
            vec3 eye = PROJECTION_MATRIX[3][3] > .5 ? INV_VIEW_MATRIX[2].xyz : normalize(CAMERA_POSITION_WORLD - surface_world);
            room_light = workroom(UV,eye,normalize(surface_normal),ALBEDO,ROUGHNESS,METALLIC);
            macro_bump = 0.;
        }
    }
    if (bump_depth>0.0) {
        NORMAL_MAP = texture(bump,uv).rgb;
        NORMAL_MAP_DEPTH = bump_depth * macro_bump;
    }
    vec4 field = local_irradiance(surface_world,surface_normal);
    AO = contact * mix(0.28,1.0,field.a);
    AO_LIGHT_AFFECT = 0.0;
    vec3 diffuse_bounce = field.rgb * vec3(1.37,1.06,.79);
    EMISSION = texture(lamps,uv).rgb * lamp_energy + ALBEDO * (1.0-METALLIC) * diffuse_bounce + room_light;
}
