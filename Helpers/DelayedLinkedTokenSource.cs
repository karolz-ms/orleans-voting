namespace Voting.Helpers;

public class DelayedLinkedTokenSource
{
    private CancellationTokenSource _cts;

    public DelayedLinkedTokenSource(CancellationToken source, TimeSpan delay)
    {
        _cts = new CancellationTokenSource();
        source.Register(() => _cts.CancelAfter(delay));
    }

    public CancellationToken Token => _cts.Token;
}

public class ApplicationStoppingTokenSource: DelayedLinkedTokenSource
{
    public static readonly TimeSpan ShutdownDelay = TimeSpan.FromSeconds(3);

    public ApplicationStoppingTokenSource(IHostApplicationLifetime hostLifetime) :
        base(hostLifetime.ApplicationStopping, ShutdownDelay)
    { }
}
