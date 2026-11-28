// Three shared material studies in metres. R/G store tangent slopes, B tone and A roughness.
uniform sampler2D stone_tile : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D glaze_tile : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D drawn_metal : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D roof_sheet : filter_linear_mipmap_anisotropic, repeat_enable;
uniform sampler2D postal_plates : filter_linear_mipmap_anisotropic, repeat_disable;
uniform sampler2D door_coat : source_color, filter_linear_mipmap_anisotropic, repeat_disable;
uniform sampler2D door_finish : filter_linear_mipmap_anisotropic, repeat_disable;
uniform bool dressed = false;
uniform bool architecture = false;

void postal_finish(vec3 pos, vec3 outwards, vec2 atlas, vec2 service, bool sidewalk,
        inout vec3 color, inout float roughness, inout float metalness, inout float occlusion,
        out vec3 perturb, out float keep_atlas_bump) {
    perturb = vec3(0.);
    keep_atlas_bump = 1.;
    ivec2 cell = ivec2(floor(atlas * vec2(8.,4.)));
    int id = cell.y * 8 + cell.x;
    vec3 n = normalize(outwards);
    vec3 a = abs(n);
    bool top = a.y > max(a.x,a.z);
    vec3 right = vec3(1.,0.,0.);
    vec3 up = vec3(0.,1.,0.);
    if (top) { up = vec3(0.,0.,1.); }
    else if (a.x > a.z) { right = vec3(0.,0.,1.); }
    vec2 m = vec2(dot(pos,right), -dot(pos,up));
    vec4 f = vec4(.5,.5,.5,roughness);
    float depth = 0.;

    if (sidewalk || id == 0 || id == 6 || id == 8) {
        // Dressed walls and flutes retain their large profile. The cut paving has its own scale.
        f = texture(stone_tile,m / (sidewalk ? 2.85 : 2.40));
        if (sidewalk || id == 0) {
            color = mix(color, sidewalk ? vec3(.554,.486,.365) : vec3(.695,.612,.458), .84);
            keep_atlas_bump = .14;
        }
        color *= mix(vec3(.67,.64,.58),vec3(1.32,1.31,1.36),f.b);
        roughness = clamp(f.a + (sidewalk ? -.12 : .035),.23,.85);
        depth = id == 8 ? .34 : .74;
    } else if (id == 9 || id == 29 || (id == 7 && !top && pos.y < 1.25)) {
        f = texture(glaze_tile,m / vec2(1.44,.72));
        // A fired celadon dado with an oxide and ivory border at counter height.
        color = mix(vec3(.018,.083,.078),vec3(.123,.368,.293),clamp((f.b-.22)*2.8,0.,1.));
        float band = abs(pos.y-.67);
        float wave = abs(fract(m.x/.24)-.5)*.05;
        float feather = max(.001, fwidth(pos.y));
        float keyline = 1. - smoothstep(.002,.002+feather,abs(band-.054));
        float fan = 1. - smoothstep(.002,.002+feather,abs(band-wave-.008));
        float pigment = max(keyline,fan) * smoothstep(.30,.44,f.b) * (1.-smoothstep(.004,.02,feather));
        color = mix(color,vec3(.55,.43,.23),pigment*.90);
        roughness = f.a;
        keep_atlas_bump = .08;
        depth = .98;
    } else if (id == 16 || id == 23) {
        vec2 art = clamp((fract(atlas*vec2(8.,4.))-.021)/.958,.002,.998);
        vec4 plate = texture(postal_plates,vec2((art.x+(id == 23 ? 1. : 0.))*.5,art.y));
        vec2 feather = clamp(fwidth(plate.rg),vec2(.035),vec2(.22));
        vec2 metal_ink = smoothstep(vec2(.5)-feather,vec2(.5)+feather,plate.rg);
        color = mix(vec3(.014,.045,.052),vec3(.80,.57,.27),metal_ink.r);
        color = mix(color,vec3(.86,.79,.59),metal_ink.g);
        metalness = mix(.11,.87,metal_ink.r);
        roughness = mix(.29,.25,metal_ink.r);
        keep_atlas_bump = id == 16 ? .15 : 0.;
        f.rg = plate.ba;
        depth = .85;
    } else if (id == 2 && service.x > .001) {
        vec4 coat = texture(door_coat,service);
        f = texture(door_finish,service);
        color = coat.rgb;
        metalness = coat.a;
        occlusion *= f.b;
        roughness = f.a;
        keep_atlas_bump = 0.;
        depth = 1.;
    } else if (id == 3 || id == 4 || id == 31 || id == 14) {
        f = texture(drawn_metal,m/vec2(.41,.32));
        if (id == 3 || id == 4) {
            color = mix(color,id == 3 ? vec3(.63,.63,.56) : vec3(.68,.45,.215),.80);
            metalness = .92;
        }
        color *= .84+.32*f.b;
        roughness = clamp(f.a + (id == 31 ? .18 : 0.),.16,.72);
        keep_atlas_bump = .21;
        // Bare brass where parcels slide over the lip and customers lean on the sill.
        if (id == 4 && service.x > .001) {
            float edge = smoothstep(.62,.97,service.y);
            vec2 dist = (vec2(service.x)-vec2(.34,.76))*vec2(5.,8.);
            vec2 hand = exp(-dist*dist);
            float reach = .35+.65*max(hand.x,hand.y);
            float polish = edge*reach;
            roughness = mix(roughness,.14,polish*.9);
            color = mix(color,vec3(.56,.36,.16),polish*.5);
        }
        depth = .26;
    } else if (id == 2 && pos.y > 3.65 && n.y > .12) {
        // The barrel shell runs front-to-back. Use height along its steep sides to keep the folds in scale.
        m = vec2(pos.z, pos.x*.55 + (n.x < 0. ? 1. : -1.)*pos.y*.83);
        right = vec3(0.,0.,1.);
        up = normalize(cross(right,n));
        f = texture(roof_sheet,m/3.20);
        color = mix(vec3(.036,.141,.135),vec3(.24,.443,.356),clamp((f.b-.25)*2.,0.,1.));
        metalness = .43;
        roughness = f.a;
        keep_atlas_bump = .15;
        depth = .78;
    } else if (id == 1 || id == 2 || id == 7) {
        // Continuous vitreous enamel differs from jointed tile. A fine orange peel breaks reflections.
        f = texture(drawn_metal,m.yx/vec2(.39,.39));
        color *= .89 + .22 * f.b;
        roughness = .245 + .28 * (f.a-.3);
        depth = .20;
    }
    vec2 tilt = f.rg*2.-1.;
    perturb = depth*(right*tilt.x + up*tilt.y);
    perturb -= n*dot(perturb,n);
}
