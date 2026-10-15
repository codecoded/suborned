-- query: who-can-introduce
-- about: The shortest chain of introducing ties (family, workmate, regular; the rules' `introduces` flag) from person A to person B, hop by hop. A turned contact can introduce the people a tie joins them to, so a tie counts in either direction. hops is none when no chain exists.
-- param $a person p-cafe-12 the person you have turned
-- param $b person p-records-office-07 the person you want to be introduced to
LET $a = person:⟨p-cafe-12⟩;
LET $b = person:⟨p-records-office-07⟩;
LET $path = $a.{..12+shortest=$b}(<->(tie WHERE introduces)<->person);
RETURN {
  from: record::id($a), to: record::id($b),
  hops: IF $a = $b { 0 } ELSE IF $path = NONE { NONE } ELSE { array::len($path) },
  steps: IF $a = $b OR $path = NONE { [] } ELSE {
    array::windows(array::prepend($path, $a), 2).map(|$w| {
      from: record::id($w[0]), from_name: $w[0].name, to: record::id($w[1]), to_name: $w[1].name,
      via: (SELECT record::id(kind) AS kind, at, mutual FROM tie WHERE introduces AND ((in = $w[0] AND out = $w[1]) OR (in = $w[1] AND out = $w[0])))
    })
  }
};
