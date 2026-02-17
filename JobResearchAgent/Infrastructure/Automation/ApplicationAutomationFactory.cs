using JobResearchAgent.Application;

namespace JobResearchAgent.Infrastructure.Automation;
public class ApplicationAutomationFactory
{
    private readonly IServiceProvider _provider;

    public ApplicationAutomationFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public IApplicationAutomation Resolve(JobPosting job)
    {
        return job.Source switch
        {
            "LinkedIn" => _provider.GetRequiredService<LinkedInAutomation>(),
            "Indeed" => _provider.GetRequiredService<IndeedAutomation>(),
            _ => throw new NotSupportedException($"No automation for {job.Source}")
        };
    }
}