<?php
/**
 * Local XAMPP / PHP Fallback Proxy for Web Source Cloner (App 21)
 * Mirrors the /api/proxy.js JSON contract with strict SSRF validation,
 * manual redirect checks, 5 MB size cap, and nosniff headers.
 */
declare(strict_types=1);

header('Content-Type: application/json; charset=utf-8');
header('Access-Control-Allow-Origin: *');
header('Access-Control-Allow-Methods: GET, POST, OPTIONS');
header('Access-Control-Allow-Headers: Content-Type');
header('X-Content-Type-Options: nosniff');
header('Cache-Control: no-store, no-cache, must-revalidate, max-age=0');

const MAX_BYTES = 5 * 1024 * 1024;
const FETCH_TIMEOUT_SEC = 10;
const MAX_REDIRECTS = 5;

if (($_SERVER['REQUEST_METHOD'] ?? 'GET') === 'OPTIONS') {
    http_response_code(200);
    exit;
}

function respondProxyJson(int $statusCode, array $payload): void
{
    http_response_code($statusCode);
    echo json_encode($payload, JSON_UNESCAPED_UNICODE | JSON_UNESCAPED_SLASHES | JSON_INVALID_UTF8_SUBSTITUTE);
    exit;
}

function isBlockedHostname(string $host): bool
{
    $h = strtolower(rtrim(trim($host, "[]"), '.'));
    if ($h === '') {
        return true;
    }
    if (
        $h === 'localhost' ||
        str_ends_with($h, '.localhost') ||
        str_ends_with($h, '.local') ||
        str_ends_with($h, '.internal') ||
        str_ends_with($h, '.localdomain') ||
        str_ends_with($h, '.arpa') ||
        $h === 'metadata.google.internal'
    ) {
        return true;
    }
    return false;
}

function isPublicIp(string $ip): bool
{
    $clean = preg_replace('/%.+$/', '', trim($ip, '[]'));
    if (filter_var($clean, FILTER_VALIDATE_IP, FILTER_FLAG_IPV4 | FILTER_FLAG_NO_PRIV_RANGE | FILTER_FLAG_NO_RES_RANGE) !== false) {
        $parts = array_map('intval', explode('.', $clean));
        // Also block CGNAT 100.64.0.0/10 and multicast/broadcast >= 224
        if ($parts[0] === 100 && $parts[1] >= 64 && $parts[1] <= 127) {
            return false;
        }
        if ($parts[0] >= 224 || $parts[0] === 0) {
            return false;
        }
        return true;
    }
    if (filter_var($clean, FILTER_VALIDATE_IP, FILTER_FLAG_IPV6 | FILTER_FLAG_NO_PRIV_RANGE | FILTER_FLAG_NO_RES_RANGE) !== false) {
        $lower = strtolower($clean);
        if (str_starts_with($lower, 'fe80:') || str_starts_with($lower, 'fc') || str_starts_with($lower, 'fd') || str_starts_with($lower, 'ff') || str_starts_with($lower, '::ffff:')) {
            return false;
        }
        return true;
    }
    return false;
}

