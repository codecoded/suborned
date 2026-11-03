namespace Suborned.Sim
{
    /// <summary>MOTIVE PROBE (brief 085), scratch only: a pressured Inez's call. MP_ON=1, MP_STAY s at her side, MP_RUN cm/s back.</summary>
    public static class MotivePress
    {
        public static readonly bool On = System.Environment.GetEnvironmentVariable("MP_ON") == "1";
        public static readonly int StayS = int.Parse(System.Environment.GetEnvironmentVariable("MP_STAY") ?? "5");
        public static readonly bool Note = System.Environment.GetEnvironmentVariable("MP_NOTE") == "1";
        public static readonly int Run = int.Parse(System.Environment.GetEnvironmentVariable("MP_RUN") ?? "220");
    }
}
