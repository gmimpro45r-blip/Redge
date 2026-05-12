namespace Ledger.Core.Enums;

/// <summary>
/// Lifecycle of a <see cref="Ledger.Core.Entities.JournalHeader"/>.
/// <para><c>Draft</c>      — editable, not part of any report.</para>
/// <para><c>Posted</c>     — immutable; appears in trial balance &amp; reports.</para>
/// <para><c>Reversed</c>   — was posted, then cancelled by a reversal entry.</para>
/// <para><c>Reversal</c>   — auto-generated entry that cancels another posted entry.</para>
/// </summary>
public enum EntryStatus
{
    Draft,
    Posted,
    Reversed,
    Reversal,
}
