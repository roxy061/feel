/**
 * Vercel Serverless Proxy for Web Source Cloner (App 21)
 * Fetches public http/https resources safely with strict SSRF checks,
 * manual redirect hop validation, size limits, and JSON+base64 encoding.
 */
'use strict';

const dns = require('node:dns').promises;
const net = require('node:net');

const MAX_BYTES = 5 * 1024 * 1024; // 5 MB safety limit per asset
const FETCH_TIMEOUT_MS = 10000;
const MAX_REDIRECTS = 5;

function setCorsHeaders(res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('Cache-Control', 'no-store, no-cache, must-revalidate, max-age=0');
}

function sendJson(res, statusCode, payload) {
  setCorsHeaders(res);
  res.statusCode = statusCode;
  res.setHeader('Content-Type', 'application/json; charset=utf-8');
  res.end(JSON.stringify(payload));
}

function isBlockedIpv4(ip) {
  const parts = ip.split('.').map(Number);
  if (parts.length !== 4 || parts.some(n => !Number.isInteger(n) || n < 0 || n > 255)) {
    return true;
  }
  const [a, b, c] = parts;
  if (a === 0 || a === 10 || a === 127) return true;
  if (a === 169 && b === 254) return true; // Link-local / cloud metadata (169.254.169.254)
  if (a === 172 && b >= 16 && b <= 31) return true; // RFC1918
  if (a === 192 && b === 168) return true; // RFC1918
  if (a === 100 && b >= 64 && b <= 127) return true; // CGNAT RFC6598
  if (a === 192 && b === 0 && (c === 0 || c === 2)) return true;
  if (a === 198 && (b === 18 || b === 19)) return true; // Benchmarking
  if (a >= 224) return true; // Multicast & reserved
  return false;
}

function isBlockedIp(rawIp) {
  if (!rawIp || typeof rawIp !== 'string') return true;
  const clean = rawIp.replace(/^\[|\]$/g, '').split('%')[0].toLowerCase();
  if (net.isIPv4(clean)) return isBlockedIpv4(clean);
  if (!net.isIPv6(clean)) return true;

  // Handle IPv4-mapped IPv6 addresses (e.g. ::ffff:127.0.0.1 or ::ffff:7f00:1)
  if (clean.startsWith('::ffff:')) {
    const tail = clean.slice(7);
    if (net.isIPv4(tail)) return isBlockedIpv4(tail);
    const hexMatch = /^([0-9a-f]{1,4}):([0-9a-f]{1,4})$/.exec(tail);
    if (hexMatch) {
      const hi = parseInt(hexMatch[1], 16);
      const lo = parseInt(hexMatch[2], 16);
      const v4 = `${(hi >> 8) & 0xff}.${hi & 0xff}.${(lo >> 8) & 0xff}.${lo & 0xff}`;
      return isBlockedIpv4(v4);
    }
    return true;
  }

  if (clean === '::' || clean === '::1') return true;
  if (clean.startsWith('fc') || clean.startsWith('fd')) return true; // Unique local fc00::/7
  if (/^fe[89ab][0-9a-f]?:/i.test(clean) || clean.startsWith('fe80')) return true; // Link-local fe80::/10
  if (clean.startsWith('ff')) return true; // Multicast
  return false;
}

function isBlockedHostname(hostname) {
  const h = hostname.replace(/^\[|\]$/g, '').replace(/\.+$/, '').toLowerCase();
  if (!h) return true;
  if (
    h === 'localhost' ||
    h.endsWith('.localhost') ||
    h.endsWith('.local') ||
    h.endsWith('.internal') ||
    h.endsWith('.localdomain') ||
    h.endsWith('.arpa') ||
    h === 'metadata.google.internal'
  ) {
    return true;
  }
  return false;
}

async function assertPublicTarget(rawUrl) {
  let parsed;
  try {
    parsed = new URL(String(rawUrl || '').trim());
  } catch {
    throw Object.assign(new Error('Invalid target URL format'), { statusCode: 400 });
  }
  if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
    throw Object.assign(new Error('Only http:// and https:// protocols are permitted'), { statusCode: 400 });
  }
  if (parsed.username || parsed.password) {
    throw Object.assign(new Error('Credentials inside URL are not allowed'), { statusCode: 400 });
  }
  if (parsed.href.length > 2048) {
    throw Object.assign(new Error('URL exceeds maximum allowed length'), { statusCode: 400 });
  }

  const host = parsed.hostname.replace(/^\[|\]$/g, '');
  if (isBlockedHostname(host)) {
    throw Object.assign(new Error('Access to internal or localhost hostnames is blocked'), { statusCode: 403 });
  }

  if (net.isIP(host)) {
    if (isBlockedIp(host)) {
      throw Object.assign(new Error('Access to private or reserved IP ranges is blocked'), { statusCode: 403 });
    }
    return parsed.href;
  }

  let records;
  try {
    records = await dns.lookup(host, { all: true, verbatim: true });
  } catch {
    throw Object.assign(new Error(`Unable to resolve hostname: ${host}`), { statusCode: 502 });
  }
  if (!Array.isArray(records) || records.length === 0) {
    throw Object.assign(new Error(`No DNS records found for: ${host}`), { statusCode: 502 });
  }
  for (const rec of records) {
    if (isBlockedIp(rec.address)) {
      throw Object.assign(new Error('Resolved target points to a private or reserved network'), { statusCode: 403 });
    }
  }
  return parsed.href;
}

