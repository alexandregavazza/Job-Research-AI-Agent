using JobResearchAgent;
using JobResearchAgent.Matching;
using JobResearchAgent.Services;
using OpenAI;
using QuestPDF.Infrastructure;

// ✅ Set QuestPDF license ONCE at startup
QuestPDF.Settings.License = LicenseType.Community;

var builder = Host.CreateApplicationBuilder(args);

// Read connection string from config
var connectionString = builder.Configuration.GetConnectionString("Default");

// Register infrastructure
builder.Services.AddSingleton(new JobRepository(connectionString));

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

// Register sources (tools)
builder.Services.AddSingleton(new OpenAIClient(apiKey));
builder.Services.AddSingleton<JobFitScorer>();
builder.Services.AddSingleton<IJobSource, LinkedInSource>();
//builder.Services.AddSingleton<IJobSource, IndeedSource>();
builder.Services.AddSingleton<ResearchAgent>();
builder.Services.AddSingleton<AgentPolicy>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<MatchingAgent>();
builder.Services.AddSingleton<ResumeCustomizer>();
builder.Services.AddSingleton<PdfResumeExporter>();

// Register the Worker (the runtime loop)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
