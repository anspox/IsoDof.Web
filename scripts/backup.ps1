<#
.SYNOPSIS
    ISO DÖF veritabanını ve yüklenen dosyaları yedekler, eski yedekleri temizler.

.DESCRIPTION
    1. SQL Server "BACKUP DATABASE" ile tam veritabanı yedeği alır (.bak, sıkıştırılmış ve doğrulanmış).
    2. Yüklenen DÖF eklerinin klasörünü .zip olarak arşivler.
    3. RetentionDays günden eski yedek dosyalarını siler.

    Not: .bak dosyasını SQL Server servisi yazar. BackupDir yolu SQL Server'ın çalıştığı makinede
    bulunmalı ve SQL Server servis hesabının bu klasöre yazma izni olmalıdır.

.EXAMPLE
    # Elle çalıştırma
    powershell -ExecutionPolicy Bypass -File scripts\backup.ps1 -ServerInstance "SQLSUNUCU" -BackupDir "D:\Yedekler\IsoDof" -UploadsDir "D:\IsoDofData\uploads"

.EXAMPLE
    # Her gece 02:00'de çalışacak zamanlanmış görev oluşturma (yönetici PowerShell):
    $action  = New-ScheduledTaskAction -Execute "powershell.exe" -Argument '-NoProfile -ExecutionPolicy Bypass -File "C:\inetpub\IsoDof\scripts\backup.ps1" -ServerInstance "SQLSUNUCU" -BackupDir "D:\Yedekler\IsoDof" -UploadsDir "D:\IsoDofData\uploads"'
    $trigger = New-ScheduledTaskTrigger -Daily -At 2am
    Register-ScheduledTask -TaskName "IsoDof Gece Yedeği" -Action $action -Trigger $trigger -User "SYSTEM" -RunLevel Highest
#>
[CmdletBinding()]
param(
    [string]$ServerInstance = "(localdb)\mssqllocaldb",
    [string]$Database = "IsoDofDb",
    [Parameter(Mandatory = $true)][string]$BackupDir,
    [string]$UploadsDir,
    [int]$RetentionDays = 14,
    # SQL kimlik doğrulaması kullanılacaksa; boş bırakılırsa Windows kimlik doğrulaması kullanılır.
    [string]$SqlUser,
    [string]$SqlPassword
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
New-Item -ItemType Directory -Force -Path $BackupDir | Out-Null

function Write-Log([string]$message) {
    $line = "[{0}] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $message
    Write-Output $line
    Add-Content -Path (Join-Path $BackupDir "backup.log") -Value $line -Encoding UTF8
}

try {
    # --- 1. Veritabanı yedeği ---
    $bakFile = Join-Path $BackupDir "$Database-$timestamp.bak"
    $connectionString = if ($SqlUser) {
        "Server=$ServerInstance;Database=master;User Id=$SqlUser;Password=$SqlPassword;TrustServerCertificate=True"
    } else {
        "Server=$ServerInstance;Database=master;Integrated Security=True;TrustServerCertificate=True"
    }

    # LocalDB ve Express sürümleri yedek sıkıştırmayı desteklemez.
    $connection = New-Object System.Data.SqlClient.SqlConnection $connectionString
    $connection.Open()
    try {
        $editionCmd = $connection.CreateCommand()
        $editionCmd.CommandText = "SELECT CAST(SERVERPROPERTY('EngineEdition') AS int)"
        $engineEdition = [int]$editionCmd.ExecuteScalar()
        # 2 = Standard, 3 = Enterprise; 4 = Express (LocalDB dahil)
        $compression = if ($engineEdition -in 2, 3) { "COMPRESSION, " } else { "" }

        $safeDb = $Database.Replace("]", "]]")
        $safeFile = $bakFile.Replace("'", "''")

        $cmd = $connection.CreateCommand()
        $cmd.CommandTimeout = 0
        $cmd.CommandText = "BACKUP DATABASE [$safeDb] TO DISK = N'$safeFile' WITH ${compression}CHECKSUM, INIT, NAME = N'$safeDb tam yedek $timestamp'"
        [void]$cmd.ExecuteNonQuery()

        $verify = $connection.CreateCommand()
        $verify.CommandTimeout = 0
        $verify.CommandText = "RESTORE VERIFYONLY FROM DISK = N'$safeFile' WITH CHECKSUM"
        [void]$verify.ExecuteNonQuery()
    }
    finally {
        $connection.Close()
    }
    Write-Log "Veritabanı yedeği alındı ve doğrulandı: $bakFile"

    # --- 2. Yüklenen dosyaların yedeği ---
    if ($UploadsDir) {
        if (Test-Path $UploadsDir) {
            $zipFile = Join-Path $BackupDir "uploads-$timestamp.zip"
            Compress-Archive -Path (Join-Path $UploadsDir "*") -DestinationPath $zipFile -CompressionLevel Optimal -Force
            Write-Log "Yüklenen dosyalar arşivlendi: $zipFile"
        } else {
            Write-Log "UYARI: Yüklenen dosyalar klasörü bulunamadı, atlandı: $UploadsDir"
        }
    }

    # --- 3. Eski yedeklerin temizliği ---
    $cutoff = (Get-Date).AddDays(-$RetentionDays)
    Get-ChildItem -Path $BackupDir -File |
        Where-Object { ($_.Name -like "$Database-*.bak" -or $_.Name -like "uploads-*.zip") -and $_.LastWriteTime -lt $cutoff } |
        ForEach-Object {
            Remove-Item $_.FullName -Force
            Write-Log "Eski yedek silindi: $($_.Name)"
        }

    Write-Log "Yedekleme tamamlandı."
}
catch {
    Write-Log "HATA: Yedekleme başarısız oldu. $($_.Exception.Message)"
    exit 1
}
