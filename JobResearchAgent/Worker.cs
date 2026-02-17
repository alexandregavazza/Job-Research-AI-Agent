using JobResearchAgent.Matching;
using JobResearchAgent.Services;
using JobResearchAgent.Infrastructure;
using JobResearchAgent.Agents;
using Microsoft.Extensions.Options;
using JobResearchAgent.Application;

namespace JobResearchAgent;

/// <summary>
/// Background worker following SOLID principles with dependency injection
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ResearchAgent _agent;
    private readonly IJobRepository _repository;
    private readonly MatchingAgent _matchingAgent;
    private readonly ResumeCustomizer _resumeCustomizer;
    private readonly ResumeProfile _resume;
    private readonly PdfResumeExporter _pdfResumeExporter;
    private readonly ICoverLetterService _coverLetterService;
    private readonly PdfCoverLetterExporter _coverLetterExporter;
    private readonly MatchingConfiguration _matchingConfig;
    private readonly ApplicationAgent _applicationAgent;

    public Worker(
        ILogger<Worker> logger,
        ResearchAgent agent,
        IJobRepository repository,
        MatchingAgent matchingAgent,
        ResumeCustomizer resumeCustomizer,
        PdfResumeExporter pdfResumeExporter,
        ICoverLetterService coverLetterService,
        PdfCoverLetterExporter coverLetterExporter,
        IResumeLoader resumeLoader,
        IOptions<MatchingConfiguration> matchingConfig,
        ApplicationAgent applicationAgent)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agent = agent ?? throw new ArgumentNullException(nameof(agent));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _matchingAgent = matchingAgent ?? throw new ArgumentNullException(nameof(matchingAgent));
        _resumeCustomizer = resumeCustomizer ?? throw new ArgumentNullException(nameof(resumeCustomizer));
        _pdfResumeExporter = pdfResumeExporter ?? throw new ArgumentNullException(nameof(pdfResumeExporter));
        _coverLetterService = coverLetterService ?? throw new ArgumentNullException(nameof(coverLetterService));
        _coverLetterExporter = coverLetterExporter ?? throw new ArgumentNullException(nameof(coverLetterExporter));
        _matchingConfig = matchingConfig?.Value ?? throw new ArgumentNullException(nameof(matchingConfig));
        _applicationAgent = applicationAgent ?? throw new ArgumentNullException(nameof(applicationAgent));

        if (resumeLoader == null)
            throw new ArgumentNullException(nameof(resumeLoader));
            
        _resume = resumeLoader.Load();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Research Pipeline started.");

        // 1️⃣ Initialize semantic matcher (embeds your resume once)
        await _matchingAgent.InitializeAsync();

        // 2️⃣ Run the research agent (scrapes LinkedIn + Indeed)
        var jobs = await _agent.RunAsync();

        _logger.LogInformation("Found {Count} jobs", jobs.Count);

        // 3️⃣ Save raw jobs first (data lake concept)
        var qualifiedJobs = new List<JobPosting>();

        // 4️⃣ Evaluate each job semantically
        foreach (var job in jobs)
        {
            var result = await _matchingAgent.EvaluateAsync(job);
            job.MatchScore = result.Score;

            _logger.LogInformation(
                "JobId: {JobId} | Score: {Score:0}% | Decision: {Decision} | {Title}",
                job.ExternalJobId,
                result.Score,
                result.Decision,
                job.Title);
            
            // ✅ Only keep strong matches
            if (result.Score >= _matchingConfig.QualificationThreshold)
            {
                job.MatchScore = result.Score;
                qualifiedJobs.Add(job);

                var tailored = await _resumeCustomizer.CustomizeAsync(
                _resume.HumanText,
                job.Title,
                job.Description);

                await _repository.SaveTailoredResumeAsync(job, tailored);

                var pdfResumePath = _pdfResumeExporter.Export(job, tailored);
                _logger.LogInformation("Generated PDF resume at {PdfPath}", pdfResumePath);

                var coverLetter = await _coverLetterService.GenerateAsync(job, tailored);

                var pdfCoverLetterPath = _coverLetterExporter.Export(coverLetter);
                _logger.LogInformation("Generated PDF cover letter at {PdfPath}", pdfCoverLetterPath);
            
                /*await _applicationAgent.ExecuteAsync(
                    job,
                    pdfResumePath,
                    pdfCoverLetterPath,
                    result.Score,
                    stoppingToken);*/
            }
        }

        _logger.LogInformation("Pipeline finished.");
        _logger.LogInformation(
            "Qualified {QualifiedCount} jobs out of {Total}",
            qualifiedJobs.Count,
            jobs.Count);

        // 4️⃣ Persist ONLY qualified jobs
        if (qualifiedJobs.Any())
        {
            await _repository.SaveAsync(qualifiedJobs);
        }

        // Stop the worker after one run (important!)
        Environment.Exit(0);
    }
}