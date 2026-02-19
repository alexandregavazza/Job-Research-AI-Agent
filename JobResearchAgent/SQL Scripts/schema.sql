-- Job Research Agent Database Schema
-- PostgreSQL 16+

-- Table: jobs
-- Stores all scraped job postings from LinkedIn and Indeed
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

-- Table: job_tailored_resumes
-- Stores AI-generated tailored resumes for each qualified job
CREATE TABLE job_tailored_resumes (
    id SERIAL PRIMARY KEY,
    external_job_id VARCHAR(255),
    professional_summary TEXT,
    key_skills TEXT,
    experience_json JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);

-- Table: job_application_logs
-- Tracks all job applications with document paths and status
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

-- Indexes for performance
CREATE INDEX idx_jobs_external_id ON jobs(external_job_id);
CREATE INDEX idx_jobs_match_score ON jobs(match_score DESC);
CREATE INDEX idx_jobs_created_at ON jobs(createdat DESC);
CREATE INDEX idx_application_logs_external_id ON job_application_logs(external_job_id);
CREATE INDEX idx_application_logs_created_at ON job_application_logs(created_at DESC);
CREATE INDEX idx_tailored_resumes_external_id ON job_tailored_resumes(external_job_id);
