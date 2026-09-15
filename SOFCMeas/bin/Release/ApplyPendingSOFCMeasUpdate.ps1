param([int]$TargetPid)
$releaseDirectory = 'C:\SOFCMeas\bin\Release'
$stagedExe = Join-Path $releaseDirectory 'SOFCMeas.pending.exe'
$stagedPdb = Join-Path $releaseDirectory 'SOFCMeas.pending.pdb'
$targetExe = Join-Path $releaseDirectory 'SOFCMeas.exe'
$targetPdb = Join-Path $releaseDirectory 'SOFCMeas.pdb'
$statusPath = Join-Path $releaseDirectory 'SOFCMeas.update.status'
try {
    Wait-Process -Id $TargetPid -ErrorAction SilentlyContinue
    for ($attempt = 1; $attempt -le 120; $attempt++) {
        try {
            Copy-Item -LiteralPath $stagedExe -Destination $targetExe -Force -ErrorAction Stop
            Copy-Item -LiteralPath $stagedPdb -Destination $targetPdb -Force -ErrorAction Stop
            Remove-Item -LiteralPath $stagedExe,$stagedPdb -Force -ErrorAction SilentlyContinue
            Set-Content -LiteralPath $statusPath -Value ('APPLIED ' + (Get-Date -Format o)) -Encoding ASCII
            exit 0
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
    Set-Content -LiteralPath $statusPath -Value ('FAILED ' + (Get-Date -Format o)) -Encoding ASCII
    exit 1
}
catch {
    Set-Content -LiteralPath $statusPath -Value ('FAILED ' + (Get-Date -Format o)) -Encoding ASCII
    exit 1
}
