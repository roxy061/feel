@echo off
chcp 65001 >nul
title ติดตั้งส่วนเสริม ESP32 DHT11 MQTT สำหรับ VS Code
echo ========================================================
echo   กำลังติดตั้งส่วนเสริม ESP32 DHT11 MQTT Snippets ^& Helper
echo ========================================================
echo.

set "VSIX_FILE=%~dp0esp32-dht-snippets-1.0.0.vsix"
set "CODE_CMD="

where code >nul 2>nul
if %errorlevel%==0 (
    set "CODE_CMD=code"
) else if exist "%LOCALAPPDATA%\Programs\Microsoft VS Code\bin\code.cmd" (
    set "CODE_CMD=%LOCALAPPDATA%\Programs\Microsoft VS Code\bin\code.cmd"
) else if exist "%ProgramFiles%\Microsoft VS Code\bin\code.cmd" (
    set "CODE_CMD=%ProgramFiles%\Microsoft VS Code\bin\code.cmd"
) else if exist "%ProgramFiles(x86)%\Microsoft VS Code\bin\code.cmd" (
    set "CODE_CMD=%ProgramFiles(x86)%\Microsoft VS Code\bin\code.cmd"
)

if "%CODE_CMD%"=="" (
    echo [!] ไม่พบโปรแกรม Visual Studio Code ในเครื่องนี้
    echo กรุณาติดตั้ง VS Code หรือติดตั้งผ่านเมนู Extensions -^> Install from VSIX...
    pause
    exit /b 1
)

echo [1/2] กำลังติดตั้ง Extension เข้าสู่ VS Code...
call "%CODE_CMD%" --install-extension "%VSIX_FILE%" --force

echo.
echo [2/2] กำลังเปิดโปรแกรม VS Code พร้อมโค้ด ESP32 DHT11 MQTT...
start "" "%CODE_CMD%"
echo.
echo ========================================================
echo   ติดตั้งสำเร็จเรียบร้อย!
echo   - โค้ดจะเด้งขึ้นมาใน VS Code อัตโนมัติ
echo   - หรือกดปุ่ม "เด้งโค้ด ESP32 DHT" ที่แถบมุมซ้ายล่างของ VS Code
echo   - หรือพิมพ์คำว่า esp32dht ในไฟล์โค้ดแล้วกด Tab
echo ========================================================
timeout /t 5
