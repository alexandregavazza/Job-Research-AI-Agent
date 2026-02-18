# Removing Sensitive Data from Git

## ⚠️ CRITICAL: Your appsettings.json is already in Git!

Your `appsettings.json` and `appsettings.Development.json` files are currently tracked by Git, which means:
- They're in your repository history
- If you've pushed to GitHub/remote, your personal data is already there

## Immediate Action Required

### Option 1: Quick Fix (Removes from future commits only)

This prevents the files from being tracked going forward, but they remain in Git history:

```powershell
# Navigate to repository root
cd "C:\Users\alexa\Documents\AI Agent"

# Remove files from Git tracking (but keep them locally)
git rm --cached JobResearchAgent/appsettings.json
git rm --cached JobResearchAgent/appsettings.Development.json

# Commit the changes
git add .gitignore
git add JobResearchAgent/appsettings.Example.json
git add SETUP.md
git commit -m "chore: remove sensitive config files and add .gitignore protection"

# Push changes
git push
```

**Note**: This doesn't remove the files from Git history. Anyone with access to your repository history can still see the old versions.

---

### Option 2: Complete Fix (Removes from history - RECOMMENDED)

This completely removes sensitive data from Git history:

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
git log -S "alexandre.gavazza@gmail.com" --all
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
