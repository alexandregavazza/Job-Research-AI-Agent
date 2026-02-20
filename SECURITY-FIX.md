# Removing Sensitive Data from Git

## ✅ COMPLETED: Sensitive data has been cleaned!

The following files have been removed from Git history:
- `appsettings.json` and `appsettings.Development.json` (database credentials, API keys)
- `resume.human.txt` and `resume.ai.txt` (personal information, phone numbers, addresses)

All sensitive data has been:
- Removed from Git tracking
- Cleaned from entire repository history (using git filter-branch)
- Force pushed to remote (GitHub)
- Replaced with example files containing mock data

## ⚠️ CRITICAL: Rotate Your Credentials!

Even though files are removed from Git, you should **immediately** change:

1. **OpenAI API Key**: Generate new key at https://platform.openai.com/api-keys
2. **Database Password**: Change your PostgreSQL password from "postgres" to something strong
3. **Check GitHub**: Visit https://github.com/YOUR_USERNAME/Job-Research-AI-Agent/settings/security_analysis
   - Check for "Secret scanning alerts"
   - GitHub may have detected exposed secrets

## Files Now Protected

The following files are in `.gitignore` and will never be committed:
- `JobResearchAgent/appsettings.json`
- `JobResearchAgent/appsettings.Development.json`
- `JobResearchAgent/Profiles/resume.human.txt`
- `JobResearchAgent/Profiles/resume.ai.txt`
- `*.pdf` (generated resumes and cover letters)

Your actual files are backed up as:
- `appsettings.json.backup`
- `resume.human.txt.backup`
- `resume.ai.txt.backup`

---

## How the Cleanup Was Done

For reference, here's what was executed:

### Step 1: Remove appsettings files from history

```powershell
# Backup files
Copy-Item JobResearchAgent/appsettings.json JobResearchAgent/appsettings.json.backup

# Remove from Git tracking
git rm --cached JobResearchAgent/appsettings.json JobResearchAgent/appsettings.Development.json

# Remove from ALL history
git filter-branch --force --index-filter `
  "git rm --cached --ignore-unmatch JobResearchAgent/appsettings.json JobResearchAgent/appsettings.Development.json" `
  --prune-empty --tag-name-filter cat -- --all

# Force push to remote
git push origin --force --all
```

### Step 2: Remove resume files from history

```powershell
# Backup files
Copy-Item JobResearchAgent/Profiles/resume.human.txt JobResearchAgent/Profiles/resume.human.txt.backup
Copy-Item JobResearchAgent/Profiles/resume.ai.txt JobResearchAgent/Profiles/resume.ai.txt.backup

# Remove from Git tracking
git rm --cached JobResearchAgent/Profiles/resume.human.txt JobResearchAgent/Profiles/resume.ai.txt

# Remove from ALL history
git filter-branch --force --index-filter `
  "git rm --cached --ignore-unmatch 'JobResearchAgent/Profiles/resume.human.txt' 'JobResearchAgent/Profiles/resume.ai.txt'" `
  --prune-empty --tag-name-filter cat -- --all

# Clean up and force push
git gc --prune=now --aggressive
git push origin --force --all
```

### Step 3: Verification

```powershell
# Verify no sensitive data in main branch
git log main -S "example.user@example.com" --oneline     # Should be empty or only in security commits
git log main -S "+1 555-0100" --oneline                   # Should be empty
git log main -S "Password=postgres" --oneline             # Should be empty

# Verify only example files are tracked
git ls-files | Select-String -Pattern "appsettings|resume"
```

---

## If You Need to Share This Repository

### For Future Commits

The `.gitignore` is now configured to permanently block:
- Configuration files: `appsettings.json`, `appsettings.Development.json`
- Resume files: `resume.human.txt`, `resume.ai.txt`
- Generated PDFs: `*.pdf`

New team members should:
1. Clone the repository
2. Copy `appsettings.Example.json` to `appsettings.json`
3. Fill in their own credentials (see [SETUP.md](SETUP.md))
4. Copy example resume files and add their own content

---

## Prevention for the Future

### Pre-commit Hook (Optional)

Create `.git/hooks/pre-commit` to prevent accidental commits:

```bash
#!/bin/sh
if git diff --cached --name-only | grep -E "appsettings\.json|resume\.(human|ai)\.txt"; then
  echo "ERROR: Attempting to commit sensitive files!"
  echo "Please remove them from your commit."
  exit 1
fi
```

Make it executable:
```powershell
chmod +x .git/hooks/pre-commit
```

