#!/bin/bash
# Run this from the root of the project folder.
# Authentication is handled by Git's own credential manager (Git Credential Manager,
# installed by default with Git for Windows) -- the first push will prompt a browser
# login or a one-time username/PAT prompt, and cache it securely OUTSIDE this file.
# Never hardcode a token in this script -- GitHub's push protection will block the
# push if it detects one anywhere in your commit history, current or past.

set -e

REPO="craigdjensen/flow-discovery-intent-analysis"
REMOTE_URL="https://github.com/${REPO}.git"

# Init only if not already a git repo
if [ ! -d .git ]; then
  git init
fi

# Add the remote only if it doesn't already exist
if ! git remote get-url origin >/dev/null 2>&1; then
  git remote add origin "$REMOTE_URL"
fi

# Create/switch to main only if it doesn't already exist
if ! git show-ref --verify --quiet refs/heads/main; then
  git checkout -b main
else
  git checkout main
fi

git add -A

# Commit only if there's actually something staged -- avoids a script-halting error
# on a re-run with no changes.
if ! git diff --cached --quiet; then
  git commit -m "feat: initial scaffold — Flow Discovery & Intent Analysis prototype"
else
  echo "Nothing to commit -- working tree already matches last commit."
fi

git push -u origin main
