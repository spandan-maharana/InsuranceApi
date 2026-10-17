using System;
using System.Collections.Generic;
using System.Text;
using InsuranceApi.Models;
using InsuranceApi.Services;
using Xunit;

namespace InsuranceApi.Tests;

public class QuoteLifeCycleTests
{
    // ---------- MOVES THAT ARE ALLOWED (7 of them) ----------
    // Format: action, status the quote is in now, status it should become

    [Theory]
    [InlineData(QuoteAction.Quote, QuoteStatus.Draft, QuoteStatus.Quoted)]
    [InlineData(QuoteAction.Accept, QuoteStatus.Quoted, QuoteStatus.Accepted)]
    [InlineData(QuoteAction.Bind, QuoteStatus.Accepted, QuoteStatus.Bound)]
    [InlineData(QuoteAction.Issue, QuoteStatus.Bound, QuoteStatus.Issued)]
    [InlineData(QuoteAction.Discard, QuoteStatus.Draft, QuoteStatus.Discarded)]
    [InlineData(QuoteAction.Discard, QuoteStatus.Quoted, QuoteStatus.Discarded)]
    [InlineData(QuoteAction.Discard, QuoteStatus.Accepted, QuoteStatus.Discarded)]
    public void Allowed_move_gives_the_new_status(QuoteAction action, QuoteStatus current, QuoteStatus expected)
    {
        Assert.True(QuoteLifeCycle.CanApply(action, current));
        Assert.Equal(expected, QuoteLifeCycle.Apply(action, current));
    }

    // ---------- MOVES THAT ARE NOT ALLOWED ----------

    [Theory]
    [InlineData(QuoteAction.Quote, QuoteStatus.Quoted)]    // quote it twice
    [InlineData(QuoteAction.Quote, QuoteStatus.Issued)]
    [InlineData(QuoteAction.Accept, QuoteStatus.Draft)]     // skipping the Quoted step
    [InlineData(QuoteAction.Accept, QuoteStatus.Discarded)]
    [InlineData(QuoteAction.Bind, QuoteStatus.Draft)]
    [InlineData(QuoteAction.Bind, QuoteStatus.Quoted)]
    [InlineData(QuoteAction.Issue, QuoteStatus.Accepted)]  // issuing before Bound
    [InlineData(QuoteAction.Discard, QuoteStatus.Bound)]
    [InlineData(QuoteAction.Discard, QuoteStatus.Issued)]    // too late, cancel the policy instead
    public void Blocked_move_throws_ConflictException(QuoteAction action, QuoteStatus current)
    {
        Assert.False(QuoteLifeCycle.CanApply(action, current));
        Assert.Throws<ConflictException>(() => QuoteLifeCycle.Apply(action, current));
    }

    // ---------- FINAL STATUSES: NOTHING CAN BE DONE ----------

    [Theory]
    [InlineData(QuoteStatus.Issued)]
    [InlineData(QuoteStatus.Expired)]
    [InlineData(QuoteStatus.Discarded)]
    public void Final_status_allows_no_action_at_all(QuoteStatus finalStatus)
    {
        foreach (QuoteAction action in Enum.GetValues<QuoteAction>())
        {
            Assert.False(QuoteLifeCycle.CanApply(action, finalStatus));
            Assert.Throws<ConflictException>(() => QuoteLifeCycle.Apply(action, finalStatus));
        }
    }

    // ---------- THE ERROR MESSAGE ----------

    [Fact]
    public void Error_message_names_the_action_and_the_status()
    {
        var ex = Assert.Throws<ConflictException>(
            () => QuoteLifeCycle.Apply(QuoteAction.Accept, QuoteStatus.Draft));

        Assert.Equal("Cannot Accept a quote that is Draft.", ex.Message);
    }
}