### Using git-secrets

Install and configure [git-secrets](https://github.com/awslabs/git-secrets):

```powershell
# Install via Chocolatey (Windows)
choco install git-secrets

# Configure
git secrets --install
git secrets --register-aws  # Scans for AWS keys
git secrets --add 'sk-[a-zA-Z0-9]{48}'  # OpenAI API key pattern
```

---

## Historical Reference

The commands below were part of earlier attempts - **these are now completed**:

### Option 1: Quick Fix (COMPLETED)

```powershell
# Navigate to repository root
cd "C:\Users\alexa\Documents\AI Agent"

# IMPORTANT: Make a backup first!
Copy-Item JobResearchAgent/appsettings.json JobResearchAgent/appsettings.json.backup
Copy-Item JobResearchAgent/appsettings.Development.json JobResearchAgent/appsettings.Development.json.backup

# Remove files from Git and history using filter-branch
git filter-branch --force --index-filter `
  "git rm --cached --ignore-unmatch JobResearchAgent/appsettings.json JobResearchAgent/appsettings.Development.json" `
  --prune-empty --tag-name-filter cat -- --all

# Clean up
git for-each-ref --format="delete %(refname)" refs/original | git update-ref --stdin
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# Add the protection files
git add .gitignore
git add JobResearchAgent/appsettings.Example.json
git add SETUP.md
git commit -m "chore: remove sensitive data from history and add security protections"

# Force push (DANGER: This rewrites history)
git push origin --force --all
git push origin --force --tags
```

⚠️ **WARNING**: 
- `--force` push rewrites history
- If others have cloned your repo, they'll need to re-clone
- Use this carefully on shared repositories

---

### Option 3: Alternative - Using BFG Repo-Cleaner (Faster for large repos)

Install BFG Repo-Cleaner, then:

```powershell
# Download from: https://rtyley.github.io/bfg-repo-cleaner/

# Clean the repo
java -jar bfg.jar --delete-files "appsettings.json" .
java -jar bfg.jar --delete-files "appsettings.Development.json" .

# Clean up
git reflog expire --expire=now --all && git gc --prune=now --aggressive

# Push changes
git push origin --force --all
```

---

## If You've Already Pushed to GitHub

### Additional Security Steps:

1. **Rotate ALL Credentials Immediately**:
   - Change your database password
   - Generate a new OpenAI API key
   - Update any other credentials that were in the files

2. **Check GitHub for exposed secrets**:
   - Go to your repository on GitHub
   - Check "Settings" → "Security" → "Secret scanning alerts"
   - GitHub may have detected API keys automatically

3. **Consider Making the Repository Private**:
   - If it's public, consider making it private
   - Go to Repository Settings → Danger Zone → Change visibility

4. **Review Commit History**:
   - Check old commits to see what was exposed
   - On GitHub: Go to your file → Click "History" to see old versions

---

## Verification

After completing the fix, verify:

```powershell
# Check current tracked files (should NOT include appsettings.json)
git ls-files | Select-String -Pattern "appsettings"

# Should only show: appsettings.Example.json

# Check Git status (should show appsettings.json as untracked if it exists)
git status

# Verify .gitignore is working
git check-ignore -v JobResearchAgent/appsettings.json
# Should output: .gitignore:XX:JobResearchAgent/appsettings.json

# Search history for sensitive data (example: email)
git log -S "example.user@example.com" --all
# Should show no results after cleaning
```

---

## Best Practices Moving Forward

1. **Always check before committing**:
   ```powershell
   git status
   git diff --staged
   ```

2. **Use pre-commit hooks** to prevent committing sensitive files:
   Create `.git/hooks/pre-commit` (remove .sample extension):
   ```bash
   #!/bin/bash
   if git diff --cached --name-only | grep -q "appsettings\.json"; then
     echo "ERROR: Attempting to commit appsettings.json"
     exit 1
   fi
   ```

3. **Regular audits**:
   ```powershell
   git ls-files | Select-String -Pattern "appsettings|password|secret|key"
   ```

4. **Use git-secrets** tool:
   - Install: https://github.com/awslabs/git-secrets
   - Prevents committing secrets

---

## Documentation Updates

After fixing, update your README.md to include:
- Reference to SETUP.md for configuration
- Security notice about not committing appsettings.json
- Steps for new developers to set up their own config

---

## Questions?

- If you've pushed to a remote, did you want me to show you how to check what's exposed?
- Do you want help implementing pre-commit hooks?
- Do you need help rotating any specific credentials?
