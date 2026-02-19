# Database Setup Guide - Job Research Agent

This guide covers PostgreSQL database setup for both local development and AWS RDS deployment.

---

## Database Schema

The application requires three tables:

### 1. Jobs Table
Stores all scraped job postings from LinkedIn and Indeed.

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
```

### 2. Job Tailored Resumes Table
Stores AI-generated tailored resumes for each qualified job.

```sql
CREATE TABLE job_tailored_resumes (
    id SERIAL PRIMARY KEY,
    external_job_id VARCHAR(255),
    professional_summary TEXT,
    key_skills TEXT,
    experience_json JSONB,
    created_at TIMESTAMP DEFAULT NOW()
);
```

### 3. Job Application Logs Table
Tracks all job applications with document paths and status.

```sql
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

## Local Development Setup

### Prerequisites
- PostgreSQL 16+ installed locally
- PostgreSQL running on `localhost:5432`

### Create Database and Tables

1. Connect to PostgreSQL:
```bash
psql -U postgres
```

2. Create the database:
```sql
CREATE DATABASE jobsdb;
```

3. Connect to the new database:
```sql
\c jobsdb
```

4. Run the schema creation scripts above (copy-paste all three CREATE TABLE statements).

5. Verify tables were created:
```sql
\dt
```

You should see:
```
              List of relations
 Schema |         Name           | Type  | Owner
--------+------------------------+-------+----------
 public | jobs                   | table | postgres
 public | job_tailored_resumes   | table | postgres
 public | job_application_logs   | table | postgres
```

6. Exit psql:
```sql
\q
```

### Configure Application

The connection string in `appsettings.json`:
```json
"ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=jobsdb;Username=YOUR_USERNAME;Password=YOUR_STRONG_PASSWORD"
}
```

---

## AWS RDS Setup

### Prerequisites
- RDS PostgreSQL instance created (from AWS deployment guide)
- Database endpoint: `job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com`
- Master username: `postgres`
- Master password: (the one you set during RDS creation)
- Database name: `jobsdb`

### Option 1: Connect via psql (Recommended)

1. Install PostgreSQL client if not already installed.

2. Connect to RDS:
```powershell
$env:PGPASSWORD="YOUR_MASTER_PASSWORD"
psql -h job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com -U postgres -d jobsdb -p 5432
```

Replace `YOUR_MASTER_PASSWORD` with the password you set during RDS creation.

3. Run the schema creation scripts:
Copy-paste all three CREATE TABLE statements from the schema section above.

4. Verify tables:
```sql
\dt
```

5. Exit:
```sql
\q
```

### Option 2: Connect via DBeaver, pgAdmin, or other GUI

1. Create a new connection with these details:
   - **Host**: `job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com`
   - **Port**: `5432`
   - **Database**: `jobsdb`
   - **Username**: `postgres`
   - **Password**: [your master password]

2. Execute the schema creation scripts in a SQL editor.

### Option 3: Using SQL Script File

1. Save the schema to a file `schema.sql`:
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

2. Execute the script:
```powershell
$env:PGPASSWORD="YOUR_MASTER_PASSWORD"
psql -h job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com -U postgres -d jobsdb -p 5432 -f schema.sql
```

---

## Verification

### Check Tables Exist
```sql
SELECT table_name 
FROM information_schema.tables 
WHERE table_schema = 'public';
```

### Check Table Structure
```sql
\d jobs
\d job_tailored_resumes
\d job_application_logs
```

### Test Insert (Optional)
```sql
INSERT INTO jobs (title, company, location, url, description, source, collectedat, createdat, external_job_id, match_score)
VALUES ('Test Job', 'Test Company', 'Test Location', 'https://test.com', 'Test description', 'Test', NOW(), NOW(), 'test-123', 85.5);

SELECT * FROM jobs;

DELETE FROM jobs WHERE external_job_id = 'test-123';
```

---

## Connection String Formats

### Local Development
```
Host=localhost;Port=5432;Database=jobsdb;Username=YOUR_USERNAME;Password=YOUR_STRONG_PASSWORD
```

### AWS RDS (stored in Secrets Manager)
```
Host=job-research-agent-db.chsiiyek8wor.sa-east-1.rds.amazonaws.com;Port=5432;Database=jobsdb;Username=postgres;Password=YOUR_MASTER_PASSWORD
```

---

## Troubleshooting

### Cannot connect to RDS

**Issue**: Connection timeout or refuses connection.

**Solution**: Verify security group allows traffic from your IP:
```powershell
aws ec2 describe-security-groups --group-ids sg-057ec7832807d06ea --region sa-east-1
```

Add your current IP if needed:
```powershell
aws ec2 authorize-security-group-ingress --group-id sg-057ec7832807d06ea --protocol tcp --port 5432 --cidr YOUR_IP/32 --region sa-east-1
```

### Tables already exist

**Issue**: "relation already exists" error.

**Solution**: Tables are already created. Skip table creation or drop them first:
```sql
DROP TABLE IF EXISTS job_application_logs CASCADE;
DROP TABLE IF EXISTS job_tailored_resumes CASCADE;
DROP TABLE IF EXISTS jobs CASCADE;
```

### Authentication failed

**Issue**: Wrong password or user doesn't exist.

**Solution**: 
1. Verify password is correct
2. Check the master username in RDS console
3. Reset RDS master password if needed via AWS Console

### Connection from ECS fails

**Issue**: Application logs show connection errors.

**Solution**:
1. Verify connection string in Secrets Manager is correct
2. Verify ECS security group can reach RDS security group
3. Check RDS is publicly accessible or add VPC endpoint

---

## Indexes for Performance (Optional)

Add these indexes to improve query performance:

```sql
CREATE INDEX idx_jobs_external_id ON jobs(external_job_id);
CREATE INDEX idx_jobs_match_score ON jobs(match_score DESC);
CREATE INDEX idx_jobs_created_at ON jobs(createdat DESC);
CREATE INDEX idx_application_logs_external_id ON job_application_logs(external_job_id);
CREATE INDEX idx_application_logs_created_at ON job_application_logs(created_at DESC);
CREATE INDEX idx_tailored_resumes_external_id ON job_tailored_resumes(external_job_id);
```

---

## Data Retention (Optional)

Set up automatic cleanup of old records:

```sql
-- Delete jobs older than 90 days
DELETE FROM jobs WHERE createdat < NOW() - INTERVAL '90 days';

-- Delete application logs older than 1 year
DELETE FROM job_application_logs WHERE created_at < NOW() - INTERVAL '1 year';
```

You can schedule this as a cron job or AWS Lambda function.

---

## Backup and Restore

### Backup Local Database
```powershell
pg_dump -U postgres -d jobsdb -F c -f jobsdb_backup.dump
```

### Restore Local Database
```powershell
pg_restore -U postgres -d jobsdb -c jobsdb_backup.dump
```

### RDS Backups
RDS automatic backups are configured with 7-day retention. Manual snapshots can be created via AWS Console or CLI:
```powershell
aws rds create-db-snapshot --db-instance-identifier job-research-agent-db --db-snapshot-identifier manual-backup-$(Get-Date -Format "yyyy-MM-dd-HHmmss") --region sa-east-1
```

---

## Next Steps

After setting up the database:
1. Test local application runs successfully
2. Deploy updated container to ECS
3. Monitor CloudWatch Logs for any database errors
4. Verify data is being written to RDS tables
