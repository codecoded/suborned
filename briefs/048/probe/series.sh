#!/bin/zsh
# quiet-wait then run each config; records who else was running at start/end
cd /Users/sky/Projects/suborned
for cfg in "$@"; do
  n=${cfg%%|*}; rest=${cfg#*|}
  for i in $(seq 1 60); do [ $(pgrep -f 'MacOS/The Suborned' | wc -l) -eq 0 ] && [ $(pgrep -x yes | wc -l) -eq 0 ] && break; sleep 5; done
  echo "$n start $(date +%T) $(uptime | sed 's/.*load averages: //') others=$(pgrep -f 'MacOS/The Suborned' | wc -l | tr -d ' ')" >> out/redline/048/series.log
  briefs/048/probe/run.sh $n -novsync ${=rest}
  echo "$n end $(date +%T) $(uptime | sed 's/.*load averages: //') others=$(pgrep -f 'MacOS/The Suborned' | grep -v $$ | wc -l | tr -d ' ')" >> out/redline/048/series.log
done
echo done >> out/redline/048/series.log
