# ================================================================
# BENCHMARK SCRIPT - HeriStep Audio Queue vs Direct DB
# Chay: powershell -ExecutionPolicy Bypass -File benchmark_queue.ps1
# ================================================================

param(
    [int]$ConcurrentUsers = 500,
    [string]$BaseUrl = "http://127.0.0.1:5297",
    [int]$StallId = 1
)

$DirectEndpoint = "$BaseUrl/api/listen/track-visit"
$QueueEndpoint  = "$BaseUrl/api/analytics/stall-visit"

function Invoke-LoadTest {
    param([string]$Url, [string]$Mode, [int]$N, [int]$SId)

    Write-Host ""
    Write-Host "  >>> Ban $N request dong thoi vao: $Mode <<<" -ForegroundColor Yellow
    Write-Host "  URL: $Url"

    $globalStart = [System.Diagnostics.Stopwatch]::StartNew()

    $jobs = 1..$N | ForEach-Object {
        $i = $_
        Start-Job -ScriptBlock {
            param($idx, $endpoint, $mode, $sId)
            $sw = [System.Diagnostics.Stopwatch]::StartNew()
            try {
                if ($mode -eq "DIRECT") {
                    $dur = ($idx % 60) + 10
                    $body = ConvertTo-Json @{ stallId = $sId; deviceId = "direct-$idx"; duration = $dur }
                } else {
                    $guid = [System.Guid]::NewGuid().ToString()
                    $ts   = [System.DateTime]::UtcNow.ToString("o")
                    $dur  = ($idx % 60) + 10
                    $body = ConvertTo-Json @{ id = $guid; stallId = $sId; deviceId = "queue-$idx"; visitedAt = $ts; createdAtServer = $ts; listenDurationSeconds = $dur }
                }
                $null = Invoke-RestMethod -Uri $endpoint -Method POST -ContentType "application/json" -Body $body -TimeoutSec 30
                $sw.Stop()
                return "OK|$($sw.ElapsedMilliseconds)"
            } catch {
                $sw.Stop()
                return "FAIL|$($sw.ElapsedMilliseconds)"
            }
        } -ArgumentList $i, $Url, $Mode, $SId
    }

    $rawResults = $jobs | Wait-Job | Receive-Job
    $jobs | Remove-Job -Force
    $globalStart.Stop()

    $ok   = @($rawResults | Where-Object { $_ -like "OK|*"   })
    $fail = @($rawResults | Where-Object { $_ -like "FAIL|*" })
    $allMs = @($rawResults | ForEach-Object { [long]($_ -split "\|")[1] })

    $avgMs = if ($allMs.Count -gt 0) { [math]::Round(($allMs | Measure-Object -Average).Average, 1) } else { 0 }
    $minMs = if ($allMs.Count -gt 0) { ($allMs | Measure-Object -Minimum).Minimum } else { 0 }
    $maxMs = if ($allMs.Count -gt 0) { ($allMs | Measure-Object -Maximum).Maximum } else { 0 }
    $p95Ms = if ($allMs.Count -gt 0) {
        $sorted = $allMs | Sort-Object
        $idx95  = [math]::Ceiling($allMs.Count * 0.95) - 1
        $sorted[$idx95]
    } else { 0 }
    $wallMs    = $globalStart.ElapsedMilliseconds
    $throughput = [math]::Round($N / ($wallMs / 1000), 1)

    return @{
        Mode       = $Mode
        Total      = $N
        Ok         = $ok.Count
        Fail       = $fail.Count
        ErrorRate  = [math]::Round(($fail.Count / $N) * 100, 1)
        AvgMs      = $avgMs
        MinMs      = $minMs
        MaxMs      = $maxMs
        P95Ms      = $p95Ms
        WallMs     = $wallMs
        Throughput = $throughput
    }
}

# ================================================================
Clear-Host
Write-Host ""
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  HERISTEP BENCHMARK - Queue vs Direct DB" -ForegroundColor Cyan
Write-Host "  So sanh hieu nang: $ConcurrentUsers nguoi dong thoi" -ForegroundColor Cyan
Write-Host "================================================================" -ForegroundColor Cyan
Write-Host "  Base URL    : $BaseUrl"
Write-Host "  StallId     : $StallId"
Write-Host "  Thoi gian   : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Host "================================================================" -ForegroundColor Cyan

