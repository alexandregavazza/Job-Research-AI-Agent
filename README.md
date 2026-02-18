# Job Research AI Agent

## Overview
Job Research AI Agent is an automated end-to-end pipeline for discovering, collecting, semantically matching, and applying to job postings using AI and LLMs. It scrapes job boards (LinkedIn, Indeed), filters and scores jobs, generates tailored resumes and cover letters, and stores results in a PostgreSQL database. The system follows SOLID principles, is fully modular and extensible, and leverages OpenAI for semantic matching, deep job fit analysis, and document customization.

---

## Architecture

- **Program.cs**: Entry point. Configures dependency injection (following DIP), loads configuration, registers all services with their interfaces, and starts the background worker.
- **Worker.cs**: Main pipeline orchestrator. Workflow: initializes semantic matcher, runs job research agent, evaluates jobs, generates tailored resumes and cover letters for qualified matches, and saves results.
- **Agents/**: Core agent logic and job source integrations.
  - **ResearchAgent.cs**: Orchestrates job search across all sources, builds search queries, applies hard filters, and aggregates results.
  - **AgentPolicy.cs**: Defines search policy (countries, keywords, levels, remote/hybrid filter, max job age).
  - **IJobSource.cs**: Interface for job board sources (follows ISP).
  - **LinkedInSource.cs**: Scrapes LinkedIn jobs using Playwright, applies policy filters, and parses job postings.
  - **IndeedSource.cs**: Scrapes Indeed jobs using Playwright, applies policy filters, and parses job postings.
- **Infrastructure/**: Data persistence and browser automation.
  - **JobRepository.cs**: Persists job postings to PostgreSQL using Dapper. Handles upserts (no duplicates on external_job_id).
  - **IJobRepository.cs**: Interface for job data persistence (follows DIP).
  - **ApplicationLogRepository.cs**: Persists application logs and checks for duplicate applications.
  - **IApplicationLogRepository.cs**: Interface for application log persistence.
  - **Automation/**:
    - **PlaywrightAutomation.cs**: Browser automation wrapper implementing IBrowserAutomation.
    - **IBrowserAutomation.cs**: Interface for browser automation operations.
    - **IndeedAutomation.cs**: Indeed-specific application automation logic.
    - **LinkedInAutomation.cs**: LinkedIn-specific application automation logic.
    - **ApplicationAutomationFactory.cs**: Factory for resolving job source-specific automation.
    - **BrowserAutomationOptions.cs**: Configuration for browser automation settings.
    - **IndeedAutomationOptions.cs**: Indeed-specific automation configuration.
- **Matching/**: Resume loading, semantic matching, and job fit scoring.
  - **MatchingAgent.cs**: Loads resume, generates embeddings, evaluates jobs for semantic similarity, invokes deep LLM scoring.
  - **IResumeLoader.cs**: Interface for loading resume profiles (follows DIP).
  - **ResumeLoader.cs**: Loads both human and AI-structured resumes from the Profiles directory.
  - **ResumeProfile.cs**: Model for resume text (human and AI versions).
  - **MatchingConfiguration.cs**: Configuration for matching thresholds (follows OCP).
  - **Similarity.cs**: Provides cosine similarity calculation for embeddings.
  - **JobMatchResult.cs**: Holds the result of a job match (score, decision, reason).
- **Application/**: Application automation and execution.
  - **ApplicationAgent.cs**: Orchestrates the application process for qualified jobs.
  - **IApplicationAutomation.cs**: Interface for job application automation strategies.
  - **ApplicationResult.cs**: Result model for application attempts.
  - **ApplicationPolicy.cs**: Configuration for application automation behavior.
  - **AutomationOptions.cs**: General automation configuration options.
- **Models/**: Domain models.
  - **JobPosting.cs**: Represents a job posting (title, company, location, url, description, source, timestamps, external id, match score).
  - **TailoredResume.cs**: Model for AI-customized resume content.
  - **GeneratedCoverLetter.cs**: Model for AI-generated cover letters.
  - **ApplicationLog.cs**: Model for tracking job applications.
  - **ApplicationPolicy.cs**: Configuration model for application behavior.
- **Services/**: Core services for AI operations and document generation.
  - **EmbeddingService.cs**: Generates text embeddings using OpenAI (Semantic Kernel).
  - **JobFitScorer.cs**: Uses OpenAI LLM to deeply score job fit and provide reasoning.
  - **ResumeCustomizer.cs**: Uses OpenAI LLM to generate tailored resumes for specific jobs.
  - **PdfResumeExporter.cs**: Exports tailored resumes to PDF using QuestPDF.
  - **CoverLetter/**:
    - **ICoverLetterService.cs**: Interface for cover letter generation (follows DIP).
    - **CoverLetterService.cs**: Uses OpenAI LLM to generate customized cover letters.
    - **PdfCoverLetterExporter.cs**: Exports cover letters to PDF using QuestPDF.
  - **FileManipulator/**:
    - **IFileSanitizer.cs**: Interface for filename sanitization (follows ISP).
    - **FileSanitizer.cs**: Sanitizes filenames for safe file system operations.
- **Profiles/**:
  - **resume.human.txt**: Your full human-readable resume.
  - **resume.ai.txt**: A structured, skill-focused version of your resume for embeddings.

---

## Pipeline Steps

1. **Initialize**: Loads configuration, resumes, and initializes embedding models (Semantic Kernel with OpenAI).
2. **Job Search**: ResearchAgent queries all configured sources (LinkedIn, Indeed) using AgentPolicy criteria.
3. **Filtering**: Hard filters applied server-side (keywords, countries, seniority levels, remote/hybrid, max age).
4. **Semantic Matching**: Each job description is embedded and compared to the resume using cosine similarity.
5. **Deep Scoring**: Jobs passing the semantic threshold (default 0.35) are scored by the LLM for precise fit and reasoning (0-100 scale).
6. **Qualification**: Jobs scoring >= 70 are considered qualified matches.
7. **Resume Customization**: For each qualified job, an AI agent generates a tailored resume highlighting relevant experience.
8. **Cover Letter Generation**: An AI agent generates a customized cover letter for each qualified job.
9. **PDF Export**: Tailored resumes and cover letters are exported to PDF format using QuestPDF.
10. **Application Logging**: Application logs are stored in the database to prevent duplicate applications.
11. **Persistence**: Qualified jobs and their associated tailored documents are saved to the PostgreSQL database.
12. **Optional Automation**: ApplicationAgent can automate job applications using browser automation (configurable, currently disabled by default).

---

## Configuration

### Required Configuration (appsettings.json)
- **ConnectionStrings:Default**: PostgreSQL connection string
- **AI:Model**: OpenAI model for LLM operations (e.g., "gpt-4")
- **AI:EmbeddingModel**: OpenAI embedding model (e.g., "text-embedding-ada-002")
- **Output:BasePath**: Base directory for generated PDF documents
- **Candidate:*** : Your personal information (name, email, phone, LinkedIn, location, education)

### Environment Variables
- **OPENAI_API_KEY**: Your OpenAI API key (required)

### Optional Configuration
- **AgentPolicy**: Job search criteria (countries, keywords, seniority levels, remote/hybrid filter, max age)
- **MatchingConfiguration**: Matching thresholds (MinimumSimilarityThreshold, ApplyThreshold, ReviewThreshold, QualificationThreshold)
- **ApplicationPolicy**: Application automation settings (Enabled, RequireApproval, DelayBetweenApplicationsSeconds, AllowedCompany for testing)
- **BrowserAutomation**: Browser settings for automation (Headless, SlowMo, Timeout, UserDataDir, Viewport)
- **IndeedAutomation**: Indeed-specific automation selectors

### Example appsettings.json Structure
```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=jobresearch;Username=user;Password=pass"
  },
  "AI": {
    "Model": "gpt-4",
    "EmbeddingModel": "text-embedding-ada-002"
  },
  "Output": {
    "BasePath": "C:\\JobApplications"
  },
  "Candidate": {
    "FullName": "Your Name",
    "Email": "your.email@example.com",
    "Phone": "+1-234-567-8900",
    "LinkedIn": "linkedin.com/in/yourprofile",
    "Location": "City, State/Country",
    "Education": ["Degree, University, Year"]
  },
  "AgentPolicy": {
    "MaxAgeHours": 48,
    "RemoteOnly": false,
    "AllowHybrid": true,
    "Countries": ["United States", "Canada"],
    "Keywords": [".NET", "C#", "Azure"],
    "Levels": ["Senior", "Lead"]
  },
  "MatchingConfiguration": {
    "MinimumSimilarityThreshold": 0.35,
    "ApplyThreshold": 75,
    "ReviewThreshold": 60,
    "QualificationThreshold": 70
  },
  "ApplicationPolicy": {
    "Enabled": false,
    "RequireApproval": true,
    "AutoSubmit": false,
    "DelayBetweenApplicationsSeconds": 30,
    "AllowedCompany": ""
  }
}
```

---

## SOLID Principles

This project strictly follows SOLID principles for maintainability, testability, and extensibility:

- **Single Responsibility Principle (SRP)**: Each class has one clear purpose (e.g., FileSanitizer only sanitizes filenames, ResumeLoader only loads resumes).
- **Open/Closed Principle (OCP)**: Behavior is configurable through appsettings.json without modifying code (e.g., MatchingConfiguration thresholds).
- **Liskov Substitution Principle (LSP)**: All implementations can be substituted for their interfaces (e.g., any IJobSource can replace LinkedInSource or IndeedSource).
- **Interface Segregation Principle (ISP)**: Focused, minimal interfaces (IJobRepository, IResumeLoader, IFileSanitizer, ICoverLetterService).
- **Dependency Inversion Principle (DIP)**: All classes depend on abstractions (interfaces), not concrete implementations. Constructor injection is used throughout.

### Benefits
- ✅ **Testability**: All dependencies can be mocked for unit testing
- ✅ **Maintainability**: Clear separation of concerns, easy to understand and modify
- ✅ **Extensibility**: New features can be added without modifying existing code
- ✅ **Flexibility**: Implementations can be swapped (e.g., switch from PostgreSQL to MongoDB by implementing IJobRepository)

---

## Extending

### Add New Job Sources
Implement the `IJobSource` interface:
```csharp
public class CustomJobSource : IJobSource
{
    public async Task<IEnumerable<JobPosting>> SearchAsync(string keyword)
    {
        // Your implementation
    }
}
```
Then register it in Program.cs:
```csharp
builder.Services.AddSingleton<IJobSource, CustomJobSource>();
```

### Add New Application Automation
Implement `IApplicationAutomation` for a new job board, then register it in Program.cs.

### Customize Matching Logic
Modify `MatchingConfiguration` thresholds in appsettings.json or extend `MatchingAgent` for custom scoring logic.

### Customize Document Generation
- Modify prompts in `ResumeCustomizer.cs` or `CoverLetterService.cs`
- Extend `PdfResumeExporter.cs` or `PdfCoverLetterExporter.cs` for custom PDF layouts

### Add New Repositories
Implement `IJobRepository` or `IApplicationLogRepository` for different data stores (e.g., MongoDB, SQL Server).

---

## Requirements
- .NET 9.0+
- PostgreSQL database
- OpenAI API key
- Playwright (automatically installed via NuGet)
- QuestPDF Community License (automatically configured)

### NuGet Packages
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.AI.OpenAI
- Microsoft.SemanticKernel
- Microsoft.SemanticKernel.Connectors.OpenAI
- OpenAI
- Microsoft.Playwright
- Dapper
- Npgsql
- QuestPDF

---

## Usage

### Initial Setup
1. **Configure Database**: Create a PostgreSQL database for job storage
2. **Set Environment Variable**: 
   ```powershell
   $env:OPENAI_API_KEY = "your-api-key-here"
   ```
3. **Create Resume Files**: Place your resumes in `Profiles/`:
   - `resume.human.txt`: Your full resume (for LLM consumption)
   - `resume.ai.txt`: Structured version emphasizing skills (for embeddings)
4. **Configure appsettings.json**: Update connection string, AI models, output path, and candidate information
5. **Install Playwright**: 
   ```sh
   pwsh JobResearchAgent/bin/Debug/net9.0/playwright.ps1 install
   ```

### Running the Application
```sh
dotnet run --project JobResearchAgent/JobResearchAgent.csproj
```

### Expected Workflow
1. Application starts and loads configuration
2. Initializes semantic matcher (embeds your resume)
3. Scrapes job boards based on AgentPolicy
4. Evaluates each job for semantic similarity
5. Scores qualified jobs with LLM (70+ threshold)
6. Generates tailored resume and cover letter for each qualified job
7. Exports documents to PDF in dated folders
8. Logs applications to prevent duplicates
9. Saves results to database
10. Application exits after one complete run

### Output Structure
```
C:\JobApplications\
  2026-02-18\
    CompanyName_JobTitle_Resume.pdf
    CompanyName_JobTitle_CoverLetter.pdf
```

---

## Component Details

### Core Components

#### Program.cs
- Configures dependency injection container following SOLID principles
- Registers all services with their corresponding interfaces
- Loads configuration from appsettings.json
- Validates required environment variables (OPENAI_API_KEY)
- Configures options pattern for all policy and configuration classes
- Sets QuestPDF license
- Starts the background worker service

#### Worker.cs
- Main orchestrator implementing BackgroundService
- Constructor injection with proper null validation
- Workflow steps:
  1. Initialize MatchingAgent (embeds resume once)
  2. Run ResearchAgent to collect jobs
  3. Evaluate each job semantically and with LLM scoring
  4. For qualified jobs (score >= 70):
     - Check for recent applications (prevents duplicates)
     - Generate tailored resume using ResumeCustomizer
     - Export resume to PDF
     - Generate cover letter using CoverLetterService
     - Export cover letter to PDF
     - Log application details
  5. Save all qualified jobs to database
  6. Exit application

### Agents Layer

#### ResearchAgent.cs
- Orchestrates job search across multiple sources
- Builds search queries from AgentPolicy
- Aggregates results from all IJobSource implementations
- Applies hard filtering (currently disabled for debugging)
- Follows SRP - only responsible for job collection

#### IJobSource.cs / LinkedInSource.cs / IndeedSource.cs
- Interface segregation for job board implementations
- Each source independently scrapes and parses job postings
- Uses Playwright for browser automation
- Handles rate limiting and error recovery
- Returns standardized JobPosting models

### Infrastructure Layer

#### Repositories
- **IJobRepository / JobRepository**: Persists job postings with upsert logic
- **IApplicationLogRepository / ApplicationLogRepository**: Tracks applications and prevents duplicates
- Uses Dapper for efficient database operations
- Follows repository pattern for data access abstraction

#### Automation
- **IBrowserAutomation / PlaywrightAutomation**: Browser automation abstraction
- **IApplicationAutomation**: Strategy pattern for job board-specific automation
- **IndeedAutomation / LinkedInAutomation**: Board-specific automation implementations
- **ApplicationAutomationFactory**: Factory pattern for resolving automation strategies
- Configurable via options pattern

### Matching Layer

#### MatchingAgent.cs
- Loads resume via IResumeLoader (dependency injection)
- Generates embeddings using EmbeddingService
- Performs two-stage evaluation:
  1. Quick semantic filter using cosine similarity
  2. Deep LLM scoring with JobFitScorer
- Returns JobMatchResult with score, decision, and reasoning
- Thresholds configurable via MatchingConfiguration

#### ResumeLoader.cs
- Implements IResumeLoader interface
- Loads both human and AI resume versions from Profiles/
- Validates file existence and content
- Returns ResumeProfile model

#### Similarity.cs
- Static utility class for cosine similarity calculation
- Used for semantic matching between resume and job embeddings

### Services Layer

#### EmbeddingService.cs
- Uses Semantic Kernel with OpenAI integration
- Generates embeddings for semantic similarity
- Configurable embedding model via appsettings

#### JobFitScorer.cs
- Uses OpenAI Chat API for deep job fit analysis
- Returns score (0-100) and reasoning
- Temperature set to 0.0 for consistency
- Handles JSON parsing and validation

#### ResumeCustomizer.cs
- Uses OpenAI Chat API to generate tailored resumes
- Preserves truthfulness - never invents experience
- Returns structured TailoredResume model
- Temperature 0.3 for controlled creativity

#### CoverLetterService.cs
- Implements ICoverLetterService interface
- Generates customized cover letters using OpenAI
- Adapts format based on job location (supports multiple languages)
- Returns GeneratedCoverLetter model

#### PdfResumeExporter.cs / PdfCoverLetterExporter.cs
- Use QuestPDF for professional PDF generation
- Follow consistent formatting standards
- Use IFileSanitizer for safe filename generation
- Output to dated folders for organization

#### FileSanitizer.cs
- Implements IFileSanitizer interface
- Removes invalid filename characters
- Follows SRP - single purpose of sanitization

### Application Layer

#### ApplicationAgent.cs
- Orchestrates automated job applications
- Uses IApplicationAutomation strategy pattern
- Logs all application attempts
- Configurable via ApplicationPolicy
- Takes screenshots for verification
- Respects rate limiting between applications

### Models

Domain models representing business entities:
- **JobPosting**: Job listing data
- **TailoredResume**: AI-customized resume content
- **GeneratedCoverLetter**: AI-generated cover letter
- **ApplicationLog**: Application tracking record
- **ResumeProfile**: Resume text container
- **JobMatchResult**: Job evaluation result

### Configuration Models

Policy and configuration classes using options pattern:
- **AgentPolicy**: Job search criteria
- **MatchingConfiguration**: Matching thresholds
- **ApplicationPolicy**: Application behavior settings
- **BrowserAutomationOptions**: Browser configuration
- **IndeedAutomationOptions**: Indeed-specific settings

---

## Troubleshooting

### Common Issues

#### "OPENAI_API_KEY environment variable is not set"
- Set the environment variable before running:
  ```powershell
  $env:OPENAI_API_KEY = "sk-..."
  ```
- Verify it's set: `$env:OPENAI_API_KEY`

#### "Output:BasePath not configured"
- Add to appsettings.json:
  ```json
  "Output": {
    "BasePath": "C:\\JobApplications"
  }
  ```

#### "Resume file not found"
- Create `Profiles/resume.human.txt` and `Profiles/resume.ai.txt`
- Ensure files contain actual resume text (not empty)

#### Database Connection Errors
- Verify PostgreSQL is running
- Check connection string format in appsettings.json
- Ensure database exists (create if needed)
- Verify user has necessary permissions

#### Playwright Browser Errors
- Install Playwright browsers after first build:
  ```powershell
  pwsh JobResearchAgent/bin/Debug/net9.0/playwright.ps1 install
  ```
- Ensure sufficient disk space for browser binaries

#### No Jobs Found
- Check AgentPolicy configuration (keywords, countries)
- Verify job boards are accessible from your network
- Review logs for scraping errors
- Try with RemoteOnly = false and broader keywords

#### PDF Export Fails
- Verify Output:BasePath directory exists and is writable
- Check disk space
- Ensure QuestPDF license is set in Program.cs

#### High API Costs
- Reduce number of jobs evaluated (adjust MaxAgeHours)
- Increase MinimumSimilarityThreshold to filter more aggressively
- Limit job sources (comment out sources in Program.cs)

### Debugging Tips

- Enable verbose logging in appsettings.json
- Check console output for detailed error messages
- Review database tables for inserted data
- Inspect generated PDF files in the output directory
- Use breakpoints in Worker.cs to step through pipeline
- Check that all configuration sections are properly set

### Database Schema

Ensure your PostgreSQL database has these tables:
```sql
CREATE TABLE jobs (
    id SERIAL PRIMARY KEY,
    external_job_id VARCHAR(255) UNIQUE,
    title VARCHAR(500),
    company VARCHAR(500),
    location VARCHAR(500),
    url TEXT,
    description TEXT,
    source VARCHAR(100),
    collectedat TIMESTAMP,
    createdat TIMESTAMP,
    match_score DOUBLE PRECISION
);

CREATE TABLE job_tailored_resumes (
    id SERIAL PRIMARY KEY,
    external_job_id VARCHAR(255),
    professional_summary TEXT,
    key_skills TEXT,
    experience_json JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);

CREATE TABLE job_application_logs (
    id UUID PRIMARY KEY,
    external_job_id VARCHAR(255),
    job_title VARCHAR(500),
    company VARCHAR(500),
    location VARCHAR(500),
    url TEXT,
    source VARCHAR(100),
    created_at TIMESTAMP,
    resume_path TEXT,
    cover_letter_path TEXT,
    match_score DOUBLE PRECISION,
    status VARCHAR(100),
    notes TEXT
);
```

---

## Project Structure

```
JobResearchAgent/
├── Agents/                    # Job search and scraping logic
│   ├── IJobSource.cs         # Interface for job sources
│   ├── LinkedInSource.cs     # LinkedIn scraper
│   ├── IndeedSource.cs       # Indeed scraper
│   ├── ResearchAgent.cs      # Orchestrates job search
│   └── AgentPolicy.cs        # Search criteria configuration
├── Application/               # Application automation
│   ├── IApplicationAutomation.cs
│   ├── ApplicationAgent.cs   # Application orchestrator
│   ├── ApplicationResult.cs  # Result model
│   ├── ApplicationPolicy.cs  # Application configuration
│   └── AutomationOptions.cs
├── Infrastructure/            # Data and browser automation
│   ├── IJobRepository.cs
│   ├── JobRepository.cs      # Job data persistence
│   ├── IApplicationLogRepository.cs
│   ├── ApplicationLogRepository.cs
│   └── Automation/           # Browser automation
│       ├── IBrowserAutomation.cs
│       ├── PlaywrightAutomation.cs
│       ├── IApplicationAutomation.cs
│       ├── IndeedAutomation.cs
│       ├── LinkedInAutomation.cs
│       ├── ApplicationAutomationFactory.cs
│       ├── BrowserAutomationOptions.cs
│       └── IndeedAutomationOptions.cs
├── Matching/                  # Semantic matching logic
│   ├── IResumeLoader.cs
│   ├── ResumeLoader.cs       # Loads resume files
│   ├── MatchingAgent.cs      # Evaluates job fit
│   ├── MatchingConfiguration.cs
│   ├── ResumeProfile.cs      # Resume model
│   ├── Similarity.cs         # Cosine similarity
│   └── JobMatchResult.cs     # Match result model
├── Models/                    # Domain models
│   ├── JobPosting.cs
│   ├── TailoredResume.cs
│   ├── GeneratedCoverLetter.cs
│   ├── ApplicationLog.cs
│   └── ApplicationPolicy.cs
├── Services/                  # Core services
│   ├── EmbeddingService.cs   # OpenAI embeddings
│   ├── JobFitScorer.cs       # LLM job scoring
│   ├── ResumeCustomizer.cs   # LLM resume tailoring
│   ├── PdfResumeExporter.cs  # PDF generation
│   ├── CoverLetter/
│   │   ├── ICoverLetterService.cs
│   │   ├── CoverLetterService.cs
│   │   └── PdfCoverLetterExporter.cs
│   └── FileManipulator/
│       ├── IFileSanitizer.cs
│       └── FileSanitizer.cs
├── Profiles/                  # Resume storage
│   ├── resume.human.txt
│   └── resume.ai.txt
├── Program.cs                 # Entry point & DI setup
├── Worker.cs                  # Main pipeline orchestrator
├── appsettings.json          # Configuration
└── JobResearchAgent.csproj   # Project file
```

---

## Best Practices

### Configuration Management
- Use appsettings.json for all configurable values
- Never hardcode API keys or connection strings
- Use the Options pattern for strongly-typed configuration
- Validate configuration at startup

### Error Handling
- All constructors validate null parameters (fail-fast)
- Services log warnings/errors with context
- Database operations handle duplicates gracefully
- Browser automation includes retry logic and screenshots

### Code Quality
- Follow SOLID principles throughout
- Use dependency injection for all dependencies
- Implement interfaces for testability
- Consistent naming conventions (C# standards)
- Comprehensive XML documentation comments

### Testing Recommendations
- Mock all external dependencies (database, APIs, browser)
- Test each component in isolation
- Use integration tests for end-to-end workflows
- Verify configuration validation logic

### Security
- Never commit API keys or credentials
- Use environment variables for sensitive data
- Sanitize all filenames before file operations
- Validate all external input (job data, API responses)

### Performance
- Embeddings are generated once per session
- Database operations use efficient upserts
- Browser automation includes rate limiting
- PDF generation is optimized with QuestPDF

---

## Future Enhancements

- [ ] Add support for more job boards (Glassdoor, Monster, etc.)
- [ ] Implement ML-based job matching (beyond embeddings)
- [ ] Add email notifications for high-match jobs
- [ ] Create web dashboard for monitoring applications
- [ ] Add resume version tracking and A/B testing
- [ ] Implement retry mechanisms for failed applications
- [ ] Add support for multiple resume profiles
- [ ] Integration with ATS systems
- [ ] Advanced analytics and reporting
- [ ] Mobile app for application management

---

## Contributing

Contributions are welcome! Please follow these guidelines:

1. Follow SOLID principles and existing code patterns
2. Add unit tests for new functionality
3. Update documentation (README and XML comments)
4. Use meaningful commit messages
5. Ensure all tests pass and code builds without warnings

---

## License

MIT License - See LICENSE file for details

---

## Acknowledgments

- **OpenAI** for GPT and embedding models
- **Microsoft Semantic Kernel** for AI orchestration
- **Playwright** for reliable browser automation
- **QuestPDF** for professional PDF generation
- **Dapper** for efficient data access

---

## Support

For issues, questions, or contributions:
- Open an issue on GitHub
- Review the SOLID_REFACTORING.md document for architecture details
- Check the troubleshooting section above

---

**Built with ❤️ following SOLID principles and clean architecture**
