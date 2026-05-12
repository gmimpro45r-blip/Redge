using FluentAssertions;
using Ledger.Core.Entities;
using Ledger.Core.Enums;
using Ledger.Core.Tests.Fakes;

namespace Ledger.Core.Tests;

public class JournalHeaderTests
{
    private static readonly Guid Org = Guid.NewGuid();
    private static readonly Guid User = Guid.NewGuid();
    private static readonly Guid Cash = Guid.NewGuid();
    private static readonly Guid Sales = Guid.NewGuid();
    private static readonly FixedClock Clock = new(new DateTimeOffset(2026, 5, 12, 0, 0, 0, TimeSpan.Zero));

    private static JournalHeader Draft() => JournalHeader.CreateDraft(
        orgId: Org,
        entryNo: "JE-0001",
        entryDate: new DateOnly(2026, 5, 12),
        currency: "USD",
        exchangeRate: 1m);

    [Fact]
    public void New_draft_is_unbalanced_and_empty()
    {
        var h = Draft();
        h.Status.Should().Be(EntryStatus.Draft);
        h.IsBalanced.Should().BeFalse();
        h.Lines.Should().BeEmpty();
    }

    [Fact]
    public void AddLine_rejects_both_debit_and_credit()
    {
        var h = Draft();
        var act = () => h.AddLine(Cash, debit: 100m, credit: 50m);
        act.Should().Throw<ArgumentException>().WithMessage("*both debit and credit*");
    }

    [Fact]
    public void AddLine_rejects_zero_amounts()
    {
        var h = Draft();
        var act = () => h.AddLine(Cash, debit: 0m, credit: 0m);
        act.Should().Throw<ArgumentException>().WithMessage("*non-zero*");
    }

    [Fact]
    public void Post_rejects_unbalanced_entry()
    {
        var h = Draft();
        h.AddLine(Cash, debit: 100m, credit: 0m);
        h.AddLine(Sales, debit: 0m, credit: 99m);

        var result = h.Post(Clock, User);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("UNBALANCED");
        h.Status.Should().Be(EntryStatus.Draft);
    }

    [Fact]
    public void Post_rejects_empty_entry()
    {
        var h = Draft();
        var result = h.Post(Clock, User);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("EMPTY");
    }

    [Fact]
    public void Post_balanced_entry_succeeds_and_freezes_status()
    {
        var h = Draft();
        h.AddLine(Cash, debit: 100m, credit: 0m, description: "cash in");
        h.AddLine(Sales, debit: 0m, credit: 100m, description: "sale");

        var result = h.Post(Clock, User);

        result.IsSuccess.Should().BeTrue();
        h.Status.Should().Be(EntryStatus.Posted);
        h.PostedAt.Should().Be(Clock.UtcNow);
        h.PostedBy.Should().Be(User);

        var editAfterPost = () => h.AddLine(Cash, 1m, 0m);
        editAfterPost.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Reversal_swaps_debit_and_credit_and_marks_original_reversed()
    {
        var h = Draft();
        h.AddLine(Cash, debit: 100m, credit: 0m);
        h.AddLine(Sales, debit: 0m, credit: 100m);
        h.Post(Clock, User);

        var reversal = h.CreateReversal(Clock, User, reversalEntryNo: "JE-0001-R");

        reversal.Status.Should().Be(EntryStatus.Reversal);
        reversal.ReversesId.Should().Be(h.Id);
        reversal.Lines.Should().HaveCount(2);
        reversal.Lines[0].Debit.Should().Be(0m);
        reversal.Lines[0].Credit.Should().Be(100m);
        reversal.Lines[1].Debit.Should().Be(100m);
        reversal.Lines[1].Credit.Should().Be(0m);
        reversal.IsBalanced.Should().BeTrue();

        h.Status.Should().Be(EntryStatus.Reversed);
        h.ReversedById.Should().Be(reversal.Id);
    }

    [Fact]
    public void Reversal_of_draft_throws()
    {
        var h = Draft();
        var act = () => h.CreateReversal(Clock, User, "X");
        act.Should().Throw<InvalidOperationException>().WithMessage("*posted entries*");
    }

    [Fact]
    public void Base_amounts_are_computed_with_exchange_rate()
    {
        var h = JournalHeader.CreateDraft(Org, "FX-1", new DateOnly(2026, 5, 12), "EUR", exchangeRate: 1.1m);
        var line = h.AddLine(Cash, debit: 100m, credit: 0m);
        line.DebitBase.Should().Be(110.0000m);
        line.CreditBase.Should().Be(0m);
    }
}
