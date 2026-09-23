<?php
/**
 * Auto-Sync Photos from Google Drive Shared Folder
 * URL: http://localhost/feel/sync_drive.php
 */
header('Content-Type: application/json; charset=utf-8');

$defaultFolderUrl = 'https://drive.google.com/drive/folders/13JPid47x7QZvmnHwQI0uZ5hVPatEi3Ow?usp=sharing';
$folderUrl = isset($_REQUEST['folder_url']) && !empty($_REQUEST['folder_url']) ? $_REQUEST['folder_url'] : $defaultFolderUrl;

$photosDir = __DIR__ . DIRECTORY_SEPARATOR . 'photos';
if (!is_dir($photosDir)) {
    mkdir($photosDir, 0777, true);
}

// 1. Fetch Google Drive folder page
$options = [
    'http' => [
        'method' => 'GET',
        'header' => "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36\r\n",
        'timeout' => 20,
        'follow_location' => 1
    ]
];
$context = stream_context_create($options);
$html = @file_get_contents($folderUrl, false, $context);

if (!$html) {
    echo json_encode([
        'success' => false,
        'message' => 'ไม่สามารถเชื่อมต่อไปยัง Google Drive ได้ กรุณาตรวจสอบการเชื่อมต่ออินเทอร์เน็ต'
    ], JSON_UNESCAPED_UNICODE);
    exit;
}

// 2. Extract image filenames and IDs
// Pattern matches: aria-label="filename.jpg Image Shared"... ssk='5:...:FILE_ID-...'
$pattern = '/aria-label="([^"]+) Image Shared"[^>]*?ssk=\'5:[^:\']*?:([a-zA-Z0-9_-]{25,45})-\d+-\d+\'/';
preg_match_all($pattern, $html, $matches, PREG_SET_ORDER);

$extracted = [];
$seen = [];
foreach ($matches as $m) {
    $name = trim($m[1]);
    $id = trim($m[2]);
    if (!isset($seen[$id])) {
        $seen[$id] = true;
        $extracted[] = [
            'name' => $name,
            'id' => $id,
            'url' => 'https://lh3.googleusercontent.com/d/' . $id . '=w1200'
        ];
    }
}

$newDownloaded = 0;
$errors = 0;

// 3. Download newly discovered photos
foreach ($extracted as $item) {
    $safeName = preg_replace('/[^a-zA-Z0-9._-]/', '_', $item['name']);
    $dest = $photosDir . DIRECTORY_SEPARATOR . $safeName;
    
    // Download if not already present or file is empty
    if (!file_exists($dest) || filesize($dest) < 1000) {
        $imgContext = stream_context_create([
            'http' => [
                'method' => 'GET',
                'header' => "User-Agent: Mozilla/5.0\r\n",
                'timeout' => 15,
                'follow_location' => 1
            ]
        ]);
        $imgData = @file_get_contents($item['url'], false, $imgContext);
        if ($imgData && strlen($imgData) > 1000) {
            file_put_contents($dest, $imgData);
            $newDownloaded++;
        } else {
            $errors++;
        }
    }
}

// 4. Rebuild manifest of all local photos
$manifest = [];
$files = scandir($photosDir);
foreach ($files as $f) {
    if (preg_match('/\.(jpe?g|png)$/i', $f)) {
        $manifest[] = [
            'filename' => $f,
            'path' => './photos/' . $f,
            'size' => filesize($photosDir . DIRECTORY_SEPARATOR . $f)
        ];
    }
}

// Sort alphabetically or newest
usort($manifest, function($a, $b) {
    return strcmp($b['filename'], $a['filename']);
});

file_put_contents(__DIR__ . DIRECTORY_SEPARATOR . 'photos_manifest.json', json_encode($manifest, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE));

echo json_encode([
    'success' => true,
    'message' => 'ซิงค์ข้อมูลจาก Google Drive สำเร็จ',
    'total_online_found' => count($extracted),
    'new_downloaded' => $newDownloaded,
    'total_local_photos' => count($manifest),
    'manifest_file' => './photos_manifest.json'
], JSON_UNESCAPED_UNICODE);
