<?php
/**
 * Lossless Digital Vault API (Feel SafeBox)
 * Stores metadata and base64-encoded binary files safely in data/vault/
 * with atomic locked writes and automatic backup recovery to prevent
 * data corruption or loss, plus 6-digit PIN sharing sessions that
 * auto-expire and rotate every 10 minutes.
 */
header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');

if ($_SERVER['REQUEST_METHOD'] === 'OPTIONS') {
    http_response_code(200);
    exit;
}

define('VAULT_SHARE_TTL_SECONDS', 600); // 10 minutes auto-rotation TTL

$vaultDir = __DIR__ . '/data/vault';
if (!is_dir($vaultDir)) {
    @mkdir($vaultDir, 0777, true);
}
$indexFile = $vaultDir . '/vault_items.json';
$backupFile = $vaultDir . '/vault_items.backup.json';
$sharesFile = $vaultDir . '/shares.json';

function respondJson(array $data, int $statusCode = 200): void
{
    http_response_code($statusCode);
    $encoded = json_encode($data, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    if ($encoded === false) {
        http_response_code(500);
        echo '{"success":false,"message":"เกิดข้อผิดพลาดในการเข้ารหัส JSON"}';
        exit;
    }
    echo $encoded;
    exit;
}

function withFileLock(string $lockFilePath, callable $callback): mixed
{
    $fp = @fopen($lockFilePath, 'c+');
    if ($fp === false) {
        return $callback();
    }
    @flock($fp, LOCK_EX);
    try {
        return $callback();
    } finally {
        @flock($fp, LOCK_UN);
        @fclose($fp);
    }
}

function readJsonAssocStrict(string $path, array $default = []): array
{
    if (!file_exists($path)) {
        return $default;
    }
    $raw = @file_get_contents($path);
    if ($raw === false || trim($raw) === '') {
        return $default;
    }
    $decoded = json_decode($raw, true);
    if (json_last_error() !== JSON_ERROR_NONE || !is_array($decoded)) {
        return $default;
    }
    return $decoded;
}

function readVaultItemsSafe(string $indexFile, string $backupFile): array
{
    if (file_exists($indexFile)) {
        $raw = @file_get_contents($indexFile);
        if ($raw !== false && trim($raw) !== '') {
            $decoded = json_decode($raw, true);
            if (json_last_error() === JSON_ERROR_NONE && is_array($decoded)) {
                return $decoded;
            }
        }
    }
    // Recover from automatic backup if primary file is unreadable or corrupted
    return readJsonAssocStrict($backupFile, []);
}

function writeJsonAtomic(string $path, array $data): int|false
{
    $jsonStr = json_encode($data, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    if ($jsonStr === false) {
        return false;
    }
    $tmpPath = $path . '.tmp.' . getmypid() . '.' . mt_rand(1000, 9999);
    $fp = @fopen($tmpPath, 'wb');
    if ($fp === false) {
        return @file_put_contents($path, $jsonStr, LOCK_EX);
    }
    $bytes = @fwrite($fp, $jsonStr);
    @fflush($fp);
    @fclose($fp);

    if ($bytes === false || $bytes !== strlen($jsonStr)) {
        @unlink($tmpPath);
        return false;
    }
    if (!@rename($tmpPath, $path)) {
        // Fallback when target lock differs on Windows
        $ok = @file_put_contents($path, $jsonStr, LOCK_EX);
        @unlink($tmpPath);
        return $ok;
    }
    return $bytes;
}

function computeItemsDigest(array $items): string
{
    $canonical = json_encode(array_values($items), JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES);
    return $canonical !== false ? hash('sha256', $canonical) : '';
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

function loadActiveSharesLocked(string $sharesFile, int $now): array
{
    return withFileLock($sharesFile . '.lock', static function () use ($sharesFile, $now): array {
        $rawShares = readJsonAssocStrict($sharesFile, []);
        $result = pruneExpiredShares($rawShares, $now);
        if ($result['changed']) {
            writeJsonAtomic($sharesFile, $result['shares']);
        }
        return $result['shares'];
    });
}

function generateUniqueShareCode(array $shares, ?string $excludeCode = null): string
{
    for ($attempt = 0; $attempt < 64; $attempt++) {
        $candidate = str_pad((string)random_int(0, 999999), 6, '0', STR_PAD_LEFT);
        if ($candidate !== $excludeCode && !isset($shares[$candidate])) {
            return $candidate;
        }
    }
    do {
        $candidate = str_pad((string)random_int(0, 999999), 6, '0', STR_PAD_LEFT);
    } while ($candidate === $excludeCode || isset($shares[$candidate]));
    return $candidate;
}

$now = time();

if ($_SERVER['REQUEST_METHOD'] === 'GET') {
    $action = (string)($_GET['action'] ?? 'list');

    if ($action === 'server_info') {
        $activeShares = loadActiveSharesLocked($sharesFile, $now);
        respondJson([
            'success' => true,
            'serverTime' => $now,
            'ttlSeconds' => VAULT_SHARE_TTL_SECONDS,
            'activeShareCount' => count($activeShares),
        ]);
    }

    if ($action === 'resolve_share') {
        $code = normalizeShareCode((string)($_GET['code'] ?? ''));
        if (!isValidShareCode($code)) {
            respondJson([
                'success' => false,
                'error' => 'INVALID_CODE',
                'message' => 'กรุณาระบุรหัส PIN ตัวเลข 6 หลักให้ถูกต้อง',
            ], 400);
        }

        $activeShares = loadActiveSharesLocked($sharesFile, $now);
        if (!isset($activeShares[$code])) {
            respondJson([
                'success' => false,
                'error' => 'EXPIRED_OR_NOT_FOUND',
                'message' => 'PIN 6 หลักนี้หมดอายุแล้ว (รีเซ็ตทุก 10 นาที) หรือยังไม่ได้เปิดแชร์',
            ], 404);
        }

        $entry = $activeShares[$code];
        $expiresAt = (int)$entry['expiresAt'];
        $items = isset($entry['items']) && is_array($entry['items']) ? array_values($entry['items']) : [];
        $payloadSha256 = (string)($entry['payloadSha256'] ?? computeItemsDigest($items));

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
            'payloadSha256' => $payloadSha256,
            'items' => $items,
        ]);
    }

    // Default: return vault items list (auto-recovering from backup if needed)
    $items = withFileLock($indexFile . '.lock', static fn(): array => readVaultItemsSafe($indexFile, $backupFile));
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

    // -------------------- 6-DIGIT PIN LIVE SHARE ACTIONS (10-MIN AUTO ROTATION) --------------------
    if ($action === 'create_share') {
        $items = (isset($payload['items']) && is_array($payload['items']))
            ? array_values($payload['items'])
            : withFileLock($indexFile . '.lock', static fn(): array => readVaultItemsSafe($indexFile, $backupFile));

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
        $payloadSha256 = computeItemsDigest($items);

        $created = withFileLock($sharesFile . '.lock', static function () use (
            $sharesFile, $now, $previousCode, $scope, $label, $items, $payloadSha256
        ): array {
            $rawShares = readJsonAssocStrict($sharesFile, []);
            $activeShares = pruneExpiredShares($rawShares, $now)['shares'];
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
                'payloadSha256' => $payloadSha256,
                'items' => $items,
            ];

            $written = writeJsonAtomic($sharesFile, $activeShares);
            return ['ok' => $written !== false, 'code' => $newCode, 'expiresAt' => $expiresAt];
        });

        if (!$created['ok']) {
            respondJson([
                'success' => false,
                'message' => 'ไม่สามารถบันทึก PIN แชร์ลงเซิร์ฟเวอร์ได้',
            ], 500);
        }

        respondJson([
            'success' => true,
            'code' => $created['code'],
            'createdAt' => $now,
            'expiresAt' => $created['expiresAt'],
            'remainingSeconds' => VAULT_SHARE_TTL_SECONDS,
            'ttlSeconds' => VAULT_SHARE_TTL_SECONDS,
            'scope' => $scope,
            'label' => $label,
            'itemCount' => count($items),
            'payloadSha256' => $payloadSha256,
        ]);
    }

    if ($action === 'update_share') {
        $code = normalizeShareCode((string)($payload['code'] ?? ''));
        if (!isValidShareCode($code)) {
            respondJson(['success' => false, 'message' => 'PIN 6 หลักไม่ถูกต้อง'], 400);
        }

        $items = (isset($payload['items']) && is_array($payload['items'])) ? array_values($payload['items']) : [];

        $result = withFileLock($sharesFile . '.lock', static function () use (
            $sharesFile, $now, $code, $items, $payload
        ): array {
            $activeShares = pruneExpiredShares(readJsonAssocStrict($sharesFile, []), $now)['shares'];
            if (!isset($activeShares[$code])) {
                return ['expired' => true];
            }
            if (count($items) === 0) {
                unset($activeShares[$code]);
                writeJsonAtomic($sharesFile, $activeShares);
                return ['revoked' => true];
            }

            $activeShares[$code]['items'] = $items;
            $activeShares[$code]['itemCount'] = count($items);
            $activeShares[$code]['payloadSha256'] = computeItemsDigest($items);
            if (isset($payload['scope']) && in_array($payload['scope'], ['all', 'single'], true)) {
                $activeShares[$code]['scope'] = (string)$payload['scope'];
            }
            if (isset($payload['label'])) {
                $activeShares[$code]['label'] = trim((string)$payload['label']);
            }
            writeJsonAtomic($sharesFile, $activeShares);
            return ['expiresAt' => (int)$activeShares[$code]['expiresAt']];
        });

        if (!empty($result['expired'])) {
            respondJson([
                'success' => false,
                'expired' => true,
                'message' => 'PIN 6 หลักหมดอายุแล้ว',
            ], 404);
        }
        if (!empty($result['revoked'])) {
            respondJson(['success' => true, 'revoked' => true, 'code' => $code, 'itemCount' => 0]);
        }

        respondJson([
            'success' => true,
            'code' => $code,
            'expiresAt' => $result['expiresAt'],
            'remainingSeconds' => max(0, $result['expiresAt'] - $now),
            'itemCount' => count($items),
        ]);
    }

    if ($action === 'revoke_share') {
        $code = normalizeShareCode((string)($payload['code'] ?? ''));
        withFileLock($sharesFile . '.lock', static function () use ($sharesFile, $now, $code): void {
            $activeShares = pruneExpiredShares(readJsonAssocStrict($sharesFile, []), $now)['shares'];
            if (isValidShareCode($code) && isset($activeShares[$code])) {
                unset($activeShares[$code]);
                writeJsonAtomic($sharesFile, $activeShares);
            }
        });
        respondJson([
            'success' => true,
            'revoked' => true,
            'code' => $code,
        ]);
    }

    // -------------------- VAULT ITEM STORAGE ACTIONS (ATOMIC LOCK + BACKUP) --------------------
    $storageResult = withFileLock($indexFile . '.lock', static function () use (
        $indexFile, $backupFile, $action, $payload
    ): array {
        $items = readVaultItemsSafe($indexFile, $backupFile);

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
            return ['invalid' => true];
        }

        // Create backup only from valid existing index
        if (file_exists($indexFile) && !empty(readJsonAssocStrict($indexFile, []))) {
            @copy($indexFile, $backupFile);
        }

        $written = writeJsonAtomic($indexFile, $items);
        return [
            'ok' => $written !== false,
            'count' => count($items),
            'bytes' => $written ?: 0,
        ];
    });

    if (!empty($storageResult['invalid'])) {
        respondJson(['success' => false, 'message' => 'คำสั่งไม่ถูกต้อง'], 400);
    }

    respondJson([
        'success' => $storageResult['ok'],
        'count' => $storageResult['count'],
        'bytes' => $storageResult['bytes'],
    ]);
}

respondJson(['success' => false, 'message' => 'Method not allowed'], 405);
