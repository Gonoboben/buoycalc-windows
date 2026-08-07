param(
    [string]$Repository = $env:GITHUB_REPOSITORY,
    [string]$Token = $env:GITHUB_TOKEN,
    [string]$OutputDirectory = "artifacts/branch-hygiene"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Repository)) {
    throw "Repository is required. Pass -Repository owner/name or set GITHUB_REPOSITORY."
}

if ([string]::IsNullOrWhiteSpace($Token)) {
    throw "GitHub token is required for the branch-hygiene inventory."
}

$apiBase = if ([string]::IsNullOrWhiteSpace($env:GITHUB_API_URL)) {
    "https://api.github.com"
} else {
    $env:GITHUB_API_URL.TrimEnd('/')
}

$headers = @{
    Authorization = "Bearer $Token"
    Accept = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

function Invoke-GitHubGet([string]$path) {
    return Invoke-RestMethod -Uri "$apiBase$path" -Headers $headers -Method Get
}

function Invoke-GitHubPagedGet([string]$path) {
    $all = @()

    for ($page = 1; ; $page++) {
        $separator = if ($path.Contains("?")) { "&" } else { "?" }
        $items = @(Invoke-GitHubGet "$path${separator}per_page=100&page=$page")
        $all += $items

        if ($items.Count -lt 100) {
            break
        }
    }

    return $all
}

function Test-ExplicitlySuperseded([object]$pullRequest) {
    $text = (($pullRequest.title ?? "") + "`n" + ($pullRequest.body ?? ""))
    return $text -match '(?i)\b(superseded|duplicate)\b|дубликат|замен[её]н|заменен'
}

function Escape-MarkdownCell([string]$value) {
    return ($value ?? "").Replace("|", "\|").Replace("`r", " ").Replace("`n", " ")
}

Write-Host "BRANCH HYGIENE: DRY RUN ONLY. No branch deletion API calls are implemented."

$repositoryInfo = Invoke-GitHubGet "/repos/$Repository"
$defaultBranch = [string]$repositoryInfo.default_branch
$branches = @(Invoke-GitHubPagedGet "/repos/$Repository/branches")
$pullRequests = @(Invoke-GitHubPagedGet "/repos/$Repository/pulls?state=all&sort=updated&direction=desc")

$localPullRequests = @($pullRequests | Where-Object {
    $_.head.repo -and $_.head.repo.full_name -eq $Repository
})

$openPullRequests = @($localPullRequests | Where-Object { $_.state -eq "open" })
$openBranchNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
foreach ($pullRequest in $openPullRequests) {
    [void]$openBranchNames.Add([string]$pullRequest.head.ref)
}

$records = foreach ($branch in $branches) {
    $branchName = [string]$branch.name
    $currentHeadSha = [string]$branch.commit.sha
    $isDefault = $branchName -eq $defaultBranch
    $isProtected = [bool]$branch.protected
    $openForBranch = @($openPullRequests | Where-Object { $_.head.ref -eq $branchName })
    $closedForBranch = @($localPullRequests | Where-Object {
        $_.state -eq "closed" -and $_.head.ref -eq $branchName
    })

    $candidate = $false
    $reason = "no-pr-evidence"
    $evidence = @()

    if ($isDefault) {
        $reason = "default-branch"
    }
    elseif ($isProtected) {
        $reason = "protected-branch"
    }
    elseif ($openBranchNames.Contains($branchName)) {
        $reason = "open-pr-branch"
        $evidence = @($openForBranch | ForEach-Object { [int]$_.number })
    }
    else {
        $mergedHeadMatches = @($closedForBranch | Where-Object {
            $_.merged_at -and [string]$_.head.sha -eq $currentHeadSha
        })

        if ($mergedHeadMatches.Count -gt 0) {
            $candidate = $true
            $reason = "merged-pr-current-head-match"
            $evidence = @($mergedHeadMatches | ForEach-Object { [int]$_.number })
        }
        else {
            $supersededHeadMatches = @($closedForBranch | Where-Object {
                -not $_.merged_at -and
                [string]$_.head.sha -eq $currentHeadSha -and
                (Test-ExplicitlySuperseded $_)
            })

            if ($supersededHeadMatches.Count -gt 0) {
                $candidate = $true
                $reason = "explicitly-superseded-closed-pr-current-head-match"
                $evidence = @($supersededHeadMatches | ForEach-Object { [int]$_.number })
            }
            elseif ($closedForBranch.Count -gt 0) {
                $reason = "historical-pr-without-safe-current-head-evidence"
                $evidence = @($closedForBranch | ForEach-Object { [int]$_.number })
            }
        }
    }

    [pscustomobject]@{
        Branch = $branchName
        HeadSha = $currentHeadSha
        IsDefault = $isDefault
        IsProtected = $isProtected
        HasOpenPullRequest = $openForBranch.Count -gt 0
        Candidate = $candidate
        Reason = $reason
        EvidencePullRequests = @($evidence | Sort-Object -Unique)
    }
}

$records = @($records | Sort-Object Branch)
$candidates = @($records | Where-Object Candidate | Sort-Object Branch)

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$jsonPath = Join-Path $OutputDirectory "branch-hygiene.json"
$markdownPath = Join-Path $OutputDirectory "branch-hygiene.md"

$payload = [pscustomobject]@{
    Repository = $Repository
    DefaultBranch = $defaultBranch
    GeneratedAtUtc = [DateTime]::UtcNow.ToString("o")
    DryRunOnly = $true
    TotalBranches = $records.Count
    CandidateCount = $candidates.Count
    Records = $records
}

$payload | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $jsonPath -Encoding utf8

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add("# Branch hygiene dry run")
$lines.Add("")
$lines.Add("- Repository: ``$Repository``")
$lines.Add("- Default branch: ``$defaultBranch``")
$lines.Add("- Total branches: $($records.Count)")
$lines.Add("- Safe-review candidates: $($candidates.Count)")
$lines.Add("- Mode: **DRY RUN ONLY — no branches are deleted**")
$lines.Add("")
$lines.Add("A candidate is reported only when it is not the default/protected/open-PR branch and its current head SHA exactly matches the head SHA of a merged PR or an explicitly superseded/duplicate closed PR. Squash-merge ancestry alone is not used as deletion evidence.")
$lines.Add("")
$lines.Add("| Branch | Reason | Evidence PRs | Current head |")
$lines.Add("|---|---|---|---|")

foreach ($candidate in $candidates) {
    $prs = if ($candidate.EvidencePullRequests.Count -gt 0) {
        ($candidate.EvidencePullRequests | ForEach-Object { "#$($_)" }) -join ", "
    } else {
        "—"
    }

    $lines.Add("| $(Escape-MarkdownCell $candidate.Branch) | $(Escape-MarkdownCell $candidate.Reason) | $prs | ``$($candidate.HeadSha)`` |")
}

if ($candidates.Count -eq 0) {
    $lines.Add("| — | No safe-review candidates found | — | — |")
}

$lines | Set-Content -LiteralPath $markdownPath -Encoding utf8

Write-Host "Branch hygiene inventory written to:"
Write-Host "  $jsonPath"
Write-Host "  $markdownPath"
Write-Host "Candidates: $($candidates.Count) / branches: $($records.Count)"
