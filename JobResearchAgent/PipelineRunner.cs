using JobResearchAgent.Agents;
using JobResearchAgent.Application;
using JobResearchAgent.Infrastructure;
using JobResearchAgent.Matching;
using JobResearchAgent.Models;
using JobResearchAgent.Services.CoverLetter;
using JobResearchAgent.Services.Resume;
using JobResearchAgent.Services.Storage;
using Microsoft.Extensions.Options;

namespace JobResearchAgent;

public class PipelineRunner
{
    private readonly ILogger<PipelineRunner> _logger;
    private readonly ResearchAgent _agent;
    private readonly IJobRepository _repository;
    private readonly MatchingAgent _matchingAgent;
    private readonly IResumeCustomizer _resumeCustomizer;
    private readonly ResumeProfile _resume;
    private readonly PdfResumeExporter _pdfResumeExporter;
    private readonly ICoverLetterService _coverLetterService;
    private readonly PdfCoverLetterExporter _coverLetterExporter;
    private readonly MatchingConfiguration _matchingConfig;
    private readonly ApplicationAgent _applicationAgent;
    private readonly IApplicationLogRepository _applicationLogRepository;
    private readonly ApplicationPolicy _applicationPolicy;
    private readonly IDocumentStorage _documentStorage;

    public PipelineRunner(
        ILogger<PipelineRunner> logger,
        ResearchAgent agent,
        IJobRepository repository,
        MatchingAgent matchingAgent,
        IResumeCustomizer resumeCustomizer,
        PdfResumeExporter pdfResumeExporter,
        ICoverLetterService coverLetterService,
        PdfCoverLetterExporter coverLetterExporter,
        IResumeLoader resumeLoader,
        IOptions<MatchingConfiguration> matchingConfig,
        ApplicationAgent applicationAgent,
        IApplicationLogRepository applicationLogRepository,
        IOptions<ApplicationPolicy> applicationPolicy,
        IDocumentStorage documentStorage)
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
        _applicationLogRepository = applicationLogRepository ?? throw new ArgumentNullException(nameof(applicationLogRepository));
        _applicationPolicy = applicationPolicy?.Value ?? throw new ArgumentNullException(nameof(applicationPolicy));
        _documentStorage = documentStorage ?? throw new ArgumentNullException(nameof(documentStorage));

        if (resumeLoader == null)
            throw new ArgumentNullException(nameof(resumeLoader));

        _resume = resumeLoader.Load();
    }

    public async Task RunAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Job Research Pipeline started.");

        // 1) Initialize semantic matcher (embeds your resume once)
        await _matchingAgent.InitializeAsync();

        // 2) Run the research agent (scrapes LinkedIn + Indeed)
        var jobs = await _agent.RunAsync();

        _logger.LogInformation("Found {Count} jobs", jobs.Count);

        // 3) Save raw jobs first (data lake concept)
        var qualifiedJobs = new List<JobPosting>();

        // 4) Evaluate each job semantically
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

            // Only keep strong matches
            if (result.Score >= _matchingConfig.QualificationThreshold)
            {
                job.MatchScore = result.Score;
                qualifiedJobs.Add(job);

                // Testing only: restrict automation to a single company if configured.
                /*if (!string.IsNullOrWhiteSpace(_applicationPolicy.AllowedCompany)
                    && !job.Company.Contains(_applicationPolicy.AllowedCompany, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Skipping job {JobId} because company does not match test filter: {Company}",
                        job.ExternalJobId,
                        _applicationPolicy.AllowedCompany);
                    continue;
                }*/

                if (!string.IsNullOrWhiteSpace(job.ExternalJobId)
                    && await _applicationLogRepository.WasJobInsertedWithinDaysAsync(
                        job.ExternalJobId,
                        job.Title,
                        job.Company,
                        job.Location,
                        15,
                        stoppingToken))
                {
                    _logger.LogInformation(
                        "Skipping resume and cover letter for recent job {JobId} (matched by external_job_id or title+company+location)",
                        job.ExternalJobId);
                    continue;
                }

                var tailored = await _resumeCustomizer.CustomizeAsync(
                    _resume.HumanText,
                    job.Title,
                    job.Description);

                await _repository.SaveTailoredResumeAsync(job, tailored);

                var pdfResumePath = _pdfResumeExporter.Export(job, tailored);
                _logger.LogInformation("Generated PDF resume at {PdfPath}", pdfResumePath);
                var resumeLocation = await _documentStorage.StoreAsync(pdfResumePath, stoppingToken);
                _logger.LogInformation("Stored resume at {Location}", resumeLocation);

                var coverLetter = await _coverLetterService.GenerateAsync(job, tailored);

                var pdfCoverLetterPath = _coverLetterExporter.Export(coverLetter);
                _logger.LogInformation("Generated PDF cover letter at {PdfPath}", pdfCoverLetterPath);
                var coverLetterLocation = await _documentStorage.StoreAsync(pdfCoverLetterPath, stoppingToken);
                _logger.LogInformation("Stored cover letter at {Location}", coverLetterLocation);

                var log = new ApplicationLog
                {
                    ExternalJobId = job.ExternalJobId ?? "unknown",
                    JobTitle = job.Title,
                    Company = job.Company,
                    Location = job.Location,
                    Url = job.Url,
                    Source = job.Source,
                    ResumePath = resumeLocation,
                    CoverLetterPath = coverLetterLocation,
                    MatchScore = result.Score,
                    Status = "applied",
                    Notes = "Dry run: application automation disabled"
                };

                await _applicationLogRepository.InsertAsync(log, stoppingToken);

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

        // Persist ONLY qualified jobs
        if (qualifiedJobs.Any())
        {
            await _repository.SaveAsync(qualifiedJobs);
        }
    }
}
