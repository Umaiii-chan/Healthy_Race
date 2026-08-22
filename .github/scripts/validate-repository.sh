#!/usr/bin/env bash
set -euo pipefail

fail=0

forbidden_regex='(^|/)(Library|Temp|Logs|UserSettings|obj|Build|Builds)/'
if git ls-files | grep -E "$forbidden_regex"; then
  echo '::error::Unity generated/build directories must not be committed.'
  fail=1
fi

if git grep -nE '^(<<<<<<<|=======|>>>>>>>)' -- ':!*.md' ':!*.txt'; then
  echo '::error::Merge conflict markers detected.'
  fail=1
fi

if git grep -nEi '(api[_-]?key|secret|password|token)[[:space:]]*[:=][[:space:]]*[A-Za-z0-9_./+=-]{12,}' -- ':!*.md' ':!.github/**' ':!.env.example'; then
  echo '::error::Possible credential committed. Review the matches above.'
  fail=1
fi

# GitHub rejects files larger than 100 MB; keep a stricter project threshold.
while IFS= read -r file; do
  [[ -f "$file" ]] || continue
  size=$(stat -c%s "$file")
  if (( size > 50 * 1024 * 1024 )); then
    if ! git check-attr filter -- "$file" | grep -q 'filter: lfs'; then
      echo "::error file=$file::File is larger than 50 MB and is not tracked by Git LFS."
      fail=1
    fi
  fi
done < <(git ls-files)

if (( fail != 0 )); then
  exit 1
fi

echo 'Repository validation passed.'
