using InsuranceApi.Models; //References the Enum.cs file for QuoteStatus and QuoteACtion

namespace InsuranceApi.Services;


public static class QuoteLifeCycle //Class container Start
{
    private static readonly Dictionary<QuoteAction, QuoteStatus[]> AllowedFrom = new() //Using Dictionary to record the actions (keys) and list of status (value) 
    {
        [QuoteAction.Quote] = new[] {QuoteStatus.Draft},
        [QuoteAction.Accept] = new[] {QuoteStatus.Quoted},
        [QuoteAction.Bind] = new[] {QuoteStatus.Accepted},
        [QuoteAction.Issue] = new[] {QuoteStatus.Bound},
        [QuoteAction.Discard] = new[] {QuoteStatus.Draft, QuoteStatus.Quoted, QuoteStatus.Accepted, QuoteStatus.Bound}

    };

    private static readonly Dictionary<QuoteAction, QuoteStatus> ResultOf = new()
    {
        [QuoteAction.Quote] = QuoteStatus.Quoted,
        [QuoteAction.Accept] = QuoteStatus.Accepted,
        [QuoteAction.Bind] = QuoteStatus.Bound,
        [QuoteAction.Issue] = QuoteStatus.Issued,
        [QuoteAction.Discard] = QuoteStatus.Discarded,

    };

    public static bool CanApply(QuoteAction action, QuoteStatus current) => AllowedFrom[action].Contains(current);

    public static QuoteStatus Apply(QuoteAction action, QuoteStatus current)
    {
        if (!CanApply(action, current))
            throw new ConflictException($"Cannot {action} a quote that is {current}.");

        return ResultOf[action];
    }

}
