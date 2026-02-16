using JobResearchAgent;
using JobResearchAgent.Matching;
using JobResearchAgent.Services;
using OpenAI;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.AI;

// ✅ Set QuestPDF license ONCE at startup
QuestPDF.Settings.License = LicenseType.Community;

var builder = Host.CreateApplicationBuilder(args);

// Read connection string from config
var connectionString = builder.Configuration.GetConnectionString("Default");

// Register infrastructure
builder.Services.AddSingleton(new JobRepository(connectionString));

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var model = builder.Configuration["AI:Model"]!;

// Register OpenAI client
var openAIClient = new OpenAIClient(apiKey);
builder.Services.AddSingleton(openAIClient);

// Register sources (tools)
builder.Services.AddSingleton<JobFitScorer>();
builder.Services.AddSingleton<IJobSource, LinkedInSource>();
//builder.Services.AddSingleton<IJobSource, IndeedSource>();
builder.Services.AddSingleton<ResearchAgent>();
builder.Services.AddSingleton<AgentPolicy>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<MatchingAgent>();
builder.Services.AddSingleton<ResumeCustomizer>();
builder.Services.AddSingleton<PdfResumeExporter>();
builder.Services.AddSingleton<PdfCoverLetterExporter>();
builder.Services.AddSingleton<ICoverLetterService, CoverLetterService>();

// Register the Worker (the runtime loop)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();