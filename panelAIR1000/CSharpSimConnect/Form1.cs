


using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using System.Globalization;
using System.Windows.Forms;
using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Drawing;
using panelAIR1000.Properties;
using System.Drawing.Drawing2D;
using System.Diagnostics;
using Microsoft.VisualBasic.Devices;

//namespace SimConnect3bool b1WasPressed_NAV = false
namespace SimConnect3
{
    public partial class frmMain : Form
    {
        private const bool enableHidReading = false;

        Thread hidThread1;
        bool stopHidThread1 = false;

        Thread hidThread2;
        bool stopHidThread2 = false;

        Thread hidThread3;
        bool stopHidThread3 = false;

        Thread hidThread4;
        bool stopHidThread4 = false;

        Thread hidThread5;
        bool stopHidThread5 = false;

        
        private SimConnect simconnect = null;

        private System.Windows.Forms.Timer simReconnectTimer;
        private bool simReconnectPending = false;
        private bool _suppressCloseOnConnectFail = false;
        private bool showAllClickAreas = false;
        private UiSurface mainSurface;

        private bool sfsMonitorRunning = false;
        private Form? _sfsMissingForm;
        private System.Threading.Timer? _sfsCheckTimer;
        private System.Windows.Forms.Button _tactileBtn = null!;
        private System.Windows.Forms.Button _showAreasBtn = null!;




        // Estat connexió RotAIR
        private bool isType1GCConnected = false;
        private bool isType1ANAConnected = false;
        private bool isType2GCConnected = false;
        private bool isType2ANAConnected = false;
        private bool isType2GCAMConnected = false;

        private const int WM_USER_SIMCONNECT = 0x0402;

        private double previousNav1Stby = 0;
        private double previousNav2Stby = 0;
        private bool b1WasPressed_NAV = false;
        private bool prevB1 = false;
        private bool prevB2 = false;
        private bool prevB3 = false;
        private bool prevB4 = false;
        private bool prevB5 = false;
        private bool prevB6 = false;
        private bool prevB7 = false;
        private bool prevB8 = false;
        private bool prevB9 = false;
        private bool prevB10 = false;
        private bool prevB11 = false;
        private bool prevB12 = false;
        private bool prevB13 = false;
        private bool prevB14 = false;
        private bool prevB15 = false;
        private bool prevB16 = false;
        private bool prevB17 = false;
        private bool prevB18 = false;
        private bool prevB19 = false;
        private bool prevB20 = false;
        private bool prevB21 = false;
        private bool prevB22 = false;
        private bool prevB23 = false;
        private bool prevB24 = false;
        private bool prevB25 = false;
        private bool prevB26 = false;
        private bool prevB27 = false;
        private bool prevB28 = false;
        private bool prevB29 = false;
        private bool prevB30 = false;
        private bool prevB31 = false;
        private bool prevB32 = false;
        private bool prevB33 = false;
        private bool prevB34 = false;
        private bool prevB35 = false;
        private bool prevB36 = false;
        private bool prevB37 = false;
        private bool prevB38 = false;
        private bool prevB39 = false;
        private bool prevB40 = false;

        private bool prevB1_b = false;
        private bool prevB2_b = false;
        private bool prevB3_b = false;
        private bool prevB4_b = false;
        private bool prevB5_b = false;
        private bool prevB6_b = false;
        private bool prevB7_b = false;
        private bool prevB8_b = false;
        private bool prevB9_b = false;
        private bool prevB10_b = false;
        private bool prevB11_b = false;
        private bool prevB12_b = false;
        private bool prevB13_b = false;
        private bool prevB14_b = false;
        private bool prevB15_b = false;
        private bool prevB16_b = false;
        private bool prevB17_b = false;
        private bool prevB18_b = false;
        private bool prevB19_b = false;
        private bool prevB20_b = false;
        private bool prevB21_b = false;
        private bool prevB22_b = false;
        private bool prevB23_b = false;
        private bool prevB24_b = false;
        private bool prevB25_b = false;
        private bool prevB26_b = false;
        private bool prevB27_b = false;
        private bool prevB28_b = false;
        private bool prevB29_b = false;
        private bool prevB30_b = false;
        private bool prevB31_b = false;
        private bool prevB32_b = false;
        private bool prevB33_b = false;
        private bool prevB34_b = false;
        private bool prevB35_b = false;
        private bool prevB36_b = false;
        private bool prevB37_b = false;
        private bool prevB38_b = false;
        private bool prevB39_b = false;
        private bool prevB40_b = false;

        bool com1Tx;
        bool com2Tx;
        bool displayBackupActive_bool;
        bool MKRmute_bool = false;

        bool com1Active;
        bool com2Active;
        bool comReceiveAll;
        bool dmeActive;
        bool nav1Active;
        bool spkrActive;
        bool mkrMute;
        bool adfMute;
        bool nav2Active;

        bool beaconActive;
        bool landActive;
        bool taxiActive;
        bool navActive;
        bool strobeActive;
        bool recognActive;
        bool panelLightActive;
        bool deiceActive;
        private System.Windows.Forms.Timer? trialTimer;
        private System.Threading.Timer? trialBgTimer;
        int cowlFlapL; // 0=closed, 1=center, 2=open
        int cowlFlapR;
        bool pitotheatActive;
        bool fuelpump1Active;
        bool fuelpump2Active;
        bool pbrakeActive;
        bool masterBatteryOff;
        bool avionics1Off;
        bool avionics2Off;
        bool altGen1Off;
        bool altGen2Off;
        bool starter1Active;
        bool starter2Active;
        bool ignition1Active;
        bool ignition2Active;
        private int ign1Pos = 2; // 0=OFF, 1=RIGHT, 2=LEFT(default), 3=BOTH, 4=START
        private int ign2Pos = 2;
        private int fuel1Pos = 0; // 0=OFF(-30°), 1=ON(0°), 2=CROSSFEED(+30°)
        private int fuel2Pos = 0; // 0=CROSSFEED(-30°), 1=ON(0°), 2=OFF(+30°)
        private int numEngines = 2; // detectat automàticament de MSFS

        int gearNosePercent = 0;
        int gearLeftPercent = 0;
        int gearRightPercent = 0;
        double mainBusVolts = 0.0;

        int flapsNumPositions = 0;
        int flapsPoscPalanca;
        
        int flapsPoscPalanca_Last = -1;
        float flapsCircleY_Start;

        double elevatorTrim;
        double elevatorTrim_Last = double.NaN;

        // --- FLAPS INDICATOR STATE ---
        float flapsCircleY_Current = 445f;   // posició visual actual
        float flapsCircleY_Target = 445f;   // destí segons flapsPoscPalanca
        DateTime flapsAnimStartTime;
        float flapsAnimDurationSec = 0f;
        private int trimAccumulatedDelta = 0;
        private const int TRIM_TICK_PIXELS = 8;         // ajustable (prova 6–12)
        private const int RUDDER_TRIM_TICK_PIXELS = 48; // 6× menys sensible

        // ================= TRIM (SCROLL LINEAR) — coords absolutes 4K =================
        private int trimZoneBaseX = 3685;
        private int trimZoneBaseY = 1564;
        private Rectangle trimZone = new Rectangle(3685, 1564, 100, 507);
        // ================= TRIM ARROWS (VISUAL) =================
        private Point trimArrowUpPos   = new Point(3735, 2029);
        private Point trimArrowDownPos = new Point(3735, 1606);
        // calibration drag
        private bool  calibDragTrimZone = false;
        private Point calibTrimZoneDragStart;
        private int   calibTrimZoneStartX;
        private int   calibTrimZoneStartY;


        private bool trimDragging = false;
        private int trimLastMouseY;

        // ================= RUDDER TRIM =================
        private double rudderTrim = 0;
        private int rudderTrimAccumulatedDelta = 0;
        private bool rudderTrimDragging = false;
        private int  rudderTrimLastMouseX;
        private int  rudderTrimLastDir = 0;
        // zona de drag horitzontal
        private int rudderTrimZoneBaseX = 2760;
        private int rudderTrimZoneBaseY = 2050;
        private Rectangle rudderTrimZone = new Rectangle(2760, 2050, 507, 100);
        private Point rudderTrimArrowLeftPos  = new Point(2802, 2100);
        private Point rudderTrimArrowRightPos = new Point(3225, 2100);
        private bool  calibDragRudderTrimZone = false;
        private Point calibRudderTrimZoneDragStart;
        private int   calibRudderTrimZoneStartX;
        private int   calibRudderTrimZoneStartY;
        // indicador horitzontal
        private int rudderTrimIndY       = 2020;
        private int rudderTrimIndXOffset = 0;
        private bool calibDragRudderTrimInd = false;
        private Point calibRudderTrimIndDragStart;
        private int   calibRudderTrimIndStartY;
        private int   calibRudderTrimIndStartXOff;

       
        //fletxes indicadores de touch lliscant
        private int trimLastDir = 0;   // -1 avall, +1 amunt, 0 res

        // sensibilitat (equivalent a SCROLL_TICK)
        private const float TRIM_SENSITIVITY = 0.0025f;

        




        // repaintAudioOverlay eliminat — tot va a pbMain.Invalidate()
        


        //accelaradors
        // --- Estat compartit per a acceleració (HDG/CRS/GYRO si cal) ---
        // Type-1 GC → HDG
        private DateTime _gcHdgLastTick = DateTime.MinValue;
        private DateTime _gcHdgWindowStart = DateTime.MinValue;
        private int _gcHdgAppliedInWindow = 0;

        // Type-1 ANA → HDG
        private DateTime _anaHdgLastTick = DateTime.MinValue;
        private DateTime _anaHdgWindowStart = DateTime.MinValue;
        private int _anaHdgAppliedInWindow = 0;

        // Type-1 ANA → GYRO
        private DateTime _gyroLastTick = DateTime.MinValue;
        private DateTime _gyroWindowStart = DateTime.MinValue;
        private int _gyroAppliedInWindow = 0;

        // Type-2 GC → CRS
        private DateTime _crsLastTick = DateTime.MinValue;
        private DateTime _crsWindowStart = DateTime.MinValue;
        private int _crsAppliedInWindow = 0;

        // Type-2 GC → COM INNER
        private DateTime _comInnerLastTick = DateTime.MinValue;
        private DateTime _comInnerWindowStart = DateTime.MinValue;
        private int _comInnerAppliedInWindow = 0;

        // Type-2 ANA → CRS1
        private DateTime _crs1LastTick = DateTime.MinValue;
        private DateTime _crs1WindowStart = DateTime.MinValue;
        private int _crs1AppliedInWindow = 0;

        // Type-2 ANA → CRS2
        private DateTime _crs2LastTick = DateTime.MinValue;
        private DateTime _crs2WindowStart = DateTime.MinValue;
        private int _crs2AppliedInWindow = 0;

        // Paràmetres (genèrics) — els que t'agraden
        private const int ACCEL_W1_CPS = 7;    // <7 cps → +1°
        private const int ACCEL_W2_CPS = 11;   // 7..10 cps → +2°
        private const int ACCEL_W3_CPS = 19;   // 11..18 cps → +4°
        private const int ACCEL_MAX_CPS = 22;  // ≥22 cps → +7°

        private const int ACCEL_STEP_L1 = 1;
        private const int ACCEL_STEP_L2 = 2;
        private const int ACCEL_STEP_L3 = 4;
        private const int ACCEL_STEP_L4 = 7;

        private const int ACCEL_WINDOW_MS = 230;      // finestra una mica més llarga
        private const int ACCEL_MAX_PER_WINDOW = 24;  // límit més natural

        private Image imglightOff;
        private Image imglightOn;
        private Image? _bgImage;
        private Image? trimWheelImg;

        // Multiplicador per HDG segons velocitat de gir (cps) + límit de finestra
        private int ComputeAccelMultiplier(ref DateTime lastTick, ref DateTime windowStart, ref int appliedInWindow)
        {
            var now = DateTime.UtcNow;
            int mult = ACCEL_STEP_L1;

            if (lastTick != DateTime.MinValue)
            {
                var dtMs = (now - lastTick).TotalMilliseconds;
                if (dtMs > 0)
                {
                    double cps = 1000.0 / dtMs;

                    if (cps >= ACCEL_MAX_CPS) mult = ACCEL_STEP_L4; // +7°
                    else if (cps >= ACCEL_W3_CPS) mult = ACCEL_STEP_L3; // +4°
                    else if (cps >= ACCEL_W2_CPS) mult = ACCEL_STEP_L2; // +2°
                    else mult = ACCEL_STEP_L1; // +1°
                }
            }

            if (windowStart == DateTime.MinValue || (now - windowStart).TotalMilliseconds > ACCEL_WINDOW_MS)
            {
                windowStart = now;
                appliedInWindow = 0;
            }

            if (appliedInWindow + mult > ACCEL_MAX_PER_WINDOW)
                mult = Math.Max(1, ACCEL_MAX_PER_WINDOW - appliedInWindow);

            appliedInWindow += mult;
            lastTick = now;
            return mult;
        }


        //fi acceladors



        private bool b1WasPressed_COM = false;

        private int previousNavSel = -1;
        private bool firstNavSelReceived = false;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private System.Windows.Forms.Timer hideTimer;
        private Struct1 lastData;

        private int nav1SwapCnt = 0; // comptador B5 (NAV1_SWAP)
        private int nav2SwapCnt = 0; // comptador B38 (NAV2_SWAP)

        enum DEFINITIONS
        {
            Struct1,
            XPNDR_SET,
        }

        enum REQUESTS
        {
            Request1,
        }
        enum SIMCONNECT_EVENTS
        {
            //rotAIR exclisive mode GC
            COM_PUSH,
            COM_SWITCH,
            COM_INNER_INC,
            COM_INNER_DEC,
            COM_OUTER_INC,
            COM_OUTER_DEC,
            NAV_PUSH,
            NAV_SWITCH,
            NAV_INNER_INC,
            NAV_INNER_DEC,
            NAV_OUTER_INC,
            NAV_OUTER_DEC,
            HDG_BUG_INC,
            HDG_BUG_DEC,
            HDG_SYNC,
            CRS_PUSH,
            CRS_INC,
            CRS_DEC,
            BARO_INC,
            BARO_DEC,
            BARO_STD,
            BARO_SET,
            CRSR_PFD_PUSH,
            FMS_PFD_INNER_INC,
            FMS_PFD_INNER_DEC,
            FMS_PFD_OUTER_INC,
            FMS_PFD_OUTER_DEC,
            CRSR_MDF_PUSH,
            FMS_MFD_INNER_INC,
            FMS_MFD_INNER_DEC,
            FMS_MFD_OUTER_INC,
            FMS_MFD_OUTER_DEC,
            PAN_PUSH,
            MFD_RANGE_INC,
            MFD_RANGE_DEC,
            MFD_PAN_LEFT,
            MFD_PAN_RIGHT,
            MFD_PAN_UP,
            MFD_PAN_DOWN,
            CDI,
            ALT_G1000_INNER_INC,
            ALT_G1000_INNER_DEC,
            ALT_G1000_OUTER_INC,
            ALT_G1000_OUTER_DEC,


            

            //rotAIR both: GC & ANA
            ADF_INNER_INC,
            ADF_INNER_DEC,
            ADF_OUTER_INC,
            ADF_OUTER_DEC,
            ADF_SWITCH,
            XPNDR_DIGIT_1_INC,
            XPNDR_DIGIT_1_DEC,
            XPNDR_DIGIT_2_INC,
            XPNDR_DIGIT_2_DEC,
            XPNDR_DIGIT_3_INC,
            XPNDR_DIGIT_3_DEC,
            XPNDR_DIGIT_4_INC,
            XPNDR_DIGIT_4_DEC,
            XPNDR_IDNT,

            //panelAIR1000
            //left keys PFD
            AP_MASTER,
            AP_HDG,
            AP_NAV,
            AP_APR,
            AP_VS,
            AP_FLC,
            AP_FD,
            AP_ALT,
            AP_VNV,
            AP_BC,
            AP_UP,
            AP_DN,
            //softkeys PFD
            PFD_SK_1,
            PFD_SK_2,
            PFD_SK_3,
            PFD_SK_4,
            PFD_SK_5,
            PFD_SK_6,
            PFD_SK_7,
            PFD_SK_8,
            PFD_SK_9,
            PFD_SK_10,
            PFD_SK_11,
            PFD_SK_12,
            //right keys PFD 
            PFD_D_TO,
            PFD_FPL,
            PFD_CLR,
            PFD_MENU,
            PFD_PROC,
            PFD_ENT,
            //audio
            AUDIO_COM1,
            AUDIO_COM1_MIC,
            AUDIO_COM2,
            AUDIO_COM2_MIC,
            AUDIO_SPKR,
            AUDIO_PA,
            AUDIO_HI_SENS,
            AUDIO_MKR_MUTE_ON,
            AUDIO_MKR_MUTE_OFF,
            AUDIO_NAV1,
            AUDIO_DME,
            AUDIO_NAV2,
            AUDIO_ADF,
            AUDIO_PLAY,
            AUDIO_MAN_SQ,
            AUDIO_PILOT,
            AUDIO_COPILOT,
            AUDIO_DISPLAY_BKUP_ON,
            AUDIO_DISPLAY_BKUP_OFF,
            SWITCH_BEACON,
            SWITCH_LAND,
            SWITCH_TAXI,
            SWITCH_NAV,
            SWITCH_STROBE,
            SWITCH_RECOGN,
            SWITCH_PANELLIGHT,
            SWITCH_DEICE,
            COWL_FLAP_L_SET,
            COWL_FLAP_R_SET,
            SWITCH_PITOTHEAT,
            SWITCH_FUELPUMP1,
            SWITCH_FUELPUMP2,
            SWITCH_PBRAKE,
            MASTER_BATTERY_ON,
            MASTER_BATTERY_OFF,
            AVIONICS_MASTER_1_ON,
            AVIONICS_MASTER_1_OFF,
            AVIONICS_MASTER_2_ON,
            AVIONICS_MASTER_2_OFF,
            TOGGLE_ALTERNATOR1,
            TOGGLE_ALTERNATOR2,
            TOGGLE_STARTER1,
            TOGGLE_STARTER2,
            TURBINE_IGNITION_SWITCH_TOGGLE,
            TURBINE_IGNITION_SWITCH_SET1,
            KINGAIR_IGN1_ARM,
            KINGAIR_IGN1_OFF,
            KINGAIR_IGN2_ARM,
            KINGAIR_IGN2_OFF,
            //softkeys MFD
            MFD_SK_1,
            MFD_SK_2,
            MFD_SK_3,
            MFD_SK_4,
            MFD_SK_5,
            MFD_SK_6,
            MFD_SK_7,
            MFD_SK_8,
            MFD_SK_9,
            MFD_SK_10,
            MFD_SK_11,
            MFD_SK_12,

            //right keys MFD 
            MFD_D_TO,
            MFD_FPL,
            MFD_CLR,
            MFD_MENU,
            MFD_PROC,
            MFD_ENT,


            FLAPS_SWITCH_UP,
            FLAPS_SWITCH_DN,
            TRIM_UP,
            TRIM_DN,
            RUDDER_TRIM_LEFT,
            RUDDER_TRIM_RIGHT,
            LGEAR_UP,
            LGEAR_DN,
            P,
            MAGNETO1_INCR,
            MAGNETO1_DECR,
            MAGNETO2_INCR,
            MAGNETO2_DECR,
            FUEL_SELECTOR_SET,
            FUEL_SELECTOR_2_SET,





        }
        enum SIMCONNECT_GROUPS
        {
            GROUP0
        }

        private const int BTN_RADIUS    = 12;
        private const int BTN_CLICK_W   = 85;
        private const int BTN_CLICK_H   = 65;
        private const int BTN_SHADOW_W  = 70;
        private const int BTN_SHADOW_H  = 50;
        private const int TRI_HALF_WIDTH = 14;
        private const int TRI_HEIGHT     = 6;



        private readonly List<UiSurface> uiSurfaces = new();
        private readonly System.Windows.Forms.Timer uiSurfaceTimer = new System.Windows.Forms.Timer { Interval = 30 };

        private sealed class UiButton
        {
            public string Name = "";
            public Point BasePos;                // CENTRE del botó
            public int W = BTN_CLICK_W;          // mida clickable (default G1000)
            public int H = BTN_CLICK_H;
            public DateTime PressedUntilUtc = DateTime.MinValue;
            public int PressVisualMs = 120;

            public Rectangle ClickRect  => new Rectangle(BasePos.X - W / 2,        BasePos.Y - H / 2,        W,      H);
            public Rectangle ShadowRect => new Rectangle(BasePos.X - (W - 15) / 2, BasePos.Y - (H - 15) / 2, W - 15, H - 15);

            public bool Resizable = false;
            public Rectangle ResizeHandle => Resizable
                ? new Rectangle(ClickRect.Right - 12, ClickRect.Bottom - 12, 16, 16)
                : Rectangle.Empty;

            public bool NoShadow = false;
            public bool IsVisuallyPressed(DateTime now) => !NoShadow && now < PressedUntilUtc;
            public Action? OnClick;
        }

        private sealed class UiSurface
        {
            public PictureBox Pb;
            public List<UiButton> Buttons = new();
            public Func<bool>? SuppressInput;
            public bool ShowAllAreas = false;

            public UiSurface(PictureBox pb)
            {
                Pb = pb;
                pb.MouseDown += OnMouseDown;
                pb.MouseMove += OnMouseMove;
                pb.Paint += OnPaint;
            }

            private void OnMouseDown(object? sender, MouseEventArgs e)
            {
                if (SuppressInput?.Invoke() == true) return;
                if (e.Button != MouseButtons.Left) return;

                foreach (var b in Buttons)
                {
                    if (b.ClickRect.Contains(e.Location))
                    {
                        b.PressedUntilUtc = DateTime.UtcNow.AddMilliseconds(b.PressVisualMs);
                        b.OnClick?.Invoke();
                        Pb.Invalidate();
                        Pb.Update();
                        break;
                    }
                }
            }

            private void OnMouseMove(object? sender, MouseEventArgs e)
            {
                if (SuppressInput?.Invoke() == true) return;
                Pb.Cursor = Buttons.Any(b => b.ClickRect.Contains(e.Location))
                    ? Cursors.Hand
                    : Cursors.Default;
            }

