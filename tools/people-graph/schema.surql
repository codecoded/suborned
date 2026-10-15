-- The people graph (tools/people-graph): a derived, read-only view of the expanded people. The JSON stays the source of truth;
-- this schema is loaded into a database of its own, p_<people hash>, and never read by a run or a replay.
-- Times are minutes of the day clock (0..1440). Positions are the plan frame, metres. Bump SCHEMA_VERSION (model.ts) with any change here.

-- ---- lookups (people.rules.json)
DEFINE TABLE IF NOT EXISTS temperament SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS name ON temperament TYPE string;
DEFINE FIELD IF NOT EXISTS responds_to ON temperament TYPE array<string>;
DEFINE FIELD IF NOT EXISTS refuses ON temperament TYPE array<string>;
DEFINE FIELD IF NOT EXISTS reports_approach ON temperament TYPE bool;
DEFINE FIELD IF NOT EXISTS institution ON temperament TYPE bool;
DEFINE FIELD IF NOT EXISTS hero ON temperament TYPE string;
DEFINE FIELD IF NOT EXISTS menace ON temperament TYPE string;
DEFINE FIELD IF NOT EXISTS start ON temperament TYPE object;
DEFINE FIELD IF NOT EXISTS start.bravery ON temperament TYPE number;
DEFINE FIELD IF NOT EXISTS start.resentment ON temperament TYPE number;
DEFINE FIELD IF NOT EXISTS start.fear ON temperament TYPE number;
DEFINE FIELD IF NOT EXISTS start.loyalty ON temperament TYPE number;

DEFINE TABLE IF NOT EXISTS need SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS met_by ON need TYPE array<string>;
DEFINE FIELD IF NOT EXISTS help ON need TYPE string;
DEFINE FIELD IF NOT EXISTS requires ON need TYPE string;

DEFINE TABLE IF NOT EXISTS tie_kind SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS direction ON tie_kind TYPE string ASSERT $value IN ['both', 'from-customer', 'from-debtor'];
DEFINE FIELD IF NOT EXISTS between ON tie_kind TYPE string;
DEFINE FIELD IF NOT EXISTS introduces ON tie_kind TYPE bool;
DEFINE FIELD IF NOT EXISTS max_per_person ON tie_kind TYPE int;
DEFINE FIELD IF NOT EXISTS max_incoming ON tie_kind TYPE option<int>;

-- ---- nodes
DEFINE TABLE IF NOT EXISTS place SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS kind ON place TYPE string ASSERT $value IN ['door', 'bay', 'stop', 'seat', 'stand', 'home', 'beyond'];
DEFINE FIELD IF NOT EXISTS name ON place TYPE string;
DEFINE FIELD IF NOT EXISTS at ON place TYPE option<array<number>>;
DEFINE FIELD IF NOT EXISTS area ON place TYPE option<string>;
DEFINE FIELD IF NOT EXISTS plot ON place TYPE option<string>;
DEFINE FIELD IF NOT EXISTS unit ON place TYPE option<record<unit>>;
DEFINE FIELD IF NOT EXISTS set ON place TYPE option<string>;
DEFINE FIELD IF NOT EXISTS residents ON place TYPE option<int>;
DEFINE FIELD IF NOT EXISTS simulated ON place TYPE option<int>;

DEFINE TABLE IF NOT EXISTS unit SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS name ON unit TYPE string;
DEFINE FIELD IF NOT EXISTS plot ON unit TYPE string;
DEFINE FIELD IF NOT EXISTS area ON unit TYPE string;
DEFINE FIELD IF NOT EXISTS trade ON unit TYPE string;
DEFINE FIELD IF NOT EXISTS employer ON unit TYPE string;
DEFINE FIELD IF NOT EXISTS staff_per_shift ON unit TYPE int;
DEFINE FIELD IF NOT EXISTS hours ON unit TYPE array<number>;
DEFINE FIELD IF NOT EXISTS door ON unit TYPE option<record<place>>;
DEFINE FIELD IF NOT EXISTS official ON unit TYPE option<record<place>>;
DEFINE FIELD IF NOT EXISTS bay ON unit TYPE option<record<place>>;

DEFINE TABLE IF NOT EXISTS shift SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS unit ON shift TYPE record<unit>;
DEFINE FIELD IF NOT EXISTS from_min ON shift TYPE int;
DEFINE FIELD IF NOT EXISTS to_min ON shift TYPE int;
DEFINE FIELD IF NOT EXISTS wraps ON shift TYPE bool;

