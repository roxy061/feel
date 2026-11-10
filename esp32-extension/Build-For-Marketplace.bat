@echo off
chcp 65001 >nul
title สร้างไฟล์ .VSIX สำหรับอัปโหลดขึ้น VS Code Marketplace
echo =====================================================================
echo   ตัวช่วยสร้างไฟล์ .VSIX สำหรับเอาขึ้น VS Code Marketplace
echo   (เพื่อให้ทุกคนพิมพ์ชื่อในช่องค้นหาของ VS Code แล้วกด Install ได้เลย)
echo =====================================================================
echo.
set /p PUB_ID="1. กรอกชื่อ Publisher ID ของคุณ (ที่ตั้งในเว็บ Marketplace เช่น feel-iot): "
if "%PUB_ID%"=="" set "PUB_ID=it-department"

set /p EXT_NAME="2. กรอกชื่อ Extension ที่อยากให้คนพิมพ์ค้นหา (เช่น esp32-dht-feel): "
if "%EXT_NAME%"=="" set "EXT_NAME=esp32-dht-snippets"

echo.
echo กำลังสร้างไฟล์ %EXT_NAME%-1.0.0.vsix สำหรับ Publisher: %PUB_ID% ...

powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "$pub = '%PUB_ID%'; $name = '%EXT_NAME%'; " ^
  "$src = 'C:\Users\feel           roxy\.gemini\antigravity\brain\52cc46fa-43c2-45b5-8377-3c4963788bbe\scratch\esp32-dht-snippets'; " ^
  "$pkgPath = Join-Path $src 'extension\package.json'; " ^
  "$pkg = Get-Content $pkgPath -Raw -Encoding UTF8 | ConvertFrom-Json; " ^
  "$pkg.publisher = $pub; $pkg.name = $name; $pkg.displayName = $name + ' (ESP32 DHT11 MQTT)'; " ^
  "$pkg | ConvertTo-Json -Depth 10 | Set-Content $pkgPath -Encoding UTF8; " ^
  "$manPath = Join-Path $src 'extension.vsixmanifest'; " ^
  "[xml]$man = Get-Content $manPath -Encoding UTF8; " ^
  "$man.PackageManifest.Metadata.Identity.Id = $name; " ^
  "$man.PackageManifest.Metadata.Identity.Publisher = $pub; " ^
  "$man.PackageManifest.Metadata.DisplayName = $name + ' (ESP32 DHT11 MQTT)'; " ^
  "$man.Save($manPath); " ^
  "$outVsix = Join-Path 'C:\xampp\htdocs\feel\esp32-extension' ($name + '-1.0.0.vsix'); " ^
  "if (Test-Path $outVsix) { Remove-Item $outVsix -Force }; " ^
  "Add-Type -AssemblyName System.IO.Compression; Add-Type -AssemblyName System.IO.Compression.FileSystem; " ^
  "$fs = [System.IO.File]::Open($outVsix, [System.IO.FileMode]::Create); " ^
  "$zip = New-Object System.IO.Compression.ZipArchive($fs, [System.IO.Compression.ZipArchiveMode]::Create); " ^
  "Get-ChildItem -Path $src -Recurse -File | ForEach-Object { $rel = $_.FullName.Substring($src.Length + 1).Replace('\', '/'); [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $_.FullName, $rel) | Out-Null }; " ^
  "$zip.Dispose(); $fs.Dispose(); " ^
  "Write-Host 'สร้างไฟล์สำเร็จ: ' $outVsix -ForegroundColor Green"

echo.
echo กำลังเปิดหน้าเว็บ VS Code Marketplace เพื่อให้อัปโหลดไฟล์...
start https://marketplace.visualstudio.com/manage
pause