            private void OnPaint(object? sender, PaintEventArgs e)
            {
                using var brush = new SolidBrush(Color.FromArgb(90, Color.White));
                using var allAreasBrush = new SolidBrush(Color.FromArgb(60, Color.Gray));
                var now = DateTime.UtcNow;
                foreach (var b in Buttons)
                {
                    if (ShowAllAreas)
                    {
                        e.Graphics.FillRectangle(allAreasBrush, b.ClickRect);
                    }
                    else if (b.IsVisuallyPressed(now))
                    {
                        using var path = frmMain.CreateRoundedRect(b.ShadowRect, BTN_RADIUS);
                        e.Graphics.FillPath(brush, path);
                    }
                }
            }

            public void RefreshVisual() => Pb.Invalidate();
        }

        private sealed class AudioTriangle
        {
            public string Name = "";
            public Point BasePos;
            public Func<bool> IsActive = () => false;
        }

        private List<AudioTriangle> audioTriangles = new();

        private sealed class OverlayImage
        {
            public string Name    = "";
            public string TypeKey = "";
            public Point  BasePos;
            public float  ZoomWidth;
            public Image? Img;
            public float AspectRatio => (Img != null && Img.Height > 0) ? (float)Img.Width / Img.Height : 1f;
            public Rectangle Bounds => new Rectangle(BasePos.X, BasePos.Y,
                (int)ZoomWidth, (int)(ZoomWidth / AspectRatio));
            public Rectangle ResizeHandle => new Rectangle(
                Bounds.Right - 12, Bounds.Bottom - 12, 16, 16);
            public int AnimOffsetY = 0;
            public Rectangle AnimatedBounds => new Rectangle(
                BasePos.X, BasePos.Y + AnimOffsetY, (int)ZoomWidth, (int)(ZoomWidth / AspectRatio));
            public Func<bool>? ShowWhen;
            public Action?     OnClick;
        }
        private List<OverlayImage> overlayImages = new();

        private bool calibrationMode = false;
        private bool  _tactilebtnDragging = false;
        private Point _tactilebtnDragOffset;
        private bool  _closeBtnDragging = false;
        private Point _closeBtnDragOffset;
        private bool  _showAreasBtnDragging = false;
        private Point _showAreasBtnDragOffset;
        private UiButton? calibDragBtn = null;
        private AudioTriangle? calibDragTri = null;
        private UiButton? calibSelectedBtn = null;
        private AudioTriangle? calibSelectedTri = null;
        private OverlayImage? calibDragOverlay    = null;
        private OverlayImage? calibResizeOverlay  = null;
        private OverlayImage? calibSelectedOverlay = null;
        private UiButton? calibResizeBtn          = null;
        private int       calibResizeBtnStartW;
        private int       calibResizeBtnStartH;
        private int       calibResizeBtnStartMouseX;
        private int       calibResizeBtnStartMouseY;
        private Point calibDragOffset;
        private Point calibOverlayDragOffset;
        private float calibResizeStartWidth;
        private int   calibResizeStartMouseX;
        private readonly Rectangle calibExportBtnRect = new Rectangle(10, 10, 180, 50);
        private TextBox calibZoomBox = new TextBox();

        private int flapsAnimOffsetY = 0;
        private int flapsAnimDir     = 0;
        private int  lgearAnimOffsetY = 0;
        private bool lgearHandleUp   = false; // true=UP, false=DN — estat visual de la palanca
        private int flapsIndX        = 3335;
        private int flapsIndYOffset  = 0;
        private bool calibDragFlapsInd = false;
        private Point calibFlapsIndDragStart;
        private int   calibFlapsIndStartX;
        private int   calibFlapsIndStartYOff;
        private int trimIndX         = 3627;
        private int trimIndYOffset   = 0;
        private bool calibDragTrimInd = false;
        private Point calibTrimIndDragStart;
        private int   calibTrimIndStartX;
        private int   calibTrimIndStartYOff;
        private DateTime flapsPressStart   = DateTime.MinValue;
        private DateTime flapsReleaseStart = DateTime.MinValue;
        private int flapsOffsetAtRelease   = 0;


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct Struct1
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string title; // Aircraft title
            // DOUBLE (FLOAT64)
            public double gyro; // GYRO directional
            public double hdg; // HDG heading
            public double crs1; // COURSE 1
            public double crs2; // COURSE 2
            public double adf_act; // ADF ACTIVE frequency
            public double adf_stby; // ADF STANDBY frequency
            public double baro_in; // KOHLSMAN SETTING HG, "inHg"
            public double baro_hpa; // KOHLSMAN SETTING MB, "Millibars"
            public double alt_sel; // AUTOPILOT ALTITUDE LOCK VAR", "feet"
            public double displayBackupActive;
            public double elevator_trim;   // ELEVATOR TRIM INDICATOR (-1 .. +1)
            public double rudder_trim;     // RUDDER TRIM PCT / 100 (-1 .. +1)
            public double gear_center;
            public double gear_left;
            public double gear_right;
            public double main_bus_volts;

            // INT (INT32)
            public int xpdr_code;
            public int xpdr_state; // TRANSPONDER STATE:1
            public int xpdr_ident; // TRANSPONDER IDENT (Bool: 1 = active)
            public int cdi_scr; // GPS DRIVES NAV1, 1 = GPS, 0 = NAV
            public int nav_sel; // AUTOPILOT NAV1-2 SELECTED
            public int com_transmit_1;   // COM TRANSMIT:1 (Bool)
            public int com_transmit_2;   // COM TRANSMIT:2 (Bool)
            public int com_receive_all;
            public int dme_sound;   // DME audio enabled (Bool: 0/1)
            public int spkr_sound;
            public int mkr_mute;
            public int nav1_sound;
            public int adf_sound;
            public int nav2_sound;
            public int read_beacon;
            public int read_land;
            public int read_taxi;
            public int read_nav;
            public int read_strobe;
            public int read_recogn;
            public int read_panellight;
            public int read_deice;
            public int read_pitotheat;
            public int read_fuelpump1;
            public int read_fuelpump2;
            public int flaps_num_handle_positions;
            public int flaps_real_handle_position;
            public int read_parking_brake;
            public int gear_handle; // GEAR HANDLE POSITION (Bool: 0=UP, 1=DN)
            public int ign1_left;    // RECIP ENG LEFT MAGNETO:1
            public int ign1_right;   // RECIP ENG RIGHT MAGNETO:1
            public int ign1_starter; // GENERAL ENG STARTER:1
            public int ign2_left;
            public int ign2_right;
            public int ign2_starter;
            public int fuel_selector_1; // FUEL TANK SELECTOR:1 (enum)
            public int fuel_selector_2; // FUEL TANK SELECTOR:2 (enum)
            public int fuel_cross_feed; // FUEL CROSS FEED (Bool)
            public int num_engines;         // NUMBER OF ENGINES
            public double read_cowlflap_l;
            public double read_cowlflap_r;
            public int elec_master_battery; // ELECTRICAL MASTER BATTERY (Bool)
            public int avionics_master_1;   // AVIONICS MASTER SWITCH:1 (Bool)
            public int avionics_master_2;   // AVIONICS MASTER SWITCH:2 (Bool)
            public int gen_master_alt_1;    // GENERAL ENG MASTER ALTERNATOR:1 (Bool)
            public int gen_master_alt_2;    // GENERAL ENG MASTER ALTERNATOR:2 (Bool)
            public int starter1;            // GENERAL ENG STARTER:1
            public int starter2;            // GENERAL ENG STARTER:2



            public override bool Equals(object obj)
            {
                if (obj is Struct1 other)
                {
                    return this.hdg == other.hdg &&
                           this.crs1 == other.crs1 &&
                           this.crs2 == other.crs2 &&
                           this.adf_act == other.adf_act &&
                           this.adf_stby == other.adf_stby &&
                           this.baro_in == other.baro_in &&
                           this.baro_hpa == other.baro_hpa &&
                           this.alt_sel == other.alt_sel &&
                           this.xpdr_code == other.xpdr_code &&
                           this.xpdr_state == other.xpdr_state &&
                           this.xpdr_ident == other.xpdr_ident &&
                           this.cdi_scr == other.cdi_scr &&
                           this.nav_sel == other.nav_sel;
                }
                return false;
            }




        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct XPNDRWrite
        {
            public int xpdr_state;   // 0=OFF,1=STBY,2=TEST,3=ON,4=ALT
        }
        public frmMain()
        {
            InitializeComponent();

            imglightOff = panelAIR1000.Properties.Resources.light_off;
            imglightOn = panelAIR1000.Properties.Resources.light_on;
            trimWheelImg = LoadOverlayImage("trim wheel.png");

            string imagePath = System.IO.Path.Combine(Application.StartupPath, "Resources", "main GA_1000 4K panel_f.png");
            if (System.IO.File.Exists(imagePath))
                _bgImage = Image.FromFile(imagePath);

            pbMain.Paint += PbMain_Paint;
            pbMain.MouseDown += PbMain_MouseDown;
            pbMain.MouseMove += PbMain_MouseMove;
            pbMain.MouseUp += PbMain_MouseUp;

            mainSurface = new UiSurface(pbMain);
            uiSurfaces.Add(mainSurface);
            mainSurface.SuppressInput = () => calibrationMode;

            this.KeyPreview = true;
            this.KeyDown += (s, e) => { if (e.KeyCode == Keys.F12) { calibrationMode = !calibrationMode; calibSelectedBtn = null; calibSelectedTri = null; calibSelectedOverlay = null; calibZoomBox.Visible = false; pbMain.Invalidate(); } };
            this.KeyDown += frmMain_KeyDown;

            calibZoomBox.Visible = false;
            calibZoomBox.Width   = 60;
            calibZoomBox.Font    = new Font("Arial", 10);
            calibZoomBox.KeyDown += (s, e) => {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape) {
                    if (e.KeyCode == Keys.Enter && calibSelectedOverlay != null
                        && int.TryParse(calibZoomBox.Text, out int w) && w > 10)
                    {
                        string tk = calibSelectedOverlay.TypeKey;
                        foreach (var ov in overlayImages.Where(o => o.TypeKey == tk))
                            ov.ZoomWidth = w;
                        try { ExportCalibrationPositions(silent: true); } catch { }
                        pbMain.Invalidate();
                    }
                    calibZoomBox.Visible = false;
                    e.Handled = true; e.SuppressKeyPress = true;
                }
            };
            this.Controls.Add(calibZoomBox);
            calibZoomBox.BringToFront();

            audioTriangles = new List<AudioTriangle>
            {
                new() { Name="COM1_tx",  BasePos=new Point(1867, 22),  IsActive=()=>com1Active },
                new() { Name="COM1_rx",  BasePos=new Point(1970, 22),  IsActive=()=>com1Active||comReceiveAll },
                new() { Name="COM2_tx",  BasePos=new Point(1867, 100), IsActive=()=>com2Active },
                new() { Name="COM2_rx",  BasePos=new Point(1970, 100), IsActive=()=>com2Active||comReceiveAll },
                new() { Name="DME",      BasePos=new Point(1867, 484), IsActive=()=>dmeActive },
                new() { Name="NAV1",     BasePos=new Point(1970, 484), IsActive=()=>nav1Active },
                new() { Name="NAV2",     BasePos=new Point(1970, 562), IsActive=()=>nav2Active },
                new() { Name="SPKR",     BasePos=new Point(1970, 330), IsActive=()=>spkrActive },
                new() { Name="MKR_MUT",  BasePos=new Point(1867, 402), IsActive=()=>mkrMute },
                new() { Name="ADF_MUTE", BasePos=new Point(1867, 562), IsActive=()=>adfMute },
            };

