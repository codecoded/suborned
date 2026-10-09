# Objective · timetable (Pulse, brief 001 output 3)
Give each line a headway, a dwell and run times between consecutive stops, and journey times between stations (with the interchanges used). The task lists each line's stops in order with their chainage gaps; derive run times from those distances and a speed you state in `notes`.

Boarding as fast travel (the player boards a tram or sky rail to move around the city) is in Datum's hold but is not priced in the GDD. Record it under `openQuestions` for Motive and Sky: never invent a price, fare, cooldown or rule for it.
Do not invent powers. A run or journey names stops as `"<line>/<stop id>"`; unknown stops are rejected.
