using VotingContract;

namespace Voting.Data;

public partial class DemoService
{
    private readonly IGrainFactory _grainFactory;
    private readonly ILogger<DemoService> _logger;
    private readonly IHostApplicationLifetime _hostLifetime;

    public DemoService(IGrainFactory grainFactory, ILogger<DemoService> logger, IHostApplicationLifetime hostLifetime)
    {
        _grainFactory = grainFactory;
        _logger = logger;
        _hostLifetime = hostLifetime;
    }

    public async Task SimulateVoters(string pollId, int numVotes)
    {
        _hostLifetime.ApplicationStopping.ThrowIfCancellationRequested();

        try
        {
            var pollGrain = _grainFactory.GetGrain<IPollGrain>(pollId);
            var results = await pollGrain.GetCurrentResults();
            var random = Random.Shared;
            while (numVotes-- > 0)
            {
                var optionId = random.Next(0, results.Options.Count);
                await pollGrain.AddVote(optionId);

                // Wait some time.
                await Task.Delay(random.Next(100, 1000));
                _hostLifetime.ApplicationStopping.ThrowIfCancellationRequested();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error while simulating voters");
        }
    }
}
