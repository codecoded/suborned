-- query: regulars-of
-- about: The regulars of a place: the people with a `regular` tie (customer -> staff) to someone who works at the place's unit, with the stops of their day at that unit's places (door, chairs, standing points). Give a time and only the regulars who are there then are listed.
-- param $place place cafe-entrance a place of the unit (its door, a bay, a chair of the café)
-- param $at time none a time of day, hh:mm; none = any time
LET $place = place:⟨cafe-entrance⟩;
LET $at = NONE;
LET $unit = (SELECT VALUE unit FROM ONLY $place);
LET $staff = (SELECT VALUE in FROM works_at WHERE out = $unit);
LET $rows = (SELECT record::id(id) AS id, name, position,
    (SELECT VALUE record::id(out) FROM tie WHERE in = $parent.id AND kind = tie_kind:regular AND out IN $staff ORDER BY out) AS regular_of,
    (SELECT doing, record::id(out) AS place, from_min, to_min, fn::hhmm(from_min) AS from, fn::hhmm(to_min) AS to FROM stops_at
      WHERE in = $parent.id AND out.unit = $unit AND ($at = NONE OR (from_min <= $at AND to_min > $at)) ORDER BY from_min) AS stops
  FROM person WHERE id IN (SELECT VALUE in FROM tie WHERE kind = tie_kind:regular AND out IN $staff) ORDER BY id);
RETURN IF $at = NONE { $rows } ELSE { SELECT * FROM $rows WHERE array::len(stops) > 0 };
