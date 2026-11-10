"use client";

import * as React from "react";

export interface SwarmRebuildProps {
  /** Whether the rebuild swarm animation is enabled. @default true */
  enabled?: boolean;
  /** Initial speed multiplier: 1, 2, or 3. @default 1 */
  initialSpeed?: 1 | 2 | 3;
  /** Whether synthesized Web Audio sound is enabled initially. @default true */
  enableSound?: boolean;
  /** Maximum number of elements to auto-scan and rebuild. @default 45 */
  maxTargets?: number;
}

interface Cursor {
  id: number;
  x: number;
  y: number;
  angle: number;
  orbitRadius: number;
  orbitSpeed: number;
  color: string;
  role: string;
  tx: number;
  ty: number;
  busy: boolean;
}

interface TargetItem {
  id: number;
  el: HTMLElement;
  tag: string;
  started: boolean;
  completed: boolean;
  progress: number;
  duration: number;
  cursors: Cursor[];
}

const PALETTE = [
  "#f43f5e",
  "#38bdf8",
  "#a855f7",
  "#10b981",
  "#f59e0b",
  "#ec4899",
  "#6366f1",
  "#14b8a6",
];

const ROLES = [
  "div",
  "svg",
  "text",
  "grid",
  "flex",
  "css",
  "dom",
  "ui",
  "nav",
  "img",
  "btn",
];

const NUM_CURSORS = 36;

