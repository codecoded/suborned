# Call box and phone booth siting: what real cities did (Splice, 2026-10-19)

Question (Sky, 2026-10-19: "Call boxes should be placed where a real city would place them"): where did mid-century fire and police call boxes and street telephone kiosks stand, relative to corners, crossings, entrances, stations and each other?

Finding in one line: no published numeric siting standard turned up. The practice is consistent, though: on the pavement at a corner or junction, in sight of it; outside stations, post offices, town halls and squares; on narrow streets in a corner building instead of the street; spacing set locally (every other corner on a grid). Distances in `tools/world/civic/siting.ts` are starting values [S], each tied to the practice below, not to a quoted figure.

## What the sources say

| Practice | Source (read 2026-10-19) | Used for |
|---|---|---|
| Fire alarm boxes "typically installed on street corners or on the outside of commercial buildings in urban areas"; still kept near schools | https://en.wikipedia.org/wiki/Fire_alarm_call_box | corner pavement; building-front option |
| Police post: "strategic street positions were on corners and in places where a person had a good view of the Police Post"; lantern visible from a distance; "situated so it can be seen in any direction"; Reading example at a junction | https://www.britishtelephones.com/postpa1.htm | corner, in sight of the junction |
| Police boxes at junctions and corners (Glasgow: Byres Road junction, Royal Bank Place corner, Glassford Street intersection; Trafalgar Square south-east corner); one outside Earl's Court station; one outside Sheffield Town Hall; smaller posts "where there was no space"; City of London posts narrower because streets too tight | https://en.wikipedia.org/wiki/Police_box | corner; station and civic-building fronts; narrow street = smaller post |
| Edinburgh boxes on corners, street ends and at junctions; on narrow Rose Street the police leased a corner building instead because the street was "too narrow to be further constricted by a box" | https://threadinburgh.scot/2025/06/19/the-thread-about-the-edinburgh-police-box-architectural-time-traveller-but-no-tardis/ | never narrow the walk; no spacing rule exists |
| First Sunderland boxes along tram routes; one built into a curved corner wall at Kayll Road / Hylton Road (search summary of the Newcastle HER record, page not fetched) | https://sitelines.newcastle.gov.uk/SMR/16597 | tram routes, corner |
| New York, grid streets: boxes on "every other corner"; other layouts "scattered at intervals" | https://forgotten-ny.com/2020/08/gamewell-fire-alarm-woodside/ | spacing: a box serves its corner, not every one |
| Gamewell boxes mounted on a pedestal or pole at a street corner; later stand-alone boxes on exterior walls and lamp posts | https://collection.ingenium.ca/en/id/1967.0045.001 · https://archive.blogs.harvard.edu/preserving/2015/03/22/the-early-use-of-police-and-fire-alarms/ (both search summaries, not fetched) | corner, kerb-side post |
| GPO kiosks stood outside post offices and railway stations (K6 outside Blackpool GPO, six outside Leeds GPO, two beside the Preston station entrance block, one outside Loughborough station); K6 designed to "occupy less pavement space"; London boroughs resisted kiosks on their streets | https://en.wikipedia.org/wiki/Red_telephone_box · https://britishlistedbuildings.co.uk/101292367-two-k6-telephone-kiosks-beside-entrance-block-of-preston-railway-station-preston-town-centre-ward · https://charnwood.gov.uk/listed_buildings/k6_telephone_kiosk_outside_the_railway_station_nottingham_road_loughborough | entrances and stations are where booths stand |

## Modern guidance that fits the same practice (not mid-century; kept short)

- A side-road drawing asks for the visibility splay to be kept clear of street furniture, and no furniture that signals the side road's presence: https://www.hertfordshire.gov.uk/media-library/documents/highways/standard-drawings/pdf/hst-1100-51-side-road-continuous-footway.pdf (search summary). The generator already keeps sight-blocking items 6 m off a corner (SF-8).
- Councils and TfL objecting to kiosk applications cite 0.45 m minimum set-back from the carriageway, 2.2 / 3.3 m clear footway, kiosks beside bus stops and traffic signals, and clutter where kiosks already stand close together: https://camdocs.camden.gov.uk/CMWebDrawer/Record/7007800/file/document (search summary). The plan already has 0.45 m (SF-5), 2 m walk (SF-1) and 2 m off crossings (SF-2).

