# TEST LOG - HeriStep Audio Queue Feature
API Base URL: http://127.0.0.1:5297
Tester: _______________
Ngay test: 2026-05-06
Moi truong: Local Dev (dotnet run)

---

## Danh sach Test Case

| ID    | Nhom             | Ten test                                    | Trang thai |
|-------|------------------|---------------------------------------------|------------|
| TC-01 | GET Listen Info  | Lay thong tin sap hop le                    | [ ]        |
| TC-02 | GET Listen Info  | Sap khong ton tai                           | [ ]        |
| TC-03 | POST Track Visit | Ghi nhan luot nghe hop le (real-time)       | [ ]        |
| TC-04 | POST Track Visit | Duration am -> bi bo qua (anti-spam)        | [ ]        |
| TC-05 | POST Track Visit | Duration = 0 -> bi bo qua                  | [ ]        |
| TC-06 | POST Stall Visit | Day vao Queue (high-concurrency path)       | [ ]        |
| TC-07 | POST Stall Visit | Body null -> tra loi 400                    | [ ]        |
| TC-08 | GET Avg Listen   | Analytics tra dung du lieu                  | [ ]        |
| TC-09 | LOAD TEST        | 500 nguoi cung luc                          | [ ]        |
| TC-10 | LOAD TEST        | 5000 nguoi cung luc (gioi han Queue)        | [ ]        |
| TC-11 | POST Trajectory  | Offline sync batch (LocationLog+ListenLog)  | [ ]        |
| TC-12 | VERIFY DB        | Kiem tra du lieu da vao bang StallVisits    | [ ]        |

---

## TC-01 - GET Listen Info: Sap hop le

Muc dich: Xac nhan API tra dung thong tin sap va danh sach ngon ngu TTS.
Endpoint: GET /api/listen/info/{stallId}

### Lenh chay (PowerShell):
```
Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/listen/info/1" -Method GET | ConvertTo-Json -Depth 5
```

### Lenh chay (curl):
```
curl -X GET "http://127.0.0.1:5297/api/listen/info/1" -H "accept: application/json"
```

### Ket qua mong doi:
HTTP 200
{
  "stallName": "Sap hang 730",
  "imageUrl": "...",
  "availableLanguages": [
    { "langCode": "vi", "langName": "Tieng Viet", "flagIconUrl": "/icons/flags/vi.png", "ttsScript": "Chao mung..." }
  ]
}

Dieu kien PASS: HTTP 200, co stallName, availableLanguages khong rong, moi item co ttsScript.
Dieu kien FAIL: HTTP 4xx/5xx, availableLanguages rong.

### Ket qua thuc te:
HTTP Status: ___
Response: _______________

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-02 - GET Listen Info: Sap khong ton tai

Endpoint: GET /api/listen/info/99999

### Lenh chay:
```
Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/listen/info/99999" -Method GET
```

### Ket qua mong doi:
HTTP 404
{ "message": "Sap khong ton tai hoac da dong cua." }

Dieu kien PASS: HTTP 404, co truong message.

### Ket qua thuc te:
HTTP Status: ___
Response: _______________

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-03 - POST Track Visit: Ghi nhan hop le (Real-time path)

Muc dich: Xac nhan luot nghe hop le duoc ghi thang vao DB qua ListenController.
Endpoint: POST /api/listen/track-visit

### Lenh chay (PowerShell):
```
$body = @{ stallId = 1; deviceId = "test-device-001"; duration = 45 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/listen/track-visit" -Method POST -ContentType "application/json" -Body $body
```

### Ket qua mong doi:
HTTP 200
{ "message": "Tracked successfully" }

Dieu kien PASS: HTTP 200, message = "Tracked successfully", kiem tra DB thay ban ghi moi (TC-12).

### Ket qua thuc te:
HTTP Status: ___
Response: _______________

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-04 - POST Track Visit: Duration am -> Anti-spam

Endpoint: POST /api/listen/track-visit voi duration = -5

### Lenh chay:
```
$body = @{ stallId = 1; deviceId = "test-device-spam"; duration = -5 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/listen/track-visit" -Method POST -ContentType "application/json" -Body $body
```

### Ket qua mong doi:
HTTP 200
{ "message": "Skipped tracking (invalid duration)" }

