param(
    [Parameter(Mandatory = $true)]
    [string]$Repository,

    [Parameter(Mandatory = $true)]
    [string]$SourceCommit,

    [Parameter(Mandatory = $true)]
    [string]$GitHubToken
)

$ErrorActionPreference = "Stop"

if ($Repository -notmatch '^[^/]+/[^/]+$') {
    throw "Repository must use owner/name form; got '$Repository'."
}
if ($SourceCommit -notmatch '^[0-9a-f]{40}$') {
    throw "SourceCommit must be a full lowercase 40-character git SHA; got '$SourceCommit'."
}
if ([string]::IsNullOrWhiteSpace($GitHubToken)) {
    throw "GitHubToken is required to inspect Actions runs."
}

$uri = "https://api.github.com/repos/$Repository/actions/runs?head_sha=$SourceCommit&event=push&per_page=100"
$headers = @{
    Authorization = "Bearer $GitHubToken"
    Accept = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

$response = Invoke-RestMethod -Uri $uri -Method Get -Headers $headers
$requiredWorkflows = @(
    ".NET Build",
    "Selected Shape Consumer Scan",
    "Report Store Consumer Scan",
    "BuoyCalc Validation 601 Main Gate"
)

foreach ($requiredName in $requiredWorkflows) {
    $matches = @(
        $response.workflow_runs |
            Where-Object {
                $_.name -eq $requiredName -and
                $_.head_sha -eq $SourceCommit -and
                $_.event -eq "push"
            } |
            Sort-Object -Property created_at -Descending
    )

    if ($matches.Count -eq 0) {
        throw "Required exact-main CI workflow '$requiredName' has no push run for source $SourceCommit."
    }

    $run = $matches[0]
    if ($run.status -ne "completed" -or $run.conclusion -ne "success") {
        throw "Required exact-main CI workflow '$requiredName' is not green for ${SourceCommit}: status=$($run.status), conclusion=$($run.conclusion), run=$($run.html_url)"
    }

    Write-Host "Exact-main CI gate confirmed: $requiredName | run=$($run.id) | conclusion=$($run.conclusion)"
}

$statusUri = "https://api.github.com/repos/$Repository/commits/$SourceCommit/statuses?per_page=100"
$statusResponse = Invoke-RestMethod -Uri $statusUri -Method Get -Headers $headers
$buildStatuses = @(
    $statusResponse |
        Where-Object { $_.context -eq "BuoyCalc Windows Build" } |
        Select-Object -First 1
)

if ($buildStatuses.Count -eq 0) {
    throw "Required exact-main commit status BuoyCalc Windows Build is missing for source $SourceCommit."
}

$buildStatus = $buildStatuses[0]
if ($buildStatus.state -ne "success") {
    throw "Required exact-main commit status BuoyCalc Windows Build is not green for ${SourceCommit}: state=$($buildStatus.state), target=$($buildStatus.target_url)"
}

Write-Host "Exact-main commit status confirmed: BuoyCalc Windows Build | state=$($buildStatus.state) | target=$($buildStatus.target_url)"
Write-Host "All exact-main RC prerequisite workflows and statuses are green for $SourceCommit."