export function SwarmRebuild({
  enabled = true,
  initialSpeed = 1,
  enableSound = true,
  maxTargets = 45,
}: SwarmRebuildProps) {
  const canvasRef = React.useRef<HTMLCanvasElement | null>(null);
  const hudRef = React.useRef<HTMLDivElement | null>(null);

  React.useEffect(() => {
    if (!enabled || typeof window === "undefined") return;

    // 1. Inject Styles
    const style = document.createElement("style");
    style.id = "swarm-rebuild-styles-react";
    style.textContent = `
      [data-swarm-item]:not(.swarm-built) {
        opacity: 0 !important;
        transform: translateY(6px) scale(0.985) !important;
        transition: opacity 0.26s cubic-bezier(0.16, 1, 0.3, 1), transform 0.26s cubic-bezier(0.16, 1, 0.3, 1) !important;
      }
      [data-swarm-item].swarm-building {
        opacity: 0.85 !important;
        transform: translateY(0) scale(0.995) !important;
      }
      .swarm-built {
        opacity: 1 !important;
      }
      body.swarm-blueprint [data-swarm-item].swarm-built,
      body.swarm-blueprint .swarm-built {
        outline: 1.5px solid rgba(59, 130, 246, 0.55) !important;
        outline-offset: -1px !important;
        position: relative;
      }
      #swarm-hud-bar {
        position: fixed;
        bottom: 14px;
        left: 50%;
        transform: translateX(-50%);
        z-index: 99999;
        width: max-content;
        max-width: 95vw;
        background: rgba(18, 20, 26, 0.94);
        border: 1px solid rgba(255, 255, 255, 0.16);
        backdrop-filter: blur(14px);
        -webkit-backdrop-filter: blur(14px);
        padding: 7px 12px;
        border-radius: 9999px;
        color: #e4e4e7;
        font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
        font-size: 11px;
        display: flex;
        flex-wrap: wrap;
        align-items: center;
        justify-content: center;
        gap: 7px;
        box-shadow: 0 10px 35px rgba(0, 0, 0, 0.65), 0 0 0 1px rgba(255, 255, 255, 0.05);
        user-select: none;
      }
      #swarm-hud-bar button {
        background: rgba(255, 255, 255, 0.07);
        color: #e4e4e7;
        border: 1px solid rgba(255, 255, 255, 0.1);
        padding: 3px 8px;
        border-radius: 6px;
        font-family: inherit;
        font-size: 11px;
        font-weight: 500;
        cursor: pointer;
        display: inline-flex;
        align-items: center;
        gap: 4px;
        transition: all 0.15s ease;
        outline: none;
      }
      #swarm-hud-bar button:hover {
        background: rgba(255, 255, 255, 0.16);
        color: #ffffff;
        border-color: rgba(255, 255, 255, 0.22);
      }
      #swarm-hud-bar button.active {
        background: #4f46e5;
        color: #ffffff;
        border-color: #6366f1;
        box-shadow: 0 0 10px rgba(99, 102, 241, 0.4);
      }
      #swarm-hud-bar .swarm-hud-badge {
        display: inline-flex;
        align-items: center;
        gap: 5px;
        padding: 2px 8px;
        background: rgba(255, 255, 255, 0.05);
        border-radius: 9999px;
        font-weight: 600;
      }
      #swarm-hud-bar .swarm-dot {
        width: 7px;
        height: 7px;
        border-radius: 50%;
        background: #10b981;
        box-shadow: 0 0 8px #10b981;
      }
      @media (max-width: 640px) {
        #swarm-hud-bar {
          bottom: 10px;
          padding: 6px 10px;
          gap: 5px;
          border-radius: 14px;
          font-size: 10px;
        }
        #swarm-hud-bar button {
          padding: 2.5px 6px;
          font-size: 10px;
        }
      }
    `;
    document.head.appendChild(style);

    // 2. Setup Canvas
    const canvas = document.createElement("canvas");
    canvas.id = "swarm-overlay-canvas";
    canvas.style.cssText =
      "position:fixed;inset:0;pointer-events:none;z-index:99998;";
    document.body.appendChild(canvas);
    canvasRef.current = canvas;
    const ctx = canvas.getContext("2d");

    // 3. Setup HUD
    const hud = document.createElement("div");
    hud.id = "swarm-hud-bar";
    hud.innerHTML = `
      <span class="swarm-hud-badge">
        <span class="swarm-dot"></span>
        <span id="swarm-hud-text">36 agents · Initializing...</span>
      </span>
      <div style="display:inline-flex;gap:3px;">
        <button data-spd="1" class="${initialSpeed === 1 ? "active" : ""}">1x</button>
        <button data-spd="2" class="${initialSpeed === 2 ? "active" : ""}">2x</button>
        <button data-spd="3" class="${initialSpeed === 3 ? "active" : ""}">3x</button>
      </div>
      <button id="swarm-btn-sound" title="Toggle Sound">${enableSound ? "🔊" : "🔇"}</button>
      <button id="swarm-btn-bp" title="Toggle Blueprint Grid">Blueprint</button>
      <button id="swarm-btn-skip" title="Skip Animation">Skip</button>
      <button id="swarm-btn-replay" style="background:#4f46e5;color:#fff;border-color:#6366f1;" title="Replay Animation">↻ Replay</button>
    `;
    document.body.appendChild(hud);
    hudRef.current = hud;

    let W = window.innerWidth;
    let H = window.innerHeight;
    const resize = () => {
      W = window.innerWidth;
      H = window.innerHeight;
      canvas.width = W * window.devicePixelRatio;
      canvas.height = H * window.devicePixelRatio;
      if (ctx) {
        ctx.setTransform(
          window.devicePixelRatio,
          0,
          0,
          window.devicePixelRatio,
          0,
          0
        );
      }
    };
    window.addEventListener("resize", resize);
    resize();

    const cursors: Cursor[] = [];
    let targets: TargetItem[] = [];
    let activeTasks: TargetItem[] = [];
    let completed = 0;
    let speed = initialSpeed;
    let soundOn = enableSound;
    let audioCtx: AudioContext | null = null;
    let phase: "ORBIT" | "BUILD" | "DONE" = "ORBIT";
    let startTime = performance.now();
    let lastTime = performance.now();
    let animId = 0;
    const userPos = { x: W / 2, y: H / 2 };

    const onMouseMove = (e: MouseEvent) => {
      userPos.x = e.clientX;
      userPos.y = e.clientY;
    };
    const onTouchMove = (e: TouchEvent) => {
      if (e.touches && e.touches[0]) {
        userPos.x = e.touches[0].clientX;
        userPos.y = e.touches[0].clientY;
      }
    };
    window.addEventListener("mousemove", onMouseMove);
    window.addEventListener("touchmove", onTouchMove, { passive: true });

    const getAudioContext = () => {
      if (!audioCtx) {
        const AC =
          window.AudioContext ||
          (window as unknown as { webkitAudioContext: typeof AudioContext })
            .webkitAudioContext;
        if (AC) audioCtx = new AC();
      }
      if (audioCtx && audioCtx.state === "suspended") {
        audioCtx.resume().catch(() => {});
      }
      return audioCtx;
    };

    const playTone = (done: boolean) => {
      if (!soundOn) return;
      try {
        const actx = getAudioContext();
        if (!actx || actx.state !== "running") return;
        const now = actx.currentTime;
        const osc = actx.createOscillator();
        const gain = actx.createGain();
        osc.type = done ? "sine" : "triangle";
        osc.frequency.setValueAtTime(done ? 587.33 : 220, now);
        gain.gain.setValueAtTime(done ? 0.025 : 0.012, now);
        gain.gain.exponentialRampToValueAtTime(0.0001, now + (done ? 0.22 : 0.035));
        osc.connect(gain);
        gain.connect(actx.destination);
        osc.start(now);
        osc.stop(now + (done ? 0.24 : 0.04));
      } catch {}
    };

    const updateHud = () => {
      const txt = document.getElementById("swarm-hud-text");
      if (!txt) return;
      if (phase === "DONE") {
        txt.textContent = `● Live · ${targets.length} / ${targets.length} layers`;
      } else {
        txt.textContent = `${NUM_CURSORS} agents · ${completed} / ${targets.length} layers`;
      }
    };

    const scanPageElements = () => {
      const selector =
        "header, nav, h1, h2, h3, p, img, svg, button, a.btn, section > div, article, .card, footer";
      const raw = Array.from(document.querySelectorAll(selector)) as HTMLElement[];
      const filtered = raw.filter((el) => {
        if (hud.contains(el) || el === canvas) return false;
        if (
          el.tagName === "SCRIPT" ||
          el.tagName === "STYLE" ||
          el.tagName === "NOSCRIPT"
        )
          return false;
        if (el.classList.contains("wheel-card") || el.closest("#wheel"))
          return false;

        const styleComputed = window.getComputedStyle(el);
        if (
          styleComputed.display === "none" ||
          styleComputed.visibility === "hidden" ||
          styleComputed.opacity === "0"
        )
          return false;

        const r = el.getBoundingClientRect();
        if (r.width < 24 || r.height < 24) return false;
        if (r.height > H * 1.3 && el.children.length > 2) return false;

        return true;
      });

      filtered.sort((a, b) => {
        const ra = a.getBoundingClientRect();
        const rb = b.getBoundingClientRect();
        return ra.top + window.scrollY - (rb.top + window.scrollY);
      });

      const capped = filtered.slice(0, maxTargets);
      capped.forEach((el) => {
        el.setAttribute("data-swarm-item", "true");
        el.classList.remove("swarm-built", "swarm-building");
      });

      targets = capped.map((el, i) => ({
        id: i,
        el,
        tag: `<${el.tagName.toLowerCase()}>`,
        started: false,
        completed: false,
        progress: 0,
        duration: 720 + (i % 3) * 160,
        cursors: [],
      }));

      completed = 0;
      activeTasks = [];
      updateHud();
    };

    const initCursors = () => {
      cursors.length = 0;
      for (let i = 0; i < NUM_CURSORS; i++) {
        const ang = (i / NUM_CURSORS) * Math.PI * 2;
        const rad = 50 + (i % 3) * 25;
        cursors.push({
          id: i,
          x: W / 2 + Math.cos(ang) * rad,
          y: H / 2 + Math.sin(ang) * rad,
          angle: ang,
          orbitRadius: rad,
          orbitSpeed: (0.024 + (i % 5) * 0.005) * (i % 2 === 0 ? 1 : -1),
          color: PALETTE[i % PALETTE.length],
          role: ROLES[i % ROLES.length],
          tx: W / 2,
          ty: H / 2,
          busy: false,
        });
      }
    };

    const drawCursor = (c: Cursor) => {
      if (!ctx) return;
      ctx.save();
      ctx.translate(c.x, c.y);

      ctx.shadowColor = "rgba(0, 0, 0, 0.35)";
      ctx.shadowBlur = 4;
      ctx.shadowOffsetX = 1;
      ctx.shadowOffsetY = 2;

      ctx.beginPath();
      ctx.moveTo(0, 0);
      ctx.lineTo(0, 14);
      ctx.lineTo(3.8, 10.5);
      ctx.lineTo(9.5, 10.5);
      ctx.closePath();

      ctx.fillStyle = c.color;
      ctx.fill();

      ctx.shadowColor = "transparent";
      ctx.lineWidth = 1.1;
      ctx.strokeStyle = "#ffffff";
      ctx.stroke();

      if (c.role && (c.busy || c.id % 3 === 0 || phase === "ORBIT")) {
        ctx.font =
          "600 9px ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
        const tw = ctx.measureText(c.role).width;
        const bw = tw + 8;
        const bh = 14;

        const bx = 8;
        const by = 11;
        const br = 3;
        ctx.fillStyle = c.color;
        ctx.beginPath();
        ctx.moveTo(bx + br, by);
        ctx.lineTo(bx + bw - br, by);
        ctx.quadraticCurveTo(bx + bw, by, bx + bw, by + br);
        ctx.lineTo(bx + bw, by + bh - br);
        ctx.quadraticCurveTo(bx + bw, by + bh, bx + bw - br, by + bh);
        ctx.lineTo(bx + br, by + bh);
        ctx.quadraticCurveTo(bx, by + bh, bx, by + bh - br);
        ctx.lineTo(bx, by + br);
        ctx.quadraticCurveTo(bx, by, bx + br, by);
        ctx.closePath();
        ctx.fill();

        ctx.fillStyle = "#ffffff";
        ctx.fillText(c.role, bx + 4, by + 10.5);
      }
      ctx.restore();
    };

    const drawTask = (t: TargetItem) => {
      if (!ctx) return;
      const r = t.el.getBoundingClientRect();
      const ease = 1 - Math.pow(1 - Math.min(1, t.progress), 3);
      const cw = Math.max(14, r.width * ease);
      const ch = Math.max(14, r.height * ease);
      const x = r.left;
      const y = r.top;

      ctx.save();

      // Red Laser Guides
      ctx.strokeStyle = "rgba(244, 63, 94, 0.65)";
      ctx.lineWidth = 1;
      ctx.beginPath();
      ctx.moveTo(x, 0); ctx.lineTo(x, H);
      ctx.moveTo(x + cw, 0); ctx.lineTo(x + cw, H);
      ctx.moveTo(0, y); ctx.lineTo(W, y);
      ctx.moveTo(0, y + ch); ctx.lineTo(W, y + ch);
      ctx.stroke();

      // Figma Blue Box
      ctx.fillStyle = "rgba(37, 99, 235, 0.08)";
      ctx.fillRect(x, y, cw, ch);
      ctx.strokeStyle = "#3b82f6";
      ctx.lineWidth = 1.4;
      ctx.strokeRect(x, y, cw, ch);

      // 4 Handles
      const handleSize = 5;
      const half = handleSize / 2;
      const handles = [
        [x, y],
        [x + cw, y],
        [x, y + ch],
        [x + cw, y + ch],
      ];
      ctx.fillStyle = "#ffffff";
      ctx.strokeStyle = "#3b82f6";
      ctx.lineWidth = 1;
      for (let h = 0; h < handles.length; h++) {
        const [hx, hy] = handles[h];
        ctx.fillRect(hx - half, hy - half, handleSize, handleSize);
        ctx.strokeRect(hx - half, hy - half, handleSize, handleSize);
      }

      // Tag Label
      const label = `${t.tag} · ${Math.round(cw)}×${Math.round(ch)}`;
      ctx.font =
        "600 10px ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
      const bw = ctx.measureText(label).width + 10;
      const bx = Math.max(4, Math.min(x, W - bw - 6));
      const by = y > 22 ? y - 18 : Math.min(H - 20, y + 4);

      ctx.fillStyle = "#2563eb";
      ctx.fillRect(bx, by, bw, 15);
      ctx.fillStyle = "#ffffff";
      ctx.fillText(label, bx + 5, by + 11);

      ctx.restore();
    };

    const loop = (now: number) => {
      if (!ctx) return;
      const dt = Math.min(60, now - lastTime) * speed;
      lastTime = now;
      ctx.clearRect(0, 0, W, H);

      if (phase === "ORBIT") {
        const elapsed = (now - startTime) * speed;
        cursors.forEach((c, i) => {
          c.angle += c.orbitSpeed * speed;
          const p = Math.sin(elapsed * 0.005 + i) * 14;
          c.x += (W / 2 + Math.cos(c.angle) * (c.orbitRadius + p) - c.x) * 0.18;
          c.y += (H / 2 + Math.sin(c.angle) * (c.orbitRadius + p) - c.y) * 0.18;
          drawCursor(c);
        });
        if (elapsed > 1200) {
          phase = "BUILD";
          updateHud();
        }
        animId = requestAnimationFrame(loop);
        return;
      }

      if (phase === "BUILD") {
        while (activeTasks.length < 3) {
          const next = targets.find((t) => !t.started && !t.completed);
          if (!next) break;
          next.started = true;
          next.el.classList.add("swarm-building");

          const available = cursors.filter((c) => !c.busy).slice(0, 5);
          available.forEach((c) => {
            c.busy = true;
            next.cursors.push(c);
          });
          activeTasks.push(next);
          playTone(false);

          const r = next.el.getBoundingClientRect();
          if (r.bottom > H - 80 || r.top < 60) {
            window.scrollTo({
              top: Math.max(0, window.scrollY + r.top - H * 0.32),
              behavior: "smooth",
            });
          }
        }

        for (let i = activeTasks.length - 1; i >= 0; i--) {
          const t = activeTasks[i];
          t.progress += dt / t.duration;
          const r = t.el.getBoundingClientRect();
          const ease = 1 - Math.pow(1 - Math.min(1, t.progress), 3);
          const cw = Math.max(14, r.width * ease);
          const ch = Math.max(14, r.height * ease);

          t.cursors.forEach((c, idx) => {
            if (idx === 0) { c.tx = r.left; c.ty = r.top; }
            else if (idx === 1) { c.tx = r.left + cw; c.ty = r.top; }
            else if (idx === 2) { c.tx = r.left + cw; c.ty = r.top + ch; }
            else if (idx === 3) { c.tx = r.left; c.ty = r.top + ch; }
            else { c.tx = r.left + cw; c.ty = r.top + ch * 0.5; }
          });

          drawTask(t);

          if (t.progress >= 1) {
            t.completed = true;
            t.el.classList.remove("swarm-building");
            t.el.classList.add("swarm-built");
            t.el.removeAttribute("data-swarm-item");
            t.cursors.forEach((c) => (c.busy = false));
            activeTasks.splice(i, 1);
            completed++;
            updateHud();
            playTone(true);
          }
        }

        cursors.forEach((c, i) => {
          if (c.busy) {
            c.x += (c.tx - c.x) * 0.28;
            c.y += (c.ty - c.y) * 0.28;
          } else {
            const sx = W / 2 + Math.cos(now * 0.002 + i) * (W < 640 ? 85 : 180);
            const sy = H / 2 + Math.sin(now * 0.0025 + i) * 85;
            c.x += (sx - c.x) * 0.05;
            c.y += (sy - c.y) * 0.05;
          }
          drawCursor(c);
        });

        if (completed >= targets.length && activeTasks.length === 0) {
          phase = "DONE";
          updateHud();
        }
        animId = requestAnimationFrame(loop);
        return;
      }

      if (phase === "DONE") {
        const maxR = W < 640 ? 85 : 180;
        cursors.forEach((c, i) => {
          const ang =
            now * 0.0012 * (i % 2 === 0 ? 1 : -1) +
            (i * Math.PI * 2) / NUM_CURSORS;
          const rad = 28 + (i % 6) * (maxR / 6);
          c.x += (userPos.x + Math.cos(ang) * rad - c.x) * 0.06;
          c.y += (userPos.y + Math.sin(ang) * rad - c.y) * 0.06;
          drawCursor(c);
        });
        animId = requestAnimationFrame(loop);
      }
    };

    // Bind HUD events
    hud.querySelectorAll("[data-spd]").forEach((btn) => {
      btn.addEventListener("click", () => {
        speed = Number(btn.getAttribute("data-spd"));
        hud.querySelectorAll("[data-spd]").forEach((b) => b.classList.remove("active"));
        btn.classList.add("active");
      });
    });

    const btnSound = document.getElementById("swarm-btn-sound");
    if (btnSound) {
      btnSound.addEventListener("click", () => {
        soundOn = !soundOn;
        btnSound.textContent = soundOn ? "🔊" : "🔇";
        if (soundOn) getAudioContext();
      });
    }

    const btnBp = document.getElementById("swarm-btn-bp");
    if (btnBp) {
      btnBp.addEventListener("click", () => {
        document.body.classList.toggle("swarm-blueprint");
        btnBp.classList.toggle("active");
      });
    }

    const btnSkip = document.getElementById("swarm-btn-skip");
    if (btnSkip) {
      btnSkip.addEventListener("click", () => {
        targets.forEach((t) => {
          t.completed = true;
          t.el.classList.remove("swarm-building");
          t.el.classList.add("swarm-built");
          t.el.removeAttribute("data-swarm-item");
        });
        cursors.forEach((c) => (c.busy = false));
        activeTasks = [];
        completed = targets.length;
        phase = "DONE";
        updateHud();
      });
    }

    const btnReplay = document.getElementById("swarm-btn-replay");
    if (btnReplay) {
      btnReplay.addEventListener("click", () => {
        window.scrollTo({ top: 0, behavior: "smooth" });
        startTime = performance.now();
        phase = "ORBIT";
        initCursors();
        scanPageElements();
      });
    }

    // Start
    initCursors();
    scanPageElements();
    animId = requestAnimationFrame(loop);

    // Cleanup
    return () => {
      cancelAnimationFrame(animId);
      window.removeEventListener("resize", resize);
      window.removeEventListener("mousemove", onMouseMove);
      window.removeEventListener("touchmove", onTouchMove);
      if (style.parentNode) style.parentNode.removeChild(style);
      if (canvas.parentNode) canvas.parentNode.removeChild(canvas);
      if (hud.parentNode) hud.parentNode.removeChild(hud);
      if (audioCtx) {
        audioCtx.close().catch(() => {});
      }
    };
  }, [enabled, initialSpeed, enableSound, maxTargets]);

  return null;
}

export default SwarmRebuild;
