$connString = "Server=db58883.public.databaseasp.net; Database=db58883; User Id=db58883; Password=4e%ZT=8hbK+7; Encrypt=True; TrustServerCertificate=True; MultipleActiveResultSets=True;"

$query = @"
SELECT TOP 5 Id, MedicineName, ReminderTimesJson, StartDate, EndDate, IsActive, CreatedAt 
FROM MedicineReminders 
ORDER BY CreatedAt DESC;

SELECT TOP 10 ReminderId, SentAt, Channel, IsSuccess, ErrorMessage 
FROM MedicineReminderLogs 
ORDER BY SentAt DESC;
"@

$conn = New-Object System.Data.SqlClient.SqlConnection($connString)
$cmd = $conn.CreateCommand()
$cmd.CommandText = $query

try {
    $conn.Open()
    $reader = $cmd.ExecuteReader()
    
    Write-Host "--- Medicine Reminders ---"
    while ($reader.Read()) {
        Write-Host ("Name: " + $reader["MedicineName"] + " | Times: " + $reader["ReminderTimesJson"] + " | Start: " + $reader["StartDate"] + " | Active: " + $reader["IsActive"] + " | CreatedAt: " + $reader["CreatedAt"])
    }
    
    if ($reader.NextResult()) {
        Write-Host "`n--- Medicine Reminder Logs ---"
        while ($reader.Read()) {
            Write-Host ("ReminderId: " + $reader["ReminderId"] + " | SentAt: " + $reader["SentAt"] + " | Channel: " + $reader["Channel"] + " | Success: " + $reader["IsSuccess"] + " | Error: " + $reader["ErrorMessage"])
        }
    }
}
catch {
    Write-Host "Error: $_"
}
finally {
    if ($conn.State -eq 'Open') { $conn.Close() }
}
