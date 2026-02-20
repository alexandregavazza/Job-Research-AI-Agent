using Amazon.S3;
using JobResearchAgent;
using JobResearchAgent.Agents;
using JobResearchAgent.Application;
using JobResearchAgent.Infrastructure;
using JobResearchAgent.Infrastructure.Automation;
using JobResearchAgent.Matching;
using JobResearchAgent.Models;
using JobResearchAgent.Services;
using JobResearchAgent.Services.CoverLetter;
using JobResearchAgent.Services.FileManipulator;
using JobResearchAgent.Services.Prompting;
using JobResearchAgent.Services.Resume;
using JobResearchAgent.Services.Storage;
using OpenAI;

namespace JobResearchAgent;

internal static class ServiceRegistration
{
    public static HostApplicationBuilder CreateBuilder(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Read connection string from config
        var connectionString = builder.Configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Database connection string 'Default' is not configured.");

        // Register infrastructure with interfaces
        builder.Services.AddSingleton<IJobRepository>(new JobRepository(connectionString));
        builder.Services.AddSingleton<IApplicationLogRepository>(new ApplicationLogRepository(connectionString));

        // Register core services with interfaces
        builder.Services.AddSingleton<IResumeLoader, ResumeLoader>();
        builder.Services.AddSingleton<IFileSanitizer, FileSanitizer>();

        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
        var model = builder.Configuration["AI:Model"]
            ?? throw new InvalidOperationException("AI:Model configuration is missing.");

        // Register OpenAI client
        var openAIClient = new OpenAIClient(apiKey);
        builder.Services.AddSingleton(openAIClient);
        builder.Services.AddSingleton<IChatCompletionClient, OpenAIChatCompletionClient>();

        builder.Services.Configure<LanguageDetectionOptions>(
            builder.Configuration.GetSection("LanguageDetection"));
        builder.Services.AddSingleton<LanguageDetector>();

        // Register prompt service
        builder.Services.AddSingleton<IPromptService, PromptService>();

        // Register sources (tools)
        builder.Services.AddSingleton<JobFitScorer>();
        builder.Services.AddSingleton<IJobSource, LinkedInSource>();
        //builder.Services.AddSingleton<IJobSource, IndeedSource>();
        builder.Services.AddSingleton<ResearchAgent>();
        builder.Services.AddSingleton<EmbeddingService>();
        builder.Services.AddSingleton<MatchingAgent>();
        builder.Services.AddSingleton<IResumeCustomizer, ResumeCustomizer>();
        builder.Services.AddSingleton<PdfResumeExporter>();
        builder.Services.AddSingleton<PdfCoverLetterExporter>();
        builder.Services.AddSingleton<ICoverLetterService, CoverLetterService>();
        builder.Services.AddSingleton<ApplicationAgent>();
        builder.Services.AddSingleton<IBrowserAutomation, PlaywrightAutomation>();
        builder.Services.AddSingleton<IApplicationAutomation, IndeedAutomation>();
        builder.Services.AddSingleton<IApplicationAutomation, LinkedInAutomation>();
        builder.Services.AddSingleton<PipelineRunner>();
        
        // Only register S3 services if S3 bucket is configured
        var s3Bucket = builder.Configuration["Storage:S3Bucket"];
        if (!string.IsNullOrWhiteSpace(s3Bucket))
        {
            builder.Services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client());
            builder.Services.AddSingleton<IDocumentStorage, S3DocumentStorage>();
        }
        else
        {
            builder.Services.AddSingleton<IDocumentStorage, LocalDocumentStorage>();
        }

        // Configure agent policies and matching thresholds
        builder.Services.Configure<AgentPolicy>(
            builder.Configuration.GetSection("AgentPolicy"));
        builder.Services.Configure<MatchingConfiguration>(
            builder.Configuration.GetSection("MatchingConfiguration"));
        builder.Services.Configure<ApplicationPolicy>(
            builder.Configuration.GetSection("ApplicationPolicy"));
        builder.Services.Configure<AutomationOptions>(
            builder.Configuration.GetSection("Automation"));
        builder.Services.Configure<BrowserAutomationOptions>(
            builder.Configuration.GetSection("BrowserAutomation"));
        builder.Services.Configure<IndeedAutomationOptions>(
            builder.Configuration.GetSection("IndeedAutomation"));
        builder.Services.Configure<StorageOptions>(
            builder.Configuration.GetSection("Storage"));

        return builder;
    }
}