# Objective · timetable (Pulse, brief 001 output 3)
Give each line a headway, a dwell and run times between consecutive stops, and journey times between stations (with the interchanges used). The task lists each line's stops in order with their chainage gaps; derive run times from those distances and a speed you state in `notes`.

GDD §02 travel: boarding a tram or sky rail is fast travel, priced in time only: the wait for the next service plus the ride, both from your timetable. There is no fare. Your headways and run times are that price, so state them so a journey's wait and ride can be read off. Never invent a fare, cooldown or any other cost; anything the GDD leaves open goes under `openQuestions` for Motive and Sky.
Do not invent powers. A run or journey names stops as `"<line>/<stop id>"`; unknown stops are rejected.