Dieu kien PASS: HTTP 200, message = "Skipped...", KHONG co ban ghi moi trong DB.

### Ket qua thuc te:
HTTP Status: ___
Response: _______________

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-05 - POST Track Visit: Duration = 0 -> Anti-spam

### Lenh chay:
```
$body = @{ stallId = 1; deviceId = "test-device-zero"; duration = 0 } | ConvertTo-Json
Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/listen/track-visit" -Method POST -ContentType "application/json" -Body $body
```

Dieu kien PASS: HTTP 200, message = "Skipped..." (vi duration < 1).

### Ket qua thuc te:
HTTP Status: ___
Response: _______________

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-06 - POST Stall Visit: Day vao Queue (High-concurrency path)

Muc dich: Xac nhan API phan hoi ngay lap tuc, Worker ghi DB ngam.
Endpoint: POST /api/analytics/stall-visit

### Lenh chay:
```
$body = @{
    id                    = [System.Guid]::NewGuid().ToString()
    stallId               = 1
    deviceId              = "test-device-queue-001"
    visitedAt             = (Get-Date).ToString("o")
    createdAtServer       = (Get-Date).ToString("o")
    listenDurationSeconds = 30
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/analytics/stall-visit" -Method POST -ContentType "application/json" -Body $body
```

### Ket qua mong doi:
HTTP 200
{ "message": "Ghi nhan thanh cong, dang xu ly ngam." }

Dieu kien PASS: HTTP 200 phan hoi NGAY LAP TUC (< 100ms), sau 2s kiem tra DB thay ban ghi moi.

### Ket qua thuc te:
HTTP Status: ___
Response time: ___ ms
Response: _______________

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-07 - POST Stall Visit: Body null -> Loi 400

### Lenh chay:
```
Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/analytics/stall-visit" -Method POST -ContentType "application/json" -Body "null"
```

Dieu kien PASS: HTTP 400, co truong Message.

### Ket qua thuc te:
HTTP Status: ___
Response: _______________

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-08 - GET Avg Listen Time: Analytics tra dung du lieu

Endpoint: GET /api/analytics/avg-listen-time

### Lenh chay:
```
Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/analytics/avg-listen-time" -Method GET | ConvertTo-Json
```

### Ket qua mong doi:
HTTP 200
[
  { "stallName": "Sap hang 730", "avgSeconds": 32.5, "visitCount": 5 },
  { "stallName": "Sap hang 136", "avgSeconds": 20.0, "visitCount": 2 }
]

Dieu kien PASS: HTTP 200, mang >= 1 phan tu, moi phan tu co stallName, avgSeconds > 0, visitCount > 0.

### Ket qua thuc te:
HTTP Status: ___
So sap tra ve: ___
Response: _______________

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-09 - LOAD TEST: 500 nguoi cung luc

Muc dich: Kiem tra he thong khong bi nghen hay crash khi 500 requests dong thoi.

### Script PowerShell ban 500 requests dong thoi:
```
$jobs = 1..500 | ForEach-Object {
    $i = $_
    Start-Job -ScriptBlock {
        param($idx)
        $body = @{
            id                    = [System.Guid]::NewGuid().ToString()
            stallId               = ($idx % 5) + 1
            deviceId              = "load-test-device-$idx"
            visitedAt             = (Get-Date).ToString("o")
            createdAtServer       = (Get-Date).ToString("o")
            listenDurationSeconds = ($idx % 60) + 5
        } | ConvertTo-Json
        try {
            Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/analytics/stall-visit" `
                -Method POST -ContentType "application/json" -Body $body | Out-Null
            return "OK"
        } catch { return "FAIL: $_" }
    } -ArgumentList $i
}