function isTextContentType(contentType, targetUrl) {
  const ct = String(contentType || '').toLowerCase();
  if (
    ct.startsWith('text/') ||
    ct.includes('javascript') ||
    ct.includes('ecmascript') ||
    ct.includes('json') ||
    ct.includes('xml') ||
    ct.includes('image/svg+xml')
  ) {
    return true;
  }
  if (!ct) {
    const cleanPath = String(targetUrl || '').split('?')[0].toLowerCase();
    return /\.(html?|css|m?js|json|svg|xml|txt|md)$/i.test(cleanPath);
  }
  return false;
}

function decodeTextBuffer(buf, contentType) {
  let charset = 'utf-8';
  const ctMatch = /charset\s*=\s*["']?([a-z0-9_-]+)/i.exec(String(contentType || ''));
  if (ctMatch) charset = ctMatch[1].toLowerCase();

  try {
    return new TextDecoder(charset).decode(buf);
  } catch {
    return buf.toString('utf8');
  }
}

async function readBodyCapped(response) {
  const cl = Number(response.headers.get('content-length') || 0);
  if (cl > MAX_BYTES) {
    throw Object.assign(new Error(`Asset exceeds 5 MB limit (${Math.round(cl / 1024)} KB)`), { statusCode: 413 });
  }
  if (!response.body) return Buffer.alloc(0);

  const reader = response.body.getReader();
  const chunks = [];
  let total = 0;
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    if (value) {
      total += value.byteLength;
      if (total > MAX_BYTES) {
        try { await reader.cancel(); } catch {}
        throw Object.assign(new Error('Upstream response exceeded 5 MB size cap'), { statusCode: 413 });
      }
      chunks.push(Buffer.from(value));
    }
  }
  return Buffer.concat(chunks, total);
}

async function readRequestUrl(req) {
  const reqUrl = new URL(req.url || '/', 'http://localhost');
  let targetUrl = reqUrl.searchParams.get('url') || '';
  if (!targetUrl && req.method === 'POST') {
    if (req.body && typeof req.body === 'object' && req.body.url) {
      targetUrl = req.body.url;
    } else if (typeof req.body === 'string') {
      try { targetUrl = JSON.parse(req.body).url || ''; } catch {}
    } else {
      const rawBody = await new Promise((resolve) => {
        const parts = [];
        let len = 0;
        req.on('data', chunk => {
          len += chunk.length;
          if (len <= 16384) parts.push(chunk);
        });
        req.on('end', () => resolve(Buffer.concat(parts).toString('utf8')));
        req.on('error', () => resolve(''));
      });
      try { targetUrl = JSON.parse(rawBody).url || ''; } catch {}
    }
  }
  return targetUrl;
}

module.exports = async function handler(req, res) {
  if (req.method === 'OPTIONS') {
    setCorsHeaders(res);
    res.statusCode = 200;
    res.end();
    return;
  }
  if (req.method !== 'GET' && req.method !== 'POST') {
    return sendJson(res, 405, { ok: false, error: 'Method not allowed' });
  }

  let currentUrl;
  try {
    const rawTarget = await readRequestUrl(req);
    currentUrl = await assertPublicTarget(rawTarget);
  } catch (err) {
    return sendJson(res, err.statusCode || 400, { ok: false, error: err.message });
  }

  const controller = new AbortController();
  const timer = setTimeout(() => controller.abort(), FETCH_TIMEOUT_MS);

  try {
    let response = null;
    for (let hop = 0; hop <= MAX_REDIRECTS; hop++) {
      // Re-validate SSRF rules on every redirect target before fetching
      currentUrl = await assertPublicTarget(currentUrl);
      response = await fetch(currentUrl, {
        method: 'GET',
        redirect: 'manual',
        signal: controller.signal,
        headers: {
          'User-Agent': 'Mozilla/5.0 (compatible; FeelWebCloner/1.0; +https://feel-ochre.vercel.app)',
          'Accept': 'text/html,application/xhtml+xml,text/css,application/javascript,image/*,*/*;q=0.8',
          'Accept-Language': 'th,en-US;q=0.9,en;q=0.8'
        }
      });

      const status = response.status;
      if (status >= 300 && status < 400) {
        const location = response.headers.get('location');
        if (!location) break;
        if (hop === MAX_REDIRECTS) {
          throw Object.assign(new Error('Too many HTTP redirects'), { statusCode: 502 });
        }
        try { await response.body?.cancel(); } catch {}
        currentUrl = new URL(location, currentUrl).href;
        continue;
      }
      break;
    }

    const contentType = response.headers.get('content-type') || 'application/octet-stream';
    const buf = await readBodyCapped(response);
    const asText = isTextContentType(contentType, currentUrl);

    return sendJson(res, 200, {
      ok: response.ok,
      engine: 'vercel-serverless',
      url: currentUrl,
      status: response.status,
      statusText: response.statusText || '',
      contentType,
      isBinary: !asText,
      size: buf.length,
      text: asText ? decodeTextBuffer(buf, contentType) : null,
      base64: !asText ? buf.toString('base64') : null
    });
  } catch (err) {
    const isAbort = err && err.name === 'AbortError';
    return sendJson(res, err.statusCode || (isAbort ? 504 : 502), {
      ok: false,
      engine: 'vercel-serverless',
      error: isAbort ? 'Upstream request timed out after 10 seconds' : (err.message || 'Upstream fetch failed')
    });
  } finally {
    clearTimeout(timer);
  }
};
