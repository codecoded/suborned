"""RecordsEntrance_Official_Open (brief 028, Vela): RecordsEntrance_Official without its 3 x 6 m marquee, for the place where BoardingPavilion_TB_Office stands over the official bay.

Everything else is civic_records.py's own flush official front, called with the same numbers: the doors at the pavement (sill z 0), the two pilasters (x +-2.35, 5.4 m) with their banners. Only
`marquee(s, 3.0, 3.0)` is left out: the testbed shelter's roof (soffit 3.9, top 4.62, back eave on the tower face) is the cover over the door, and a second slab at 4.05 to 4.62 over x 75.5..78.5
would sit inside it. Same pivot and yaw as RecordsEntrance_Official (face centre at the pavement, block (75.5, -30), yaw 90)."""
from civic_records import banner, doors, pilaster
from kit import REGISTRY, asset


@asset("RecordsEntrance_Official_Open", "Plaza", bevel=0.0)
def records_entrance_official_open(s):
    doors(s, 1.45, 0.0)
    for sx in (-1, 1):
        pilaster(s, sx * 2.35, 5.4)
        x0, x1 = sorted((sx * 2.1, sx * 2.6))
        banner(s, x0, x1, -0.255, 3.8, 2.8, kr=0.04)