DEFINE TABLE IF NOT EXISTS person SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS name ON person TYPE string;
DEFINE FIELD IF NOT EXISTS tier ON person TYPE string ASSERT $value = 'simulated';
DEFINE FIELD IF NOT EXISTS position ON person TYPE string;
DEFINE FIELD IF NOT EXISTS temperament ON person TYPE record<temperament>;
DEFINE FIELD IF NOT EXISTS need ON person TYPE record<need>;
DEFINE FIELD IF NOT EXISTS need_weights ON person TYPE object FLEXIBLE;
DEFINE FIELD IF NOT EXISTS institution ON person TYPE option<string>;
DEFINE FIELD IF NOT EXISTS start ON person TYPE object;
DEFINE FIELD IF NOT EXISTS start.bravery ON person TYPE number;
DEFINE FIELD IF NOT EXISTS start.resentment ON person TYPE number;
DEFINE FIELD IF NOT EXISTS start.fear ON person TYPE number;
DEFINE FIELD IF NOT EXISTS start.loyalty ON person TYPE number;
DEFINE FIELD IF NOT EXISTS memory ON person TYPE array<string> ASSERT array::len($value) = 0;
DEFINE FIELD IF NOT EXISTS look ON person TYPE string;
DEFINE FIELD IF NOT EXISTS routine_variant ON person TYPE string;
DEFINE FIELD IF NOT EXISTS home_plot ON person TYPE string;

DEFINE TABLE IF NOT EXISTS role SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS seq ON role TYPE int;
DEFINE FIELD IF NOT EXISTS what ON role TYPE string;
DEFINE FIELD IF NOT EXISTS employer ON role TYPE string;
DEFINE FIELD IF NOT EXISTS position ON role TYPE option<string>;
DEFINE FIELD IF NOT EXISTS civic_role ON role TYPE option<string>;
DEFINE FIELD IF NOT EXISTS at ON role TYPE option<string>;
DEFINE FIELD IF NOT EXISTS near ON role TYPE option<number>;
DEFINE FIELD IF NOT EXISTS window_from ON role TYPE string;
DEFINE FIELD IF NOT EXISTS window_to ON role TYPE string;
DEFINE FIELD IF NOT EXISTS need ON role TYPE option<record<need>>;
DEFINE FIELD IF NOT EXISTS approach ON role TYPE option<string>;
DEFINE FIELD IF NOT EXISTS regular_of ON role TYPE option<string>;
DEFINE FIELD IF NOT EXISTS tied_to ON role TYPE option<array<object>> FLEXIBLE;
DEFINE FIELD IF NOT EXISTS leaves_to ON role TYPE option<object> FLEXIBLE;
DEFINE FIELD IF NOT EXISTS waits_for ON role TYPE option<string>;
DEFINE FIELD IF NOT EXISTS alone ON role TYPE option<bool>;
DEFINE FIELD IF NOT EXISTS cast_ok ON role TYPE bool;
DEFINE FIELD IF NOT EXISTS cast_mode ON role TYPE string ASSERT $value IN ['play', 'rules', 'place'];
DEFINE FIELD IF NOT EXISTS cast_why ON role TYPE string;

-- ---- edges (RELATE)
-- One table for every tie kind: `kind` links the tie_kind lookup, so "any tie", "the introducing ties" and "the 2-hop neighbourhood" are one traversal, and a new kind in
-- people.rules.json is a lookup row, not a table. `introduces` is the kind's rule copied onto the edge so a traversal filters it without a join.
-- A two-way kind (family, workmate, rival) is one edge per pair, in = the lower person id; a directed kind (regular: customer -> staff, debtor: debtor -> creditor) is one edge, in = its source.
DEFINE TABLE IF NOT EXISTS tie TYPE RELATION IN person OUT person ENFORCED SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS kind ON tie TYPE record<tie_kind>;
DEFINE FIELD IF NOT EXISTS introduces ON tie TYPE bool;
DEFINE FIELD IF NOT EXISTS mutual ON tie TYPE bool;
DEFINE FIELD IF NOT EXISTS at ON tie TYPE option<string>;
DEFINE INDEX IF NOT EXISTS tie_kind_idx ON tie FIELDS kind;

