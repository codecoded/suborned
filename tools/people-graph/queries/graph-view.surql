-- query: graph-view
-- about: For Surrealist's graph view: a person, their ties to the people within `hops` (1 or 2) and the ties among those people, as plain records (nodes with their `id`, edges with `in` and `out`). Their work unit, home and need are added for the centre.
-- param $person person p-cafe-12 the centre
-- param $hops int 1 how far to go: 1 or 2
LET $person = person:⟨p-cafe-12⟩;
LET $hops = 1;
LET $hop1 = array::complement(array::distinct($person<->tie<->person), [$person]);
LET $hop2 = array::complement(array::distinct(array::flatten($hop1<->tie<->person)), array::push($hop1, $person));
LET $people = array::union([$person], IF $hops >= 2 { array::union($hop1, $hop2) } ELSE { $hop1 });
SELECT * FROM array::flatten([
  $people,
  (SELECT VALUE id FROM tie WHERE in IN $people AND out IN $people),
  $person->works_at, $person->works_at->unit, $person->lives_at, $person->lives_at->place, $person->has_need, $person->has_need->need, $person->has_temperament, $person->has_temperament->temperament
]);