function assertPublicTarget(string $rawUrl): string
{
    $url = trim($rawUrl);
    if ($url === '' || strlen($url) > 2048) {
        respondProxyJson(400, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Invalid target URL']);
    }
    $parts = @parse_url($url);
    if (!is_array($parts) || empty($parts['scheme']) || empty($parts['host'])) {
        respondProxyJson(400, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Malformed URL']);
    }
    $scheme = strtolower((string)$parts['scheme']);
    if ($scheme !== 'http' && $scheme !== 'https') {
        respondProxyJson(400, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Only http:// and https:// protocols are permitted']);
    }
    if (isset($parts['user']) || isset($parts['pass'])) {
        respondProxyJson(400, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Credentials inside URL are not allowed']);
    }
    $host = trim((string)$parts['host'], '[]');
    if (isBlockedHostname($host)) {
        respondProxyJson(403, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Access to internal or localhost hostnames is blocked']);
    }
    if (filter_var($host, FILTER_VALIDATE_IP) !== false) {
        if (!isPublicIp($host)) {
            respondProxyJson(403, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Access to private or reserved IP ranges is blocked']);
        }
        return $url;
    }

    $ips = [];
    if (function_exists('dns_get_record')) {
        $recs = @dns_get_record($host, DNS_A | DNS_AAAA);
        if (is_array($recs)) {
            foreach ($recs as $r) {
                if (!empty($r['ip'])) {
                    $ips[] = $r['ip'];
                }
                if (!empty($r['ipv6'])) {
                    $ips[] = $r['ipv6'];
                }
            }
        }
    }
    if (empty($ips)) {
        $v4s = @gethostbynamel($host);
        if (is_array($v4s)) {
            $ips = $v4s;
        }
    }
    if (empty($ips)) {
        respondProxyJson(502, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Unable to resolve hostname: ' . $host]);
    }
    foreach ($ips as $ip) {
        if (!isPublicIp((string)$ip)) {
            respondProxyJson(403, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Resolved target points to a private or reserved network']);
        }
    }
    return $url;
}

function resolveRelativeRedirect(string $baseUrl, string $location): string
{
    if (preg_match('/^https?:\/\//i', $location)) {
        return $location;
    }
    $p = parse_url($baseUrl);
    $scheme = $p['scheme'] ?? 'https';
    $host = $p['host'] ?? '';
    $port = isset($p['port']) ? ':' . $p['port'] : '';
    if (str_starts_with($location, '//')) {
        return $scheme . ':' . $location;
    }
    if (str_starts_with($location, '/')) {
        return $scheme . '://' . $host . $port . $location;
    }
    $basePath = $p['path'] ?? '/';
    $dir = preg_replace('#/[^/]*$#', '/', $basePath);
    return $scheme . '://' . $host . $port . $dir . $location;
}

function isTextMime(string $contentType, string $url): bool
{
    $ct = strtolower($contentType);
    if (
        str_starts_with($ct, 'text/') ||
        str_contains($ct, 'javascript') ||
        str_contains($ct, 'ecmascript') ||
        str_contains($ct, 'json') ||
        str_contains($ct, 'xml') ||
        str_contains($ct, 'image/svg+xml')
    ) {
        return true;
    }
    if ($ct === '') {
        $path = strtolower((string)parse_url($url, PHP_URL_PATH));
        return (bool)preg_match('/\.(html?|css|m?js|json|svg|xml|txt|md)$/', $path);
    }
    return false;
}

$method = $_SERVER['REQUEST_METHOD'] ?? 'GET';
if ($method !== 'GET' && $method !== 'POST') {
    respondProxyJson(405, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Method not allowed']);
}

$rawUrl = $_GET['url'] ?? '';
if ($rawUrl === '' && $method === 'POST') {
    $rawBody = file_get_contents('php://input');
    if (is_string($rawBody) && $rawBody !== '') {
        $decoded = json_decode($rawBody, true);
        if (is_array($decoded) && isset($decoded['url'])) {
            $rawUrl = (string)$decoded['url'];
        }
    }
}

$currentUrl = assertPublicTarget((string)$rawUrl);

if (!function_exists('curl_init')) {
    respondProxyJson(500, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'PHP cURL extension is unavailable']);
}

$status = 0;
$contentType = 'application/octet-stream';
$body = '';

for ($hop = 0; $hop <= MAX_REDIRECTS; $hop++) {
    $currentUrl = assertPublicTarget($currentUrl);
    $respHeaders = [];
    $buffer = '';
    $abortedSize = false;

    $ch = curl_init($currentUrl);
    curl_setopt_array($ch, [
        CURLOPT_RETURNTRANSFER => false,
        CURLOPT_FOLLOWLOCATION => false,
        CURLOPT_TIMEOUT => FETCH_TIMEOUT_SEC,
        CURLOPT_CONNECTTIMEOUT => 5,
        CURLOPT_ENCODING => '',
        CURLOPT_USERAGENT => 'Mozilla/5.0 (compatible; FeelWebCloner/1.0; +https://feel-ochre.vercel.app)',
        CURLOPT_HTTPHEADER => [
            'Accept: text/html,application/xhtml+xml,text/css,application/javascript,image/*,*/*;q=0.8',
            'Accept-Language: th,en-US;q=0.9,en;q=0.8'
        ],
        CURLOPT_HEADERFUNCTION => function ($curl, $headerLine) use (&$respHeaders) {
            $len = strlen($headerLine);
            $parts = explode(':', $headerLine, 2);
            if (count($parts) === 2) {
                $respHeaders[strtolower(trim($parts[0]))] = trim($parts[1]);
            }
            return $len;
        },
        CURLOPT_WRITEFUNCTION => function ($curl, $chunk) use (&$buffer, &$abortedSize) {
            $nextLen = strlen($buffer) + strlen($chunk);
            if ($nextLen > MAX_BYTES) {
                $abortedSize = true;
                return 0;
            }
            $buffer .= $chunk;
            return strlen($chunk);
        }
    ]);

    $execOk = curl_exec($ch);
    $curlErr = curl_error($ch);
    $status = (int)curl_getinfo($ch, CURLINFO_HTTP_CODE);
    $ctInfo = (string)curl_getinfo($ch, CURLINFO_CONTENT_TYPE);
    curl_close($ch);

    if ($abortedSize) {
        respondProxyJson(413, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Upstream response exceeded 5 MB size cap']);
    }
    if ($execOk === false && $status === 0) {
        respondProxyJson(502, ['ok' => false, 'engine' => 'php-proxy', 'error' => $curlErr ?: 'Upstream request failed']);
    }

    $contentType = $ctInfo !== '' ? $ctInfo : ($respHeaders['content-type'] ?? 'application/octet-stream');
    if ($status >= 300 && $status < 400 && !empty($respHeaders['location'])) {
        if ($hop === MAX_REDIRECTS) {
            respondProxyJson(502, ['ok' => false, 'engine' => 'php-proxy', 'error' => 'Too many HTTP redirects']);
        }
        $currentUrl = resolveRelativeRedirect($currentUrl, $respHeaders['location']);
        continue;
    }

    $body = $buffer;
    break;
}

$asText = isTextMime($contentType, $currentUrl);
$textOut = null;
$base64Out = null;
if ($asText) {
    if (preg_match('/charset\s*=\s*["\']?([a-z0-9_-]+)/i', $contentType, $m)) {
        $cs = strtoupper($m[1]);
        if ($cs !== 'UTF-8' && function_exists('mb_convert_encoding')) {
            $converted = @mb_convert_encoding($body, 'UTF-8', $cs);
            if (is_string($converted)) {
                $textOut = $converted;
            }
        }
    }
    if ($textOut === null) {
        $textOut = $body;
    }
} else {
    $base64Out = base64_encode($body);
}

respondProxyJson(200, [
    'ok' => $status >= 200 && $status < 300,
    'engine' => 'php-proxy',
    'url' => $currentUrl,
    'status' => $status,
    'statusText' => '',
    'contentType' => $contentType,
    'isBinary' => !$asText,
    'size' => strlen($body),
    'text' => $textOut,
    'base64' => $base64Out
]);
