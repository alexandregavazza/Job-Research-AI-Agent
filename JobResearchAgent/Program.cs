using JobResearchAgent;
using JobResearchAgent.Matching;
using JobResearchAgent.Services;
using JobResearchAgent.Services.CoverLetter;
using JobResearchAgent.Infrastructure;
using OpenAI;
using QuestPDF.Infrastructure;
using JobResearchAgent.Services.FileManipulator;
using JobResearchAgent.Application;
using JobResearchAgent.Infrastructure.Automation;
using JobResearchAgent.Models;
using JobResearchAgent.Agents;

// ✅ Set QuestPDF license ONCE at startup
QuestPDF.Settings.License = LicenseType.Community;

var builder = ServiceRegistration.CreateBuilder(args);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();