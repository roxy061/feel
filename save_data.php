<?php
/**
 * Backend Data Saver for Antigravity Portfolio CMS
 * Saves updated site configuration and data to data/portfolio_data.json
 */
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    echo json_encode(['success' => false, 'message' => 'Method Not Allowed']);
    exit;
}

// Read raw POST body
$input = file_get_contents('php://input');
if (empty($input)) {
    http_response_code(400);
    echo json_encode(['success' => false, 'message' => 'ไม่พบข้อมูลที่ส่งมา']);
    exit;
}

// Decode JSON
$data = json_decode($input, true);
if ($data === null && json_last_error() !== JSON_ERROR_NONE) {
    http_response_code(400);
    echo json_encode(['success' => false, 'message' => 'รูปแบบข้อมูล JSON ไม่ถูกต้อง']);
    exit;
}

// Target file path
$dataDir = __DIR__ . '/data';
if (!is_dir($dataDir)) {
    mkdir($dataDir, 0777, true);
}

$dataFile = $dataDir . '/portfolio_data.json';

// Backup existing data if exists
if (file_exists($dataFile)) {
    $backupFile = $dataDir . '/portfolio_data.backup.json';
    @copy($dataFile, $backupFile);
}

// Write file with exclusive lock
$jsonString = json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
$result = file_put_contents($dataFile, $jsonString, LOCK_EX);

if ($result === false) {
    http_response_code(500);
    echo json_encode(['success' => false, 'message' => 'ไม่สามารถบันทึกไฟล์ข้อมูลลงเซิร์ฟเวอร์ได้']);
    exit;
}

echo json_encode([
    'success' => true,
    'message' => 'บันทึกข้อมูลเว็บไซต์ลงฐานข้อมูลเซิร์ฟเวอร์เรียบร้อยแล้ว',
    'timestamp' => time(),
    'bytes' => $result
]);