            var screen4k = Screen.AllScreens.FirstOrDefault(s => !s.Primary && s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                ?? Screen.AllScreens.First(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = screen4k.Bounds.Location;
            this.Size = screen4k.Bounds.Size;

            ApplyPfdRegion();
            this.TopMost = false;

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { System.IO.File.WriteAllText("panelAIR1000_crash.txt", e.ExceptionObject.ToString()); } catch { }
            };
            Application.ThreadException += (s, e) =>
            {
                try { System.IO.File.AppendAllText("panelAIR1000_crash.txt", "\r\n[UI] " + e.Exception.ToString()); } catch { }
            };

            this.Icon = Resources.logo;

            _tactileBtn = new System.Windows.Forms.Button
            {
                Text = "🖐  Make this screen touch",
                Location = new Point(3477, 1230),
                Size = new Size(363, 63),
                BackColor = Color.Black,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 17.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _tactileBtn.FlatAppearance.BorderSize = 2;
            _tactileBtn.FlatAppearance.BorderColor = Color.White;
            _tactileBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = "shell:::{80F3F1D5-FECA-45F3-BC32-752C152E456E}",
                    UseShellExecute = true
                });
            };
            _tactileBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _tactilebtnDragging = true;
                _tactilebtnDragOffset = e.Location;
            };
            _tactileBtn.MouseMove += (s, e) =>
            {
                if (!_tactilebtnDragging) return;
                var screenPt = _tactileBtn.PointToScreen(e.Location);
                var formPt = this.PointToClient(screenPt);
                _tactileBtn.Location = new Point(formPt.X - _tactilebtnDragOffset.X, formPt.Y - _tactilebtnDragOffset.Y);
            };
            _tactileBtn.MouseUp += (s, e) => { _tactilebtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            this.Controls.Add(_tactileBtn);
            _tactileBtn.BringToFront();

            closeButton.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _closeBtnDragging = true;
                _closeBtnDragOffset = e.Location;
            };
            closeButton.MouseMove += (s, e) =>
            {
                if (!_closeBtnDragging) return;
                var screenPt = closeButton.PointToScreen(e.Location);
                var formPt = this.PointToClient(screenPt);
                closeButton.Location = new Point(formPt.X - _closeBtnDragOffset.X, formPt.Y - _closeBtnDragOffset.Y);
            };
            closeButton.MouseUp += (s, e) => { _closeBtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };

            _showAreasBtn = new System.Windows.Forms.Button
            {
                Text = "👆  show the clickable areas",
                Location = new Point(3477, 1170),
                Size = new Size(363, 63),
                BackColor = Color.DimGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 17.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            _showAreasBtn.FlatAppearance.BorderSize = 0;
            _showAreasBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                mainSurface.ShowAllAreas = !mainSurface.ShowAllAreas;
                _showAreasBtn.BackColor = mainSurface.ShowAllAreas ? Color.LimeGreen : Color.DimGray;
                pbMain.Invalidate();
            };
            _showAreasBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _showAreasBtnDragging = true;
                _showAreasBtnDragOffset = e.Location;
            };
            _showAreasBtn.MouseMove += (s, e) =>
            {
                if (!_showAreasBtnDragging) return;
                var screenPt = _showAreasBtn.PointToScreen(e.Location);
                var formPt = this.PointToClient(screenPt);
                _showAreasBtn.Location = new Point(formPt.X - _showAreasBtnDragOffset.X, formPt.Y - _showAreasBtnDragOffset.Y);
            };
            _showAreasBtn.MouseUp += (s, e) => { _showAreasBtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            this.Controls.Add(_showAreasBtn);
            _showAreasBtn.BringToFront();

            //this.TopMost = true;
            this.Load += frmMain_Load;
            //EnableFormDragging(this);
            // EnableFormDragging: pendent afegir icona de moviment sobre pbMain



            // Definició botons virtuals — BasePos = CENTRE del botó (85×65 per defecte)
            var pfdLeft = mainSurface;
            pfdLeft.Buttons.Add(new UiButton { Name = "NAV_SWAP", BasePos = new Point(157, 147), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.NAV_SWITCH,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_NAV_SWAP", BasePos = new Point(2157, 147), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.NAV_SWITCH, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "AP",       BasePos = new Point(62,  602), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_MASTER,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_AP", BasePos = new Point(2126, 606), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_MASTER, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_HDG", BasePos = new Point(2121, 664), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_HDG,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_NAV", BasePos = new Point(2122, 734), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_NAV,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_APR", BasePos = new Point(2122, 803), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_APR,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_VS",  BasePos = new Point(2121, 872), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_VS,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_FLC", BasePos = new Point(2120, 938), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_FLC,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_FD",  BasePos = new Point(2210, 598), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_FD,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_ALT", BasePos = new Point(2209, 665), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_ALT,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_VNV", BasePos = new Point(2209, 734), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_VNV,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_BC",  BasePos = new Point(2210, 803), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_BC,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_UP",  BasePos = new Point(2209, 872), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_UP,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "MFD_DN",  BasePos = new Point(2209, 938), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_DN,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "HDG",      BasePos = new Point(62,  664), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_HDG,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "NAV",      BasePos = new Point(62,  727), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_NAV,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "APR",      BasePos = new Point(62,  792), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_APR,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "VS",       BasePos = new Point(62,  856), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_VS,       0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "FLC",      BasePos = new Point(62,  919), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_FLC,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "FD",       BasePos = new Point(154, 602), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_FD,       0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "ALT",      BasePos = new Point(154, 664), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_ALT,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "VNV",      BasePos = new Point(154, 727), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_VNV,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "BC",       BasePos = new Point(154, 792), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_BC,       0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "UP",       BasePos = new Point(154, 856), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_UP,       0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdLeft.Buttons.Add(new UiButton { Name = "DN",       BasePos = new Point(154, 919), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_DN,       0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });

            var pfdBottom = mainSurface;
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_1",  BasePos = new Point(300,  1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_1,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_2",  BasePos = new Point(408,  1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_2,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_3",  BasePos = new Point(517,  1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_3,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_4",  BasePos = new Point(625,  1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_4,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_5",  BasePos = new Point(736,  1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_5,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_6",  BasePos = new Point(845,  1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_6,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_7",  BasePos = new Point(954,  1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_7,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_8",  BasePos = new Point(1063, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_8,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_9",  BasePos = new Point(1172, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_9,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_10", BasePos = new Point(1280, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_10, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_11", BasePos = new Point(1388, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_11, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdBottom.Buttons.Add(new UiButton { Name = "PFD_softkey_12", BasePos = new Point(1497, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_SK_12, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });

            var pfdRight = mainSurface;
            pfdRight.Buttons.Add(new UiButton { Name = "PFD_COM_swap",  BasePos = new Point(1638, 147), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COM_SWITCH, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdRight.Buttons.Add(new UiButton { Name = "PFD_direct_to", BasePos = new Point(1643, 792), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_D_TO,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdRight.Buttons.Add(new UiButton { Name = "PFD_flp",       BasePos = new Point(1643, 857), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_FPL,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdRight.Buttons.Add(new UiButton { Name = "PFD_clr",       BasePos = new Point(1643, 922), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_CLR,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdRight.Buttons.Add(new UiButton { Name = "PFD_menu",      BasePos = new Point(1735, 792), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_MENU,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdRight.Buttons.Add(new UiButton { Name = "PFD_prog",      BasePos = new Point(1735, 857), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_PROC,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdRight.Buttons.Add(new UiButton { Name = "PFD_ent",       BasePos = new Point(1735, 922), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PFD_ENT,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });

            var pfdAudio = mainSurface;
            pfdAudio.Buttons.Add(new UiButton { Name = "COM1_mic",    BasePos = new Point(1867, 57),   OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_COM1_MIC,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "COM1",        BasePos = new Point(1972, 57),   OnClick = () => { if (!com1Tx) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_COM1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            pfdAudio.Buttons.Add(new UiButton { Name = "COM2_mic",    BasePos = new Point(1867, 137),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_COM2_MIC,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "COM2",        BasePos = new Point(1972, 137),  OnClick = () => { if (!com2Tx) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_COM2, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            pfdAudio.Buttons.Add(new UiButton { Name = "SPKR",        BasePos = new Point(1972, 364),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_SPKR,         0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "PA",          BasePos = new Point(1867, 364),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_PA,           0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "HI_SENS",     BasePos = new Point(1972, 442),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_HI_SENS,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "MKR_MUT",     BasePos = new Point(1867, 442),  OnClick = () => { if (!MKRmute_bool) { simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_MKR_MUTE_ON, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); MKRmute_bool = true; } else { simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_MKR_MUTE_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); MKRmute_bool = false; } } });
            pfdAudio.Buttons.Add(new UiButton { Name = "NAV1",        BasePos = new Point(1972, 520),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_NAV1,         0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "DME",         BasePos = new Point(1867, 520),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_DME,          0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "NAV2",        BasePos = new Point(1972, 598),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_NAV2,         0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "ADF",         BasePos = new Point(1867, 598),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_ADF,          0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "PLAY",        BasePos = new Point(1972, 754),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_PLAY,         0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "MAN_SQ",      BasePos = new Point(1867, 754),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_MAN_SQ,       0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "PILOT",       BasePos = new Point(1972, 832),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_PILOT,        0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "COPILOT",     BasePos = new Point(1867, 832),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_COPILOT,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "DISPLAY_BKUP", BasePos = new Point(1920, 1082), W = 60, H = 60, OnClick = () => { if (!displayBackupActive_bool) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_DISPLAY_BKUP_ON, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); else simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_DISPLAY_BKUP_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });

            var mfdBottom = mainSurface;
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_1",  BasePos = new Point(2340, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_1,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_2",  BasePos = new Point(2448, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_2,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_3",  BasePos = new Point(2557, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_3,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_4",  BasePos = new Point(2665, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_4,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_5",  BasePos = new Point(2776, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_5,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_6",  BasePos = new Point(2885, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_6,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_7",  BasePos = new Point(2994, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_7,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_8",  BasePos = new Point(3103, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_8,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_9",  BasePos = new Point(3212, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_9,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_10", BasePos = new Point(3320, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_10, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_11", BasePos = new Point(3428, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_11, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdBottom.Buttons.Add(new UiButton { Name = "MFD_softkey_12", BasePos = new Point(3537, 1088), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_SK_12, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });

            var mfdRight = mainSurface;
            mfdRight.Buttons.Add(new UiButton { Name = "MFD_COM_swap",  BasePos = new Point(3678, 147), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COM_SWITCH, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdRight.Buttons.Add(new UiButton { Name = "MFD_direct_to", BasePos = new Point(3683, 792), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_D_TO,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdRight.Buttons.Add(new UiButton { Name = "MFD_flp",       BasePos = new Point(3683, 857), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_FPL,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdRight.Buttons.Add(new UiButton { Name = "MFD_clr",       BasePos = new Point(3683, 922), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_CLR,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdRight.Buttons.Add(new UiButton { Name = "MFD_menu",      BasePos = new Point(3775, 792), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_MENU,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdRight.Buttons.Add(new UiButton { Name = "MFD_prog",      BasePos = new Point(3775, 857), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_PROC,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            mfdRight.Buttons.Add(new UiButton { Name = "MFD_ent",       BasePos = new Point(3775, 922), OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_ENT,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });

            var cockpitRight = mainSurface;
            cockpitRight.Buttons.Add(new UiButton { Name = "FLAPS-UP",   BasePos = new Point(3450, 1646), W = 110, H = 115, Resizable = true, NoShadow = true, OnClick = () => { flapsAnimDir = -1; pbMain.Invalidate(); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FLAPS_SWITCH_UP,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); Task.Delay(150).ContinueWith(_ => this.Invoke(() => { flapsAnimDir = 0; pbMain.Invalidate(); })); } });
            cockpitRight.Buttons.Add(new UiButton { Name = "FLAPS-DN",   BasePos = new Point(3450, 1776), W = 110, H = 115, Resizable = true, NoShadow = true, OnClick = () => { flapsAnimDir =  1; pbMain.Invalidate(); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FLAPS_SWITCH_DN,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); Task.Delay(150).ContinueWith(_ => this.Invoke(() => { flapsAnimDir = 0; pbMain.Invalidate(); })); } });
            cockpitRight.Buttons.Add(new UiButton { Name="LGEAR-UP", BasePos=new Point(2843, 1590), W=110, H=115, Resizable=true, NoShadow=true,
                OnClick = () => {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.LGEAR_UP, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="LGEAR-DN", BasePos=new Point(2843, 1750), W=110, H=115, Resizable=true, NoShadow=true,
                OnClick = () => {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.LGEAR_DN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // Avionics switches — single toggle area each
            cockpitRight.Buttons.Add(new UiButton { Name="AVIONICS1-TOGGLE", BasePos=new Point(1770, 1220), W=100, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    var ev = avionics1Off ? SIMCONNECT_EVENTS.AVIONICS_MASTER_1_ON : SIMCONNECT_EVENTS.AVIONICS_MASTER_1_OFF;
                    simconnect?.TransmitClientEvent(0, ev, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="AVIONICS2-TOGGLE", BasePos=new Point(1970, 1220), W=100, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    var ev = avionics2Off ? SIMCONNECT_EVENTS.AVIONICS_MASTER_2_ON : SIMCONNECT_EVENTS.AVIONICS_MASTER_2_OFF;
                    simconnect?.TransmitClientEvent(0, ev, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // ALT/GEN switch 1 — single toggle area
            cockpitRight.Buttons.Add(new UiButton { Name="ALTGEN1-TOGGLE", BasePos=new Point(1420, 1220), W=100, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // ALT/GEN switch 2 — single toggle area
            cockpitRight.Buttons.Add(new UiButton { Name="ALTGEN2-TOGGLE", BasePos=new Point(1620, 1220), W=100, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR2, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // Master Battery switch — single toggle area
            cockpitRight.Buttons.Add(new UiButton { Name="BATTERY-TOGGLE", BasePos=new Point(1320, 1020), W=100, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    var ev = masterBatteryOff ? SIMCONNECT_EVENTS.MASTER_BATTERY_ON : SIMCONNECT_EVENTS.MASTER_BATTERY_OFF;
                    simconnect?.TransmitClientEvent(0, ev, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="P-BRAKE", BasePos=new Point(2843, 1820), W=110, H=115, Resizable=true, NoShadow=true,
                OnClick = () => {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_PBRAKE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // IGN1 rotate right (clockwise = INCR)
            cockpitRight.Buttons.Add(new UiButton { Name="IGN1-RIGHT", BasePos=new Point(1970, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (ign1Pos >= 4) return;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MAGNETO1_INCR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // IGN1 rotate left (counter-clockwise = DECR)
            cockpitRight.Buttons.Add(new UiButton { Name="IGN1-LEFT", BasePos=new Point(1880, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (ign1Pos <= 0) return;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MAGNETO1_DECR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="IGN2-RIGHT", BasePos=new Point(2170, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (ign2Pos >= 4) return;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MAGNETO2_INCR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="IGN2-LEFT", BasePos=new Point(2080, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (ign2Pos <= 0) return;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MAGNETO2_DECR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // SW_FUEL1 — left and right areas
            cockpitRight.Buttons.Add(new UiButton { Name="FUEL1-LEFT", BasePos=new Point(1620, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (fuel1Pos <= 0) return;
                    int newPos = fuel1Pos - 1;
                    if (numEngines >= 2) {
                        uint[] fuel1Enum = { 0, 2, 3 };
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, fuel1Enum[newPos], SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    } else {
                        uint msfsVal = SingleEngineFuelVal(newPos, fuel2Pos);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, msfsVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    }
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="FUEL1-RIGHT", BasePos=new Point(1720, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (fuel1Pos >= 2) return;
                    int newPos = fuel1Pos + 1;
                    if (numEngines >= 2) {
                        uint[] fuel1Enum = { 0, 2, 3 };
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, fuel1Enum[newPos], SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    } else {
                        uint msfsVal = SingleEngineFuelVal(newPos, fuel2Pos);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, msfsVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    }
                    pbMain.Invalidate();
                }});
            // SW_FUEL2 — left and right areas
            cockpitRight.Buttons.Add(new UiButton { Name="FUEL2-LEFT", BasePos=new Point(2120, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (fuel2Pos <= 0) return;
                    int newPos = fuel2Pos - 1;
                    if (numEngines >= 2) {
                        uint[] fuel2Enum = { 13, 3, 0 };
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_2_SET, fuel2Enum[newPos], SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    } else {
                        uint msfsVal = SingleEngineFuelVal(fuel1Pos, newPos);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, msfsVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    }
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="FUEL2-RIGHT", BasePos=new Point(2220, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (fuel2Pos >= 2) return;
                    int newPos = fuel2Pos + 1;
                    if (numEngines >= 2) {
                        uint[] fuel2Enum = { 13, 3, 0 };
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_2_SET, fuel2Enum[newPos], SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    } else {
                        uint msfsVal = SingleEngineFuelVal(fuel1Pos, newPos);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, msfsVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    }
                    pbMain.Invalidate();
                }});

            cockpitRight.Buttons.Add(new UiButton { Name="BTN_STARTER1", BasePos=new Point(1564, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_STARTER1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_IGNITION1", BasePos=new Point(1624, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => simconnect?.TransmitClientEvent(0,
                    ignition1Active ? SIMCONNECT_EVENTS.KINGAIR_IGN1_OFF : SIMCONNECT_EVENTS.KINGAIR_IGN1_ARM,
                    0u, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_STARTER2", BasePos=new Point(1684, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_STARTER2, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_IGNITION2", BasePos=new Point(1744, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => simconnect?.TransmitClientEvent(0,
                    ignition2Active ? SIMCONNECT_EVENTS.KINGAIR_IGN2_OFF : SIMCONNECT_EVENTS.KINGAIR_IGN2_ARM,
                    0u, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_PANELLIGHT", BasePos=new Point(1924, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_PANELLIGHT, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_DEICE", BasePos=new Point(1984, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_DEICE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_COWLFLAP_L_INC", BasePos=new Point(1500, 1560), W=75, H=40, Resizable=true, NoShadow=true,
                OnClick = () => { uint target = cowlFlapL == 0 ? 8191u : 16383u;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COWL_FLAP_L_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_COWLFLAP_L_DEC", BasePos=new Point(1500, 1640), W=75, H=40, Resizable=true, NoShadow=true,
                OnClick = () => { uint target = cowlFlapL == 2 ? 8191u : 0u;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COWL_FLAP_L_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_COWLFLAP_R_INC", BasePos=new Point(1600, 1560), W=75, H=40, Resizable=true, NoShadow=true,
                OnClick = () => { uint target = cowlFlapR == 0 ? 8191u : 16383u;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COWL_FLAP_R_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_COWLFLAP_R_DEC", BasePos=new Point(1600, 1640), W=75, H=40, Resizable=true, NoShadow=true,
                OnClick = () => { uint target = cowlFlapR == 2 ? 8191u : 0u;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COWL_FLAP_R_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });


            var swWhiteImg = LoadOverlayImage("SW DN white.png");
            var swGreenImg = LoadOverlayImage("SW DN green.png");
            var swBlueImg  = LoadOverlayImage("SW DN blue.png");
            var ledGreenImg = LoadOverlayImage("led green.png");
            var ledRedImg   = LoadOverlayImage("led red.png");
            var swLgearImg  = LoadOverlayImage("SW landing gear.png");
            var swPbrakeImg  = LoadOverlayImage("Parking Brake switch_b.png");
            var swIgnitionImg = LoadOverlayImage("SW ignition.png");
            var swFuelImg     = LoadOverlayImage("SW fuel_r.png");
            var swBatteryImg  = LoadOverlayImage("SW battery.png");
            var swAvionicsImg = LoadOverlayImage("SW avionics.png");
            var swDnRedImg    = LoadOverlayImage("SW DN red.png");
            var swCenterWhiteImg = Image.FromFile(@"C:\Users\usuario\Desktop\panelAIR1000-j\panelAIR1000\CSharpSimConnect\Resources\SW CENTER white_b.png");
            overlayImages = new List<OverlayImage>
            {
                // Fila superior (Y=1553) — blanc — ordre E-D: beacon, land, taxi, nav, strobe, recognition
                new() { Name="SW_DN_BEACON",    TypeKey="SW DN white", BasePos=new Point(1564, 1553), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !beaconActive,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_BEACON,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="SW_DN_LAND",      TypeKey="SW DN white", BasePos=new Point(1624, 1553), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !landActive,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_LAND,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="SW_DN_TAXI",      TypeKey="SW DN white", BasePos=new Point(1684, 1553), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !taxiActive,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_TAXI,      0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="SW_DN_NAV",       TypeKey="SW DN white", BasePos=new Point(1744, 1553), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !navActive,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_NAV,       0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="SW_DN_STROBE",    TypeKey="SW DN white", BasePos=new Point(1804, 1553), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !strobeActive,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_STROBE,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="SW_DN_RECOGN",    TypeKey="SW DN white", BasePos=new Point(1864, 1553), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !recognActive,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_RECOGN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="SW_DN_STARTER1",  TypeKey="SW_DN_STARTER1",  BasePos=new Point(1564, 1653), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !starter1Active, OnClick=null },
                new() { Name="SW_DN_IGNITION1", TypeKey="SW_DN_IGNITION1", BasePos=new Point(1624, 1653), ZoomWidth=75, Img=swWhiteImg, ShowWhen=null, OnClick=null },
                new() { Name="SW_DN_STARTER2",  TypeKey="SW_DN_STARTER2",  BasePos=new Point(1684, 1653), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !starter2Active, OnClick=null },
                new() { Name="SW_DN_IGNITION2", TypeKey="SW_DN_IGNITION2", BasePos=new Point(1744, 1653), ZoomWidth=75, Img=swWhiteImg, ShowWhen=null, OnClick=null },
                new() { Name="SW_DN_PANELLIGHT", TypeKey="SW DN white", BasePos=new Point(1924, 1653), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !panelLightActive,
                        OnClick  = null },
                new() { Name="SW_DN_DEICE", TypeKey="SW DN green", BasePos=new Point(1984, 1653), ZoomWidth=75, Img=swGreenImg,
                        ShowWhen = () => !deiceActive,
                        OnClick  = null },
                // Cowl Flap L
                new() { Name="SW_CN_COWLFLAP_L", TypeKey="SW CN cowlflap", BasePos=new Point(1500, 1600), ZoomWidth=75,
                        Img=swCenterWhiteImg, ShowWhen=() => cowlFlapL == 1, OnClick=null },
                new() { Name="SW_DN_COWLFLAP_L", TypeKey="SW DN cowlflap", BasePos=new Point(1500, 1600), ZoomWidth=75,
                        Img=swWhiteImg,       ShowWhen=() => cowlFlapL == 2, OnClick=null },
                // Cowl Flap R
                new() { Name="SW_CN_COWLFLAP_R", TypeKey="SW CN cowlflap", BasePos=new Point(1600, 1600), ZoomWidth=75,
                        Img=swCenterWhiteImg, ShowWhen=() => cowlFlapR == 1, OnClick=null },
                new() { Name="SW_DN_COWLFLAP_R", TypeKey="SW DN cowlflap", BasePos=new Point(1600, 1600), ZoomWidth=75,
                        Img=swWhiteImg,       ShowWhen=() => cowlFlapR == 2, OnClick=null },
                // Verd — pitot heat, 60px a la dreta de recognition
                new() { Name="SW_DN_PITOTHEAT", TypeKey="SW DN green", BasePos=new Point(1924, 1553), ZoomWidth=75, Img=swGreenImg,
                        ShowWhen = () => !pitotheatActive,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_PITOTHEAT, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                // Blau — pumps sota de strobe i recognition (Y+100)
                new() { Name="SW_DN_FUELPUMP1", TypeKey="SW DN blue",  BasePos=new Point(1804, 1653), ZoomWidth=75, Img=swBlueImg,
                        ShowWhen = () => !fuelpump1Active,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_FUELPUMP1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="SW_DN_FUELPUMP2", TypeKey="SW DN blue",  BasePos=new Point(1864, 1653), ZoomWidth=75, Img=swBlueImg,
                        ShowWhen = () => !fuelpump2Active,
                        OnClick  = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_FUELPUMP2, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                // Flaps switch
                new() { Name="SW_FLAPS", TypeKey="SW flaps", BasePos=new Point(3000, 1500), ZoomWidth=75,
                        Img=LoadOverlayImage("SW flaps.png"),
                        ShowWhen = null, OnClick = null },
                // Landing gear switch
                new() { Name="SW_LGEAR", TypeKey="SW lgear", BasePos=new Point(2843, 1620), ZoomWidth=75,
                        Img=swLgearImg, ShowWhen=null, OnClick=null },
                // Parking brake switch
                new() { Name="SW_PBRAKE", TypeKey="SW pbrake", BasePos=new Point(2843, 1755), ZoomWidth=56,
                        Img=swPbrakeImg, ShowWhen=null, OnClick=null },
                // Ignition switch
                new() { Name="SW_IGNITION",  TypeKey="SW ignition",  BasePos=new Point(1920, 1080), ZoomWidth=100,
                        Img=swIgnitionImg, ShowWhen=null, OnClick=null },
                new() { Name="SW_IGNITION2", TypeKey="SW ignition2", BasePos=new Point(2120, 1080), ZoomWidth=100,
                        Img=swIgnitionImg, ShowWhen=null, OnClick=null },
                new() { Name="SW_FUEL1", TypeKey="SW fuel1", BasePos=new Point(1670, 1080), ZoomWidth=100,
                        Img=swFuelImg, ShowWhen=null, OnClick=null },
                new() { Name="SW_FUEL2", TypeKey="SW fuel2", BasePos=new Point(2170, 1080), ZoomWidth=100,
                        Img=swFuelImg, ShowWhen=null, OnClick=null },
                new() { Name="SW_BATTERY",    TypeKey="SW battery",    BasePos=new Point(1320, 1080), ZoomWidth=100,
                        Img=swBatteryImg,  ShowWhen = () => masterBatteryOff, OnClick=null },
                new() { Name="SW_AVIONICS1",  TypeKey="SW avionics1",  BasePos=new Point(1770, 1280), ZoomWidth=100,
                        Img=swAvionicsImg, ShowWhen = () => avionics1Off,     OnClick=null },
                new() { Name="SW_AVIONICS2",  TypeKey="SW avionics2",  BasePos=new Point(1970, 1280), ZoomWidth=100,
                        Img=swAvionicsImg, ShowWhen = () => avionics2Off,     OnClick=null },
                new() { Name="SW_ALT1", TypeKey="SW altgen1", BasePos=new Point(1420, 1280), ZoomWidth=100,
                        Img=swDnRedImg, ShowWhen = () => altGen1Off, OnClick=null },
                new() { Name="SW_ALT2", TypeKey="SW altgen2", BasePos=new Point(1620, 1280), ZoomWidth=100,
                        Img=swDnRedImg, ShowWhen = () => altGen2Off, OnClick=null },
                new() { Name="LED_PBRAKE", TypeKey="LED_PBRAKE", BasePos=new Point(2918, 1755), ZoomWidth=32, Img=ledRedImg,
                        ShowWhen = () => pbrakeActive },
                // GEAR LEDs
                new() { Name="LED_GEAR_LEFT",    TypeKey="LED_GEAR_LEFT",    BasePos=new Point(2749, 1740), ZoomWidth=43, Img=ledGreenImg,
                        ShowWhen = () => mainBusVolts >= 5.0 && gearLeftPercent  >= 100 },
                new() { Name="LED_GEAR_NOSE",    TypeKey="LED_GEAR_NOSE",    BasePos=new Point(2843, 1605), ZoomWidth=43, Img=ledGreenImg,
                        ShowWhen = () => mainBusVolts >= 5.0 && gearNosePercent  >= 100 },
                new() { Name="LED_GEAR_RIGHT",   TypeKey="LED_GEAR_RIGHT",   BasePos=new Point(2918, 1740), ZoomWidth=43, Img=ledGreenImg,
                        ShowWhen = () => mainBusVolts >= 5.0 && gearRightPercent >= 100 },
                new() { Name="LED_GEAR_TRANSIT", TypeKey="LED_GEAR_TRANSIT", BasePos=new Point(2843, 1585), ZoomWidth=32, Img=ledRedImg,
                        ShowWhen = () => mainBusVolts >= 5.0 &&
                                         ((gearLeftPercent  > 0 && gearLeftPercent  < 100) ||
                                          (gearNosePercent  > 0 && gearNosePercent  < 100) ||
                                          (gearRightPercent > 0 && gearRightPercent < 100)) },
            };

            LoadCalibrationPositions();

            // Timer global per apagar ombres + animació flaps SW
            uiSurfaceTimer.Tick += (s, e) =>
            {
                foreach (var srf in uiSurfaces)
                    srf.RefreshVisual();


            };

            uiSurfaceTimer.Start();



        }
        private void closeButton_Click(object sender, EventArgs e)
        {
            if (calibrationMode) return;
            this.Close(); // Això dispara Form1_FormClosing i para els threads
        }

        private void ApplyPfdRegion()
        {
            // Regió completa del Form
            Region formRegion = new Region(new Rectangle(
                0, 0, this.Width, this.Height));

            // FORAT PFD
            Rectangle pfdHole = new Rectangle(
                238,   // X
                47,    // Y
                1323,  // Width
                992    // Height
            );
            formRegion.Exclude(pfdHole);

            // FORAT MFD
            Rectangle mfdHole = new Rectangle(
                2278,  // X
                47,    // Y
                1323,  // Width
                992    // Height
            );
            formRegion.Exclude(mfdHole);

            // Aplica la regió al Form
            this.Region = formRegion;
        }



        private bool HasDataChanged(Struct1 newData)
        {
            return !newData.Equals(lastData);
        }
        private void frmMain_Load(object sender, EventArgs e)
        {
            // Level 1 — 4K screen check
            bool has4K = Screen.AllScreens.Any(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160);
            if (!has4K)
            {
                MessageBox.Show("A 4K screen (3840x2160) is required to run this application.", "panelAIR1000", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
                return;
            }

            // Level 2 — Touch screen HID check (VID=0457, PID=0819)
            bool hasPanel = false;
            using (var searcher = new System.Management.ManagementObjectSearcher(
                @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%VID_0457%PID_0819%' AND ConfigManagerErrorCode = 0 AND Present = TRUE"))
            {
                hasPanel = searcher.Get().Count > 0;
            }
            if (!hasPanel)
            {
                var screen4K = Screen.AllScreens.FirstOrDefault(s => !s.Primary && s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                    ?? Screen.AllScreens.First(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160);
                using (var msgHelper = new Form { StartPosition = FormStartPosition.Manual, TopMost = true, Size = new Size(1, 1), ShowInTaskbar = false })
                {
                    msgHelper.Location = new Point(screen4K.Bounds.X + screen4K.Bounds.Width / 2, screen4K.Bounds.Y + screen4K.Bounds.Height / 2);
                    msgHelper.Show();
                    MessageBox.Show(msgHelper, "No panelAIR device detected.\n\nThis application requires an SFS panelAIR device to operate.\nThe application will remain functional for 10 minutes only.\n\n(Please connect the panelAIR USB touch interface and restart the application.)", "panelAIR1000", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                var countdownForm = new Form
                {
                    Text = "panelAIR1000 - Trial Mode",
                    Size = new Size(800, 300),
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.Manual,
                    TopMost = true,
                    ControlBox = false
                };
                countdownForm.Location = new Point(
                    screen4K.Bounds.X + (screen4K.Bounds.Width - countdownForm.Width) / 2,
                    screen4K.Bounds.Y + (screen4K.Bounds.Height - countdownForm.Height) / 2
                );
                var label = new Label
                {
                    Text = "No panelAIR1000 detected.\nClosing in 20 seconds...",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 24)
                };
                countdownForm.Controls.Add(label);
                countdownForm.Show();   // no-modal: l'app principal continua inicialitzant-se

                var startTime = DateTime.Now;
                int totalSeconds = 20;
                bool expiryHandled = false;   // guard contra callbacks duplicats

                trialBgTimer = new System.Threading.Timer(_ => {
                    int elapsed = (int)(DateTime.Now - startTime).TotalSeconds;
                    int secondsLeft = totalSeconds - elapsed;
                    if (countdownForm.IsDisposed) return;
                    try
                    {
                        countdownForm.BeginInvoke(new Action(() => {
                            if (expiryHandled) return;   // descarta callbacks duplicats
                            if (secondsLeft <= 0)
                            {
                                expiryHandled = true;
                                System.Diagnostics.Process.GetCurrentProcess().Kill();
                            }
                            else
                            {
                                label.Text = $"No panelAIR1000 detected.\nClosing in {secondsLeft} seconds...";
                            }
                        }));
                    }
                    catch { }
                }, null, 0, 200);
            }

            //flapsTimer.Interval = 30; // ~33 FPS, suau
            //flapsTimer.Tick += (s, e) =>
            {
                pbMain.Invalidate();
            };
            // Reconnect timer (inactiu fins que calgui)
            simReconnectTimer = new System.Windows.Forms.Timer();
            simReconnectTimer.Interval = 5000; // 5 s
            simReconnectTimer.Tick += (s2, ev2) =>
            {
                if (simReconnectPending) { TryReconnectSim(); }
            };


            ConnectToSimConnect();

            // SFSmonitor detection
            sfsMonitorRunning = Process.GetProcesses().Any(p => p.ProcessName.StartsWith("SFSmonitor"));
            if (!sfsMonitorRunning) ShowSfsWarning();
            _sfsCheckTimer = new System.Threading.Timer(_ =>
            {
                if (IsDisposed || !IsHandleCreated) return;
                bool running = Process.GetProcesses().Any(p => p.ProcessName.StartsWith("SFSmonitor"));
                try { Invoke(() => { sfsMonitorRunning = running; if (running) HideSfsWarning(); else ShowSfsWarning(); }); }
                catch (ObjectDisposedException) { }
            }, null, 3000, 3000);

            // Si s'ha obert RAM abans que MSFS, quedem en "waiting" i reintentem sols.
            if (simconnect == null)
            {
                textBox21.Text = "waiting";
                textBox21.ForeColor = Color.Orange;

                simReconnectPending = true;
                simReconnectTimer.Start();
            }

            hideTimer = new System.Windows.Forms.Timer();
            hideTimer.Interval = 5000;
            hideTimer.Tick += (s, ev) =>
            {
                if (!checkBox1.Checked)
                {
                    this.ShowInTaskbar = true;
                    this.WindowState = FormWindowState.Minimized;
                }
                hideTimer.Stop();
            };



            //this.ShowInTaskbar = true;
            //this.WindowState = FormWindowState.Normal;
            //this.Activate();
            //this.BringToFront();
            checkBox1.Checked = true;
            //hideTimer.Start();
            checkBox1.CheckedChanged += checkBox1_CheckedChanged;
            if (!checkBox1.Checked) hideTimer.Start();


            this.Text = "panelAIR1000";

            if (enableHidReading)
            {
                hidThread1 = new Thread(ReadHidButtons1);
                hidThread1.IsBackground = true;
                hidThread1.Start();

                hidThread2 = new Thread(ReadHidButtons2);
                hidThread2.IsBackground = true;
                hidThread2.Start();

                hidThread5 = new Thread(ReadHidButtons5);
                hidThread5.IsBackground = true;
                hidThread5.Start();
            }

            this.FormClosing += Form1_FormClosing;

            // Timer per refrescar estat de connexió rotAIR
            var statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Interval = 1000; // cada 1 segon
            statusTimer.Tick += (s, ev) => UpdateRotAIRStatus();
            statusTimer.Start();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _sfsCheckTimer?.Dispose();
            stopHidThread1 = true;
            stopHidThread2 = true;
            stopHidThread3 = true;
            stopHidThread4 = true;
            stopHidThread5 = true;
        }
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox1.Checked)
            {
                // Reinicia el temporitzador per amagar la finestra en 5 segons
                hideTimer.Stop();
                hideTimer.Start();
            }
            else
            {
                // Si es torna a marcar, assegurem que la finestra es mostra
                this.ShowInTaskbar = true;
                this.WindowState = FormWindowState.Normal;
                this.Activate();
                this.BringToFront();
                hideTimer.Stop();
            }
        }

        private void UpdateRotAIRStatus()
        {
            // Type-1
            if (!isType1GCConnected && !isType1ANAConnected)
            {
                textBox22.Text = "disconnected";
                textBox22.ForeColor = Color.Orange;
            }
            else if (isType1GCConnected)
            {
                textBox22.Text = "GC mode";
                textBox22.ForeColor = Color.LimeGreen;
            }
            else if (isType1ANAConnected)
            {
                textBox22.Text = "ANA mode";
                textBox22.ForeColor = Color.LimeGreen;
            }

            // Type-2
            if (!isType2GCConnected && !isType2ANAConnected && !isType2GCAMConnected)
            {
                textBox23.Text = "disconnected";
                textBox23.ForeColor = Color.Orange;
            }
            else if (isType2GCAMConnected)
            {
                textBox23.Text = "GC (AM) mode";
                textBox23.ForeColor = Color.Red;
            }
            else if (isType2GCConnected)
            {
                textBox23.Text = "GC mode";
                textBox23.ForeColor = Color.LimeGreen;
            }
            else if (isType2ANAConnected)
            {
                textBox23.Text = "ANA mode";
                textBox23.ForeColor = Color.LimeGreen;
            }
        }

        private void ConnectToSimConnect()
        {
            try
            {
                simconnect = new SimConnect("MSFS2020 SimConnect WinForms", this.Handle, WM_USER_SIMCONNECT, null, 0);

                simconnect.OnRecvOpen += new SimConnect.RecvOpenEventHandler(Simconnect_OnRecvOpen);
                simconnect.OnRecvQuit += new SimConnect.RecvQuitEventHandler(Simconnect_OnRecvQuit);
                simconnect.OnRecvSimobjectData += new SimConnect.RecvSimobjectDataEventHandler(Simconnect_OnRecvSimobjectData);

                //rotAIR exclisive mode GC*****************************************************************************************************************
                //NAV Push
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.NAV_PUSH, "SFS.G1000_PFD_NAV_PUSH");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.NAV_PUSH, false);

                //NAV Switch
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.NAV_SWITCH, "SFS.G1000_PFD_NAV_SWAP");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.NAV_SWITCH, false);

                // COM PUSH
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.COM_PUSH, "SFS.G1000_PFD_COM_PUSH");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.COM_PUSH, false);

                // COM SWITCH
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.COM_SWITCH, "SFS.G1000_PFD_COM_SWAP");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.COM_SWITCH, false);

                // COM inner
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.COM_INNER_INC, "SFS.G1000_PFD_COM_INNER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.COM_INNER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.COM_INNER_DEC, "SFS.G1000_PFD_COM_INNER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.COM_INNER_DEC, false);

                // COM Outer
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.COM_OUTER_INC, "SFS.G1000_PFD_COM_OUTER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.COM_OUTER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.COM_OUTER_DEC, "SFS.G1000_PFD_COM_OUTER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.COM_OUTER_DEC, false);

                // CRS PUSH
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.CRS_PUSH, "SFS.G1000_PFD_CRS_PUSH");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.CRS_PUSH, false);

                // CRS INC / DEC
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.CRS_INC, "SFS.G1000_PFD_CRS_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.CRS_INC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.CRS_DEC, "SFS.G1000_PFD_CRS_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.CRS_DEC, false);

                // BARO (INC / DEC)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.BARO_INC, "SFS.G1000_PFD_BARO_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.BARO_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.BARO_DEC, "SFS.G1000_PFD_BARO_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.BARO_DEC, false);

                // BARO STD / SET
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.BARO_STD, "BAROMETRIC_STD_PRESSURE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.BARO_STD, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.BARO_SET, "BAROMETRIC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.BARO_SET, false);

                // PAN PUSH
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PAN_PUSH, "SFS.G1000_MFD_PAN_PUSH");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PAN_PUSH, false);

                // MFD RANGE INC / DEC (Zoom Pan)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_RANGE_INC, "SFS.G1000_MFD_RANGE_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_RANGE_INC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_RANGE_DEC, "SFS.G1000_MFD_RANGE_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_RANGE_DEC, false);

                // MFD PAN (Left / Right)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_PAN_LEFT, "SFS.G1000_MFD_PAN_LEFT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_PAN_LEFT, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_PAN_RIGHT, "SFS.G1000_MFD_PAN_RIGHT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_PAN_RIGHT, false);

                // MFD PAN (Up / Down)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_PAN_UP, "SFS.G1000_MFD_PAN_UP");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_PAN_UP, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_PAN_DOWN, "SFS.G1000_MFD_PAN_DOWN");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_PAN_DOWN, false);

                // SOFTKEY 6 (CDI)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.CDI, "SFS.G1000_PFD_SOFTKEY_6");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.CDI, false);

                // CRSR PDF PUSH
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.CRSR_PFD_PUSH, "SFS.G1000_PFD_FMS_PUSH");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.CRSR_PFD_PUSH, false);

                // FMS INNER (PFD)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FMS_PFD_INNER_INC, "SFS.G1000_PFD_FMS_INNER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FMS_PFD_INNER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FMS_PFD_INNER_DEC, "SFS.G1000_PFD_FMS_INNER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FMS_PFD_INNER_DEC, false);

                // FMS OUTER (PFD)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FMS_PFD_OUTER_INC, "SFS.G1000_PFD_FMS_OUTER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FMS_PFD_OUTER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FMS_PFD_OUTER_DEC, "SFS.G1000_PFD_FMS_OUTER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FMS_PFD_OUTER_DEC, false);

                // CRSR MDF PUSH
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.CRSR_MDF_PUSH, "SFS.G1000_MFD_FMS_PUSH");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.CRSR_MDF_PUSH, false);

                // FMS INNER (MFD)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FMS_MFD_INNER_INC, "SFS.G1000_MFD_FMS_INNER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FMS_MFD_INNER_INC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FMS_MFD_INNER_DEC, "SFS.G1000_MFD_FMS_INNER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FMS_MFD_INNER_DEC, false);

                // FMS OUTER (MFD)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FMS_MFD_OUTER_INC, "SFS.G1000_MFD_FMS_OUTER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FMS_MFD_OUTER_INC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FMS_MFD_OUTER_DEC, "SFS.G1000_MFD_FMS_OUTER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FMS_MFD_OUTER_DEC, false);

                //HDG sync
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.HDG_SYNC, "HEADING_BUG_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.HDG_SYNC, false);

                //XPDR SET
                simconnect.AddToDataDefinition(DEFINITIONS.XPNDR_SET, "TRANSPONDER STATE:1", "Enum", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<XPNDRWrite>(DEFINITIONS.XPNDR_SET);

                // XPDR IDENT
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_IDNT, "XPNDR_IDENT_ON");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_IDNT, false);


                //NAV Inner
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.NAV_INNER_INC, "SFS.G1000_PFD_NAV_INNER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.NAV_INNER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.NAV_INNER_DEC, "SFS.G1000_PFD_NAV_INNER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.NAV_INNER_DEC, false);

                //NAV Outer
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.NAV_OUTER_INC, "SFS.G1000_PFD_NAV_OUTER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.NAV_OUTER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.NAV_OUTER_DEC, "SFS.G1000_PFD_NAV_OUTER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.NAV_OUTER_DEC, false);

                //Heading
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.HDG_BUG_INC, "HEADING_BUG_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.HDG_BUG_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.HDG_BUG_DEC, "HEADING_BUG_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.HDG_BUG_DEC, false);

                //ALT Inner
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ALT_G1000_INNER_INC, "SFS.G1000_PFD_ALT_INNER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ALT_G1000_INNER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ALT_G1000_INNER_DEC, "SFS.G1000_PFD_ALT_INNER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ALT_G1000_INNER_DEC, false);

                //ALT Outer
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ALT_G1000_OUTER_INC, "SFS.G1000_PFD_ALT_OUTER_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ALT_G1000_OUTER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ALT_G1000_OUTER_DEC, "SFS.G1000_PFD_ALT_OUTER_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ALT_G1000_OUTER_DEC, false);

                //ADF1 Inner
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ADF_INNER_INC, "ADF1_WHOLE_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ADF_INNER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ADF_INNER_DEC, "ADF1_WHOLE_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ADF_INNER_DEC, false);

                //ADF1 Outer
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ADF_OUTER_INC, "ADF_100_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ADF_OUTER_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ADF_OUTER_DEC, "ADF_100_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ADF_OUTER_DEC, false);

                // ADF SWAP (Active <-> Standby)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ADF_SWITCH, "ADF1_RADIO_SWAP");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ADF_SWITCH, false);

                // XPDR - 1000's
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_DIGIT_1_INC, "XPNDR_1000_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_1_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_DIGIT_1_DEC, "XPNDR_1000_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_1_DEC, false);

                // XPDR - 100's
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_DIGIT_2_INC, "XPNDR_100_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_2_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_DIGIT_2_DEC, "XPNDR_100_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_2_DEC, false);

                // XPDR - 10's
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_DIGIT_3_INC, "XPNDR_10_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_3_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_DIGIT_3_DEC, "XPNDR_10_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_3_DEC, false);

                // XPDR - 1's
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_DIGIT_4_INC, "XPNDR_1_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_4_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.XPNDR_DIGIT_4_DEC, "XPNDR_1_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_4_DEC, false);

                // grup
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_3_INC, false);
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_3_DEC, false);
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_4_INC, false);
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.XPNDR_DIGIT_4_DEC, false);

                

                //panelAIR1000-------------------------------------------------------------------------
                //simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.NAV_SWAP, "AP_MASTER");
                //simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.NAV_SWAP, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_MASTER, "AP_MASTER");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_MASTER, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_HDG, "AP_HDG_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_HDG, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_NAV, "AP_NAV1_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_NAV, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_APR, "AP_APR_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_APR, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_VS, "AP_PANEL_VS_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_VS, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_FLC, "FLIGHT_LEVEL_CHANGE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_FLC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_FD, "TOGGLE_FLIGHT_DIRECTOR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_FD, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_ALT, "AP_ALT_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_ALT, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_VNV, "SFS.G1000_PFD_AP_VNV");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_VNV, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_BC, "AP_BC_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_BC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_UP, "SFS.G1000_PFD_AP_NOSE_UP");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_UP, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AP_DN, "SFS.G1000_PFD_AP_NOSE_DN");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AP_DN, false);

                //PFD SOFT KEYS
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_1, "SFS.G1000_PFD_SOFTKEY_1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_1, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_2, "SFS.G1000_PFD_SOFTKEY_2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_2, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_3, "SFS.G1000_PFD_SOFTKEY_3");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_3, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_4, "SFS.G1000_PFD_SOFTKEY_4");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_4, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_5, "SFS.G1000_PFD_SOFTKEY_5");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_5, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_6, "SFS.G1000_PFD_SOFTKEY_6");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_6, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_7, "SFS.G1000_PFD_SOFTKEY_7");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_7, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_8, "SFS.G1000_PFD_SOFTKEY_8");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_8, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_9, "SFS.G1000_PFD_SOFTKEY_9");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_9, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_10, "SFS.G1000_PFD_SOFTKEY_10");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_10, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_11, "SFS.G1000_PFD_SOFTKEY_11");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_11, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_SK_12, "SFS.G1000_PFD_SOFTKEY_12");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_SK_12, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_D_TO, "SFS.G1000_PFD_DTO");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_D_TO, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_FPL, "SFS.G1000_PFD_FPL");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_FPL, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_CLR, "SFS.G1000_PFD_CLR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_CLR, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_MENU, "SFS.G1000_PFD_MENU");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_MENU, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_PROC, "SFS.G1000_PFD_PROC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_PROC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PFD_ENT, "SFS.G1000_PFD_ENT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PFD_ENT, false);

                //AUDIO
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_COM1_MIC, "COM1_TRANSMIT_SELECT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_COM1_MIC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_COM1, "COM_RECEIVE_ALL_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_COM1, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_COM2_MIC, "COM2_TRANSMIT_SELECT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_COM2_MIC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_COM2, "COM_RECEIVE_ALL_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_COM2, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_SPKR, "SFS.G1000_AUDIO_SPKR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_SPKR, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_PA, "SFS.G1000_AUDIO_PA");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_PA, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_HI_SENS, "SFS.G1000_AUDIO_HI_SENS");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_HI_SENS, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_MKR_MUTE_ON, "SFS.G1000_AUDIO_MKR_MUTE_ON");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_MKR_MUTE_ON, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_MKR_MUTE_OFF, "SFS.G1000_AUDIO_MKR_MUTE_OFF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_MKR_MUTE_OFF, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_NAV1, "SFS.G1000_AUDIO_NAV1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_NAV1, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_DME, "SFS.G1000_AUDIO_DME");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_DME, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_NAV2, "SFS.G1000_AUDIO_NAV2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_NAV2, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_ADF, "SFS.G1000_AUDIO_ADF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_ADF, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_PLAY, "SFS.G1000_AUDIO_PLAY");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_PLAY, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_MAN_SQ, "SFS.G1000_AUDIO_MAN_SQ");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_MAN_SQ, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_PILOT, "SFS.G1000_AUDIO_PILOT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_PILOT, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_COPILOT, "SFS.G1000_AUDIO_COPLT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_COPILOT, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_DISPLAY_BKUP_ON, "SFS.G1000_AUDIO_DISPLAY_BACKUP_ON");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_DISPLAY_BKUP_ON, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUDIO_DISPLAY_BKUP_OFF, "SFS.G1000_AUDIO_DISPLAY_BACKUP_OFF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUDIO_DISPLAY_BKUP_OFF, false);

                //MFD SOFT KEYS
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_1, "SFS.G1000_MFD_SOFTKEY_1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_1, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_2, "SFS.G1000_MFD_SOFTKEY_2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_2, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_3, "SFS.G1000_MFD_SOFTKEY_3");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_3, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_4, "SFS.G1000_MFD_SOFTKEY_4");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_4, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_5, "SFS.G1000_MFD_SOFTKEY_5");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_5, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_6, "SFS.G1000_MFD_SOFTKEY_6");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_6, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_7, "SFS.G1000_MFD_SOFTKEY_7");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_7, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_8, "SFS.G1000_MFD_SOFTKEY_8");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_8, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_9, "SFS.G1000_MFD_SOFTKEY_9");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_9, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_10, "SFS.G1000_MFD_SOFTKEY_10");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_10, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_11, "SFS.G1000_MFD_SOFTKEY_11");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_11, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_SK_12, "SFS.G1000_MFD_SOFTKEY_12");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_SK_12, false);

                //MFD RIGHT

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_D_TO, "SFS.G1000_MFD_DTO");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_D_TO, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_FPL, "SFS.G1000_MFD_FPL");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_FPL, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_CLR, "SFS.G1000_MFD_CLR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_CLR, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_MENU, "SFS.G1000_MFD_MENU");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_MENU, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_PROC, "SFS.G1000_MFD_PROC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_PROC, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MFD_ENT, "SFS.G1000_MFD_ENT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MFD_ENT, false);

                //COCKPIT RIGHT

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_BEACON, "TOGGLE_BEACON_LIGHTS");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_BEACON, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_LAND, "LANDING_LIGHTS_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_LAND, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_TAXI, "TOGGLE_TAXI_LIGHTS");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_TAXI, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_NAV, "TOGGLE_NAV_LIGHTS");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_NAV, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_STROBE, "STROBES_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_STROBE, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_RECOGN, "RECOGNITION_LIGHTS_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_RECOGN, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_PITOTHEAT, "PITOT_HEAT_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_PITOTHEAT, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_PANELLIGHT, "PANEL_LIGHTS_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_PANELLIGHT, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_DEICE, "TOGGLE_STRUCTURAL_DEICE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_DEICE, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.COWL_FLAP_L_SET, "COWLFLAP1_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.COWL_FLAP_L_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.COWL_FLAP_R_SET, "COWLFLAP2_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.COWL_FLAP_R_SET, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_FUELPUMP1, "TOGGLE_ELECT_FUEL_PUMP1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_FUELPUMP1, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_FUELPUMP2, "TOGGLE_ELECT_FUEL_PUMP2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_FUELPUMP2, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FLAPS_SWITCH_UP, "FLAPS_DECR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FLAPS_SWITCH_UP, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FLAPS_SWITCH_DN, "FLAPS_INCR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FLAPS_SWITCH_DN, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TRIM_UP, "ELEV_TRIM_UP");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TRIM_UP, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TRIM_DN, "ELEV_TRIM_DN");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TRIM_DN, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.RUDDER_TRIM_LEFT, "RUDDER_TRIM_LEFT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.RUDDER_TRIM_LEFT, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.RUDDER_TRIM_RIGHT, "RUDDER_TRIM_RIGHT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.RUDDER_TRIM_RIGHT, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.LGEAR_UP, "GEAR_UP");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.LGEAR_UP, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.LGEAR_DN, "GEAR_DOWN");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.LGEAR_DN, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SWITCH_PBRAKE, "PARKING_BRAKES");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SWITCH_PBRAKE, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MASTER_BATTERY_ON,  "MASTER_BATTERY_ON");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MASTER_BATTERY_ON,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MASTER_BATTERY_OFF, "MASTER_BATTERY_OFF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MASTER_BATTERY_OFF, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AVIONICS_MASTER_1_ON,  "AVIONICS_MASTER_1_ON");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AVIONICS_MASTER_1_ON,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AVIONICS_MASTER_1_OFF, "AVIONICS_MASTER_1_OFF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AVIONICS_MASTER_1_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AVIONICS_MASTER_2_ON,  "AVIONICS_MASTER_2_ON");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AVIONICS_MASTER_2_ON,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AVIONICS_MASTER_2_OFF, "AVIONICS_MASTER_2_OFF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AVIONICS_MASTER_2_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR1, "TOGGLE_ALTERNATOR1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR1, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR2, "TOGGLE_ALTERNATOR2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR2, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TOGGLE_STARTER1, "TOGGLE_STARTER1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TOGGLE_STARTER1, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TOGGLE_STARTER2, "TOGGLE_STARTER2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TOGGLE_STARTER2, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TURBINE_IGNITION_SWITCH_TOGGLE, "TURBINE_IGNITION_SWITCH_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TURBINE_IGNITION_SWITCH_TOGGLE, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TURBINE_IGNITION_SWITCH_SET1, "TURBINE_IGNITION_SWITCH_SET1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TURBINE_IGNITION_SWITCH_SET1, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.KINGAIR_IGN1_ARM, "SFS.KingAir_Ign1_Arm");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.KINGAIR_IGN1_ARM, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.KINGAIR_IGN1_OFF, "SFS.KingAir_Ign1_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.KINGAIR_IGN1_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.KINGAIR_IGN2_ARM, "SFS.KingAir_Ign2_Arm");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.KINGAIR_IGN2_ARM, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.KINGAIR_IGN2_OFF, "SFS.KingAir_Ign2_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.KINGAIR_IGN2_OFF, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MAGNETO1_INCR, "MAGNETO1_INCR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MAGNETO1_INCR, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MAGNETO1_DECR, "MAGNETO1_DECR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MAGNETO1_DECR, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MAGNETO2_INCR, "MAGNETO2_INCR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MAGNETO2_INCR, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.MAGNETO2_DECR, "MAGNETO2_DECR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.MAGNETO2_DECR, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FUEL_SELECTOR_SET,   "FUEL_SELECTOR_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET,   false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FUEL_SELECTOR_2_SET, "FUEL_SELECTOR_2_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FUEL_SELECTOR_2_SET, false);



                simconnect.SetNotificationGroupPriority(SIMCONNECT_GROUPS.GROUP0, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);


                // DOUBLE (FLOAT64)
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "Title", null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "HEADING INDICATOR", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT HEADING LOCK DIR", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "NAV1 OBS", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "NAV2 OBS", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ADF ACTIVE FREQUENCY:1", "Hz", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ADF STANDBY FREQUENCY:1", "Hz", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "KOHLSMAN SETTING HG", "inHg", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "KOHLSMAN SETTING MB", "Millibars", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT ALTITUDE LOCK VAR", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:AS1000_MID_Display_Backup_Active", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ELEVATOR TRIM INDICATOR", "Position", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RUDDER TRIM PCT", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR CENTER POSITION", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR LEFT POSITION", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR right POSITION", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ELECTRICAL MAIN BUS VOLTAGE", "Volts", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);

                // INT (INT32)
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "TRANSPONDER CODE:1", "Enum", SIMCONNECT_DATATYPE.INT32, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "TRANSPONDER STATE:1", "Enum", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "TRANSPONDER IDENT:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GPS DRIVES NAV1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT NAV SELECTED", "number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                //simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "SIMCONNECT VERSION", "number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "COM TRANSMIT:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "COM TRANSMIT:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "COM RECEIVE ALL", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "DME SOUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "SPEAKER ACTIVE", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "MARKER BEACON TEST MUTE", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "NAV1 SOUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ADF SOUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "NAV2 SOUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "LIGHT BEACON",      "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "LIGHT LANDING",     "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "LIGHT TAXI",        "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "LIGHT NAV",         "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "LIGHT STROBE",      "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "LIGHT RECOGNITION", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "LIGHT PANEL",       "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "STRUCTURAL DEICE SWITCH", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PITOT HEAT", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG FUEL PUMP SWITCH:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG FUEL PUMP SWITCH:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "FLAPS NUM HANDLE POSITIONS", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "FLAPS HANDLE INDEX", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "BRAKE PARKING POSITION", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GEAR HANDLE POSITION", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RECIP ENG LEFT MAGNETO:1",  "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RECIP ENG RIGHT MAGNETO:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG STARTER:1",     "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RECIP ENG LEFT MAGNETO:2",  "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RECIP ENG RIGHT MAGNETO:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG STARTER:2",     "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "FUEL TANK SELECTOR:1", "Enum", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "FUEL TANK SELECTOR:2", "Enum", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "FUEL CROSS FEED", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "NUMBER OF ENGINES", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RECIP ENG COWL FLAP POSITION:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RECIP ENG COWL FLAP POSITION:2", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ELECTRICAL MASTER BATTERY", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AVIONICS MASTER SWITCH:1",   "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AVIONICS MASTER SWITCH:2",   "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG MASTER ALTERNATOR:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG MASTER ALTERNATOR:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG STARTER:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG STARTER:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);


                simconnect.RegisterDataDefineStruct<Struct1>(DEFINITIONS.Struct1);
                simconnect.RequestDataOnSimObject(REQUESTS.Request1, DEFINITIONS.Struct1, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

            }
            catch (COMException ex)
            {
                // Evitem mostrar el missatge repetit cada 5 s; només marquem "waiting"
                textBox21.Text = "waiting";
                textBox21.ForeColor = Color.Orange;

                if (!_suppressCloseOnConnectFail)
                {
                    // No tanquem; simplement esperem el proper intent del timer
                    simReconnectPending = true;
                    simReconnectTimer.Start();
                }
            }
        }
        private void ShowSfsWarning()
        {
            if (_sfsMissingForm != null && !_sfsMissingForm.IsDisposed) return;
            _sfsMissingForm = new Form
            {
                Text = "panelAIR1000 - WARNING",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false,
                StartPosition = FormStartPosition.Manual,
                Size = new Size(1640, 360),
                BackColor = Color.FromArgb(40, 40, 40),
                TopMost = true,
                Icon = Resources.logo
            };
            var rtb = new RichTextBox
            {
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                Bounds = new Rectangle(10, 145, 1608, 90),
                Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                ScrollBars = RichTextBoxScrollBars.None,
                TabStop = false,
                WordWrap = false
            };
            const string msg = "Please activate the SFSmonitor plugin to complete the functionality of this panelAIR1000 App.";
            rtb.Text = msg;
            _sfsMissingForm.Controls.Add(rtb);
            _sfsMissingForm.Shown += (s, ev) =>
            {
                var f = new Font("Segoe UI", 24f, FontStyle.Bold);
                rtb.SelectAll();
                rtb.SelectionFont = f;
                rtb.SelectionColor = Color.White;
                rtb.SelectionAlignment = HorizontalAlignment.Center;
                int idx = msg.IndexOf("SFSmonitor");
                rtb.Select(idx, "SFSmonitor".Length);
                rtb.SelectionFont = f;
                rtb.SelectionColor = Color.DeepSkyBlue;
                rtb.DeselectAll();
            };

            var screen = Screen.FromControl(this).Bounds;
            _sfsMissingForm.Location = new Point(
                screen.X + (screen.Width - _sfsMissingForm.Width) / 2,
                screen.Y
            );
            _sfsMissingForm.Show(this);
        }

        private void HideSfsWarning()
        {
            if (_sfsMissingForm != null && !_sfsMissingForm.IsDisposed)
            {
                _sfsMissingForm.Hide();
                _sfsMissingForm.Close();
                _sfsMissingForm = null;
            }
        }

        private void TryReconnectSim()
        {
            try
            {
                _suppressCloseOnConnectFail = true; // evita tancar el formulari si MSFS no hi és encara
                                                    // Desconnecta net i reintenta connectar
                try { DisconnectFromSimConnect(); } catch { }
                ConnectToSimConnect();

                if (simconnect != null)
                {
                    simReconnectPending = false;
                    simReconnectTimer.Stop();

                    textBox21.Text = "MSFS";
                    textBox21.ForeColor = Color.Cyan;

                    try
                    {
                        System.IO.File.AppendAllText("panelAIR1000_crash.txt",
                            "\r\n[SimConnect] Reconnected OK.");
                    }
                    catch { }
                }
                else
                {
                    simReconnectPending = true;
                    if (!simReconnectTimer.Enabled) simReconnectTimer.Start();

                    textBox21.Text = "waiting";
                    textBox21.ForeColor = Color.Orange;
                }
            }
            catch
            {
                // Seguirem reintentant cada 5 s
            }
            finally
            {
                _suppressCloseOnConnectFail = false;
            }
        }


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT)
            {
                if (simconnect != null)
                {
                    try
                    {
                        simconnect.ReceiveMessage();
                    }
                    catch (System.Collections.Generic.KeyNotFoundException ex)
                    {
                        // Ignorem els Key '27' repetits per evitar logs massius
                        if (!ex.Message.Contains("'27'"))
                        {
                            try { System.IO.File.AppendAllText("panelAIR1000_crash.txt", "\r\n[SIMCONNECT KeyNotFound] " + ex.Message); } catch { }
                        }
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        // Desconnexió brusc (0xC000014B): no tanquem; marquem "disconnected" i reintentem
                        try
                        {
                            System.IO.File.AppendAllText("panelAIR1000_crash.txt",
                            "\r\n[SIMCONNECT Dispatch] " + ex.ToString());
                        }
                        catch { }

                        textBox21.Text = "waiting";
                        textBox21.ForeColor = Color.Orange;

                        simReconnectPending = true;
                        simReconnectTimer.Start();
                        return; // molt important: no propagar ni tancar el form
                    }

                    catch (System.Exception ex)
                    {
                        // Altres errors del dispatcher SimConnect
                        try { System.IO.File.AppendAllText("panelAIR1000_crash.txt", "\r\n[SIMCONNECT Dispatch] " + ex.ToString()); } catch { }
                    }
                }
            }
            else
            {
                base.WndProc(ref m);
            }

        }

        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("SimConnect connection established");

            textBox21.Text = "MSFS";
            textBox21.ForeColor = Color.Cyan;
            // Ens acabem de connectar: aturem reintents i forcem un "refresh" inicial.
            simReconnectPending = false;
            if (simReconnectTimer.Enabled) simReconnectTimer.Stop();

            // Restablim els flags perquè el primer paquet pinti immediatament
            

            // Tornem a demanar la subscripció (inofensiu si ja hi era) per tenir dades ràpid
            simconnect.RequestDataOnSimObject(
                REQUESTS.Request1,
                DEFINITIONS.Struct1,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.VISUAL_FRAME,
                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0
            );

            // Opcional: actualitza estat HID un cop per pantalla
            UpdateRotAIRStatus();
        }

        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("SimConnect connection closed");

            textBox21.Text = "waiting";
            textBox21.ForeColor = Color.Orange;

            simReconnectPending = true;
            simReconnectTimer.Start();

            try
            {
                System.IO.File.AppendAllText("panelAIR1000_crash.txt",
                "\r\n[SimConnect] Quit received -> will try reconnect...");
            }
            catch { }

            DisconnectFromSimConnect();
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            //Debug.WriteLine(">>> OnRecvSimobjectData CALLED");
            if (data.dwRequestID == (uint)REQUESTS.Request1)
            {
                Struct1 s1 = (Struct1)data.dwData[0];
                bool newCom1Active = (s1.com_transmit_1 & 1) != 0;
                bool newCom2Active = (s1.com_transmit_2 & 1) != 0;
                bool newComReceiveAll = (s1.com_receive_all & 1) != 0;
                bool newDmeActive = (s1.dme_sound & 1) != 0;
                bool newNav1Active = (s1.nav1_sound & 1) != 0;
                bool newSpkrActive = (s1.spkr_sound & 1) != 0;
                bool newMkrMute = (s1.mkr_mute & 1) != 0;
                bool newAdfActive = (s1.adf_sound & 1) != 0;
                bool newNav2Active = (s1.nav2_sound & 1) != 0;
                bool newBeacon = (s1.read_beacon & 1) != 0;
                bool newLand = (s1.read_land & 1) != 0;
                bool newTaxi = (s1.read_taxi & 1) != 0;
                bool newNav = (s1.read_nav & 1) != 0;
                bool newStrobe = (s1.read_strobe & 1) != 0;
                bool newRecogn = (s1.read_recogn & 1) != 0;
                bool newPanelLight = (s1.read_panellight & 1) != 0;
                bool newPitotheat = (s1.read_pitotheat & 1) != 0;
                bool newFuelpump1 = (s1.read_fuelpump1 & 1) != 0;
                bool newFuelpump2 = (s1.read_fuelpump2 & 1) != 0;
                gearNosePercent = (int)s1.gear_center;
                gearLeftPercent = (int)s1.gear_left;
                gearRightPercent = (int)s1.gear_right;
                mainBusVolts = s1.main_bus_volts;
                bool newGearHandleUp = (s1.gear_handle & 1) == 0; // 0=UP, 1=DN
                if (newGearHandleUp != lgearHandleUp)
                {
                    lgearHandleUp = newGearHandleUp;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                bool newParkingbrake = (s1.read_parking_brake & 1) != 0;


                //System.Diagnostics.Debug.WriteLine($"DME SOUND raw={s1.dme_read}");
                //System.Diagnostics.Debug.WriteLine(">>> OnRecvSimobjectData CALLED");
                //bool newDmeActive = s1.dme_read == 1;
                if (flapsNumPositions != s1.flaps_num_handle_positions)
                {
                    flapsNumPositions = s1.flaps_num_handle_positions;

                    // forcem repintat cockpit right
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (flapsPoscPalanca != s1.flaps_real_handle_position)
                {
                    flapsPoscPalanca = s1.flaps_real_handle_position;

                    //flapsTimer.Start();
                    // forcem repintat cockpit right
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (elevatorTrim != s1.elevator_trim)
                {
                    elevatorTrim = s1.elevator_trim;

                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                double newRudderTrim = s1.rudder_trim / 100.0;
                if (rudderTrim != newRudderTrim)
                {
                    rudderTrim = newRudderTrim;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }

                if (newCom1Active != com1Active)
                {
                    com1Active = newCom1Active;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newCom2Active != com2Active)
                {
                    com2Active = newCom2Active;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newComReceiveAll != comReceiveAll)
                {
                    comReceiveAll = newComReceiveAll;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newDmeActive != dmeActive)
                {
                    dmeActive = newDmeActive;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newNav1Active != nav1Active)
                {
                    nav1Active = newNav1Active;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newSpkrActive != spkrActive)
                {
                    spkrActive = newSpkrActive;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newMkrMute != mkrMute)
                {
                    mkrMute = newMkrMute;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newAdfActive != adfMute)
                {
                    adfMute = newAdfActive;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newNav2Active != nav2Active)
                {
                    nav2Active = newNav2Active;

                    // força repintat immediat del pbAUDIO
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newBeacon != beaconActive )
                {
                    beaconActive = newBeacon;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newLand != landActive)
                {
                    landActive = newLand;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newTaxi != taxiActive)
                {
                    taxiActive = newTaxi;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newNav != navActive)
                {
                    navActive = newNav;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newStrobe != strobeActive)
                {
                    strobeActive = newStrobe;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newRecogn != recognActive)
                {
                    recognActive = newRecogn;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newPanelLight != panelLightActive)
                {
                    panelLightActive = newPanelLight;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                bool newDeice = (s1.read_deice & 1) != 0;
                if (newDeice != deiceActive)
                {
                    deiceActive = newDeice;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                int newCowlL = s1.read_cowlflap_l < 10 ? 0 : s1.read_cowlflap_l < 70 ? 1 : 2;
                int newCowlR = s1.read_cowlflap_r < 10 ? 0 : s1.read_cowlflap_r < 70 ? 1 : 2;
                if (newCowlL != cowlFlapL || newCowlR != cowlFlapR)
                {
                    cowlFlapL = newCowlL;
                    cowlFlapR = newCowlR;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newPitotheat != pitotheatActive)
                {
                    pitotheatActive = newPitotheat;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newFuelpump1 != fuelpump1Active)
                {
                    fuelpump1Active = newFuelpump1;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newFuelpump2 != fuelpump2Active)
                {
                    fuelpump2Active = newFuelpump2;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newParkingbrake != pbrakeActive)
                {
                    pbrakeActive = newParkingbrake;

                    // força repintat immediat del pbcokpitright
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                bool newMasterBatteryOff = s1.elec_master_battery == 0;
                if (newMasterBatteryOff != masterBatteryOff)
                {
                    masterBatteryOff = newMasterBatteryOff;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                bool newAvionics1Off = s1.avionics_master_1 == 0;
                if (newAvionics1Off != avionics1Off)
                {
                    avionics1Off = newAvionics1Off;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                bool newAvionics2Off = s1.avionics_master_2 == 0;
                if (newAvionics2Off != avionics2Off)
                {
                    avionics2Off = newAvionics2Off;
                    if (pbMain.IsHandleCreated)
                        pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                bool newAltGen1Off = s1.gen_master_alt_1 == 0;
                bool newAltGen2Off = s1.gen_master_alt_2 == 0;
                if (newAltGen1Off != altGen1Off)
                {
                    altGen1Off = newAltGen1Off;
                    if (pbMain.IsHandleCreated) pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                if (newAltGen2Off != altGen2Off)
                {
                    altGen2Off = newAltGen2Off;
                    if (pbMain.IsHandleCreated) pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }
                bool newStarter1 = s1.starter1 != 0;
                starter1Active = newStarter1;
                pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                bool newStarter2 = s1.starter2 != 0;
                starter2Active = newStarter2;
                pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                int newIgn1Pos;
                if      (s1.ign1_left == 0 && s1.ign1_right == 0 && s1.ign1_starter == 0) newIgn1Pos = 0; // OFF
                else if (s1.ign1_left == 0 && s1.ign1_right == 1 && s1.ign1_starter == 0) newIgn1Pos = 1; // RIGHT
                else if (s1.ign1_left == 1 && s1.ign1_right == 0 && s1.ign1_starter == 0) newIgn1Pos = 2; // LEFT
                else if (s1.ign1_left == 1 && s1.ign1_right == 1 && s1.ign1_starter == 0) newIgn1Pos = 3; // BOTH
                else if (s1.ign1_left == 1 && s1.ign1_right == 1 && s1.ign1_starter == 1) newIgn1Pos = 4; // START
                else newIgn1Pos = ign1Pos; // mantenim l'últim valor conegut
                if (newIgn1Pos != ign1Pos) { ign1Pos = newIgn1Pos; pbMain.BeginInvoke(new Action(() => pbMain.Invalidate())); }
                int newIgn2Pos;
                if      (s1.ign2_left == 0 && s1.ign2_right == 0 && s1.ign2_starter == 0) newIgn2Pos = 0;
                else if (s1.ign2_left == 0 && s1.ign2_right == 1 && s1.ign2_starter == 0) newIgn2Pos = 1;
                else if (s1.ign2_left == 1 && s1.ign2_right == 0 && s1.ign2_starter == 0) newIgn2Pos = 2;
                else if (s1.ign2_left == 1 && s1.ign2_right == 1 && s1.ign2_starter == 0) newIgn2Pos = 3;
                else if (s1.ign2_left == 1 && s1.ign2_right == 1 && s1.ign2_starter == 1) newIgn2Pos = 4;
                else newIgn2Pos = ign2Pos;
                if (newIgn2Pos != ign2Pos) { ign2Pos = newIgn2Pos; pbMain.BeginInvoke(new Action(() => pbMain.Invalidate())); }
                numEngines = s1.num_engines;

                int newFuel1Pos, newFuel2Pos;

                if (numEngines >= 2)
                {
                    // TWIN ENGINE — selectors independents
                    if      (s1.fuel_selector_1 == 0 || s1.fuel_selector_1 == 20) newFuel1Pos = 0; // OFF
                    else if (s1.fuel_selector_1 == 2)                              newFuel1Pos = 1; // ON
                    else if (s1.fuel_selector_1 == 3)                              newFuel1Pos = 2; // CF
                    else                                                           newFuel1Pos = fuel1Pos;

                    if      (s1.fuel_selector_2 == 2)                              newFuel2Pos = 0; // CF
                    else if (s1.fuel_selector_2 == 3)                              newFuel2Pos = 1; // ON
                    else if (s1.fuel_selector_2 == 0 || s1.fuel_selector_2 == 20) newFuel2Pos = 2; // OFF
                    else                                                           newFuel2Pos = fuel2Pos;
                }
                else
                {
                    // SINGLE ENGINE — 1 selector, 2 knobs sincronitzats
                    // MSFS: 0=OFF, 1=ALL/BOTH, 2=LEFT, 3=RIGHT, 16=BOTH
                    switch (s1.fuel_selector_1)
                    {
                        case 0: case 20:                    // OFF → ambdós OFF
                            newFuel1Pos = 0; newFuel2Pos = 2; break;
                        case 2:                             // LEFT → esquerre ON, dret OFF
                            newFuel1Pos = 1; newFuel2Pos = 2; break;
                        case 3:                             // RIGHT → esquerre OFF, dret ON
                            newFuel1Pos = 0; newFuel2Pos = 1; break;
                        case 1: case 16:                    // BOTH → ambdós ON
                            newFuel1Pos = 1; newFuel2Pos = 1; break;
                        default:
                            newFuel1Pos = fuel1Pos; newFuel2Pos = fuel2Pos; break;
                    }
                }

                if (newFuel1Pos != fuel1Pos) { fuel1Pos = newFuel1Pos; pbMain.BeginInvoke(new Action(() => pbMain.Invalidate())); }
                if (newFuel2Pos != fuel2Pos) { fuel2Pos = newFuel2Pos; pbMain.BeginInvoke(new Action(() => pbMain.Invalidate())); }
                //dmeActive = s1.dme_read == 1; // o la variable que ja estàs llegint

                //com1Tx = s1.com_transmit_1 == 1;
                //com2Tx = s1.com_transmit_2 == 1;
                com1Tx = (s1.com_transmit_1 & 1) != 0;
                com2Tx = (s1.com_transmit_2 & 1) != 0;
                //Debug.WriteLine($"COM TX raw: c1={s1.com_transmit_1} c2={s1.com_transmit_2} -> b1={com1Tx} b2={com2Tx}");

                displayBackupActive_bool = s1.displayBackupActive > 0.5;

                

                bool hasChanged = HasDataChanged(s1);

                if (hasChanged)
                {
                    //this.ShowInTaskbar = true;
                    //this.WindowState = FormWindowState.Normal;
                    //this.Activate();
                    //this.BringToFront();
                    if (!checkBox1.Checked)
                    {
                        hideTimer.Stop();
                        hideTimer.Start();
                    }
                    else
                    {
                        hideTimer.Stop(); // si és “Always visible”, no programem cap ocultació
                    }




                    lastData = s1;
                   
                }
                

                previousNavSel = s1.nav_sel;
                firstNavSelReceived = true;             

            }
        }

        private void PbMain_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
            if (_bgImage != null)
                e.Graphics.DrawImage(_bgImage, pbMain.ClientRectangle);

            // --- AUDIO TRIANGLES ---
            foreach (var tri in audioTriangles)
                if (tri.IsActive())
                    DrawTriangle(e.Graphics, tri.BasePos, Brushes.White);

            // --- COCKPIT RIGHT — origen (2695,1144) ---

            // FLAPS MARKERS
            int fmX = flapsIndX - 25;
            e.Graphics.FillRectangle(Brushes.White, new Rectangle(fmX, 1589 + flapsIndYOffset, 20, 5));
            e.Graphics.FillRectangle(Brushes.White, new Rectangle(fmX, 1824 + flapsIndYOffset, 20, 5));

            if (flapsNumPositions > 0)
            {
                int intermediatePositions = flapsNumPositions - 1;
                int spaces = intermediatePositions + 1;
                int totalRange = 680 - 445;
                float stepY = totalRange / (float)spaces;
                for (int i = 0; i <= spaces; i++)
                {
                    int rectY = (int)(445 + stepY * i) + 1144 + flapsIndYOffset;
                    e.Graphics.FillRectangle(Brushes.White, new Rectangle(fmX, rectY, 20, 5));
                }
            }

            // TRIM TAKEOFF MARKER
            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(180, 180, 180)), new Rectangle(trimIndX - 32, 1758 + trimIndYOffset, 20, 2));

            // FLAPS CIRCLE
            if (flapsNumPositions > 0)
            {
                int yUp = 450 + 1144;   // 1594
                int yFull = 680 + 1144; // 1824
                float stepYIndicator = (yFull - yUp) / (float)flapsNumPositions;
                flapsCircleY_Target = yUp + stepYIndicator * flapsPoscPalanca;

                if (flapsPoscPalanca != flapsPoscPalanca_Last)
                {
                    if (flapsPoscPalanca_Last < 0)
                    {
                        flapsCircleY_Current = flapsCircleY_Target;
                        flapsAnimDurationSec = 0f;
                    }
                    else
                    {
                        flapsCircleY_Start = flapsCircleY_Current;
                        flapsAnimStartTime = DateTime.Now;
                        float distance = Math.Abs(flapsCircleY_Target - flapsCircleY_Current);
                        flapsAnimDurationSec = 8f * (distance / (yFull - yUp));
                    }
                    flapsPoscPalanca_Last = flapsPoscPalanca;
                }

                if (flapsAnimDurationSec > 0f)
                {
                    float t = Math.Min((float)(DateTime.Now - flapsAnimStartTime).TotalSeconds / flapsAnimDurationSec, 1f);
                    flapsCircleY_Current = flapsCircleY_Start + (flapsCircleY_Target - flapsCircleY_Start) * t;
                    if (t >= 1f) flapsAnimDurationSec = 0f;
                }
                else
                {
                    flapsCircleY_Current = flapsCircleY_Target;
                }

                int diameter = 20;
                int drawY = (int)Math.Round(flapsCircleY_Current) - diameter / 2 + flapsIndYOffset;
                e.Graphics.FillEllipse(Brushes.White, new Rectangle(flapsIndX, drawY, diameter, diameter));
                // F12: mostrar track arrossegable
                if (calibrationMode)
                {
                    int trackTop = yUp + flapsIndYOffset;
                    int trackH   = yFull - yUp + diameter;
                    e.Graphics.DrawRectangle(new Pen(Color.Lime, 2), new Rectangle(flapsIndX - 2, trackTop - 2, diameter + 4, trackH + 4));
                    e.Graphics.DrawString("FLI", new Font("Arial", 7), Brushes.Lime, new PointF(flapsIndX, trackTop - 16));
                }
            }

            if (flapsAnimDurationSec > 0f)
                pbMain.Invalidate();

            // TRIM WHEEL IMAGE VERTICAL — segueix trimZone, rotat 90°
            if (trimWheelImg != null)
            {
                float imgW = trimWheelImg.Width;
                float imgH = trimWheelImg.Height;
                float scale = trimZone.Height / imgW;  // altura resultant = trimZone.Height
                float drawW = imgW * scale;            // = trimZone.Height
                float drawH = imgH * scale;            // amplada resultant (del ratio)
                float cx = trimZone.X + trimZone.Width / 2f;
                float cy = trimZone.Y + trimZone.Height / 2f;
                var state = e.Graphics.Save();
                e.Graphics.TranslateTransform(cx, cy);
                e.Graphics.RotateTransform(90);
                e.Graphics.DrawImage(trimWheelImg, -drawW / 2, -drawH / 2, drawW, drawH);
                e.Graphics.Restore(state);
            }

            // TRIM ARROWS
            if (trimDragging && trimLastDir != 0)
            {
                if (trimLastDir > 0)
                    DrawTrimArrow(e.Graphics, trimArrowDownPos.X, trimArrowDownPos.Y, -1);
                else
                    DrawTrimArrow(e.Graphics, trimArrowUpPos.X, trimArrowUpPos.Y, +1);
            }

            // TRIM CIRCLE — rang absolut 1537..1976 (+35%)
            int trimYUp = 1537;
            int trimYFull = 1976;
            float trimNorm = (float)((elevatorTrim + 1.0) / 2.0);
            int trimY = trimYUp + (int)(trimNorm * (trimYFull - trimYUp)) + trimIndYOffset;
            e.Graphics.FillEllipse(Brushes.White, new Rectangle(trimIndX, trimY - 10, 20, 20));
            if (calibrationMode)
            {
                int trkTop = trimYUp + trimIndYOffset;
                int trkH   = trimYFull - trimYUp + 20;
                e.Graphics.DrawRectangle(new Pen(Color.Lime, 2), new Rectangle(trimIndX - 2, trkTop - 2, 24, trkH + 4));
                e.Graphics.DrawString("TRI", new Font("Arial", 7), Brushes.Lime, new PointF(trimIndX, trkTop - 16));
                // TRZ — zona de drag del trim
                e.Graphics.DrawRectangle(new Pen(Color.Lime, 2), new Rectangle(trimZone.X - 2, trimZone.Y - 2, trimZone.Width + 4, trimZone.Height + 4));
                e.Graphics.DrawString("TRZ", new Font("Arial", 7), Brushes.Lime, new PointF(trimZone.X, trimZone.Y - 16));
            }

            // TRIM WHEEL IMAGE — segueix rudderTrimZone
            if (trimWheelImg != null)
            {
                int tw = rudderTrimZone.Width;
                int th = (int)(tw * (double)trimWheelImg.Height / trimWheelImg.Width);
                int ty = rudderTrimZone.Y + (rudderTrimZone.Height - th) / 2;
                e.Graphics.DrawImage(trimWheelImg, rudderTrimZone.X, ty, tw, th);
            }

            // RUDDER TRIM ARROWS
            if (rudderTrimDragging && rudderTrimLastDir != 0)
            {
                if (rudderTrimLastDir > 0)
                    DrawRudderTrimArrow(e.Graphics, rudderTrimArrowRightPos.X, rudderTrimArrowRightPos.Y, +1);
                else
                    DrawRudderTrimArrow(e.Graphics, rudderTrimArrowLeftPos.X,  rudderTrimArrowLeftPos.Y,  -1);
            }

            // RUDDER TRIM INDICATOR — track horitzontal
            {
                int rtXLeft  = 2760;
                int rtXRight = 3210;
                float rtNorm = (float)((rudderTrim + 1.0) / 2.0);
                int rtX = rtXLeft + (int)(rtNorm * (rtXRight - rtXLeft)) + rudderTrimIndXOffset;
                // marcador centre (línea vertical)
                int rtCenter = (rtXLeft + rtXRight) / 2 + rudderTrimIndXOffset;
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(180, 180, 180)), new Rectangle(rtCenter - 1, rudderTrimIndY - 42, 2, 20));
                // cercle indicador
                e.Graphics.FillEllipse(Brushes.White, new Rectangle(rtX - 10, rudderTrimIndY - 10, 20, 20));
                if (calibrationMode)
                {
                    int trkLeft = rtXLeft + rudderTrimIndXOffset;
                    int trkW    = rtXRight - rtXLeft + 20;
                    e.Graphics.DrawRectangle(new Pen(Color.Lime, 2), new Rectangle(trkLeft - 2, rudderTrimIndY - 12, trkW + 4, 24));
                    e.Graphics.DrawString("RRI", new Font("Arial", 7), Brushes.Lime, new PointF(trkLeft, rudderTrimIndY - 28));
                    // RRZ — zona de drag
                    e.Graphics.DrawRectangle(new Pen(Color.Lime, 2), new Rectangle(rudderTrimZone.X - 2, rudderTrimZone.Y - 2, rudderTrimZone.Width + 4, rudderTrimZone.Height + 4));
                    e.Graphics.DrawString("RRZ", new Font("Arial", 7), Brushes.Lime, new PointF(rudderTrimZone.X, rudderTrimZone.Y - 16));
                }
            }


            // SW_FLAPS — posició fixa ±75px mentre es prem, 0 en soltar
            {
                int newOffset = flapsAnimDir != 0 ? flapsAnimDir * 30 : 0;
                flapsAnimOffsetY = newOffset;
                var swFlaps = overlayImages.FirstOrDefault(o => o.Name == "SW_FLAPS");
                if (swFlaps != null) swFlaps.AnimOffsetY = newOffset;
            }
            // SW_LGEAR — toggle: UP=-50px, DN=+50px, sincronitzat amb GEAR HANDLE POSITION
            {
                var swLgear = overlayImages.FirstOrDefault(o => o.Name == "SW_LGEAR");
                if (swLgear != null)
                    swLgear.AnimOffsetY = lgearHandleUp ? -50 : 50;
            }

            // SW_PBRAKE — drawn manually below, no AnimOffsetY needed

            // Overlays: visibles sempre en calibratge, o quan ShowWhen ho indica
            foreach (var ov in overlayImages)
            {
                if (ov.Name == "SW_PBRAKE") continue;
                if (ov.Name == "SW_IGNITION") continue;
                if (ov.Name == "SW_IGNITION2") continue;
                if (ov.Name == "SW_FUEL1") continue;
                if (ov.Name == "SW_FUEL2") continue;
                if (ov.Img != null && (calibrationMode || (ov.ShowWhen?.Invoke() ?? true)))
                    e.Graphics.DrawImage(ov.Img, ov.AnimatedBounds);
            }

            // SW_PBRAKE — manual draw amb rotació i escala
            {
                var swPbrake = overlayImages.FirstOrDefault(o => o.Name == "SW_PBRAKE");
                if (swPbrake?.Img != null && (calibrationMode || pbrakeActive || true))
                {
                    float baseW = swPbrake.ZoomWidth;
                    float baseH = baseW / swPbrake.AspectRatio;
                    float drawW = pbrakeActive ? baseW * 1.15f : baseW;
                    float drawH = pbrakeActive ? baseH * 1.15f : baseH;
                    float angle = pbrakeActive ? 30f : 0f;
                    float cx    = swPbrake.BasePos.X + baseW / 2f;
                    float cy    = swPbrake.BasePos.Y + baseH / 2f;

                    var state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(cx, cy);
                    e.Graphics.RotateTransform(angle);
                    e.Graphics.DrawImage(swPbrake.Img, -drawW / 2f, -drawH / 2f, drawW, drawH);
                    e.Graphics.Restore(state);
                }
            }

            // SW_IGNITION — manual draw amb rotació segons ign1Pos
            // 0=OFF(-60°), 1=RIGHT(-30°), 2=LEFT(0°), 3=BOTH(+30°), 4=START(+60°)
            {
                var swIgn = overlayImages.FirstOrDefault(o => o.Name == "SW_IGNITION");
                if (swIgn?.Img != null)
                {
                    float angle = (ign1Pos - 2) * 30f; // LEFT=0°, OFF=-60°, START=+60°
                    float baseW = swIgn.ZoomWidth;
                    float baseH = baseW / swIgn.AspectRatio;
                    float cx    = swIgn.BasePos.X + baseW / 2f;
                    float cy    = swIgn.BasePos.Y + baseH / 2f;

                    var state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(cx, cy);
                    e.Graphics.RotateTransform(angle);
                    e.Graphics.DrawImage(swIgn.Img, -baseW / 2f, -baseH / 2f, baseW, baseH);
                    e.Graphics.Restore(state);
                }
            }

            // SW_IGNITION2 — manual draw amb rotació segons ign2Pos
            {
                var swIgn2 = overlayImages.FirstOrDefault(o => o.Name == "SW_IGNITION2");
                if (swIgn2?.Img != null)
                {
                    float angle = (ign2Pos - 2) * 30f;
                    float baseW = swIgn2.ZoomWidth;
                    float baseH = baseW / swIgn2.AspectRatio;
                    float cx    = swIgn2.BasePos.X + baseW / 2f;
                    float cy    = swIgn2.BasePos.Y + baseH / 2f;

                    var state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(cx, cy);
                    e.Graphics.RotateTransform(angle);
                    e.Graphics.DrawImage(swIgn2.Img, -baseW / 2f, -baseH / 2f, baseW, baseH);
                    e.Graphics.Restore(state);
                }
            }

            // SW_FUEL1: pos0=OFF(-30°), pos1=ON(0°), pos2=CROSSFEED(+30°)
            {
                var sw = overlayImages.FirstOrDefault(o => o.Name == "SW_FUEL1");
                if (sw?.Img != null)
                {
                    float angle = (fuel1Pos - 1) * 30f + 30f;
                    float baseW = sw.ZoomWidth;
                    float baseH = baseW / sw.AspectRatio;
                    float cx = sw.BasePos.X + baseW / 2f;
                    float cy = sw.BasePos.Y + baseH / 2f;
                    var state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(cx, cy);
                    e.Graphics.RotateTransform(angle);
                    e.Graphics.DrawImage(sw.Img, -baseW / 2f, -baseH / 2f, baseW, baseH);
                    e.Graphics.Restore(state);
                }
            }
            // SW_FUEL2: pos0=CROSSFEED(-30°), pos1=ON(0°), pos2=OFF(+30°)
            {
                var sw = overlayImages.FirstOrDefault(o => o.Name == "SW_FUEL2");
                if (sw?.Img != null)
                {
                    float angle = (fuel2Pos - 1) * 30f + 30f;
                    float baseW = sw.ZoomWidth;
                    float baseH = baseW / sw.AspectRatio;
                    float cx = sw.BasePos.X + baseW / 2f;
                    float cy = sw.BasePos.Y + baseH / 2f;
                    var state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(cx, cy);
                    e.Graphics.RotateTransform(angle);
                    e.Graphics.DrawImage(sw.Img, -baseW / 2f, -baseH / 2f, baseW, baseH);
                    e.Graphics.Restore(state);
                }
            }

            if (mainSurface.ShowAllAreas)
            {
                using var overlayBrush = new SolidBrush(Color.FromArgb(60, Color.Gray));

                // OverlayImages amb OnClick
                foreach (var ov in overlayImages)
                {
                    if (ov.OnClick != null)
                        e.Graphics.FillRectangle(overlayBrush, ov.Bounds);
                }

                // Trim wheel vertical
                e.Graphics.FillRectangle(overlayBrush, trimZone);

                // Rudder trim wheel horitzontal
                e.Graphics.FillRectangle(overlayBrush, rudderTrimZone);
            }


            // --- CALIBRATGE OVERLAY (F12 per activar/desactivar) ---
            if (calibrationMode)
            {
                using var btnBrush  = new SolidBrush(Color.FromArgb(120, 0, 120, 255));
                using var triBrush  = new SolidBrush(Color.FromArgb(150, 255, 220, 0));
                using var borderPen = new Pen(Color.White, 1);
                using var font      = new Font("Arial", 9, FontStyle.Bold);
                using var smallFont = new Font("Arial", 7, FontStyle.Bold);
                using var textBrush = new SolidBrush(Color.White);
                var centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

                foreach (var b in mainSurface.Buttons)
                {
                    e.Graphics.FillRectangle(btnBrush, b.ClickRect);
                    e.Graphics.DrawRectangle(borderPen, b.ClickRect);
                    e.Graphics.DrawString(b.Name, font, textBrush, b.ClickRect, centerFmt);
                    e.Graphics.DrawLine(Pens.White, b.BasePos.X - 6, b.BasePos.Y, b.BasePos.X + 6, b.BasePos.Y);
                    e.Graphics.DrawLine(Pens.White, b.BasePos.X, b.BasePos.Y - 6, b.BasePos.X, b.BasePos.Y + 6);
                    if (b == calibSelectedBtn)
                        using (var selPen = new Pen(Color.Yellow, 2))
                            e.Graphics.DrawRectangle(selPen, b.ClickRect);
                }

                foreach (var tri in audioTriangles)
                {
                    var r = new Rectangle(tri.BasePos.X - 15, tri.BasePos.Y - 10, 30, 20);
                    e.Graphics.FillRectangle(triBrush, r);
                    e.Graphics.DrawRectangle(borderPen, r);
                    e.Graphics.DrawString(tri.Name, smallFont, textBrush, r, centerFmt);
                    if (tri == calibSelectedTri)
                        using (var selPen = new Pen(Color.Yellow, 2))
                            e.Graphics.DrawRectangle(selPen, r);
                }

                using var ovPen    = new Pen(Color.Cyan, 2);
                using var selPen2  = new Pen(Color.Yellow, 2);
                using var handleBr = new SolidBrush(Color.FromArgb(200, 255, 165, 0));
                foreach (var b in mainSurface.Buttons.Where(b => b.Resizable))
                    e.Graphics.FillRectangle(handleBr, b.ResizeHandle);
                foreach (var ov in overlayImages)
                {
                    var ovBorder = (ov == calibSelectedOverlay) ? selPen2 : ovPen;
                    e.Graphics.DrawRectangle(ovBorder, ov.Bounds);
                    e.Graphics.FillRectangle(handleBr, ov.ResizeHandle);
                    e.Graphics.DrawString(ov.Name, smallFont, Brushes.Cyan,
                        new PointF(ov.BasePos.X, ov.BasePos.Y - 14));
                    e.Graphics.DrawString($"W:{(int)ov.ZoomWidth}", smallFont, Brushes.White,
                        new PointF(ov.ResizeHandle.X - 30, ov.ResizeHandle.Y + 2));
                }

                e.Graphics.FillRectangle(new SolidBrush(Color.DarkGreen), calibExportBtnRect);
                e.Graphics.DrawString("EXPORTA\ncoordenades", font, textBrush, calibExportBtnRect, centerFmt);
                e.Graphics.DrawString("CALIBRATGE ACTIU — F12 per sortir", font, Brushes.Red, new Point(200, 15));
            }

            }
            catch (OverflowException) { }
        }

        private void PbMain_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (calibrationMode)
            {
                if (calibExportBtnRect.Contains(e.Location)) { ExportCalibrationPositions(); return; }
                // Flaps indicator drag
                {
                    int trackTop = (450 + 1144) + flapsIndYOffset;
                    int trackH   = (680 - 450) + 20;
                    var trackRect = new Rectangle(flapsIndX - 2, trackTop - 2, 24, trackH + 4);
                    if (trackRect.Contains(e.Location))
                    {
                        calibDragFlapsInd    = true;
                        calibFlapsIndDragStart = e.Location;
                        calibFlapsIndStartX   = flapsIndX;
                        calibFlapsIndStartYOff = flapsIndYOffset;
                        pbMain.Invalidate(); return;
                    }
                }
                // Trim indicator drag
                {
                    int trkTop = 1537 + trimIndYOffset;
                    int trkH   = 1976 - 1537 + 20;
                    var trkRect = new Rectangle(trimIndX - 2, trkTop - 2, 24, trkH + 4);
                    if (trkRect.Contains(e.Location))
                    {
                        calibDragTrimInd     = true;
                        calibTrimIndDragStart = e.Location;
                        calibTrimIndStartX   = trimIndX;
                        calibTrimIndStartYOff = trimIndYOffset;
                        pbMain.Invalidate(); return;
                    }
                }
                // Trim zone drag (TRZ)
                {
                    var trzRect = new Rectangle(trimZone.X - 2, trimZone.Y - 2, trimZone.Width + 4, trimZone.Height + 4);
                    if (trzRect.Contains(e.Location))
                    {
                        calibDragTrimZone     = true;
                        calibTrimZoneDragStart = e.Location;
                        calibTrimZoneStartX   = trimZoneBaseX;
                        calibTrimZoneStartY   = trimZoneBaseY;
                        pbMain.Invalidate(); return;
                    }
                }
                // Rudder trim indicator drag (RRI)
                {
                    int rtXLeft  = 2760 + rudderTrimIndXOffset;
                    int rtXRight = 3210 + rudderTrimIndXOffset;
                    var rriRect = new Rectangle(rtXLeft - 2, rudderTrimIndY - 12, rtXRight - rtXLeft + 24, 24);
                    if (rriRect.Contains(e.Location))
                    {
                        calibDragRudderTrimInd     = true;
                        calibRudderTrimIndDragStart = e.Location;
                        calibRudderTrimIndStartY    = rudderTrimIndY;
                        calibRudderTrimIndStartXOff = rudderTrimIndXOffset;
                        pbMain.Invalidate(); return;
                    }
                }
                // Rudder trim zone drag (RRZ)
                {
                    var rrzRect = new Rectangle(rudderTrimZone.X - 2, rudderTrimZone.Y - 2, rudderTrimZone.Width + 4, rudderTrimZone.Height + 4);
                    if (rrzRect.Contains(e.Location))
                    {
                        calibDragRudderTrimZone     = true;
                        calibRudderTrimZoneDragStart = e.Location;
                        calibRudderTrimZoneStartX   = rudderTrimZoneBaseX;
                        calibRudderTrimZoneStartY   = rudderTrimZoneBaseY;
                        pbMain.Invalidate(); return;
                    }
                }
                foreach (var b in mainSurface.Buttons.Where(b => b.Resizable))
                {
                    if (b.ResizeHandle.Contains(e.Location))
                    {
                        calibResizeBtn          = b;
                        calibSelectedBtn        = b; calibSelectedTri = null; calibSelectedOverlay = null;
                        calibResizeBtnStartW    = b.W;
                        calibResizeBtnStartH    = b.H;
                        calibResizeBtnStartMouseX = e.X;
                        calibResizeBtnStartMouseY = e.Y;
                        pbMain.Invalidate(); return;
                    }
                }
                foreach (var ov in overlayImages)
                {
                    if (ov.ResizeHandle.Contains(e.Location))
                    {
                        calibResizeOverlay     = ov;
                        calibSelectedOverlay   = ov;
                        calibResizeStartWidth  = ov.ZoomWidth;
                        calibResizeStartMouseX = e.X;
                        calibZoomBox.Text      = ((int)ov.ZoomWidth).ToString();
                        calibZoomBox.Location  = new Point(ov.ResizeHandle.Right + 5, ov.ResizeHandle.Top);
                        calibZoomBox.Visible   = true;
                        calibZoomBox.Focus();
                        pbMain.Invalidate(); return;
                    }
                    if (ov.Bounds.Contains(e.Location))
                    {
                        calibDragOverlay       = ov;
                        calibSelectedOverlay   = ov;
                        calibOverlayDragOffset = new Point(e.X - ov.BasePos.X, e.Y - ov.BasePos.Y);
                        calibZoomBox.Visible   = false;
                        pbMain.Invalidate(); return;
                    }
                }
                foreach (var b in mainSurface.Buttons)
                {
                    if (b.ClickRect.Contains(e.Location))
                    {
                        calibDragBtn = b; calibDragOffset = new Point(e.X - b.BasePos.X, e.Y - b.BasePos.Y);
                        calibSelectedBtn = b; calibSelectedTri = null;
                        pbMain.Invalidate(); return;
                    }
                }
                foreach (var tri in audioTriangles)
                {
                    var r = new Rectangle(tri.BasePos.X - 15, tri.BasePos.Y - 10, 30, 20);
                    if (r.Contains(e.Location))
                    {
                        calibDragTri = tri; calibDragOffset = new Point(e.X - tri.BasePos.X, e.Y - tri.BasePos.Y);
                        calibSelectedTri = tri; calibSelectedBtn = null;
                        pbMain.Invalidate(); return;
                    }
                }
                calibSelectedBtn = null; calibSelectedTri = null;
                calibSelectedOverlay = null; calibZoomBox.Visible = false;
                pbMain.Invalidate();
                return;
            }

            foreach (var ov in overlayImages)
                if (ov.OnClick != null && ov.Bounds.Contains(e.Location))
                { ov.OnClick.Invoke(); return; }

            if (trimZone.Contains(e.Location))
            {
                trimDragging = true;
                trimLastMouseY = e.Y;
            }
            if (rudderTrimZone.Contains(e.Location))
            {
                rudderTrimDragging = true;
                rudderTrimLastMouseX = e.X;
            }
        }

        private void PbMain_MouseMove(object? sender, MouseEventArgs e)
        {
            if (calibrationMode)
            {
                if (calibDragFlapsInd)
                {
                    flapsIndX       = calibFlapsIndStartX    + (e.X - calibFlapsIndDragStart.X);
                    flapsIndYOffset = calibFlapsIndStartYOff + (e.Y - calibFlapsIndDragStart.Y);
                    pbMain.Invalidate(); return;
                }
                if (calibDragTrimInd)
                {
                    trimIndX       = calibTrimIndStartX    + (e.X - calibTrimIndDragStart.X);
                    trimIndYOffset = calibTrimIndStartYOff + (e.Y - calibTrimIndDragStart.Y);
                    pbMain.Invalidate(); return;
                }
                if (calibDragTrimZone)
                {
                    trimZoneBaseX  = calibTrimZoneStartX + (e.X - calibTrimZoneDragStart.X);
                    trimZoneBaseY  = calibTrimZoneStartY + (e.Y - calibTrimZoneDragStart.Y);
                    trimZone       = new Rectangle(trimZoneBaseX, trimZoneBaseY, 100, 507);
                    trimArrowUpPos   = new Point(trimZoneBaseX + 50, trimZoneBaseY + 465);
                    trimArrowDownPos = new Point(trimZoneBaseX + 50, trimZoneBaseY + 42);
                    pbMain.Invalidate(); return;
                }
                if (calibDragRudderTrimInd)
                {
                    rudderTrimIndY       = calibRudderTrimIndStartY    + (e.Y - calibRudderTrimIndDragStart.Y);
                    rudderTrimIndXOffset = calibRudderTrimIndStartXOff + (e.X - calibRudderTrimIndDragStart.X);
                    pbMain.Invalidate(); return;
                }
                if (calibDragRudderTrimZone)
                {
                    rudderTrimZoneBaseX  = calibRudderTrimZoneStartX + (e.X - calibRudderTrimZoneDragStart.X);
                    rudderTrimZoneBaseY  = calibRudderTrimZoneStartY + (e.Y - calibRudderTrimZoneDragStart.Y);
                    rudderTrimZone       = new Rectangle(rudderTrimZoneBaseX, rudderTrimZoneBaseY, 507, 100);
                    rudderTrimArrowLeftPos  = new Point(rudderTrimZoneBaseX + 42,  rudderTrimZoneBaseY + 50);
                    rudderTrimArrowRightPos = new Point(rudderTrimZoneBaseX + 465, rudderTrimZoneBaseY + 50);
                    pbMain.Invalidate(); return;
                }
                if (calibResizeBtn != null)
                {
                    calibResizeBtn.W = Math.Max(20, calibResizeBtnStartW + (e.X - calibResizeBtnStartMouseX));
                    calibResizeBtn.H = Math.Max(20, calibResizeBtnStartH + (e.Y - calibResizeBtnStartMouseY));
                    pbMain.Invalidate(); return;
                }
                if (calibResizeOverlay != null)
                {
                    float newW = Math.Max(10, calibResizeStartWidth + (e.X - calibResizeStartMouseX));
                    string tk  = calibResizeOverlay.TypeKey;
                    foreach (var ov in overlayImages.Where(o => o.TypeKey == tk))
                        ov.ZoomWidth = newW;
                    calibZoomBox.Text = ((int)newW).ToString();
                    pbMain.Invalidate(); return;
                }
                if (calibDragOverlay != null)
                {
                    calibDragOverlay.BasePos = new Point(
                        e.X - calibOverlayDragOffset.X, e.Y - calibOverlayDragOffset.Y);
                    pbMain.Invalidate(); return;
                }
                if (calibDragBtn != null)  { calibDragBtn.BasePos = new Point(e.X - calibDragOffset.X, e.Y - calibDragOffset.Y); pbMain.Invalidate(); }
                else if (calibDragTri != null) { calibDragTri.BasePos = new Point(e.X - calibDragOffset.X, e.Y - calibDragOffset.Y); pbMain.Invalidate(); }
                return;
            }
            if (trimDragging)
            {
                int deltaY = trimLastMouseY - e.Y;
                if (deltaY > 0) trimLastDir = +1;
                else if (deltaY < 0) trimLastDir = -1;
                trimLastMouseY = e.Y;
                trimAccumulatedDelta += deltaY;
                while (trimAccumulatedDelta >= TRIM_TICK_PIXELS)  { SendTrimTick(+1); trimAccumulatedDelta -= TRIM_TICK_PIXELS; }
                while (trimAccumulatedDelta <= -TRIM_TICK_PIXELS) { SendTrimTick(-1); trimAccumulatedDelta += TRIM_TICK_PIXELS; }
                pbMain.Invalidate();
            }
            if (rudderTrimDragging)
            {
                int deltaX = e.X - rudderTrimLastMouseX;   // dreta = positiu
                if (deltaX > 0) rudderTrimLastDir = +1;
                else if (deltaX < 0) rudderTrimLastDir = -1;
                rudderTrimLastMouseX = e.X;
                rudderTrimAccumulatedDelta += deltaX;
                while (rudderTrimAccumulatedDelta >= RUDDER_TRIM_TICK_PIXELS)  { SendRudderTrimTick(+1); rudderTrimAccumulatedDelta -= RUDDER_TRIM_TICK_PIXELS; }
                while (rudderTrimAccumulatedDelta <= -RUDDER_TRIM_TICK_PIXELS) { SendRudderTrimTick(-1); rudderTrimAccumulatedDelta += RUDDER_TRIM_TICK_PIXELS; }
                pbMain.Invalidate();
            }
        }

        private void SendTrimTick(int dir)
        {
            if (simconnect == null) return;

            if (dir > 0)
            {
                simconnect.TransmitClientEvent(0,SIMCONNECT_EVENTS.TRIM_DN,0,SIMCONNECT_GROUPS.GROUP0,SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            else
            {
                simconnect.TransmitClientEvent(0,SIMCONNECT_EVENTS.TRIM_UP,0,SIMCONNECT_GROUPS.GROUP0,SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
        }

        private void SendRudderTrimTick(int dir)
        {
            if (simconnect == null) return;
            if (dir > 0)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.RUDDER_TRIM_RIGHT, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            else
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.RUDDER_TRIM_LEFT,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }


        private void PbMain_MouseUp(object? sender, MouseEventArgs e)
        {
            if (calibrationMode)
            {
                if (calibDragFlapsInd)
                {
                    calibDragFlapsInd = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (calibDragTrimInd)
                {
                    calibDragTrimInd = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (calibDragTrimZone)
                {
                    calibDragTrimZone = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (calibDragRudderTrimInd)
                {
                    calibDragRudderTrimInd = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (calibDragRudderTrimZone)
                {
                    calibDragRudderTrimZone = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (calibResizeBtn != null)
                {
                    calibResizeBtn = null;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (calibResizeOverlay != null || calibDragOverlay != null)
                {
                    calibResizeOverlay = null;
                    calibDragOverlay   = null;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                calibDragBtn = null;
                calibDragTri = null;
                try { ExportCalibrationPositions(silent: true); } catch { }
                return;
            }
            // flapsAnimDir es reseteja automàticament pel Task.Delay dels OnClick
            trimDragging = false;
            trimLastDir = 0;
            rudderTrimDragging = false;
            rudderTrimLastDir = 0;
            pbMain.Invalidate();
        }


        private void frmMain_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!calibrationMode) return;

            int dx = 0, dy = 0;
            switch (e.KeyCode)
            {
                case Keys.Left:  dx = -1; break;
                case Keys.Right: dx =  1; break;
                case Keys.Up:    dy = -1; break;
                case Keys.Down:  dy =  1; break;
                case Keys.Escape:
                    calibSelectedBtn = null;
                    calibSelectedTri = null;
                    calibSelectedOverlay = null;
                    calibZoomBox.Visible = false;
                    pbMain.Invalidate();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    return;
                default: return;
            }

            if (calibSelectedBtn != null)
            {
                calibSelectedBtn.BasePos = new Point(calibSelectedBtn.BasePos.X + dx, calibSelectedBtn.BasePos.Y + dy);
                pbMain.Invalidate();
                try { ExportCalibrationPositions(silent: true); } catch { }
            }
            else if (calibSelectedTri != null)
            {
                calibSelectedTri.BasePos = new Point(calibSelectedTri.BasePos.X + dx, calibSelectedTri.BasePos.Y + dy);
                pbMain.Invalidate();
                try { ExportCalibrationPositions(silent: true); } catch { }
            }
            else if (calibSelectedOverlay != null)
            {
                calibSelectedOverlay.BasePos = new Point(
                    calibSelectedOverlay.BasePos.X + dx, calibSelectedOverlay.BasePos.Y + dy);
                pbMain.Invalidate();
                try { ExportCalibrationPositions(silent: true); } catch { }
            }

            e.Handled = true;
            e.SuppressKeyPress = true;
        }

        private void ExportCalibrationPositions(bool silent = false)
        {
            var sb = new System.Text.StringBuilder();
            foreach (var b in mainSurface.Buttons)
                if (b.Resizable)
                    sb.AppendLine($"BTN:{b.Name}:{b.BasePos.X}:{b.BasePos.Y}:{b.W}:{b.H}");
                else
                    sb.AppendLine($"BTN:{b.Name}:{b.BasePos.X}:{b.BasePos.Y}");
            foreach (var tri in audioTriangles)
                sb.AppendLine($"TRI:{tri.Name}:{tri.BasePos.X}:{tri.BasePos.Y}");
            foreach (var ov in overlayImages)
                sb.AppendLine($"IMG:{ov.Name}:{ov.BasePos.X}:{ov.BasePos.Y}:{(int)ov.ZoomWidth}");
            sb.AppendLine($"FLI:fli:{flapsIndX}:{flapsIndYOffset}");
            sb.AppendLine($"TRIND:tri:{trimIndX}:{trimIndYOffset}");
            sb.AppendLine($"TRIMZ:tz:{trimZoneBaseX}:{trimZoneBaseY}");
            sb.AppendLine($"RRI:rri:{rudderTrimIndXOffset}:{rudderTrimIndY}");
            sb.AppendLine($"RRZ:rrz:{rudderTrimZoneBaseX}:{rudderTrimZoneBaseY}");
            sb.AppendLine($"TACBTN:tactile:{_tactileBtn.Location.X}:{_tactileBtn.Location.Y}");
            sb.AppendLine($"CLOSEBTN:close:{closeButton.Location.X}:{closeButton.Location.Y}");
            sb.AppendLine($"SHOWBTN:show:{_showAreasBtn.Location.X}:{_showAreasBtn.Location.Y}");
            string projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.StartupPath, @"..\..\..\..\.."));
            string path = System.IO.Path.Combine(projectDir, "calibration_export.txt");
            System.IO.File.WriteAllText(path, sb.ToString());
            if (!silent)
                MessageBox.Show($"Exportat a:\n{path}", "Calibratge", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadCalibrationPositions()
        {
            string projectDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.StartupPath, @"..\..\..\..\.."));
            string path = System.IO.Path.Combine(projectDir, "calibration_export.txt");
            if (!System.IO.File.Exists(path)) return;
            foreach (var line in System.IO.File.ReadAllLines(path))
            {
                var parts = line.Split(':');
                if (parts.Length < 4) continue;
                string type = parts[0], name = parts[1];
                if (!int.TryParse(parts[2], out int x) || !int.TryParse(parts[3], out int y)) continue;
                if (type == "BTN")
                {
                    var b = mainSurface.Buttons.FirstOrDefault(b => b.Name == name);
                    if (b != null)
                    {
                        b.BasePos = new Point(x, y);
                        if (b.Resizable && parts.Length >= 6
                            && int.TryParse(parts[4], out int bw)
                            && int.TryParse(parts[5], out int bh))
                        { b.W = bw; b.H = bh; }
                    }
                }
                else if (type == "TRI")
                {
                    var tri = audioTriangles.FirstOrDefault(t => t.Name == name);
                    if (tri != null) tri.BasePos = new Point(x, y);
                }
                else if (type == "IMG" && parts.Length == 5 && int.TryParse(parts[4], out int w))
                {
                    var ov = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (ov != null)
                    {
                        ov.BasePos = new Point(x, y);
                        string tk = ov.TypeKey;
                        foreach (var same in overlayImages.Where(o => o.TypeKey == tk))
                            same.ZoomWidth = w;
                    }
                }
                else if (type == "FLI")
                {
                    flapsIndX = x;
                    flapsIndYOffset = y;
                }
                else if (type == "TRIND")
                {
                    trimIndX = x;
                    trimIndYOffset = y;
                }
                else if (type == "TRIMZ")
                {
                    trimZoneBaseX = x;
                    trimZoneBaseY = y;
                    trimZone       = new Rectangle(trimZoneBaseX, trimZoneBaseY, 100, 507);
                    trimArrowUpPos   = new Point(trimZoneBaseX + 50, trimZoneBaseY + 465);
                    trimArrowDownPos = new Point(trimZoneBaseX + 50, trimZoneBaseY + 42);
                }
                else if (type == "RRI")
                {
                    rudderTrimIndXOffset = x;
                    rudderTrimIndY       = y;
                }
                else if (type == "RRZ")
                {
                    rudderTrimZoneBaseX  = x;
                    rudderTrimZoneBaseY  = y;
                    rudderTrimZone       = new Rectangle(rudderTrimZoneBaseX, rudderTrimZoneBaseY, 507, 100);
                    rudderTrimArrowLeftPos  = new Point(rudderTrimZoneBaseX + 42,  rudderTrimZoneBaseY + 50);
                    rudderTrimArrowRightPos = new Point(rudderTrimZoneBaseX + 465, rudderTrimZoneBaseY + 50);
                }
                else if (type == "TACBTN")
                {
                    _tactileBtn.Location = new Point(x, y);
                }
                else if (type == "CLOSEBTN")
                {
                    closeButton.Location = new Point(x, y);
                }
                else if (type == "SHOWBTN")
                {
                    _showAreasBtn.Location = new Point(x, y);
                }
            }
        }

        private Image? LoadOverlayImage(string filename)
        {
            string path = System.IO.Path.Combine(Application.StartupPath, "Resources", filename);
            return System.IO.File.Exists(path) ? Image.FromFile(path) : null;
        }

        private void DrawTrimArrow(Graphics g, int x, int y, int dir)
        {
            int scale = 4;              // ← AQUÍ tens el factor (5x)
            int halfWidth = 8 * scale;  // abans ±8
            int height = 8 * scale;  // abans ±8

            Point[] pts;

            if (dir > 0)
            {
                // ↑
                pts = new[]
                {
            new Point(x, y + height),
            new Point(x - halfWidth, y - height),
            new Point(x + halfWidth, y - height)
                };
            }
            else
            {
                // ↓
                pts = new[]
                {
            new Point(x, y - height),
            new Point(x - halfWidth, y + height),
            new Point(x + halfWidth, y + height)
                };
            }

            g.FillPolygon(Brushes.Gray, pts);
        }

        private void DrawRudderTrimArrow(Graphics g, int x, int y, int dir)
        {
            int scale = 4;
            int halfWidth = 8 * scale;
            int height    = 8 * scale;
            Point[] pts;
            if (dir > 0) // →
                pts = new[] { new Point(x + height, y), new Point(x - height, y - halfWidth), new Point(x - height, y + halfWidth) };
            else         // ←
                pts = new[] { new Point(x - height, y), new Point(x + height, y - halfWidth), new Point(x + height, y + halfWidth) };
            g.FillPolygon(Brushes.Gray, pts);
        }

        private uint SingleEngineFuelVal(int f1, int f2)
        {
            // f1: 0=OFF,1=ON,2=CF(→OFF)   f2: 0=CF(→OFF),1=ON,2=OFF
            bool left  = f1 == 1;          // esquerre ON
            bool right = f2 == 1;          // dret ON
            if  (left && right)  return 1; // BOTH
            if  (left)           return 2; // LEFT
            if  (right)          return 3; // RIGHT
            return 0;                      // OFF
        }

        private void DrawGearCircle(Graphics g, int percent, int x, int y, int diameter, Color deployedColor = default)
        {
            if (deployedColor == default) deployedColor = Color.LimeGreen;
            Color color;

            if (percent <= 0)
            {
                color = Color.FromArgb(0, 30, 0); ; // gris fosc (UP)
            }
            else if (percent >= 100)
            {
                color = deployedColor; // DOWN
            }
            else
            {
                color = Color.Red; // en moviment
            }

            using (Brush b = new SolidBrush(color))
            {
                g.FillEllipse(b, x, y, diameter, diameter);
            }
        }




        private void DrawTriangle(Graphics g, Point center, Brush brush)
        {
            Point p1 = new Point(center.X,                center.Y + TRI_HEIGHT);
            Point p2 = new Point(center.X - TRI_HALF_WIDTH, center.Y - TRI_HEIGHT);
            Point p3 = new Point(center.X + TRI_HALF_WIDTH, center.Y - TRI_HEIGHT);
            g.FillPolygon(brush, new[] { p1, p2, p3 });
        }
        private void DrawLight(Graphics g, Rectangle rect, bool isOn)
        {
            // OFF sempre a sota
            g.DrawImage(imglightOff, rect);

            // ON només si toca
            if (isOn)
                g.DrawImage(imglightOn, rect);
        }

        

        private void DisconnectFromSimConnect()
        {
            if (simconnect != null)
            {
                simconnect.Dispose();
                simconnect = null;
            }

        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectFromSimConnect();
        }



        private void EnableFormDragging(Control control)
        {
            if (control == pbMain) return; // IMPORTANT: no volem drag sobre el PFD
            control.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;

                // 1) No fem drag si l’origen del clic és el closeButton o el checkBox1
                if (s == closeButton || s == checkBox1) return;

                // 2) Salvaguarda extra: si el cursor és DINS del checkBox1, tampoc fem drag
                if (checkBox1 != null && checkBox1.Visible && checkBox1.Enabled)
                {
                    var cbScreenRect = new Rectangle(checkBox1.PointToScreen(Point.Empty), checkBox1.Size);
                    if (cbScreenRect.Contains(Cursor.Position)) return;
                }

                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            };

            foreach (Control child in control.Controls)
            {
                if (child == pbMain) continue;
                // No enganxem el handler de drag ni al closeButton ni al checkBox1
                if (child == closeButton || child == checkBox1) continue;

                EnableFormDragging(child);
            }
        }
        internal static GraphicsPath CreateRoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }





        //private readonly System.Windows.Forms.Timer apVisualTimer = new System.Windows.Forms.Timer { Interval = 120 };

        private void FrmMain_MouseUp(object sender, MouseEventArgs e)
        {
            //if (!apButtonPressed) return;

            //apButtonPressed = false;

            //Rectangle rPb = new Rectangle(apButtonRect.X - pbMain.Left,apButtonRect.Y - pbMain.Top,apButtonRect.Width,apButtonRect.Height);

            //pbMain.Invalidate(rPb);

            // Enviem la comanda a MSFS (mantinc el que ja tens)
            //simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AP_MASTER, 0,
            //SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }











        // Inici Type-1 GC-------------------------------------------------------------------------------------------------------------------------------------------
        void ReadHidButtons1()
        {
            int vid = 0x16D0;       // 5840
            int pid = 0x128F;       // 4751 (Type-1 GC)

            while (!stopHidThread1)
            {
                try
                {
                    var device = DeviceList.Local.GetHidDevices(vid, pid).FirstOrDefault();
                    if (device == null) { isType1GCConnected = false; Thread.Sleep(500); continue; }
                    if (!device.TryOpen(out HidStream stream)) { isType1GCConnected = false; Thread.Sleep(500); continue; }
                    isType1GCConnected = true;

                    using (stream)
                    {
                        byte[] buffer = new byte[device.GetMaxInputReportLength()];
                        while (!stopHidThread1)
                        {
                            try
                            {
                                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                                if (bytesRead <= 0) break;

                                // Byte 1: B1..B8 ***********************************************************************************
                                if (bytesRead > 1)
                                {
                                    byte byte1 = buffer[1];

                                    // NAV PUSH----------------------------------------------------------------------------------------------
                                    bool b1Premut = (byte1 & 0x01) != 0;
                                    if (b1Premut && !prevB1)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.NAV_PUSH, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB1 = b1Premut;

                                    //HGG SYNC
                                    bool b2 = (byte1 & 0x02) != 0;   // Botó2
                                    if (b2 && !prevB2)
                                    {
                                        double hdgReal = lastData.gyro; // ja tens HEADING INDICATOR
                                        uint param = (uint)((((int)Math.Round(hdgReal)) % 360 + 360) % 360);

                                        simconnect?.TransmitClientEvent
                                        (
                                            0,
                                            SIMCONNECT_EVENTS.HDG_SYNC, param, SIMCONNECT_GROUPS.GROUP0,
                                            SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY
                                        );
                                    }
                                    prevB2 = b2;

                                    // XPDR ALT --------------------------------------------------------
                                    bool b3 = (byte1 & 0x04) != 0;   // Botó3
                                    if (b3 && !prevB3)
                                    {
                                        var set = new XPNDRWrite { xpdr_state = 4 }; // ALT
                                        simconnect?.SetDataOnSimObject(DEFINITIONS.XPNDR_SET, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, set);
                                    }
                                    prevB3 = b3;

                                    List<string> botons = new List<string>();

                                    // XPDR ON --------------------------------------------------------
                                    bool b4Pressed = (byte1 & 0x08) != 0;   // Botó4
                                    if (b4Pressed && !prevB4)
                                    {
                                        var set = new XPNDRWrite { xpdr_state = 3 }; // ON
                                        simconnect?.SetDataOnSimObject(DEFINITIONS.XPNDR_SET, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, set);
                                    }
                                    prevB4 = b4Pressed;

                                    // NAV SWITCH (swap) --------------------------------------------------------
                                    bool b5Pressed = (byte1 & 0x10) != 0;   // Botó5
                                    if (b5Pressed && !prevB5)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.NAV_SWITCH, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);

                                    }
                                    prevB5 = b5Pressed;

                                    //XPDR IDENT------------------------------------------------------------
                                    bool b6Pressed = (byte1 & 0x20) != 0;   // Botó6
                                    if (b6Pressed && !prevB6)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_IDNT, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB6 = b6Pressed;

                                    //ADF SWITCH (swap)--------------------------------------------------------
                                    bool b7Pressed = (byte1 & 0x40) != 0;   // Botó7
                                    if (b7Pressed && !prevB7)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ADF_SWITCH, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB7 = b7Pressed;

                                    // XPDR STBY ------------------------------------------------------------
                                    bool b8Pressed = (byte1 & 0x80) != 0;   // Botó8
                                    if (b8Pressed && !prevB8)
                                    {
                                        var set = new XPNDRWrite { xpdr_state = 1 }; // STBY
                                        simconnect?.SetDataOnSimObject(DEFINITIONS.XPNDR_SET, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, set);
                                    }
                                    prevB8 = b8Pressed;



                                    // B1..B4 (debug opcional)
                                    //bool b1Premut = (b1 & 0x01) != 0;
                                    if (b1Premut) botons.Add("B1");
                                    if ((byte1 & 0x02) != 0) botons.Add("B2");
                                    if ((byte1 & 0x04) != 0) botons.Add("B3");
                                    if ((byte1 & 0x08) != 0) botons.Add("B4");

                                    // Commutació NAV 1/2 amb B1 (tal com tenies)
                                    
                                    b1WasPressed_NAV = b1Premut;
                                }

                                // Byte 2: B9..B16 ***********************************************************************************
                                if (bytesRead > 2)
                                {
                                    byte byte2 = buffer[2];

                                    //NAV INNER-----------------------------------------------------------------------------
                                    bool b9 = (byte2 & 0x01) != 0;   // Inner INC
                                    if (b9 && !prevB9)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.NAV_INNER_INC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB9 = b9;

                                    bool b10 = (byte2 & 0x02) != 0;   // Inner DEC
                                    if (b10 && !prevB10)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.NAV_INNER_DEC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB10 = b10;

                                    //HEADING------------------------------------------------------------------------------
                                    // b11 -> HDG INC, b12 -> HDG DEC amb acceleració per velocitat de gir
                                    bool b11 = (byte2 & 0x04) != 0;   // HDG INC
                                    if (b11 && !prevB11)
                                    {
                                        int mult = ComputeAccelMultiplier(ref _gcHdgLastTick, ref _gcHdgWindowStart, ref _gcHdgAppliedInWindow);
                                        for (int i = 0; i < mult; i++)
                                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.HDG_BUG_INC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB11 = b11;

                                    // B12 -> HEADING BUG DEC (amb accelerador)
                                    bool b12 = (buffer[2] & 0x08) != 0;
                                    if (b12 && !prevB12)
                                    {
                                        int mult = ComputeAccelMultiplier(ref _gcHdgLastTick, ref _gcHdgWindowStart, ref _gcHdgAppliedInWindow);
                                        for (int i = 0; i < mult; i++)
                                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.HDG_BUG_DEC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB12 = b12;


                                    // ADF1 INNER---------------------------------------------------------------------------
                                    bool b13 = (byte2 & 0x10) != 0;   // TENTHS INC
                                    if (b13 && !prevB13)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ADF_INNER_INC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB13 = b13;

                                    bool b14 = (byte2 & 0x20) != 0;   // TENTHS DEC
                                    if (b14 && !prevB14)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ADF_INNER_DEC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB14 = b14;

                                    // ALT INNER--------------------------------------------------------------------------
                                    bool b15 = (byte2 & 0x40) != 0;   // ALT Small INC
                                    if (b15 && !prevB15)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ALT_G1000_INNER_INC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB15 = b15;

                                    bool b16 = (byte2 & 0x80) != 0;   // ALT Small DEC
                                    if (b16 && !prevB16)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ALT_G1000_INNER_DEC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB16 = b16;
                                }


                                if (bytesRead > 3)
                                {
                                    byte byte3 = buffer[3];

                                    //NAV OUTER-----------------------------------------------------------------------------
                                    bool b17 = (byte3 & 0x01) != 0;   // Outer INC
                                    if (b17 && !prevB17)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.NAV_OUTER_INC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB17 = b17;

                                    bool b18 = (byte3 & 0x02) != 0;   // Outer DEC
                                    if (b18 && !prevB18)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.NAV_OUTER_DEC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB18 = b18;

                                    // ADF1 OUTER----------------------------------------------------------------------------
                                    bool b21 = (byte3 & 0x10) != 0;   // WHOLE INC
                                    if (b21 && !prevB21)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ADF_OUTER_INC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB21 = b21;

                                    bool b22 = (byte3 & 0x20) != 0;   // WHOLE DEC
                                    if (b22 && !prevB22)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ADF_OUTER_DEC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB22 = b22;


                                    // ALT OUTER----------------------------------------------------------------------------
                                    bool b23 = (byte3 & 0x40) != 0;   // ALT Large INC
                                    if (b23 && !prevB23)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ALT_G1000_OUTER_INC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB23 = b23;

                                    bool b24 = (byte3 & 0x80) != 0;   // ALT Large DEC
                                    if (b24 && !prevB24)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ALT_G1000_OUTER_DEC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB24 = b24;
                                }


                                if (bytesRead > 4)
                                {
                                    byte byte4 = buffer[4];

                                    // XPDR_1000's-----------------------------------------------------------------------------
                                    bool b25 = (byte4 & 0x01) != 0;   // DIGIT 1 INC
                                    if (b25 && !prevB25)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_DIGIT_1_INC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB25 = b25;

                                    bool b26 = (byte4 & 0x02) != 0;   // DIGIT 1 DEC
                                    if (b26 && !prevB26)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_DIGIT_1_DEC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB26 = b26;

                                    // XPDR_100's-----------------------------------------------------------------------------
                                    bool b27 = (byte4 & 0x04) != 0;   // DIGIT 2 INC
                                    if (b27 && !prevB27)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_DIGIT_2_INC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB27 = b27;

                                    bool b28 = (byte4 & 0x08) != 0;   // DIGIT 2 DEC
                                    if (b28 && !prevB28)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_DIGIT_2_DEC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB28 = b28;

                                    // XPDR_10's-----------------------------------------------------------------------------
                                    bool b29 = (byte4 & 0x10) != 0;   // DIGIT 3 INC
                                    if (b29 && !prevB29)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_DIGIT_3_INC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB29 = b29;

                                    bool b30 = (byte4 & 0x20) != 0;   // DIGIT 3 DEC
                                    if (b30 && !prevB30)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_DIGIT_3_DEC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB30 = b30;

                                    // XPDR_1's-----------------------------------------------------------------------------
                                    bool b31 = (byte4 & 0x40) != 0;   // DIGIT 4 INC
                                    if (b31 && !prevB31)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_DIGIT_4_INC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB31 = b31;

                                    bool b32 = (byte4 & 0x80) != 0;   // DIGIT 4 DEC
                                    if (b32 && !prevB32)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.XPNDR_DIGIT_4_DEC, 0,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    prevB32 = b32;
                                }
                            }
                            catch (IOException) { break; }            // desconnexió típica
                            catch { /* ignora cicle i continua */ }
                        }
                    }

                    isType1GCConnected = false;        // forçar reobrir
                    Thread.Sleep(100);
                }
                catch { isType1GCConnected = false; Thread.Sleep(500); }
            }
        }
        // Fi Type-1 GC-------------------------------------------------------------------------------------------------------------------------------------------

        // Inici Type-2 GC (AM)-------------------------------------------------------------------------------------------------------------------------------------------
        void ReadHidButtons2()
        {
            int vid = 0x16D0;
            int pid = 0x13E5;       // Type-2 GC (AM)

            while (!stopHidThread2)
            {
                try
                {
                    var device = DeviceList.Local.GetHidDevices(vid, pid).FirstOrDefault();
                    if (device == null) { isType2GCAMConnected = false; Thread.Sleep(500); continue; }
                    if (!device.TryOpen(out HidStream stream)) { isType2GCAMConnected = false; Thread.Sleep(500); continue; }
                    isType2GCAMConnected = true;

                    using (stream)
                    {
                        byte[] buffer = new byte[device.GetMaxInputReportLength()];
                        while (!stopHidThread2)
                        {
                            try
                            {
                                int read = stream.Read(buffer, 0, buffer.Length);
                                if (read <= 0) break;


                                // --- processat existent ---
                                byte b1 = buffer[1]; // (abans tenies buffer[10])
                                List<string> botons = new List<string>();

                                if ((b1 & 0x01) != 0) botons.Add("B1");
                                if ((b1 & 0x02) != 0) botons.Add("B2");
                                if ((b1 & 0x04) != 0) botons.Add("B3");
                                if ((b1 & 0x08) != 0) botons.Add("B4");
                                if ((b1 & 0x10) != 0) botons.Add("B5");
                                if ((b1 & 0x20) != 0) botons.Add("B6");
                                if ((b1 & 0x40) != 0) botons.Add("B7");
                                if ((b1 & 0x80) != 0) botons.Add("B8");

                                bool b1Premut = botons.Contains("B1");
                                b1WasPressed_COM = b1Premut;
                            }
                            catch (IOException) { break; }
                            catch { }
                        }
                    }

                    isType2GCAMConnected = false;
                    Thread.Sleep(100);
                }
                catch { isType2GCAMConnected = false; Thread.Sleep(500); }
            }
        }

        // Fi Type-2 GC AM)-------------------------------------------------------------------------------------------------------------------------------------------

        

        

        // Inici Type-2 GC-------------------------------------------------------------------------------------------------------------------------------------------
        void ReadHidButtons5()
        {
            int vid = 0x16D0;
            int pid = 0x13E3;       // Type-2 GC (AM)

            while (!stopHidThread5)
            {
                try
                {
                    var device = DeviceList.Local.GetHidDevices(vid, pid).FirstOrDefault();
                    if (device == null) { isType2GCConnected = false; Thread.Sleep(500); continue; }
                    if (!device.TryOpen(out HidStream stream)) { isType2GCConnected = false; Thread.Sleep(500); continue; }
                    isType2GCConnected = true;

                    using (stream)
                    {
                        byte[] buffer = new byte[device.GetMaxInputReportLength()];
                        while (!stopHidThread5)
                        {
                            try
                            {
                                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                                if (bytesRead <= 0) break;

                                // Byte 1: B1..B8 ***********************************************************************************
                                if (bytesRead > 1)
                                {
                                    byte b1 = buffer[1];

                                    // B1 -> COM PUSH-----------------------------------------------------------------------------
                                    bool b1Pressed_b = (b1 & 0x01) != 0;
                                    if (b1Pressed_b && !prevB1_b)
                                    {
                                        // 1) Envia l’event SEMPRE (com NAV)
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COM_PUSH, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);

                                        // 2) Feedback visual NOMÉS si ja estem sincronitzats amb el sim
                                        
                                    }
                                    prevB1_b = b1Pressed_b;

                                    // B2 -> CRS PUSH-----------------------------------------------------------------------------
                                    bool b2Pressed_b = (b1 & 0x02) != 0;
                                    if (b2Pressed_b && !prevB2_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.CRS_PUSH, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB2_b = b2Pressed_b;

                                    // B3 -> PAN PUSH----------------------------------------------------------------------------
                                    bool b3Pressed_b = (b1 & 0x04) != 0;
                                    if (b3Pressed_b && !prevB3_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PAN_PUSH, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB3_b = b3Pressed_b;

                                    // B4 -> FMS MFD PUSH----------------------------------------------------------------------------
                                    bool b4Pressed_b = (b1 & 0x08) != 0;
                                    if (b4Pressed_b && !prevB4_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.CRSR_MDF_PUSH, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB4_b = b4Pressed_b;

                                    // B5 -> COM SWITCH-------------------------------------------------------------------------
                                    bool b5Pressed_b = (b1 & 0x10) != 0;
                                    if (b5Pressed_b && !prevB5_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COM_SWITCH, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB5_b = b5Pressed_b;

                                    // B8 -> SOFTKEY 6 (CDI) --------------------------------------------
                                    bool b8Pressed_b = (b1 & 0x80) != 0;
                                    if (b8Pressed_b && !prevB8_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.CDI, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB8_b = b8Pressed_b;

                                }

                                // Byte 2: B9..B16 ********************************************************************************
                                if (bytesRead > 2)
                                {
                                    byte byte2 = buffer[2];

                                    // B9 -> COM INNER INC -------------------------------------------
                                    bool b9Pressed_b = (byte2 & 0x01) != 0;   // bit 0
                                    if (b9Pressed_b && !prevB9_b)
                                    {
                                        int mult = ComputeAccelMultiplier(ref _comInnerLastTick, ref _comInnerWindowStart, ref _comInnerAppliedInWindow);
                                        for (int i = 0; i < mult; i++)
                                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COM_INNER_INC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB9_b = b9Pressed_b;

                                    // B10 -> COM INNER DEC (G1000) ----------------------------------
                                    bool b10Pressed_b = (byte2 & 0x02) != 0;  // bit 1
                                    if (b10Pressed_b && !prevB10_b)
                                    {
                                        int mult = ComputeAccelMultiplier(ref _comInnerLastTick, ref _comInnerWindowStart, ref _comInnerAppliedInWindow);
                                        for (int i = 0; i < mult; i++)
                                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COM_INNER_DEC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB10_b = b10Pressed_b;

                                    // B11 -> CRS INC ---------------------------------------------------
                                    bool b11Pressed_b = (byte2 & 0x04) != 0;   // bit2
                                    if (b11Pressed_b && !prevB11_b)
                                    {
                                        int mult = ComputeAccelMultiplier(ref _crsLastTick, ref _crsWindowStart, ref _crsAppliedInWindow);
                                        for (int i = 0; i < mult; i++)
                                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.CRS_INC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB11_b = b11Pressed_b;

                                    /// B12 -> CRS DEC ---------------------------------------------------
                                    bool b12Pressed_b = (byte2 & 0x08) != 0;   // bit3
                                    if (b12Pressed_b && !prevB12_b)
                                    {
                                        int mult = ComputeAccelMultiplier(ref _crsLastTick, ref _crsWindowStart, ref _crsAppliedInWindow);
                                        for (int i = 0; i < mult; i++)
                                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.CRS_DEC, 1,
                                            SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB12_b = b12Pressed_b;

                                    // B13 -> MFD RANGE DEC (Zoom -) -----------------------------------
                                    bool b13Pressed_b = (byte2 & 0x10) != 0;   // bit4
                                    if (b13Pressed_b && !prevB13_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_RANGE_INC, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB13_b = b13Pressed_b;

                                    // B14 -> MFD RANGE INC (Zoom +) -----------------------------------
                                    bool b14Pressed_b = (byte2 & 0x20) != 0;   // bit5
                                    if (b14Pressed_b && !prevB14_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_RANGE_DEC, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB14_b = b14Pressed_b;

                                    // B15 -> MFD FMS INNER DEC --------------------------------------------
                                    bool b15Pressed_b = (byte2 & 0x40) != 0;   // bit6
                                    if (b15Pressed_b && !prevB15_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FMS_MFD_INNER_INC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB15_b = b15Pressed_b;

                                    // B16 -> MFD FMS INNER INC --------------------------------------------
                                    bool b16Pressed_b = (byte2 & 0x80) != 0;   // bit7
                                    if (b16Pressed_b && !prevB16_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FMS_MFD_INNER_DEC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB16_b = b16Pressed_b;

                                }

                                // Byte 3: B17..B24 ***********************************************************************************
                                if (bytesRead > 3)
                                {
                                    byte byte3 = buffer[3];

                                    // B17 -> COM OUTER INC --------------------------------------------
                                    bool b17Pressed_b = (byte3 & 0x01) != 0;   // bit0
                                    if (b17Pressed_b && !prevB17_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COM_OUTER_INC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB17_b = b17Pressed_b;

                                    // B18 -> COM OUTER DEC --------------------------------------------
                                    bool b18Pressed_b = (byte3 & 0x02) != 0;   // bit1
                                    if (b18Pressed_b && !prevB18_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COM_OUTER_DEC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB18_b = b18Pressed_b;

                                    // B19 -> BARO INC -------------------------------------------------
                                    bool b19Pressed_b = (byte3 & 0x04) != 0;   // bit2
                                    if (b19Pressed_b && !prevB19_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.BARO_INC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB19_b = b19Pressed_b;

                                    // B20 -> BARO DEC -------------------------------------------------
                                    bool b20Pressed_b = (byte3 & 0x08) != 0;   // bit3
                                    if (b20Pressed_b && !prevB20_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.BARO_DEC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB20_b = b20Pressed_b;
                                    // B21 -> MFD PAN LEFT ---------------------------------------------
                                    bool b21Pressed_b = (byte3 & 0x10) != 0;   // bit4
                                    if (b21Pressed_b && !prevB21_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_PAN_RIGHT, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB21_b = b21Pressed_b;

                                    // B22 -> MFD PAN RIGHT --------------------------------------------
                                    bool b22Pressed_b = (byte3 & 0x20) != 0;   // bit5
                                    if (b22Pressed_b && !prevB22_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_PAN_LEFT, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB22_b = b22Pressed_b;
                                    // B23 -> FMS MFD OUTER INC ----------------------------------------
                                    bool b23Pressed_b = (byte3 & 0x40) != 0;   // bit6
                                    if (b23Pressed_b && !prevB23_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FMS_MFD_OUTER_INC, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB23_b = b23Pressed_b;

                                    // B24 -> FMS MFD OUTER DEC ----------------------------------------
                                    bool b24Pressed_b = (byte3 & 0x80) != 0;   // bit7
                                    if (b24Pressed_b && !prevB24_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FMS_MFD_OUTER_DEC, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB24_b = b24Pressed_b;


                                }

                                // Byte 4: B25..B32 **********************************************************************************
                                if (bytesRead > 4)
                                {
                                    byte byte4 = buffer[4];

                                    // B27 -> BARO STD ----------------------------------------------
                                    bool b27Pressed_b = (byte4 & 0x04) != 0;   // bit2
                                    if (b27Pressed_b && !prevB27_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.BARO_STD, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB27_b = b27Pressed_b;

                                    // B28 -> BARO SET ----------------------------------------------
                                    bool b28Pressed_b = (byte4 & 0x08) != 0;   // bit3
                                    if (b28Pressed_b && !prevB28_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.BARO_SET, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB28_b = b28Pressed_b;

                                    // B29 -> MFD PAN UP -----------------------------------------------
                                    bool b29Pressed_b = (byte4 & 0x10) != 0;   // bit4
                                    if (b29Pressed_b && !prevB29_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_PAN_UP, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB29_b = b29Pressed_b;

                                    // B30 -> MFD PAN DOWN ---------------------------------------------
                                    bool b30Pressed_b = (byte4 & 0x20) != 0;   // bit5
                                    if (b30Pressed_b && !prevB30_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MFD_PAN_DOWN, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB30_b = b30Pressed_b;

                                }

                                // Byte 5: B33..B40 **********************************************************************************
                                if (bytesRead > 5)
                                {
                                    byte byte5 = buffer[5];

                                    // B34 -> FMS PFD INNER INC ----------------------------------------
                                    bool b34Pressed_b = (byte5 & 0x02) != 0;   // B34 (bit1)
                                    if (b34Pressed_b && !prevB34_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FMS_PFD_INNER_INC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB34_b = b34Pressed_b;

                                    // B35 -> FMS PFD INNER DEC ----------------------------------------
                                    bool b35Pressed_b = (byte5 & 0x04) != 0;   // B35 (bit2)
                                    if (b35Pressed_b && !prevB35_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FMS_PFD_INNER_DEC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB35_b = b35Pressed_b;

                                    // B36 -> FMS PFD OUTER INC ----------------------------------------
                                    bool b36Pressed_b = (byte5 & 0x08) != 0;   // bit3
                                    if (b36Pressed_b && !prevB36_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FMS_PFD_OUTER_INC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB36_b = b36Pressed_b;

                                    // B37 -> FMS PFD OUTER DEC ----------------------------------------
                                    bool b37Pressed_b = (byte5 & 0x10) != 0;   // bit4
                                    if (b37Pressed_b && !prevB37_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FMS_PFD_OUTER_DEC, 1,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB37_b = b37Pressed_b;

                                    // B38 -> CRSR PFD PUSH --------------------------------------------------
                                    bool b38Pressed_b = (byte5 & 0x20) != 0;   // B38
                                    if (b38Pressed_b && !prevB38_b)
                                    {
                                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.CRSR_PFD_PUSH, 0,
                                        SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    }
                                    prevB38_b = b38Pressed_b;
                                }
                            }
                            catch (IOException) { break; }
                            catch { }
                        }
                    }

                    isType2GCConnected = false;
                    Thread.Sleep(100);
                }
                catch { isType2GCConnected = false; Thread.Sleep(500); }
            }
        }
        // Fi Type-2 GC-------------------------------------------------------------------------------------------------------------------------------------------

        
        


    }
}
