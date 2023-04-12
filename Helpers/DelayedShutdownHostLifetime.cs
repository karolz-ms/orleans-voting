#nullable enable
using System.Runtime.InteropServices;

namespace Voting.Helpers;

public class DelayedShutdownHostLifetime : IHostLifetime, IDisposable
{
    private IHostApplicationLifetime _applicationLifetime;
    private TimeSpan _delay;
    private IEnumerable<IDisposable>? _disposables;

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
        _disposables = new IDisposable[]
        {
            PosixSignalRegistration.Create(PosixSignal.SIGINT, HandleSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGQUIT, HandleSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, HandleSignal)
        };
        return Task.CompletedTask;
    }

    protected void HandleSignal(PosixSignalContext ctx)
    {
        ctx.Cancel = true;
        Task.Delay(_delay).ContinueWith(t =>  _applicationLifetime.StopApplication());
    }

    public void Dispose()
    {
        foreach (var disposable in _disposables ?? Enumerable.Empty<IDisposable>()) 
        { 
            disposable.Dispose(); 
        }
    }
}
