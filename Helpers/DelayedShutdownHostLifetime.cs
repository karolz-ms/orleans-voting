using System.Runtime.InteropServices;

namespace Voting.Helpers;

public class DelayedShutdownHostLifetime : IHostLifetime, IDisposable
{
    private IHostApplicationLifetime _applicationLifetime;
    private TimeSpan _delay;
    private IEnumerable<IDisposable> _disposables;

    public DelayedShutdownHostLifetime(IHostApplicationLifetime applicationLifetime, TimeSpan delay) { 
        _applicationLifetime = applicationLifetime;
        _delay = delay;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task WaitForStartAsync(CancellationToken cancellationToken)
    {
        Action<PosixSignalContext> handler = HandleSignal;
        var da = new IDisposable[3];
        da[0] = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
        da[1] = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
        da[2] = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handler);
        _disposables = da;
        return Task.CompletedTask;
    }

    protected void HandleSignal(PosixSignalContext ctx)
    {
        ctx.Cancel = true;
        Task.Delay(_delay).ContinueWith(t => { _applicationLifetime.StopApplication(); });
    }

    public void Dispose()
    {
        if (_disposables != null)
        {
            foreach (var disposable in _disposables) { disposable.Dispose(); }
        }
    }
}
