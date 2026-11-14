<?php
/**
 * Lossless Digital Vault API (Feel SafeBox)
 * Stores metadata and base64-encoded binary files safely in data/vault/
 * with automatic backup to prevent any data corruption.
 */
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

$vaultDir = __DIR__ . '/data/vault';
if (!is_dir($vaultDir)) {
    @mkdir($vaultDir, 0777, true);
}
$indexFile = $vaultDir . '/vault_items.json';

if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    if (!file_exists($indexFile)) {
        echo json_encode(['success' => true, 'items' => []]);
        exit;
    }
    $raw = @file_get_contents($indexFile);
    $items = json_decode($raw, true);
    echo json_encode([
        'success' => true,
        'items' => is_array($items) ? $items : []
    ]);
    exit;
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $input = file_get_contents('php://input');
    $payload = json_decode($input, true);
    if (!is_array($payload) || !isset($payload['action'])) {
        http_response_code(400);
        echo json_encode(['success' => false, 'message' => 'คำสั่งไม่ถูกต้อง']);
        exit;
    }

    $items = [];
    if (file_exists($indexFile)) {
        $decoded = json_decode(@file_get_contents($indexFile), true);
        if (is_array($decoded)) $items = $decoded;
    }

    $action = $payload['action'];

    if ($action === 'save_item' && isset($payload['item'])) {
        $newItem = $payload['item'];
        // Remove existing with same ID if updating
        $items = array_values(array_filter($items, function($it) use ($newItem) {
            return isset($it['id']) && $it['id'] !== $newItem['id'];
        }));
        array_unshift($items, $newItem);
    } elseif ($action === 'delete_item' && isset($payload['id'])) {
        $delId = $payload['id'];
        $items = array_values(array_filter($items, function($it) use ($delId) {
            return isset($it['id']) && $it['id'] !== $delId;
        }));
    } elseif ($action === 'sync_all' && isset($payload['items']) && is_array($payload['items'])) {
        $items = $payload['items'];
    }

    // Create backup before writing
    if (file_exists($indexFile)) {
        @copy($indexFile, $vaultDir . '/vault_items.backup.json');
    }

    $jsonStr = json_encode($items, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    $written = @file_put_contents($indexFile, $jsonStr, LOCK_EX);

    echo json_encode([
        'success' => $written !== false,
        'count' => count($items),
        'bytes' => $written ?: 0
    ]);
    exit;
}
