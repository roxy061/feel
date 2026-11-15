using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FigmaCursorApp
{
    // =========================================================================
    // Win32 Native APIs for Layered Window, Real Input & Window Perception
    // =========================================================================
    internal static class NativeMethods
    {
        public const int WS_EX_LAYERED = 0x00080000;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOPMOST = 0x00000008;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;

        public const byte AC_SRC_OVER = 0x00;
        public const byte AC_SRC_ALPHA = 0x01;
        public const int ULW_ALPHA = 0x00000002;

        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        public const int WM_HOTKEY = 0x0312;

        // Mouse Event Flags
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_WHEEL = 0x0800;

        // Window Messages for Background Click
        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_LBUTTONUP = 0x0202;
        public const uint WM_LBUTTONDBLCLK = 0x0203;
        public const uint WM_RBUTTONDOWN = 0x0204;
        public const uint WM_RBUTTONUP = 0x0205;
        public const uint WM_MOUSEWHEEL = 0x020A;
        public const int MK_LBUTTON = 0x0001;
        public const int MK_RBUTTON = 0x0002;

        public const int VK_LBUTTON = 0x01;
        public const int VK_RBUTTON = 0x02;

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public int x;
            public int y;
            public Point(int x, int y) { this.x = x; this.y = y; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Size
        {
            public int cx;
            public int cy;
            public Size(int cx, int cy) { this.cx = cx; this.cy = cy; }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool UpdateLayeredWindow(
            IntPtr hwnd,
            IntPtr hdcDst,
            ref Point pptDst,
            ref Size psize,
            IntPtr hdcSrc,
            ref Point pprSrc,
            int crKey,
            ref BLENDFUNCTION pblend,
            int dwFlags);

        [DllImport("user32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll", ExactSpelling = true, SetLastError = true)]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int X,
            int Y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(Point Point);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        public static IntPtr MakeLParam(int loWord, int hiWord)
        {
            return (IntPtr)((hiWord << 16) | (loWord & 0xFFFF));
        }
    }

    // =========================================================================
    // Data Models, AI Personas & Action Types
    // =========================================================================
    public enum CursorMovementMode
    {
        HumanAIPlayer = 0,  // 🧠 AI เล่นแทนเหมือนคนจริงๆ (Smart Human-Like AI Player)
        Waypoints = 1,      // 📍 วิ่งและคลิกตามลำดับจุด Waypoints / เส้นทางที่อัดไว้
        FollowMouse = 2,    // 🧲 ติดตามเมาส์หลักของผู้ใช้
        StaticPin = 3       // 📌 Auto-Clicker ปักหมุดคลิกซ้ำที่จุดเดิม
    }

    public enum HumanPlayStyle
    {
        NaturalUser = 0,    // 🧑‍💻 คนใช้งานทั่วไป (กวาดสายตาอ่านเว็บ เลื่อนจอ คลิกสำรวจปุ่มอย่างเป็นธรรมชาติ)
        ActiveGamer = 1,    // 🎮 คนเล่นเกม / คลิกไว (สะบัดเมาส์ไว เล็งเป้าละเอียด คลิกและตอบสนองต่อเนื่อง)
        FigmaDesigner = 2   // 🎨 นักออกแบบ UI (เลื่อนเช็คจุดต่างๆ ลากคลุม คอมเมนต์งานเหมือนเพื่อนในทีม)
    }

    public enum HumanBrainPhase
    {
        BallisticFlick = 0,     // จังหวะสะบัดเมาส์หลัก (85-95% ของระยะทางด้วย Minimum-Jerk Curve)
        MicroCorrection = 1,    // จังหวะขยับเล็งแก้ระยะสั้นๆ ก่อนถึงปุ่มจริง (Overshoot / Fine-tune)
        HoverReaction = 2,      // จังหวะสายตามองยืนยันก่อนนิ้วกดคลิก (120-280ms + มือสั่นนิดๆ)
        HoldingClick = 3,       // จังหวะนิ้วกดปุ่มเมาส์ค้างไว้ (65-120ms เหมือนนิ้วคนกดสวิตช์เมาส์จริง)
        ReadingAndScrolling = 4,// จังหวะไล่เมาส์อ่านเนื้อหาพร้อมหมุนลูกกลิ้งเมาส์เป็นจังหวะ
        ThinkingIdle = 5        // จังหวะหยุดคิด / ขยับเมาส์วนเบาๆ (Micro-drift / Wiggle)
    }

    public enum WaypointActionType
    {
        LeftClick = 0,
        DoubleClick = 1,
        RightClick = 2,
        ScrollDown = 3,
        ScrollUp = 4,
        ClickAndType = 5,
        MoveOnly = 6
    }

    public enum ClickEngineMode
    {
        HardwareInstantRestore = 0, // คลิกจริงทุกโปรแกรม + คืนตำแหน่งเมาส์หลักทันที
        HardwareMoveCursor = 1,     // คลิกจริงโดยย้ายเมาส์หลักไปที่จุดนั้นด้วย
        BackgroundMessageOnly = 2,  // ส่งคำสั่งคลิกเบื้องหลัง (WM_LBUTTONDOWN)
        VisualOnly = 3              // แสดงเฉพาะเอฟเฟกต์วงคลื่น (ไม่คลิกจริง)
    }

    public class WaypointItem
    {
        public int X;
        public int Y;
        public int WaitMs;
        public WaypointActionType Action;
        public string TextToType;
        public string Note;

        public WaypointItem(int x, int y, int waitMs, WaypointActionType action, string textToType, string note)
        {
            X = x;
            Y = y;
            WaitMs = waitMs;
            Action = action;
            TextToType = textToType ?? "";
            Note = note ?? "";
        }

        public static string GetActionLabel(WaypointActionType act)
        {
            switch (act)
            {
                case WaypointActionType.LeftClick: return "🖱 คลิกซ้าย";
                case WaypointActionType.DoubleClick: return "⚡ ดับเบิลคลิก";
                case WaypointActionType.RightClick: return "🖱 คลิกขวา";
                case WaypointActionType.ScrollDown: return "⬇ เลื่อนจอลง";
                case WaypointActionType.ScrollUp: return "⬆ เลื่อนจอขึ้น";
                case WaypointActionType.ClickAndType: return "⌨ คลิก+พิมพ์ข้อความ";
                case WaypointActionType.MoveOnly: return "➖ ชี้เฉยๆ";
                default: return act.ToString();
            }
        }
    }

    // =========================================================================
    // Transparent Click-Through Overlay Window with Human-Like AI Brain
    // =========================================================================
    public class FigmaCursorOverlay : Form
    {
        public const int TIP_OFFSET_X = 28;
        public const int TIP_OFFSET_Y = 28;
        private const int OVERLAY_WIDTH = 385;
        private const int OVERLAY_HEIGHT = 135;

        public string CursorName;
        public string ChatMessage;
        public Color ThemeColor;
        public CursorMovementMode Mode;
        public HumanPlayStyle PlayStyle;
        public ClickEngineMode ClickEngine;
        public double SpeedMultiplier;
        public bool IsPaused;
        public bool AutoChatMessages;
        public Form ProtectedControlPanel;

        // Current Position (Screen Coordinates of the Cursor Tip)
        public double CurrentX;
        public double CurrentY;

        // Human AI Brain State
        private HumanBrainPhase brainPhase = HumanBrainPhase.ThinkingIdle;
        private double startX, startY;
        private double ctrl1X, ctrl1Y;
        private double ctrl2X, ctrl2Y;
        private double subTargetX, subTargetY; // Initial ballistic landing point (slight overshoot/undershoot)
        private double finalTargetX, finalTargetY; // True target point
        private bool needsMicroCorrection = false;
        private double moveProgress = 1.0;
        private double moveDurationSec = 0.6;
        private double phaseTimerSec = 0.2;

        // Cluster / Attention Anchor (humans do 3-5 actions around an area of interest before jumping far)
        private double clusterCenterX, clusterCenterY;
        private int clusterActionsLeft = 0;

        // Scrolling bursts state
        private int scrollTicksRemaining = 0;
        private int scrollDirection = -1; // -1 = down, +1 = up
        private double scrollCooldownSec = 0;

        // Continuous organic hand tremor phase
        private double tremorTime = 0;

        // Static Pin auto-click interval
        public int PinClickIntervalMs = 1000;
        private int waitTicksRemaining = 0;

        // Waypoints list
        public List<WaypointItem> Waypoints = new List<WaypointItem>();
        public int CurrentWaypointIndex = 0;

        // Follow mode velocity
        private double velX = 0, velY = 0;

        // Click ripple animation
        private float rippleProgress = 0f;
        private Color rippleColor = Color.White;
        private bool isPressedVisual = false;
        private bool bitmapDirty = true;

        // Stats
        public int TotalClicksPerformed = 0;
        public string LastActionDescription = "กำลังคิดและสำรวจหน้าจอ...";

        private readonly Random rng;
        private int autoChatCooldown = 140;

        private static readonly string[] NaturalUserChats = new string[]
        {
            "อืม... ดูตรงนี้หน่อย 🤔",
            "เลื่อนดูข้างล่างแป๊บ",
            "กดตรงนี้เลยละกัน ✨",
            "หน้านี้เนื้อหาใช้ได้นะ",
            "เช็คเมนูนี้หน่อย 👀",
            "โอเค เรียบร้อย 👍"
        };

        private static readonly string[] GamerChats = new string[]
        {
            "เล็งเป้า... คลิก! ⚡",
            "จังหวะนี้แหละ! 🔥",
            "ฟาร์มรัวๆ เลย 🕹️",
            "ไวจัด! 🚀",
            "เคลียร์พื้นที่แป๊บ",
            "GG EZ! ✨"
        };

        private static readonly string[] DesignerChats = new string[]
        {
            "เช็คระยะ Margin นิดนึง 📐",
            "สีปุ่มตรงนี้สวยแล้ว ✨",
            "ปรับ Alignment เรียบร้อย",
            "ลองกดดู Flow หน้านี้หน่อย",
            "LGTM! ดีไซน์ลงตัว 👍",
            "เลื่อนดูส่วน Footer หน่อย"
        };

        public FigmaCursorOverlay(int seed, string name, Color color, double initX, double initY, Form controlPanel)
        {
            rng = new Random(seed);
            CursorName = name;
            ChatMessage = "พร้อมเล่นแทนแล้ว ✨";
            ThemeColor = color;
            Mode = CursorMovementMode.HumanAIPlayer;
            PlayStyle = HumanPlayStyle.NaturalUser;
            ClickEngine = ClickEngineMode.HardwareInstantRestore;
            SpeedMultiplier = 1.0;
            IsPaused = false;
            AutoChatMessages = true;
            ProtectedControlPanel = controlPanel;

            CurrentX = initX;
            CurrentY = initY;
            clusterCenterX = initX;
            clusterCenterY = initY;
            finalTargetX = initX;
            finalTargetY = initY;
            tremorTime = rng.NextDouble() * 100.0;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(OVERLAY_WIDTH, OVERLAY_HEIGHT);
            Location = new Point((int)CurrentX - TIP_OFFSET_X, (int)CurrentY - TIP_OFFSET_Y);
            TopMost = true;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= NativeMethods.WS_EX_LAYERED
                            | NativeMethods.WS_EX_TRANSPARENT
                            | NativeMethods.WS_EX_TOPMOST
                            | NativeMethods.WS_EX_TOOLWINDOW
                            | NativeMethods.WS_EX_NOACTIVATE;
                return cp;
            }
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }

        public void MarkDirty()
        {
            bitmapDirty = true;
        }

        // =====================================================================
        // Safety Guard: Prevent AI from clicking Window Close [X] or Control Panel
        // =====================================================================
        private bool IsPointSafeForAutonomousClick(int x, int y)
        {
            Rectangle workArea = Screen.PrimaryScreen.WorkingArea;
            // Avoid clicking taskbar or outer 40px screen edges (where top-right window X button lives)
            if (x < workArea.Left + 35 || x > workArea.Right - 65) return false;
            if (y < workArea.Top + 58 || y > workArea.Bottom - 35) return false;

            // Never click inside our own Figma Cursor Studio control panel
            if (ProtectedControlPanel != null && !ProtectedControlPanel.IsDisposed && ProtectedControlPanel.Visible &&
                ProtectedControlPanel.WindowState != FormWindowState.Minimized)
            {
                Rectangle panelBounds = ProtectedControlPanel.Bounds;
                panelBounds.Inflate(16, 16);
                if (panelBounds.Contains(x, y)) return false;
            }

            // Also check if the foreground window has its top-right close/minimize/maximize buttons near (x,y)
            IntPtr fgWnd = NativeMethods.GetForegroundWindow();
            if (fgWnd != IntPtr.Zero)
            {
                NativeMethods.RECT r;
                if (NativeMethods.GetWindowRect(fgWnd, out r))
                {
                    // Avoid the top 55px title bar of the active window so we never close/minimize/drag it accidentally
                    if (y >= r.Top - 5 && y <= r.Top + 55 && x >= r.Left && x <= r.Right)
                        return false;
                }
            }

            return true;
        }

        // Finds the active application's interactive client region so AI focuses on real windows
        private Rectangle GetActivePlayableRegion()
        {
            Rectangle screenArea = Screen.PrimaryScreen.WorkingArea;
            Rectangle defaultSafe = new Rectangle(
                screenArea.Left + 70,
                screenArea.Top + 80,
                Math.Max(200, screenArea.Width - 150),
                Math.Max(200, screenArea.Height - 140));

            IntPtr fgWnd = NativeMethods.GetForegroundWindow();
            if (fgWnd != IntPtr.Zero && (ProtectedControlPanel == null || fgWnd != ProtectedControlPanel.Handle))
            {
                NativeMethods.RECT r;
                if (NativeMethods.GetWindowRect(fgWnd, out r))
                {
                    int w = r.Right - r.Left;
                    int h = r.Bottom - r.Top;
                    if (w >= 280 && h >= 220)
                    {
                        Rectangle winSafe = new Rectangle(
                            Math.Max(screenArea.Left + 50, r.Left + 45),
                            Math.Max(screenArea.Top + 65, r.Top + 70), // Below title bar
                            Math.Min(screenArea.Width - 100, w - 90),
                            Math.Min(screenArea.Height - 120, h - 100));
                        if (winSafe.Width > 120 && winSafe.Height > 100)
                            return winSafe;
                    }
                }
            }
            return defaultSafe;
        }

        // =====================================================================
        // Human-Like AI Brain & Biomechanical Movement Engine
        // =====================================================================
        private void UpdateHumanAIPlayer(double dtSeconds)
        {
            tremorTime += dtSeconds;

            switch (brainPhase)
            {
                case HumanBrainPhase.BallisticFlick:
                {
                    moveProgress += dtSeconds / moveDurationSec;
                    if (moveProgress >= 1.0)
                    {
                        moveProgress = 1.0;
                        CurrentX = subTargetX;
                        CurrentY = subTargetY;

                        if (needsMicroCorrection)
                        {
                            // Transition to short corrective sub-movement (like a real human hand adjusting onto a button)
                            startX = CurrentX;
                            startY = CurrentY;
                            subTargetX = finalTargetX;
                            subTargetY = finalTargetY;
                            ctrl1X = startX + (finalTargetX - startX) * 0.35;
                            ctrl1Y = startY + (finalTargetY - startY) * 0.35;
                            ctrl2X = startX + (finalTargetX - startX) * 0.70;
                            ctrl2Y = startY + (finalTargetY - startY) * 0.70;

                            moveDurationSec = (0.09 + rng.NextDouble() * 0.08) / Math.Max(0.4, SpeedMultiplier);
                            moveProgress = 0.0;
                            needsMicroCorrection = false;
                            brainPhase = HumanBrainPhase.MicroCorrection;
                        }
                        else
                        {
                            EnterHoverBeforeAction();
                        }
                    }
                    else
                    {
                        // Human Minimum-Jerk Trajectory with slight initial surge
                        double s = HumanMinimumJerk(moveProgress);
                        double bx, by;
                        EvaluateCubicBezier(s, out bx, out by);

                        // Subtle physiological hand tremor proportional to movement
                        double tremorAmp = (1.0 - moveProgress) * 0.9;
                        CurrentX = bx + Math.Sin(tremorTime * 19.0) * tremorAmp;
                        CurrentY = by + Math.Cos(tremorTime * 23.0) * tremorAmp;
                    }
                    break;
                }

                case HumanBrainPhase.MicroCorrection:
                {
                    moveProgress += dtSeconds / moveDurationSec;
                    if (moveProgress >= 1.0)
                    {
                        moveProgress = 1.0;
                        CurrentX = finalTargetX;
                        CurrentY = finalTargetY;
                        EnterHoverBeforeAction();
                    }
                    else
                    {
                        double s = HumanMinimumJerk(moveProgress);
                        double bx, by;
                        EvaluateCubicBezier(s, out bx, out by);
                        CurrentX = bx;
                        CurrentY = by;
                    }
                    break;
                }

                case HumanBrainPhase.HoverReaction:
                {
                    // Humans pause 110-280ms while eyes verify the target before finger clicks
                    phaseTimerSec -= dtSeconds;
                    // Tiny natural hand settle drift (0.3px)
                    CurrentX += Math.Sin(tremorTime * 11.0) * 0.12;
                    CurrentY += Math.Cos(tremorTime * 14.0) * 0.12;

                    if (phaseTimerSec <= 0)
                    {
                        double roll = rng.NextDouble();
                        if (PlayStyle == HumanPlayStyle.NaturalUser && roll < 0.25)
                        {
                            // Start reading & scrolling the page naturally!
                            StartHumanReadingAndScrolling();
                        }
                        else if (roll < 0.82 || PlayStyle == HumanPlayStyle.ActiveGamer)
                        {
                            // Press mouse button down and hold for realistic 65-115ms!
                            StartHumanRealisticClickPress();
                        }
                        else
                        {
                            // Just inspected/hovered without clicking, move on after short pause
                            brainPhase = HumanBrainPhase.ThinkingIdle;
                            phaseTimerSec = (0.18 + rng.NextDouble() * 0.35) / Math.Max(0.4, SpeedMultiplier);
                        }
                    }
                    break;
                }

                case HumanBrainPhase.HoldingClick:
                {
                    phaseTimerSec -= dtSeconds;
                    if (phaseTimerSec <= 0)
                    {
                        // Release mouse button!
                        CompleteHumanRealisticClickRelease();

                        // Decide how long to pause after clicking (humans wait to see UI response!)
                        if (PlayStyle == HumanPlayStyle.ActiveGamer)
                        {
                            phaseTimerSec = (0.12 + rng.NextDouble() * 0.28) / Math.Max(0.4, SpeedMultiplier);
                        }
                        else
                        {
                            phaseTimerSec = (0.35 + rng.NextDouble() * 0.85) / Math.Max(0.4, SpeedMultiplier);
                        }
                        brainPhase = HumanBrainPhase.ThinkingIdle;
                    }
                    break;
                }

                case HumanBrainPhase.ReadingAndScrolling:
                {
                    phaseTimerSec -= dtSeconds;
                    scrollCooldownSec -= dtSeconds;

                    // Slowly glide cursor as if eyes are tracking lines of text
                    CurrentX += Math.Sin(tremorTime * 2.8) * 0.65;
                    CurrentY += (scrollDirection < 0 ? 0.35 : -0.25);

                    if (scrollCooldownSec <= 0 && scrollTicksRemaining > 0)
                    {
                        scrollTicksRemaining--;
                        scrollCooldownSec = 0.08 + rng.NextDouble() * 0.09; // Natural finger scroll wheel rhythm
                        ExecuteScrollTick(scrollDirection * 120);
                    }

                    if (phaseTimerSec <= 0 || scrollTicksRemaining <= 0)
                    {
                        brainPhase = HumanBrainPhase.ThinkingIdle;
                        phaseTimerSec = 0.35 + rng.NextDouble() * 0.55;
                    }
                    break;
                }

                case HumanBrainPhase.ThinkingIdle:
                default:
                {
                    phaseTimerSec -= dtSeconds;
                    // Subtle idle breathing / micro-movement of hand on mouse
                    CurrentX += Math.Sin(tremorTime * 5.2) * 0.22;
                    CurrentY += Math.Cos(tremorTime * 4.1) * 0.18;

                    if (phaseTimerSec <= 0)
                    {
                        PlanNextHumanMove();
                    }
                    break;
                }
            }
        }

        private void EnterHoverBeforeAction()
        {
            brainPhase = HumanBrainPhase.HoverReaction;
            if (PlayStyle == HumanPlayStyle.ActiveGamer)
            {
                phaseTimerSec = (0.06 + rng.NextDouble() * 0.10) / Math.Max(0.5, SpeedMultiplier);
            }
            else
            {
                phaseTimerSec = (0.13 + rng.NextDouble() * 0.20) / Math.Max(0.5, SpeedMultiplier);
            }
        }

        private void PlanNextHumanMove()
        {
            Rectangle region = GetActivePlayableRegion();
            startX = CurrentX;
            startY = CurrentY;

            // Humans work in clusters: 3-5 actions within a ~240px neighborhood before shifting focus
            if (clusterActionsLeft <= 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    double cx = region.Left + rng.NextDouble() * region.Width;
                    double cy = region.Top + rng.NextDouble() * region.Height;
                    if (IsPointSafeForAutonomousClick((int)cx, (int)cy))
                    {
                        clusterCenterX = cx;
                        clusterCenterY = cy;
                        break;
                    }
                }
                clusterActionsLeft = rng.Next(2, 6);
            }

            clusterActionsLeft--;

            // Pick target around current cluster center
            double radiusX = PlayStyle == HumanPlayStyle.ActiveGamer ? 260.0 : 190.0;
            double radiusY = PlayStyle == HumanPlayStyle.ActiveGamer ? 200.0 : 150.0;

            for (int attempt = 0; attempt < 15; attempt++)
            {
                double tx = clusterCenterX + (rng.NextDouble() - 0.5) * 2.0 * radiusX;
                double ty = clusterCenterY + (rng.NextDouble() - 0.5) * 2.0 * radiusY;

                // Clamp inside playable region
                tx = Math.Max(region.Left + 20, Math.Min(region.Right - 20, tx));
                ty = Math.Max(region.Top + 20, Math.Min(region.Bottom - 20, ty));

                if (IsPointSafeForAutonomousClick((int)tx, (int)ty))
                {
                    finalTargetX = tx;
                    finalTargetY = ty;
                    break;
                }
            }

            double dx = finalTargetX - startX;
            double dy = finalTargetY - startY;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // If distance is > 180px, simulate human ballistic flick + slight overshoot/undershoot correction (Fitts's Law)
            if (dist > 180.0 && rng.NextDouble() < 0.72)
            {
                needsMicroCorrection = true;
                // Land 10-24px slightly past or beside the target on the first fast flick
                double overshootFactor = 0.92 + rng.NextDouble() * 0.14;
                double perpAngle = Math.Atan2(dy, dx) + Math.PI / 2.0;
                double lateralError = (rng.NextDouble() - 0.5) * 22.0;

                subTargetX = startX + dx * overshootFactor + Math.Cos(perpAngle) * lateralError;
                subTargetY = startY + dy * overshootFactor + Math.Sin(perpAngle) * lateralError;
            }
            else
            {
                needsMicroCorrection = false;
                subTargetX = finalTargetX;
                subTargetY = finalTargetY;
            }

            // Natural human wrist arc (human wrists pivot from the forearm, creating a gentle arc)
            double arcMagnitude = Math.Min(110.0, dist * 0.18) * (rng.NextDouble() - 0.45);
            double normX = dist > 1 ? -dy / dist : 0;
            double normY = dist > 1 ? dx / dist : 0;

            ctrl1X = startX + (subTargetX - startX) * 0.28 + normX * arcMagnitude;
            ctrl1Y = startY + (subTargetY - startY) * 0.28 + normY * arcMagnitude;
            ctrl2X = startX + (subTargetX - startX) * 0.72 + normX * arcMagnitude * 0.6;
            ctrl2Y = startY + (subTargetY - startY) * 0.72 + normY * arcMagnitude * 0.6;

            // Fitts's Law duration: MT = a + b * log2(D / W + 1)
            double fittsTime = 0.16 + 0.115 * Math.Log(dist / 24.0 + 1.0, 2.0);
            if (PlayStyle == HumanPlayStyle.ActiveGamer) fittsTime *= 0.68;

            moveDurationSec = Math.Max(0.12, fittsTime / Math.Max(0.3, SpeedMultiplier));
            moveProgress = 0.0;
            brainPhase = HumanBrainPhase.BallisticFlick;
            LastActionDescription = string.Format("กำลังเลื่อนเมาส์ไปที่ ({0}, {1})...", (int)finalTargetX, (int)finalTargetY);
        }

        private void StartHumanRealisticClickPress()
        {
            int clickX = (int)Math.Round(CurrentX);
            int clickY = (int)Math.Round(CurrentY);

            rippleProgress = 0.02f;
            rippleColor = ThemeColor;
            isPressedVisual = true;
            bitmapDirty = true;

            if (ClickEngine != ClickEngineMode.VisualOnly && IsPointSafeForAutonomousClick(clickX, clickY))
            {
                if (ClickEngine == ClickEngineMode.BackgroundMessageOnly)
                {
                    ExecuteBackgroundWindowClick(clickX, clickY, WaypointActionType.LeftClick);
                }
                else
                {
                    Point savedUserPos = Cursor.Position;
                    NativeMethods.SetCursorPos(clickX, clickY);
                    NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, clickX, clickY, 0, 0);
                    NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, clickX, clickY, 0, 0);

                    if (ClickEngine == ClickEngineMode.HardwareInstantRestore)
                    {
                        NativeMethods.SetCursorPos(savedUserPos.X, savedUserPos.Y);
                    }
                }
                TotalClicksPerformed++;
                LastActionDescription = string.Format("คลิกเหมือนคนจริงที่ ({0}, {1}) [รวม {2} ครั้ง]", clickX, clickY, TotalClicksPerformed);
            }
            else
            {
                LastActionDescription = string.Format("สำรวจจุด ({0}, {1})", clickX, clickY);
            }

            brainPhase = HumanBrainPhase.HoldingClick;
            phaseTimerSec = 0.075 + rng.NextDouble() * 0.045; // 75-120ms human click hold visual
        }

        private void CompleteHumanRealisticClickRelease()
        {
            isPressedVisual = false;
            bitmapDirty = true;
        }

        private void StartHumanReadingAndScrolling()
        {
            brainPhase = HumanBrainPhase.ReadingAndScrolling;
            scrollTicksRemaining = rng.Next(3, 7);
            scrollDirection = rng.NextDouble() < 0.72 ? -1 : 1; // Mostly scroll down, sometimes scroll back up
            scrollCooldownSec = 0.05;
            phaseTimerSec = 0.65 + rng.NextDouble() * 0.75;
            LastActionDescription = scrollDirection < 0
                ? "กำลังเลื่อนหน้าจอลงเพื่ออ่านเนื้อหา..."
                : "กำลังเลื่อนหน้าจอขึ้น...";
        }

        private void ExecuteScrollTick(int wheelDelta)
        {
            int sx = (int)Math.Round(CurrentX);
            int sy = (int)Math.Round(CurrentY);

            if (ClickEngine == ClickEngineMode.VisualOnly || !IsPointSafeForAutonomousClick(sx, sy))
                return;

            if (ClickEngine == ClickEngineMode.BackgroundMessageOnly)
            {
                ExecuteBackgroundWindowClick(sx, sy, wheelDelta < 0 ? WaypointActionType.ScrollDown : WaypointActionType.ScrollUp);
            }
            else
            {
                Point savedPos = Cursor.Position;
                NativeMethods.SetCursorPos(sx, sy);
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_WHEEL, sx, sy, wheelDelta, 0);
                if (ClickEngine == ClickEngineMode.HardwareInstantRestore)
                {
                    NativeMethods.SetCursorPos(savedPos.X, savedPos.Y);
                }
            }
        }

        // Minimum-Jerk trajectory with slight human fast-start skew
        private static double HumanMinimumJerk(double t)
        {
            if (t <= 0.0) return 0.0;
            if (t >= 1.0) return 1.0;
            // Warp t slightly (t^0.88) so peak velocity happens slightly before midpoint (~42% of time), matching human EMG studies
            double tw = Math.Pow(t, 0.88);
            double t3 = tw * tw * tw;
            double t4 = t3 * tw;
            double t5 = t4 * tw;
            return 10.0 * t3 - 15.0 * t4 + 6.0 * t5;
        }

        // =====================================================================
        // Execute Real Windows Action for Waypoints / Manual Trigger
        // =====================================================================
        public void ExecuteActionAtCurrentPosition(WaypointActionType action, string textToType, bool isAutonomousBot)
        {
            if (action == WaypointActionType.MoveOnly)
                return;

            int clickX = (int)Math.Round(CurrentX);
            int clickY = (int)Math.Round(CurrentY);

            rippleProgress = 0.02f;
            rippleColor = (action == WaypointActionType.RightClick) ? Color.FromArgb(255, 180, 0) : ThemeColor;
            isPressedVisual = true;
            bitmapDirty = true;

            if (ClickEngine == ClickEngineMode.VisualOnly)
            {
                LastActionDescription = "แสดงเอฟเฟกต์: " + WaypointItem.GetActionLabel(action);
                return;
            }

            if (isAutonomousBot && !IsPointSafeForAutonomousClick(clickX, clickY))
            {
                LastActionDescription = "ข้ามจุดขอบหน้าต่าง (ระบบความปลอดภัย)";
                return;
            }

            try
            {
                if (ClickEngine == ClickEngineMode.BackgroundMessageOnly)
                {
                    ExecuteBackgroundWindowClick(clickX, clickY, action);
                }
                else
                {
                    Point savedUserPos = Cursor.Position;
                    bool restorePos = (ClickEngine == ClickEngineMode.HardwareInstantRestore);

                    NativeMethods.SetCursorPos(clickX, clickY);

                    switch (action)
                    {
                        case WaypointActionType.LeftClick:
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, clickX, clickY, 0, 0);
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, clickX, clickY, 0, 0);
                            break;

                        case WaypointActionType.DoubleClick:
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, clickX, clickY, 0, 0);
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, clickX, clickY, 0, 0);
                            Thread.Sleep(35);
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, clickX, clickY, 0, 0);
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, clickX, clickY, 0, 0);
                            break;

                        case WaypointActionType.RightClick:
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTDOWN, clickX, clickY, 0, 0);
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_RIGHTUP, clickX, clickY, 0, 0);
                            break;

                        case WaypointActionType.ScrollDown:
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_WHEEL, clickX, clickY, -240, 0);
                            break;

                        case WaypointActionType.ScrollUp:
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_WHEEL, clickX, clickY, 240, 0);
                            break;

                        case WaypointActionType.ClickAndType:
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN, clickX, clickY, 0, 0);
                            NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTUP, clickX, clickY, 0, 0);
                            break;
                    }

                    if (restorePos)
                    {
                        NativeMethods.SetCursorPos(savedUserPos.X, savedUserPos.Y);
                    }

                    if (action == WaypointActionType.ClickAndType && !string.IsNullOrEmpty(textToType))
                    {
                        SendKeys.SendWait(textToType);
                    }
                }

                TotalClicksPerformed++;
                LastActionDescription = string.Format("{0} ที่ ({1}, {2}) [รวม {3} ครั้ง]",
                    WaypointItem.GetActionLabel(action), clickX, clickY, TotalClicksPerformed);
            }
            catch (Exception ex)
            {
                LastActionDescription = "ข้อผิดพลาดคลิก: " + ex.Message;
            }
        }

        private void ExecuteBackgroundWindowClick(int screenX, int screenY, WaypointActionType action)
        {
            NativeMethods.Point pt = new NativeMethods.Point(screenX, screenY);
            IntPtr hWnd = NativeMethods.WindowFromPoint(pt);
            if (hWnd == IntPtr.Zero) return;

            NativeMethods.Point clientPt = new NativeMethods.Point(screenX, screenY);
            NativeMethods.ScreenToClient(hWnd, ref clientPt);
            IntPtr lParam = NativeMethods.MakeLParam(clientPt.x, clientPt.y);

            switch (action)
            {
                case WaypointActionType.LeftClick:
                case WaypointActionType.ClickAndType:
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_LBUTTONDOWN, (IntPtr)NativeMethods.MK_LBUTTON, lParam);
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, lParam);
                    break;

                case WaypointActionType.DoubleClick:
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_LBUTTONDOWN, (IntPtr)NativeMethods.MK_LBUTTON, lParam);
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, lParam);
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_LBUTTONDBLCLK, (IntPtr)NativeMethods.MK_LBUTTON, lParam);
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_LBUTTONUP, IntPtr.Zero, lParam);
                    break;

                case WaypointActionType.RightClick:
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_RBUTTONDOWN, (IntPtr)NativeMethods.MK_RBUTTON, lParam);
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_RBUTTONUP, IntPtr.Zero, lParam);
                    break;

                case WaypointActionType.ScrollDown:
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_MOUSEWHEEL, NativeMethods.MakeLParam(0, -120), NativeMethods.MakeLParam(screenX, screenY));
                    break;

                case WaypointActionType.ScrollUp:
                    NativeMethods.PostMessage(hWnd, NativeMethods.WM_MOUSEWHEEL, NativeMethods.MakeLParam(0, 120), NativeMethods.MakeLParam(screenX, screenY));
                    break;
            }
        }

        public void TickUpdate(double dtSeconds)
        {
            if (IsDisposed || !Visible) return;

            // 1. Update ripple animation if active
            if (rippleProgress > 0f)
            {
                rippleProgress += (float)(dtSeconds * 2.3);
                if (rippleProgress > 0.38f && brainPhase != HumanBrainPhase.HoldingClick)
                    isPressedVisual = false;
                if (rippleProgress >= 1.0f)
                {
                    rippleProgress = 0f;
                    isPressedVisual = false;
                }
                bitmapDirty = true;
            }

            // 2. Context-aware human thoughts / chat bubble
            if (AutoChatMessages && !IsPaused)
            {
                autoChatCooldown--;
                if (autoChatCooldown <= 0)
                {
                    string[] pool = PlayStyle == HumanPlayStyle.ActiveGamer ? GamerChats
                                  : PlayStyle == HumanPlayStyle.FigmaDesigner ? DesignerChats
                                  : NaturalUserChats;

                    if (string.IsNullOrEmpty(ChatMessage) && rng.NextDouble() < 0.55)
                    {
                        ChatMessage = pool[rng.Next(pool.Length)];
                        bitmapDirty = true;
                        autoChatCooldown = rng.Next(120, 220);
                    }
                    else if (!string.IsNullOrEmpty(ChatMessage))
                    {
                        ChatMessage = "";
                        bitmapDirty = true;
                        autoChatCooldown = rng.Next(140, 260);
                    }
                    else
                    {
                        autoChatCooldown = rng.Next(80, 160);
                    }
                }
            }

            // 3. Movement & Action execution logic
            if (!IsPaused)
            {
                switch (Mode)
                {
                    case CursorMovementMode.HumanAIPlayer:
                        UpdateHumanAIPlayer(dtSeconds);
                        break;
                    case CursorMovementMode.Waypoints:
                        UpdateWaypointsMovement(dtSeconds);
                        break;
                    case CursorMovementMode.FollowMouse:
                        UpdateFollowMouseMovement(dtSeconds);
                        break;
                    case CursorMovementMode.StaticPin:
                        UpdateStaticPinClicker();
                        break;
                }
            }

            // 4. Render or reposition window
            int winX = (int)Math.Round(CurrentX) - TIP_OFFSET_X;
            int winY = (int)Math.Round(CurrentY) - TIP_OFFSET_Y;

            if (bitmapDirty)
            {
                RenderLayeredBitmap(winX, winY);
                bitmapDirty = false;
            }
            else
            {
                NativeMethods.SetWindowPos(
                    this.Handle,
                    NativeMethods.HWND_TOPMOST,
                    winX,
                    winY,
                    0,
                    0,
                    NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
            }
        }

        private void UpdateWaypointsMovement(double dtSeconds)
        {
            if (Waypoints.Count == 0) return;

            if (waitTicksRemaining > 0)
            {
                waitTicksRemaining--;
                return;
            }

            if (CurrentWaypointIndex < 0 || CurrentWaypointIndex >= Waypoints.Count)
            {
                CurrentWaypointIndex = 0;
            }

            WaypointItem wp = Waypoints[CurrentWaypointIndex];

            if (moveProgress >= 1.0)
            {
                startX = CurrentX;
                startY = CurrentY;
                finalTargetX = wp.X;
                finalTargetY = wp.Y;
                subTargetX = wp.X;
                subTargetY = wp.Y;

                double dist = Math.Sqrt((finalTargetX - startX) * (finalTargetX - startX) + (finalTargetY - startY) * (finalTargetY - startY));

                if (dist < 35 && wp.WaitMs <= 30)
                {
                    ctrl1X = startX + (finalTargetX - startX) * 0.33;
                    ctrl1Y = startY + (finalTargetY - startY) * 0.33;
                    ctrl2X = startX + (finalTargetX - startX) * 0.66;
                    ctrl2Y = startY + (finalTargetY - startY) * 0.66;
                    moveDurationSec = Math.Max(0.015, 0.022 / Math.Max(0.25, SpeedMultiplier));
                }
                else
                {
                    double curveOffset = Math.Min(55.0, dist * 0.14);
                    ctrl1X = startX + (finalTargetX - startX) * 0.33;
                    ctrl1Y = startY + (finalTargetY - startY) * 0.33 - curveOffset * 0.35;
                    ctrl2X = startX + (finalTargetX - startX) * 0.66;
                    ctrl2Y = startY + (finalTargetY - startY) * 0.66 - curveOffset * 0.35;
                    double baseSpeed = Math.Max(0.25, SpeedMultiplier);
                    moveDurationSec = Math.Max(0.08, (dist / 780.0) / baseSpeed);
                }

                moveProgress = 0.0;
            }
            else
            {
                moveProgress += dtSeconds / moveDurationSec;
                if (moveProgress >= 1.0)
                {
                    moveProgress = 1.0;
                    CurrentX = finalTargetX;
                    CurrentY = finalTargetY;

                    ExecuteActionAtCurrentPosition(wp.Action, wp.TextToType, false);

                    waitTicksRemaining = Math.Max(1, (int)Math.Round(wp.WaitMs / 16.0));
                    CurrentWaypointIndex = (CurrentWaypointIndex + 1) % Waypoints.Count;
                }
                else
                {
                    double t = (wp.WaitMs <= 30) ? moveProgress : HumanMinimumJerk(moveProgress);
                    double bx, by;
                    EvaluateCubicBezier(t, out bx, out by);
                    CurrentX = bx;
                    CurrentY = by;
                }
            }
        }

        private void UpdateFollowMouseMovement(double dtSeconds)
        {
            Point mousePos = Cursor.Position;
            double desiredX = mousePos.X + 26;
            double desiredY = mousePos.Y + 22;

            double stiffness = 14.0 * Math.Max(0.3, SpeedMultiplier);
            double damping = 0.78;

            double forceX = (desiredX - CurrentX) * stiffness;
            double forceY = (desiredY - CurrentY) * stiffness;

            velX = (velX + forceX * dtSeconds) * damping;
            velY = (velY + forceY * dtSeconds) * damping;

            CurrentX += velX;
            CurrentY += velY;
        }

        private void UpdateStaticPinClicker()
        {
            if (waitTicksRemaining > 0)
            {
                waitTicksRemaining--;
                return;
            }
            ExecuteActionAtCurrentPosition(WaypointActionType.LeftClick, "", false);
            int ticks = Math.Max(4, (int)Math.Round((PinClickIntervalMs / Math.Max(0.25, SpeedMultiplier)) / 16.0));
            waitTicksRemaining = ticks;
        }

        private void EvaluateCubicBezier(double t, out double x, out double y)
        {
            double u = 1.0 - t;
            double tt = t * t;
            double uu = u * u;
            double uuu = uu * u;
            double ttt = tt * t;

            x = uuu * startX + 3 * uu * t * ctrl1X + 3 * u * tt * ctrl2X + ttt * subTargetX;
            y = uuu * startY + 3 * uu * t * ctrl1Y + 3 * u * tt * ctrl2Y + ttt * subTargetY;
        }

        // =====================================================================
        // High-DPI Crisp Vector Drawing of the Figma Cursor + Name Tag + Ripple
        // =====================================================================
        public void RenderLayeredBitmap(int screenX, int screenY)
        {
            if (!IsHandleCreated) return;

            using (Bitmap bmp = new Bitmap(OVERLAY_WIDTH, OVERLAY_HEIGHT, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.Clear(Color.Transparent);

                float tipX = TIP_OFFSET_X;
                float tipY = TIP_OFFSET_Y;

                // 1. Draw Click Ripple Ring if active
                if (rippleProgress > 0f && rippleProgress < 1f)
                {
                    float radius = 5f + rippleProgress * 24f;
                    int alpha = (int)((1f - rippleProgress) * 220);
                    using (Pen ripplePen = new Pen(Color.FromArgb(alpha, rippleColor), 2.8f))
                    {
                        g.DrawEllipse(ripplePen, tipX - radius, tipY - radius, radius * 2f, radius * 2f);
                    }
                    float innerR = 2f + rippleProgress * 12f;
                    using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(alpha / 3, Color.White)))
                    {
                        g.FillEllipse(glowBrush, tipX - innerR, tipY - innerR, innerR * 2f, innerR * 2f);
                    }
                }

                // 2. Build Figma Cursor Arrow Vector Path
                float scale = isPressedVisual ? 0.85f : 1.0f;
                PointF[] arrowPts = new PointF[]
                {
                    new PointF(tipX, tipY),
                    new PointF(tipX + 5.5f * scale, tipY + 20.5f * scale),
                    new PointF(tipX + 10.2f * scale, tipY + 12.8f * scale),
                    new PointF(tipX + 19.2f * scale, tipY + 9.8f * scale)
                };

                using (GraphicsPath arrowPath = new GraphicsPath())
                {
                    arrowPath.AddPolygon(arrowPts);

                    using (Matrix shadowMat = new Matrix())
                    {
                        shadowMat.Translate(0f, 2.0f);
                        using (GraphicsPath shadowPath = (GraphicsPath)arrowPath.Clone())
                        {
                            shadowPath.Transform(shadowMat);
                            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(75, 0, 0, 0)))
                            using (Pen shadowPen = new Pen(Color.FromArgb(55, 0, 0, 0), 3.0f) { LineJoin = LineJoin.Round })
                            {
                                g.DrawPath(shadowPen, shadowPath);
                                g.FillPath(shadowBrush, shadowPath);
                            }
                        }
                    }

                    using (Pen whiteOutline = new Pen(Color.White, 3.2f) { LineJoin = LineJoin.Round })
                    {
                        g.DrawPath(whiteOutline, arrowPath);
                    }

                    using (SolidBrush fillBrush = new SolidBrush(ThemeColor))
                    {
                        g.FillPath(fillBrush, arrowPath);
                    }
                }

                // 3. Draw Name Tag Pill (and optional Chat Message)
                string displayName = string.IsNullOrWhiteSpace(CursorName) ? "AI Player" : CursorName.Trim();
                bool hasChat = !string.IsNullOrWhiteSpace(ChatMessage);

                using (Font nameFont = new Font("Segoe UI Semibold", 9.5f, FontStyle.Bold, GraphicsUnit.Point))
                using (Font chatFont = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point))
                {
                    SizeF nameSize = g.MeasureString(displayName, nameFont);
                    float pillX = tipX + 13f;
                    float pillY = tipY + 18f;
                    float padX = 9f;
                    float padY = 4.5f;

                    float pillWidth = nameSize.Width + padX * 2f;
                    float pillHeight = nameSize.Height + padY * 2f;

                    if (hasChat)
                    {
                        SizeF chatSize = g.MeasureString(ChatMessage, chatFont);
                        pillWidth = Math.Max(pillWidth, chatSize.Width + padX * 2f + 4f);
                        pillHeight += chatSize.Height + 4f;
                    }

                    if (pillX + pillWidth > OVERLAY_WIDTH - 6)
                    {
                        pillWidth = OVERLAY_WIDTH - 6 - pillX;
                    }

                    RectangleF pillRect = new RectangleF(pillX, pillY, pillWidth, pillHeight);
                    float cornerRadius = hasChat ? 11f : 7f;

                    RectangleF shadowRect = new RectangleF(pillRect.X, pillRect.Y + 2.2f, pillRect.Width, pillRect.Height);
                    using (GraphicsPath pillShadowPath = CreateRoundedRectPath(shadowRect, cornerRadius))
                    using (SolidBrush pillShadowBrush = new SolidBrush(Color.FromArgb(68, 0, 0, 0)))
                    {
                        g.FillPath(pillShadowBrush, pillShadowPath);
                    }

                    using (GraphicsPath pillPath = CreateRoundedRectPath(pillRect, cornerRadius))
                    using (SolidBrush pillBrush = new SolidBrush(ThemeColor))
                    using (Pen pillBorder = new Pen(Color.FromArgb(65, 255, 255, 255), 1.0f))
                    {
                        g.FillPath(pillBrush, pillPath);
                        g.DrawPath(pillBorder, pillPath);
                    }

                    double luminance = (0.299 * ThemeColor.R + 0.587 * ThemeColor.G + 0.114 * ThemeColor.B) / 255.0;
                    Color textColor = luminance > 0.68 ? Color.FromArgb(24, 24, 27) : Color.White;

                    using (SolidBrush textBrush = new SolidBrush(textColor))
                    {
                        g.DrawString(displayName, nameFont, textBrush, pillX + padX, pillY + padY);

                        if (hasChat)
                        {
                            Color subTextColor = luminance > 0.68
                                ? Color.FromArgb(210, 24, 24, 27)
                                : Color.FromArgb(235, 255, 255, 255);
                            using (SolidBrush subBrush = new SolidBrush(subTextColor))
                            {
                                g.DrawString(ChatMessage, chatFont, subBrush, pillX + padX, pillY + padY + nameSize.Height + 1f);
                            }
                        }
                    }
                }

                // 4. Push 32-bit ARGB Bitmap to Windows DWM
                IntPtr screenDc = NativeMethods.GetDC(IntPtr.Zero);
                IntPtr memDc = NativeMethods.CreateCompatibleDC(screenDc);
                IntPtr hBitmap = IntPtr.Zero;
                IntPtr oldBitmap = IntPtr.Zero;

                try
                {
                    hBitmap = bmp.GetHbitmap(Color.FromArgb(0));
                    oldBitmap = NativeMethods.SelectObject(memDc, hBitmap);

                    NativeMethods.Size size = new NativeMethods.Size(OVERLAY_WIDTH, OVERLAY_HEIGHT);
                    NativeMethods.Point pointSource = new NativeMethods.Point(0, 0);
                    NativeMethods.Point topPos = new NativeMethods.Point(screenX, screenY);

                    NativeMethods.BLENDFUNCTION blend = new NativeMethods.BLENDFUNCTION
                    {
                        BlendOp = NativeMethods.AC_SRC_OVER,
                        BlendFlags = 0,
                        SourceConstantAlpha = 255,
                        AlphaFormat = NativeMethods.AC_SRC_ALPHA
                    };

                    NativeMethods.UpdateLayeredWindow(
                        this.Handle,
                        screenDc,
                        ref topPos,
                        ref size,
                        memDc,
                        ref pointSource,
                        0,
                        ref blend,
                        NativeMethods.ULW_ALPHA);
                }
                finally
                {
                    if (hBitmap != IntPtr.Zero)
                    {
                        NativeMethods.SelectObject(memDc, oldBitmap);
                        NativeMethods.DeleteObject(hBitmap);
                    }
                    NativeMethods.DeleteDC(memDc);
                    NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
                }
            }
        }

        private static GraphicsPath CreateRoundedRectPath(RectangleF rect, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float d = radius * 2f;
            if (d > rect.Width) d = rect.Width;
            if (d > rect.Height) d = rect.Height;

            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // =========================================================================
    // Main Control Panel Window (Figma Cursor Studio - Human AI Player Edition)
    // =========================================================================
    public class ControlPanelForm : Form
    {
        private const int HOTKEY_RECORD_PATH = 9001; // F7
        private const int HOTKEY_ADD_WAYPOINT = 9002; // F8
        private const int HOTKEY_PLAY_PAUSE = 9003;   // F9
        private const int HOTKEY_SHOW_HIDE = 9004;    // F10

        private readonly List<FigmaCursorOverlay> cursors = new List<FigmaCursorOverlay>();
        private int selectedCursorIndex = 0;
        private bool overlaysVisible = true;
        private bool isRecordingPath = false;
        private Point lastRecordedPoint = Point.Empty;
        private bool prevLeftMouseDown = false;
        private bool prevRightMouseDown = false;

        private readonly System.Windows.Forms.Timer frameTimer;
        private DateTime lastFrameTime;

        // UI Controls
        private ListBox lstCursors;
        private TextBox txtName;
        private TextBox txtChat;
        private CheckBox chkAutoChat;
        private Panel pnlColorPreview;
        private ComboBox cmbMode;
        private ComboBox cmbPlayStyle;
        private ComboBox cmbClickEngine;
        private TrackBar trkSpeed;
        private Label lblSpeedVal;
        private Button btnPlayPause;
        private Button btnToggleHide;

        // Waypoints & Actions Section
        private ListView lvWaypoints;
        private NumericUpDown numWpX;
        private NumericUpDown numWpY;
        private NumericUpDown numWpWait;
        private ComboBox cmbWpAction;
        private TextBox txtWpTypeString;
        private Button btnRecordPath;
        private Label lblStatusBanner;
        private Label lblActionLog;

        private NotifyIcon trayIcon;

        private static readonly Color[] FigmaPresetColors = new Color[]
        {
            Color.FromArgb(13, 153, 255),  // Figma Blue #0D99FF
            Color.FromArgb(151, 71, 255),  // Figma Purple #9747FF
            Color.FromArgb(255, 36, 189),  // Figma Pink #FF24BD
            Color.FromArgb(242, 72, 34),   // Figma Red/Coral #F24822
            Color.FromArgb(255, 136, 0),   // Figma Orange #FF8800
            Color.FromArgb(20, 174, 92),   // Figma Green #14AE5C
            Color.FromArgb(0, 181, 206)    // Figma Teal #00B5CE
        };

        private bool suppressUiEvents = false;

        public ControlPanelForm()
        {
            Text = "Figma Cursor Studio (Human AI Edition) — AI เล่นแทนเหมือนคนจริง";
            Size = new Size(880, 715);
            MinimumSize = new Size(860, 695);
            StartPosition = FormStartPosition.Manual;
            // Position neat at bottom-right so the rest of the screen is open for the AI player
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(Math.Max(20, wa.Right - 890), Math.Max(20, wa.Bottom - 725));
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            BackColor = Color.FromArgb(30, 30, 34);
            ForeColor = Color.FromArgb(240, 240, 245);

            BuildUI();
            SetupTrayIcon();

            // Create initial default Human AI Cursor
            AddNewFigmaCursor("AI Player", FigmaPresetColors[0], CursorMovementMode.HumanAIPlayer);

            lastFrameTime = DateTime.UtcNow;
            frameTimer = new System.Windows.Forms.Timer { Interval = 16 };
            frameTimer.Tick += OnFrameTick;
            frameTimer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_RECORD_PATH, 0, (uint)Keys.F7);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_ADD_WAYPOINT, 0, (uint)Keys.F8);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_PLAY_PAUSE, 0, (uint)Keys.F9);
            NativeMethods.RegisterHotKey(this.Handle, HOTKEY_SHOW_HIDE, 0, (uint)Keys.F10);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_RECORD_PATH);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_ADD_WAYPOINT);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_PLAY_PAUSE);
            NativeMethods.UnregisterHotKey(this.Handle, HOTKEY_SHOW_HIDE);

            frameTimer.Stop();
            foreach (var c in cursors)
            {
                if (!c.IsDisposed) c.Close();
            }
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
            }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == NativeMethods.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                if (id == HOTKEY_RECORD_PATH)
                {
                    TogglePathRecording();
                }
                else if (id == HOTKEY_ADD_WAYPOINT)
                {
                    CaptureCurrentMouseAsWaypoint();
                }
                else if (id == HOTKEY_PLAY_PAUSE)
                {
                    TogglePlayPauseAllOrSelected();
                }
                else if (id == HOTKEY_SHOW_HIDE)
                {
                    ToggleAllOverlaysVisibility();
                }
            }
            base.WndProc(ref m);
        }

        private void BuildUI()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.FromArgb(20, 20, 24)
            };

            Label lblTitle = new Label
            {
                Text = "🧠 Figma Human AI Player — ระบบ AI เล่นและขยับเมาส์เหมือนคนจริง",
                Font = new Font("Segoe UI Semibold", 13.5f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(16, 8)
            };

            Label lblSub = new Label
            {
                Text = "ทางลัด:  [F9] หยุด/เริ่มให้ AI เล่นแทนทันที  |  [F7] อัดการเล่นของเราให้ AI ทำตาม  |  [F8] บันทึกจุดพิกัด  |  [F10] ซ่อน/แสดงเคอร์เซอร์",
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(110, 220, 160),
                AutoSize = true,
                Location = new Point(18, 35)
            };

            header.Controls.Add(lblTitle);
            header.Controls.Add(lblSub);
            Controls.Add(header);

            // Left Panel
            Panel leftPanel = new Panel
            {
                Location = new Point(14, 72),
                Size = new Size(245, 595),
                BackColor = Color.FromArgb(38, 38, 44)
            };

            Label lblListHeader = new Label
            {
                Text = "รายการ AI เคอร์เซอร์บนหน้าจอ",
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Location = new Point(12, 12),
                AutoSize = true
            };

            lstCursors = new ListBox
            {
                Location = new Point(12, 38),
                Size = new Size(221, 180),
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 10f)
            };
            lstCursors.SelectedIndexChanged += (s, e) =>
            {
                if (suppressUiEvents || lstCursors.SelectedIndex < 0) return;
                selectedCursorIndex = lstCursors.SelectedIndex;
                LoadSelectedCursorToUI();
            };

            Button btnAddCursor = CreateStyledButton("+ เพิ่มเพื่อน AI อีกตัวบนจอ", new Point(12, 226), new Size(221, 34), Color.FromArgb(13, 153, 255));
            btnAddCursor.Click += (s, e) =>
            {
                int idx = cursors.Count + 1;
                Color c = FigmaPresetColors[(idx - 1) % FigmaPresetColors.Length];
                AddNewFigmaCursor("AI Player " + idx, c, CursorMovementMode.HumanAIPlayer);
            };

            Button btnRemoveCursor = CreateStyledButton("ลบตัวที่เลือก", new Point(12, 266), new Size(221, 32), Color.FromArgb(80, 45, 50));
            btnRemoveCursor.Click += (s, e) =>
            {
                if (cursors.Count <= 1)
                {
                    MessageBox.Show("ต้องมีเคอร์เซอร์อย่างน้อย 1 ตัวครับ (สามารถกด F9 เพื่อหยุดพักได้)", "แจ้งเตือน", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                FigmaCursorOverlay rem = cursors[selectedCursorIndex];
                rem.Close();
                cursors.RemoveAt(selectedCursorIndex);
                selectedCursorIndex = Math.Max(0, selectedCursorIndex - 1);
                RefreshCursorList();
                LoadSelectedCursorToUI();
            };

            btnPlayPause = CreateStyledButton("⏸ หยุดให้ AI เล่นชั่วคราว (F9)", new Point(12, 314), new Size(221, 42), Color.FromArgb(220, 120, 20));
            btnPlayPause.Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold);
            btnPlayPause.Click += (s, e) => TogglePlayPauseAllOrSelected();

            Button btnClickNow = CreateStyledButton("⚡ สั่งคลิกจุดที่ AI อยู่ทันที!", new Point(12, 364), new Size(221, 34), Color.FromArgb(20, 150, 85));
            btnClickNow.Click += (s, e) =>
            {
                var c = GetSelectedCursor();
                if (c != null)
                {
                    c.ExecuteActionAtCurrentPosition((WaypointActionType)cmbWpAction.SelectedIndex, txtWpTypeString.Text, false);
                    lblActionLog.Text = c.LastActionDescription;
                }
            };

            btnToggleHide = CreateStyledButton("👁 ซ่อนเคอร์เซอร์ทั้งหมด (F10)", new Point(12, 404), new Size(221, 32), Color.FromArgb(62, 62, 72));
            btnToggleHide.Click += (s, e) => ToggleAllOverlaysVisibility();

            Button btnMinimizeTray = CreateStyledButton("⬇ ย่อหน้าต่างนี้หลบลงมุมจอ (Tray)", new Point(12, 442), new Size(221, 34), Color.FromArgb(75, 95, 140));
            btnMinimizeTray.Click += (s, e) =>
            {
                this.Hide();
                trayIcon.ShowBalloonTip(2000, "Figma Human AI Player", "AI กำลังเล่นแทนอยู่บนหน้าจอ กด F9 เมื่อไรก็ได้เพื่อหยุด/เริ่ม", ToolTipIcon.Info);
            };

            lblStatusBanner = new Label
            {
                Text = "สถานะ: AI กำลังเล่นแทนเหมือนคนจริง",
                Location = new Point(12, 488),
                Size = new Size(221, 40),
                ForeColor = Color.FromArgb(110, 235, 165),
                Font = new Font("Segoe UI Semibold", 9.0f, FontStyle.Bold)
            };

            lblActionLog = new Label
            {
                Text = "กำลังสำรวจหน้าจอ...",
                Location = new Point(12, 532),
                Size = new Size(221, 55),
                ForeColor = Color.FromArgb(190, 205, 225),
                Font = new Font("Segoe UI", 8.75f)
            };

            leftPanel.Controls.AddRange(new Control[]
            {
                lblListHeader, lstCursors, btnAddCursor, btnRemoveCursor,
                btnPlayPause, btnClickNow, btnToggleHide, btnMinimizeTray, lblStatusBanner, lblActionLog
            });
            Controls.Add(leftPanel);

            // Right Panel
            Panel rightPanel = new Panel
            {
                Location = new Point(272, 72),
                Size = new Size(578, 595),
                BackColor = Color.FromArgb(38, 38, 44)
            };

            // 1. Appearance Section
            Label lblAppearance = new Label
            {
                Text = "1. ป้ายชื่อและสีของเคอร์เซอร์ (Figma Name Tag & Thoughts)",
                Font = new Font("Segoe UI Semibold", 10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 195, 255),
                Location = new Point(16, 10),
                AutoSize = true
            };

            Label lblName = new Label { Text = "ชื่อบนป้าย:", Location = new Point(16, 38), AutoSize = true };
            txtName = new TextBox
            {
                Location = new Point(95, 35),
                Size = new Size(175, 25),
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtName.TextChanged += (s, e) =>
            {
                if (suppressUiEvents) return;
                var c = GetSelectedCursor();
                if (c != null)
                {
                    c.CursorName = txtName.Text;
                    c.MarkDirty();
                    RefreshCursorList();
                }
            };

            Label lblChat = new Label { Text = "ข้อความพูด:", Location = new Point(285, 38), AutoSize = true };
            txtChat = new TextBox
            {
                Location = new Point(368, 35),
                Size = new Size(192, 25),
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            txtChat.TextChanged += (s, e) =>
            {
                if (suppressUiEvents) return;
                var c = GetSelectedCursor();
                if (c != null)
                {
                    c.ChatMessage = txtChat.Text;
                    c.MarkDirty();
                }
            };

            chkAutoChat = new CheckBox
            {
                Text = "ให้ AI แสดงความคิด/คำพูดขณะเล่นอัตโนมัติ",
                Location = new Point(95, 64),
                AutoSize = true,
                Checked = true
            };
            chkAutoChat.CheckedChanged += (s, e) =>
            {
                if (suppressUiEvents) return;
                var c = GetSelectedCursor();
                if (c != null) c.AutoChatMessages = chkAutoChat.Checked;
            };

            Label lblColor = new Label { Text = "สีเคอร์เซอร์:", Location = new Point(16, 92), AutoSize = true };
            pnlColorPreview = new Panel
            {
                Location = new Point(95, 88),
                Size = new Size(28, 28),
                BackColor = FigmaPresetColors[0],
                BorderStyle = BorderStyle.FixedSingle
            };

            int swatchX = 132;
            foreach (Color preset in FigmaPresetColors)
            {
                Color captured = preset;
                Button swatch = new Button
                {
                    Location = new Point(swatchX, 88),
                    Size = new Size(28, 28),
                    BackColor = captured,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                swatch.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                swatch.FlatAppearance.BorderSize = 1;
                swatch.Click += (s, e) => ApplyColorToSelected(captured);
                rightPanel.Controls.Add(swatch);
                swatchX += 34;
            }

            Button btnCustomColor = CreateStyledButton("เลือกสีเอง...", new Point(swatchX + 6, 88), new Size(96, 28), Color.FromArgb(60, 60, 70));
            btnCustomColor.Click += (s, e) =>
            {
                var c = GetSelectedCursor();
                using (ColorDialog cd = new ColorDialog { FullOpen = true, Color = c != null ? c.ThemeColor : Color.DodgerBlue })
                {
                    if (cd.ShowDialog() == DialogResult.OK)
                    {
                        ApplyColorToSelected(cd.Color);
                    }
                }
            };

            // 2. Human AI Brain & Play Style Section
            Label lblMoveHeader = new Label
            {
                Text = "2. บุคลิกความฉลาดของ AI และรูปแบบการคลิก (Human Biomechanics & AI Persona)",
                Font = new Font("Segoe UI Semibold", 10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 195, 255),
                Location = new Point(16, 126),
                AutoSize = true
            };

            Label lblMode = new Label { Text = "โหมดหลัก:", Location = new Point(16, 154), AutoSize = true };
            cmbMode = new ComboBox
            {
                Location = new Point(95, 150),
                Size = new Size(465, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.FromArgb(130, 245, 185)
            };
            cmbMode.Items.AddRange(new object[]
            {
                "🧠 AI เล่นแทนเหมือนคนจริงๆ (Smart Human Player — มีจังหวะเล็ง คิด อ่าน เลื่อนจอ และคลิกจริง)",
                "📍 วิ่งและคลิกตามลำดับจุด Waypoints / เส้นทางที่เราอัดไว้",
                "🧲 ติดตามเมาส์หลัก (Follow Main Cursor)",
                "📌 Auto-Clicker ปักหมุดคลิกซ้ำที่จุดเดิม"
            });
            cmbMode.SelectedIndexChanged += (s, e) =>
            {
                if (suppressUiEvents) return;
                var c = GetSelectedCursor();
                if (c != null)
                {
                    c.Mode = (CursorMovementMode)cmbMode.SelectedIndex;
                    RefreshCursorList();
                }
            };

            Label lblStyle = new Label { Text = "นิสัยการเล่น:", Location = new Point(16, 186), AutoSize = true };
            cmbPlayStyle = new ComboBox
            {
                Location = new Point(95, 182),
                Size = new Size(465, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White
            };
            cmbPlayStyle.Items.AddRange(new object[]
            {
                "🧑‍💻 คนใช้งานทั่วไป (กวาดสายตาดูหน้าต่างที่เปิดอยู่ เลื่อนลูกกลิ้งอ่านเว็บ คลิกสำรวจอย่างเป็นธรรมชาติ)",
                "🎮 คนเล่นเกม / คลิกไว (สะบัดข้อมือไว เล็งเป้าละเอียด ตอบสนองและคลิกต่อเนื่อง)",
                "🎨 นักออกแบบ Figma (สำรวจ Layout ไล่เช็ครายละเอียดเป็นกลุ่มก้อน พร้อมคอมเมนต์งาน)"
            });
            cmbPlayStyle.SelectedIndexChanged += (s, e) =>
            {
                if (suppressUiEvents) return;
                var c = GetSelectedCursor();
                if (c != null)
                {
                    c.PlayStyle = (HumanPlayStyle)cmbPlayStyle.SelectedIndex;
                }
            };

            Label lblEngine = new Label { Text = "ระบบคลิกจริง:", Location = new Point(16, 218), AutoSize = true };
            cmbClickEngine = new ComboBox
            {
                Location = new Point(95, 214),
                Size = new Size(465, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White
            };
            cmbClickEngine.Items.AddRange(new object[]
            {
                "✅ คลิกและเลื่อนจอจริงทุกแอป + คืนเมาส์หลักทันที (ไม่แย่งเมาส์เวลาคุณขยับเอง)",
                "🖱 คลิกและย้ายเมาส์หลักไปตาม AI ด้วยเลย (Full Hardware Mouse Control)",
                "👻 คลิกเบื้องหลังผ่าน Window Message (WM_LBUTTONDOWN)",
                "✨ แสดงเฉพาะท่าทางและเอฟเฟกต์ (ไม่ส่งคลิกจริง)"
            });
            cmbClickEngine.SelectedIndexChanged += (s, e) =>
            {
                if (suppressUiEvents) return;
                var c = GetSelectedCursor();
                if (c != null)
                {
                    c.ClickEngine = (ClickEngineMode)cmbClickEngine.SelectedIndex;
                }
            };

            Label lblSpeed = new Label { Text = "ความไวมือคน:", Location = new Point(16, 252), AutoSize = true };
            trkSpeed = new TrackBar
            {
                Location = new Point(95, 246),
                Size = new Size(250, 32),
                Minimum = 3,
                Maximum = 40,
                Value = 10,
                TickStyle = TickStyle.None
            };
            lblSpeedVal = new Label { Text = "1.0x (ธรรมชาติ)", Location = new Point(350, 252), AutoSize = true };
            trkSpeed.ValueChanged += (s, e) =>
            {
                double mult = trkSpeed.Value / 10.0;
                lblSpeedVal.Text = mult.ToString("0.0") + "x";
                if (suppressUiEvents) return;
                var c = GetSelectedCursor();
                if (c != null) c.SpeedMultiplier = mult;
            };

            // 3. Waypoints & Macro Recording Section
            Label lblWpHeader = new Label
            {
                Text = "3. สอนท่าทางให้ AI เล่นซ้ำ (กด F7 เพื่ออัดการเล่นจริงของคุณ หรือกด F8 เพื่อปักจุด)",
                Font = new Font("Segoe UI Semibold", 10.2f, FontStyle.Bold),
                ForeColor = Color.FromArgb(120, 195, 255),
                Location = new Point(16, 284),
                AutoSize = true
            };

            lvWaypoints = new ListView
            {
                Location = new Point(16, 310),
                Size = new Size(544, 165),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            lvWaypoints.Columns.Add("#", 34);
            lvWaypoints.Columns.Add("X", 62);
            lvWaypoints.Columns.Add("Y", 62);
            lvWaypoints.Columns.Add("คำสั่งทำงานจริง (Action)", 140);
            lvWaypoints.Columns.Add("หยุดรอ (ms)", 82);
            lvWaypoints.Columns.Add("ข้อความพิมพ์ / หมายเหตุ", 155);

            Label lblX = new Label { Text = "X:", Location = new Point(16, 486), AutoSize = true };
            numWpX = new NumericUpDown
            {
                Location = new Point(36, 483),
                Size = new Size(64, 25),
                Minimum = 0,
                Maximum = 10000,
                Value = 500,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White
            };

            Label lblY = new Label { Text = "Y:", Location = new Point(106, 486), AutoSize = true };
            numWpY = new NumericUpDown
            {
                Location = new Point(126, 483),
                Size = new Size(64, 25),
                Minimum = 0,
                Maximum = 10000,
                Value = 400,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White
            };

            Label lblWait = new Label { Text = "รอ(ms):", Location = new Point(196, 486), AutoSize = true };
            numWpWait = new NumericUpDown
            {
                Location = new Point(248, 483),
                Size = new Size(68, 25),
                Minimum = 0,
                Maximum = 60000,
                Increment = 100,
                Value = 800,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White
            };

            Label lblAct = new Label { Text = "คำสั่ง:", Location = new Point(324, 486), AutoSize = true };
            cmbWpAction = new ComboBox
            {
                Location = new Point(368, 482),
                Size = new Size(192, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White
            };
            cmbWpAction.Items.AddRange(new object[]
            {
                "🖱 คลิกซ้าย (Left Click)",
                "⚡ ดับเบิลคลิก (Double Click)",
                "🖱 คลิกขวา (Right Click)",
                "⬇ เลื่อนจอลง (Scroll Down)",
                "⬆ เลื่อนจอขึ้น (Scroll Up)",
                "⌨ คลิกแล้วพิมพ์ข้อความ (Type)",
                "➖ เลื่อนไปชี้เฉยๆ (ไม่คลิก)"
            });
            cmbWpAction.SelectedIndex = 0;

            Label lblType = new Label { Text = "ข้อความที่จะให้พิมพ์ (ถ้าเลือกคลิกแล้วพิมพ์):", Location = new Point(16, 518), AutoSize = true };
            txtWpTypeString = new TextBox
            {
                Location = new Point(268, 515),
                Size = new Size(165, 25),
                Text = "Hello{ENTER}",
                BackColor = Color.FromArgb(24, 24, 28),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            Button btnAddWpManual = CreateStyledButton("+ เพิ่มคำสั่งนี้", new Point(442, 513), new Size(118, 28), Color.FromArgb(13, 153, 255));
            btnAddWpManual.Click += (s, e) =>
            {
                var c = GetSelectedCursor();
                if (c == null) return;
                WaypointActionType act = (WaypointActionType)cmbWpAction.SelectedIndex;
                c.Waypoints.Add(new WaypointItem(
                    (int)numWpX.Value,
                    (int)numWpY.Value,
                    (int)numWpWait.Value,
                    act,
                    txtWpTypeString.Text,
                    act == WaypointActionType.ClickAndType ? txtWpTypeString.Text : "เพิ่มเอง"));
                RefreshWaypointsList();
            };

            btnRecordPath = CreateStyledButton("⏺ สอน AI ด้วยการอัดเมาส์จริง (F7)", new Point(16, 550), new Size(218, 32), Color.FromArgb(190, 50, 60));
            btnRecordPath.Click += (s, e) => TogglePathRecording();

            Button btnDeleteWp = CreateStyledButton("ลบจุดที่เลือก", new Point(240, 550), new Size(94, 32), Color.FromArgb(62, 62, 72));
            btnDeleteWp.Click += (s, e) =>
            {
                var c = GetSelectedCursor();
                if (c == null || lvWaypoints.SelectedIndices.Count == 0) return;
                int idx = lvWaypoints.SelectedIndices[0];
                if (idx >= 0 && idx < c.Waypoints.Count)
                {
                    c.Waypoints.RemoveAt(idx);
                    RefreshWaypointsList();
                }
            };

            Button btnClearWp = CreateStyledButton("ล้างตาราง", new Point(340, 550), new Size(88, 32), Color.FromArgb(62, 62, 72));
            btnClearWp.Click += (s, e) =>
            {
                var c = GetSelectedCursor();
                if (c == null) return;
                c.Waypoints.Clear();
                RefreshWaypointsList();
            };

            Button btnRunWpNow = CreateStyledButton("▶ เล่นตามตาราง", new Point(434, 550), new Size(126, 32), Color.FromArgb(20, 150, 85));
            btnRunWpNow.Font = new Font("Segoe UI Semibold", 9.2f, FontStyle.Bold);
            btnRunWpNow.Click += (s, e) =>
            {
                var c = GetSelectedCursor();
                if (c == null) return;
                c.CurrentWaypointIndex = 0;
                c.Mode = CursorMovementMode.Waypoints;
                c.IsPaused = false;
                cmbMode.SelectedIndex = (int)CursorMovementMode.Waypoints;
                RefreshCursorList();
                UpdateStatusUI();
            };

            rightPanel.Controls.AddRange(new Control[]
            {
                lblAppearance, lblName, txtName, lblChat, txtChat, chkAutoChat,
                lblColor, pnlColorPreview, btnCustomColor,
                lblMoveHeader, lblMode, cmbMode, lblStyle, cmbPlayStyle, lblEngine, cmbClickEngine, lblSpeed, trkSpeed, lblSpeedVal,
                lblWpHeader, lvWaypoints,
                lblX, numWpX, lblY, numWpY, lblWait, numWpWait, lblAct, cmbWpAction,
                lblType, txtWpTypeString, btnAddWpManual,
                btnRecordPath, btnDeleteWp, btnClearWp, btnRunWpNow
            });
            Controls.Add(rightPanel);
        }

        private void SetupTrayIcon()
        {
            ContextMenuStrip trayMenu = new ContextMenuStrip();
            trayMenu.Items.Add("เปิดหน้าต่าง Figma Cursor Studio", null, (s, e) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); });
            trayMenu.Items.Add("เริ่ม/หยุดให้ AI เล่นแทน (F9)", null, (s, e) => TogglePlayPauseAllOrSelected());
            trayMenu.Items.Add("ซ่อน/แสดงเคอร์เซอร์ (F10)", null, (s, e) => ToggleAllOverlaysVisibility());
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add("ออกจากโปรแกรม", null, (s, e) => Application.Exit());

            trayIcon = new NotifyIcon
            {
                Text = "Figma Human AI Player",
                Icon = SystemIcons.Application,
                ContextMenuStrip = trayMenu,
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) =>
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Activate();
            };
        }

        private Button CreateStyledButton(string text, Point loc, Size size, Color backColor)
        {
            Button btn = new Button
            {
                Text = text,
                Location = loc,
                Size = size,
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderColor = Color.FromArgb(90, 255, 255, 255);
            btn.FlatAppearance.BorderSize = 1;
            return btn;
        }

        private FigmaCursorOverlay GetSelectedCursor()
        {
            if (selectedCursorIndex >= 0 && selectedCursorIndex < cursors.Count)
                return cursors[selectedCursorIndex];
            return null;
        }

        private void AddNewFigmaCursor(string name, Color color, CursorMovementMode mode)
        {
            Rectangle scr = Screen.PrimaryScreen.WorkingArea;
            int initX = scr.Left + scr.Width / 3 + (cursors.Count * 70);
            int initY = scr.Top + scr.Height / 3 + (cursors.Count * 45);

            var overlay = new FigmaCursorOverlay(Environment.TickCount + cursors.Count * 1337, name, color, initX, initY, this)
            {
                Mode = mode,
                PlayStyle = HumanPlayStyle.NaturalUser,
                ClickEngine = ClickEngineMode.HardwareInstantRestore,
                SpeedMultiplier = 1.0,
                AutoChatMessages = true
            };

            overlay.Waypoints.Add(new WaypointItem(scr.Left + 240, scr.Top + 220, 900, WaypointActionType.LeftClick, "", "จุดที่ 1 (คลิกซ้าย)"));
            overlay.Waypoints.Add(new WaypointItem(scr.Left + 580, scr.Top + 260, 900, WaypointActionType.LeftClick, "", "จุดที่ 2 (คลิกซ้าย)"));
            overlay.Waypoints.Add(new WaypointItem(scr.Left + 420, scr.Top + 480, 900, WaypointActionType.ScrollDown, "", "จุดที่ 3 (เลื่อนจอลง)"));

            overlay.Show();
            overlay.RenderLayeredBitmap(initX - FigmaCursorOverlay.TIP_OFFSET_X, initY - FigmaCursorOverlay.TIP_OFFSET_Y);

            cursors.Add(overlay);
            selectedCursorIndex = cursors.Count - 1;
            RefreshCursorList();
            LoadSelectedCursorToUI();
        }

        private void ApplyColorToSelected(Color color)
        {
            var c = GetSelectedCursor();
            if (c != null)
            {
                c.ThemeColor = color;
                c.MarkDirty();
                pnlColorPreview.BackColor = color;
            }
        }

        private void RefreshCursorList()
        {
            suppressUiEvents = true;
            lstCursors.Items.Clear();
            for (int i = 0; i < cursors.Count; i++)
            {
                var c = cursors[i];
                string modeTag = c.Mode == CursorMovementMode.HumanAIPlayer ? "Human AI"
                               : c.Mode == CursorMovementMode.Waypoints ? "Waypoints"
                               : c.Mode == CursorMovementMode.FollowMouse ? "Follow" : "Pin Clicker";
                string stateTag = c.IsPaused ? "⏸" : "▶";
                lstCursors.Items.Add(string.Format("{0} {1}. {2} [{3}]", stateTag, i + 1, c.CursorName, modeTag));
            }
            if (selectedCursorIndex >= 0 && selectedCursorIndex < lstCursors.Items.Count)
            {
                lstCursors.SelectedIndex = selectedCursorIndex;
            }
            suppressUiEvents = false;
        }

        private void LoadSelectedCursorToUI()
        {
            var c = GetSelectedCursor();
            if (c == null) return;

            suppressUiEvents = true;
            txtName.Text = c.CursorName;
            txtChat.Text = c.ChatMessage;
            chkAutoChat.Checked = c.AutoChatMessages;
            pnlColorPreview.BackColor = c.ThemeColor;
            cmbMode.SelectedIndex = (int)c.Mode;
            cmbPlayStyle.SelectedIndex = (int)c.PlayStyle;
            cmbClickEngine.SelectedIndex = (int)c.ClickEngine;
            int speedSlider = Math.Max(trkSpeed.Minimum, Math.Min(trkSpeed.Maximum, (int)Math.Round(c.SpeedMultiplier * 10)));
            trkSpeed.Value = speedSlider;
            lblSpeedVal.Text = (speedSlider / 10.0).ToString("0.0") + "x";
            suppressUiEvents = false;

            RefreshWaypointsList();
            UpdateStatusUI();
        }

        private void RefreshWaypointsList()
        {
            var c = GetSelectedCursor();
            lvWaypoints.BeginUpdate();
            lvWaypoints.Items.Clear();
            if (c != null)
            {
                for (int i = 0; i < c.Waypoints.Count; i++)
                {
                    var wp = c.Waypoints[i];
                    ListViewItem item = new ListViewItem((i + 1).ToString());
                    item.SubItems.Add(wp.X.ToString());
                    item.SubItems.Add(wp.Y.ToString());
                    item.SubItems.Add(WaypointItem.GetActionLabel(wp.Action));
                    item.SubItems.Add(wp.WaitMs.ToString());
                    item.SubItems.Add(string.IsNullOrEmpty(wp.Note) ? wp.TextToType : wp.Note);
                    lvWaypoints.Items.Add(item);
                }
            }
            lvWaypoints.EndUpdate();
        }

        private void CaptureCurrentMouseAsWaypoint()
        {
            var c = GetSelectedCursor();
            if (c == null) return;

            Point pt = Cursor.Position;
            numWpX.Value = Math.Max(0, Math.Min(10000, pt.X));
            numWpY.Value = Math.Max(0, Math.Min(10000, pt.Y));

            WaypointActionType act = (WaypointActionType)cmbWpAction.SelectedIndex;
            string note = act == WaypointActionType.ClickAndType ? ("พิมพ์: " + txtWpTypeString.Text) : "บันทึกด้วย F8";

            c.Waypoints.Add(new WaypointItem(pt.X, pt.Y, (int)numWpWait.Value, act, txtWpTypeString.Text, note));
            RefreshWaypointsList();
            lblStatusBanner.Text = string.Format("✔ บันทึกจุด ({0}, {1}) [{2}] แล้ว!", pt.X, pt.Y, WaypointItem.GetActionLabel(act));
        }

        private void TogglePathRecording()
        {
            var c = GetSelectedCursor();
            if (c == null) return;

            isRecordingPath = !isRecordingPath;
            if (isRecordingPath)
            {
                c.Waypoints.Clear();
                c.IsPaused = true;
                lastRecordedPoint = Cursor.Position;
                prevLeftMouseDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;
                prevRightMouseDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RBUTTON) & 0x8000) != 0;

                c.Waypoints.Add(new WaypointItem(lastRecordedPoint.X, lastRecordedPoint.Y, 20, WaypointActionType.MoveOnly, "", "เริ่มอัด"));
                btnRecordPath.Text = "⏹ หยุดอัดและให้ AI เล่นซ้ำ (F7)";
                btnRecordPath.BackColor = Color.FromArgb(240, 35, 35);
                lblStatusBanner.Text = "🔴 กำลังจดจำการเล่นของคุณ... กด F7 เพื่อหยุดและให้ AI เล่นซ้ำ";
                lblStatusBanner.ForeColor = Color.FromArgb(255, 130, 130);
            }
            else
            {
                c.CurrentWaypointIndex = 0;
                c.Mode = CursorMovementMode.Waypoints;
                c.IsPaused = false;
                cmbMode.SelectedIndex = (int)CursorMovementMode.Waypoints;
                btnRecordPath.Text = "⏺ สอน AI ด้วยการอัดเมาส์จริง (F7)";
                btnRecordPath.BackColor = Color.FromArgb(190, 50, 60);
                RefreshWaypointsList();
                RefreshCursorList();
                UpdateStatusUI();
            }
        }

        private void TogglePlayPauseAllOrSelected()
        {
            var c = GetSelectedCursor();
            if (c == null) return;
            bool newPausedState = !c.IsPaused;
            foreach (var cur in cursors)
            {
                cur.IsPaused = newPausedState;
            }
            RefreshCursorList();
            UpdateStatusUI();
        }

        private void ToggleAllOverlaysVisibility()
        {
            overlaysVisible = !overlaysVisible;
            foreach (var c in cursors)
            {
                c.Visible = overlaysVisible;
                if (overlaysVisible) c.MarkDirty();
            }
            btnToggleHide.Text = overlaysVisible ? "👁 ซ่อนเคอร์เซอร์ทั้งหมด (F10)" : "👁 แสดงเคอร์เซอร์ทั้งหมด (F10)";
            UpdateStatusUI();
        }

        private void UpdateStatusUI()
        {
            var c = GetSelectedCursor();
            if (c == null) return;

            if (c.IsPaused)
            {
                btnPlayPause.Text = "▶ เริ่มให้ AI เล่นแทนต่อ (F9)";
                btnPlayPause.BackColor = Color.FromArgb(20, 150, 85);
                lblStatusBanner.Text = "สถานะ: หยุดพักชั่วคราว (กดปุ่ม F9 เพื่อเริ่มให้ AI เล่นต่อ)";
                lblStatusBanner.ForeColor = Color.FromArgb(255, 200, 110);
            }
            else
            {
                btnPlayPause.Text = "⏸ หยุดให้ AI เล่นชั่วคราว (F9)";
                btnPlayPause.BackColor = Color.FromArgb(220, 120, 20);
                lblStatusBanner.Text = overlaysVisible
                    ? string.Format("สถานะ: AI กำลังเล่นแทนเหมือนคนจริง ({0} ตัว)", cursors.Count)
                    : "สถานะ: ซ่อนเคอร์เซอร์อยู่ (กด F10 เพื่อแสดง)";
                lblStatusBanner.ForeColor = Color.FromArgb(110, 235, 165);
            }
        }

        private void OnFrameTick(object sender, EventArgs e)
        {
            DateTime now = DateTime.UtcNow;
            double dt = (now - lastFrameTime).TotalSeconds;
            lastFrameTime = now;
            if (dt <= 0 || dt > 0.25) dt = 0.016;

            if (isRecordingPath)
            {
                var c = GetSelectedCursor();
                if (c != null)
                {
                    Point pt = Cursor.Position;
                    bool leftDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_LBUTTON) & 0x8000) != 0;
                    bool rightDown = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_RBUTTON) & 0x8000) != 0;

                    if (leftDown && !prevLeftMouseDown)
                    {
                        c.Waypoints.Add(new WaypointItem(pt.X, pt.Y, 240, WaypointActionType.LeftClick, "", "คลิกซ้ายที่อัดไว้"));
                        lastRecordedPoint = pt;
                    }
                    else if (rightDown && !prevRightMouseDown)
                    {
                        c.Waypoints.Add(new WaypointItem(pt.X, pt.Y, 240, WaypointActionType.RightClick, "", "คลิกขวาที่อัดไว้"));
                        lastRecordedPoint = pt;
                    }
                    else
                    {
                        double dist = Math.Sqrt((pt.X - lastRecordedPoint.X) * (pt.X - lastRecordedPoint.X) +
                                                (pt.Y - lastRecordedPoint.Y) * (pt.Y - lastRecordedPoint.Y));
                        if (dist >= 16.0)
                        {
                            c.Waypoints.Add(new WaypointItem(pt.X, pt.Y, 16, WaypointActionType.MoveOnly, "", "เคลื่อนที่"));
                            lastRecordedPoint = pt;
                        }
                    }

                    prevLeftMouseDown = leftDown;
                    prevRightMouseDown = rightDown;
                }
            }

            if (!overlaysVisible) return;

            for (int i = 0; i < cursors.Count; i++)
            {
                cursors[i].TickUpdate(dt);
            }

            var sel = GetSelectedCursor();
            if (sel != null && lblActionLog.Text != sel.LastActionDescription)
            {
                lblActionLog.Text = sel.LastActionDescription;
            }
        }
    }

    // =========================================================================
    // Program Entry Point
    // =========================================================================
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            try { NativeMethods.SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ControlPanelForm());
        }
    }
}
