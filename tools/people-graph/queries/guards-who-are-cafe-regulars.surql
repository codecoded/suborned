-- query: guards-who-are-cafe-regulars
-- about: The guards who are regulars of the café: a `regular` tie to someone who works at the café (GDD §05: the guard's café stop; §08: the café keeper tips on the guard), with the café staff they are regulars of and the tie's stop.
-- param $position string guard the position or civic role of the customers
-- param $unit unit cafe the unit whose staff they are regulars of
LET $position = "guard";
LET $unit = unit:⟨cafe⟩;
LET $staff = (SELECT VALUE in FROM works_at WHERE out = $unit);
SELECT record::id(id) AS id, name, record::id(temperament) AS temperament, record::id(need) AS need,
    (SELECT record::id(out) AS staff, out.name AS staff_name, out.position AS staff_position, at FROM tie WHERE in = $parent.id AND kind = tie_kind:regular AND out IN $staff ORDER BY staff) AS regular_of
  FROM person WHERE position = $position AND id IN (SELECT VALUE in FROM tie WHERE kind = tie_kind:regular AND out IN $staff) ORDER BY id;
