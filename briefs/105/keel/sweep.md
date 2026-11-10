# Brief 105, engine 0.53.0: the 15 bot plans' sweep (Keel, 2026-11-10)

suborned-sim batch contracts/missions/handover/adaptive --seeds 100 --no-replay (one process, 18 threads, ~26 s). Columns: plan, wins of 100, the rest by fail/cause.

## Before (0.52.0, base 98488a770; 084a687bb changed only the site collide hash and the HUD)
```
careless                   0   entry-seen-camera/seen 97, vale-leaves/driven-away 3
drive-early                0   rivals-reach-vale/lookout 100
drive-round               14   rivals-reach-vale/reached-car 81, closed-in/boxed 5
prepared-loud             62   sealed/sealed 20, rivals-reach-vale/reached-car 6, enforcer-down/shot 5, closed-in/no-go 3, closed-in/on-foot 2, closed-in/car-held 1, vale-leaves/driven-away 1
prepared-loud-lend        25   closed-in/car-held 74, closed-in/no-go 1
quiet-a                  100   
quiet-b                  100   
quiet-b-lookout-ignored    0   rivals-reach-vale/lookout 100
quiet-b-taped              0   rivals-reach-vale/lookout 100
quiet-b-thinned          100   
quiet-g                   95   rivals-reach-vale/downed-enforcer 4, closed-in/no-driver 1
thin-quiet                91   rivals-reach-vale/downed-enforcer 6, vale-leaves/driven-away 2, entry-seen-camera/seen 1
unprepared-loud            0   entry-seen-camera/seen 95, sealed/sealed 5
veteran                   47   enforcer-down/shot 45, sealed/sealed 6, closed-in/car-held 1, closed-in/no-go 1
veteran-drive              8   sealed/sealed 51, vale-leaves/too-late 17, closed-in/car-held 12, enforcer-down/shot 7, cell-down/shot 2, closed-in/no-go 1, closed-in/no-driver 1, rivals-reach-vale/lookout 1
```

## After (0.53.0)
```
careless                   0   entry-seen-camera/seen 97, vale-leaves/driven-away 3
drive-early                0   rivals-reach-vale/lookout 100
drive-round               18   rivals-reach-vale/downed-enforcer 35, closed-in/boxed 28, closed-in/on-foot 18, closed-in/no-go 1
prepared-loud             68   sealed/sealed 15, rivals-reach-vale/downed-enforcer 5, enforcer-down/shot 5, closed-in/no-go 3, closed-in/on-foot 2, closed-in/boxed 1, vale-leaves/driven-away 1
prepared-loud-lend        25   closed-in/car-held 74, closed-in/no-go 1
quiet-a                   99   rivals-reach-vale/downed-enforcer 1
quiet-b                  100   
quiet-b-lookout-ignored    0   rivals-reach-vale/lookout 100
quiet-b-taped              0   rivals-reach-vale/lookout 100
quiet-b-thinned          100   
quiet-g                   94   rivals-reach-vale/downed-enforcer 5, closed-in/no-driver 1
thin-quiet                92   rivals-reach-vale/downed-enforcer 4, vale-leaves/driven-away 2, sealed/sealed 1, entry-seen-camera/seen 1
unprepared-loud            0   entry-seen-camera/seen 95, sealed/sealed 5
veteran                   47   enforcer-down/shot 45, sealed/sealed 6, closed-in/car-held 1, closed-in/no-go 1
veteran-drive              8   sealed/sealed 51, vale-leaves/too-late 17, closed-in/car-held 15, enforcer-down/shot 7, closed-in/no-go 1, rivals-reach-vale/lookout 1
```
