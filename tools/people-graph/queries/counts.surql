-- query: counts
-- about: How many of everything the database holds, and the ties by kind.
RETURN {
  people: count(SELECT id FROM person), places: count(SELECT id FROM place), units: count(SELECT id FROM unit), shifts: count(SELECT id FROM shift), roles: count(SELECT id FROM role),
  temperaments: count(SELECT id FROM temperament), needs: count(SELECT id FROM need), secrets: count(SELECT id FROM secret),
  edges: {
    tie: count(SELECT id FROM tie), works_at: count(SELECT id FROM works_at), lives_at: count(SELECT id FROM lives_at), stops_at: count(SELECT id FROM stops_at), sits_at: count(SELECT id FROM sits_at),
    knows_need_of: count(SELECT id FROM knows_need_of), casts: count(SELECT id FROM casts), has_need: count(SELECT id FROM has_need), has_temperament: count(SELECT id FROM has_temperament)
  },
  ties: (SELECT record::id(kind) AS kind, count() AS n FROM tie GROUP BY kind ORDER BY kind)
};
