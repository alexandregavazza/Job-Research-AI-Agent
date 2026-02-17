using JobResearchAgent;
using JobResearchAgent.Matching;
using JobResearchAgent.Services;
using OpenAI;
using QuestPDF.Infrastructure;

// ✅ Set QuestPDF license ONCE at startup
QuestPDF.Settings.License = LicenseType.Community;

var builder = Host.CreateApplicationBuilder(args);

// Read connection string from config
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Database connection string 'Default' is not configured.");

// Register infrastructure
builder.Services.AddSingleton(new JobRepository(connectionString));

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
var model = builder.Configuration["AI:Model"]
    ?? throw new InvalidOperationException("AI:Model configuration is missing.");

// Register OpenAI client
var openAIClient = new OpenAIClient(apiKey);
builder.Services.AddSingleton(openAIClient);

// Register sources (tools)
builder.Services.AddSingleton<JobFitScorer>();
builder.Services.AddSingleton<IJobSource, LinkedInSource>();
//builder.Services.AddSingleton<IJobSource, IndeedSource>();
builder.Services.AddSingleton<ResearchAgent>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<MatchingAgent>();
builder.Services.AddSingleton<ResumeCustomizer>();
builder.Services.AddSingleton<PdfResumeExporter>();
builder.Services.AddSingleton<PdfCoverLetterExporter>();
builder.Services.AddSingleton<ICoverLetterService, CoverLetterService>();
builder.Services.Configure<AgentPolicy>(
    builder.Configuration.GetSection("AgentPolicy"));

// Register the Worker (the runtime loop)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();