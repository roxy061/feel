<?php
/**
 * Lossless Digital Vault API (Feel SafeBox)
 * Stores metadata and base64-encoded binary files safely in data/vault/
 * with automatic backup to prevent any data corruption, plus temporary
 * 6-digit IP/PIN sharing sessions that auto-expire and rotate every 10 minutes.
 */
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(204);
    exit;
}

define('VAULT_SHARE_TTL_SECONDS', 600); // 10 minutes auto-rotation TTL

$vaultDir = __DIR__ . '/data/vault';
if (!is_dir($vaultDir)) {
    @mkdir($vaultDir, 0777, true);
}
$indexFile = $vaultDir . '/vault_items.json';
$sharesFile = $vaultDir . '/shares.json';

function respondJson(array $data, int $statusCode = 200): void
{
    http_response_code($statusCode);
    echo json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    exit;
}

function readJsonAssoc(string $path, array $default = []): array
{
    if (!file_exists($path)) {
        return $default;
    }
    $raw = @file_get_contents($path);
    if ($raw === false || trim($raw) === '') {
        return $default;
    }
    $decoded = json_decode($raw, true);
    return is_array($decoded) ? $decoded : $default;
}

function writeJsonAssoc(string $path, array $data): int|false
{
    $jsonStr = json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    if ($jsonStr === false) {
        return false;
    }
    return @file_put_contents($path, $jsonStr, LOCK_EX);
}

function normalizeShareCode(string $raw): string
{
    return preg_replace('/\D+/', '', $raw) ?? '';
}

function isValidShareCode(string $code): bool
{
    return preg_match('/^\d{6}$/', $code) === 1;
}

function pruneExpiredShares(array $shares, int $now): array
{
    $active = [];
    $changed = false;
    foreach ($shares as $code => $entry) {
        $codeKey = (string)$code;
        if (
            !isValidShareCode($codeKey) ||
            !is_array($entry) ||
            !isset($entry['expiresAt']) ||
            (int)$entry['expiresAt'] <= $now
        ) {
            $changed = true;
            continue;
        }
        $active[$codeKey] = $entry;
    }
    return ['shares' => $active, 'changed' => $changed];
}

function loadActiveShares(string $sharesFile, int $now): array
{
    $rawShares = readJsonAssoc($sharesFile, []);
    $result = pruneExpiredShares($rawShares, $now);
    if ($result['changed']) {
        writeJsonAssoc($sharesFile, $result['shares']);
    }
    return $result['shares'];
}

function generateUniqueShareCode(array $shares, ?string $excludeCode = null): string
{
    for ($attempt = 0; $attempt < 64; $attempt++) {
        $candidate = str_pad((string)random_int(0, 999999), 6, '0', STR_PAD_LEFT);
        if ($candidate !== $excludeCode && !isset($shares[$candidate])) {
            return $candidate;
        }
    }
    // Deterministic fallback in extreme collision case
    do {
        $candidate = str_pad((string)random_int(0, 999999), 6, '0', STR_PAD_LEFT);
    } while ($candidate === $excludeCode || isset($shares[$candidate]));
    return $candidate;
}

function scoreLanIpv4(string $ip): int
{
    if (str_starts_with($ip, '192.168.')) return 3;
    if (str_starts_with($ip, '10.')) return 2;
    if (preg_match('/^172\.(1[6-9]|2\d|3[0-1])\./', $ip) === 1) return 1;
    return 0;
}

