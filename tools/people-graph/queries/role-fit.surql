-- query: role-fit
-- about: Each Handover role (mission order) with everyone who fits it in its window, the pinned person first: their need and temperament against what the role asks (need_met; approach_ok = the temperament does not refuse the role's approach; regular_ok = a regular tie to the staff of the employer the role names) and their ties (holds: the tie stands on this person; a two-way tie stands on both).
LET $rows = (SELECT seq, record::id(id) AS role, what, employer, position ?? civic_role AS as, (IF need != NONE { record::id(need) }) AS needs, approach, regular_of, cast_ok, cast_mode,
  (SELECT record::id(out) AS id, out.name AS name, pinned, record::id(out.need) AS need, ($parent.need = NONE OR out.need = $parent.need) AS need_met,
      record::id(out.temperament) AS temperament, ($parent.approach = NONE OR $parent.approach NOT IN out.temperament.refuses) AS approach_ok,
      ($parent.regular_of = NONE OR fn::regular_of(out, $parent.regular_of)) AS regular_ok,
      (SELECT record::id(kind) AS kind, record::id(IF in = $parent.out { out } ELSE { in }) AS with, (IF in = $parent.out { out } ELSE { in }).name AS with_name, mutual, in = $parent.out AS holds, introduces, at FROM tie WHERE in = $parent.out OR out = $parent.out ORDER BY with) AS ties
    FROM casts WHERE in = $parent.id ORDER BY pinned DESC, id) AS people
  FROM role ORDER BY seq);
RETURN $rows;
