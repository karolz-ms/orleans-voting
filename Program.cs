using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Voting.Data;
using Voting.Helpers;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Reflection.Metadata.Ecma335;
using System.Numerics;

var builder = WebApplication.CreateBuilder(args);

builder.Host.ConfigureHostOptions(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(5));

builder.Host.UseOrleans((ctx, builder) =>
{
    if (ctx.HostingEnvironment.IsDevelopment())
    {
        // During development time, we don't want to have to deal with
        // storage emulators or other dependencies. Just "Hit F5" to run.
        builder.UseLocalhostClustering();
        builder.AddMemoryGrainStorage("votes");
        builder.UseDashboard(options => {
            options.Port = 8888;
        });
    }
    else
    {
        // In Kubernetes, we use environment variables and the pod manifest
        //orleansBuilder.UseKubernetesHosting();

        // Use Redis for clustering & persistence
        //var redisAddress = $"{Environment.GetEnvironmentVariable("REDIS")}:6379";
        //orleansBuilder.UseRedisClustering(options => options.ConnectionString = redisAddress);
        //orleansBuilder.AddRedisGrainStorage("votes", options => options.ConnectionString = redisAddress);
    }
});

// Add services to the container.
//builder.Services.AddSingleton<IHostLifetime>(sp => new DelayedShutdownHostLifetime(
//    sp.GetRequiredService<IHostApplicationLifetime>(), 
//    TimeSpan.FromSeconds(5)
//));
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<PollService>();
builder.Services.AddSingleton<DemoService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Map("/longop/{value}", async Task<Results<StatusCodeHttpResult, Ok<String>>> (int value, CancellationToken ct, [FromServices] IHostApplicationLifetime appLifetime) =>
{
    var effectiveCt = ct;
    // var effectiveCt = CancellationTokenSource.CreateLinkedTokenSource(ct, appLifetime.ApplicationStopping).Token;

    try
    {
    await Task.Delay(value * 1000, effectiveCt);
    } catch (OperationCanceledException)
    {
        return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }
    
    return TypedResults.Ok($"Worked {value} seconds, looks good");
});

app.Map("/longop2/{value}", Results<StatusCodeHttpResult, Ok<String>> (int value, CancellationToken ct, [FromServices] IHostApplicationLifetime appLifetime) =>
{
    var effectiveCt = ct;
    // var effectiveCt = CancellationTokenSource.CreateLinkedTokenSource(ct, appLifetime.ApplicationStopping).Token;
    DateTimeOffset deadline = DateTimeOffset.UtcNow + TimeSpan.FromSeconds(value);
    var rng = new Random();
    byte[] bytes = new byte[4];
    rng.NextBytes(bytes);
    var v = new BigInteger(bytes);

    try
    {
        while (DateTimeOffset.UtcNow < deadline)
        {
            for (int i = 0; i < 10_000; i++)
            {
                if (i % 2 == 0)
                {
                    v /= 2;
                } else
                {
                    v = v * 3 + 1;
                }
            }
        }
    }
    catch (OperationCanceledException)
    {
        return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);
    }

    return TypedResults.Ok($"Worked {value} seconds, final value is {v}");
});

app.Map("/shortop", Ok<String> () => {
    return TypedResults.Ok("Short, but sweeet success");
});

app.Run();
