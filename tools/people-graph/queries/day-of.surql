-- query: day-of
-- about: One person's day: their stops in order (where, doing, from, to), and where they work and live.
-- param $person person p-cafe-12 the person
LET $person = person:⟨p-cafe-12⟩;
SELECT seq, doing, record::id(out) AS place, out.name AS place_name, fn::hhmm(from_min) AS from, fn::hhmm(to_min) AS to FROM stops_at WHERE in = $person ORDER BY seq;
