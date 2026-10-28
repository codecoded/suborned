// Brief 075 swap (Latch): not part of the verbatim copy. The swap's assembly (Editor/OldTown/Swap) calls the old builders' internal API
// (DecoKitPlacer, DecoKitFrontage, DecoKitStage, ...) the way DecoKitStreetDemo does, without making any of it public.
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Suborned.OldTown.Swap.Editor")]