function detectServerLanInfo(): array
{
    $isHttps = (!empty($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off')
        || (isset($_SERVER['SERVER_PORT']) && (string)$_SERVER['SERVER_PORT'] === '443');
    $scheme = $isHttps ? 'https' : 'http';
    $httpHost = (string)($_SERVER['HTTP_HOST'] ?? 'localhost');

    $candidates = [];
    $serverAddr = (string)($_SERVER['SERVER_ADDR'] ?? '');
    if ($serverAddr !== '') {
        $candidates[] = $serverAddr;
    }
    $hostName = @gethostname();
    if (is_string($hostName) && $hostName !== '') {
        $resolved = @gethostbynamel($hostName);
        if (is_array($resolved)) {
            $candidates = array_merge($candidates, $resolved);
        }
    }

    $lanIps = [];
    foreach ($candidates as $ip) {
        if (
            filter_var($ip, FILTER_VALIDATE_IP, FILTER_FLAG_IPV4) &&
            !str_starts_with($ip, '127.') &&
            !str_starts_with($ip, '169.254.') &&
            !in_array($ip, $lanIps, true)
        ) {
            $lanIps[] = $ip;
        }
    }

    usort($lanIps, static fn(string $a, string $b): int => scoreLanIpv4($b) <=> scoreLanIpv4($a));
    $bestLanIp = $lanIps[0] ?? '';

    $scriptDir = rtrim(str_replace('\\', '/', dirname((string)($_SERVER['SCRIPT_NAME'] ?? '/vault_api.php'))), '/');
    $hostPort = '';
    if (str_contains($httpHost, ':')) {
        $parts = explode(':', $httpHost, 2);
        if (!in_array($parts[1], ['80', '443'], true) && ctype_digit($parts[1])) {
            $hostPort = ':' . $parts[1];
        }
    }

    $currentBaseUrl = "{$scheme}://{$httpHost}{$scriptDir}/vault.html";
    $lanBaseUrl = $bestLanIp !== ''
        ? "{$scheme}://{$bestLanIp}{$hostPort}{$scriptDir}/vault.html"
        : $currentBaseUrl;

    return [
        'scheme' => $scheme,
        'httpHost' => $httpHost,
        'lanIp' => $bestLanIp,
        'allIps' => $lanIps,
        'currentBaseUrl' => $currentBaseUrl,
        'lanBaseUrl' => $lanBaseUrl,
    ];
}

$now = time();

if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    $action = (string)($_GET['action'] ?? 'list');

    if ($action === 'server_info') {
        $activeShares = loadActiveShares($sharesFile, $now);
        respondJson([
            'success' => true,
            'serverTime' => $now,
            'ttlSeconds' => VAULT_SHARE_TTL_SECONDS,
            'activeShareCount' => count($activeShares),
            'network' => detectServerLanInfo(),
        ]);
    }

    if ($action === 'resolve_share') {
        $code = normalizeShareCode((string)($_GET['code'] ?? ''));
        if (!isValidShareCode($code)) {
            respondJson([
                'success' => false,
                'error' => 'INVALID_CODE',
                'message' => 'กรุณาระบุรหัสแชร์ตัวเลข 6 หลักให้ถูกต้อง',
            ], 400);
        }

        $activeShares = loadActiveShares($sharesFile, $now);
        if (!isset($activeShares[$code])) {
            respondJson([
                'success' => false,
                'error' => 'EXPIRED_OR_NOT_FOUND',
                'message' => 'รหัส 6 หลักนี้หมดอายุแล้ว (รีเซ็ตทุก 10 นาที) หรือยังไม่ได้เปิดแชร์',
            ], 404);
        }

        $entry = $activeShares[$code];
        $expiresAt = (int)$entry['expiresAt'];
        $items = isset($entry['items']) && is_array($entry['items']) ? $entry['items'] : [];

        respondJson([
            'success' => true,
            'code' => $code,
            'createdAt' => (int)($entry['createdAt'] ?? $now),
            'expiresAt' => $expiresAt,
            'remainingSeconds' => max(0, $expiresAt - $now),
            'ttlSeconds' => VAULT_SHARE_TTL_SECONDS,
            'scope' => (string)($entry['scope'] ?? 'all'),
            'label' => (string)($entry['label'] ?? ''),
            'itemCount' => count($items),
            'items' => $items,
            'network' => detectServerLanInfo(),
        ]);
    }

    // Default: return vault items list
    $items = readJsonAssoc($indexFile, []);
    respondJson([
        'success' => true,
        'items' => $items,
    ]);
}

if ($_SERVER['REQUEST_METHOD'] === 'POST') {
    $input = file_get_contents('php://input');
    $payload = json_decode((string)$input, true);
    if (!is_array($payload) || !isset($payload['action'])) {
        respondJson([
            'success' => false,
            'message' => 'คำสั่งไม่ถูกต้องหรือขนาดข้อมูลเกินขีดจำกัดของเซิร์ฟเวอร์',
        ], 400);
    }

    $action = (string)$payload['action'];

    // -------------------- 6-DIGIT LIVE SHARE ACTIONS (10-MIN AUTO ROTATION) --------------------
    if ($action === 'create_share') {
        $items = (isset($payload['items']) && is_array($payload['items']))
            ? array_values($payload['items'])
            : readJsonAssoc($indexFile, []);

        if (count($items) === 0) {
            respondJson([
                'success' => false,
                'error' => 'EMPTY_VAULT',
                'message' => 'ไม่มีข้อมูลในตู้เซฟสำหรับแชร์ กรุณาฝากไฟล์ รูปภาพ ลิงก์ หรือข้อความก่อน',
            ], 400);
        }

        $previousCode = normalizeShareCode((string)($payload['previousCode'] ?? ''));
        $scope = in_array(($payload['scope'] ?? 'all'), ['all', 'single'], true)
            ? (string)$payload['scope']
            : 'all';
        $label = trim((string)($payload['label'] ?? ''));

        $activeShares = loadActiveShares($sharesFile, $now);
        if (isValidShareCode($previousCode) && isset($activeShares[$previousCode])) {
            unset($activeShares[$previousCode]);
        }

        $newCode = generateUniqueShareCode($activeShares, isValidShareCode($previousCode) ? $previousCode : null);
        $expiresAt = $now + VAULT_SHARE_TTL_SECONDS;

        $activeShares[$newCode] = [
            'code' => $newCode,
            'createdAt' => $now,
            'expiresAt' => $expiresAt,
            'ttlSeconds' => VAULT_SHARE_TTL_SECONDS,
            'scope' => $scope,
            'label' => $label,
            'itemCount' => count($items),
            'items' => $items,
            'createdIp' => (string)($_SERVER['REMOTE_ADDR'] ?? ''),
        ];

        if (writeJsonAssoc($sharesFile, $activeShares) === false) {
            respondJson([
                'success' => false,
                'message' => 'ไม่สามารถบันทึกรหัสแชร์ลงเซิร์ฟเวอร์ได้',
            ], 500);
        }

        respondJson([
            'success' => true,
            'code' => $newCode,
            'createdAt' => $now,
            'expiresAt' => $expiresAt,
            'remainingSeconds' => VAULT_SHARE_TTL_SECONDS,
            'ttlSeconds' => VAULT_SHARE_TTL_SECONDS,
            'scope' => $scope,
            'label' => $label,
            'itemCount' => count($items),
            'network' => detectServerLanInfo(),
        ]);
    }

    if ($action === 'update_share') {
        $code = normalizeShareCode((string)($payload['code'] ?? ''));
        if (!isValidShareCode($code)) {
            respondJson(['success' => false, 'message' => 'รหัสแชร์ 6 หลักไม่ถูกต้อง'], 400);
        }

        $activeShares = loadActiveShares($sharesFile, $now);
        if (!isset($activeShares[$code])) {
            respondJson([
                'success' => false,
                'expired' => true,
                'message' => 'รหัสแชร์ 6 หลักหมดอายุแล้ว',
            ], 404);
        }

        $items = (isset($payload['items']) && is_array($payload['items'])) ? array_values($payload['items']) : [];
        if (count($items) === 0) {
            unset($activeShares[$code]);
            writeJsonAssoc($sharesFile, $activeShares);
            respondJson(['success' => true, 'revoked' => true, 'code' => $code, 'itemCount' => 0]);
        }

        $activeShares[$code]['items'] = $items;
        $activeShares[$code]['itemCount'] = count($items);
        if (isset($payload['scope']) && in_array($payload['scope'], ['all', 'single'], true)) {
            $activeShares[$code]['scope'] = (string)$payload['scope'];
        }
        if (isset($payload['label'])) {
            $activeShares[$code]['label'] = trim((string)$payload['label']);
        }

        writeJsonAssoc($sharesFile, $activeShares);
        $expiresAt = (int)$activeShares[$code]['expiresAt'];

        respondJson([
            'success' => true,
            'code' => $code,
            'expiresAt' => $expiresAt,
            'remainingSeconds' => max(0, $expiresAt - $now),
            'itemCount' => count($items),
        ]);
    }

    if ($action === 'revoke_share') {
        $code = normalizeShareCode((string)($payload['code'] ?? ''));
        $activeShares = loadActiveShares($sharesFile, $now);
        if (isValidShareCode($code) && isset($activeShares[$code])) {
            unset($activeShares[$code]);
            writeJsonAssoc($sharesFile, $activeShares);
        }
        respondJson([
            'success' => true,
            'revoked' => true,
            'code' => $code,
        ]);
    }

    // -------------------- VAULT ITEM STORAGE ACTIONS --------------------
    $items = readJsonAssoc($indexFile, []);

    if ($action === 'save_item' && isset($payload['item']) && is_array($payload['item'])) {
        $newItem = $payload['item'];
        $items = array_values(array_filter($items, static function ($it) use ($newItem) {
            return isset($it['id'], $newItem['id']) && $it['id'] !== $newItem['id'];
        }));
        array_unshift($items, $newItem);
    } elseif ($action === 'delete_item' && isset($payload['id'])) {
        $delId = $payload['id'];
        $items = array_values(array_filter($items, static function ($it) use ($delId) {
            return isset($it['id']) && $it['id'] !== $delId;
        }));
    } elseif ($action === 'sync_all' && isset($payload['items']) && is_array($payload['items'])) {
        $items = $payload['items'];
    } else {
        respondJson(['success' => false, 'message' => 'คำสั่งไม่ถูกต้อง'], 400);
    }

    // Create backup before writing
    if (file_exists($indexFile)) {
        @copy($indexFile, $vaultDir . '/vault_items.backup.json');
    }

    $written = writeJsonAssoc($indexFile, $items);

    respondJson([
        'success' => $written !== false,
        'count' => count($items),
        'bytes' => $written ?: 0,
    ]);
}

respondJson(['success' => false, 'message' => 'Method not allowed'], 405);
