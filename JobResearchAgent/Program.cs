using JobResearchAgent;
using JobResearchAgent.Matching;
using JobResearchAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI;

var builder = Host.CreateApplicationBuilder(args);

// Read connection string from config
var connectionString = builder.Configuration.GetConnectionString("Default");

// Register infrastructure
builder.Services.AddSingleton(new JobRepository(connectionString));

var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

builder.Services.AddSingleton(new OpenAIClient(apiKey));
builder.Services.AddSingleton<JobFitScorer>();

// Register sources (tools)
builder.Services.AddSingleton<IJobSource, LinkedInSource>();
//builder.Services.AddSingleton<IJobSource, IndeedSource>();

// Register agent
builder.Services.AddSingleton<ResearchAgent>();
// Register policy
builder.Services.AddSingleton<AgentPolicy>();
builder.Services.AddSingleton<EmbeddingService>();
builder.Services.AddSingleton<MatchingAgent>();

// Register the Worker (the runtime loop)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
