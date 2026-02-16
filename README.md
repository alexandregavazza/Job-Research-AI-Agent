# Job Research AI Agent

## Overview
Job Research AI Agent is an automated pipeline for discovering, collecting, and semantically matching job postings to a candidate's resume using AI and LLMs. It scrapes job boards (LinkedIn, Indeed), filters and scores jobs, and stores results in a PostgreSQL database. The system is modular, extensible, and leverages OpenAI for semantic matching and deep job fit analysis.

---

## Architecture

- **Program.cs**: Entry point. Configures dependency injection, loads configuration, registers all services, and starts the background worker.
- **Worker.cs**: Main pipeline loop. Orchestrates the workflow: initializes semantic matcher, runs job research agent, evaluates jobs, and saves strong matches.
- **Agents/**: Contains the core agent logic and job source integrations.
  - **ResearchAgent.cs**: Orchestrates job search across all sources, builds search queries, applies hard filters, and aggregates results.
  - **AgentPolicy.cs**: Defines search policy (countries, keywords, levels, remote/hybrid, max age).
  - **IJobSource.cs**: Interface for job board sources (e.g., LinkedIn, Indeed).
  - **LinkedInSource.cs**: Scrapes LinkedIn jobs using Playwright, applies policy, and parses job cards.
  - **IndeedSource.cs**: Scrapes Indeed jobs using Playwright, applies policy, and parses job cards.
- **Infrastructure/**:
  - **JobRepository.cs**: Persists job postings to PostgreSQL using Dapper. Handles upserts (no duplicates on external_job_id).
- **Matching/**: Handles resume loading, semantic matching, and job fit scoring.
  - **MatchingAgent.cs**: Loads resume, generates embeddings, evaluates jobs for semantic similarity, and invokes deep LLM scoring.
  - **ResumeLoader.cs**: Loads both human and AI-structured resumes from the Profiles directory.
  - **ResumeProfile.cs**: Model for resume text (human and AI versions).
  - **Similarity.cs**: Provides cosine similarity calculation for embeddings.
  - **JobMatchResult.cs**: Holds the result of a job match (score, decision, reason).
- **Models/**:
  - **JobPosting.cs**: Represents a job posting (title, company, location, url, description, source, timestamps, external id, match score).
- **Services/**:
  - **EmbeddingService.cs**: Generates text embeddings using OpenAI (Semantic Kernel).
  - **JobFitScorer.cs**: Uses OpenAI LLM to deeply score job fit and provide reasoning.
- **Profiles/**:
  - **resume.human.txt**: Paste your full human-readable resume here.
  - **resume.ai.txt**: Paste a structured, skill-focused version of your resume here.

---

## Pipeline Steps

1. **Initialize**: Loads configuration, resumes, and initializes embedding models.
2. **Job Search**: ResearchAgent queries all sources (LinkedIn, Indeed) using AgentPolicy.
3. **Filtering**: Hard filters applied (keywords, countries, levels, remote/hybrid, max age).
4. **Semantic Matching**: Each job is embedded and compared to the resume using cosine similarity.
5. **Deep Scoring**: Jobs passing the semantic threshold are scored by the LLM for fit and reasoning.
6. **Persistence**: Strong matches (score >= 70) are saved to the database.

---

## Configuration
- **appsettings.json**: Set your PostgreSQL connection string and AI model settings.
- **OPENAI_API_KEY**: Must be set in your environment variables.

---

## Extending
- Add new job sources by implementing `IJobSource`.
- Adjust search policy in `AgentPolicy.cs`.
- Customize resume files in `Profiles/`.

---

## Requirements
- .NET 9.0+
- PostgreSQL
- OpenAI API key
- Playwright (for scraping)

---

## Usage
1. Place your resumes in `Profiles/`.
2. Set up your database and connection string.
3. Set the `OPENAI_API_KEY` environment variable.
4. Run the app:
   ```sh
   dotnet run --project JobResearchAgent/JobResearchAgent.csproj
   ```
5. Review logs and database for matched jobs.

---

## File/Component Details

### Program.cs
- Configures DI for all services and agents.
- Loads connection string and OpenAI API key.
- Registers job sources, agents, and the background worker.

### Worker.cs
- Main orchestrator. Steps:
  1. Initializes semantic matcher (embeds resume).
  2. Runs ResearchAgent to collect jobs.
  3. Evaluates each job for semantic match and deep fit.
  4. Logs and saves strong matches.

### Agents/
- **ResearchAgent.cs**: Builds search queries, queries all sources, applies hard filters, aggregates jobs.
- **AgentPolicy.cs**: Search policy (countries, keywords, levels, remote/hybrid, max age).
- **IJobSource.cs**: Interface for job sources.
- **LinkedInSource.cs**: Scrapes LinkedIn jobs using Playwright, applies policy, parses job cards.
- **IndeedSource.cs**: Scrapes Indeed jobs using Playwright, applies policy, parses job cards.

### Infrastructure/
- **JobRepository.cs**: Saves jobs to PostgreSQL using Dapper. Upserts by external_job_id.

### Matching/
- **MatchingAgent.cs**: Loads resume, generates embeddings, evaluates jobs, invokes LLM for deep scoring.
- **ResumeLoader.cs**: Loads resumes from Profiles/.
- **ResumeProfile.cs**: Model for resume text.
- **Similarity.cs**: Cosine similarity for embeddings.
- **JobMatchResult.cs**: Holds job match result.

### Models/
- **JobPosting.cs**: Model for job posting.

### Services/
- **EmbeddingService.cs**: Generates embeddings using OpenAI.
- **JobFitScorer.cs**: Uses OpenAI LLM to score job fit and provide reasoning.

### Profiles/
- **resume.human.txt**: Human-readable resume.
- **resume.ai.txt**: AI-structured resume.

---

## Troubleshooting
- Ensure all required files exist in Profiles/.
- Check your database connection string.
- Make sure your OpenAI API key is set and valid.
- Playwright must be installed and working for scraping.

---

## License
MIT
