"""Deco Kit low-rise family: Southside, the neighbourhood south of the boulevard (DecoKitSouthside), sized for the
2.5D camera. From the 38 deg orthographic view a building hides ~0.9 x its height of the street behind it, so the
boulevard's 26-37 m walls would bury a street; these carry the same frontage at 2 and 3 floors over the shop
(12.9 / 16.3 m to the coping). Conventions as skyline.py: face -Y, pivot front-centre at the walk, 6 x 14 m bays.
Every Southside block is a podium: DecoKitHighrise lays a landscaped deck over its roof and stands its programme and
towers on it, so these roofs are left flat inside the coping (no plant, skylights or tanks to pierce the deck).
  StreamBay_2 / _3 / _3W      skyline.py's streamline bay
  StreamEnd_2 / _3 / _3W      skyline.py's run end
  StreamCorner_2 / _3         facades.py's quarter-round corner with its fin and mast
  PortholeBay_3, BalconyBay_3 facades.py's variety bays
"""

import math

from facades import balcony_bay, porthole_bay, stream_corner
from kit import asset
from skyline import stream_bay, stream_end
from structures import ccw


def arc_pts(r, n=10):
    """Half disc of radius r standing on local x = 0 (x <= 0), CCW: a barrel vault's section."""
    return ccw([(-r * math.sin(math.pi * i / n), r * math.cos(math.pi * i / n)) for i in range(n + 1)])


for _n in (2, 3):
    asset(f"StreamBay_{_n}", "Facades", bevel=0.0)(stream_bay(_n, "Limestone_Tex"))
    asset(f"StreamEnd_{_n}", "Facades", bevel=0.0)(stream_end(_n, "Limestone_Tex"))
    asset(f"StreamCorner_{_n}", "Facades", bevel=0.0)(stream_corner(_n, roof=False))
asset("StreamBay_3W", "Facades", bevel=0.0)(stream_bay(3, "Render_White"))
asset("StreamEnd_3W", "Facades", bevel=0.0)(stream_end(3, "Render_White"))
asset("PortholeBay_3", "Facades", bevel=0.0)(porthole_bay(3, roof=False))
asset("BalconyBay_3", "Facades", bevel=0.0)(balcony_bay(3, roof=False))
