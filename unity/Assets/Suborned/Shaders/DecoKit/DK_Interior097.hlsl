// Brief 097: rooms behind the glass (interior mapping), for DecoKit/Lit Cutaway's windows under _DK_INTERIOR (DK_Forward097.hlsl's copy).
// Exteriors only (GDD §01): nobody goes in; the glass shows a room as a passer-by would see one. Each pane looks into a box room on a world grid
// (_DKRooms: x room width, y storey height, z depth, m) laid on the wall's own plane (its normal picks the plane: a curved wall's rooms bend
// with it); the view ray is traced to the room's back wall, floor, ceiling and side walls (Andrew Willmott's interior mapping), all procedural:
// a pastel back wall, a warm floor, a pale ceiling with a light panel, venetian blinds part-way down, one in a few rooms lit brighter. No
// texture, about 60 ALU, on glass pixels only. The room is emission (an interior is lit by its own lamps, not the street's sun), _DKRooms.w its
// brightness by day; the glass's own reflections (URP's, from the material's smoothness and the street probe) lie over it by Fresnel.
#ifndef DK_INTERIOR097_INCLUDED
#define DK_INTERIOR097_INCLUDED

float4 _DKRooms;       // global: x width, y storey height, z depth (m), w daytime brightness (unset: 3.6, 3.4, 4.5, 0.32)
float4 _DKRoomsNight;  // global: x lit share of rooms (0..1), y night brightness, z,w spare (Suborned.Look097; unset: day)
float _DKInteriorOff097; // global: 1 = no rooms in the GlassSky film (the Air's A/B); unset = 0 = on

float DKRoomHash(float3 p) { return frac(sin(dot(p, float3(127.1, 311.7, 74.7))) * 43758.5453); }

half3 DKRoomPalette(float h)
{   // cream, mint, peach, pale blue, butter: the city's interiors (GDD §15 base pastels, deepened a little to read behind glass)
    half3 c0 = half3(0.93, 0.87, 0.76), c1 = half3(0.70, 0.86, 0.79), c2 = half3(0.95, 0.78, 0.68), c3 = half3(0.72, 0.82, 0.90), c4 = half3(0.96, 0.90, 0.62);
    return h < 0.2 ? c0 : h < 0.4 ? c1 : h < 0.6 ? c2 : h < 0.8 ? c3 : c4;
}

// positionWS on the glass, its world normal, the view direction (from the surface toward the eye); returns the room's emitted colour
half3 DKInterior(float3 positionWS, half3 normalWS, half3 viewWS)
{
    float3 R = _DKRooms.x > 0.1 ? _DKRooms.xyz : float3(3.6, 3.4, 4.5);
    half bright = _DKRooms.w > 0.0 ? _DKRooms.w : 0.32h;
    float3 n = normalize(float3(normalWS.x, 0, normalWS.z));
    if (abs(normalWS.y) > 0.7) return half3(0, 0, 0);            // a roof light or a canopy: no room
    float3 t = float3(-n.z, 0, n.x), up = float3(0, 1, 0);       // the wall's right and up
    // room space: u along the wall, v up, w into the building (0 at the glass)
    float3 p = float3(dot(positionWS, t), positionWS.y, 0.0);
    float3 d = -viewWS; float3 rd = float3(dot(d, t), d.y, -dot(d, n)); // into the wall = -n
    rd.z = max(rd.z, 1e-3);
    float3 cell = floor(p / float3(R.x, R.y, 1.0));
    float3 f = float3(frac(p.x / R.x) * R.x, frac(p.y / R.y) * R.y, 0.0);
    // distances along the ray to each plane of the box [0, R.x] x [0, R.y] x [0, R.z]
    float tx = rd.x > 0 ? (R.x - f.x) / rd.x : -f.x / min(rd.x, -1e-4);
    float ty = rd.y > 0 ? (R.y - f.y) / rd.y : -f.y / min(rd.y, -1e-4);
    float tz = R.z / rd.z;
    float tHit = min(tz, min(tx, ty));
    float3 h = f + rd * tHit;                                    // the hit, in the room
    float id = DKRoomHash(cell + float3(0.37 * floor(dot(n, float3(1.7, 0, 2.3))), 0, 0));
    half3 wall = DKRoomPalette(frac(id * 7.13));
    half3 col;
    if (tHit == tz) col = wall;                                  // back wall
    else if (tHit == ty) col = rd.y > 0 ? half3(0.97, 0.96, 0.93) : half3(0.55, 0.42, 0.33) * (0.8 + 0.4 * frac(id * 3.7)); // ceiling / floor
    else col = wall * 0.82;                                      // side walls
    // depth shading: the back of the room is darker, the corners darker still (a cheap occlusion)
    float depthK = saturate(h.z / R.z);
    col *= lerp(1.0, 0.62, depthK);
    float2 edge = min(h.xy, R.xy - h.xy);
    col *= 0.75 + 0.25 * saturate(min(edge.x, edge.y) * 1.6);
    // the ceiling's light panel, in the middle of the room
    if (tHit == ty && rd.y > 0) { float2 q = abs(float2(h.x - R.x * 0.5, h.z - R.z * 0.5)); col += half3(1.0, 0.95, 0.82) * 0.9 * step(q.x, R.x * 0.28) * step(q.y, 0.35); }
    // a desk's dark band along the back wall in some rooms
    if (tHit == tz && frac(id * 11.3) > 0.45) col *= (h.y > 0.72 && h.y < 0.78) ? 0.55 : 1.0;
    // venetian blinds: from the top of the window down to a height per room, slats every 6 cm, a little light between them
    float blind = frac(id * 5.9) * 0.65;                         // share of the storey covered from the top
    float fromTop = (R.y - f.y) / R.y;
    if (fromTop < blind) { float slat = frac(f.y / 0.06); col = lerp(half3(0.92, 0.9, 0.84), col, 0.25) * (0.82 + 0.18 * step(0.5, slat)); }
    // lamps: a share of the rooms lit brighter (and at night most of the light there is)
    half lit = frac(id * 17.7) < 0.3 ? 1.6h : 1.0h;
    half nightLit = frac(id * 23.1) < _DKRoomsNight.x ? 1.0h : 0.0h;
    half3 warm = half3(1.0, 0.86, 0.62);
    return col * bright * lit + col * warm * nightLit * _DKRoomsNight.y;
}

#endif
