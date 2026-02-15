using JobResearchAgent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Read connection string from config
var connectionString = builder.Configuration.GetConnectionString("Default");

// Register infrastructure
builder.Services.AddSingleton(new JobRepository(connectionString));

// Register sources (tools)
builder.Services.AddSingleton<IJobSource, LinkedInSource>();
//builder.Services.AddSingleton<IJobSource, IndeedSource>();

// Register agent
builder.Services.AddSingleton<ResearchAgent>();
// Register policy
builder.Services.AddSingleton<AgentPolicy>();

// Register the Worker (the runtime loop)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
