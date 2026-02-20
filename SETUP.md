# Setup Guide

## Initial Configuration

### 1. Create Your Configuration File

Copy the example configuration to create your personal settings:

```powershell
# PowerShell
Copy-Item JobResearchAgent/appsettings.Example.json JobResearchAgent/appsettings.json
```

Or manually:
1. Copy `appsettings.Example.json`
2. Rename the copy to `appsettings.json`
3. Update with your personal information (see below)

### 2. Set Environment Variable

Set your OpenAI API key as an environment variable:

```powershell
# PowerShell (Current Session)
$env:OPENAI_API_KEY = "sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"

# PowerShell (Permanent - User Level)
[System.Environment]::SetEnvironmentVariable('OPENAI_API_KEY', 'sk-proj-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxx', 'User')
```

### 3. Update appsettings.json

Open `JobResearchAgent/appsettings.json` and update the following sections:

#### Database Connection
```json
"ConnectionStrings": {
  "Default": "Host=localhost;Port=5432;Database=jobsdb;Username=YOUR_USERNAME;Password=YOUR_PASSWORD"
}
```

#### Output Paths
```json
"Output": {
  "BasePath": "C:\\YOUR_PATH\\JobApplications"
},
"ApplicationPolicy": {
  "DocumentsBasePath": "C:\\YOUR_PATH\\JobApplications"
}
```

#### Your Personal Information
```json
"Candidate": {
  "FullName": "Your Full Name",
  "Phone": "Your Phone Number",
  "Email": "your.email@example.com",
  "LinkedIn": "https://linkedin.com/in/yourprofile",
  "PersonalWebsite": "https://yourwebsite.com",
  "Location": "Your City, State/Province, Country",
  "Education": [
    "School Name | Degree | Graduation Year",
    "Another School | Degree | Graduation Year"
  ],
  "Career": {
    "StartYear": "Your Career Start Date",
    "EarlyCareerLabelEn": "Your Early Career Title (EN)",
    "EarlyCareerLabelPt": "Seu titulo de inicio de carreira (PT)",
    "DescriptionEn": "Brief description of your early career (EN)",
    "DescriptionPt": "Descricao breve do inicio de carreira (PT)"
  }
}
```

#### Job Search Criteria
```json
"AgentPolicy": {
  "CountriesTargeted": ["Country 1", "Country 2"],
  "Keywords": ["Skill 1", "Job Title 1", "Technology 1"],
  "Levels": ["Mid", "Senior", "Lead"],
  "SearchJobsInTheLast": 48,
  "RemoteOnly": false,
  "AllowHybrid": true
}
```

#### Indeed Automation (Optional)
```json
"IndeedAutomation": {
  "AdditionalFields": [
    {
      "Selector": "input[name='applicant.name']",
      "Value": "Your Full Name"
    },
    {
      "Selector": "input[name='applicant.email']",
      "Value": "your.email@example.com"
    }
  ]
}
```

### 4. Create Resume Files

Create your resume files in the `Profiles/` directory:

1. **resume.human.txt**: Your complete, human-readable resume with all details
2. **resume.ai.txt**: A condensed, keyword-focused version emphasizing skills and technologies

### 5. Setup Database

```sql
-- Create PostgreSQL database
CREATE DATABASE jobsdb;

-- Run the schema creation scripts (see README for table schemas)
```

### 6. Install Playwright Browsers

After building the project for the first time:

```powershell
# Navigate to the build output directory
cd JobResearchAgent/bin/Debug/net9.0

# Install Playwright browsers
pwsh playwright.ps1 install
```

## Security Best Practices

### ✅ DO:
- Keep `appsettings.json` private (it's in .gitignore)
- Use environment variables for API keys
- Use strong database passwords
- Rotate any default or weak local credentials (avoid `postgres/postgres`)
- Review .gitignore before committing

### ❌ DON'T:
- Commit `appsettings.json` to version control
- Share your `appsettings.json` file publicly
- Hardcode API keys in code
- Commit PDF files with personal information
- Commit resume files with your actual information
- Commit database dumps or export files (e.g., `export_data.sql`, `export_data.dump`)

## Verifying Your Setup

Before committing to Git, verify protected files are ignored:

```powershell
# Check what files Git will track
git status

# You should NOT see:
# - appsettings.json
# - appsettings.Development.json
# - Profiles/resume.*.txt
# - Any .pdf files
```

## Troubleshooting

### "appsettings.json not found"
- Make sure you copied `appsettings.Example.json` to `appsettings.json`

### "OPENAI_API_KEY not set"
- Verify the environment variable: `echo $env:OPENAI_API_KEY`
- Restart your terminal if you just set it

### "Database connection failed"
- Verify PostgreSQL is running
- Check your connection string credentials
- Ensure the database exists

## After Cloning (For Other Developers)

If someone clones your repository, they should:

1. Copy `appsettings.Example.json` to `appsettings.json`
2. Update `appsettings.json` with their personal information
3. Set their `OPENAI_API_KEY` environment variable
4. Create their own resume files in `Profiles/`
5. Install Playwright browsers

The repository should NOT contain any personal information or credentials.
