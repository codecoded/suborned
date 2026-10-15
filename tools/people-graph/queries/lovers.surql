-- query: lovers
-- about: The lover pairs (brief 011, Sky's canon: Inez and the door guard): the mutual, secret `lover` tie, each end's position and secret (the affair, held by both), and the stop of the day they meet on (one's stop at the other's work). Empty when no table makes one.
SELECT record::id(in) AS a, in.name AS a_name, in.position AS a_position, IF in.secret != NONE { record::id(in.secret) } ELSE { NONE } AS a_secret,
    record::id(out) AS b, out.name AS b_name, out.position AS b_position, IF out.secret != NONE { record::id(out.secret) } ELSE { NONE } AS b_secret, at
  FROM tie WHERE kind = tie_kind:lover ORDER BY a;
