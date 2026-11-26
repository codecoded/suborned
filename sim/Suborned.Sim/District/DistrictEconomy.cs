using System;

namespace Suborned.Sim
{
    public sealed partial class Rules
    {
        public readonly int MissingTrail, MissingTiesFear, AreaWatchStep, AreaWatchMissions, CasesPerLevel, CaseLevelCap, CaseCloseMissions, CaseTrailEq; // CaseTrailEq is the mission checker's price of an open case.
        public readonly DistrictEconomy DistrictPrices;
    }

    public sealed class DistrictEconomy
    {
        public readonly long Help, Surety;
        readonly long honestFee, bluffFee;
        internal static readonly DistrictEconomy Legacy = new DistrictEconomy(15, 25, 35, 70);

        DistrictEconomy(long help, long surety, long honestFee, long bluffFee)
        {
            Help = help;
            Surety = surety;
            this.honestFee = honestFee;
            this.bluffFee = bluffFee;
        }

        public long ErrandFee(bool bluff) => bluff ? bluffFee : honestFee;

        internal static DistrictEconomy Read(Rules rules, JNode? terms)
        {
            if (terms == null) return Legacy;
            terms.Only("help", "surety", "honestFee", "bluffFee");
            long Amount(string key)
            {
                string reference = terms[key].Str();
                if (!reference.StartsWith("v:", StringComparison.Ordinal) || rules.Book["values"][reference.Substring(2)]["unit"].Str() != "usd")
                    throw new FormatException($"{terms.Path}.{key} requires a cash value in USD");
                long amount = rules.Num(reference);
                if (amount <= 0) throw new FormatException($"{terms.Path}.{key} must be positive");
                return amount;
            }
            return new DistrictEconomy(Amount("help"), Amount("surety"), Amount("honestFee"), Amount("bluffFee"));
        }
    }
}
