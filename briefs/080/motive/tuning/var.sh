#!/bin/zsh
# usage: var.sh "k=v,k=v" seeds plan...   measures plans with those rulebook values, without touching the tree's rulebook
W=/Users/sky/Projects/suborned/out/scratch/keel/wt080; T=$W/out/scratch/motive/tune; R=$T/root
python3 -I - "$1" <<'P'
import sys,re
s=open('/Users/sky/Projects/suborned/out/scratch/keel/wt080/contracts/rules/rulebook.json').read()
for kv in filter(None,sys.argv[1].split(',')):
    k,v=kv.split('=')
    s,n=re.subn(r'("%s": \{ ?"v": )-?\d+'%re.escape(k),lambda m:m.group(1)+v,s)
    assert n==1,k
open('/Users/sky/Projects/suborned/out/scratch/keel/wt080/out/scratch/motive/tune/root/contracts/rules/rulebook.json','w').write(s)
P
shift; N=$1; shift; F=$(mktemp); (cd $R && DOTNET_gcServer=1 dotnet $W/out/missions/engine/tree/bin/suborned-sim.dll batch "$@" --seeds $N --seed-from 1 --digest --no-replay --out $F >/dev/null 2>&1); python3 -I $T/sum.py $F