DEFINE TABLE IF NOT EXISTS works_at TYPE RELATION IN person OUT unit ENFORCED SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS position ON works_at TYPE string;
DEFINE FIELD IF NOT EXISTS civic ON works_at TYPE bool;
DEFINE FIELD IF NOT EXISTS post ON works_at TYPE string ASSERT $value IN ['door', 'inside', 'bay', 'official'];
DEFINE FIELD IF NOT EXISTS place ON works_at TYPE record<place>;
DEFINE FIELD IF NOT EXISTS shift ON works_at TYPE record<shift>;
DEFINE FIELD IF NOT EXISTS shift_from ON works_at TYPE int;
DEFINE FIELD IF NOT EXISTS shift_to ON works_at TYPE int;
DEFINE FIELD IF NOT EXISTS employer ON works_at TYPE string;

DEFINE TABLE IF NOT EXISTS lives_at TYPE RELATION IN person OUT place ENFORCED SCHEMAFULL;

DEFINE TABLE IF NOT EXISTS stops_at TYPE RELATION IN person OUT place ENFORCED SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS seq ON stops_at TYPE int;
DEFINE FIELD IF NOT EXISTS doing ON stops_at TYPE string ASSERT $value IN ['home', 'commute', 'work', 'meal', 'pre-shift', 'evening', 'errand'];
DEFINE FIELD IF NOT EXISTS from_min ON stops_at TYPE int;
DEFINE FIELD IF NOT EXISTS to_min ON stops_at TYPE int;
DEFINE INDEX IF NOT EXISTS stops_at_place ON stops_at FIELDS out;

-- A stop at a chair of a café set: the seat holds (one person to a chair at a time, P-13). A subset of stops_at, kept so "who sat where" is one hop.
DEFINE TABLE IF NOT EXISTS sits_at TYPE RELATION IN person OUT place ENFORCED SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS doing ON sits_at TYPE string;
DEFINE FIELD IF NOT EXISTS from_min ON sits_at TYPE int;
DEFINE FIELD IF NOT EXISTS to_min ON sits_at TYPE int;

DEFINE TABLE IF NOT EXISTS knows_need_of TYPE RELATION IN person OUT person ENFORCED SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS via ON knows_need_of TYPE array<string>;

DEFINE TABLE IF NOT EXISTS casts TYPE RELATION IN role OUT person ENFORCED SCHEMAFULL;
-- role -> every person who fits the role in its window; `pinned` marks the one the Handover casts (the first by id, unless the mission pins one)
DEFINE FIELD IF NOT EXISTS pinned ON casts TYPE bool;

DEFINE TABLE IF NOT EXISTS has_need TYPE RELATION IN person OUT need ENFORCED SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS weight ON has_need TYPE int;
DEFINE TABLE IF NOT EXISTS has_temperament TYPE RELATION IN person OUT temperament ENFORCED SCHEMAFULL;

-- ---- the load record, published last (a database without it is half loaded)
DEFINE TABLE IF NOT EXISTS graph SCHEMAFULL;
DEFINE FIELD IF NOT EXISTS people_hash ON graph TYPE string;
DEFINE FIELD IF NOT EXISTS schema_version ON graph TYPE int;
DEFINE FIELD IF NOT EXISTS source ON graph TYPE string;
DEFINE FIELD IF NOT EXISTS plan ON graph TYPE string;
DEFINE FIELD IF NOT EXISTS inputs ON graph TYPE object FLEXIBLE;
DEFINE FIELD IF NOT EXISTS counts ON graph TYPE object FLEXIBLE;

DEFINE FUNCTION IF NOT EXISTS fn::hhmm($m: number) -> string { RETURN string::concat(string::slice(string::concat('0', <string> math::floor($m / 60)), -2), ':', string::slice(string::concat('0', <string> math::floor($m % 60)), -2)); };

-- Whether a person holds a `regular` tie to anyone who works for an employer (GDD §05, §08; the Handover's `regularOf`).
DEFINE FUNCTION IF NOT EXISTS fn::regular_of($person: record<person>, $employer: string) -> bool {
  RETURN array::len((SELECT VALUE id FROM tie WHERE in = $person AND kind = tie_kind:regular AND out IN (SELECT VALUE in FROM works_at WHERE employer = $employer))) > 0;
};
