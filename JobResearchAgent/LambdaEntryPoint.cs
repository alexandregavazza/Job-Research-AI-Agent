using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace JobResearchAgent;

public class LambdaEntryPoint
{
    public async Task FunctionHandler(ILambdaContext context)
    {
        var builder = ServiceRegistration.CreateBuilder(Array.Empty<string>());
        using var host = builder.Build();

        await host.StartAsync();

        var runner = host.Services.GetRequiredService<PipelineRunner>();
        await runner.RunAsync(CancellationToken.None);

        await host.StopAsync();
    }
}
