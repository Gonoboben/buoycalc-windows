$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path

function Read-RepoText([string]$relativePath) {
    $path = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Required governance file is missing: $relativePath"
    }

    return Get-Content -LiteralPath $path -Raw
}

function Assert-Contains([string]$content, [string]$needle, [string]$label) {
    if (-not $content.Contains($needle)) {
        throw "$label does not contain required text: $needle"
    }
}

function Assert-NotContains([string]$content, [string]$needle, [string]$label) {
    if ($content.Contains($needle)) {
        throw "$label contains forbidden text: $needle"
    }
}

$prTemplate = Read-RepoText ".github/pull_request_template.md"
Assert-Contains $prTemplate "1 Issue -> 1 branch -> 1 PR" "PR template"
Assert-Contains $prTemplate "Numerical / physics impact" "PR template"
Assert-Contains $prTemplate "Engineering invariants preserved" "PR template"
Assert-Contains $prTemplate ".NET Build" "PR template"
Assert-Contains $prTemplate "Selected Shape Consumer Scan" "PR template"
Assert-Contains $prTemplate "Report Store Consumer Scan" "PR template"
Assert-Contains $prTemplate "Out of scope" "PR template"
Assert-Contains $prTemplate "AGENTS.md" "PR template"

$optimizationForm = Read-RepoText ".github/ISSUE_TEMPLATE/optimization.yml"
Assert-Contains $optimizationForm "Behavior-preserving optimization" "Optimization issue form"
Assert-Contains $optimizationForm "Engineering invariants to preserve" "Optimization issue form"
Assert-Contains $optimizationForm "Validation / architecture guards" "Optimization issue form"
Assert-Contains $optimizationForm "I will not change solver/physics as a side effect" "Optimization issue form"

$physicsForm = Read-RepoText ".github/ISSUE_TEMPLATE/physics-rfc.yml"
Assert-Contains $physicsForm "Physics / solver RFC" "Physics RFC issue form"
Assert-Contains $physicsForm "Proposed equations and assumptions" "Physics RFC issue form"
Assert-Contains $physicsForm "Units and sign conventions" "Physics RFC issue form"
Assert-Contains $physicsForm "Acceptance tolerances" "Physics RFC issue form"
Assert-Contains $physicsForm "Validation evidence plan" "Physics RFC issue form"
Assert-Contains $physicsForm "Visual agreement alone will not be treated as validation" "Physics RFC issue form"

$branchScript = Read-RepoText "tools/branch-hygiene.ps1"
Assert-Contains $branchScript "DRY RUN ONLY" "Branch hygiene script"
Assert-Contains $branchScript '$defaultBranch' "Branch hygiene script"
Assert-Contains $branchScript '$openBranchNames.Contains($branchName)' "Branch hygiene script"
Assert-Contains $branchScript 'merged-pr-current-head-match' "Branch hygiene script"
Assert-Contains $branchScript 'explicitly-superseded-closed-pr-current-head-match' "Branch hygiene script"
Assert-Contains $branchScript '[string]$_.head.sha -eq $currentHeadSha' "Branch hygiene script"
Assert-Contains $branchScript 'ConvertTo-Json' "Branch hygiene script"
Assert-Contains $branchScript 'branch-hygiene.md' "Branch hygiene script"
Assert-NotContains $branchScript "-Method Delete" "Branch hygiene script"
Assert-NotContains $branchScript "git push --delete" "Branch hygiene script"
Assert-NotContains $branchScript "git branch -D" "Branch hygiene script"
Assert-NotContains $branchScript "Remove-Git" "Branch hygiene script"

$workflow = Read-RepoText ".github/workflows/branch-hygiene.yml"
Assert-Contains $workflow "workflow_dispatch:" "Branch hygiene workflow"
Assert-Contains $workflow "contents: read" "Branch hygiene workflow"
Assert-Contains $workflow "pull-requests: read" "Branch hygiene workflow"
Assert-Contains $workflow "tools/branch-hygiene.ps1" "Branch hygiene workflow"
Assert-Contains $workflow "actions/upload-artifact@v4" "Branch hygiene workflow"
Assert-Contains $workflow "branch-hygiene-dry-run" "Branch hygiene workflow"
Assert-NotContains $workflow "contents: write" "Branch hygiene workflow"
Assert-NotContains $workflow "pull-requests: write" "Branch hygiene workflow"
Assert-NotContains $workflow "delete" "Branch hygiene workflow"
Assert-NotContains $workflow "schedule:" "Branch hygiene workflow"

Write-Host "Repository governance smoke check passed."