Write-Host "Dang cho ket qua 500 jobs..."
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
$ok   = ($results | Where-Object { $_ -eq "OK" }).Count
$fail = ($results | Where-Object { $_ -ne "OK" }).Count
Write-Host "Thanh cong : $ok / 500"
Write-Host "That bai   : $fail / 500"
```

Dieu kien PASS:
- Thanh cong >= 498 / 500 (cho phep <= 0.4% loi mang)
- Server KHONG crash, terminal dotnet run van chay
- RAM Server tang khong qua 50 MB so voi truoc test
- Sau 5 giay: kiem tra DB thay ~500 ban ghi moi

### Ket qua thuc te:
Thanh cong : ___ / 500
That bai   : ___ / 500
Thoi gian  : ___ giay
RAM Server truoc: ___ MB  |  Sau: ___ MB
Ban ghi DB sau 5s: ___
Server con chay: [ ] Co  [ ] Khong

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-10 - STRESS TEST: 5000 nguoi (gioi han Queue)

CANH BAO: Test nay ton nhieu tai nguyen hon. Dam bao khong co process nang nao khac dang chay.

### Script PowerShell ban 5000 requests theo tung batch 500:
```
$totalSuccess = 0
$totalFail    = 0

for ($batch = 1; $batch -le 10; $batch++) {
    $start = ($batch - 1) * 500 + 1
    $end   = $batch * 500
    $jobs = $start..$end | ForEach-Object {
        $i = $_
        Start-Job -ScriptBlock {
            param($idx)
            $body = @{
                id                    = [System.Guid]::NewGuid().ToString()
                stallId               = ($idx % 10) + 1
                deviceId              = "stress-device-$idx"
                visitedAt             = (Get-Date).ToString("o")
                createdAtServer       = (Get-Date).ToString("o")
                listenDurationSeconds = ($idx % 120) + 5
            } | ConvertTo-Json
            try {
                Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/analytics/stall-visit" `
                    -Method POST -ContentType "application/json" -Body $body | Out-Null
                return "OK"
            } catch { return "FAIL" }
        } -ArgumentList $i
    }
    $results       = $jobs | Wait-Job | Receive-Job
    $jobs | Remove-Job
    $ok   = ($results | Where-Object { $_ -eq "OK" }).Count
    $fail = ($results | Where-Object { $_ -ne "OK" }).Count
    $totalSuccess += $ok
    $totalFail    += $fail
    Write-Host "Batch $batch/10: OK=$ok  FAIL=$fail"
}

Write-Host "=== KET QUA TONG ==="
Write-Host "Thanh cong : $totalSuccess / 5000"
Write-Host "That bai   : $totalFail / 5000"
```

Dieu kien PASS:
- Thanh cong >= 4950 / 5000 (cho phep <= 1% loi)
- Khong co OutOfMemoryException trong log server
- Server van phan hoi request moi sau khi test xong (khong deadlock)
- Sau 30 giay: Bang StallVisits co ~5000 ban ghi moi

### Ket qua thuc te:
Thanh cong : ___ / 5000
That bai   : ___ / 5000
Thoi gian  : ___ giay
RAM Server truoc: ___ MB  |  Sau: ___ MB
OutOfMemoryException: [ ] Khong  [ ] Co -> ___
Ban ghi DB sau 30s: ___
Server van phan hoi sau test: [ ] Co  [ ] Khong

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-11 - POST Trajectory: Offline sync batch

Muc dich: Xac nhan offline sync dong thoi GPS logs + Listen logs trong 1 request.
Endpoint: POST /api/analytics/trajectory

### Lenh chay:
```
$body = @{
    deviceId     = "offline-test-device-999"
    locationLogs = @(
        @{ lat = 10.7769; lng = 106.7009; timestamp = (Get-Date).AddMinutes(-10).ToString("o") }
        @{ lat = 10.7770; lng = 106.7010; timestamp = (Get-Date).AddMinutes(-5).ToString("o") }
    )
    listenLogs   = @(
        @{ poiId = 1; listenDurationSeconds = 60 }
        @{ poiId = 2; listenDurationSeconds = 45 }
    )
} | ConvertTo-Json -Depth 5

Invoke-RestMethod -Uri "http://127.0.0.1:5297/api/analytics/trajectory" -Method POST -ContentType "application/json" -Body $body
```

### Ket qua mong doi:
HTTP 200
{ "Message": "Dong bo du lieu tracking thanh cong." }

Dieu kien PASS: HTTP 200, DB co 2 ban ghi moi trong TouristTrajectories va 2 ban ghi moi trong StallVisits voi deviceId = "offline-test-device-999".

### Ket qua thuc te:
HTTP Status: ___
Response: _______________
Ban ghi TouristTrajectories moi: ___
Ban ghi StallVisits moi: ___

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## TC-12 - VERIFY DB: Kiem tra du lieu trong bang StallVisits

Muc dich: Xac nhan Worker da ghi dung du lieu tu Queue vao DB.
Cong cu: SSMS hoac Azure Data Studio

### Query SQL kiem tra:
```sql
USE VinhKhanhTourDB;

