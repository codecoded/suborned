shader_type spatial;
render_mode diffuse_burley, specular_schlick_ggx;

varying vec3 city_position;
varying vec3 city_normal;

void vertex() {
    city_position = (MODEL_MATRIX * vec4(VERTEX, 1.)).xyz;
    city_normal = MODEL_NORMAL_MATRIX * NORMAL;
}

// The bay and blind follow the solid glazing in the Harbour models. Widen a
// mullion's filter footprint as it approaches one screen pixel.
float seam(float metres, float pitch, float width) {
    float d = abs(fract(metres / pitch + .5) - .5) * pitch;
    float aa = max(.025, fwidth(metres));
    return (1. - smoothstep(width, width + aa, d)) * (1. - smoothstep(.35, .90, aa / pitch));
}

void fragment() {
    int finish = int(UV.x * 8.);
    vec3 paint = COLOR.rgb;
    vec3 n = normalize(city_normal);
    float rough = .68;
    float metal = .05;
    vec3 light = vec3(0.);
    if (finish == 1) {
        float along = abs(n.z) > abs(n.x) ? city_position.x : city_position.z;
        vec2 bay = floor(vec2(along / 2.4, city_position.y / 3.25));
        float shade = fract(sin(dot(bay, vec2(23.3, 71.1))) * 1395.72);
        paint *= .75 + .36 * shade;
        float blinds = smoothstep(.73, .90, shade) * (1.-seam(city_position.y, 3.25, .18));
        paint = mix(paint, vec3(.55,.44,.29), .57 * blinds);
        float sill = seam(city_position.y, 3.25, .14);
        float rail = max(sill, seam(along, 2.4, .065) * .65);
        paint = mix(paint, vec3(.50,.47,.38), rail * .85);
        rough = .29;
        metal = .32;
    } else if (finish == 2) {
        rough = .31;
        metal = .70;
    } else if (finish == 3) {
        light = paint * .56;
        rough = .34;
    } else if (finish == 4) {
        rough = .39;
        metal = .16;
    } else if (finish >= 5) {
        // A wraparound salon and a fluted office shaft have different elevations.
        // Glazing is recessed between the limestone piers, never on the domes.
        float wall = 1. - smoothstep(.27,.65,abs(n.y));
        float y = city_position.y;
        float flutes = finish == 5 ? 10. : 8.;
        float arc = UV2.x * flutes;
        float pier = abs(fract(arc + .5) - .5);
        float floorline = fract((y + .4)/3.0);
        float ay = clamp(fwidth(y)*.33,.02,.18);
        float ax = clamp(fwidth(arc),.008,.14);
        float panes = smoothstep(.14-ay,.14+ay,floorline) * (1.-smoothstep(.78-ay,.78+ay,floorline));
        float bay = smoothstep((finish==5 ? .054 : .16)-ax,(finish==5 ? .054 : .16)+ax,pier);
        float shell = wall * smoothstep(.07,.16,UV2.y);
        float window = panes * bay * shell;
        float skyface = dot(n,normalize(vec3(-.40,0.,.86))) * .5 + .5;
        vec3 glass = mix(vec3(.034,.099,.12), vec3(.18,.31,.34), skyface);
        float room = fract(sin(dot(floor(vec2(arc*4.,y/3.)),vec2(17.83,61.12)))*345.26);
        glass *= .83 + .30*room;
        glass = mix(glass,vec3(.42,.36,.25),smoothstep(.83,.97,room)*.6);
        float mullion = seam(arc*4.,1.,.035);
        glass = mix(glass,vec3(.50,.51,.44),mullion*.65);
        paint = mix(paint,glass,window*.93);
        float enamel = (1.-smoothstep(.055,.12,abs(floorline-.91))) * shell;
        paint = mix(paint,vec3(.15,.38,.35),enamel*.65);
        rough = mix(.64,.32,window);
        metal = .06 + window*.22;
    }
    float air = .025 + .29 * smoothstep(58., 186., distance(city_position.xz, CAMERA_POSITION_WORLD.xz));
    vec3 haze = vec3(.49,.63,.70);
    ALBEDO = mix(paint, haze, air * .67);
    EMISSION = light + haze * air * .34;
    ROUGHNESS = rough;
    METALLIC = metal * (1. - air);
}
