-- query: neighbourhood
-- about: The 1-hop and 2-hop neighbourhood of a person over every tie kind, either direction: hop1 = tied to the person; hop2 = tied to someone in hop1, not the person or hop1. via says how (the tie kinds for hop1, the hop1 people for hop2).
-- param $person person p-cafe-12 the centre
LET $person = person:⟨p-cafe-12⟩;
LET $hop1 = array::sort(array::complement(array::distinct($person<->tie<->person), [$person]));
LET $hop2 = array::sort(array::complement(array::distinct(array::flatten($hop1<->tie<->person)), array::push($hop1, $person)));
RETURN {
  person: (SELECT record::id(id) AS id, name, position, record::id(need) AS need, record::id(temperament) AS temperament FROM ONLY $person),
  hop1: (SELECT record::id(id) AS id, name, position, record::id(need) AS need, record::id(temperament) AS temperament,
      (SELECT VALUE record::id(kind) FROM tie WHERE (in = $person AND out = $parent.id) OR (in = $parent.id AND out = $person)) AS via FROM $hop1),
  hop2: (SELECT record::id(id) AS id, name, position, record::id(need) AS need, record::id(temperament) AS temperament,
      (SELECT VALUE record::id(IF in = $parent.id { out } ELSE { in }) FROM tie WHERE (in = $parent.id AND out IN $hop1) OR (out = $parent.id AND in IN $hop1)) AS via FROM $hop2)
};