-- Xem 20 ban ghi moi nhat
SELECT TOP 20
    Id, StallId, DeviceId, ListenDurationSeconds, VisitedAt, created_at_server AS CreatedAtServer
FROM StallVisits
ORDER BY created_at_server DESC;

-- Dem tong ban ghi test
SELECT COUNT(*) AS TotalTestRecords
FROM StallVisits
WHERE DeviceId LIKE 'test-device-%'
   OR DeviceId LIKE 'load-test-%'
   OR DeviceId LIKE 'stress-device-%'
   OR DeviceId LIKE 'offline-test-%';

-- Thong ke theo sap
SELECT StallId, COUNT(*) AS VisitCount, AVG(CAST(ListenDurationSeconds AS FLOAT)) AS AvgSeconds
FROM StallVisits
WHERE ListenDurationSeconds > 0
GROUP BY StallId
ORDER BY VisitCount DESC;
```

Dieu kien PASS:
- created_at_server la thoi gian UTC do Server ghi, khong phai gio client
- Khong co ban ghi nao co ListenDurationSeconds < 1
- Tong ban ghi khop voi tong request thanh cong o cac TC truoc

### Ket qua thuc te:
Tong ban ghi test trong DB: ___
Ban ghi tu TC-03 (track-visit real-time): ___
Ban ghi tu TC-06 (stall-visit queue): ___
Ban ghi tu TC-09 (500 load test): ___
Ban ghi tu TC-10 (5000 stress test): ___
created_at_server dung UTC: [ ] Co  [ ] Khong
Co ban ghi Duration < 1: [ ] Khong  [ ] Co (BUG!)

Trang thai: [ ] Pass  [ ] Fail
Ghi chu: _______________

---

## Bang Tong Ket Ket Qua

| ID    | Ten test                        | Ket qua                  | Ghi chu |
|-------|---------------------------------|--------------------------|---------|
| TC-01 | GET Listen Info - hop le        | [ ] Pass  [ ] Fail       |         |
| TC-02 | GET Listen Info - khong ton tai | [ ] Pass  [ ] Fail       |         |
| TC-03 | POST Track Visit - hop le       | [ ] Pass  [ ] Fail       |         |
| TC-04 | POST Track Visit - Duration am  | [ ] Pass  [ ] Fail       |         |
| TC-05 | POST Track Visit - Duration = 0 | [ ] Pass  [ ] Fail       |         |
| TC-06 | POST Stall Visit - Queue path   | [ ] Pass  [ ] Fail       |         |
| TC-07 | POST Stall Visit - body null    | [ ] Pass  [ ] Fail       |         |
| TC-08 | GET Avg Listen Time             | [ ] Pass  [ ] Fail       |         |
| TC-09 | Load Test 500 nguoi             | [ ] Pass  [ ] Fail       |         |
| TC-10 | Stress Test 5000 nguoi          | [ ] Pass  [ ] Fail       |         |
| TC-11 | Offline sync batch              | [ ] Pass  [ ] Fail       |         |
| TC-12 | Verify DB                       | [ ] Pass  [ ] Fail       |         |

Tong Pass: ___ / 12
Tong Fail: ___ / 12
Danh gia tong the: [ ] Dat yeu cau  [ ] Can sua

Tester ky ten: _______________  Ngay: _______________

---

## Don dep Du lieu Test (Cleanup)

Chay sau khi test xong de tranh lam nhieu du lieu thuc te:

```sql
USE VinhKhanhTourDB;

DELETE FROM StallVisits
WHERE DeviceId LIKE 'test-device-%'
   OR DeviceId LIKE 'load-test-%'
   OR DeviceId LIKE 'stress-device-%'
   OR DeviceId LIKE 'offline-test-%';

DELETE FROM TouristTrajectories
WHERE DeviceId LIKE 'offline-test-%';

PRINT N'Da don dep xong du lieu test.';
```

---
File test log - HeriStep Audio Queue Feature v1.2
Tao boi Antigravity AI - 2026-05-06
