#!/bin/zsh
# Run the probe for each named configuration: root dir + MF_* env. Usage: sweep.sh <out.jsonl> <benches> <policies> name:root:ENV=..,ENV=.. ...
set -e
here=${0:A:h}; mf=$here/../mfbin/suborned-sim.dll
out=${1:A}; benches=$2; policies=$3; shift 3
: > $out
for spec in "$@"; do
  name=${spec%%:*}; rest=${spec#*:}; root=${rest%%:*}; envs=${rest#*:}
  [[ "$envs" == "$rest" ]] && envs=""
  (
    cd $here/../roots/$root
    for kv in ${(s:,:)envs}; do export $kv; done
    dotnet $mf mf all --benches $benches --policies $policies --out $out.tmp 2>/dev/null | sed "s/^/$name  /"
  )
  if [[ -f $out.tmp ]]; then sed "s/^{/{\"config\":\"$name\",/" $out.tmp >> $out; rm $out.tmp; fi
done
