/* Feel Web Apps — Shared Navigation Hub, Theme Sync & Page Info Shell */
(function (window, document) {
    'use strict';

    var RECENT_KEY = 'feel_recent_apps_v1';
    var THEME_KEY = 'feel_theme';
    var LEGACY_DARK_KEY = 'nst_dark';
    var ALT_THEME_KEY_1 = 'feel_portfolio_theme';
    var ALT_THEME_KEY_2 = 'theme';

    var CATEGORIES = [
        { id: 'all', label: 'ทั้งหมด (21)', icon: 'fa-grip' },
        { id: 'ai-docs', label: 'AI & เอกสาร', icon: 'fa-wand-magic-sparkles' },
        { id: 'media-vault', label: 'รูปภาพ & เซฟ', icon: 'fa-photo-film' },
        { id: 'dev-hw', label: 'Dev & อุปกรณ์', icon: 'fa-code' },
        { id: 'nakhon-utils', label: 'นครฯ & ทั่วไป', icon: 'fa-compass' }
    ];

    var APPS = [
        { file: 'wheel.html', title: 'นิทรรศการผลงาน 3 มิติ', sub: 'วงล้อชม 6 ผลงานเด่น เกียรติบัตร & ฝึกงาน', badge: '3D', cat: 'nakhon-utils', icon: 'fa-dharmachakra', color: '#d97706', bg: 'rgba(217,119,6,0.12)', kw: 'wheel spin 3d portfolio showcase วงล้อ ผลงาน เกียรติบัตร ฝึกงาน' },
        { file: 'southern-food.html', title: 'หรอยเมืองคอน! 48 ร้านเด็ด', sub: 'สุ่ม 48 ร้านอร่อย อ.เมืองนครศรีฯ + แพลน 1 วัน', badge: '48 ร้าน', cat: 'nakhon-utils', icon: 'fa-utensils', color: '#d97706', bg: 'rgba(217,119,6,0.12)', kw: 'food southern nakhon อาหาร นครศรีธรรมราช สุ่มร้าน กินไรดี แผนที่' },
        { file: 'cctv.html', title: 'กล้อง CCTV เทศบาล (222 จุด)', sub: 'วัดระดับน้ำ 32, จราจร 131, รร. 41, Safety 18', badge: '222 จุด', cat: 'nakhon-utils', icon: 'fa-video', color: '#d97706', bg: 'rgba(217,119,6,0.12)', kw: 'cctv live camera จราจร น้ำท่วม กล้อง นครศรีธรรมราช' },
        { file: 'weather.html', title: 'เรดาร์อากาศ & พายุภาคใต้', sub: 'พยากรณ์ 24 ชม./7 วัน 6 พื้นที่ + แผนที่ Windy', badge: 'Radar', cat: 'nakhon-utils', icon: 'fa-cloud-sun-rain', color: '#d97706', bg: 'rgba(217,119,6,0.12)', kw: 'weather rain radar windy อากาศ ฝน ตก พายุ ภาคใต้ นครศรี' },
        { file: 'rubik.html', title: 'จับเวลา & ฝึกแก้รูบิค 3x3', sub: 'WCA Scramble 20 จังหวะ จำลอง 6 ด้าน & CFOP', badge: 'WCA', cat: 'nakhon-utils', icon: 'fa-cube', color: '#d97706', bg: 'rgba(217,119,6,0.12)', kw: 'rubik cube wca cfop timer 3d รูบิค จับเวลา' },
        { file: 'ai-assistant.html', title: 'Feel AI Assistant & สรุปไฟล์', sub: 'ถาม-ตอบจากเว็บจริง สรุปเอกสาร + WebGPU/Ollama/Gemini', badge: 'AI Chat', cat: 'ai-docs', icon: 'fa-wand-magic-sparkles', color: '#4f46e5', bg: 'rgba(79,70,229,0.12)', kw: 'ai assistant chat summarize ollama webgpu gemini สรุป ผู้ช่วย' },
        { file: 'vault.html', title: 'SafeBox คลังไฟล์ชัด 100%', sub: 'เก็บ Bit-for-Bit ตรวจ SHA-256 & แชร์ PIN 6 หลัก', badge: 'SHA-256', cat: 'media-vault', icon: 'fa-vault', color: '#059669', bg: 'rgba(5,150,105,0.12)', kw: 'vault safebox sha256 pin ตู้เซฟ ฝากไฟล์ รูปความชัด' },
        { file: 'photo-studio.html', title: 'Photo Studio & ลบพื้นหลัง', sub: 'ตัดฉากหลังอัตโนมัติ รูปติดบัตร & โหลด PNG ชัดเต็ม', badge: 'Studio', cat: 'media-vault', icon: 'fa-wand-sparkles', color: '#059669', bg: 'rgba(5,150,105,0.12)', kw: 'photo studio remove background แต่งรูป ลบพื้นหลัง ติดบัตร PNG' },
        { file: 'ai-tools.html', title: 'คลัง 166+ เว็บ AI & โฮสต์ฟรี', sub: '130+ เว็บ AI 8 สายงาน + 36+ เว็บรันเว็บฟรี', badge: '166+ เว็บ', cat: 'ai-docs', icon: 'fa-robot', color: '#4f46e5', bg: 'rgba(79,70,229,0.12)', kw: 'ai tools directory hosting free คลัง เครื่องมือ รวมเว็บ' },
        { file: 'dev-tools.html', title: 'Dev Utilities ออฟไลน์', sub: 'JSON Tree, JWT HS256, Base64, URL & Hash/UUID', badge: 'DevTools', cat: 'dev-hw', icon: 'fa-toolbox', color: '#0284c7', bg: 'rgba(2,132,199,0.12)', kw: 'dev tools json jwt base64 url timestamp hash uuid' },
        { file: 'subnet.html', title: 'IP & Subnet / VLAN', sub: 'คำนวณ CIDR, FLSM/VLSM, Binary 32-Bit & Netplan', badge: 'Network', cat: 'dev-hw', icon: 'fa-network-wired', color: '#0284c7', bg: 'rgba(2,132,199,0.12)', kw: 'subnet cidr flsm vlsm netplan ipv4 network vlan' },
        { file: 'regex.html', title: 'Regex Tester & 16 สูตร', sub: 'ทดสอบสด Capture Group, Replace & โค้ด JS/PHP/Py', badge: 'Regex', cat: 'dev-hw', icon: 'fa-code', color: '#0284c7', bg: 'rgba(2,132,199,0.12)', kw: 'regex regular expression preset javascript php python' },
        { file: 'diff.html', title: 'Diff Checker เทียบโค้ด', sub: 'Side-by-Side, Word Diff, จัด JSON & โหลด .diff', badge: 'Diff', cat: 'dev-hw', icon: 'fa-code-compare', color: '#0284c7', bg: 'rgba(2,132,199,0.12)', kw: 'diff compare word patch code เทียบ โค้ด config' },
        { file: 'keygen.html', title: 'KeyGen, .env & Hash', sub: 'สุ่ม CSPRNG 100% วัด Entropy, UUID v4 & HMAC', badge: 'Security', cat: 'dev-hw', icon: 'fa-key', color: '#0284c7', bg: 'rgba(2,132,199,0.12)', kw: 'keygen password entropy uuid sha256 hmac env csprng' },
        { file: 'pdf-tools.html', title: 'PDF Merge & Split ออฟไลน์', sub: 'รวม แยกช่วงหน้า หมุน 90° & ลบหน้าในเครื่อง 100%', badge: 'PDF', cat: 'ai-docs', icon: 'fa-file-pdf', color: '#4f46e5', bg: 'rgba(79,70,229,0.12)', kw: 'pdf merge split rotate รวม แยก หมุน ลบหน้า เอกสาร' },
        { file: 'image-converter.html', title: 'Image Compressor & ZIP', sub: 'แปลง JPG/PNG/WebP ย่อขนาดเป็นชุด & โหลด .ZIP', badge: 'Batch ZIP', cat: 'media-vault', icon: 'fa-images', color: '#059669', bg: 'rgba(5,150,105,0.12)', kw: 'image converter webp resize batch zip แปลงรูป ย่อภาพ' },
        { file: 'qr-studio.html', title: 'Custom QR Code Studio', sub: 'QR เว็บ Wi-Fi PromptPay ใส่โลโก้ PNG 2048px/SVG', badge: 'QR/SVG', cat: 'media-vault', icon: 'fa-qrcode', color: '#059669', bg: 'rgba(5,150,105,0.12)', kw: 'qr code studio promptpay wifi emvco svg png คิวอาร์ โลโก้' },
        { file: 'ocr.html', title: 'OCR Image-to-Text', sub: 'ดึงข้อความไทย/อังกฤษจากภาพ & สไลด์ด้วย Tesseract', badge: 'TH / EN', cat: 'ai-docs', icon: 'fa-file-lines', color: '#4f46e5', bg: 'rgba(79,70,229,0.12)', kw: 'ocr image to text tesseract otsu ไทย อังกฤษ ชีทเรียน สไลด์' },
        { file: 'resume-builder.html', title: 'Resume & Portfolio A4 PDF', sub: '20 เทมเพลต A4 + สร้างเองได้, QR พอร์ต & PDF', badge: 'A4 PDF', cat: 'ai-docs', icon: 'fa-id-card', color: '#4f46e5', bg: 'rgba(79,70,229,0.12)', kw: 'resume cv portfolio pdf a4 autofit custom เรซูเม่ พิมพ์' },
        { file: 'hardware-tester.html', title: 'Hardware Tester เช็กเครื่อง', sub: 'Dead Pixel 12 สี, ไมค์ FFT, กล้อง & คีย์บอร์ด NKRO', badge: 'Hardware', cat: 'dev-hw', icon: 'fa-microchip', color: '#0284c7', bg: 'rgba(2,132,199,0.12)', kw: 'hardware dead pixel refresh mic fft webcam keyboard nkro' },
        { file: 'web-cloner.html', title: 'Web Source Cloner & ZIP', sub: 'ดึง HTML/CSS/JS/รูปภาพผ่าน Proxy/Bookmarklet -> .ZIP', badge: 'Cloner', cat: 'dev-hw', icon: 'fa-file-zipper', color: '#0284c7', bg: 'rgba(2,132,199,0.12)', kw: 'web cloner source html css js zip proxy bookmarklet' }
    ];

    var currentFile = (function () {
        try {
            var p = (window.location.pathname || '').split('/').pop() || 'index.html';
            return p.toLowerCase();
        } catch (e) {
            return 'index.html';
        }
    })();

    var hubState = {
        built: false,
        isOpen: false,
        query: '',
        category: 'all',
        activeIndex: -1,
        filtered: []
    };

    var helpState = {
        built: false,
        isOpen: false
    };

    function syncThemeStorage() {
        try {
            var primary = window.localStorage.getItem(THEME_KEY);
            var legacy = window.localStorage.getItem(LEGACY_DARK_KEY);
            var alt1 = window.localStorage.getItem(ALT_THEME_KEY_1);
            var alt2 = window.localStorage.getItem(ALT_THEME_KEY_2);
            var isDark = false;
            if (primary === 'dark' || primary === 'light') {
                isDark = primary === 'dark';
            } else if (alt1 === 'dark' || alt1 === 'light') {
                isDark = alt1 === 'dark';
            } else if (alt2 === 'dark' || alt2 === 'light') {
                isDark = alt2 === 'dark';
            } else if (legacy === 'true' || legacy === 'false') {
                isDark = legacy === 'true';
            } else if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
                isDark = true;
            }
            if (isDark) {
                document.documentElement.classList.add('dark');
            } else {
                document.documentElement.classList.remove('dark');
            }
            var modeStr = isDark ? 'dark' : 'light';
            window.localStorage.setItem(THEME_KEY, modeStr);
            window.localStorage.setItem(ALT_THEME_KEY_1, modeStr);
            window.localStorage.setItem(ALT_THEME_KEY_2, modeStr);
            window.localStorage.setItem(LEGACY_DARK_KEY, String(isDark));
            updateThemeIcons(isDark);
        } catch (e) {}
    }

    function updateThemeIcons(isDark) {
        var iconA = document.getElementById('theme-icon');
        if (iconA) {
            iconA.className = isDark ? 'fa-solid fa-sun text-amber-400' : 'fa-solid fa-moon';
        }
        var iconB = document.getElementById('darkIcon');
        if (iconB) {
            iconB.className = isDark ? 'fa-solid fa-sun text-amber-400' : 'fa-solid fa-moon';
        }
    }

    function persistTheme(isDark) {
        try {
            var modeStr = isDark ? 'dark' : 'light';
            window.localStorage.setItem(THEME_KEY, modeStr);
            window.localStorage.setItem(ALT_THEME_KEY_1, modeStr);
            window.localStorage.setItem(ALT_THEME_KEY_2, modeStr);
            window.localStorage.setItem(LEGACY_DARK_KEY, String(isDark));
        } catch (e) {}
        updateThemeIcons(isDark);
    }

    function installThemeHooks() {
        if (typeof window.toggleTheme === 'function' && !window.toggleTheme.__feelHooked) {
            var origToggle = window.toggleTheme;
            var wrapped = function () {
                var res = origToggle.apply(this, arguments);
                persistTheme(document.documentElement.classList.contains('dark'));
                return res;
            };
            wrapped.__feelHooked = true;
            window.toggleTheme = wrapped;
        }
        if (typeof window.toggleDarkMode === 'function' && !window.toggleDarkMode.__feelHooked) {
            var origDark = window.toggleDarkMode;
            var wrappedDark = function () {
                var res = origDark.apply(this, arguments);
                persistTheme(document.documentElement.classList.contains('dark'));
                return res;
            };
            wrappedDark.__feelHooked = true;
            window.toggleDarkMode = wrappedDark;
        }
    }

    function recordRecentVisit() {
        if (!currentFile || currentFile === 'index.html') return;
        var matched = null;
        for (var i = 0; i < APPS.length; i++) {
            if (APPS[i].file === currentFile) {
                matched = APPS[i].file;
                break;
            }
        }
        if (!matched) return;
        try {
            var raw = window.localStorage.getItem(RECENT_KEY);
            var arr = raw ? JSON.parse(raw) : [];
            if (!Array.isArray(arr)) arr = [];
            arr = arr.filter(function (f) { return f !== matched; });
            arr.unshift(matched);
            if (arr.length > 5) arr.length = 5;
            window.localStorage.setItem(RECENT_KEY, JSON.stringify(arr));
        } catch (e) {}
    }

    function getRecentApps() {
        try {
            var raw = window.localStorage.getItem(RECENT_KEY);
            var arr = raw ? JSON.parse(raw) : [];
            if (!Array.isArray(arr)) return [];
            var out = [];
            for (var i = 0; i < arr.length; i++) {
                for (var j = 0; j < APPS.length; j++) {
                    if (APPS[j].file === arr[i]) {
                        out.push(APPS[j]);
                        break;
                    }
                }
            }
            return out;
        } catch (e) {
            return [];
        }
    }

    function el(tag, className, text) {
        var node = document.createElement(tag);
        if (className) node.className = className;
        if (text !== undefined && text !== null) node.textContent = text;
        return node;
    }

    function iconNode(faClass, color) {
        var i = document.createElement('i');
        i.className = 'fa-solid ' + faClass;
        i.setAttribute('aria-hidden', 'true');
        if (color) i.style.color = color;
        return i;
    }

    var hubOverlay, hubSearchInput, hubClearBtn, hubCatsWrap, hubBody;
    var helpOverlay, helpTitleNode, helpBodyNode;

    function buildHub() {
        if (hubState.built) return;
        hubState.built = true;

        hubOverlay = el('div', 'feel-overlay no-print');
        hubOverlay.id = 'feel-app-hub';
        hubOverlay.setAttribute('role', 'dialog');
        hubOverlay.setAttribute('aria-modal', 'true');
        hubOverlay.setAttribute('aria-label', 'ศูนย์รวมเว็บแอปทั้ง 21 รายการ');
        hubOverlay.setAttribute('aria-hidden', 'true');

        var dialog = el('div', 'feel-dialog');
        var handle = el('div', 'feel-sheet-handle');
        dialog.appendChild(handle);

        var head = el('div', 'feel-hub-head');
        var topRow = el('div', 'feel-hub-top');
        var titleWrap = el('div', 'flex items-center gap-2.5 min-w-0');
        var brandIco = el('span', 'w-8 h-8 rounded-xl bg-blue-500/15 text-blue-600 dark:text-blue-400 flex items-center justify-center shrink-0');
        brandIco.appendChild(iconNode('fa-grip'));
        var titleText = el('div', 'min-w-0');
        var titleMain = el('div', 'text-sm font-semibold leading-tight', 'ศูนย์รวมเว็บแอป');
        var titleSub = el('div', 'text-xs opacity-70 truncate', 'ค้นหาและสลับใช้งานได้ทันทีทั้ง 21 เครื่องมือ');
        titleText.appendChild(titleMain);
        titleText.appendChild(titleSub);
        titleWrap.appendChild(brandIco);
        titleWrap.appendChild(titleText);

        var rightBtns = el('div', 'flex items-center gap-1.5 shrink-0');
        if (currentFile !== 'index.html') {
            var homeLink = el('a', 'px-2.5 py-1.5 rounded-full text-xs font-medium border border-[var(--feel-border)] hover:border-[var(--feel-blue)] flex items-center gap-1.5');
            homeLink.href = 'index.html';
            homeLink.appendChild(iconNode('fa-house'));
            homeLink.appendChild(document.createTextNode('หน้าหลัก'));
            rightBtns.appendChild(homeLink);
        }
        var closeBtn = el('button', 'feel-icon-btn w-9 h-9 hover:bg-black/5 dark:hover:bg-white/10');
        closeBtn.type = 'button';
        closeBtn.setAttribute('aria-label', 'ปิดหน้าต่างเลือกแอป');
        closeBtn.appendChild(iconNode('fa-xmark'));
        closeBtn.addEventListener('click', closeHub);
        rightBtns.appendChild(closeBtn);

        topRow.appendChild(titleWrap);
        topRow.appendChild(rightBtns);
        head.appendChild(topRow);

        var searchWrap = el('div', 'feel-hub-search-wrap');
        var searchIco = el('span', 'feel-hub-search-icon');
        searchIco.appendChild(iconNode('fa-magnifying-glass'));
        hubSearchInput = el('input', 'feel-hub-search');
        hubSearchInput.id = 'feel-hub-search';
        hubSearchInput.type = 'search';
        hubSearchInput.placeholder = 'ค้นหาแอป เช่น QR, OCR, PDF, อาหาร, CCTV...';
        hubSearchInput.setAttribute('autocomplete', 'off');
        hubSearchInput.setAttribute('spellcheck', 'false');

        hubClearBtn = el('button', 'feel-hub-clear hidden');
        hubClearBtn.type = 'button';
        hubClearBtn.setAttribute('aria-label', 'ล้างคำค้นหา');
        hubClearBtn.appendChild(iconNode('fa-circle-xmark'));
        hubClearBtn.addEventListener('click', function () {
            hubState.query = '';
            hubSearchInput.value = '';
            hubClearBtn.classList.add('hidden');
            hubState.activeIndex = -1;
            renderHubBody();
            hubSearchInput.focus();
        });

        hubSearchInput.addEventListener('input', function () {
            hubState.query = (hubSearchInput.value || '').trim().toLowerCase();
            hubClearBtn.classList.toggle('hidden', !hubState.query);
            hubState.activeIndex = -1;
            renderHubBody();
        });

        searchWrap.appendChild(searchIco);
        searchWrap.appendChild(hubSearchInput);
        searchWrap.appendChild(hubClearBtn);
        head.appendChild(searchWrap);

        hubCatsWrap = el('div', 'feel-hub-cats');
        renderCategoryChips();
        head.appendChild(hubCatsWrap);

        hubBody = el('div', 'feel-hub-body');

        var foot = el('div', 'feel-hub-foot');
        var kbdHint = el('div', 'hidden sm:flex items-center gap-2');
        var k1 = el('span', 'feel-kbd', 'Ctrl+K');
        kbdHint.appendChild(k1);
        kbdHint.appendChild(document.createTextNode('สลับแอปด่วน · '));
        var k2 = el('span', 'feel-kbd', '↑↓');
        kbdHint.appendChild(k2);
        kbdHint.appendChild(document.createTextNode('เลือก · '));
        var k3 = el('span', 'feel-kbd', 'Esc');
        kbdHint.appendChild(k3);
        kbdHint.appendChild(document.createTextNode('ปิด'));

        var mobileHint = el('div', 'sm:hidden text-xs opacity-75', 'แตะเพื่อเปิดใช้งานทันที');
        var allLink = el('a', '', 'ดูทั้งหมดใน Portfolio →');
        allLink.href = 'index.html#live-services';
        allLink.addEventListener('click', closeHub);

        foot.appendChild(kbdHint);
        foot.appendChild(mobileHint);
        foot.appendChild(allLink);

        dialog.appendChild(head);
        dialog.appendChild(hubBody);
        dialog.appendChild(foot);
        hubOverlay.appendChild(dialog);

        hubOverlay.addEventListener('click', function (e) {
            if (e.target === hubOverlay) closeHub();
        });

        document.body.appendChild(hubOverlay);
        renderHubBody();
    }

    function renderCategoryChips() {
        if (!hubCatsWrap) return;
        hubCatsWrap.textContent = '';
        for (var i = 0; i < CATEGORIES.length; i++) {
            (function (cat) {
                var btn = el('button', 'feel-cat-chip' + (hubState.category === cat.id ? ' is-active' : ''));
                btn.type = 'button';
                btn.appendChild(iconNode(cat.icon));
                btn.appendChild(document.createTextNode(cat.label));
                btn.addEventListener('click', function () {
                    hubState.category = cat.id;
                    hubState.activeIndex = -1;
                    renderCategoryChips();
                    renderHubBody();
                });
                hubCatsWrap.appendChild(btn);
            })(CATEGORIES[i]);
        }
    }

    function createAppItemNode(app, index) {
        var a = el('a', 'feel-hub-item');
        if (app.file === currentFile) a.classList.add('is-current');
        if (index === hubState.activeIndex) a.classList.add('is-kb-active');
        a.href = app.file;
        a.dataset.hubIdx = String(index);

        var icoBox = el('div', 'feel-hub-ico');
        icoBox.style.background = app.bg;
        icoBox.appendChild(iconNode(app.icon, app.color));

        var meta = el('div', 'feel-hub-meta');
        meta.appendChild(el('div', 'feel-hub-title', app.title));
        meta.appendChild(el('div', 'feel-hub-sub', app.sub));

        var badge = el('span', 'feel-hub-badge', app.file === currentFile ? 'ใช้อยู่' : app.badge);

        a.appendChild(icoBox);
        a.appendChild(meta);
        a.appendChild(badge);
        return a;
    }

    function renderHubBody() {
        if (!hubBody) return;
        hubBody.textContent = '';

        var q = hubState.query;
        var cat = hubState.category;

        if (!q && cat === 'all') {
            var recent = getRecentApps().filter(function (a) { return a.file !== currentFile; });
            if (recent.length > 0) {
                var recentSec = el('div');
                recentSec.appendChild(el('div', 'feel-hub-section-label', 'เปิดใช้งานล่าสุด'));
                var row = el('div', 'feel-recent-row');
                for (var r = 0; r < recent.length; r++) {
                    var ra = recent[r];
                    var pill = el('a', 'feel-recent-pill');
                    pill.href = ra.file;
                    pill.appendChild(iconNode(ra.icon, ra.color));
                    pill.appendChild(document.createTextNode(ra.title));
                    row.appendChild(pill);
                }
                recentSec.appendChild(row);
                hubBody.appendChild(recentSec);
            }
        }

        var filtered = [];
        for (var i = 0; i < APPS.length; i++) {
            var item = APPS[i];
            if (cat !== 'all' && item.cat !== cat) continue;
            if (q) {
                var hay = (item.title + ' ' + item.sub + ' ' + item.badge + ' ' + item.kw + ' ' + item.file).toLowerCase();
                if (hay.indexOf(q) === -1) continue;
            }
            filtered.push(item);
        }
        hubState.filtered = filtered;

        var listSec = el('div');
        var labelRow = el('div', 'feel-hub-section-label');
        labelRow.appendChild(el('span', '', q ? 'ผลการค้นหา (' + filtered.length + ')' : 'เว็บแอป (' + filtered.length + ')'));
        listSec.appendChild(labelRow);

        if (filtered.length === 0) {
            var empty = el('div', 'text-center py-8 opacity-75 text-sm');
            var emptyIco = el('div', 'text-2xl mb-2 opacity-60');
            emptyIco.appendChild(iconNode('fa-magnifying-glass'));
            empty.appendChild(emptyIco);
            empty.appendChild(el('div', '', 'ไม่พบเว็บแอปที่ตรงกับ "' + hubState.query + '"'));
            listSec.appendChild(empty);
        } else {
            var grid = el('div', 'feel-hub-grid');
            for (var k = 0; k < filtered.length; k++) {
                grid.appendChild(createAppItemNode(filtered[k], k));
            }
            listSec.appendChild(grid);
        }

        hubBody.appendChild(listSec);
    }

    function updateKbHighlight() {
        if (!hubBody) return;
        var nodes = hubBody.querySelectorAll('.feel-hub-item');
        for (var i = 0; i < nodes.length; i++) {
            var active = i === hubState.activeIndex;
            nodes[i].classList.toggle('is-kb-active', active);
            if (active && typeof nodes[i].scrollIntoView === 'function') {
                nodes[i].scrollIntoView({ block: 'nearest' });
            }
        }
    }

    function openHub() {
        buildHub();
        if (helpState.isOpen) closeHelp();
        hubState.isOpen = true;
        hubOverlay.classList.add('is-open');
        hubOverlay.setAttribute('aria-hidden', 'false');
        var trigger = document.getElementById('app-switcher-btn') || document.getElementById('google-apps-btn');
        if (trigger) trigger.setAttribute('aria-expanded', 'true');
        renderHubBody();
        window.setTimeout(function () {
            if (hubSearchInput && window.innerWidth >= 640) {
                hubSearchInput.focus();
            }
        }, 40);
    }

    function closeHub() {
        if (!hubState.built) return;
        hubState.isOpen = false;
        hubOverlay.classList.remove('is-open');
        hubOverlay.setAttribute('aria-hidden', 'true');
        var trigger = document.getElementById('app-switcher-btn') || document.getElementById('google-apps-btn');
        if (trigger) trigger.setAttribute('aria-expanded', 'false');
    }

    function toggleHub(forceState) {
        var next = typeof forceState === 'boolean' ? forceState : !hubState.isOpen;
        if (next) openHub();
        else closeHub();
    }

    function buildHelpSheet() {
        if (helpState.built) return;
        helpState.built = true;

        helpOverlay = el('div', 'feel-overlay no-print');
        helpOverlay.id = 'feel-help-sheet';
        helpOverlay.setAttribute('role', 'dialog');
        helpOverlay.setAttribute('aria-modal', 'true');
        helpOverlay.setAttribute('aria-hidden', 'true');

        var dialog = el('div', 'feel-dialog feel-help-dialog');
        dialog.appendChild(el('div', 'feel-sheet-handle'));

        var head = el('div', 'feel-hub-head');
        var topRow = el('div', 'feel-hub-top');
        var titleWrap = el('div', 'flex items-center gap-2.5 min-w-0');
        var infoIco = el('span', 'w-8 h-8 rounded-xl bg-blue-500/15 text-blue-600 dark:text-blue-400 flex items-center justify-center shrink-0');
        infoIco.appendChild(iconNode('fa-circle-info'));
        helpTitleNode = el('div', 'text-sm font-semibold truncate', 'รายละเอียดเครื่องมือ');
        titleWrap.appendChild(infoIco);
        titleWrap.appendChild(helpTitleNode);

        var closeBtn = el('button', 'feel-icon-btn w-9 h-9 hover:bg-black/5 dark:hover:bg-white/10');
        closeBtn.type = 'button';
        closeBtn.setAttribute('aria-label', 'ปิดคำแนะนำ');
        closeBtn.appendChild(iconNode('fa-xmark'));
        closeBtn.addEventListener('click', closeHelp);

        topRow.appendChild(titleWrap);
        topRow.appendChild(closeBtn);
        head.appendChild(topRow);

        helpBodyNode = el('div', 'feel-hub-body text-sm leading-relaxed');
        dialog.appendChild(head);
        dialog.appendChild(helpBodyNode);
        helpOverlay.appendChild(dialog);

        helpOverlay.addEventListener('click', function (e) {
            if (e.target === helpOverlay) closeHelp();
        });

        document.body.appendChild(helpOverlay);
    }

    function openHelp(title, bullets) {
        buildHelpSheet();
        if (hubState.isOpen) closeHub();
        helpTitleNode.textContent = title || 'รายละเอียดเครื่องมือ';
        helpBodyNode.textContent = '';

        var list = Array.isArray(bullets) ? bullets : (bullets ? [String(bullets)] : []);
        for (var i = 0; i < list.length; i++) {
            var card = el('div', 'p-3 rounded-xl border border-[var(--feel-border)] bg-[var(--feel-surface-2)] flex items-start gap-2.5');
            var dot = el('span', 'text-blue-500 mt-0.5 shrink-0');
            dot.appendChild(iconNode('fa-check-circle'));
            var txt = el('div', 'text-[13px] leading-relaxed', list[i]);
            card.appendChild(dot);
            card.appendChild(txt);
            helpBodyNode.appendChild(card);
        }

        helpState.isOpen = true;
        helpOverlay.classList.add('is-open');
        helpOverlay.setAttribute('aria-hidden', 'false');
    }

    function closeHelp() {
        if (!helpState.built) return;
        helpState.isOpen = false;
        helpOverlay.classList.remove('is-open');
        helpOverlay.setAttribute('aria-hidden', 'true');
    }

    function wireInfoButtons() {
        var btns = document.querySelectorAll('[data-feel-info-title], [data-feel-info]');
        for (var i = 0; i < btns.length; i++) {
            (function (btn) {
                if (btn.__feelInfoBound) return;
                btn.__feelInfoBound = true;
                btn.addEventListener('click', function () {
                    var title = btn.getAttribute('data-feel-info-title') || document.title;
                    var raw = btn.getAttribute('data-feel-info') || '';
                    var lines = raw.split('|').map(function (s) { return s.trim(); }).filter(Boolean);
                    if (lines.length === 0) {
                        var metaDesc = document.querySelector('meta[name="description"]');
                        if (metaDesc && metaDesc.getAttribute('content')) {
                            lines = [metaDesc.getAttribute('content')];
                        }
                    }
                    openHelp(title, lines);
                });
            })(btns[i]);
        }
    }

    function isShortcutSuspended() {
        if (document.fullscreenElement) return true;
        var fsOv = document.getElementById('screen-fullscreen-overlay') || document.getElementById('screen-test-overlay');
        if (fsOv && !fsOv.classList.contains('hidden')) return true;
        var kbdLock = document.getElementById('kbd-lock-keys');
        if (kbdLock && kbdLock.checked) return true;
        if (document.body && document.body.dataset.feelSuspendShortcuts === '1') return true;
        return false;
    }

    function handleGlobalKeydown(e) {
        if ((e.ctrlKey || e.metaKey) && String(e.key || '').toLowerCase() === 'k') {
            if (isShortcutSuspended()) return;
            e.preventDefault();
            toggleHub();
            return;
        }
        if (e.key === 'Escape') {
            if (hubState.isOpen) {
                e.stopPropagation();
                closeHub();
                return;
            }
            if (helpState.isOpen) {
                e.stopPropagation();
                closeHelp();
                return;
            }
        }
        if (!hubState.isOpen) return;
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            if (hubState.filtered.length > 0) {
                hubState.activeIndex = (hubState.activeIndex + 1) % hubState.filtered.length;
                updateKbHighlight();
            }
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            if (hubState.filtered.length > 0) {
                hubState.activeIndex = (hubState.activeIndex - 1 + hubState.filtered.length) % hubState.filtered.length;
                updateKbHighlight();
            }
        } else if (e.key === 'Enter' && hubState.activeIndex >= 0 && hubState.filtered[hubState.activeIndex]) {
            e.preventDefault();
            window.location.href = hubState.filtered[hubState.activeIndex].file;
        }
    }

    function ensureUnifiedSubAppChrome() {
        var hdr = document.querySelector('header');
        if (hdr && !hdr.classList.contains('feel-header-refined')) {
            hdr.classList.add('feel-header-refined');
        }
        if (!currentFile || currentFile === 'index.html' || currentFile === 'cctv.html') return;
        if (document.body && document.body.dataset.feelNoFooter === '1') return;
        if (document.getElementById('feel-unified-footer')) return;

        var appMeta = null;
        for (var i = 0; i < APPS.length; i++) {
            if (APPS[i].file === currentFile) {
                appMeta = APPS[i];
                break;
            }
        }
        if (!appMeta) return;

        // Replace outdated legacy multi-link sub-app footers cleanly unless opted in to keep custom credits
        var existingFooters = document.querySelectorAll('body > footer, main > footer, footer');
        for (var f = 0; f < existingFooters.length; f++) {
            var ft = existingFooters[f];
            if (ft.dataset && ft.dataset.feelKeepFooter === '1') {
                return;
            }
            ft.remove();
        }

        var footer = el('footer', 'feel-unified-footer no-print');
        footer.id = 'feel-unified-footer';
        var inner = el('div', 'feel-unified-footer-inner');

        var brand = el('div', 'feel-unified-footer-brand');
        var pill = el('span', 'feel-unified-footer-pill');
        pill.appendChild(iconNode(appMeta.icon, appMeta.color));
        pill.appendChild(document.createTextNode(appMeta.title));
        brand.appendChild(pill);
        brand.appendChild(el('span', '', '© 2026 แฟ้มสะสมผลงาน นายภคพล สมัยแก้ว (ฟีล)'));

        var actions = el('div', 'feel-unified-footer-actions');

        var homeLink = el('a', 'feel-unified-footer-btn');
        homeLink.href = 'index.html';
        homeLink.appendChild(iconNode('fa-house', 'var(--feel-blue)'));
        homeLink.appendChild(document.createTextNode('หน้าหลัก Portfolio'));
        actions.appendChild(homeLink);

        var hubBtn = el('button', 'feel-unified-footer-btn');
        hubBtn.type = 'button';
        hubBtn.appendChild(iconNode('fa-grip', 'var(--feel-blue)'));
        hubBtn.appendChild(document.createTextNode('สลับเว็บแอป (21)'));
        hubBtn.addEventListener('click', function () { openHub(); });
        actions.appendChild(hubBtn);

        var infoTrigger = document.querySelector('[data-feel-info-title]');
        if (infoTrigger) {
            var infoBtn = el('button', 'feel-unified-footer-btn');
            infoBtn.type = 'button';
            infoBtn.appendChild(iconNode('fa-circle-info'));
            infoBtn.appendChild(document.createTextNode('คู่มือระบบ'));
            infoBtn.addEventListener('click', function () { infoTrigger.click(); });
            actions.appendChild(infoBtn);
        }

        inner.appendChild(brand);
        inner.appendChild(actions);
        footer.appendChild(inner);
        document.body.appendChild(footer);
    }

    function initDom() {
        syncThemeStorage();
        installThemeHooks();
        recordRecentVisit();
        wireInfoButtons();
        ensureUnifiedSubAppChrome();

        window.addEventListener('storage', function (ev) {
            if (ev.key === THEME_KEY || ev.key === LEGACY_DARK_KEY) {
                syncThemeStorage();
            }
        });
    }

    // Immediate theme bootstrap before first paint when included in <head>
    syncThemeStorage();

    // Compatibility globals replacing legacy dropdown switchers
    window.toggleSubAppSwitcher = function (forceState) {
        toggleHub(forceState);
    };

    window.toggleGoogleAppsMenu = function (forceState) {
        toggleHub(forceState);
    };

    window.openFeelAppHub = openHub;
    window.openFeelPageInfo = openHelp;

    window.FeelShell = {
        APPS: APPS,
        CATEGORIES: CATEGORIES,
        openHub: openHub,
        closeHub: closeHub,
        toggleHub: toggleHub,
        openInfo: openHelp,
        closeInfo: closeHelp,
        syncTheme: syncThemeStorage
    };

    document.addEventListener('keydown', handleGlobalKeydown);

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initDom);
    } else {
        initDom();
    }
})(window, document);
