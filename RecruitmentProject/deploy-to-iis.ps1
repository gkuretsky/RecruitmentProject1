<#
.SYNOPSIS
	Publishes RecruitmentProject and deploys it to the local IIS site.

.DESCRIPTION
	1. Runs `dotnet publish` in Release mode to the local `publish` folder.
	2. Stops the IIS app pool (and site) so files aren't locked.
	3. Copies the published output into the IIS site's physical path
	   (excluding the existing app.db so live data isn't overwritten).
	4. Grants the app pool identity write access to the site folder (required
	   by SQLite to create/update app.db and its -wal/-shm files).
	5. Restarts the app pool and site. The app applies any pending EF Core
	   migrations automatically on startup (see Program.cs), so the database
	   schema stays up to date without manual `dotnet ef database update` runs.
	6. Verifies the site responds on http://localhost:8080.

.NOTES
	Must be run from an elevated (Administrator) PowerShell prompt, since it
	manages IIS via appcmd.exe.
#>

$ErrorActionPreference = "Stop"

# ----- Configuration -----
$solutionRoot   = "C:\Users\gabri\source\repos\RecruitmentProject"
$projectPath    = Join-Path $solutionRoot "RecruitmentProject\RecruitmentProject.csproj"
$publishFolder  = Join-Path $solutionRoot "publish"
$iisPhysicalPath = "C:\publish\RecruitmentProject"
$appPoolName    = "RecruitmentProject"
$siteName       = "RecruitmentProject"
$siteUrl        = "http://localhost:8080"
$appCmd         = "$env:windir\system32\inetsrv\appcmd.exe"

function Assert-Admin {
	$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
	if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
		Write-Error "This script must be run as Administrator (required to manage IIS)."
		exit 1
	}
}

Assert-Admin

Write-Host "==> Publishing project (Release)..." -ForegroundColor Cyan
dotnet publish $projectPath -c Release -o $publishFolder
if ($LASTEXITCODE -ne 0) {
	Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
	exit 1
}

Write-Host "==> Stopping IIS app pool '$appPoolName'..." -ForegroundColor Cyan
& $appCmd stop apppool /apppool.name:$appPoolName
Start-Sleep -Seconds 2

Write-Host "==> Copying published files to '$iisPhysicalPath'..." -ForegroundColor Cyan
if (-not (Test-Path $iisPhysicalPath)) {
	New-Item -ItemType Directory -Path $iisPhysicalPath -Force | Out-Null
}
# Preserve the existing database (and its WAL/SHM files) so deployed data isn't wiped out.
Copy-Item -Path (Join-Path $publishFolder "*") -Destination $iisPhysicalPath -Recurse -Force -Exclude "app.db","app.db-shm","app.db-wal"

Write-Host "==> Ensuring app pool identity can write to '$iisPhysicalPath' (needed for SQLite)..." -ForegroundColor Cyan
icacls $iisPhysicalPath /grant "IIS APPPOOL\$appPoolName`:(OI)(CI)M" /T | Out-Null

Write-Host "==> Starting IIS app pool '$appPoolName'..." -ForegroundColor Cyan
& $appCmd start apppool /apppool.name:$appPoolName

Write-Host "==> Ensuring site '$siteName' is started..." -ForegroundColor Cyan
& $appCmd start site /site.name:$siteName

Start-Sleep -Seconds 2

Write-Host "==> Verifying site responds at $siteUrl..." -ForegroundColor Cyan
try {
	$response = Invoke-WebRequest -Uri $siteUrl -UseBasicParsing -TimeoutSec 15
	Write-Host "Site responded with status $($response.StatusCode). Deployment complete!" -ForegroundColor Green
}
catch {
	Write-Warning "Site did not respond as expected: $($_.Exception.Message)"
	Write-Warning "Check the IIS logs / Event Viewer if the site does not come up."
}
