-- query: needs-known-by-keeper
-- about: The people whose need a keeper knows from the start (rules knowledge.needsKnown: a keeper knows the need of each workmate and of each unit regular), with the need, the temperament and how they know (via). Empty for anyone who is not a keeper.
-- param $keeper person p-cafe-03 the keeper
LET $keeper = person:⟨p-cafe-03⟩;
SELECT record::id(out) AS id, out.name AS name, record::id(out.need) AS need, out.need.help AS help, record::id(out.temperament) AS temperament, via
  FROM knows_need_of WHERE in = $keeper ORDER BY id;