## Not found

- A numeric spacing or setback rule for call boxes. NFPA 72 results were about indoor pull stations. A modern "San Francisco boxes about two blocks apart" claim appeared in a search summary only and is not used.
- Which side of the street a box stood on, and how far from the corner point. Chosen values are ours.

## How the generator reads it (tools/world/civic/siting.ts, tools/world/gen/street/quiet.ts)

- candidates: kerb stations on the pavement `setback` (6 m, the SF-8 sight clearance) to `zone` (25 m, one frontage: in sight of the junction) from a junction corner of the same pavement; never in any other road's carriageway (a side-road mouth is that road's carriageway); opens on the footway.
- preference: bigger street first (primary, district, access); within a tier, the one that brings most ground in reach.
- doors, stations and stops: booths at stations and stops are already placed by the catalogue (2 per stop and bank); a booth at a shop or station door is a design choice (it is R-REPORT's source list), not a corner box.

## What the block's north-west mouth needed (2026-10-19, after the siting rule)

- Starline Street north of Garden Row has built ground on both kerbs and no corner of its own (Garden Row leaves it eastward only): no pavement stands there. The only corner pavement within reach of that ground is the square's north-west corner (block -26, -8); a box 6 m off it along Starline Street (block -25, -14) reaches the 14 samples in 31-34 m, and a police post is needed there too for R-DOCK (the last sample is 39.8 m from it). Both want the same two kerb stations (block y -14 and -16), so a box may stand beside a post or a dock at 2 m (`QUIET.apartOther`) instead of 8 m; two of one kind still keep 8 m.
- The call boxes are placed before the posts now (a box is where a witness reports; the post takes what is left), and a tie in gain goes to the box nearest its corner.
- Picture: briefs/023/splice/nw-corner.png (briefs/023/splice/nw-corner.ts draws it). Options for Sky if the box-beside-a-post corner is not wanted: (a) a plan change (Datum): a footway on Starline Street's west or east kerb north of Garden Row, so a box can stand there; (b) move the robot dock 5 m north-west in Records Square (Vantage), so R-DOCK needs no post at the corner; (c) accept a box 4 m off the corner (SF-8 sight clearance relaxed: refused).

## After the walk grid learned the pavement (2026-10-19, Sky: "add pavement" on Starline Street north of Garden Row)

- The plan always had the 6.5 m access pavement inside each plot (B-3 keeps buildings behind it, the world builds it with trees and lamps); the walk grid blocked a built plot to its edge, so no person, call box or R-REPORT sample could stand on it. A built plot now blocks only behind its building line (`buildingLine`, tools/world/gen/edges.ts: the plot edge set back along each road by the street section's pavement); the pavement is ground, its furniture blocks where it stands. That was the whole of the "no pavement there" finding above: it held for every street, not Starline alone.
- Consequences for siting: the 14 samples are covered by a box on Starline's east pavement north of Garden Row (block -25, 14), 18 m off the Starline/Garden Row corner, not by one at the square's corner; a police post stands on Garden Row's north pavement (block -12, 5). The box, the post and the dock no longer stand within 7 m of each other at (-26, -8).
- `QUIET.apartOther` (a box beside a post or a dock at 2 m) is gone: the reason for it (both wanted the same two kerb stations at the square's corner) was the missing pavement. Everything keeps `apart` (8 m) from everything.
- A police post prefers a corner's pavement too (the same 6..25 m set-back, bigger street first), and stands elsewhere in a kerb zone only where no corner's ground brings more samples in reach. Without that, the new strip of pavement along the window's south edge (Rocket Lane's far side) took four posts along one kerb.
