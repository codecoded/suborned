-- query: who-is-near
-- about: Who stands within a radius of a place at a time: every stop overlapping the time (from <= time < to) at a place within the radius, in metres on the plan, straight line. 13 m is the witness reach (GDD §05, people rules knowledge). Nearest first.
-- param $place place cafe-entrance the place to stand by
-- param $at time 17:35 the time of day, hh:mm
-- param $radius int 13 metres
LET $place = place:⟨cafe-entrance⟩;
LET $at = 1055;
LET $radius = 13;
LET $here = (SELECT VALUE at FROM ONLY $place);
LET $near = (SELECT VALUE id FROM place WHERE at != NONE AND vector::distance::euclidean(at, $here) <= $radius);
SELECT record::id(in) AS person, in.name AS name, in.position AS position, record::id(out) AS place, math::round(vector::distance::euclidean(out.at, $here) * 10) / 10 AS metres,
    doing, fn::hhmm(from_min) AS from, fn::hhmm(to_min) AS to
  FROM stops_at WHERE out IN $near AND from_min <= $at AND to_min > $at ORDER BY metres, person;
