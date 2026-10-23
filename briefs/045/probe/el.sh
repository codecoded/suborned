#!/bin/zsh
# usage: el.sh <run> [extra player flags]  — early/late perf windows with main-thread samples and managed names
R=/Users/sky/Projects/suborned/briefs/045; run=$1; shift
mkdir -p $R/$run; rm -f $R/$run/*.jpg
"/Users/sky/Projects/suborned/out/build/play/The Suborned.app/Contents/MacOS/The Suborned" -shots $R/$run.resolved.json $R/$run -novsync -logFile $R/$run/player.log "$@" >/dev/null 2>&1 &
sleep 2; P=$(pgrep -f "$run.resolved.json" | head -1)
until [ -f $R/$run/m0.jpg ]; do sleep 0.5; done; sleep 4; sample $P 6 -file $R/$run/sample-early.txt >/dev/null 2>&1
until [ -f $R/$run/early.jpg ]; do sleep 0.5; done; sleep 4; sample $P 6 -file $R/$run/sample-late.txt >/dev/null 2>&1
until [ -f $R/$run/late.jpg ]; do sleep 0.5; done; sleep 3
cat $R/$run/sample-early.txt $R/$run/sample-late.txt | grep -o "???  (in <unknown binary>)  \[0x[0-9a-f]*\]" | grep -o "0x[0-9a-f]*" | sort -u > $R/$run/addrs.txt
{ echo "process attach --pid $P"; while read a; do echo "expr -l c -- (char*)mono_pmip((void*)$a)"; done < $R/$run/addrs.txt; echo "process detach"; echo quit; } > $R/$run/lldb.cmds
lldb -b -s $R/$run/lldb.cmds > $R/$run/lldb.out 2>&1
wait; echo done $run
