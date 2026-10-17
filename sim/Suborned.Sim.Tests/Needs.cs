// Open needs, phase 1 (brief 017): the people file carries each person's need record (kind, label, size, holder) and the engine reads it
// strictly (unknown = reject). Nothing acts on it yet: the hashes held to one start text are the same before and after (Keel's report).
using System;
using System.Linq;
using Suborned.Sim;
using Xunit;

public class NeedRecordTests
{
    static Engine.Inputs I => Repo.Inputs;

    [Fact]
    public void TheHandoversPinnedRecordsReadWithTheirLabels()
    {
        var g = Engine.New(I);
        Assert.Equal(("debt", "bureau-loan", "Bureau loan"), g.NeedRecordOf("waitress"));
        Assert.Equal(("cash", "lean-month", "Lean month"), g.NeedRecordOf("driver"));
        Assert.Equal(g.NeedRecordOf("waitress"), g.NeedRecordOf(g.Inez.Id)); // by role or by id
        Assert.Null(g.NeedRecordOf("p-nobody"));
    }

    [Fact]
    public void EveryoneCarriesARecordOfTheirOwnNeedsKind()
    {
        var g = Engine.New(I); var c = I.Crowd!;
        var everyone = g.People.Concat(g.Walkers.Select(w => w.P)).ToList();
        Assert.True(everyone.Count > 4);
        foreach (var p in everyone)
        {
            var (kind, record, label) = g.NeedRecordOf(p);
            Assert.Equal(p.Need, kind);
            Assert.Equal(kind, c.Records[record].kind);
            Assert.False(string.IsNullOrEmpty(label), p.Id);
            Assert.Equal("beyond", p.Holder); // phase 1: no holder on the sheet
            Assert.Equal(c.Records[record].size, p.Size);
        }
    }

    [Fact]
    public void UnknownRecordsSizesHoldersAndKeysAreRejected()
    {
        var cast = I.SiteData.Cast.Select(x => x.Id).ToList();
        void bad(string from, string to)
        {
            Assert.Contains(from, I.People);
            Assert.Throws<FormatException>(() => new Crowd(I.People.Replace(from, to), I.Rules, cast));
        }
        bad("\"record\": \"lean-month\"", "\"record\": \"no-such-record\"");      // unknown record
        bad("\"record\": \"lean-month\"", "\"record\": \"bureau-loan\"");         // a record of another kind than the person's need
        bad("\"size\": \"standard\"", "\"size\": \"huge\"");                      // a size the record does not have
        bad("\"holder\": \"beyond\"", "\"holder\": \"p-nobody\"");                // a holder who is not a person of the file
        bad("\"holder\": \"beyond\"", "\"holder\": \"beyond\", \"favour\": 1");   // a key the engine does not read (phase 2)
        bad("[\"debt\",\"Bureau loan\",\"standard\"]", "[\"grudge\",\"Bureau loan\",\"standard\"]"); // a kind people.rules does not list
        bad("\"sizes\": [\"large\",\"small\",\"standard\"]", "\"sizes\": [\"large\",\"small\"]");    // a record's size the kinds do not name
        bad("\"suborned.sim.people/2\"", "\"suborned.sim.people/1\"");
    }

    [Fact]
    public void AReplacedNeedTakesItsKindsRecordWithNoDraw()
    {
        var run = Scenarios.Run(I, Scenarios.Find("N18")!); var g = run.G;
        var next = Debrief.Of(g).Next;
        var inez = next.Contacts.Find(p => p.Id == g.Inez.Id);
        Assert.NotNull(inez);
        Assert.NotEqual("debt", inez!.Need);
        var (kind, record, label) = g.NeedRecordOf(inez);
        Assert.Equal(inez.Need, kind);
        Assert.Equal(I.Crowd!.DefaultRecord(kind), record);
        // people.rules needs.list still lists the retired kind `secret` (needs.rules.json sameAs minus secret), and the replacement may draw it:
        // the catalogue has no record of it, so the record and label are "" and the host shows the kind's word. Phase 2 draws records (Motive).
        Assert.Equal(record.Length == 0, label.Length == 0);
        Assert.Equal(record.Length == 0, !I.Crowd.Records.Values.Any(r => r.kind == kind));
    }
}