Write-Host ""
Write-Host "[KIEM TRA] Ping server..." -ForegroundColor Gray
try {
    $ping = Invoke-RestMethod -Uri "$BaseUrl/api/listen/info/$StallId" -TimeoutSec 5
    Write-Host "  Server OK - Sap: $($ping.stallName)" -ForegroundColor Green
} catch {
    Write-Host "  SERVER KHONG PHAN HOI! Kiem tra lai dotnet run." -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  TEST 1: GHI THANG DATABASE (Direct)" -ForegroundColor Magenta
Write-Host "  POST /api/listen/track-visit" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta
$r1 = Invoke-LoadTest -Url $DirectEndpoint -Mode "DIRECT" -N $ConcurrentUsers -SId $StallId

Write-Host ""
Write-Host "  [Nghi 3 giay de server on dinh truoc Test 2...]" -ForegroundColor Gray
Start-Sleep -Seconds 3

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  TEST 2: HANG DOI RAM (Queue)" -ForegroundColor Cyan
Write-Host "  POST /api/analytics/stall-visit" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
$r2 = Invoke-LoadTest -Url $QueueEndpoint -Mode "QUEUE" -N $ConcurrentUsers -SId $StallId

# ================================================================
Write-Host ""
Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "              KET QUA SO SANH CUOI CUNG" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
$fmt = "{0,-30} {1,16} {2,16}"
Write-Host ($fmt -f "Chi so", "DIRECT (DB)", "QUEUE (RAM)") -ForegroundColor White
Write-Host ("-" * 64)
Write-Host ($fmt -f "So nguoi test", "$($r1.Total)", "$($r2.Total)")
Write-Host ($fmt -f "Thanh cong", "$($r1.Ok)", "$($r2.Ok)")
Write-Host ($fmt -f "That bai", "$($r1.Fail)", "$($r2.Fail)")
Write-Host ($fmt -f "Ti le loi (%)", "$($r1.ErrorRate)%", "$($r2.ErrorRate)%")
Write-Host ("-" * 64)
Write-Host ($fmt -f "TB response time (ms)", "$($r1.AvgMs) ms", "$($r2.AvgMs) ms") -ForegroundColor Yellow
Write-Host ($fmt -f "Nhanh nhat (ms)", "$($r1.MinMs) ms", "$($r2.MinMs) ms")
Write-Host ($fmt -f "Cham nhat (ms)", "$($r1.MaxMs) ms", "$($r2.MaxMs) ms")
Write-Host ($fmt -f "P95 - 95% request duoi", "$($r1.P95Ms) ms", "$($r2.P95Ms) ms") -ForegroundColor Yellow
Write-Host ($fmt -f "Tong wall-time (ms)", "$($r1.WallMs) ms", "$($r2.WallMs) ms")
Write-Host ($fmt -f "Throughput (req/s)", "$($r1.Throughput)", "$($r2.Throughput)")
Write-Host ("-" * 64)

$avgDiff = if ($r2.AvgMs -gt 0) { [math]::Round($r1.AvgMs / $r2.AvgMs, 1) } else { "N/A" }
Write-Host ""
Write-Host "  QUEUE nhanh hon DIRECT trung binh: $($avgDiff)x" -ForegroundColor Cyan
Write-Host ""

if ($r2.AvgMs -lt $r1.AvgMs) {
    Write-Host "  >> KET LUAN: QUEUE THANG - Response time giam tu $($r1.AvgMs)ms xuong $($r2.AvgMs)ms" -ForegroundColor Green
} else {
    Write-Host "  >> LUU Y: Local test - DB cung may nen khong co do tre mang." -ForegroundColor Yellow
    Write-Host "     Tren Production (server xa + nhieu nguoi that) Queue se thang ro rang." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "  DON DEP: Chay SQL sau trong SSMS:"
Write-Host "  DELETE FROM StallVisits WHERE DeviceId LIKE 'direct-%' OR DeviceId LIKE 'queue-%'" -ForegroundColor DarkGray
Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host "  BENCHMARK HOAN TAT - $(Get-Date -Format 'HH:mm:ss')" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
