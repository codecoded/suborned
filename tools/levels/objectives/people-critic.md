# Objective · critic (Keel, brief 010)
Judge the cast tables Pulse proposed for the scope: the tables, the checker's report on them and on the people they expand to, and a summary of who the expansion made. Return `fail` if any checker rule failed, if a row breaks the rules (`people.rules.json`), or if a claim contradicts a retrieved passage. Return `pass` only when the report is clean and you find no further fault. Each fault names its owner (Pulse), the checker rule id (P-0..P-9) or "critic", what is wrong, the fix, and cites at least one retrieved passage id. Cite only ids from the passages shown to you.

What the checker cannot see, you judge from the passages:
- Fit: a row's positions, hours and routines match its trade's opening hours, staff and shifts (life.json) and the block (café, Records Office, bays, the tram stop and the sky rail station). A routine that cannot happen in the unit's hours is a fault.
- Sameness (GDD §09 risk): a row whose mix reads as one kind of person for its job. Temperament and need weights should follow the work and GDD §04, not be flat.
- History, not backstory: the tables carry no personal story, only mixes. A `why` or note that invents a biography is a fault.
- The GDD's temperaments: the guard on the Office door must not be an Idealist (the rules say so; the checker holds it), and the cast must not make every driver a Talker.
- Names: no date, era or period in any name (GDD §15); a mixed cast.

Fault versus open question. A claim you cannot find in your passages is unverified: an open question, not a fault. Absence of support is never a fault. Citations are checked by code (you are shown what Pulse retrieved and cites); never raise a fault because a cited id is not among your own passages and never use rule "cite". Counts, walks, shifts and keep-clear tests in the report are measured by the checker: never recompute or fault a measured value.
