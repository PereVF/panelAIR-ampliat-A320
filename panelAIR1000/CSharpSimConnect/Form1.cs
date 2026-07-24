


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
        bool propDeiceActive;
        bool antiIceEng1Active;
        double _isDeiceAirframe;
        double _isDeiceAirframe2;
        bool _ltsTestActive;
        bool pitotHeatLActive;
        bool pitotHeatRActive;
        bool windshieldDeiceActive;
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
        private float _g3kFuelSelAngle = -1f;       // -1 = no inicialitzat
        private float _g3kFuelSelTargetAngle = 0f;
        private DateTime _g3kFuelSelLastPaint = DateTime.MinValue;

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
        // G3K AP slider (nose up / nose dn)
        private int g3kApSliderBaseX = 10;
        private int g3kApSliderBaseY = 10;
        private Rectangle g3kApSliderZone      = new Rectangle(10, 10, 30, 200);
        private bool calibDragG3kApSlider = false;
        private Point calibG3kApSliderDragStart;
        private int calibG3kApSliderStartX;
        private int calibG3kApSliderStartY;
        // G3K AP input zone (zona d'input, independent de la visual)
        private int g3kApSliderInputBaseX = 10;
        private int g3kApSliderInputBaseY = 10;
        private Rectangle g3kApSliderInputZone = new Rectangle(10, 10, 70, 300);
        private bool calibDragG3kApSliderInput = false;
        private Point calibG3kApSliderInputDragStart;
        private int calibG3kApSliderInputStartX;
        private int calibG3kApSliderInputStartY;
        // LVL indicator zone (green transparent, draggable+resizable)
        private int _apLvlIndBaseX = 200, _apLvlIndBaseY = 50;
        private int _apLvlIndW = 60, _apLvlIndH = 40;
        private Rectangle _apLvlIndRect = new Rectangle(200, 50, 60, 40);
        private bool _calibDragApLvlInd = false;
        private bool _calibResizeApLvlInd = false;
        private Point _calibApLvlIndDragStart;
        private int _calibApLvlIndStartX, _calibApLvlIndStartY, _calibApLvlIndStartW, _calibApLvlIndStartH;

        private bool g3kApSliderDragging = false;
        private int g3kApSliderLastMouseY;
        private int g3kApSliderAccumDelta = 0;
        private const int G3K_AP_SLIDER_TICK_PIXELS = 40;
        private float _g3kApWheelOffset = 0f;
        private Image[] trimFrames = new Image[26];
        private float trimFramePos = 0f;
        private const float TRIM_PIXELS_PER_FRAME = 9f;
        private const int TRIM_FRAME_COUNT = 26;

        // ================= RUDDER TRIM =================
        private double rudderTrim = 0;
        private int rudderTrimAccumulatedDelta = 0;
        private bool rudderTrimDragging = false;
        private int  rudderTrimLastMouseX;
        private int  rudderTrimLastDir = 0;
        private Image[] rudderTrimFrames = new Image[26];
        private float rudderTrimFramePos = 0f;
        private const int RUDDER_TRIM_FRAME_COUNT = 26;
        private const float RUDDER_TRIM_PIXELS_PER_FRAME = 9f;
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

        // G3K Trim wheel
        private int _g3kTrimZoneBaseX = 3685;
        private int _g3kTrimZoneBaseY = 1564;
        private Rectangle _g3kTrimZone = new Rectangle(3685, 1564, 119, 603);
        private bool _g3kTrimDragging = false;
        private int _g3kTrimLastMouseY;
        private int _g3kTrimAccumulatedDelta = 0;
        private int _g3kTrimLastDir = 0;
        private bool _calibDragG3kTrimZone = false;
        private Point _calibG3kTrimZoneDragStart;
        private int _calibG3kTrimZoneStartX;
        private int _calibG3kTrimZoneStartY;
        // G3K Trim indicator
        private int _g3kTrimIndX = 3620;
        private int _g3kTrimIndYOffset = 0;
        private bool _calibDragG3kTrimInd = false;
        private Point _calibG3kTrimIndDragStart;
        private int _calibG3kTrimIndStartX;
        private int _calibG3kTrimIndStartYOff;
        // G3K Trim zone resize (ratio fix 119:603)
        private bool _calibResizeG3kTrimZone = false;
        private Point _calibResizeG3kTrimZoneDragStart;
        private int _calibResizeG3kTrimZoneStartH;
        // G3K Rudder Trim wheel (horitzontal, frames sense rotar)
        private int _g3kRudderTrimZoneBaseX = 1280;
        private int _g3kRudderTrimZoneBaseY = 2000;
        private Rectangle _g3kRudderTrimZone = new Rectangle(1280, 2000, 385, 76);
        private bool _g3kRudderTrimDragging = false;
        private int _g3kRudderTrimLastMouseX;
        private int _g3kRudderTrimAccumulatedDelta = 0;
        private int _g3kRudderTrimLastDir = 0;
        private bool _calibDragG3kRudderTrimZone = false;
        private Point _calibG3kRudderTrimZoneDragStart;
        private int _calibG3kRudderTrimZoneStartX;
        private int _calibG3kRudderTrimZoneStartY;
        private bool _calibResizeG3kRudderTrimZone = false;
        private Point _calibResizeG3kRudderTrimZoneDragStart;
        private int _calibResizeG3kRudderTrimZoneStartW;
        // G3K Rudder Trim indicator
        private int _g3kRudderTrimIndY = 2038;
        private int _g3kRudderTrimIndXOffset = 0;
        private bool _calibDragG3kRudderTrimInd = false;
        private Point _calibG3kRudderTrimIndDragStart;
        private int _calibG3kRudderTrimIndStartY;
        private int _calibG3kRudderTrimIndStartXOff;
        // G3K Aileron Trim buttons
        private double _aileronTrimFrac = 0.0;
        private int _aileronTrimPos = 0;       // -16383..+16383, 328 units per 1%
        private DateTime _ailTrimIndLeftUntilUtc  = DateTime.MinValue;
        private DateTime _ailTrimIndRightUntilUtc = DateTime.MinValue;






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
        private Image? _bgG3000Image;
        private bool   _g3000Active = false;
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

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();


        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;
        private const byte VK_VOLUME_UP    = 0xAF;
        private const byte VK_VOLUME_DOWN  = 0xAE;
        private const byte VK_MENU         = 0x12;
        private const byte VK_RETURN       = 0x0D;
        private const byte VK_ESCAPE       = 0x1B;
        private const uint KEYEVENTF_KEYUP      = 0x0002;

        private Struct1 lastData;

        private int nav1SwapCnt = 0; // comptador B5 (NAV1_SWAP)
        private int nav2SwapCnt = 0; // comptador B38 (NAV2_SWAP)

        enum DEFINITIONS
        {
            Struct1,
            XPNDR_SET,
            CameraState,
            ElecSrcWrite,
            ElecGenWrite,
            ElecCrashbarWrite,
            ElecStarterWrite,
            ElecIgnitionWrite,
            APTrimWrite,
            AileronTrimWrite,
            LightingLogo1Write,
            FuelSelectionWrite,
            CondLeverCutoffWrite,
            PropLeverWrite,
            PropDeiceWrite,
            LtsTestWrite,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ElecSrcStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ElecGenStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ElecCrashbarStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ElecStarterStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ElecIgnitionStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct APTrimStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct LightingLogo1Struct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FuelSelectionStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CondLeverCutoffStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PropLeverStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PropDeiceWriteStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct LtsTestWriteStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct AileronTrimWriteStruct { public double value; }

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
            // G3000 PFD1 softkeys
            G3K_SK_1,  G3K_SK_2,  G3K_SK_3,  G3K_SK_4,
            G3K_SK_5,  G3K_SK_6,  G3K_SK_7,  G3K_SK_8,
            G3K_SK_9,  G3K_SK_10, G3K_SK_11, G3K_SK_12,
            // G3000 MFD1 softkeys
            G3K_MFD_SK_1,  G3K_MFD_SK_2,  G3K_MFD_SK_3,  G3K_MFD_SK_4,
            G3K_MFD_SK_5,  G3K_MFD_SK_6,  G3K_MFD_SK_7,  G3K_MFD_SK_8,
            G3K_MFD_SK_9,  G3K_MFD_SK_10, G3K_MFD_SK_11, G3K_MFD_SK_12,
            // G3000 TSC softkeys
            G3K_TSC1_SK_1, G3K_TSC1_SK_2, G3K_TSC1_SK_3,
            G3K_TSC2_SK_1, G3K_TSC2_SK_2, G3K_TSC2_SK_3,
            G3K_AP_MASTER,
            G3K_AP_HDG_HOLD,
            G3K_AP_NAV1_HOLD,
            G3K_AP_APR_HOLD,
            G3K_XFR,
            G3K_AP_VS_HOLD,
            G3K_AP_FLC,
            G3K_AP_FD,
            G3K_AP_ALT,
            G3K_AP_VNV,
            G3K_AP_BC,
            G3K_AP_MACH_TOGGLE,
            G3K_AP_YAW_DAMPER,
            G3K_AP_LVL,
            G3K_AP_BANK_INC,
            G3K_HDG_SYNC,
            G3K_CRS1_DIR,
            G3K_CRS2_DIR,
            G3K_TSC1_TKP,
            G3K_TSC1_BKP,
            G3K_MASTER_WARN,
            G3K_MASTER_CAUT,
            G3K_AP_KAP140_UP,
            G3K_AP_KAP140_DN,
            G3K_AP_SPD_DEC,
            G3K_AP_SPD_INC,
            G3K_AP_VS_VAR_INC,
            G3K_AP_VS_VAR_DEC,
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
            PROPDEICE_TOGGLE,
            INERT_SEP_TOGGLE,
            PITOTHEAT_ON,
            PITOTHEAT_OFF,
            PITOTHEAT2_ON,
            PITOTHEAT2_OFF,
            WING_LIGHTS_SET,
            WINDSHIELD_DEICE_SET,
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
            TBM930_IGN1_ON,
            TBM930_IGN1_OFF,
            TBM930_IGN1_AUTO,
            ELECT_FUEL_PUMP1_SET,
            SET_STARTER1_HELD,
            STARTER1_SET,
            AUTOPILOT_DISENGAGE_TOGGLE,
            RUDDER_TRIM_DISABLED_SET,
            AILERON_TRIM_DISABLED_SET,
            AXIS_AILERONTRIM_SET,
            AILERON_TRIM_RIGHT,
            AILERON_TRIM_LEFT,
            AILERON_TRIM_SET_EX1,
            TBM930_PULSELIGHT_TOGGLE,
            TBM930_LIGHT_LDG,
            TBM930_LIGHT_TAXI,
            TBM930_LIGHT_OFF,
            TBM930_FUELSEL_AUTO,
            TBM930_FUELSEL_MAN,
            SET_FUEL_VALVE_ENG1,
            PROP_PITCH1_SET,
            AXIS_PROPELLER1_SET,
            PROPELLER_SET,
            AXIS_PROPELLER_SET,
            PROPELLER1_SET,
            PROP_PITCH1_AXIS_SET_EX,
            CONDITION_LEVER_SET,
            PROP_PITCH_SET,
            AXIS_THROTTLE_SET,
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
            ENGINE_AUTO_START,
            ENGINE_AUTO_SHUTDOWN,

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
        private readonly System.Windows.Forms.Timer _thrSyncTimer  = new System.Windows.Forms.Timer { Interval = 3000 };

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
            public bool IsRound = false;
            public bool IsVisuallyPressed(DateTime now) => !NoShadow && now < PressedUntilUtc;
            public Func<bool>? ShowWhen;
            public bool IsVisible() => ShowWhen == null || ShowWhen();
            public Action? OnClick;
        }

        private sealed class UiSurface
        {
            public PictureBox Pb;
            public List<UiButton> Buttons = new();
            public Func<bool>? SuppressInput;
            public Func<bool>? HideAreasWhenActive;
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
                Pb.Cursor = Buttons.Any(b => b.IsVisible() && b.ClickRect.Contains(e.Location))
                    ? Cursors.Hand
                    : Cursors.Default;
            }

            private void OnPaint(object? sender, PaintEventArgs e)
            {
                using var brush = new SolidBrush(Color.FromArgb(90, Color.White));
                using var allAreasBrush = new SolidBrush(Color.FromArgb(60, Color.Orange));
                var now = DateTime.UtcNow;
                foreach (var b in Buttons)
                {
                    if (!b.IsVisible()) continue;
                    if (ShowAllAreas && !(HideAreasWhenActive?.Invoke() ?? false))
                    {
                        if (b.IsRound)
                            e.Graphics.FillEllipse(allAreasBrush, b.ClickRect);
                        else
                            e.Graphics.FillRectangle(allAreasBrush, b.ClickRect);
                    }
                    else if (b.IsVisuallyPressed(now))
                    {
                        if (b.IsRound)
                        {
                            e.Graphics.FillEllipse(brush, b.ClickRect);
                        }
                        else
                        {
                            using var path = frmMain.CreateRoundedRect(b.ShadowRect, BTN_RADIUS);
                            e.Graphics.FillPath(brush, path);
                        }
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
            public float  ZoomHeight = 0;
            public Image? Img;
            public float AspectRatio => (Img != null && Img.Height > 0) ? (float)Img.Width / Img.Height : 1f;
            private int CalcH => ZoomHeight > 0 ? (int)ZoomHeight : (int)(ZoomWidth / AspectRatio);
            public Rectangle Bounds => new Rectangle(BasePos.X, BasePos.Y, (int)ZoomWidth, CalcH);
            public Rectangle ResizeHandle => new Rectangle(
                Bounds.Right - 12, Bounds.Bottom - 12, 16, 16);
            public int AnimOffsetY = 0;
            public Rectangle AnimatedBounds => new Rectangle(
                BasePos.X, BasePos.Y + AnimOffsetY, (int)ZoomWidth, CalcH);
            public Func<bool>? ShowWhen;
            public Action?     OnClick;
            public DateTime    PressedUntilUtc = DateTime.MinValue;
            public int         PressVisualMs   = 120;
            public bool        IsRound         = false;
            public bool        NoResize        = false;
            public bool IsVisuallyPressed(DateTime now) => now < PressedUntilUtc;
        }
        private List<OverlayImage> overlayImages = new();
        private List<OverlayImage> g3000OverlayImages = new();
        private double _elecSourceSwitch = 0;
        private double _elecGeneratorSwitch = 0;
        private double _elecCrashbar = 1;
        private double _elecStarter = 2;
        private double _elecIgnition = 2;
        private double _elecFuelPump = 0;
        private double _apTrim = 0;
        private int _apDisengaged = 0;
        private int _kap140State = 0;
        private int _apVsHold = 0;
        private int _apFlcActive = 0;
        private int _apWingLeveler = 0;
        private int _apHdgLock = 0;
        private int _apAprHold = 0;
        private int _apGsHold = 0;
        private int _apNav1Lock = 0;
        private int _apFdActive = 0;
        private int _apAltLock = 0;
        private int _apVnavBtn = 0;
        private int _apYawDamper = 0;
        private double _apMaxBank = 1.0;
        private int _apBcHold = 0;
        private int _apMaster = 0;
        private int _xfrPushL = 0;
        private double _headingIndicator = 0;
        private int _masterCautionActive = 0;
        private int _masterWarningActive = 0;
        private int _rudderTrimDisabled = 0;
        private int _aileronTrimDisabled = 0;
        private double _lightingLogo1 = 0;
        private double _lightingWing1 = 0;
        private int _lightLogo = 0;
        private int _circuitConn17 = 0;
        private double _fuelSelectionSet = 2;

        private bool calibrationMode = false;
        private bool  _tactilebtnDragging = false;
        private Point _tactilebtnDragOffset;
        private bool  _closeBtnDragging = false;
        private Point _closeBtnDragOffset;
        private bool  _showAreasBtnDragging = false;
        private Point _showAreasBtnDragOffset;
        private System.Windows.Forms.Button _gearBtn = null!;
        private System.Windows.Forms.Button _overlayGearBtn = null!;
        private bool  _gearBtnDragging = false;
        private Point _gearBtnDragOffset;
        private bool  _panelVisible = false;

        // Volum
        private System.Windows.Forms.Button _volUpBtn   = null!;
        private System.Windows.Forms.Button _volDownBtn = null!;
        private Point _volGrpAnchor = new Point(1300, 80);
        private bool  _volGrpDragging;
        private Point _volGrpDragStartScreen;

        // Fullscreen
        private System.Windows.Forms.Button _fullscreenBtn = null!;
        private bool  _fullscreenBtnDragging;
        private Point _fullscreenBtnDragOffset;

        // MSFS End key
        private System.Windows.Forms.Button _msfsEndBtn = null!;
        private bool  _msfsEndBtnDragging;
        private Point _msfsEndBtnDragOffset;

        // MSFS Focus button
        private System.Windows.Forms.Button _focusBtn = null!;
        private bool  _focusBtnDragging;
        private Point _focusBtnDragOffset;

        // Engine Start/Stop buttons
        private System.Windows.Forms.Button _engineStartBtn = null!;
        private bool  _engineStartBtnDragging;
        private Point _engineStartBtnDragOffset;

        private System.Windows.Forms.Button _engineStopBtn = null!;
        private bool  _engineStopBtnDragging;
        private Point _engineStopBtnDragOffset;

        private System.Windows.Forms.Button _g3000Btn = null!;
        private bool  _g3000BtnDragging;
        private Point _g3000BtnDragOffset;

        private bool  _tb21Dragging;
        private Point _tb21DragOffset;
        private DateTime _lastRepaint = DateTime.MinValue;

        private Label? _tipLbl;
        private Form? _overlayPanel;
        private Panel? _fsConnPanel;
        private bool _taxiPropActive = false;
        private bool _thrSliderFollow = false;
        private bool _thrQuadrantVisible = false;
        private System.Windows.Forms.Button _thrQuadrantBtn = null!;
        private bool _thrHandleDragging = false;
        private int _thrHandleDragOffsetY;
        private int _thrHandleDragOffsetX;
        private int _thrHandleDefaultY = -1;
        private bool _thrHandleLeftRail = false;
        private int _thrHandleRightRailX = 3734;
        private int _thrHandleLeftRailCenterY;
        private DateTime _thrIgnoreFollowUntil = DateTime.MinValue;
        private DateTime _thrIgnoreRightRailUntil = DateTime.MinValue; // anti-rebot
        private double _lastThrLeverPos = double.NaN;
        private bool _tbm930ModCheckDone = false;


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
        private float calibResizeStartHeight;
        private int   calibResizeStartMouseX;
        private int   calibResizeStartMouseY;
        private readonly Rectangle calibExportBtnRect = new Rectangle(10, 10, 180, 50);
        private TextBox calibZoomBox = new TextBox();

        private int flapsAnimOffsetY = 0;
        private int flapsAnimDir     = 0;
        // G3K flaps
        private int   _g3kFlapsAnimDir          = 0;
        private int   _g3kFlapsPoscLast         = -1;
        private float _g3kFlapsCircleY_Current  = 0f;
        private float _g3kFlapsCircleY_Target   = 0f;
        private float _g3kFlapsCircleY_Start    = 0f;
        private DateTime _g3kFlapsAnimStartTime;
        private float _g3kFlapsAnimDurationSec  = 0f;
        private int   _g3kFlapsIndX             = 400;
        private int   _g3kFlapsIndYOffset       = 0;
        private bool  _calibDragG3kFlapsInd     = false;
        private Point _calibG3kFlapsIndDragStart;
        private int   _calibG3kFlapsIndStartX;
        private int   _calibG3kFlapsIndStartYOff;
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
            public double cameraState;
            public double elecSourceSwitch; // L:XMLVAR_Elec_Source_Switch_State (0=OFF, 1=BATT, 2=GPU)
            public double elecGeneratorSwitch; // L:XMLVAR_Elec_Generator_Switch_State
            public double elecCrashbarState;   // L:XMLVAR_Elec_CrashBar_State (0=visible, 1=hidden)
            public double elecStarter;         // L:XMLVAR_Starter (0=up, 1=center, 2=down)
            public double elecIgnition;        // L:XMLVAR_Ignition (0=up, 1=center, 2=down)
            public double elecFuelPump;        // L:XMLVAR_BoostFuelPump (0=up/auto, 1=center/on, 2=down/off)
            public double apTrim;              // L:XMLVAR_APTrim (0=ON/up, 1=AP OFF/center, 2=OFF/dn)
            public int apDisengaged;           // AUTOPILOT DISENGAGED (Bool)
            public int rudderTrimDisabled;     // RUDDER TRIM DISABLED (Bool)
            public int aileronTrimDisabled;    // AILERON TRIM DISABLED (Bool)
            public double lightingLogo1;       // L:LIGHTING_LOGO_1 (0=OFF, 1=ON)
            public int lightLogo;              // LIGHT LOGO (Bool)
            public int circuitConn17;          // CIRCUIT CONNECTION ON:17 (Bool)
            public double fuelSelectionSet;    // L:TBM930_FUEL_SELECTION_SET (1=MANUAL, 2=AUTO)
            public double propLeverPos;        // GENERAL ENG PROPELLER LEVER POSITION:1 (0-100%)
            public double propRpm;             // PROP RPM:1
            public double thrLeverPos;         // GENERAL ENG THROTTLE LEVER POSITION:1
            public int fuelValve;              // GENERAL ENG FUEL VALVE:1 (0=CUTOFF, 1=open)
            public int kap140State;            // L:KAP140_State
            public int apVsHold;               // AUTOPILOT VERTICAL HOLD (Bool)
            public int apFlcActive;            // AUTOPILOT FLIGHT LEVEL CHANGE (Bool)
            public int apWingLeveler;          // AUTOPILOT WING LEVELER (Bool)
            public int apHdgLock;              // AUTOPILOT HEADING LOCK (Bool)
            public int apAprHold;              // AUTOPILOT APPROACH HOLD (Bool)
            public int apGsHold;               // AUTOPILOT GLIDESLOPE HOLD (Bool)
            public int apNav1Lock;             // AUTOPILOT NAV1 LOCK (Bool)
            public int apFdActive;             // AUTOPILOT FLIGHT DIRECTOR ACTIVE (Bool)
            public int apAltLock;              // AUTOPILOT ALTITUDE LOCK (Bool)
            public int apVnavBtn;              // L:XMLVAR_VNAVButtonValue
            public int apYawDamper;            // AUTOPILOT YAW DAMPER (Bool)
            public double apMaxBank;           // AUTOPILOT MAX BANK (Radians)
            public int apBcHold;               // AUTOPILOT BACKCOURSE HOLD (Bool)
            public int apMaster;               // AUTOPILOT MASTER (Bool)
            public int xfrPushL;               // L:XMLVAR_PushXFR
            public double headingIndicator;    // HEADING INDICATOR (degrees)
            public int masterCautionActive;    // L:Generic_Master_Caution_Active
            public int masterWarningActive;    // L:Generic_Master_Warning_Active
            public double aileronTrimFrac;     // AILERON TRIM PCT (-1..+1)
            public double lightingWing1;       // L:LIGHTING_WING_1 (0=OFF, 1=ON)
            public int read_propdeice;         // PROP DEICE SWITCH:1 (Bool)
            public int read_windshielddeice;   // WINDSHIELD DEICE SWITCH (Bool)
            public int read_pitot1;            // L:DEICE_Pitot_1 (Bool)
            public int read_pitot2;            // L:DEICE_Pitot_2 (Bool)
            public int read_antiice1;          // ENG ANTI ICE:1 (Bool)
            public double read_isDeiceAirframe; // L:XMLVAR_IsDeiceAirFrame (Number)
            public double read_isDeiceAirframe2; // L:XMLVAR_IsDeiceAirFrame2 (Number)



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
        struct CameraStateWrite { public double value; }
        public frmMain()
        {
            InitializeComponent();

            imglightOff = panelAIR1000.Properties.Resources.light_off;
            imglightOn = panelAIR1000.Properties.Resources.light_on;
            trimWheelImg = LoadOverlayImage("trim wheel.png");

            var asmFrames = System.Reflection.Assembly.GetExecutingAssembly();
            for (int i = 1; i <= RUDDER_TRIM_FRAME_COUNT; i++)
            {
                using var stream = asmFrames.GetManifestResourceStream($"panelAIR1000.trimframe{i}.png");
                if (stream != null)
                    rudderTrimFrames[i - 1] = Image.FromStream(stream);
            }
            for (int i = 1; i <= RUDDER_TRIM_FRAME_COUNT; i++)
            {
                using var stream = asmFrames.GetManifestResourceStream($"panelAIR1000.trimframe{i}.png");
                if (stream != null)
                {
                    using var src = Image.FromStream(stream);
                    var rotated = new Bitmap(src.Height, src.Width);
                    using var g = Graphics.FromImage(rotated);
                    g.TranslateTransform(rotated.Width / 2f, rotated.Height / 2f);
                    g.RotateTransform(90);
                    g.DrawImage(src, -src.Width / 2f, -src.Height / 2f);
                    trimFrames[i - 1] = rotated;
                }
            }

            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using (var s = asm.GetManifestResourceStream("panelAIR1000.Resources.main GA_1000 4K panel_f.png"))
                if (s != null) { using var src = Image.FromStream(s); _bgImage = new Bitmap(src); }
            using (var s = asm.GetManifestResourceStream("panelAIR1000.Resources.main GA_3000 4K panel_m.png"))
                if (s != null) { using var src = Image.FromStream(s); _bgG3000Image = new Bitmap(src); }

            pbMain.Paint += PbMain_Paint;
            pbMain.MouseDown += PbMain_MouseDown;
            pbMain.MouseMove += PbMain_MouseMove;
            pbMain.MouseUp += PbMain_MouseUp;

            mainSurface = new UiSurface(pbMain);
            mainSurface.HideAreasWhenActive = () => _g3000Active;
            uiSurfaces.Add(mainSurface);
            mainSurface.SuppressInput = () => calibrationMode || _g3000Active;

            this.KeyPreview = true;
            Action<KeyEventArgs> handleF12 = (e) => { if (e.KeyCode == Keys.F12) { calibrationMode = !calibrationMode; calibSelectedBtn = null; calibSelectedTri = null; calibSelectedOverlay = null; calibZoomBox.Visible = false; pbMain.Invalidate(); } };
            this.KeyDown += (s, e) => handleF12(e);
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
                        foreach (var ov in overlayImages.Concat(g3000OverlayImages).Where(o => o.TypeKey == tk))
                        {
                            ov.ZoomWidth = w;
                            if (ov.IsRound) ov.ZoomHeight = w;
                        }
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
            this.TopMost = true;

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { System.IO.File.WriteAllText("panelAIR1000_crash.txt", e.ExceptionObject.ToString()); } catch { }
            };
            Application.ThreadException += (s, e) =>
            {
                try { System.IO.File.AppendAllText("panelAIR1000_crash.txt", "\r\n[UI] " + e.Exception.ToString()); } catch { }
            };

            this.Icon = Resources.logo;

            _overlayPanel = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(40, 40, 40),
                ShowInTaskbar = false,
                Size = new Size(3440, 270),
                Location = new Point(this.Left + 200, this.Top + 1160)
            };
            _overlayPanel.KeyPreview = true;
            _overlayPanel.KeyDown += (s, e) => handleF12(e);
            _overlayPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.Orange, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _overlayPanel.Width - 1, _overlayPanel.Height - 1);
            };

            _tactileBtn = new System.Windows.Forms.Button
            {
                Text = "🖐  Make this screen touch",
                Location = new Point(3477, 1230),
                Size = new Size(363, 63),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 26f, FontStyle.Bold, GraphicsUnit.Pixel),
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
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _tactileBtn.Location = new Point(formPt.X - _tactilebtnDragOffset.X, formPt.Y - _tactilebtnDragOffset.Y);
            };
            _tactileBtn.MouseUp += (s, e) => { _tactilebtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _tactileBtn.Location = new Point(900, 100);
            _overlayPanel.Controls.Add(_tactileBtn);

            _fsConnPanel = new Panel
            {
                Location = new Point(1870, 30),
                Size = new Size(363, 126),
                BackColor = Color.FromArgb(40, 40, 40)
            };
            _fsConnPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.White, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _fsConnPanel.Width - 1, _fsConnPanel.Height - 1);
            };
            var _fsConnTitle = new Label
            {
                Text = "FS CONNECTION",
                Location = new Point(5, 5),
                Size = new Size(353, 55),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Emoji", 26f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40)
            };
            textBox21.Font = new Font("Segoe UI Emoji", 26f, FontStyle.Bold, GraphicsUnit.Pixel);
            textBox21.Size = new Size(353, 55);
            textBox21.Location = new Point(5, 63);
            _fsConnPanel.Controls.Add(_fsConnTitle);
            _fsConnPanel.Controls.Add(textBox21);

            _fsConnPanel.MouseDown += (s, e) => { if (!calibrationMode) return; _tb21Dragging = true; _tb21DragOffset = e.Location; };
            _fsConnPanel.MouseMove += (s, e) =>
            {
                if (!_tb21Dragging) return;
                var screenPt = _fsConnPanel.PointToScreen(e.Location);
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _fsConnPanel.Location = new Point(formPt.X - _tb21DragOffset.X, formPt.Y - _tb21DragOffset.Y);
            };
            _fsConnPanel.MouseUp += (s, e) => { _tb21Dragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _fsConnTitle.MouseDown += (s, e) => { if (!calibrationMode) return; _tb21Dragging = true; _tb21DragOffset = e.Location; };
            _fsConnTitle.MouseMove += (s, e) =>
            {
                if (!_tb21Dragging) return;
                var screenPt = _fsConnTitle.PointToScreen(e.Location);
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _fsConnPanel.Location = new Point(formPt.X - _tb21DragOffset.X, formPt.Y - _tb21DragOffset.Y);
            };
            _fsConnTitle.MouseUp += (s, e) => { _tb21Dragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _overlayPanel.Controls.Add(_fsConnPanel);

            _g3000Btn = MakeGroupBtn("TBM 930 cockpit", 363, 63);
            _g3000Btn.Location = new Point(3100, 100);
            _g3000Btn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                _g3000Active = !_g3000Active;
                _g3000Btn.Text = _g3000Active ? "G1000 cockpit" : "TBM 930 cockpit";
                _panelVisible = false;
                _gearBtn.BackColor = Color.FromArgb(40, 40, 40);
                _gearBtn.Text = "⚙";
                _overlayPanel!.Hide();
                _thrQuadrantBtn.Visible = _g3000Active;
                if (!_g3000Active) { _thrQuadrantVisible = false; }
                if (_g3000Active)
                {
                    _thrQuadrantVisible = true;
                    ApplyG3000Region();
                    SetAPTrimPos(0);
                    _thrHandleLeftRail = false;
                    _lastThrLeverPos = double.NaN;
                    _thrIgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    var thrH = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                    if (thrH != null)
                    {
                        thrH.BasePos = new Point(3734, _thrHandleDefaultY >= 0 ? _thrHandleDefaultY : thrH.BasePos.Y);
                        int cx = thrH.BasePos.X + thrH.Bounds.Width / 2;
                        int cy = thrH.BasePos.Y + thrH.Bounds.Height / 2;
                    }
                    if (simconnect != null)
                    {
                        simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_FUELSEL_AUTO, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); _fuelSelectionSet = 2;
                        //simconnect.SetDataOnSimObject(DEFINITIONS.CondLeverCutoffWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new CondLeverCutoffStruct { value = 1 }); // DISABLED — causa HIGH IDLE si MSFS a CUTOFF
                        //simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); // DISABLED — idem
                        //simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)(-16383)), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        //simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        Task.Run(async () => { await Task.Delay(500); this.BeginInvoke(new Action(() => {
                            //simconnect?.SetDataOnSimObject(DEFINITIONS.CondLeverCutoffWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new CondLeverCutoffStruct { value = 1 }); // DISABLED — idem
                            //simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); // DISABLED — idem
                            //simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)(-16383)), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            //simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            SyncThrHandleFromMsfs();
                        })); });
                    }
                    CheckTBM930Mod();
                }
                else
                {
                    ApplyPfdRegion();
                    if (_tipLbl != null) { this.Controls.Remove(_tipLbl); _tipLbl.Dispose(); _tipLbl = null; }
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        this.BeginInvoke(new Action(() =>
                        {
                            _tipLbl = new Label
                            {
                                Text = "If you no longer need G3000 mode, close the MSFS G3000 pop‑out windows to restore full panel functionality.  [10]",
                                AutoSize = false,
                                Size = new Size(1440, 120),
                                Location = new Point((3840 - 1440) / 2, (2160 - 120) / 2 + 50),
                                TextAlign = ContentAlignment.MiddleCenter,
                                Font = new Font("Segoe UI", 26f),
                                ForeColor = Color.White,
                                BackColor = Color.DarkRed
                            };
                            this.Controls.Add(_tipLbl);
                            _tipLbl.BringToFront();
                        }));
                        for (int i = 9; i >= 0; i--)
                        {
                            await Task.Delay(1000);
                            this.BeginInvoke(new Action(() =>
                            {
                                if (_tipLbl == null) return;
                                if (i <= 0)
                                {
                                    this.Controls.Remove(_tipLbl);
                                    _tipLbl.Dispose();
                                    _tipLbl = null;
                                }
                                else
                                {
                                    _tipLbl.Text = "If you no longer need G3000 mode, close the MSFS G3000 pop‑out windows to restore full G1000 panel functionality.  [" + i + "]";
                                }
                            }));
                        }
                    });
                }
                pbMain.Invalidate();
            };
            _g3000Btn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _g3000BtnDragging = true;
                _g3000BtnDragOffset = e.Location;
            };
            _g3000Btn.MouseMove += (s, e) =>
            {
                if (!_g3000BtnDragging) return;
                var screenPt = _g3000Btn.PointToScreen(e.Location);
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _g3000Btn.Location = new Point(formPt.X - _g3000BtnDragOffset.X, formPt.Y - _g3000BtnDragOffset.Y);
            };
            _g3000Btn.MouseUp += (s, e) => { _g3000BtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _overlayPanel.Controls.Add(_g3000Btn);

            _thrQuadrantBtn = new System.Windows.Forms.Button
            {
                Text = "THR",
                Location = new Point(3750, 1000),
                Size = new Size(79, 79),
                BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 22f, FontStyle.Bold, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand,
                Visible = false
            };
            _thrQuadrantBtn.FlatAppearance.BorderSize = 0;
            _thrQuadrantBtn.FlatAppearance.BorderColor = Color.Black;
            _thrQuadrantBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 40, 40);
            _thrQuadrantBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 40);
            _thrQuadrantBtn.Region = new Region(CreateRoundedRect(new Rectangle(0, 0, 79, 79), 15));
            _thrQuadrantBtn.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.White, 1);
                using var path = CreateRoundedRect(new Rectangle(0, 0, 78, 78), 15);
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawPath(pen, path);
            };
            bool _thrQDragging = false;
            Point _thrQDragOffset = Point.Empty;
            _thrQuadrantBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                _thrQuadrantVisible = !_thrQuadrantVisible;
                pbMain.Invalidate();
            };
            _thrQuadrantBtn.MouseDown += (s, e) => { if (!calibrationMode) return; _thrQDragging = true; _thrQDragOffset = e.Location; };
            _thrQuadrantBtn.MouseMove += (s, e) => { if (!_thrQDragging) return; var sp = _thrQuadrantBtn.PointToScreen(e.Location); var fp = this.PointToClient(sp); _thrQuadrantBtn.Location = new Point(fp.X - _thrQDragOffset.X, fp.Y - _thrQDragOffset.Y); };
            _thrQuadrantBtn.MouseUp += (s, e) => { _thrQDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            this.Controls.Add(_thrQuadrantBtn); _thrQuadrantBtn.BringToFront();

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
                var formPt = _overlayPanel!.PointToClient(screenPt);
                closeButton.Location = new Point(formPt.X - _closeBtnDragOffset.X, formPt.Y - _closeBtnDragOffset.Y);
            };
            closeButton.MouseUp += (s, e) => { _closeBtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            closeButton.Location = new Point(100, 100);
            closeButton.Anchor = AnchorStyles.None;
            _overlayPanel.Controls.Add(closeButton);

            _showAreasBtn = new System.Windows.Forms.Button
            {
                Text = "👆  show the clickable areas",
                Location = new Point(3477, 1170),
                Size = new Size(363, 63),
                BackColor = Color.DimGray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 26f, FontStyle.Bold, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            _showAreasBtn.FlatAppearance.BorderSize = 1;
            _showAreasBtn.FlatAppearance.BorderColor = Color.White;
            _showAreasBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                mainSurface.ShowAllAreas = !mainSurface.ShowAllAreas;
                _showAreasBtn.BackColor = mainSurface.ShowAllAreas ? Color.Orange : Color.DimGray;
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
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _showAreasBtn.Location = new Point(formPt.X - _showAreasBtnDragOffset.X, formPt.Y - _showAreasBtnDragOffset.Y);
            };
            _showAreasBtn.MouseUp += (s, e) => { _showAreasBtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _showAreasBtn.Location = new Point(500, 100);
            _overlayPanel.Controls.Add(_showAreasBtn);

            _gearBtn = new System.Windows.Forms.Button
            {
                Text = "⚙",
                Location = new Point(3477, 1040),
                Size = new Size(63, 63),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 34f, FontStyle.Bold, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            _gearBtn.FlatAppearance.BorderSize = 1;
            _gearBtn.FlatAppearance.BorderColor = Color.White;
            _overlayGearBtn = new System.Windows.Forms.Button
            {
                Text = "",
                Location = new Point(1896, 22),
                Size = new Size(63, 63),
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 34f, FontStyle.Bold, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            _overlayGearBtn.FlatAppearance.BorderSize = 1;
            _overlayGearBtn.FlatAppearance.BorderColor = Color.White;
            _overlayGearBtn.Paint += (s, e) =>
            {
                using var xPen = new Pen(Color.White, 3);
                int m = 14;
                e.Graphics.DrawLine(xPen, m, m, _overlayGearBtn.Width - m, _overlayGearBtn.Height - m);
                e.Graphics.DrawLine(xPen, _overlayGearBtn.Width - m, m, m, _overlayGearBtn.Height - m);
            };
            bool _overlayGearDragging = false;
            Point _overlayGearDragOffset = Point.Empty;
            _overlayGearBtn.MouseDown += (s, e) => { if (!calibrationMode) return; _overlayGearDragging = true; _overlayGearDragOffset = e.Location; };
            _overlayGearBtn.MouseMove += (s, e) =>
            {
                if (!_overlayGearDragging) return;
                var sp = _overlayGearBtn.PointToScreen(e.Location);
                var fp = _overlayPanel.PointToClient(sp);
                _overlayGearBtn.Location = new Point(fp.X - _overlayGearDragOffset.X, fp.Y - _overlayGearDragOffset.Y);
            };
            _overlayGearBtn.MouseUp += (s, e) => { _overlayGearDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _overlayPanel.Controls.Add(_overlayGearBtn);

            Action toggleOverlay = () =>
            {
                if (calibrationMode) return;
                _panelVisible = !_panelVisible;
                _gearBtn.BackColor = _panelVisible ? Color.Red : Color.FromArgb(40, 40, 40);
                _gearBtn.Text = _panelVisible ? "" : "⚙";
                _gearBtn.Invalidate();
                if (_panelVisible)
                    _overlayPanel!.Show(this);
                else
                    _overlayPanel!.Hide();
            };
            _gearBtn.Paint += (s, e) =>
            {
                if (!_panelVisible) return;
                using var xPen = new Pen(Color.White, 3);
                int m = 14;
                e.Graphics.DrawLine(xPen, m, m, _gearBtn.Width - m, _gearBtn.Height - m);
                e.Graphics.DrawLine(xPen, _gearBtn.Width - m, m, m, _gearBtn.Height - m);
            };
            _gearBtn.Click += (s, e) => toggleOverlay();
            _overlayGearBtn.Click += (s, e) => toggleOverlay();
            _gearBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _gearBtnDragging = true;
                _gearBtnDragOffset = e.Location;
            };
            _gearBtn.MouseMove += (s, e) =>
            {
                if (!_gearBtnDragging) return;
                var screenPt = _gearBtn.PointToScreen(e.Location);
                var formPt = this.PointToClient(screenPt);
                _gearBtn.Location = new Point(formPt.X - _gearBtnDragOffset.X, formPt.Y - _gearBtnDragOffset.Y);
            };
            _gearBtn.MouseUp += (s, e) => { _gearBtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            this.Controls.Add(_gearBtn);
            _gearBtn.BringToFront();

            // ── Helpers de posicionament de grups ──────────────────────────
            void UpdateVolGroupPositions()
            {
                _volUpBtn.Location   = new Point(_volGrpAnchor.X, _volGrpAnchor.Y);
                _volDownBtn.Location = new Point(_volGrpAnchor.X, _volGrpAnchor.Y + 70);
            }
            // ── Helpers MSFS focus + tecles ────────────────────────────────
            System.Diagnostics.Process? GetMSFSProcess() =>
                System.Diagnostics.Process.GetProcessesByName("FlightSimulator").FirstOrDefault();

            // ── Drag handlers — grup volum ─────────────────────────────────
            void OnVolGrpMouseDown(object? s, MouseEventArgs e)
            {
                if (!calibrationMode) return;
                _volGrpDragging = true;
                _volGrpDragStartScreen = ((Control)s!).PointToScreen(e.Location);
            }
            void OnVolGrpMouseMove(object? s, MouseEventArgs e)
            {
                if (!_volGrpDragging) return;
                var cur = ((Control)s!).PointToScreen(e.Location);
                _volGrpAnchor = new Point(_volGrpAnchor.X + cur.X - _volGrpDragStartScreen.X,
                                          _volGrpAnchor.Y + cur.Y - _volGrpDragStartScreen.Y);
                _volGrpDragStartScreen = cur;
                UpdateVolGroupPositions();
            }
            void OnVolGrpMouseUp(object? s, MouseEventArgs e)
            {
                _volGrpDragging = false;
                try { ExportCalibrationPositions(silent: true); } catch { }
            }
            // ── Factory de botó ────────────────────────────────────────────
            System.Windows.Forms.Button MakeGroupBtn(string text, int w = 90, int h = 63)
            {
                var b = new System.Windows.Forms.Button
                {
                    Text = text, Size = new Size(w, h),
                    BackColor = Color.FromArgb(40, 40, 40), ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Emoji", 26f, FontStyle.Bold, GraphicsUnit.Pixel),
                    Cursor = Cursors.Hand
                };
                b.FlatAppearance.BorderSize = 1;
                b.FlatAppearance.BorderColor = Color.White;
                return b;
            }

            // ── Grup VOLUM ─────────────────────────────────────────────────
            _volUpBtn   = MakeGroupBtn("🔊+");
            _volDownBtn = MakeGroupBtn("🔊-");
            _volUpBtn.Click   += (s, e) => { if (!calibrationMode) keybd_event(VK_VOLUME_UP,   0, 0, UIntPtr.Zero); };
            _volDownBtn.Click += (s, e) => { if (!calibrationMode) keybd_event(VK_VOLUME_DOWN, 0, 0, UIntPtr.Zero); };
            foreach (var b in new[] { _volUpBtn, _volDownBtn })
            {
                b.MouseDown += OnVolGrpMouseDown;
                b.MouseMove += OnVolGrpMouseMove;
                b.MouseUp   += OnVolGrpMouseUp;
                _overlayPanel.Controls.Add(b);
            }
            UpdateVolGroupPositions();

            // ── Botó FULLSCREEN ────────────────────────────────────────────
            _fullscreenBtn = MakeGroupBtn("[Full Screen]", 220, 63);
            _fullscreenBtn.Location = new Point(1920, 1300);
            _fullscreenBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                var msfs = GetMSFSProcess();
                if (msfs == null) return;
                SetForegroundWindow(msfs.MainWindowHandle);
                System.Threading.Thread.Sleep(80);
                keybd_event(VK_MENU,   0, 0,                UIntPtr.Zero);
                keybd_event(VK_RETURN, 0, 0,                UIntPtr.Zero);
                keybd_event(VK_RETURN, 0, KEYEVENTF_KEYUP,  UIntPtr.Zero);
                keybd_event(VK_MENU,   0, KEYEVENTF_KEYUP,  UIntPtr.Zero);
                var t = new System.Windows.Forms.Timer { Interval = 500 };
                t.Tick += (ts, te) => { ((System.Windows.Forms.Timer)ts!).Stop(); SetForegroundWindow(this.Handle); };
                t.Start();
            };
            _fullscreenBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _fullscreenBtnDragging = true;
                _fullscreenBtnDragOffset = e.Location;
            };
            _fullscreenBtn.MouseMove += (s, e) =>
            {
                if (!_fullscreenBtnDragging) return;
                var sp = _fullscreenBtn.PointToScreen(e.Location);
                var fp = _overlayPanel!.PointToClient(sp);
                _fullscreenBtn.Location = new Point(fp.X - _fullscreenBtnDragOffset.X, fp.Y - _fullscreenBtnDragOffset.Y);
            };
            _fullscreenBtn.MouseUp += (s, e) =>
            {
                _fullscreenBtnDragging = false;
                try { ExportCalibrationPositions(silent: true); } catch { }
            };
            _fullscreenBtn.Location = new Point(1600, 100);
            _overlayPanel.Controls.Add(_fullscreenBtn);

            // ── Botó MSFS END KEY ──────────────────────────────────────────
            _msfsEndBtn = MakeGroupBtn("CAM IN/EXT", 220, 63);
            _msfsEndBtn.Font = new Font("Segoe UI Emoji", 26f, FontStyle.Bold, GraphicsUnit.Pixel);
            _msfsEndBtn.Location = new Point(1920, 1380);
            _msfsEndBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                if (simconnect == null) return;
                int currentCam = (int)lastData.cameraState;
                System.Diagnostics.Debug.WriteLine($"CAMERA STATE actual: {lastData.cameraState}");
                int newCam = (currentCam == 2) ? 3 : 2;
                var cs = new CameraStateWrite { value = (double)newCam };
                simconnect.SetDataOnSimObject(DEFINITIONS.CameraState, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, cs);
            };
            _msfsEndBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _msfsEndBtnDragging = true;
                _msfsEndBtnDragOffset = e.Location;
            };
            _msfsEndBtn.MouseMove += (s, e) =>
            {
                if (!_msfsEndBtnDragging) return;
                var sp = _msfsEndBtn.PointToScreen(e.Location);
                var fp = _overlayPanel!.PointToClient(sp);
                _msfsEndBtn.Location = new Point(fp.X - _msfsEndBtnDragOffset.X, fp.Y - _msfsEndBtnDragOffset.Y);
            };
            _msfsEndBtn.MouseUp += (s, e) =>
            {
                _msfsEndBtnDragging = false;
                try { ExportCalibrationPositions(silent: true); } catch { }
            };
            _msfsEndBtn.Location = new Point(1900, 100);
            _overlayPanel.Controls.Add(_msfsEndBtn);

            // ── Botó FOCUS MSFS ──────────────────────────────────────────
            _focusBtn = MakeGroupBtn("MSFS FOCUS", 220, 63);
            _focusBtn.Location = new Point(1920 + 120, 1380);
            _focusBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                var msfs = GetMSFSProcess();
                if (msfs == null) return;
                SetForegroundWindow(msfs.MainWindowHandle);
            };
            _focusBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _focusBtnDragging = true;
                _focusBtnDragOffset = e.Location;
            };
            _focusBtn.MouseMove += (s, e) =>
            {
                if (!_focusBtnDragging) return;
                var sp = _focusBtn.PointToScreen(e.Location);
                var fp = _overlayPanel!.PointToClient(sp);
                _focusBtn.Location = new Point(fp.X - _focusBtnDragOffset.X, fp.Y - _focusBtnDragOffset.Y);
            };
            _focusBtn.MouseUp += (s, e) =>
            {
                _focusBtnDragging = false;
                try { ExportCalibrationPositions(silent: true); } catch { }
            };
            _focusBtn.Location = new Point(2200, 100);
            _overlayPanel.Controls.Add(_focusBtn);

            // ── Botó ENGINE START ──────────────────────────────────────────
            _engineStartBtn = MakeGroupBtn("ENG START", 220, 63);
            _engineStartBtn.Location = new Point(1920 + 120 + 120, 1380);
            _engineStartBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ENGINE_AUTO_START, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            };
            _engineStartBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _engineStartBtnDragging = true;
                _engineStartBtnDragOffset = e.Location;
            };
            _engineStartBtn.MouseMove += (s, e) =>
            {
                if (!_engineStartBtnDragging) return;
                var sp = _engineStartBtn.PointToScreen(e.Location);
                var fp = _overlayPanel!.PointToClient(sp);
                _engineStartBtn.Location = new Point(fp.X - _engineStartBtnDragOffset.X, fp.Y - _engineStartBtnDragOffset.Y);
            };
            _engineStartBtn.MouseUp += (s, e) =>
            {
                _engineStartBtnDragging = false;
                try { ExportCalibrationPositions(silent: true); } catch { }
            };
            _engineStartBtn.Location = new Point(2500, 100);
            _overlayPanel.Controls.Add(_engineStartBtn);

            // ── Botó ENGINE STOP ──────────────────────────────────────────
            _engineStopBtn = MakeGroupBtn("ENG STOP", 220, 63);
            _engineStopBtn.Location = new Point(1920 + 120 + 120 + 120, 1380);
            _engineStopBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ENGINE_AUTO_SHUTDOWN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            };
            _engineStopBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _engineStopBtnDragging = true;
                _engineStopBtnDragOffset = e.Location;
            };
            _engineStopBtn.MouseMove += (s, e) =>
            {
                if (!_engineStopBtnDragging) return;
                var sp = _engineStopBtn.PointToScreen(e.Location);
                var fp = _overlayPanel!.PointToClient(sp);
                _engineStopBtn.Location = new Point(fp.X - _engineStopBtnDragOffset.X, fp.Y - _engineStopBtnDragOffset.Y);
            };
            _engineStopBtn.MouseUp += (s, e) =>
            {
                _engineStopBtnDragging = false;
                try { ExportCalibrationPositions(silent: true); } catch { }
            };
            _engineStopBtn.Location = new Point(2800, 100);
            _overlayPanel.Controls.Add(_engineStopBtn);

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
            pfdAudio.Buttons.Add(new UiButton { Name = "DISPLAY_BKUP", BasePos = new Point(1920, 1082), W = 60, H = 60, IsRound = true, OnClick = () => { if (!displayBackupActive_bool) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_DISPLAY_BKUP_ON, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); else simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_DISPLAY_BKUP_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });

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
            var swCenterWhiteImg = LoadOverlayImage("SW CENTER white_b.png");
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
                        ShowWhen = () => mainBusVolts >= 5.0 && pbrakeActive },
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

            // G3000 overlay images
            var swUpWhiteImg = LoadOverlayImage("switch-up-white.png");
            var swCenterWhiteG3kImg = LoadOverlayImage("switch-center-white.png");
            var swDownWhiteImg = LoadOverlayImage("switch-down-white.png");
            var swUpBlackImg = LoadOverlayImage("switch-up-black.png");
            var swCenterBlackImg = LoadOverlayImage("switch-center-black.png");
            var swDownBlackImg = LoadOverlayImage("switch-down-black.png");
            g3000OverlayImages = new List<OverlayImage>
            {
                new() { Name="G3K_ELECSRC_OFF",  TypeKey="G3K elec src", BasePos=new Point(100, 1500), ZoomWidth=75, Img=swDownWhiteImg,
                        ShowWhen = () => _elecSourceSwitch < 0.5 },
                new() { Name="G3K_ELECSRC_BATT", TypeKey="G3K elec src", BasePos=new Point(100, 1500), ZoomWidth=75, Img=swCenterWhiteG3kImg,
                        ShowWhen = () => _elecSourceSwitch >= 0.5 && _elecSourceSwitch < 1.5 },
                new() { Name="G3K_ELECSRC_GPU",  TypeKey="G3K elec src", BasePos=new Point(100, 1500), ZoomWidth=75, Img=swUpWhiteImg,
                        ShowWhen = () => _elecSourceSwitch >= 1.5 },
                new() { Name="G3K_ELECSRC_UP",   TypeKey="G3K elec click", BasePos=new Point(100, 1470), ZoomWidth=40, ZoomHeight=40,
                        OnClick = () => { if (_elecCrashbar >= 0.5 && _elecSourceSwitch < 1.5) { var newVal = Math.Round(_elecSourceSwitch) + 1; if (newVal >= 0.5 && _elecSourceSwitch < 0.5) { simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_STARTER1_HELD, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.STARTER1_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } SetElecSourceSwitch(newVal); } } },
                new() { Name="G3K_ELECSRC_DN",   TypeKey="G3K elec click", BasePos=new Point(100, 1540), ZoomWidth=40, ZoomHeight=40,
                        OnClick = () => { if (_elecCrashbar >= 0.5 && _elecSourceSwitch > 0.5) SetElecSourceSwitch(Math.Round(_elecSourceSwitch) - 1); } },
                new() { Name="G3K_ELECGEN_OFF",  TypeKey="G3K elec gen", BasePos=new Point(1225, 147), ZoomWidth=80, Img=swDownWhiteImg,
                        ShowWhen = () => _elecGeneratorSwitch < 0.5 },
                new() { Name="G3K_ELECGEN_BATT", TypeKey="G3K elec gen", BasePos=new Point(1225, 147), ZoomWidth=80, Img=swCenterWhiteG3kImg,
                        ShowWhen = () => _elecGeneratorSwitch >= 0.5 && _elecGeneratorSwitch < 1.5 },
                new() { Name="G3K_ELECGEN_GPU",  TypeKey="G3K elec gen", BasePos=new Point(1225, 147), ZoomWidth=80, Img=swUpWhiteImg,
                        ShowWhen = () => _elecGeneratorSwitch >= 1.5 },
                new() { Name="G3K_ELECGEN_UP",   TypeKey="G3K gen click", BasePos=new Point(1216, 80), ZoomWidth=100, ZoomHeight=100,
                        OnClick = () => { if (_elecCrashbar >= 0.5 && _elecGeneratorSwitch < 1.5) SetElecGeneratorSwitch(Math.Round(_elecGeneratorSwitch) + 1); } },
                new() { Name="G3K_ELECGEN_DN",   TypeKey="G3K gen click", BasePos=new Point(1217, 200), ZoomWidth=100, ZoomHeight=100,
                        OnClick = () => { if (_elecCrashbar >= 0.5 && _elecGeneratorSwitch > 0.5) SetElecGeneratorSwitch(Math.Round(_elecGeneratorSwitch) - 1); } },
                new() { Name="G3K_CRASHBAR",     TypeKey="G3K crashbar", BasePos=new Point(1175, 300), ZoomWidth=200, Img=LoadOverlayImage("Crashbar.png"),
                        ShowWhen = () => _elecCrashbar < 0.5 },
                new() { Name="G3K_CRASHBAR_TOG", TypeKey="G3K crashbar click", BasePos=new Point(1175, 300), ZoomWidth=200, ZoomHeight=50,
                        OnClick = () => ToggleElecCrashbar() },
                new() { Name="G3K_CRASHBAR_TOG2", TypeKey="G3K crashbar click2", BasePos=new Point(1175, 360), ZoomWidth=200, ZoomHeight=50,
                        OnClick = () => ToggleElecCrashbar() },
                new() { Name="G3K_LIGHT_LAND",   TypeKey="G3K light sw", BasePos=new Point(100, 147), ZoomWidth=80, Img=swUpBlackImg,
                        ShowWhen = () => landActive },
                new() { Name="G3K_LIGHT_TAXI",   TypeKey="G3K light sw", BasePos=new Point(100, 147), ZoomWidth=80, Img=swCenterBlackImg,
                        ShowWhen = () => !landActive && taxiActive },
                new() { Name="G3K_LIGHT_OFF",    TypeKey="G3K light sw", BasePos=new Point(100, 147), ZoomWidth=80, Img=swDownBlackImg,
                        ShowWhen = () => !landActive && !taxiActive },
                new() { Name="G3K_LIGHT_UP",     TypeKey="G3K light click", BasePos=new Point(20, 80), ZoomWidth=97, ZoomHeight=97,
                        OnClick = () => {
                            if (!landActive && !taxiActive) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_LIGHT_TAXI, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            else if (taxiActive && !landActive) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_LIGHT_LDG, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        } },
                new() { Name="G3K_LIGHT_DN",     TypeKey="G3K light click", BasePos=new Point(30, 200), ZoomWidth=97, ZoomHeight=97,
                        OnClick = () => {
                            if (landActive) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_LIGHT_TAXI, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            else if (taxiActive) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_LIGHT_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        } },
                new() { Name="G3K_STARTER_DN",    TypeKey="G3K starter sw", BasePos=new Point(1900, 123), ZoomWidth=105, Img=swDownBlackImg,
                        ShowWhen = () => _elecStarter >= 1.5 },
                new() { Name="G3K_STARTER_CTR",   TypeKey="G3K starter sw", BasePos=new Point(1900, 122), ZoomWidth=105, Img=swCenterBlackImg,
                        ShowWhen = () => _elecStarter >= 0.5 && _elecStarter < 1.5 },
                new() { Name="G3K_STARTER_UP",    TypeKey="G3K starter sw", BasePos=new Point(1900, 97), ZoomWidth=105, Img=swUpBlackImg,
                        ShowWhen = () => _elecStarter < 0.5 },
                new() { Name="G3K_STARTER_CK_UP", TypeKey="G3K starter click", BasePos=new Point(1894, 38), ZoomWidth=119, ZoomHeight=112,
                        OnClick = () => { SetElecStarter(0); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_STARTER1_HELD, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); Task.Run(async () => { await Task.Delay(4000); this.BeginInvoke(new Action(() => { SetElecStarter(1); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_STARTER1_HELD, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); })); }); } },
                new() { Name="G3K_STARTER_CK_DN", TypeKey="G3K starter click", BasePos=new Point(1893, 193), ZoomWidth=119, ZoomHeight=112,
                        OnClick = () => { SetElecStarter(2); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_STARTER1_HELD, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.STARTER1_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); Task.Run(async () => { await Task.Delay(1500); SetElecStarter(1); }); } },
                new() { Name="G3K_IGNITION_DN",    TypeKey="G3K ignition sw", BasePos=new Point(2000, 123), ZoomWidth=105, Img=swDownBlackImg,
                        ShowWhen = () => _elecIgnition >= 1.5 },
                new() { Name="G3K_IGNITION_CTR",   TypeKey="G3K ignition sw", BasePos=new Point(2000, 122), ZoomWidth=105, Img=swCenterBlackImg,
                        ShowWhen = () => _elecIgnition >= 0.5 && _elecIgnition < 1.5 },
                new() { Name="G3K_IGNITION_UP",    TypeKey="G3K ignition sw", BasePos=new Point(2000, 97), ZoomWidth=105, Img=swUpBlackImg,
                        ShowWhen = () => _elecIgnition < 0.5 },
                new() { Name="G3K_IGNITION_CK_UP", TypeKey="G3K ignition click", BasePos=new Point(1994, 38), ZoomWidth=119, ZoomHeight=112,
                        OnClick = () => SetIgnitionPos((int)Math.Max(0, Math.Round(_elecIgnition) - 1)) },
                new() { Name="G3K_IGNITION_CK_DN", TypeKey="G3K ignition click", BasePos=new Point(1993, 193), ZoomWidth=119, ZoomHeight=112,
                        OnClick = () => SetIgnitionPos((int)Math.Min(2, Math.Round(_elecIgnition) + 1)) },
                new() { Name="G3K_FUELPUMP_DN",    TypeKey="G3K fuelpump sw", BasePos=new Point(2200, 123), ZoomWidth=105, Img=swDownBlackImg,
                        ShowWhen = () => _elecFuelPump >= 1.5 },
                new() { Name="G3K_FUELPUMP_CTR",   TypeKey="G3K fuelpump sw", BasePos=new Point(2200, 122), ZoomWidth=105, Img=swCenterBlackImg,
                        ShowWhen = () => _elecFuelPump >= 0.5 && _elecFuelPump < 1.5 },
                new() { Name="G3K_FUELPUMP_UP",    TypeKey="G3K fuelpump sw", BasePos=new Point(2200, 97), ZoomWidth=105, Img=swUpBlackImg,
                        ShowWhen = () => _elecFuelPump < 0.5 },
                new() { Name="G3K_FUELPUMP_CK_UP", TypeKey="G3K fuelpump click", BasePos=new Point(2194, 38), ZoomWidth=119, ZoomHeight=112,
                        OnClick = () => { if (_elecFuelPump > 0.5) SetFuelPump(Math.Round(_elecFuelPump) - 1); } },
                new() { Name="G3K_FUELPUMP_CK_DN", TypeKey="G3K fuelpump click", BasePos=new Point(2193, 193), ZoomWidth=119, ZoomHeight=112,
                        OnClick = () => { if (_elecFuelPump < 1.5) SetFuelPump(Math.Round(_elecFuelPump) + 1); } },
                new() { Name="G3K_APTRIM_DN",    TypeKey="G3K aptrim sw", BasePos=new Point(2025, 123), ZoomWidth=105, Img=swDownWhiteImg,
                        ShowWhen = () => _apTrim >= 1.5 },
                new() { Name="G3K_APTRIM_CTR",   TypeKey="G3K aptrim sw", BasePos=new Point(2025, 122), ZoomWidth=105, Img=swCenterWhiteG3kImg,
                        ShowWhen = () => _apTrim >= 0.5 && _apTrim < 1.5 },
                new() { Name="G3K_APTRIM_UP",    TypeKey="G3K aptrim sw", BasePos=new Point(2025, 97), ZoomWidth=105, Img=swUpWhiteImg,
                        ShowWhen = () => _apTrim < 0.5 },
                new() { Name="G3K_APTRIM_CK_UP", TypeKey="G3K aptrim click", BasePos=new Point(2019, 38), ZoomWidth=119, ZoomHeight=112,
                        OnClick = () => SetAPTrimPos((int)Math.Max(0, Math.Round(_apTrim) - 1)) },
                new() { Name="G3K_APTRIM_CK_DN", TypeKey="G3K aptrim click", BasePos=new Point(2018, 193), ZoomWidth=119, ZoomHeight=112,
                        OnClick = () => SetAPTrimPos((int)Math.Min(2, Math.Round(_apTrim) + 1)) },
                new() { Name="G3K_PULSELIGHT_ON", TypeKey="G3K pulselight", BasePos=new Point(200, 113), ZoomWidth=105, Img=LoadOverlayImage("rocker_black_sw_OFF.png"),
                        ShowWhen = () => _lightingLogo1 < 0.5 },
                new() { Name="G3K_PULSELIGHT_TOG", TypeKey="G3K pulselight click", BasePos=new Point(200, 113), ZoomWidth=105, ZoomHeight=105,
                        OnClick = () => TogglePulseLight() },
                new() { Name="G3K_NAV_ON", TypeKey="G3K nav", BasePos=new Point(250, 113), ZoomWidth=105, Img=LoadOverlayImage("rocker_black_sw_OFF.png"),
                        ShowWhen = () => !navActive },
                new() { Name="G3K_NAV_TOG", TypeKey="G3K nav click", BasePos=new Point(250, 113), ZoomWidth=105, ZoomHeight=105,
                        OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_NAV, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="G3K_STROBE_ON", TypeKey="G3K strobe", BasePos=new Point(414, 115), ZoomWidth=67, Img=LoadOverlayImage("rocker_black_sw_OFF.png"),
                        ShowWhen = () => !strobeActive },
                new() { Name="G3K_STROBE_TOG", TypeKey="G3K strobe click", BasePos=new Point(411, 101), ZoomWidth=71, ZoomHeight=113,
                        OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_STROBE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
                new() { Name="G3K_FUELSEL_ON", TypeKey="G3K fuelsel", BasePos=new Point(1905, 115), ZoomWidth=67, Img=LoadOverlayImage("rocker_black_sw_OFF.png"),
                        ShowWhen = () => _fuelSelectionSet < 1.5 },
                new() { Name="G3K_FUELSEL_TOG", TypeKey="G3K fuelsel click", BasePos=new Point(1902, 101), ZoomWidth=71, ZoomHeight=113,
                        OnClick = () => {
                            if (simconnect == null) return;
                            if (_fuelSelectionSet >= 1.5) { simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_FUELSEL_MAN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); _fuelSelectionSet = 1; }
                            else { simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_FUELSEL_AUTO, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); _fuelSelectionSet = 2; }
                            pbMain.Invalidate();
                        } },
                new() { Name="G3K_THR_BASE", TypeKey="G3K thr base", BasePos=new Point(0, 0), ZoomWidth=166, Img=LoadOverlayImage("THR_base.png"),
                        ShowWhen = () => _thrQuadrantVisible },
                new() { Name="G3K_THR_HANDLE", TypeKey="G3K thr handle", BasePos=new Point(0, 0), ZoomWidth=78, Img=LoadOverlayImage("THR_handle.png"),
                        ShowWhen = () => _thrQuadrantVisible },
                new() { Name="G3K_THR_CUTOFF", TypeKey="G3K thr cutoff click", BasePos=new Point(3700, 1700), ZoomWidth=100, ZoomHeight=80,
                        OnClick = () => { if (_thrHandleLeftRail) return;
                            _taxiPropActive = false; _thrSliderFollow = false;
                            _lastThrLeverPos = double.NaN; _thrIgnoreFollowUntil = DateTime.UtcNow.AddSeconds(1.5); _thrIgnoreRightRailUntil = DateTime.UtcNow.AddMilliseconds(800); // anti-rebot
                            simconnect?.SetDataOnSimObject(DEFINITIONS.CondLeverCutoffWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new CondLeverCutoffStruct { value = 0 });
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            MoveThrHandle(_thrHandleDefaultY + 150); } },
                new() { Name="G3K_THR_LOWIDLE", TypeKey="G3K thr lowidle click", BasePos=new Point(3700, 1600), ZoomWidth=100, ZoomHeight=80,
                        OnClick = () => {
                            if (_thrHandleLeftRail)
                            {
                                var thrH2 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                                var taxiA = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_TAXI");
                                if (thrH2 != null && taxiA != null)
                                {
                                    int hCY = thrH2.BasePos.Y + thrH2.Bounds.Height / 2;
                                    int tCY = taxiA.BasePos.Y + taxiA.Bounds.Height / 2;
                                    if (Math.Abs(hCY - tCY) > 20) return;
                                    _thrHandleLeftRail = false;
                                    thrH2.BasePos = new Point(3734, thrH2.BasePos.Y);
                                }
                            }
                            _taxiPropActive = false; _thrSliderFollow = false;
                            _lastThrLeverPos = double.NaN; _thrIgnoreFollowUntil = DateTime.UtcNow.AddSeconds(1.5); _thrIgnoreRightRailUntil = DateTime.UtcNow.AddMilliseconds(800); // anti-rebot
                            Action sendLI = () => { simconnect?.SetDataOnSimObject(DEFINITIONS.CondLeverCutoffWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new CondLeverCutoffStruct { value = 1 });
                                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)(-16383)), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); };
                            sendLI(); Task.Run(async () => { await Task.Delay(500); this.BeginInvoke(sendLI); });
                            var liArea = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_LOWIDLE");
                            if (liArea != null) { var thrH3 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE"); if (thrH3 != null) MoveThrHandle(liArea.BasePos.Y + liArea.Bounds.Height / 2 - thrH3.Bounds.Height / 2); }
                            else MoveThrHandle(_thrHandleDefaultY); } },
                new() { Name="G3K_THR_HIGHIDLE", TypeKey="G3K thr highidle click", BasePos=new Point(3700, 1500), ZoomWidth=100, ZoomHeight=80,
                        OnClick = () => {
                            // No permetre salt directe des de CUTOFF
                            if (!_thrHandleLeftRail)
                            {
                                var thrHck = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                                if (thrHck != null && thrHck.BasePos.Y >= _thrHandleDefaultY + 140) return;
                            }
                            if (_thrHandleLeftRail)
                            {
                                var thrH = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                                var taxiArea = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_TAXI");
                                if (thrH != null && taxiArea != null)
                                {
                                    int handleCY = thrH.BasePos.Y + thrH.Bounds.Height / 2;
                                    int taxiCY = taxiArea.BasePos.Y + taxiArea.Bounds.Height / 2;
                                    if (Math.Abs(handleCY - taxiCY) <= 20)
                                    {
                                        thrH.BasePos = new Point(_thrHandleRightRailX, thrH.BasePos.Y);
                                        _thrHandleLeftRail = false;
                                    }
                                    else return;
                                }
                            }
                            _taxiPropActive = false; _thrSliderFollow = false;
                            _lastThrLeverPos = double.NaN; _thrIgnoreFollowUntil = DateTime.UtcNow.AddSeconds(1.5); _thrIgnoreRightRailUntil = DateTime.UtcNow.AddMilliseconds(800); // anti-rebot
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)(-11000)), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            MoveThrHandle(_thrHandleDefaultY - 170); } },
                new() { Name="G3K_THR_TAXI", TypeKey="G3K thr taxi click", BasePos=new Point(3700, 1400), ZoomWidth=100, ZoomHeight=80,
                        OnClick = () => {
                            var thrH = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                            var taxiArea = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_TAXI");
                            var lowIdleA = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_LOWIDLE");
                            if (thrH == null || taxiArea == null) return;
                            int handleCY = thrH.BasePos.Y + thrH.Bounds.Height / 2;
                            int hiIdleY = _thrHandleDefaultY - 170;
                            bool nearHiIdle = handleCY <= hiIdleY + thrH.Bounds.Height / 2 + 50;
                            bool nearLowIdle = lowIdleA != null && Math.Abs(handleCY - (lowIdleA.BasePos.Y + lowIdleA.Bounds.Height / 2)) <= 20;
                            if (!_thrHandleLeftRail && (nearHiIdle || nearLowIdle))
                            {
                                _thrHandleLeftRail = true;
                                int taxiCenterY = taxiArea.BasePos.Y + taxiArea.Bounds.Height / 2 - thrH.Bounds.Height / 2;
                                _thrHandleLeftRailCenterY = taxiCenterY;
                            }
                            if (_thrHandleLeftRail)
                            {
                                int taxiCenterY2 = taxiArea.BasePos.Y + taxiArea.Bounds.Height / 2 - thrH.Bounds.Height / 2;
                                _taxiPropActive = true; _thrSliderFollow = true;
                                _thrIgnoreRightRailUntil = DateTime.UtcNow.AddMilliseconds(800); // anti-rebot
                                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 8250, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)(-16383)), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                MoveThrHandle(taxiCenterY2);
                            } } },

            // G3000 PFD1 softkeys
            new() { Name="G3K_softkey_1",  TypeKey="G3K softkey 1",  BasePos=new Point( 241, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_1,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_2",  TypeKey="G3K softkey 2",  BasePos=new Point( 370, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_2,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_3",  TypeKey="G3K softkey 3",  BasePos=new Point( 499, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_3,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_4",  TypeKey="G3K softkey 4",  BasePos=new Point( 628, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_4,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_5",  TypeKey="G3K softkey 5",  BasePos=new Point( 757, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_5,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_6",  TypeKey="G3K softkey 6",  BasePos=new Point( 886, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_6,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_7",  TypeKey="G3K softkey 7",  BasePos=new Point(1015, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_7,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_8",  TypeKey="G3K softkey 8",  BasePos=new Point(1144, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_8,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_9",  TypeKey="G3K softkey 9",  BasePos=new Point(1273, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_9,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_10", TypeKey="G3K softkey 10", BasePos=new Point(1402, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_10, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_11", TypeKey="G3K softkey 11", BasePos=new Point(1531, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_11, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_softkey_12",     TypeKey="G3K softkey 12",     BasePos=new Point(1660, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_SK_12,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_1",  TypeKey="G3K MFD softkey 1",  BasePos=new Point(2082, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_1,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_2",  TypeKey="G3K MFD softkey 2",  BasePos=new Point(2211, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_2,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_3",  TypeKey="G3K MFD softkey 3",  BasePos=new Point(2337, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_3,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_4",  TypeKey="G3K MFD softkey 4",  BasePos=new Point(2463, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_4,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_5",  TypeKey="G3K MFD softkey 5",  BasePos=new Point(2590, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_5,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_6",  TypeKey="G3K MFD softkey 6",  BasePos=new Point(2717, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_6,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_7",  TypeKey="G3K MFD softkey 7",  BasePos=new Point(2844, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_7,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_8",  TypeKey="G3K MFD softkey 8",  BasePos=new Point(2972, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_8,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_9",  TypeKey="G3K MFD softkey 9",  BasePos=new Point(3100, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_9,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_10", TypeKey="G3K MFD softkey 10", BasePos=new Point(3227, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_10, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_11", TypeKey="G3K MFD softkey 11", BasePos=new Point(3355, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_11, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MFD_softkey_12", TypeKey="G3K MFD softkey 12", BasePos=new Point(3482, 1504), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MFD_SK_12, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_TSC1_softkey_1", TypeKey="G3K TSC1 softkey 1", BasePos=new Point(0, 0), ZoomWidth=95, ZoomHeight=73, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_TSC1_SK_1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_TSC1_softkey_2", TypeKey="G3K TSC1 softkey 2", BasePos=new Point(2634, 1847), ZoomWidth=77, ZoomHeight=58, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_TSC1_SK_2, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_TSC1_softkey_3", TypeKey="G3K TSC1 softkey 3", BasePos=new Point(2634, 1905), ZoomWidth=77, ZoomHeight=58, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_TSC1_SK_3, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_TSC2_softkey_1", TypeKey="G3K TSC2 softkey 1", BasePos=new Point(3765, 1789), ZoomWidth=77, ZoomHeight=58, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_TSC2_SK_1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_TSC2_softkey_2", TypeKey="G3K TSC2 softkey 2", BasePos=new Point(3765, 1847), ZoomWidth=77, ZoomHeight=58, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_TSC2_SK_2, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_TSC2_softkey_3", TypeKey="G3K TSC2 softkey 3", BasePos=new Point(3765, 1905), ZoomWidth=77, ZoomHeight=58, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_TSC2_SK_3, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_MASTER",    TypeKey="G3K AP MASTER",    BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_MASTER,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_HDG_HOLD",  TypeKey="G3K AP HDG HOLD",  BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_HDG_HOLD,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_NAV1_HOLD", TypeKey="G3K AP NAV1 HOLD", BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_NAV1_HOLD, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_APR_HOLD",  TypeKey="G3K AP APR HOLD",  BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_APR_HOLD,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_XFR",          TypeKey="G3K XFR",          BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_XFR,          0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_VS_HOLD",   TypeKey="G3K AP VS HOLD",   BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_VS_HOLD,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_FLC",        TypeKey="G3K AP FLC",        BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_FLC,        0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_FD",         TypeKey="G3K AP FD",         BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_FD,         0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_ALT",        TypeKey="G3K AP ALT",        BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_ALT,        0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_VNV",        TypeKey="G3K AP VNV",        BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_VNV,        0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_BC",         TypeKey="G3K AP BC",         BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_BC,         0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_MACH_TOG",   TypeKey="G3K AP MACH TOG",   BasePos=new Point(10,  10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_MACH_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_YAW_DMPR",   TypeKey="G3K AP YAW DMPR",   BasePos=new Point(10,  10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_YAW_DAMPER,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AP_LVL",         TypeKey="G3K AP LVL",         BasePos=new Point(10,  10), ZoomWidth=93, ZoomHeight=63, OnClick = () => { if (_apWingLeveler == 0) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_LVL, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } },
            new() { Name="G3K_AP_BANK_INC",    TypeKey="G3K AP BANK INC",    BasePos=new Point(10,  10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_BANK_INC,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_HDG_SYNC", TypeKey="G3K HDG SYNC", BasePos=new Point(200, 200), ZoomWidth=70, ZoomHeight=70, IsRound=true, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_HDG_SYNC, (uint)Math.Round(_headingIndicator), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_CRS1_DIR", TypeKey="G3K CRS1 DIR", BasePos=new Point(300, 200), ZoomWidth=70, ZoomHeight=70, IsRound=true, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_CRS1_DIR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_CRS2_DIR", TypeKey="G3K CRS2 DIR", BasePos=new Point(400, 200), ZoomWidth=117, ZoomHeight=117, IsRound=true, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_CRS2_DIR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_TSC1_TKP", TypeKey="G3K TSC1 TKP", BasePos=new Point(500, 200), ZoomWidth=70, ZoomHeight=70, IsRound=true, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_TSC1_TKP, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_TSC1_BKP", TypeKey="G3K TSC1 BKP", BasePos=new Point(600, 200), ZoomWidth=70, ZoomHeight=70, IsRound=true, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_TSC1_BKP, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MASTER_WARN", TypeKey="G3K Master Warn", BasePos=new Point(700, 200), ZoomWidth=100, ZoomHeight=60, PressVisualMs=0, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MASTER_WARN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_MASTER_CAUT", TypeKey="G3K Master Caut", BasePos=new Point(820, 200), ZoomWidth=100, ZoomHeight=60, PressVisualMs=0, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_MASTER_CAUT, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_AILTRIM_INC", TypeKey="G3K AilTrim Inc", BasePos=new Point(3380, 2020), ZoomWidth=120, ZoomHeight=70, PressVisualMs=0, OnClick = () => { _aileronTrimPos = Math.Clamp(_aileronTrimPos + 328, -16383, 16383); SendAileronTrimSet(_aileronTrimPos); _ailTrimIndRightUntilUtc = DateTime.UtcNow.AddMilliseconds(250); } },
            new() { Name="G3K_AILTRIM_DEC", TypeKey="G3K AilTrim Dec", BasePos=new Point(3520, 2020), ZoomWidth=120, ZoomHeight=70, PressVisualMs=0, OnClick = () => { _aileronTrimPos = Math.Clamp(_aileronTrimPos - 328, -16383, 16383); SendAileronTrimSet(_aileronTrimPos); _ailTrimIndLeftUntilUtc = DateTime.UtcNow.AddMilliseconds(250); } },
            new() { Name="G3K_AILTRIM_IND_L", TypeKey="G3K AilTrim Ind L", BasePos=new Point(3380, 1930), ZoomWidth=164, ZoomHeight=88, NoResize=true, Img=LoadOverlayImage("ail trim sw left.png"), ShowWhen=() => DateTime.UtcNow < _ailTrimIndLeftUntilUtc },
            new() { Name="G3K_AILTRIM_IND_R", TypeKey="G3K AilTrim Ind R", BasePos=new Point(3520, 1930), ZoomWidth=164, ZoomHeight=88, NoResize=true, Img=LoadOverlayImage("ail trim sw right.png"), ShowWhen=() => DateTime.UtcNow < _ailTrimIndRightUntilUtc },
            new() { Name="G3K_STRUCT_DEICE", TypeKey="G3K Struct Deice", BasePos=new Point(5, 2000), ZoomWidth=150, ZoomHeight=100, PressVisualMs=0, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_DEICE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_STRUCT_DEICE_UP", TypeKey="G3K Struct Deice Up", BasePos=new Point(5, 2000), ZoomWidth=46, ZoomHeight=78, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_up.png"), ShowWhen=() => deiceActive },
            new() { Name="G3K_STRUCT_DEICE_DN", TypeKey="G3K Struct Deice Dn", BasePos=new Point(5, 2000), ZoomWidth=43, ZoomHeight=74, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_dn.png"), ShowWhen=() => !deiceActive },
            new() { Name="G3K_WING_LIGHTS", TypeKey="G3K Wing Lights", BasePos=new Point(400, 2000), ZoomWidth=64, ZoomHeight=135, PressVisualMs=0, OnClick = () => ToggleWingLights() },
            new() { Name="G3K_WING_LIGHTS_UP", TypeKey="G3K Wing Lights Up", BasePos=new Point(399, 1715), ZoomWidth=46, ZoomHeight=78, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_up.png"), ShowWhen=() => _lightingWing1 >= 0.5 },
            new() { Name="G3K_WING_LIGHTS_DN", TypeKey="G3K Wing Lights Dn", BasePos=new Point(399, 1778), ZoomWidth=43, ZoomHeight=74, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_dn.png"), ShowWhen=() => _lightingWing1 < 0.5 },
            new() { Name="G3K_PROPDEICE", TypeKey="G3K Prop Deice", BasePos=new Point(429, 1716), ZoomWidth=64, ZoomHeight=135, PressVisualMs=0, OnClick = () => { if (propDeiceActive) SendPropDeiceOff(); else SendPropDeiceOn(); } },
            new() { Name="G3K_PROPDEICE_UP", TypeKey="G3K Prop Deice Up", BasePos=new Point(437, 1714), ZoomWidth=46, ZoomHeight=78, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_up.png"), ShowWhen=() => propDeiceActive },
            new() { Name="G3K_PROPDEICE_DN", TypeKey="G3K Prop Deice Dn", BasePos=new Point(437, 1777), ZoomWidth=43, ZoomHeight=74, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_dn.png"), ShowWhen=() => !propDeiceActive },
            new() { Name="G3K_PITOTHEAT_L", TypeKey="G3K Pitot Heat L", BasePos=new Point(290, 1958), ZoomWidth=64, ZoomHeight=135, PressVisualMs=0, OnClick = () => TogglePitotHeatL() },
            new() { Name="G3K_PITOTHEAT_L_UP", TypeKey="G3K Pitot Heat L Up", BasePos=new Point(298, 1956), ZoomWidth=46, ZoomHeight=78, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_up.png"), ShowWhen=() => pitotHeatLActive },
            new() { Name="G3K_PITOTHEAT_L_DN", TypeKey="G3K Pitot Heat L Dn", BasePos=new Point(298, 2019), ZoomWidth=43, ZoomHeight=74, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_dn.png"), ShowWhen=() => !pitotHeatLActive },
            new() { Name="G3K_PITOTHEAT_R", TypeKey="G3K Pitot Heat R", BasePos=new Point(442, 1954), ZoomWidth=64, ZoomHeight=135, PressVisualMs=0, OnClick = () => TogglePitotHeatR() },
            new() { Name="G3K_PITOTHEAT_R_UP", TypeKey="G3K Pitot Heat R Up", BasePos=new Point(450, 1952), ZoomWidth=46, ZoomHeight=78, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_up.png"), ShowWhen=() => pitotHeatRActive },
            new() { Name="G3K_PITOTHEAT_R_DN", TypeKey="G3K Pitot Heat R Dn", BasePos=new Point(450, 2015), ZoomWidth=43, ZoomHeight=74, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_dn.png"), ShowWhen=() => !pitotHeatRActive },
            new() { Name="G3K_INERTSEP", TypeKey="G3K Inert Sep", BasePos=new Point(564, 1954), ZoomWidth=64, ZoomHeight=135, PressVisualMs=0, OnClick = () => { if (antiIceEng1Active) SendInertSepOff(); else SendInertSepOn(); } },
            new() { Name="G3K_INERTSEP_UP", TypeKey="G3K Inert Sep Up", BasePos=new Point(572, 1952), ZoomWidth=46, ZoomHeight=78, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_up.png"), ShowWhen=() => antiIceEng1Active },
            new() { Name="G3K_INERTSEP_DN", TypeKey="G3K Inert Sep Dn", BasePos=new Point(572, 2015), ZoomWidth=43, ZoomHeight=74, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_dn.png"), ShowWhen=() => !antiIceEng1Active },
            new() { Name="G3K_LTS_TEST", TypeKey="G3K Lts Test", BasePos=new Point(686, 1954), ZoomWidth=64, ZoomHeight=135, PressVisualMs=0, OnClick = () => SendLtsTest() },
            new() { Name="G3K_WINDSHIELD_DEICE", TypeKey="G3K Windshield Deice", BasePos=new Point(551, 1716), ZoomWidth=64, ZoomHeight=135, PressVisualMs=0, OnClick = () => ToggleWindshieldDeice() },
            new() { Name="G3K_WINDSHIELD_DEICE_UP", TypeKey="G3K Windshield Deice Up", BasePos=new Point(559, 1714), ZoomWidth=46, ZoomHeight=78, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_up.png"), ShowWhen=() => windshieldDeiceActive },
            new() { Name="G3K_WINDSHIELD_DEICE_DN", TypeKey="G3K Windshield Deice Dn", BasePos=new Point(559, 1777), ZoomWidth=43, ZoomHeight=74, NoResize=true, Img=LoadOverlayImage("SW ice sysrem_dn.png"), ShowWhen=() => !windshieldDeiceActive },
            new() { Name="G3K_FLAPS_SW",  TypeKey="G3K Flaps SW",  BasePos=new Point(400, 400), ZoomWidth=217, Img=LoadOverlayImage("SW flaps.png") },
            // Gear LEDs — mateixa lògica que G1000, mides × 14/11.75
            new() { Name="G3K_LED_GEAR_LEFT",    TypeKey="G3K LED Gear Left",    BasePos=new Point(720,  1710), ZoomWidth=51, Img=ledGreenImg,
                    ShowWhen = () => mainBusVolts >= 5.0 && gearLeftPercent  >= 100 },
            new() { Name="G3K_LED_GEAR_NOSE",    TypeKey="G3K LED Gear Nose",    BasePos=new Point(770,  1664), ZoomWidth=51, Img=ledGreenImg,
                    ShowWhen = () => mainBusVolts >= 5.0 && gearNosePercent  >= 100 },
            new() { Name="G3K_LED_GEAR_RIGHT",   TypeKey="G3K LED Gear Right",   BasePos=new Point(822,  1710), ZoomWidth=51, Img=ledGreenImg,
                    ShowWhen = () => mainBusVolts >= 5.0 && gearRightPercent >= 100 },
            new() { Name="G3K_LED_DEICE_AIRFRAME", TypeKey="G3K LED Deice Airframe", BasePos=new Point(188, 1643), ZoomWidth=22, Img=ledGreenImg,
                    ShowWhen = () => _isDeiceAirframe >= 0.5 || _ltsTestActive },
            new() { Name="G3K_LED_DEICE_AIRFRAME2", TypeKey="G3K LED Deice Airframe2", BasePos=new Point(238, 1643), ZoomWidth=22, Img=ledGreenImg,
                    ShowWhen = () => _isDeiceAirframe2 >= 0.5 || _ltsTestActive },
            new() { Name="G3K_LED_PROPDEICE", TypeKey="G3K LED Propdeice", BasePos=new Point(288, 1643), ZoomWidth=22, Img=ledGreenImg,
                    ShowWhen = () => propDeiceActive || _ltsTestActive },
            new() { Name="G3K_LED_WINDSHIELDDEICE", TypeKey="G3K LED Windshielddeice", BasePos=new Point(338, 1643), ZoomWidth=22, Img=ledGreenImg,
                    ShowWhen = () => windshieldDeiceActive || _ltsTestActive },
            new() { Name="G3K_LED_WINDSHIELDDEICE2", TypeKey="G3K LED Windshielddeice2", BasePos=new Point(388, 1643), ZoomWidth=22, Img=ledGreenImg,
                    ShowWhen = () => windshieldDeiceActive || _ltsTestActive },
            new() { Name="G3K_LGEAR_SW",  TypeKey="G3K LGear SW",  BasePos=new Point(890, 1640), ZoomWidth=118, Img=swLgearImg },
            new() { Name="G3K_PBRAKE_SW", TypeKey="G3K PBrake SW", BasePos=new Point(1010, 1640), ZoomWidth=260, Img=swPbrakeImg },
            new() { Name="G3K_PBRAKE_CK", TypeKey="G3K PBrake CK", BasePos=new Point(1010, 1640), ZoomWidth=260, ZoomHeight=130, PressVisualMs=0,
                    OnClick = () => { simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_PBRAKE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); pbMain.Invalidate(); } },
            new() { Name="G3K_LED_PBRAKE", TypeKey="G3K LED PBrake", BasePos=new Point(1280, 1620), ZoomWidth=50, Img=ledRedImg,
                    ShowWhen = () => mainBusVolts >= 5.0 && pbrakeActive },
            new() { Name="G3K_LGEAR_UP",  TypeKey="G3K LGear UP",  BasePos=new Point(890, 1590), ZoomWidth=118, ZoomHeight=70, PressVisualMs=0,
                    OnClick = () => { simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.LGEAR_UP, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } },
            new() { Name="G3K_LGEAR_DN",  TypeKey="G3K LGear DN",  BasePos=new Point(890, 1750), ZoomWidth=118, ZoomHeight=70, PressVisualMs=0,
                    OnClick = () => { simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.LGEAR_DN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } },
            new() { Name="G3K_LED_GEAR_TRANSIT", TypeKey="G3K LED Gear Transit", BasePos=new Point(770,  1601), ZoomWidth=47, Img=ledRedImg,
                    ShowWhen = () => mainBusVolts >= 5.0 &&
                                     ((gearLeftPercent  > 0 && gearLeftPercent  < 100) ||
                                      (gearNosePercent  > 0 && gearNosePercent  < 100) ||
                                      (gearRightPercent > 0 && gearRightPercent < 100)) },
            new() { Name="G3K_FLAPS_UP",  TypeKey="G3K Flaps UP",  BasePos=new Point(400, 370), ZoomWidth=217, ZoomHeight=60, PressVisualMs=0,
                    OnClick = () => { _g3kFlapsAnimDir = -1; pbMain.Invalidate();
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FLAPS_SWITCH_UP, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        Task.Delay(150).ContinueWith(_ => this.BeginInvoke(() => { _g3kFlapsAnimDir = 0; pbMain.Invalidate(); })); } },
            new() { Name="G3K_FLAPS_DN",  TypeKey="G3K Flaps DN",  BasePos=new Point(400, 450), ZoomWidth=217, ZoomHeight=60, PressVisualMs=0,
                    OnClick = () => { _g3kFlapsAnimDir =  1; pbMain.Invalidate();
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FLAPS_SWITCH_DN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        Task.Delay(150).ContinueWith(_ => this.BeginInvoke(() => { _g3kFlapsAnimDir = 0; pbMain.Invalidate(); })); } },
            new() { Name="G3K_CAUT_LIGHT", TypeKey="G3K Caut Light", BasePos=new Point(900, 200), ZoomWidth=108, ZoomHeight=105, NoResize=true, Img=LoadOverlayImage("MASTER CAUTION LIGHT.png"), ShowWhen=() => _masterCautionActive != 0 },
            new() { Name="G3K_WARN_LIGHT", TypeKey="G3K Warn Light", BasePos=new Point(1020, 200), ZoomWidth=111, ZoomHeight=107, NoResize=true, Img=LoadOverlayImage("MASTER WARNING LIGHT.png"), ShowWhen=() => _masterWarningActive != 0 },
            new() { Name="G3K_FSEL_L", TypeKey="G3K Fuel Sel L", BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, 2, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_FSEL_R", TypeKey="G3K Fuel Sel R", BasePos=new Point(300, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, 3, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_FSEL_OFF", TypeKey="G3K Fuel Sel Off", BasePos=new Point(400, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) },
            new() { Name="G3K_FSEL_SW", TypeKey="G3K Fuel Sel SW", BasePos=new Point(500, 100), ZoomWidth=244, ZoomHeight=244, Img=LoadOverlayImage("FUEL SEL SW.png") },
            new() { Name="G3K_AP_NOSE_UP", TypeKey="G3K AP NOSE UP", BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => {
                if (_kap140State != 0 && _apVsHold == 1)
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_KAP140_UP,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                else if (_kap140State != 0 && _apFlcActive == 1)
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_SPD_DEC,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                else {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_VS_VAR_INC,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_SPD_DEC,     0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
                _g3kApWheelOffset += 15; pbMain.Invalidate();
            }},
            new() { Name="G3K_AP_NOSE_DN", TypeKey="G3K AP NOSE DN", BasePos=new Point(200, 10), ZoomWidth=93, ZoomHeight=63, OnClick = () => {
                if (_kap140State != 0 && _apVsHold == 1)
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_KAP140_DN,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                else if (_kap140State != 0 && _apFlcActive == 1)
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_SPD_INC,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                else {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_VS_VAR_DEC, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_SPD_INC,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
                _g3kApWheelOffset -= 15; pbMain.Invalidate();
            }},
            };

            LoadCalibrationPositions();

            // Timer global per apagar ombres + animació flaps SW
            uiSurfaceTimer.Tick += (s, e) =>
            {
                var now = DateTime.UtcNow;
                bool needsRepaint = flapsAnimDurationSec > 0f;
                if (!needsRepaint)
                    foreach (var srf in uiSurfaces)
                        foreach (var b in srf.Buttons)
                            if (b.IsVisuallyPressed(now)) { needsRepaint = true; break; }
                if (!needsRepaint && _g3000Active)
                    foreach (var ov in g3000OverlayImages)
                        if (ov.IsVisuallyPressed(now)) { needsRepaint = true; break; }
                if (!needsRepaint && _g3000Active && (now < _ailTrimIndLeftUntilUtc || now < _ailTrimIndRightUntilUtc))
                    needsRepaint = true;
                if (needsRepaint)
                {
                    foreach (var srf in uiSurfaces)
                        srf.RefreshVisual();
                    if (_g3000Active) pbMain.Invalidate();
                }
            };

            uiSurfaceTimer.Start();

            _thrSyncTimer.Tick += (s, e) => SyncThrHandleFromMsfs();
            _thrSyncTimer.Start();

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

        private void ApplyG3000Region()
        {
            Region formRegion = new Region(new Rectangle(
                0, 0, this.Width, this.Height));

            // FORAT PFD
            formRegion.Exclude(new Rectangle(200, 458, 1595, 995));

            // FORAT MFD (amplada 1615px confirmada per l'usuari → fins a x=3655)
            formRegion.Exclude(new Rectangle(2040, 459, 1615, 991));

            // FORAT TSC esquerra
            formRegion.Exclude(new Rectangle(1867, 1653, 721, 442));

            // FORAT TSC dreta
            formRegion.Exclude(new Rectangle(2821, 1653, 724, 442));

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
            bool hasMonitor = false;
            using (var searcher = new System.Management.ManagementObjectSearcher(
                @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%RTK1B1A%' AND ConfigManagerErrorCode = 0"))
            {
                hasMonitor = searcher.Get().Count > 0;
            }
            if (!hasPanel || !hasMonitor)
            {
                var screen4K = Screen.AllScreens.FirstOrDefault(s => !s.Primary && s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                    ?? Screen.AllScreens.First(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160);
                using (var msgHelper = new Form { StartPosition = FormStartPosition.Manual, TopMost = true, Size = new Size(1, 1), ShowInTaskbar = false })
                {
                    msgHelper.Location = new Point(screen4K.Bounds.X + screen4K.Bounds.Width / 2, screen4K.Bounds.Y + screen4K.Bounds.Height / 2);
                    msgHelper.Show();
                    MessageBox.Show(msgHelper, "panelAIR verification failed.\n\nThis application requires an SFS panelAIR device to operate.\n\nIf you own a panelAIR device, please ensure that:\n\n  • The panelAIR display is connected to the computer and powered on.\n  • The panelAIR USB touch cable is connected to the computer.\n\nThen restart the application.\n\n(If you do not own a panelAIR device, the application will remain functional for 10 minutes only.)", "panelAIR1000", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    Text = "No panelAIR1000 detected.\nClosing in 10:00...",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 24)
                };
                countdownForm.Controls.Add(label);
                countdownForm.Show();   // no-modal: l'app principal continua inicialitzant-se

                var startTime = DateTime.Now;
                int totalSeconds = 600;
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
                                label.Text = $"No panelAIR1000 detected.\nClosing in {secondsLeft / 60}:{secondsLeft % 60:D2}...";
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
        private void SetElecSourceSwitch(double value)
        {
            if (simconnect == null) return;
            simconnect.SetDataOnSimObject(DEFINITIONS.ElecSrcWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new ElecSrcStruct { value = value });
        }

        private void ToggleElecCrashbar()
        {
            if (simconnect == null) return;
            double newVal = _elecCrashbar < 0.5 ? 1.0 : 0.0;
            simconnect.SetDataOnSimObject(DEFINITIONS.ElecCrashbarWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new ElecCrashbarStruct { value = newVal });
            if (newVal < 0.5)
            {
                SetElecSourceSwitch(0);
                SetElecGeneratorSwitch(0);
            }
        }

        private void SetElecStarter(double value)
        {
            if (simconnect == null) return;
            simconnect.SetDataOnSimObject(DEFINITIONS.ElecStarterWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new ElecStarterStruct { value = value });
        }

        private void SetElecIgnition(double value)
        {
            if (simconnect == null) return;
            simconnect.SetDataOnSimObject(DEFINITIONS.ElecIgnitionWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new ElecIgnitionStruct { value = value });
        }

        private void SetIgnitionPos(int pos)
        {
            if (simconnect == null) return;
            // pos: 0=UP=AUTO(L:1), 1=CENTER=ON(L:2), 2=DN=OFF(L:3)
            double lval = pos + 1;
            simconnect.SetDataOnSimObject(DEFINITIONS.ElecIgnitionWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new ElecIgnitionStruct { value = lval });
        }

        private void SetAPTrimPos(int pos)
        {
            if (simconnect == null) return;
            _apTrim = pos;
            simconnect.SetDataOnSimObject(DEFINITIONS.APTrimWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new APTrimStruct { value = pos });
            if (pos == 0)
            {
                // ON (UP): enable AP if disengaged
                if (_apDisengaged != 0)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUTOPILOT_DISENGAGE_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            else if (pos == 1)
            {
                // AP OFF (CENTER): disengage AP if engaged, enable trims
                if (_apDisengaged == 0)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUTOPILOT_DISENGAGE_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                if (_rudderTrimDisabled != 0)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.RUDDER_TRIM_DISABLED_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                if (_aileronTrimDisabled != 0)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.AILERON_TRIM_DISABLED_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
            else
            {
                // OFF (DN): disengage AP if engaged, disable trims
                if (_apDisengaged == 0)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUTOPILOT_DISENGAGE_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                if (_rudderTrimDisabled == 0)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.RUDDER_TRIM_DISABLED_SET, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                if (_aileronTrimDisabled == 0)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.AILERON_TRIM_DISABLED_SET, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            }
        }

        private void SetFuelSelection(double value)
        {
            if (simconnect == null) return;
            simconnect.SetDataOnSimObject(DEFINITIONS.FuelSelectionWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new FuelSelectionStruct { value = value });
        }

        private void MoveThrHandle(int targetY)
        {
            var thrHandle = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
            if (thrHandle == null) return;
            int targetX = _thrHandleLeftRail ? 3694 : 3734;
            thrHandle.BasePos = new Point(targetX, targetY);
            pbMain.Invalidate();
        }

        private void SyncThrHandleFromMsfs()
        {
            if (!_thrQuadrantVisible || _thrHandleDragging || simconnect == null) return;
            if (double.IsNaN(_lastThrLeverPos)) return;
            if (DateTime.UtcNow < _thrIgnoreFollowUntil) return;

            if (!_thrHandleLeftRail)
            {
                var taxiOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_TAXI");
                var thrH   = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                if (taxiOv == null || thrH == null) return;
                _thrHandleLeftRailCenterY = taxiOv.BasePos.Y + taxiOv.Bounds.Height / 2 - thrH.Bounds.Height / 2;
                _thrHandleLeftRail = true;
                _taxiPropActive    = true;
            }

            int read    = (int)_lastThrLeverPos;
            int centerY = _thrHandleLeftRailCenterY;
            int handleY = read >= 0
                ? centerY - (int)((long)read * 375 / 16383)
                : centerY - (int)((long)read * 250 / 2048);
            handleY = Math.Max(centerY - 375, Math.Min(centerY + 250, handleY));
            MoveThrHandle(handleY);
        }

        private void TogglePulseLight()
        {
            if (simconnect == null) return;
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_PULSELIGHT_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void ToggleWingLights()
        {
            if (simconnect == null) return;
            uint newState = _lightingWing1 < 0.5 ? 1u : 0u;
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.WING_LIGHTS_SET, newState, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void SendPropDeiceOn()
        {
            if (simconnect == null) return;
            if (!propDeiceActive)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROPDEICE_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            simconnect.SetDataOnSimObject(DEFINITIONS.PropDeiceWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PropDeiceWriteStruct { value = 1 });
        }

        private void SendPropDeiceOff()
        {
            if (simconnect == null) return;
            if (propDeiceActive)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROPDEICE_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            simconnect.SetDataOnSimObject(DEFINITIONS.PropDeiceWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PropDeiceWriteStruct { value = 0 });
        }

        private void SendInertSepOn()
        {
            if (simconnect == null) return;
            if (!antiIceEng1Active)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.INERT_SEP_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void SendInertSepOff()
        {
            if (simconnect == null) return;
            if (antiIceEng1Active)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.INERT_SEP_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void SendLtsTest()
        {
            if (simconnect == null) return;
            simconnect.SetDataOnSimObject(DEFINITIONS.LtsTestWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new LtsTestWriteStruct { value = 1 });
            _ltsTestActive = true;
            pbMain.Invalidate();
            Task.Delay(1000).ContinueWith(_ => this.Invoke(() =>
            {
                simconnect?.SetDataOnSimObject(DEFINITIONS.LtsTestWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new LtsTestWriteStruct { value = 0 });
                _ltsTestActive = false;
                pbMain.Invalidate();
            }));
        }

        private void TogglePitotHeatL()
        {
            if (simconnect == null) return;
            simconnect.TransmitClientEvent(0, pitotHeatLActive ? SIMCONNECT_EVENTS.PITOTHEAT_OFF : SIMCONNECT_EVENTS.PITOTHEAT_ON, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void TogglePitotHeatR()
        {
            if (simconnect == null) return;
            simconnect.TransmitClientEvent(0, pitotHeatRActive ? SIMCONNECT_EVENTS.PITOTHEAT2_OFF : SIMCONNECT_EVENTS.PITOTHEAT2_ON, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void ToggleWindshieldDeice()
        {
            if (simconnect == null) return;
            uint newState = windshieldDeiceActive ? 0u : 1u;
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.WINDSHIELD_DEICE_SET, newState, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void SetFuelPump(double pos)
        {
            if (simconnect == null) return;
            // pos: 0=UP=AUTO(K:2), 1=CENTER=ON(K:1), 2=DN=OFF(K:0)
            uint kval = (uint)(2 - (int)pos);
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.ELECT_FUEL_PUMP1_SET, kval, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void SetElecGeneratorSwitch(double value)
        {
            if (simconnect == null) return;
            simconnect.SetDataOnSimObject(DEFINITIONS.ElecGenWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new ElecGenStruct { value = value });
        }

        private void CheckTBM930Mod()
        {
            if (_tbm930ModCheckDone) return;
            _tbm930ModCheckDone = true;

            string? communityPath = null;
            string[] cfgPaths = new[]
            {
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft Flight Simulator", "UserCfg.opt"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\Microsoft.FlightSimulator_8wekyb3d8bbwe\LocalCache\UserCfg.opt")
            };

            foreach (var cfgPath in cfgPaths)
            {
                if (!System.IO.File.Exists(cfgPath)) continue;
                try
                {
                    foreach (var line in System.IO.File.ReadAllLines(cfgPath))
                    {
                        if (line.TrimStart().StartsWith("InstalledPackagesPath"))
                        {
                            int q1 = line.IndexOf('"');
                            int q2 = line.IndexOf('"', q1 + 1);
                            if (q1 >= 0 && q2 > q1)
                            {
                                communityPath = System.IO.Path.Combine(line.Substring(q1 + 1, q2 - q1 - 1), "Community");
                                break;
                            }
                        }
                    }
                }
                catch { }
                if (communityPath != null) break;
            }

            if (communityPath == null || !System.IO.Directory.Exists(communityPath)) return;

            bool found = false;
            try
            {
                foreach (var dir in System.IO.Directory.GetDirectories(communityPath))
                {
                    var dirName = System.IO.Path.GetFileName(dir).ToLowerInvariant();
                    if (dirName.Contains("tbm930") && System.IO.Directory.Exists(System.IO.Path.Combine(dir, "SimObjects", "Airplanes", "Asobo_TBM930")))
                    { found = true; break; }
                }
            }
            catch { }

            if (!found)
            {
                var result = MessageBox.Show(
                    "MSFS 2020 users: for full G3000 panel functionality, we recommend installing the free mod " +
                    "'TBM930 - LVars for Bleed Air, Inert Sep and Cabin Lights' by FlyingDutch.\n\n" +
                    "Download it at:\nhttps://flightsim.to/addon/57918/tbm930-lvars-for-bleed-inert-and-cabin-lights\n\n" +
                    "Once downloaded, copy the contents to your MSFS 2020 Community folder.\n\n" +
                    "Would you like to open the download page?",
                    "Recommended mod not detected",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://flightsim.to/addon/57918/tbm930-lvars-for-bleed-inert-and-cabin-lights",
                        UseShellExecute = true
                    }); } catch { }
                }
            }
        }

        private void UpdateRotAIRStatus()
        {
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

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_1, "SFS.G3000_PFD1_SoftKey_1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_1, false);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_2,  "SFS.G3000_PFD1_SoftKey_2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_2,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_3,  "SFS.G3000_PFD1_SoftKey_3");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_3,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_4,  "SFS.G3000_PFD1_SoftKey_4");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_4,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_5,  "SFS.G3000_PFD1_SoftKey_5");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_5,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_6,  "SFS.G3000_PFD1_SoftKey_6");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_6,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_7,  "SFS.G3000_PFD1_SoftKey_7");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_7,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_8,  "SFS.G3000_PFD1_SoftKey_8");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_8,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_9,  "SFS.G3000_PFD1_SoftKey_9");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_9,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_10, "SFS.G3000_PFD1_SoftKey_10");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_10, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_11, "SFS.G3000_PFD1_SoftKey_11");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_11, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_SK_12, "SFS.G3000_PFD1_SoftKey_12");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_SK_12, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_1,  "SFS.G3000_PFD2_SoftKey_1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_1,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_2,  "SFS.G3000_PFD2_SoftKey_2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_2,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_3,  "SFS.G3000_PFD2_SoftKey_3");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_3,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_4,  "SFS.G3000_PFD2_SoftKey_4");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_4,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_5,  "SFS.G3000_PFD2_SoftKey_5");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_5,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_6,  "SFS.G3000_PFD2_SoftKey_6");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_6,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_7,  "SFS.G3000_PFD2_SoftKey_7");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_7,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_8,  "SFS.G3000_PFD2_SoftKey_8");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_8,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_9,  "SFS.G3000_PFD2_SoftKey_9");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_9,  false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_10, "SFS.G3000_PFD2_SoftKey_10");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_10, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_11, "SFS.G3000_PFD2_SoftKey_11");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_11, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MFD_SK_12, "SFS.G3000_PFD2_SoftKey_12");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MFD_SK_12, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_TSC1_SK_1, "SFS.G3000_TSC1_SoftKey_1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_TSC1_SK_1, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_TSC1_SK_2, "SFS.G3000_TSC1_SoftKey_2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_TSC1_SK_2, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_TSC1_SK_3, "SFS.G3000_TSC1_SoftKey_3");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_TSC1_SK_3, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_TSC2_SK_1, "SFS.G3000_TSC2_SoftKey_1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_TSC2_SK_1, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_TSC2_SK_2, "SFS.G3000_TSC2_SoftKey_2");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_TSC2_SK_2, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_TSC2_SK_3, "SFS.G3000_TSC2_SoftKey_3");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_TSC2_SK_3, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_MASTER, "AP_MASTER");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_MASTER, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_HDG_HOLD, "AP_HDG_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_HDG_HOLD, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_NAV1_HOLD, "AP_NAV1_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_NAV1_HOLD, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_APR_HOLD, "AP_APR_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_APR_HOLD, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_XFR, "SFS.XFR_Toggle");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_XFR, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_VS_HOLD, "AP_VS_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_VS_HOLD, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_FLC, "FLIGHT_LEVEL_CHANGE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_FLC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_FD, "TOGGLE_FLIGHT_DIRECTOR");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_FD, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_ALT, "AP_ALT_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_ALT, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_VNV, "SFS.VNAV_Toggle");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_VNV, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_BC, "AP_BC_HOLD");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_BC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_MACH_TOGGLE, "AP_MANAGED_SPEED_IN_MACH_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_MACH_TOGGLE, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_YAW_DAMPER, "YAW_DAMPER_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_YAW_DAMPER, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_LVL, "AP_WING_LEVELER");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_LVL, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_BANK_INC, "AP_MAX_BANK_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_BANK_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_HDG_SYNC, "HEADING_BUG_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_HDG_SYNC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_CRS1_DIR, "SFS.G3000_PFD1_CRS_Push");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_CRS1_DIR, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_CRS2_DIR, "SFS.G3000_PFD1_CRS_Push");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_CRS2_DIR, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_TSC1_TKP, "SFS.G3000_TSC1_TopKnob_Push");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_TSC1_TKP, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_TSC1_BKP, "SFS.G3000_TSC1_BottomKnob_Push");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_TSC1_BKP, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MASTER_WARN, "MASTER_WARNING_ACKNOWLEDGE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MASTER_WARN, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_MASTER_CAUT, "MASTER_CAUTION_ACKNOWLEDGE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_MASTER_CAUT, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_KAP140_UP, "AP_KAP140_UP");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_KAP140_UP, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_KAP140_DN, "AP_KAP140_DN");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_KAP140_DN, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_SPD_DEC, "AP_SPD_VAR_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_SPD_DEC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_SPD_INC, "AP_SPD_VAR_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_SPD_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_VS_VAR_INC, "AP_VS_VAR_INC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_VS_VAR_INC, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.G3K_AP_VS_VAR_DEC, "AP_VS_VAR_DEC");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.G3K_AP_VS_VAR_DEC, false);

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
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PROPDEICE_TOGGLE, "TOGGLE_PROPELLER_DEICE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PROPDEICE_TOGGLE, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.INERT_SEP_TOGGLE, "ANTI_ICE_TOGGLE_ENG1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.INERT_SEP_TOGGLE, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PITOTHEAT_ON, "SFS.TBM930_PitotHeat_On");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PITOTHEAT_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PITOTHEAT_OFF, "SFS.TBM930_PitotHeat_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PITOTHEAT_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PITOTHEAT2_ON, "SFS.TBM930_PitotHeat2_On");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PITOTHEAT2_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PITOTHEAT2_OFF, "SFS.TBM930_PitotHeat2_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PITOTHEAT2_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.WING_LIGHTS_SET, "WING_LIGHTS_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.WING_LIGHTS_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.WINDSHIELD_DEICE_SET, "WINDSHIELD_DEICE_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.WINDSHIELD_DEICE_SET, false);
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
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ENGINE_AUTO_START, "ENGINE_AUTO_START");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ENGINE_AUTO_START, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ENGINE_AUTO_SHUTDOWN, "ENGINE_AUTO_SHUTDOWN");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ENGINE_AUTO_SHUTDOWN, false);
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
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_IGN1_ON, "SFS.TBM930_Ign1_On");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_IGN1_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_IGN1_OFF, "SFS.TBM930_Ign1_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_IGN1_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_IGN1_AUTO, "SFS.TBM930_Ign1_Auto");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_IGN1_AUTO, false);

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
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CAMERA STATE", "number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_Elec_Source_Switch_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_Elec_Generator_Switch_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_Elec_CrashBar_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_Starter", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_Ignition", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_BoostFuelPump", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_APTrim", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT DISENGAGED", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "RUDDER TRIM DISABLED", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AILERON TRIM DISABLED", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:LIGHTING_LOGO_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "LIGHT LOGO", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "CIRCUIT CONNECTION ON:17", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:TBM930_FUEL_SELECTION_SET", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG PROPELLER LEVER POSITION:1", "Position 16k", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PROP RPM:1", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG THROTTLE LEVER POSITION:1", "Position 16k", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "GENERAL ENG FUEL VALVE:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:KAP140_State", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT VERTICAL HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT FLIGHT LEVEL CHANGE", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT WING LEVELER", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT HEADING LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT APPROACH HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT GLIDESLOPE HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT NAV1 LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT FLIGHT DIRECTOR ACTIVE:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT ALTITUDE LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_VNAVButtonValue", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT YAW DAMPER", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT MAX BANK", "Radians", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT BACKCOURSE HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AUTOPILOT MASTER", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_PushXFR", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "HEADING INDICATOR", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:Generic_Master_Caution_Active", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:Generic_Master_Warning_Active", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "AILERON TRIM PCT", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:LIGHTING_WING_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "PROP DEICE SWITCH:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "WINDSHIELD DEICE SWITCH", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:DEICE_Pitot_1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:DEICE_Pitot_2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "ENG ANTI ICE:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_IsDeiceAirFrame", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Struct1, "L:XMLVAR_IsDeiceAirFrame2", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.CameraState, "CAMERA STATE", "number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CameraStateWrite>(DEFINITIONS.CameraState);

                simconnect.AddToDataDefinition(DEFINITIONS.ElecSrcWrite, "L:XMLVAR_Elec_Source_Switch_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<ElecSrcStruct>(DEFINITIONS.ElecSrcWrite);

                simconnect.AddToDataDefinition(DEFINITIONS.ElecGenWrite, "L:XMLVAR_Elec_Generator_Switch_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<ElecGenStruct>(DEFINITIONS.ElecGenWrite);

                simconnect.AddToDataDefinition(DEFINITIONS.ElecCrashbarWrite, "L:XMLVAR_Elec_CrashBar_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<ElecCrashbarStruct>(DEFINITIONS.ElecCrashbarWrite);

                simconnect.AddToDataDefinition(DEFINITIONS.ElecStarterWrite, "L:XMLVAR_Starter", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<ElecStarterStruct>(DEFINITIONS.ElecStarterWrite);

                simconnect.AddToDataDefinition(DEFINITIONS.ElecIgnitionWrite, "L:TBM930_IGNITION_SET", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<ElecIgnitionStruct>(DEFINITIONS.ElecIgnitionWrite);
                simconnect.AddToDataDefinition(DEFINITIONS.APTrimWrite, "L:XMLVAR_APTrim", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<APTrimStruct>(DEFINITIONS.APTrimWrite);
                simconnect.AddToDataDefinition(DEFINITIONS.AileronTrimWrite, "AILERON TRIM PCT", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<AileronTrimWriteStruct>(DEFINITIONS.AileronTrimWrite);

                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ELECT_FUEL_PUMP1_SET, "ELECT_FUEL_PUMP1_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ELECT_FUEL_PUMP1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SET_STARTER1_HELD, "SET_STARTER1_HELD");
                //simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SET_STARTER1_HELD, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.STARTER1_SET, "STARTER1_SET");
                //simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.STARTER1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AUTOPILOT_DISENGAGE_TOGGLE, "AUTOPILOT_DISENGAGE_TOGGLE");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AUTOPILOT_DISENGAGE_TOGGLE, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.RUDDER_TRIM_DISABLED_SET, "RUDDER_TRIM_DISABLED_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.RUDDER_TRIM_DISABLED_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AILERON_TRIM_DISABLED_SET, "AILERON_TRIM_DISABLED_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AILERON_TRIM_DISABLED_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AXIS_AILERONTRIM_SET, "AXIS_AILERONTRIM_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AXIS_AILERONTRIM_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AILERON_TRIM_RIGHT, "AILERON_TRIM_RIGHT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AILERON_TRIM_RIGHT, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AILERON_TRIM_LEFT, "AILERON_TRIM_LEFT");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AILERON_TRIM_LEFT, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AILERON_TRIM_SET_EX1, "AILERON_TRIM_SET_EX1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AILERON_TRIM_SET_EX1, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_PULSELIGHT_TOGGLE, "SFS.TBM930_PulseLight_Toggle");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_PULSELIGHT_TOGGLE, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_LIGHT_LDG, "SFS.TBM930_Light_LDG");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_LIGHT_LDG, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_LIGHT_TAXI, "SFS.TBM930_Light_TAXI");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_LIGHT_TAXI, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_LIGHT_OFF, "SFS.TBM930_Light_OFF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_LIGHT_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_FUELSEL_AUTO, "SFS.TBM930_FuelSel_Auto");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_FUELSEL_AUTO, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.TBM930_FUELSEL_MAN, "SFS.TBM930_FuelSel_Man");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.TBM930_FUELSEL_MAN, false);
                simconnect.AddToDataDefinition(DEFINITIONS.LightingLogo1Write, "L:LIGHTING_LOGO_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightingLogo1Struct>(DEFINITIONS.LightingLogo1Write);
                simconnect.AddToDataDefinition(DEFINITIONS.PropDeiceWrite, "L:DEICE_Propeller_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<PropDeiceWriteStruct>(DEFINITIONS.PropDeiceWrite);
                simconnect.AddToDataDefinition(DEFINITIONS.LtsTestWrite, "L:XMLVAR_LTS_Test", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LtsTestWriteStruct>(DEFINITIONS.LtsTestWrite);
                simconnect.AddToDataDefinition(DEFINITIONS.FuelSelectionWrite, "L:TBM930_FUEL_SELECTION_SET", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<FuelSelectionStruct>(DEFINITIONS.FuelSelectionWrite);
                simconnect.AddToDataDefinition(DEFINITIONS.CondLeverCutoffWrite, "L:Condition_Lever_CutOff_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CondLeverCutoffStruct>(DEFINITIONS.CondLeverCutoffWrite);
                simconnect.AddToDataDefinition(DEFINITIONS.PropLeverWrite, "GENERAL ENG PROPELLER LEVER POSITION:1", "Position 16k", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<PropLeverStruct>(DEFINITIONS.PropLeverWrite);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, "SET_FUEL_VALVE_ENG1");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PROP_PITCH1_SET, "PROP_PITCH1_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PROP_PITCH1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AXIS_PROPELLER1_SET, "AXIS_PROPELLER1_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AXIS_PROPELLER1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PROPELLER_SET, "PROPELLER_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PROPELLER_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AXIS_PROPELLER_SET, "AXIS_PROPELLER_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AXIS_PROPELLER_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PROPELLER1_SET, "PROPELLER1_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PROPELLER1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PROP_PITCH1_AXIS_SET_EX, "PROP_PITCH1_AXIS_SET_EX");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PROP_PITCH1_AXIS_SET_EX, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.CONDITION_LEVER_SET, "CONDITION_LEVER_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.CONDITION_LEVER_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.PROP_PITCH_SET, "PROP_PITCH_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.PROP_PITCH_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, "AXIS_THROTTLE_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, false);

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
                    textBox21.ForeColor = Color.DodgerBlue;

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

            textBox21.Text = data.dwApplicationVersionMajor >= 12 ? "MSFS 2024" : "MSFS 2020";
            textBox21.ForeColor = Color.DodgerBlue;
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
                bool needsRepaint = false;
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
                    needsRepaint = true;
                }
                bool newParkingbrake = (s1.read_parking_brake & 1) != 0;


                //System.Diagnostics.Debug.WriteLine($"DME SOUND raw={s1.dme_read}");
                //System.Diagnostics.Debug.WriteLine(">>> OnRecvSimobjectData CALLED");
                //bool newDmeActive = s1.dme_read == 1;
                if (flapsNumPositions != s1.flaps_num_handle_positions)
                {
                    flapsNumPositions = s1.flaps_num_handle_positions;

                    needsRepaint = true;
                }
                if (flapsPoscPalanca != s1.flaps_real_handle_position)
                {
                    flapsPoscPalanca = s1.flaps_real_handle_position;

                    //flapsTimer.Start();
                    needsRepaint = true;
                }
                if (elevatorTrim != s1.elevator_trim)
                {
                    double delta = s1.elevator_trim - elevatorTrim;
                    elevatorTrim = s1.elevator_trim;

                    float frameDelta = (float)(delta / 2.0) * TRIM_FRAME_COUNT * 35f;
                    trimFramePos -= frameDelta;
                    trimFramePos = ((trimFramePos % TRIM_FRAME_COUNT) + TRIM_FRAME_COUNT) % TRIM_FRAME_COUNT;

                    needsRepaint = true;
                }
                double newRudderTrim = s1.rudder_trim / 100.0;
                if (rudderTrim != newRudderTrim)
                {
                    double delta = newRudderTrim - rudderTrim;
                    rudderTrim = newRudderTrim;

                    float frameDelta = (float)(delta / 2.0) * RUDDER_TRIM_FRAME_COUNT * 35f;
                    rudderTrimFramePos -= frameDelta;
                    rudderTrimFramePos = ((rudderTrimFramePos % RUDDER_TRIM_FRAME_COUNT) + RUDDER_TRIM_FRAME_COUNT) % RUDDER_TRIM_FRAME_COUNT;

                    needsRepaint = true;
                }

                if (newCom1Active != com1Active)
                {
                    com1Active = newCom1Active;

                    needsRepaint = true;
                }
                if (newCom2Active != com2Active)
                {
                    com2Active = newCom2Active;

                    needsRepaint = true;
                }
                if (newComReceiveAll != comReceiveAll)
                {
                    comReceiveAll = newComReceiveAll;

                    needsRepaint = true;
                }
                if (newDmeActive != dmeActive)
                {
                    dmeActive = newDmeActive;

                    needsRepaint = true;
                }
                if (newNav1Active != nav1Active)
                {
                    nav1Active = newNav1Active;

                    needsRepaint = true;
                }
                if (newSpkrActive != spkrActive)
                {
                    spkrActive = newSpkrActive;

                    needsRepaint = true;
                }
                if (newMkrMute != mkrMute)
                {
                    mkrMute = newMkrMute;

                    needsRepaint = true;
                }
                if (newAdfActive != adfMute)
                {
                    adfMute = newAdfActive;

                    needsRepaint = true;
                }
                if (newNav2Active != nav2Active)
                {
                    nav2Active = newNav2Active;

                    needsRepaint = true;
                }
                if (newBeacon != beaconActive )
                {
                    beaconActive = newBeacon;

                    needsRepaint = true;
                }
                if (newLand != landActive)
                {
                    landActive = newLand;

                    needsRepaint = true;
                }
                if (newTaxi != taxiActive)
                {
                    taxiActive = newTaxi;

                    needsRepaint = true;
                }
                if (newNav != navActive)
                {
                    navActive = newNav;

                    needsRepaint = true;
                }
                if (newStrobe != strobeActive)
                {
                    strobeActive = newStrobe;

                    needsRepaint = true;
                }
                if (newRecogn != recognActive)
                {
                    recognActive = newRecogn;
                    needsRepaint = true;
                }
                if (newPanelLight != panelLightActive)
                {
                    panelLightActive = newPanelLight;
                    needsRepaint = true;
                }
                bool newDeice = (s1.read_deice & 1) != 0;
                if (newDeice != deiceActive)
                {
                    deiceActive = newDeice;
                    needsRepaint = true;
                }
                bool newPropDeice = (s1.read_propdeice & 1) != 0;
                if (newPropDeice != propDeiceActive)
                {
                    propDeiceActive = newPropDeice;
                    needsRepaint = true;
                }
                bool newPitotL = (s1.read_pitot1 & 1) != 0;
                if (newPitotL != pitotHeatLActive)
                {
                    pitotHeatLActive = newPitotL;
                    needsRepaint = true;
                }
                bool newPitotR = (s1.read_pitot2 & 1) != 0;
                if (newPitotR != pitotHeatRActive)
                {
                    pitotHeatRActive = newPitotR;
                    needsRepaint = true;
                }
                bool newAntiIce1 = (s1.read_antiice1 & 1) != 0;
                if (newAntiIce1 != antiIceEng1Active)
                {
                    antiIceEng1Active = newAntiIce1;
                    needsRepaint = true;
                }
                _isDeiceAirframe = s1.read_isDeiceAirframe;
                _isDeiceAirframe2 = s1.read_isDeiceAirframe2;
                bool newWindshieldDeice = (s1.read_windshielddeice & 1) != 0;
                if (newWindshieldDeice != windshieldDeiceActive)
                {
                    windshieldDeiceActive = newWindshieldDeice;
                    needsRepaint = true;
                }
                int newCowlL = s1.read_cowlflap_l < 10 ? 0 : s1.read_cowlflap_l < 70 ? 1 : 2;
                int newCowlR = s1.read_cowlflap_r < 10 ? 0 : s1.read_cowlflap_r < 70 ? 1 : 2;
                if (newCowlL != cowlFlapL || newCowlR != cowlFlapR)
                {
                    cowlFlapL = newCowlL;
                    cowlFlapR = newCowlR;
                    needsRepaint = true;
                }
                if (newPitotheat != pitotheatActive)
                {
                    pitotheatActive = newPitotheat;

                    needsRepaint = true;
                }
                if (newFuelpump1 != fuelpump1Active)
                {
                    fuelpump1Active = newFuelpump1;

                    needsRepaint = true;
                }
                if (newFuelpump2 != fuelpump2Active)
                {
                    fuelpump2Active = newFuelpump2;

                    needsRepaint = true;
                }
                if (newParkingbrake != pbrakeActive)
                {
                    pbrakeActive = newParkingbrake;

                    needsRepaint = true;
                }
                bool newMasterBatteryOff = s1.elec_master_battery == 0;
                if (newMasterBatteryOff != masterBatteryOff)
                {
                    masterBatteryOff = newMasterBatteryOff;
                    needsRepaint = true;
                }
                bool newAvionics1Off = s1.avionics_master_1 == 0;
                if (newAvionics1Off != avionics1Off)
                {
                    avionics1Off = newAvionics1Off;
                    needsRepaint = true;
                }
                bool newAvionics2Off = s1.avionics_master_2 == 0;
                if (newAvionics2Off != avionics2Off)
                {
                    avionics2Off = newAvionics2Off;
                    needsRepaint = true;
                }
                bool newAltGen1Off = s1.gen_master_alt_1 == 0;
                bool newAltGen2Off = s1.gen_master_alt_2 == 0;
                if (newAltGen1Off != altGen1Off)
                {
                    altGen1Off = newAltGen1Off;
                    needsRepaint = true;
                }
                if (newAltGen2Off != altGen2Off)
                {
                    altGen2Off = newAltGen2Off;
                    needsRepaint = true;
                }
                bool newStarter1 = s1.starter1 != 0;
                starter1Active = newStarter1;
                needsRepaint = true;
                bool newStarter2 = s1.starter2 != 0;
                starter2Active = newStarter2;
                int newIgn1Pos;
                if      (s1.ign1_left == 0 && s1.ign1_right == 0 && s1.ign1_starter == 0) newIgn1Pos = 0; // OFF
                else if (s1.ign1_left == 0 && s1.ign1_right == 1 && s1.ign1_starter == 0) newIgn1Pos = 1; // RIGHT
                else if (s1.ign1_left == 1 && s1.ign1_right == 0 && s1.ign1_starter == 0) newIgn1Pos = 2; // LEFT
                else if (s1.ign1_left == 1 && s1.ign1_right == 1 && s1.ign1_starter == 0) newIgn1Pos = 3; // BOTH
                else if (s1.ign1_left == 1 && s1.ign1_right == 1 && s1.ign1_starter == 1) newIgn1Pos = 4; // START
                else newIgn1Pos = ign1Pos; // mantenim l'últim valor conegut
                if (newIgn1Pos != ign1Pos) { ign1Pos = newIgn1Pos; needsRepaint = true; }
                int newIgn2Pos;
                if      (s1.ign2_left == 0 && s1.ign2_right == 0 && s1.ign2_starter == 0) newIgn2Pos = 0;
                else if (s1.ign2_left == 0 && s1.ign2_right == 1 && s1.ign2_starter == 0) newIgn2Pos = 1;
                else if (s1.ign2_left == 1 && s1.ign2_right == 0 && s1.ign2_starter == 0) newIgn2Pos = 2;
                else if (s1.ign2_left == 1 && s1.ign2_right == 1 && s1.ign2_starter == 0) newIgn2Pos = 3;
                else if (s1.ign2_left == 1 && s1.ign2_right == 1 && s1.ign2_starter == 1) newIgn2Pos = 4;
                else newIgn2Pos = ign2Pos;
                if (newIgn2Pos != ign2Pos) { ign2Pos = newIgn2Pos; needsRepaint = true; }
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

                if (newFuel1Pos != fuel1Pos) { fuel1Pos = newFuel1Pos; needsRepaint = true; }
                if (newFuel2Pos != fuel2Pos) { fuel2Pos = newFuel2Pos; needsRepaint = true; }
                //dmeActive = s1.dme_read == 1; // o la variable que ja estàs llegint

                //com1Tx = s1.com_transmit_1 == 1;
                //com2Tx = s1.com_transmit_2 == 1;
                com1Tx = (s1.com_transmit_1 & 1) != 0;
                com2Tx = (s1.com_transmit_2 & 1) != 0;
                //Debug.WriteLine($"COM TX raw: c1={s1.com_transmit_1} c2={s1.com_transmit_2} -> b1={com1Tx} b2={com2Tx}");

                displayBackupActive_bool = s1.displayBackupActive > 0.5;
                if (_elecSourceSwitch != s1.elecSourceSwitch) { _elecSourceSwitch = s1.elecSourceSwitch; needsRepaint = true; }
                if (_elecGeneratorSwitch != s1.elecGeneratorSwitch) { _elecGeneratorSwitch = s1.elecGeneratorSwitch; needsRepaint = true; }
                if (_elecCrashbar != s1.elecCrashbarState) { _elecCrashbar = s1.elecCrashbarState; needsRepaint = true; }
                if (_elecStarter != s1.elecStarter) { _elecStarter = s1.elecStarter; needsRepaint = true; }
                if (_elecIgnition != s1.elecIgnition) { _elecIgnition = s1.elecIgnition; needsRepaint = true; }
                if (_elecFuelPump != s1.elecFuelPump) { _elecFuelPump = s1.elecFuelPump; needsRepaint = true; }
                // _apTrim es gestiona localment, no es llegeix de MSFS
                _apDisengaged = s1.apDisengaged;
                _kap140State = s1.kap140State;
                _apVsHold = s1.apVsHold;
                _apFlcActive = s1.apFlcActive;
                _apWingLeveler = s1.apWingLeveler;
                _apHdgLock = s1.apHdgLock;
                _apAprHold = s1.apAprHold;
                _apGsHold = s1.apGsHold;
                _apNav1Lock = s1.apNav1Lock;
                _apFdActive = s1.apFdActive;
                _apAltLock = s1.apAltLock;
                _apVnavBtn = s1.apVnavBtn;
                _apYawDamper = s1.apYawDamper;
                _apMaxBank = s1.apMaxBank;
                _apBcHold = s1.apBcHold;
                _apMaster = s1.apMaster;
                _xfrPushL = s1.xfrPushL;
                _headingIndicator = s1.headingIndicator;
                _masterCautionActive = s1.masterCautionActive;
                _masterWarningActive = s1.masterWarningActive;
                _aileronTrimFrac = s1.aileronTrimFrac;
                // G3K fuel selector animation
                {
                    float fselTarget = s1.fuel_selector_1 == 2 ? 245f : (s1.fuel_selector_1 == 3 ? 113f : 0f);
                    if (_g3kFuelSelAngle < 0f)
                    {
                        _g3kFuelSelAngle = fselTarget;
                        _g3kFuelSelTargetAngle = fselTarget;
                    }
                    else if (fselTarget != _g3kFuelSelTargetAngle)
                    {
                        _g3kFuelSelTargetAngle = fselTarget;
                        _g3kFuelSelLastPaint = DateTime.MinValue;
                        needsRepaint = true;
                    }
                }
                _rudderTrimDisabled = s1.rudderTrimDisabled;
                _aileronTrimDisabled = s1.aileronTrimDisabled;
                if (_lightingLogo1 != s1.lightingLogo1) { _lightingLogo1 = s1.lightingLogo1; needsRepaint = true; }
                _lightingWing1 = s1.lightingWing1;
                _lightLogo = s1.lightLogo;
                _circuitConn17 = s1.circuitConn17;
                if (_g3000Active && pbMain.IsHandleCreated)
                {
                    pbMain.BeginInvoke(new Action(() =>
                    {
                        if (!_thrHandleLeftRail && _thrQuadrantVisible && !_thrHandleDragging && DateTime.UtcNow > _thrIgnoreFollowUntil)
                        {
                            if (!double.IsNaN(_lastThrLeverPos) && Math.Abs(s1.thrLeverPos - _lastThrLeverPos) > 200)
                            {
                                var taxiA5 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_TAXI");
                                var thrH5 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                                if (taxiA5 != null && thrH5 != null)
                                    _thrHandleLeftRailCenterY = taxiA5.BasePos.Y + taxiA5.Bounds.Height / 2 - thrH5.Bounds.Height / 2;
                                _thrHandleLeftRail = true;
                                _taxiPropActive = true;
                            }
                            _lastThrLeverPos = s1.thrLeverPos;
                        }
                        if (_thrQuadrantVisible && !_thrHandleDragging && DateTime.UtcNow > _thrIgnoreFollowUntil && _thrHandleLeftRail)
                        {
                            var thrH4 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                            if (thrH4 != null)
                            {
                                int read = (int)s1.thrLeverPos;
                                int centerY = _thrHandleLeftRailCenterY;
                                int handleY;
                                if (read >= 0)
                                    handleY = centerY - (int)((long)read * 375 / 16383);
                                else
                                    handleY = centerY - (int)((long)read * 250 / 2048);
                                handleY = Math.Max(centerY - 375, Math.Min(centerY + 250, handleY));
                                thrH4.BasePos = new Point(3694, handleY);
                                pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                            }
                        }
                        // Seguiment carril dret des de MSFS (fuel valve / prop+thr)
                        if (_thrQuadrantVisible && !_thrHandleDragging && DateTime.UtcNow > _thrIgnoreRightRailUntil) // anti-rebot
                        {
                            if (s1.fuelValve == 0)
                            {
                                if (_thrHandleLeftRail) { _thrHandleLeftRail = false; _lastThrLeverPos = double.NaN; }
                                MoveThrHandle(_thrHandleDefaultY + 150);
                            }
                            else if ((int)s1.propLeverPos == -4096 && (int)s1.thrLeverPos == 4915)
                            {
                                if (_thrHandleLeftRail) { _thrHandleLeftRail = false; _lastThrLeverPos = double.NaN; }
                                MoveThrHandle(_thrHandleDefaultY - 170);
                            }
                            else if ((int)s1.propLeverPos == -4096 && (int)s1.thrLeverPos == 0)
                            {
                                if (_thrHandleLeftRail) { _thrHandleLeftRail = false; _lastThrLeverPos = double.NaN; }
                                var liArea2 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_LOWIDLE");
                                var thrHli2 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                                if (liArea2 != null && thrHli2 != null)
                                    MoveThrHandle(liArea2.BasePos.Y + liArea2.Bounds.Height / 2 - thrHli2.Bounds.Height / 2);
                                else
                                    MoveThrHandle(_thrHandleDefaultY);
                            }
                        }
                    }));
                }
                // _fuelSelectionSet es gestiona localment, no es llegeix de MSFS
                if (_elecCrashbar < 0.5 && (_elecSourceSwitch > 0.5 || _elecGeneratorSwitch > 0.5))
                {
                    SetElecSourceSwitch(0);
                    SetElecGeneratorSwitch(0);
                }

                bool hasChanged = HasDataChanged(s1);

                if (hasChanged)
                {





                }

                if (needsRepaint && pbMain.IsHandleCreated && (DateTime.UtcNow - _lastRepaint).TotalMilliseconds > 200)
                {
                    _lastRepaint = DateTime.UtcNow;
                    pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
                }

                lastData = s1;

                previousNavSel = s1.nav_sel;
                firstNavSelReceived = true;             

            }
        }

        private void PbMain_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
            var activeBg = (_g3000Active && _bgG3000Image != null) ? _bgG3000Image : _bgImage;
            if (activeBg != null)
                e.Graphics.DrawImage(activeBg, pbMain.ClientRectangle);

            if (_g3000Active)
            {
                foreach (var ov in g3000OverlayImages)
                {
                    if (ov.Name == "G3K_FSEL_SW") continue;
                    if (ov.Name == "G3K_PBRAKE_SW") continue;
                    if (ov.Img != null && (calibrationMode || (ov.ShowWhen?.Invoke() ?? true)))
                        e.Graphics.DrawImage(ov.Img, ov.AnimatedBounds);
                }

                // G3K_PBRAKE_SW — manual draw amb rotació quan actiu
                {
                    var swG3kPbrake = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_PBRAKE_SW");
                    if (swG3kPbrake?.Img != null)
                    {
                        float baseW = swG3kPbrake.ZoomWidth;
                        float baseH = baseW / swG3kPbrake.AspectRatio;
                        float drawW = pbrakeActive ? baseW * 1.15f : baseW;
                        float drawH = pbrakeActive ? baseH * 1.15f : baseH;
                        float angle = pbrakeActive ? 30f : 0f;
                        float cx    = swG3kPbrake.BasePos.X + baseW / 2f;
                        float cy    = swG3kPbrake.BasePos.Y + baseH / 2f;
                        var state = e.Graphics.Save();
                        e.Graphics.TranslateTransform(cx, cy);
                        e.Graphics.RotateTransform(angle);
                        e.Graphics.DrawImage(swG3kPbrake.Img, -drawW / 2f, -drawH / 2f, drawW, drawH);
                        e.Graphics.Restore(state);
                    }
                }

                // G3K_FSEL_SW — selector combustible animat (rotació CCW)
                {
                    var sw = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_FSEL_SW");
                    if (sw?.Img != null && _g3kFuelSelAngle >= 0f)
                    {
                        var nowAnim = DateTime.UtcNow;
                        if (_g3kFuelSelAngle != _g3kFuelSelTargetAngle)
                        {
                            float elapsed = _g3kFuelSelLastPaint == DateTime.MinValue ? 0f
                                : Math.Min((float)(nowAnim - _g3kFuelSelLastPaint).TotalSeconds, 0.1f);
                            float step = 90f * elapsed;
                            if (_g3kFuelSelTargetAngle > _g3kFuelSelAngle)
                                _g3kFuelSelAngle = Math.Min(_g3kFuelSelAngle + step, _g3kFuelSelTargetAngle);
                            else
                                _g3kFuelSelAngle = Math.Max(_g3kFuelSelAngle - step, _g3kFuelSelTargetAngle);
                            if (_g3kFuelSelAngle != _g3kFuelSelTargetAngle)
                                this.BeginInvoke(new Action(() => pbMain.Invalidate()));
                        }
                        _g3kFuelSelLastPaint = nowAnim;

                        float baseW = sw.Img.Width;
                        float baseH = sw.Img.Height;
                        float cx = sw.BasePos.X + baseW / 2f;
                        float cy = sw.BasePos.Y + baseH / 2f;
                        var state = e.Graphics.Save();
                        e.Graphics.TranslateTransform(cx, cy);
                        e.Graphics.RotateTransform(-_g3kFuelSelAngle);
                        e.Graphics.DrawImage(sw.Img, -baseW / 2f, -baseH / 2f, baseW, baseH);
                        e.Graphics.Restore(state);
                    }
                }

                var now = DateTime.UtcNow;
                using var pressBrush = new SolidBrush(Color.FromArgb(90, Color.White));
                foreach (var ov in g3000OverlayImages)
                {
                    if (!ov.IsVisuallyPressed(now)) continue;
                    var r = ov.Bounds;
                    var inner = new Rectangle(r.X + 4, r.Y + 4, r.Width - 8, r.Height - 8);
                    if (ov.IsRound)
                        e.Graphics.FillEllipse(pressBrush, inner);
                    else
                    {
                        using var path = CreateRoundedRect(inner, BTN_RADIUS);
                        e.Graphics.FillPath(pressBrush, path);
                    }
                }

                // AP feedback indicators
                {
                    var hdgOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_HDG_HOLD");
                    if (hdgOv != null)
                    {
                        var r = hdgOv.Bounds;
                        int indX = r.Right + 5;
                        int indY = r.Y + 5;
                        int indH = 53;
                        if (_apHdgLock == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // APR feedback
                {
                    var aprOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_APR_HOLD");
                    if (aprOv != null)
                    {
                        var r = aprOv.Bounds;
                        int indX = r.Right + 5;
                        int indY = r.Y + 5;
                        int indH = 53;
                        if (_apAprHold == 1 && _apGsHold == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // BC feedback
                {
                    var bcOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_BC");
                    if (bcOv != null)
                    {
                        var r = bcOv.Bounds;
                        int indX = r.Right + 0; int indY = r.Y + 5; int indH = 53;
                        if (_apBcHold == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // BANK HOLD feedback
                {
                    var bankOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_BANK_INC");
                    if (bankOv != null)
                    {
                        var r = bankOv.Bounds;
                        int indX = r.Right + 5; int indY = r.Y + 5; int indH = 53;
                        if (_apMaxBank < 0.5)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // YAW DAMPER feedback
                {
                    var ydOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_YAW_DMPR");
                    if (ydOv != null)
                    {
                        var r = ydOv.Bounds;
                        int indX = r.Right + 0; int indY = r.Y + 5; int indH = 53;
                        if (_apYawDamper == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // AP MASTER feedback
                {
                    var apOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_MASTER");
                    if (apOv != null)
                    {
                        var r = apOv.Bounds;
                        int indX = r.Right + 2; int indY = r.Y + 2; int indH = 53;
                        if (_apMaster == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // LVL indicator (green transparent zone)
                if (_apWingLeveler == 1)
                {
                    using var lvlBr = new SolidBrush(Color.FromArgb(80, 0, 220, 80));
                    e.Graphics.FillRectangle(lvlBr, _apLvlIndRect);
                }
                if (calibrationMode)
                {
                    using var lvlPen = new Pen(Color.Lime, 2);
                    using var lvlFont = new Font("Arial", 9);
                    using var handleBr = new SolidBrush(Color.FromArgb(200, 255, 165, 0));
                    e.Graphics.DrawRectangle(lvlPen, _apLvlIndRect);
                    e.Graphics.DrawString("LVL", lvlFont, Brushes.Lime, new PointF(_apLvlIndRect.X, _apLvlIndRect.Y - 14));
                    var resH = new Rectangle(_apLvlIndRect.Right - 8, _apLvlIndRect.Bottom - 8, 8, 8);
                    e.Graphics.FillRectangle(handleBr, resH);
                }

                // VNV feedback
                {
                    var vnvOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_VNV");
                    if (vnvOv != null)
                    {
                        var r = vnvOv.Bounds;
                        int indX = r.Right; int indY = r.Y + 5; int indH = 53;
                        if (_apVnavBtn == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // VS / SPD / FLC feedback
                foreach (var (ovName, isOn, xOff) in new (string, bool, int)[] {
                    ("G3K_AP_VS_HOLD",  _apVsHold   == 1, 5),
                    ("G3K_AP_FLC",      _apFlcActive == 1, 0) })
                {
                    var ov = g3000OverlayImages.FirstOrDefault(o => o.Name == ovName);
                    if (ov == null) continue;
                    var r = ov.Bounds;
                    int indX = r.Right + xOff; int indY = r.Y + 5; int indH = 53;
                    if (isOn)
                    {
                        using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                        e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                        using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                        e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                    }
                    else
                    {
                        using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                        e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                        e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                    }
                }

                // ALT feedback
                {
                    var altOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_ALT");
                    if (altOv != null)
                    {
                        var r = altOv.Bounds;
                        int indX = r.Right + 5;
                        int indY = r.Y + 5;
                        int indH = 53;
                        if (_apAltLock == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // FD feedback
                {
                    var fdOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_FD");
                    if (fdOv != null)
                    {
                        var r = fdOv.Bounds;
                        int indX = r.Right + 5;
                        int indY = r.Y + 5;
                        int indH = 53;
                        if (_apFdActive == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // NAV feedback
                {
                    var navOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_AP_NAV1_HOLD");
                    if (navOv != null)
                    {
                        var r = navOv.Bounds;
                        int indX = r.Right + 5;
                        int indY = r.Y + 5;
                        int indH = 53;
                        if (_apNav1Lock == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillRectangle(glowBr, indX - 3, indY, 13, indH);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillRectangle(coreBr, indX, indY, 7, indH);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawRectangle(borderPen, indX, indY, 6, indH - 1);
                        }
                    }
                }

                // XFR feedback — triangle esquerra ◀ (encès quan L:XMLVAR_PushXFR == 0)
                {
                    var xfrOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_XFR");
                    if (xfrOv != null)
                    {
                        var r = xfrOv.Bounds;
                        int side = 26; int w = 22; int indY = r.Y + r.Height / 2 - side / 2;
                        int baseX = r.X - 5;
                        Point[] tri     = { new(baseX,     indY),     new(baseX,     indY + side),     new(baseX - w,     indY + side / 2) };
                        Point[] triGlow = { new(baseX + 3, indY - 3), new(baseX + 3, indY + side + 3), new(baseX - w - 3, indY + side / 2) };
                        if (_xfrPushL == 0)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillPolygon(glowBr, triGlow);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillPolygon(coreBr, tri);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillPolygon(coreBr, tri);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawPolygon(borderPen, tri);
                        }
                    }
                }

                // XFR feedback — triangle dreta ▶ (encès quan L:XMLVAR_PushXFR == 1)
                {
                    var xfrOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_XFR");
                    if (xfrOv != null)
                    {
                        var r = xfrOv.Bounds;
                        int side = 26; int w = 22; int indY = r.Y + r.Height / 2 - side / 2;
                        int baseX = r.Right + 5;
                        Point[] tri     = { new(baseX,     indY),     new(baseX,     indY + side),     new(baseX + w,     indY + side / 2) };
                        Point[] triGlow = { new(baseX - 3, indY - 3), new(baseX - 3, indY + side + 3), new(baseX + w + 3, indY + side / 2) };
                        if (_xfrPushL == 1)
                        {
                            using var glowBr = new SolidBrush(Color.FromArgb(80, 255, 255, 200));
                            e.Graphics.FillPolygon(glowBr, triGlow);
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 255, 255, 220));
                            e.Graphics.FillPolygon(coreBr, tri);
                        }
                        else
                        {
                            using var coreBr = new SolidBrush(Color.FromArgb(255, 35, 35, 30));
                            e.Graphics.FillPolygon(coreBr, tri);
                            using var borderPen = new Pen(Color.FromArgb(160, 100, 100, 90), 1);
                            e.Graphics.DrawPolygon(borderPen, tri);
                        }
                    }
                }

                // APZ wheel procedural animation
                {
                    const int stripeH = 10;
                    const int period  = stripeH * 2;
                    int startOff = ((int)_g3kApWheelOffset % period + period) % period;
                    int sy = g3kApSliderZone.Y - startOff;
                    var gs = e.Graphics.Save();
                    e.Graphics.SetClip(g3kApSliderZone);
                    using var darkBr  = new SolidBrush(Color.FromArgb(20, 20, 20));
                    using var lightBr = new SolidBrush(Color.FromArgb(65, 65, 65));
                    for (int y = sy, i = 0; y < g3kApSliderZone.Bottom; y += stripeH, i++)
                        e.Graphics.FillRectangle(i % 2 == 0 ? darkBr : lightBr,
                            g3kApSliderZone.X, y, g3kApSliderZone.Width, stripeH);
                    e.Graphics.Restore(gs);
                }

                // G3K_FLAPS_SW — AnimOffsetY quan es prem UP/DN
                {
                    var swG3kFlaps = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_FLAPS_SW");
                    if (swG3kFlaps != null) swG3kFlaps.AnimOffsetY = _g3kFlapsAnimDir != 0 ? _g3kFlapsAnimDir * 30 : 0;
                }
                // G3K_LGEAR_SW — AnimOffsetY sincronitzat amb MSFS gear handle
                {
                    var swG3kLgear = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_LGEAR_SW");
                    if (swG3kLgear != null) swG3kLgear.AnimOffsetY = lgearHandleUp ? -50 : 50;
                }

                // G3K Trim wheel (fotogrames rotats 90°)
                {
                    int frameIdx = (int)trimFramePos % TRIM_FRAME_COUNT;
                    var frameImg = trimFrames[frameIdx];
                    if (frameImg != null)
                    {
                        int th = (int)(_g3kTrimZone.Height * 0.95f);
                        int tw = (int)(th * (double)frameImg.Width / frameImg.Height);
                        int tx = _g3kTrimZone.X + (_g3kTrimZone.Width - tw) / 2;
                        int ty = _g3kTrimZone.Y + (_g3kTrimZone.Height - th) / 2;
                        e.Graphics.DrawImage(frameImg, tx, ty, tw, th);
                    }
                }
                // G3K Trim indicator + takeoff marker
                {
                    int g3kTrimYUp   = _g3kTrimZone.Y;
                    int g3kTrimYFull = _g3kTrimZone.Bottom;
                    float g3kTrimNorm = (float)((elevatorTrim + 1.0) / 2.0);
                    int g3kTrimY  = g3kTrimYUp + (int)(g3kTrimNorm * (g3kTrimYFull - g3kTrimYUp)) + _g3kTrimIndYOffset;
                    int g3kMarkerY = g3kTrimYUp + (int)(0.5f * (g3kTrimYFull - g3kTrimYUp)) + _g3kTrimIndYOffset;
                    using var g3kMarkerBr = new SolidBrush(Color.FromArgb(180, 180, 180));
                    e.Graphics.FillRectangle(g3kMarkerBr, new Rectangle(_g3kTrimIndX - 27, g3kMarkerY, 17, 2));
                    e.Graphics.FillEllipse(Brushes.White, new Rectangle(_g3kTrimIndX, g3kTrimY - 8, 17, 17));
                    if (calibrationMode)
                    {
                        using var triPen  = new Pen(Color.Lime, 2);
                        using var triFont = new Font("Arial", 7);
                        using var g3kOvBr = new SolidBrush(Color.FromArgb(60, Color.Gray));
                        e.Graphics.DrawRectangle(triPen, new Rectangle(_g3kTrimIndX - 2, g3kTrimYUp + _g3kTrimIndYOffset - 2, 21, g3kTrimYFull - g3kTrimYUp + 21));
                        e.Graphics.DrawString("G3KTRI", triFont, Brushes.Lime, new PointF(_g3kTrimIndX, g3kTrimYUp + _g3kTrimIndYOffset - 16));
                        e.Graphics.DrawRectangle(triPen, new Rectangle(_g3kTrimZone.X - 2, _g3kTrimZone.Y - 2, _g3kTrimZone.Width + 4, _g3kTrimZone.Height + 4));
                        e.Graphics.DrawString("G3KTRZ", triFont, Brushes.Lime, new PointF(_g3kTrimZone.X, _g3kTrimZone.Y - 16));
                        e.Graphics.FillRectangle(g3kOvBr, _g3kTrimZone);
                        // resize handle (bottom-right)
                        e.Graphics.FillRectangle(Brushes.Yellow, new Rectangle(_g3kTrimZone.Right - 7, _g3kTrimZone.Bottom - 7, 14, 14));
                    }
                }

                // G3K Rudder Trim wheel (horitzontal, frames originals sense rotar)
                {
                    int frameIdx = (int)rudderTrimFramePos % RUDDER_TRIM_FRAME_COUNT;
                    var frameImg = rudderTrimFrames[frameIdx];
                    if (frameImg != null)
                    {
                        int tw = (int)(_g3kRudderTrimZone.Width * 0.95f);
                        int th = (int)(tw * (double)frameImg.Height / frameImg.Width);
                        int tx = _g3kRudderTrimZone.X + (_g3kRudderTrimZone.Width - tw) / 2;
                        int ty = _g3kRudderTrimZone.Y + (_g3kRudderTrimZone.Height - th) / 2;
                        e.Graphics.DrawImage(frameImg, tx, ty, tw, th);
                    }
                }
                // G3K Rudder Trim indicator + centre marker
                {
                    int rtXLeft  = _g3kRudderTrimZone.X;
                    int rtXRight = _g3kRudderTrimZone.Right;
                    float rtNorm = (float)((rudderTrim + 1.0) / 2.0);
                    int rtX = rtXLeft + (int)(rtNorm * (rtXRight - rtXLeft)) + _g3kRudderTrimIndXOffset;
                    int rtCenter = (rtXLeft + rtXRight) / 2 + _g3kRudderTrimIndXOffset;
                    using var g3kRtCenterBr = new SolidBrush(Color.FromArgb(180, 180, 180));
                    e.Graphics.FillRectangle(g3kRtCenterBr, new Rectangle(rtCenter - 1, _g3kRudderTrimIndY - 36, 2, 17));
                    e.Graphics.FillEllipse(Brushes.White, new Rectangle(rtX - 8, _g3kRudderTrimIndY - 8, 17, 17));
                    if (calibrationMode)
                    {
                        using var rriPen  = new Pen(Color.Lime, 2);
                        using var rriFont = new Font("Arial", 7);
                        using var g3kRrOvBr = new SolidBrush(Color.FromArgb(60, Color.Gray));
                        int trkW = rtXRight - rtXLeft + 17;
                        e.Graphics.DrawRectangle(rriPen, new Rectangle(rtXLeft + _g3kRudderTrimIndXOffset - 2, _g3kRudderTrimIndY - 10, trkW + 4, 20));
                        e.Graphics.DrawString("G3KRRI", rriFont, Brushes.Lime, new PointF(rtXLeft + _g3kRudderTrimIndXOffset, _g3kRudderTrimIndY - 24));
                        e.Graphics.DrawRectangle(rriPen, new Rectangle(_g3kRudderTrimZone.X - 2, _g3kRudderTrimZone.Y - 2, _g3kRudderTrimZone.Width + 4, _g3kRudderTrimZone.Height + 4));
                        e.Graphics.DrawString("G3KRRZ", rriFont, Brushes.Lime, new PointF(_g3kRudderTrimZone.X, _g3kRudderTrimZone.Y - 16));
                        e.Graphics.FillRectangle(g3kRrOvBr, _g3kRudderTrimZone);
                        // resize handle (bottom-right)
                        e.Graphics.FillRectangle(Brushes.Yellow, new Rectangle(_g3kRudderTrimZone.Right - 7, _g3kRudderTrimZone.Bottom - 7, 14, 14));
                    }
                }

                // G3K FLI — marcadors + cercle animat (escala 11.75/14 respecte G1000)
                if (flapsNumPositions > 0)
                {
                    const int G3K_FLI_RANGE = 193; // 230 × 11.75/14
                    const int G3K_FLI_DIAM  = 17;
                    const int G3K_FLI_BASE_Y = 450;
                    int yUp   = G3K_FLI_BASE_Y + _g3kFlapsIndYOffset;
                    int yFull = yUp + G3K_FLI_RANGE;
                    int g3kFmX = _g3kFlapsIndX - 21;

                    // Marcadors UP i FULL
                    e.Graphics.FillRectangle(Brushes.White, new Rectangle(g3kFmX, yUp,   17, 4));
                    e.Graphics.FillRectangle(Brushes.White, new Rectangle(g3kFmX, yFull, 17, 4));

                    // Marcadors intermedis
                    int spaces = flapsNumPositions;
                    float stepY = G3K_FLI_RANGE / (float)spaces;
                    for (int i = 1; i < spaces; i++)
                        e.Graphics.FillRectangle(Brushes.White, new Rectangle(g3kFmX, (int)(yUp + stepY * i), 17, 4));

                    // Cercle animat
                    float stepYInd = G3K_FLI_RANGE / (float)flapsNumPositions;
                    _g3kFlapsCircleY_Target = yUp + stepYInd * flapsPoscPalanca;

                    if (flapsPoscPalanca != _g3kFlapsPoscLast)
                    {
                        if (_g3kFlapsPoscLast < 0)
                        {
                            _g3kFlapsCircleY_Current = _g3kFlapsCircleY_Target;
                            _g3kFlapsAnimDurationSec = 0f;
                        }
                        else
                        {
                            _g3kFlapsCircleY_Start   = _g3kFlapsCircleY_Current;
                            _g3kFlapsAnimStartTime   = DateTime.Now;
                            float dist = Math.Abs(_g3kFlapsCircleY_Target - _g3kFlapsCircleY_Current);
                            _g3kFlapsAnimDurationSec = 8f * (dist / G3K_FLI_RANGE);
                        }
                        _g3kFlapsPoscLast = flapsPoscPalanca;
                    }

                    if (_g3kFlapsAnimDurationSec > 0f)
                    {
                        float t = Math.Min((float)(DateTime.Now - _g3kFlapsAnimStartTime).TotalSeconds / _g3kFlapsAnimDurationSec, 1f);
                        _g3kFlapsCircleY_Current = _g3kFlapsCircleY_Start + (_g3kFlapsCircleY_Target - _g3kFlapsCircleY_Start) * t;
                        if (t >= 1f) _g3kFlapsAnimDurationSec = 0f;
                    }
                    else
                        _g3kFlapsCircleY_Current = _g3kFlapsCircleY_Target;

                    int drawY = (int)Math.Round(_g3kFlapsCircleY_Current) - G3K_FLI_DIAM / 2;
                    e.Graphics.FillEllipse(Brushes.White, new Rectangle(_g3kFlapsIndX, drawY, G3K_FLI_DIAM, G3K_FLI_DIAM));

                    if (_g3kFlapsAnimDurationSec > 0f)
                        this.BeginInvoke(new Action(() => pbMain.Invalidate()));

                    if (calibrationMode)
                    {
                        using var fliPen  = new Pen(Color.Lime, 2);
                        using var fliFont = new Font("Arial", 7);
                        e.Graphics.DrawRectangle(fliPen, new Rectangle(_g3kFlapsIndX - 2, yUp - 2, G3K_FLI_DIAM + 4, G3K_FLI_RANGE + G3K_FLI_DIAM + 4));
                        e.Graphics.DrawString("FLI", fliFont, Brushes.Lime, new PointF(_g3kFlapsIndX, yUp - 16));
                    }
                }

                if (calibrationMode)
                {
                    using var ovPen = new Pen(Color.Cyan, 2);
                    using var selPen = new Pen(Color.Yellow, 2);
                    using var handleBr = new SolidBrush(Color.FromArgb(200, 255, 165, 0));
                    using var smallFont = new Font("Segoe UI", 9f);
                    foreach (var ov in g3000OverlayImages)
                    {
                        var pen = (ov == calibSelectedOverlay) ? selPen : ovPen;
                        if (ov.IsRound)
                        {
                            using var roundFill = new SolidBrush(Color.FromArgb(50, 0, 220, 220));
                            e.Graphics.FillEllipse(roundFill, ov.Bounds);
                            e.Graphics.DrawEllipse(pen, ov.Bounds);
                        }
                        else
                            e.Graphics.DrawRectangle(pen, ov.Bounds);
                        if (!ov.NoResize) e.Graphics.FillRectangle(handleBr, ov.ResizeHandle);
                        e.Graphics.DrawString(ov.Name, smallFont, Brushes.Cyan, new PointF(ov.BasePos.X, ov.BasePos.Y - 14));
                    }
                    // APZ — G3K AP slider zone (input=taronja gran, visual=blanc petit)
                    using var apzOrangePen = new Pen(Color.Orange, 2);
                    using var apzWhitePen  = new Pen(Color.White, 1);
                    using var apzFont      = new Font("Arial", 9);
                    e.Graphics.DrawRectangle(apzOrangePen, new Rectangle(g3kApSliderInputZone.X - 2, g3kApSliderInputZone.Y - 2, g3kApSliderInputZone.Width + 4, g3kApSliderInputZone.Height + 4));
                    e.Graphics.DrawString("APZ", apzFont, Brushes.Orange, new PointF(g3kApSliderInputZone.X, g3kApSliderInputZone.Y - 16));
                    e.Graphics.DrawRectangle(apzWhitePen, g3kApSliderZone);
                }

                if (mainSurface.ShowAllAreas)
                {
                    using var overlayBrush = new SolidBrush(Color.FromArgb(60, Color.Orange));
                    foreach (var ov in g3000OverlayImages)
                    {
                        if (ov.OnClick == null) continue;
                        if (ov.IsRound)
                            e.Graphics.FillEllipse(overlayBrush, ov.Bounds);
                        else
                            e.Graphics.FillRectangle(overlayBrush, ov.Bounds);
                    }
                    e.Graphics.FillRectangle(overlayBrush, g3kApSliderInputZone);

                    // Zones arrossegables (throttle, trim vertical, trim de rudder)
                    var thrHandleOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                    if (thrHandleOv != null && _thrQuadrantVisible)
                        e.Graphics.FillRectangle(overlayBrush, thrHandleOv.Bounds);
                    e.Graphics.FillRectangle(overlayBrush, _g3kTrimZone);
                    e.Graphics.FillRectangle(overlayBrush, _g3kRudderTrimZone);
                }

                return;
            }

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
            using var trimMarkerBr = new SolidBrush(Color.FromArgb(180, 180, 180));
            e.Graphics.FillRectangle(trimMarkerBr, new Rectangle(trimIndX - 32, 1758 + trimIndYOffset, 20, 2));

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
                    using var fliPen  = new Pen(Color.Lime, 2);
                    using var fliFont = new Font("Arial", 7);
                    e.Graphics.DrawRectangle(fliPen, new Rectangle(flapsIndX - 2, trackTop - 2, diameter + 4, trackH + 4));
                    e.Graphics.DrawString("FLI", fliFont, Brushes.Lime, new PointF(flapsIndX, trackTop - 16));
                }
            }

            if (flapsAnimDurationSec > 0f)
                pbMain.Invalidate();

            // TRIM WHEEL IMAGE VERTICAL — segueix trimZone
            {
                int frameIdx = (int)trimFramePos % RUDDER_TRIM_FRAME_COUNT;
                var frameImg = trimFrames[frameIdx];
                if (frameImg != null)
                {
                    int th = (int)(trimZone.Height * 0.95f);
                    int tw = (int)(th * (double)frameImg.Width / frameImg.Height);
                    int tx = trimZone.X + (trimZone.Width - tw) / 2;
                    int ty = trimZone.Y + (trimZone.Height - th) / 2;
                    e.Graphics.DrawImage(frameImg, tx, ty, tw, th);
                }
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
                using var triPen  = new Pen(Color.Lime, 2);
                using var triFont = new Font("Arial", 7);
                e.Graphics.DrawRectangle(triPen, new Rectangle(trimIndX - 2, trkTop - 2, 24, trkH + 4));
                e.Graphics.DrawString("TRI", triFont, Brushes.Lime, new PointF(trimIndX, trkTop - 16));
                // TRZ — zona de drag del trim
                e.Graphics.DrawRectangle(triPen, new Rectangle(trimZone.X - 2, trimZone.Y - 2, trimZone.Width + 4, trimZone.Height + 4));
                e.Graphics.DrawString("TRZ", triFont, Brushes.Lime, new PointF(trimZone.X, trimZone.Y - 16));
                // APZ — G3K AP slider zone
                if (_g3000Active)
                {
                    using var apzVisPen  = new Pen(Color.Cyan, 2);
                    e.Graphics.DrawRectangle(apzVisPen, new Rectangle(g3kApSliderZone.X - 2, g3kApSliderZone.Y - 2, g3kApSliderZone.Width + 4, g3kApSliderZone.Height + 4));
                    e.Graphics.DrawString("APZ", triFont, Brushes.Cyan, new PointF(g3kApSliderZone.X, g3kApSliderZone.Y - 16));
                }
            }

            // TRIM WHEEL IMAGE — segueix rudderTrimZone
            {
                int frameIdx = (int)rudderTrimFramePos % RUDDER_TRIM_FRAME_COUNT;
                var frameImg = rudderTrimFrames[frameIdx];
                if (frameImg != null)
                {
                    int tw = (int)(rudderTrimZone.Width * 0.95f);
                    int th = (int)(tw * (double)frameImg.Height / frameImg.Width);
                    int tx = rudderTrimZone.X + (rudderTrimZone.Width - tw) / 2;
                    int ty = rudderTrimZone.Y + (rudderTrimZone.Height - th) / 2;
                    e.Graphics.DrawImage(frameImg, tx, ty, tw, th);
                }
            }



            // RUDDER TRIM INDICATOR — track horitzontal
            {
                int rtXLeft  = 2760;
                int rtXRight = 3210;
                float rtNorm = (float)((rudderTrim + 1.0) / 2.0);
                int rtX = rtXLeft + (int)(rtNorm * (rtXRight - rtXLeft)) + rudderTrimIndXOffset;
                // marcador centre (línea vertical)
                int rtCenter = (rtXLeft + rtXRight) / 2 + rudderTrimIndXOffset;
                using var rtCenterBr = new SolidBrush(Color.FromArgb(180, 180, 180));
                e.Graphics.FillRectangle(rtCenterBr, new Rectangle(rtCenter - 1, rudderTrimIndY - 42, 2, 20));
                // cercle indicador
                e.Graphics.FillEllipse(Brushes.White, new Rectangle(rtX - 10, rudderTrimIndY - 10, 20, 20));
                if (calibrationMode)
                {
                    int trkLeft = rtXLeft + rudderTrimIndXOffset;
                    int trkW    = rtXRight - rtXLeft + 20;
                    using var rriPen  = new Pen(Color.Lime, 2);
                    using var rriFont = new Font("Arial", 7);
                    e.Graphics.DrawRectangle(rriPen, new Rectangle(trkLeft - 2, rudderTrimIndY - 12, trkW + 4, 24));
                    e.Graphics.DrawString("RRI", rriFont, Brushes.Lime, new PointF(trkLeft, rudderTrimIndY - 28));
                    // RRZ — zona de drag
                    e.Graphics.DrawRectangle(rriPen, new Rectangle(rudderTrimZone.X - 2, rudderTrimZone.Y - 2, rudderTrimZone.Width + 4, rudderTrimZone.Height + 4));
                    e.Graphics.DrawString("RRZ", rriFont, Brushes.Lime, new PointF(rudderTrimZone.X, rudderTrimZone.Y - 16));
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
                using var overlayBrush = new SolidBrush(Color.FromArgb(60, Color.Orange));

                // OverlayImages amb OnClick (G1000 — el G3000 ja ha fet return abans d'arribar aquí)
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
                using var centerFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

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

                using var exportBtnBrush = new SolidBrush(Color.DarkGreen);
                e.Graphics.FillRectangle(exportBtnBrush, calibExportBtnRect);
                e.Graphics.DrawString("EXPORTA\ncoordenades", font, textBrush, calibExportBtnRect, centerFmt);
                e.Graphics.DrawString("CALIBRATGE ACTIU — F12 per sortir", font, Brushes.Red, new Point(200, 15));
            }

            }
            catch (OverflowException) { }
        }

        private void PbMain_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_g3000Active && !calibrationMode)
            {
                var thrHandle = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                if (thrHandle != null && _thrQuadrantVisible && _thrHandleLeftRail && thrHandle.Bounds.Contains(e.Location))
                {
                    if (_thrHandleDefaultY < 0) _thrHandleDefaultY = thrHandle.BasePos.Y;
                    _thrHandleDragging = true;
                    _thrHandleDragOffsetY = e.Y - thrHandle.BasePos.Y;
                    return;
                }
                foreach (var ov in g3000OverlayImages)
                {
                    if (ov.OnClick == null || !ov.Bounds.Contains(e.Location)) continue;
                    bool isThrZone = ov.Name.StartsWith("G3K_THR_");
                    if (isThrZone && !_thrQuadrantVisible) continue;
                    ov.PressedUntilUtc = DateTime.UtcNow.AddMilliseconds(ov.PressVisualMs);
                    ov.OnClick.Invoke();
                    pbMain.Invalidate();
                    return;
                }
                if (g3kApSliderZone.Contains(e.Location))
                {
                    g3kApSliderDragging = true;
                    g3kApSliderLastMouseY = e.Y;
                }
                if (_g3kTrimZone.Contains(e.Location))
                {
                    _g3kTrimDragging = true;
                    _g3kTrimLastMouseY = e.Y;
                }
                if (_g3kRudderTrimZone.Contains(e.Location))
                {
                    _g3kRudderTrimDragging = true;
                    _g3kRudderTrimLastMouseX = e.X;
                }
                return;
            }

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
                // G3K AP slider zone drag (APZ)
                if (_g3000Active)
                {
                    // Zona visual (blanc, petita) — prioritat alta
                    var vizRect = new Rectangle(g3kApSliderZone.X - 2, g3kApSliderZone.Y - 2, g3kApSliderZone.Width + 4, g3kApSliderZone.Height + 4);
                    if (vizRect.Contains(e.Location))
                    {
                        calibDragG3kApSlider      = true;
                        calibG3kApSliderDragStart = e.Location;
                        calibG3kApSliderStartX    = g3kApSliderBaseX;
                        calibG3kApSliderStartY    = g3kApSliderBaseY;
                        pbMain.Invalidate(); return;
                    }
                    // Zona input (taronja, gran)
                    var apzRect = new Rectangle(g3kApSliderInputZone.X - 2, g3kApSliderInputZone.Y - 2, g3kApSliderInputZone.Width + 4, g3kApSliderInputZone.Height + 4);
                    if (apzRect.Contains(e.Location))
                    {
                        calibDragG3kApSliderInput      = true;
                        calibG3kApSliderInputDragStart = e.Location;
                        calibG3kApSliderInputStartX    = g3kApSliderInputBaseX;
                        calibG3kApSliderInputStartY    = g3kApSliderInputBaseY;
                        pbMain.Invalidate(); return;
                    }
                    // LVL indicator drag/resize
                    var lvlResH = new Rectangle(_apLvlIndRect.Right - 8, _apLvlIndRect.Bottom - 8, 8, 8);
                    if (lvlResH.Contains(e.Location))
                    {
                        _calibResizeApLvlInd      = true;
                        _calibApLvlIndDragStart   = e.Location;
                        _calibApLvlIndStartW      = _apLvlIndW;
                        _calibApLvlIndStartH      = _apLvlIndH;
                        pbMain.Invalidate(); return;
                    }
                    if (_apLvlIndRect.Contains(e.Location))
                    {
                        _calibDragApLvlInd        = true;
                        _calibApLvlIndDragStart   = e.Location;
                        _calibApLvlIndStartX      = _apLvlIndBaseX;
                        _calibApLvlIndStartY      = _apLvlIndBaseY;
                        pbMain.Invalidate(); return;
                    }
                    // G3K FLI drag
                    {
                        const int G3K_FLI_RANGE = 193; const int G3K_FLI_DIAM = 17;
                        int yUp = 450 + _g3kFlapsIndYOffset;
                        var fliRect = new Rectangle(_g3kFlapsIndX - 2, yUp - 2, G3K_FLI_DIAM + 4, G3K_FLI_RANGE + G3K_FLI_DIAM + 4);
                        if (fliRect.Contains(e.Location))
                        {
                            _calibDragG3kFlapsInd       = true;
                            _calibG3kFlapsIndDragStart  = e.Location;
                            _calibG3kFlapsIndStartX     = _g3kFlapsIndX;
                            _calibG3kFlapsIndStartYOff  = _g3kFlapsIndYOffset;
                            pbMain.Invalidate(); return;
                        }
                    }
                    // G3K Trim indicator drag (G3KTRI)
                    {
                        var r = new Rectangle(_g3kTrimIndX - 2, _g3kTrimZone.Y + _g3kTrimIndYOffset - 2, 21, _g3kTrimZone.Height + 21);
                        if (r.Contains(e.Location))
                        {
                            _calibDragG3kTrimInd = true;
                            _calibG3kTrimIndDragStart = e.Location;
                            _calibG3kTrimIndStartX = _g3kTrimIndX;
                            _calibG3kTrimIndStartYOff = _g3kTrimIndYOffset;
                            pbMain.Invalidate(); return;
                        }
                    }
                    // G3K Trim zone resize handle (bottom-right, ha d'anar ABANS del drag)
                    {
                        var rh = new Rectangle(_g3kTrimZone.Right - 7, _g3kTrimZone.Bottom - 7, 14, 14);
                        if (rh.Contains(e.Location))
                        {
                            _calibResizeG3kTrimZone = true;
                            _calibResizeG3kTrimZoneDragStart = e.Location;
                            _calibResizeG3kTrimZoneStartH = _g3kTrimZone.Height;
                            pbMain.Invalidate(); return;
                        }
                    }
                    // G3K Trim zone drag (G3KTRZ)
                    {
                        var r = new Rectangle(_g3kTrimZone.X - 2, _g3kTrimZone.Y - 2, _g3kTrimZone.Width + 4, _g3kTrimZone.Height + 4);
                        if (r.Contains(e.Location))
                        {
                            _calibDragG3kTrimZone = true;
                            _calibG3kTrimZoneDragStart = e.Location;
                            _calibG3kTrimZoneStartX = _g3kTrimZoneBaseX;
                            _calibG3kTrimZoneStartY = _g3kTrimZoneBaseY;
                            pbMain.Invalidate(); return;
                        }
                    }
                    // G3K Rudder Trim indicator drag (G3KRRI)
                    {
                        int trkW = _g3kRudderTrimZone.Width + 17;
                        var r = new Rectangle(_g3kRudderTrimZone.X + _g3kRudderTrimIndXOffset - 2, _g3kRudderTrimIndY - 10, trkW + 4, 20);
                        if (r.Contains(e.Location))
                        {
                            _calibDragG3kRudderTrimInd = true;
                            _calibG3kRudderTrimIndDragStart = e.Location;
                            _calibG3kRudderTrimIndStartY = _g3kRudderTrimIndY;
                            _calibG3kRudderTrimIndStartXOff = _g3kRudderTrimIndXOffset;
                            pbMain.Invalidate(); return;
                        }
                    }
                    // G3K Rudder Trim zone resize handle (ABANS del drag)
                    {
                        var rh = new Rectangle(_g3kRudderTrimZone.Right - 7, _g3kRudderTrimZone.Bottom - 7, 14, 14);
                        if (rh.Contains(e.Location))
                        {
                            _calibResizeG3kRudderTrimZone = true;
                            _calibResizeG3kRudderTrimZoneDragStart = e.Location;
                            _calibResizeG3kRudderTrimZoneStartW = _g3kRudderTrimZone.Width;
                            pbMain.Invalidate(); return;
                        }
                    }
                    // G3K Rudder Trim zone drag (G3KRRZ)
                    {
                        var r = new Rectangle(_g3kRudderTrimZone.X - 2, _g3kRudderTrimZone.Y - 2, _g3kRudderTrimZone.Width + 4, _g3kRudderTrimZone.Height + 4);
                        if (r.Contains(e.Location))
                        {
                            _calibDragG3kRudderTrimZone = true;
                            _calibG3kRudderTrimZoneDragStart = e.Location;
                            _calibG3kRudderTrimZoneStartX = _g3kRudderTrimZoneBaseX;
                            _calibG3kRudderTrimZoneStartY = _g3kRudderTrimZoneBaseY;
                            pbMain.Invalidate(); return;
                        }
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
                foreach (var b in mainSurface.Buttons.Where(b => b.Resizable && b.IsVisible()))
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
                var activeOverlays = _g3000Active ? g3000OverlayImages : overlayImages;
                foreach (var ov in activeOverlays)
                {
                    if (!ov.NoResize && ov.ResizeHandle.Contains(e.Location))
                    {
                        calibResizeOverlay     = ov;
                        calibSelectedOverlay   = ov;
                        calibSelectedBtn       = null; calibSelectedTri = null;
                        calibResizeStartWidth  = ov.ZoomWidth;
                        calibResizeStartHeight = ov.ZoomHeight;
                        calibResizeStartMouseX = e.X;
                        calibResizeStartMouseY = e.Y;
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
                        calibSelectedBtn       = null; calibSelectedTri = null;
                        calibOverlayDragOffset = new Point(e.X - ov.BasePos.X, e.Y - ov.BasePos.Y);
                        calibZoomBox.Visible   = false;
                        pbMain.Invalidate(); return;
                    }
                }
                foreach (var b in mainSurface.Buttons.Where(b => b.IsVisible()))
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

            if (_g3000Active)
                foreach (var ov in g3000OverlayImages.Where(o => o.IsRound))
                    if (ov.OnClick != null && ov.Bounds.Contains(e.Location))
                    { ov.PressedUntilUtc = DateTime.UtcNow.AddMilliseconds(ov.PressVisualMs); ov.OnClick.Invoke(); pbMain.Invalidate(); return; }

            foreach (var ov in overlayImages)
                if (ov.OnClick != null && ov.Bounds.Contains(e.Location))
                { ov.OnClick.Invoke(); return; }

            if (trimZone.Contains(e.Location))
            {
                trimDragging = true;
                trimLastMouseY = e.Y;
            }
            if (_g3000Active && g3kApSliderInputZone.Contains(e.Location))
            {
                g3kApSliderDragging = true;
                g3kApSliderLastMouseY = e.Y;
            }
            if (rudderTrimZone.Contains(e.Location))
            {
                rudderTrimDragging = true;
                rudderTrimLastMouseX = e.X;
            }
        }

        private void PbMain_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_thrHandleDragging)
            {
                var thrHandle = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                if (thrHandle != null)
                {
                    int newY = e.Y - _thrHandleDragOffsetY;
                    int minY, maxY;
                    if (_thrHandleLeftRail)
                    {
                        minY = _thrHandleLeftRailCenterY - 375;
                        maxY = _thrHandleLeftRailCenterY + 250;
                    }
                    else
                    {
                        minY = _thrHandleDefaultY - 170;
                        maxY = _thrHandleDefaultY + 150;
                    }
                    newY = Math.Max(minY, Math.Min(maxY, newY));
                    thrHandle.BasePos = new Point(thrHandle.BasePos.X, newY);
                    if (_thrHandleLeftRail)
                    {
                        // Mapatge lineal: centre TAXI=-16383, amunt=+16383, avall=-21350
                        int centerY = _thrHandleLeftRailCenterY;
                        int distFromCenter = centerY - newY; // positiu = amunt
                        int thrVal;
                        if (distFromCenter >= 0)
                            thrVal = -16383 + (int)((long)distFromCenter * (16383 + 16383) / 375);
                        else
                            thrVal = -16383 + (int)((long)distFromCenter * (21350 - 16383) / 250);
                        thrVal = Math.Max(-21350, Math.Min(16383, thrVal));
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)thrVal), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 8250, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    }
                    int topLimit = minY;
                    int botLimit = maxY;
                    if (!_thrHandleLeftRail && newY <= topLimit + 10)
                    {
                        _taxiPropActive = false; _thrSliderFollow = false;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)(-11000)), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    }
                    if (!_thrHandleLeftRail && newY >= botLimit - 10)
                    {
                        _taxiPropActive = false; _thrSliderFollow = false;
                        simconnect?.SetDataOnSimObject(DEFINITIONS.CondLeverCutoffWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new CondLeverCutoffStruct { value = 0 });
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    }
                    var lowIdleArea = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_LOWIDLE");
                    if (!_thrHandleLeftRail && lowIdleArea != null)
                    {
                        int handleCenterY = newY + thrHandle.Bounds.Height / 2;
                        int areaCenterY = lowIdleArea.BasePos.Y + lowIdleArea.Bounds.Height / 2;
                        if (Math.Abs(handleCenterY - areaCenterY) <= 20)
                        {
                            _taxiPropActive = false; _thrSliderFollow = false;
                            simconnect?.SetDataOnSimObject(DEFINITIONS.CondLeverCutoffWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new CondLeverCutoffStruct { value = 1 });
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)(-16383)), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        }
                    }
                    pbMain.Invalidate();
                }
                return;
            }
            if (calibrationMode)
            {
                if (calibDragFlapsInd)
                {
                    flapsIndX       = calibFlapsIndStartX    + (e.X - calibFlapsIndDragStart.X);
                    flapsIndYOffset = calibFlapsIndStartYOff + (e.Y - calibFlapsIndDragStart.Y);
                    pbMain.Invalidate(); return;
                }
                if (_calibDragG3kFlapsInd)
                {
                    _g3kFlapsIndX       = _calibG3kFlapsIndStartX    + (e.X - _calibG3kFlapsIndDragStart.X);
                    _g3kFlapsIndYOffset = _calibG3kFlapsIndStartYOff + (e.Y - _calibG3kFlapsIndDragStart.Y);
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
                if (_calibDragG3kTrimInd)
                {
                    _g3kTrimIndX      = _calibG3kTrimIndStartX   + (e.X - _calibG3kTrimIndDragStart.X);
                    _g3kTrimIndYOffset = _calibG3kTrimIndStartYOff + (e.Y - _calibG3kTrimIndDragStart.Y);
                    pbMain.Invalidate(); return;
                }
                if (_calibResizeG3kTrimZone)
                {
                    int newH = Math.Max(50, _calibResizeG3kTrimZoneStartH + (e.Y - _calibResizeG3kTrimZoneDragStart.Y));
                    int newW = Math.Max(10, (int)(newH * 119.0 / 603.0));
                    _g3kTrimZone = new Rectangle(_g3kTrimZoneBaseX, _g3kTrimZoneBaseY, newW, newH);
                    pbMain.Invalidate(); return;
                }
                if (_calibDragG3kTrimZone)
                {
                    _g3kTrimZoneBaseX = _calibG3kTrimZoneStartX + (e.X - _calibG3kTrimZoneDragStart.X);
                    _g3kTrimZoneBaseY = _calibG3kTrimZoneStartY + (e.Y - _calibG3kTrimZoneDragStart.Y);
                    _g3kTrimZone = new Rectangle(_g3kTrimZoneBaseX, _g3kTrimZoneBaseY, _g3kTrimZone.Width, _g3kTrimZone.Height);
                    pbMain.Invalidate(); return;
                }
                if (_calibDragG3kRudderTrimInd)
                {
                    _g3kRudderTrimIndY       = _calibG3kRudderTrimIndStartY    + (e.Y - _calibG3kRudderTrimIndDragStart.Y);
                    _g3kRudderTrimIndXOffset = _calibG3kRudderTrimIndStartXOff + (e.X - _calibG3kRudderTrimIndDragStart.X);
                    pbMain.Invalidate(); return;
                }
                if (_calibResizeG3kRudderTrimZone)
                {
                    int newW = Math.Max(50, _calibResizeG3kRudderTrimZoneStartW + (e.X - _calibResizeG3kRudderTrimZoneDragStart.X));
                    int newH = Math.Max(10, (int)(newW * 76.0 / 385.0));
                    _g3kRudderTrimZone = new Rectangle(_g3kRudderTrimZoneBaseX, _g3kRudderTrimZoneBaseY, newW, newH);
                    pbMain.Invalidate(); return;
                }
                if (_calibDragG3kRudderTrimZone)
                {
                    _g3kRudderTrimZoneBaseX = _calibG3kRudderTrimZoneStartX + (e.X - _calibG3kRudderTrimZoneDragStart.X);
                    _g3kRudderTrimZoneBaseY = _calibG3kRudderTrimZoneStartY + (e.Y - _calibG3kRudderTrimZoneDragStart.Y);
                    _g3kRudderTrimZone = new Rectangle(_g3kRudderTrimZoneBaseX, _g3kRudderTrimZoneBaseY, _g3kRudderTrimZone.Width, _g3kRudderTrimZone.Height);
                    pbMain.Invalidate(); return;
                }
                if (calibDragG3kApSlider)
                {
                    g3kApSliderBaseX = calibG3kApSliderStartX + (e.X - calibG3kApSliderDragStart.X);
                    g3kApSliderBaseY = calibG3kApSliderStartY + (e.Y - calibG3kApSliderDragStart.Y);
                    g3kApSliderZone  = new Rectangle(g3kApSliderBaseX, g3kApSliderBaseY, 30, 200);
                    pbMain.Invalidate(); return;
                }
                if (calibDragG3kApSliderInput)
                {
                    g3kApSliderInputBaseX = calibG3kApSliderInputStartX + (e.X - calibG3kApSliderInputDragStart.X);
                    g3kApSliderInputBaseY = calibG3kApSliderInputStartY + (e.Y - calibG3kApSliderInputDragStart.Y);
                    g3kApSliderInputZone  = new Rectangle(g3kApSliderInputBaseX, g3kApSliderInputBaseY, 70, 300);
                    pbMain.Invalidate(); return;
                }
                if (_calibDragApLvlInd)
                {
                    _apLvlIndBaseX = _calibApLvlIndStartX + (e.X - _calibApLvlIndDragStart.X);
                    _apLvlIndBaseY = _calibApLvlIndStartY + (e.Y - _calibApLvlIndDragStart.Y);
                    _apLvlIndRect  = new Rectangle(_apLvlIndBaseX, _apLvlIndBaseY, _apLvlIndW, _apLvlIndH);
                    pbMain.Invalidate(); return;
                }
                if (_calibResizeApLvlInd)
                {
                    _apLvlIndW = Math.Max(10, _calibApLvlIndStartW + (e.X - _calibApLvlIndDragStart.X));
                    _apLvlIndH = Math.Max(10, _calibApLvlIndStartH + (e.Y - _calibApLvlIndDragStart.Y));
                    _apLvlIndRect = new Rectangle(_apLvlIndBaseX, _apLvlIndBaseY, _apLvlIndW, _apLvlIndH);
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
                    foreach (var ov in overlayImages.Concat(g3000OverlayImages).Where(o => o.TypeKey == tk))
                    {
                        ov.ZoomWidth = newW;
                        if (ov.IsRound)
                            ov.ZoomHeight = newW;
                        else if (ov.ZoomHeight > 0)
                            ov.ZoomHeight = Math.Max(10, calibResizeStartHeight + (e.Y - calibResizeStartMouseY));
                    }
                    calibZoomBox.Text = ((int)newW).ToString();
                    pbMain.Invalidate(); return;
                }
                if (calibDragOverlay != null)
                {
                    calibDragOverlay.BasePos = new Point(
                        e.X - calibOverlayDragOffset.X, e.Y - calibOverlayDragOffset.Y);
                    if (calibDragOverlay.Name == "G3K_THR_HANDLE") _thrHandleDefaultY = calibDragOverlay.BasePos.Y;
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

                trimFramePos += deltaY / TRIM_PIXELS_PER_FRAME;
                trimFramePos = ((trimFramePos % RUDDER_TRIM_FRAME_COUNT)
                               + RUDDER_TRIM_FRAME_COUNT) % RUDDER_TRIM_FRAME_COUNT;
                pbMain.Invalidate();
            }
            if (_g3kTrimDragging)
            {
                int deltaY = _g3kTrimLastMouseY - e.Y;
                if (deltaY > 0) _g3kTrimLastDir = +1;
                else if (deltaY < 0) _g3kTrimLastDir = -1;
                _g3kTrimLastMouseY = e.Y;
                _g3kTrimAccumulatedDelta += deltaY;
                while (_g3kTrimAccumulatedDelta >= TRIM_TICK_PIXELS)  { SendTrimTick(+1); _g3kTrimAccumulatedDelta -= TRIM_TICK_PIXELS; }
                while (_g3kTrimAccumulatedDelta <= -TRIM_TICK_PIXELS) { SendTrimTick(-1); _g3kTrimAccumulatedDelta += TRIM_TICK_PIXELS; }
                trimFramePos += deltaY / TRIM_PIXELS_PER_FRAME;
                trimFramePos = ((trimFramePos % TRIM_FRAME_COUNT) + TRIM_FRAME_COUNT) % TRIM_FRAME_COUNT;
                pbMain.Invalidate();
            }
            if (_g3kRudderTrimDragging)
            {
                int deltaX = e.X - _g3kRudderTrimLastMouseX;
                if (deltaX > 0) _g3kRudderTrimLastDir = +1;
                else if (deltaX < 0) _g3kRudderTrimLastDir = -1;
                _g3kRudderTrimLastMouseX = e.X;
                _g3kRudderTrimAccumulatedDelta += deltaX;
                while (_g3kRudderTrimAccumulatedDelta >= RUDDER_TRIM_TICK_PIXELS)  { SendRudderTrimTick(+1); _g3kRudderTrimAccumulatedDelta -= RUDDER_TRIM_TICK_PIXELS; }
                while (_g3kRudderTrimAccumulatedDelta <= -RUDDER_TRIM_TICK_PIXELS) { SendRudderTrimTick(-1); _g3kRudderTrimAccumulatedDelta += RUDDER_TRIM_TICK_PIXELS; }
                rudderTrimFramePos -= deltaX / RUDDER_TRIM_PIXELS_PER_FRAME;
                rudderTrimFramePos = ((rudderTrimFramePos % RUDDER_TRIM_FRAME_COUNT) + RUDDER_TRIM_FRAME_COUNT) % RUDDER_TRIM_FRAME_COUNT;
                pbMain.Invalidate();
            }
            if (g3kApSliderDragging)
            {
                int deltaY = e.Y - g3kApSliderLastMouseY; // positiu = swipe avall = nose up (invertit)
                g3kApSliderLastMouseY = e.Y;
                _g3kApWheelOffset -= deltaY * 0.5f;
                g3kApSliderAccumDelta += deltaY;
                while (g3kApSliderAccumDelta >= G3K_AP_SLIDER_TICK_PIXELS)  { SendG3kApSliderTick(+1); g3kApSliderAccumDelta -= G3K_AP_SLIDER_TICK_PIXELS; }
                while (g3kApSliderAccumDelta <= -G3K_AP_SLIDER_TICK_PIXELS) { SendG3kApSliderTick(-1); g3kApSliderAccumDelta += G3K_AP_SLIDER_TICK_PIXELS; }
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

                rudderTrimFramePos -= deltaX / RUDDER_TRIM_PIXELS_PER_FRAME;
                rudderTrimFramePos = ((rudderTrimFramePos % RUDDER_TRIM_FRAME_COUNT)
                                     + RUDDER_TRIM_FRAME_COUNT) % RUDDER_TRIM_FRAME_COUNT;
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

        private void SendAileronTrimSet(int axisVal)
        {
            if (simconnect == null) return;
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.AILERON_TRIM_SET_EX1, (uint)axisVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void SendG3kApSliderTick(int dir)
        {
            if (simconnect == null) return;
            if (dir > 0) // swipe up = nose up
            {
                if (_kap140State != 0 && _apVsHold == 1)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_KAP140_UP, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                else if (_kap140State != 0 && _apFlcActive == 1)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_SPD_DEC,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                else {
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_VS_VAR_INC, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_SPD_DEC,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
            }
            else // swipe down = nose down
            {
                if (_kap140State != 0 && _apVsHold == 1)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_KAP140_DN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                else if (_kap140State != 0 && _apFlcActive == 1)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_SPD_INC,   0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                else {
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_VS_VAR_DEC, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.G3K_AP_SPD_INC,    0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                }
            }
        }


        private void PbMain_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_thrHandleDragging)
            {
                _thrHandleDragging = false;
                return;
            }
            if (calibrationMode)
            {
                if (calibDragFlapsInd)
                {
                    calibDragFlapsInd = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (_calibDragG3kFlapsInd)
                {
                    _calibDragG3kFlapsInd = false;
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
                if (_calibDragG3kTrimInd)
                {
                    _calibDragG3kTrimInd = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (_calibResizeG3kTrimZone)
                {
                    _calibResizeG3kTrimZone = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (_calibDragG3kTrimZone)
                {
                    _calibDragG3kTrimZone = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (_calibDragG3kRudderTrimInd)
                {
                    _calibDragG3kRudderTrimInd = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (_calibResizeG3kRudderTrimZone)
                {
                    _calibResizeG3kRudderTrimZone = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (_calibDragG3kRudderTrimZone)
                {
                    _calibDragG3kRudderTrimZone = false;
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
                if (calibDragG3kApSlider)
                {
                    calibDragG3kApSlider = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (calibDragG3kApSliderInput)
                {
                    calibDragG3kApSliderInput = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (_calibDragApLvlInd)
                {
                    _calibDragApLvlInd = false;
                    try { ExportCalibrationPositions(silent: true); } catch { }
                    return;
                }
                if (_calibResizeApLvlInd)
                {
                    _calibResizeApLvlInd = false;
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
            _g3kTrimDragging = false;
            _g3kTrimLastDir = 0;
            _g3kRudderTrimDragging = false;
            _g3kRudderTrimLastDir = 0;
            rudderTrimDragging = false;
            rudderTrimLastDir = 0;
            g3kApSliderDragging = false;
            g3kApSliderAccumDelta = 0;
            pbMain.Invalidate();
        }


        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (!calibrationMode) return base.ProcessCmdKey(ref msg, keyData);

            int dx = 0, dy = 0;
            switch (keyData)
            {
                case Keys.Left:   dx = -1; break;
                case Keys.Right:  dx =  1; break;
                case Keys.Up:     dy = -1; break;
                case Keys.Down:   dy =  1; break;
                case Keys.Escape:
                    calibSelectedBtn = null; calibSelectedTri = null;
                    calibSelectedOverlay = null; calibZoomBox.Visible = false;
                    pbMain.Invalidate(); return true;
                default: return base.ProcessCmdKey(ref msg, keyData);
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
            return true;
        }

        private void frmMain_KeyDown(object? sender, KeyEventArgs e)
        {
            // F12 és gestionat per la lambda a InitializeComponent; ProcessCmdKey gestiona fletxes i Escape
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
            foreach (var ov in g3000OverlayImages)
            {
                int saveY = (ov.Name == "G3K_THR_HANDLE" && _thrHandleDefaultY >= 0) ? _thrHandleDefaultY : ov.BasePos.Y;
                sb.AppendLine(ov.ZoomHeight > 0
                    ? $"G3K:{ov.Name}:{ov.BasePos.X}:{saveY}:{(int)ov.ZoomWidth}:{(int)ov.ZoomHeight}"
                    : $"G3K:{ov.Name}:{ov.BasePos.X}:{saveY}:{(int)ov.ZoomWidth}");
            }
            sb.AppendLine($"FLI:fli:{flapsIndX}:{flapsIndYOffset}");
            sb.AppendLine($"G3KFLI:g3kfli:{_g3kFlapsIndX}:{_g3kFlapsIndYOffset}");
            sb.AppendLine($"G3KTRIND:g3ktri:{_g3kTrimIndX}:{_g3kTrimIndYOffset}");
            sb.AppendLine($"G3KTRIMZ:g3ktrz:{_g3kTrimZoneBaseX}:{_g3kTrimZoneBaseY}:{_g3kTrimZone.Width}:{_g3kTrimZone.Height}");
            sb.AppendLine($"G3KRRI:g3krri:{_g3kRudderTrimIndXOffset}:{_g3kRudderTrimIndY}");
            sb.AppendLine($"G3KRRZ:g3krrz:{_g3kRudderTrimZoneBaseX}:{_g3kRudderTrimZoneBaseY}:{_g3kRudderTrimZone.Width}:{_g3kRudderTrimZone.Height}");
            sb.AppendLine($"TRIND:tri:{trimIndX}:{trimIndYOffset}");
            sb.AppendLine($"TRIMZ:tz:{trimZoneBaseX}:{trimZoneBaseY}");
            sb.AppendLine($"G3KAPZ:apz:{g3kApSliderBaseX}:{g3kApSliderBaseY}");
            sb.AppendLine($"G3KAPZIN:apzin:{g3kApSliderInputBaseX}:{g3kApSliderInputBaseY}");
            sb.AppendLine($"LVLIND:lvlind:{_apLvlIndBaseX}:{_apLvlIndBaseY}:{_apLvlIndW}:{_apLvlIndH}");
            sb.AppendLine($"RRI:rri:{rudderTrimIndXOffset}:{rudderTrimIndY}");
            sb.AppendLine($"RRZ:rrz:{rudderTrimZoneBaseX}:{rudderTrimZoneBaseY}");
            sb.AppendLine($"TACBTN:tactile:{_tactileBtn.Location.X}:{_tactileBtn.Location.Y}");
            sb.AppendLine($"CLOSEBTN:close:{closeButton.Location.X}:{closeButton.Location.Y}");
            sb.AppendLine($"SHOWBTN:show:{_showAreasBtn.Location.X}:{_showAreasBtn.Location.Y}");
            sb.AppendLine($"GEARBTN:gear:{_gearBtn.Location.X}:{_gearBtn.Location.Y}");
            sb.AppendLine($"OVGEARBTN:ovgear:{_overlayGearBtn.Location.X}:{_overlayGearBtn.Location.Y}");
            sb.AppendLine($"VOLGRP:vg:{_volGrpAnchor.X}:{_volGrpAnchor.Y}");
            sb.AppendLine($"FSBTN:fs:{_fullscreenBtn.Location.X}:{_fullscreenBtn.Location.Y}");
            sb.AppendLine($"ENDBTN:end:{_msfsEndBtn.Location.X}:{_msfsEndBtn.Location.Y}");
            sb.AppendLine($"FOCUSBTN:focus:{_focusBtn.Location.X}:{_focusBtn.Location.Y}");
            sb.AppendLine($"ENGSTARTBTN:engstart:{_engineStartBtn.Location.X}:{_engineStartBtn.Location.Y}");
            sb.AppendLine($"ENGSTOPBTN:engstop:{_engineStopBtn.Location.X}:{_engineStopBtn.Location.Y}");
            sb.AppendLine($"G3KBTN:g3k:{_g3000Btn.Location.X}:{_g3000Btn.Location.Y}");
            sb.AppendLine($"THRQBTN:tq:{_thrQuadrantBtn.Location.X}:{_thrQuadrantBtn.Location.Y}:0:0");
            if (_fsConnPanel != null) sb.AppendLine($"FSCONN:fsc:{_fsConnPanel.Location.X}:{_fsConnPanel.Location.Y}");
            string path = System.IO.Path.Combine(Application.StartupPath, "calibration_export.txt");
            System.IO.File.WriteAllText(path, sb.ToString());
            if (!silent)
                MessageBox.Show($"Exportat a:\n{path}", "Calibratge", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadCalibrationPositions()
        {
            string path = System.IO.Path.Combine(Application.StartupPath, "calibration_export.txt");
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
                else if (type == "G3K" && parts.Length >= 5 && int.TryParse(parts[4], out int g3kw))
                {
                    var ov = g3000OverlayImages.FirstOrDefault(o => o.Name == name);
                    if (ov != null)
                    {
                        ov.BasePos = new Point(x, y);
                        if (name == "G3K_THR_HANDLE") { _thrHandleDefaultY = y; _thrHandleRightRailX = x; }
                        string tk = ov.TypeKey;
                        int g3kh = (parts.Length >= 6 && int.TryParse(parts[5], out int h)) ? h : 0;
                        foreach (var same in g3000OverlayImages.Where(o => o.TypeKey == tk))
                        {
                            same.ZoomWidth = g3kw;
                            if (g3kh > 0) same.ZoomHeight = g3kh;
                        }
                    }
                }
                else if (type == "FLI")
                {
                    flapsIndX = x;
                    flapsIndYOffset = y;
                }
                else if (type == "G3KFLI")
                {
                    _g3kFlapsIndX = x;
                    _g3kFlapsIndYOffset = y;
                }
                else if (type == "G3KTRIND")
                {
                    _g3kTrimIndX = x;
                    _g3kTrimIndYOffset = y;
                }
                else if (type == "G3KTRIMZ")
                {
                    _g3kTrimZoneBaseX = x; _g3kTrimZoneBaseY = y;
                    int loadH = (parts.Length >= 6 && int.TryParse(parts[5], out int th)) ? th : 603;
                    int loadW = (parts.Length >= 5 && int.TryParse(parts[4], out int tw)) ? tw : (int)(loadH * 119.0 / 603.0);
                    _g3kTrimZone = new Rectangle(x, y, loadW, loadH);
                }
                else if (type == "G3KRRI")
                {
                    _g3kRudderTrimIndXOffset = x;
                    _g3kRudderTrimIndY = y;
                }
                else if (type == "G3KRRZ")
                {
                    _g3kRudderTrimZoneBaseX = x; _g3kRudderTrimZoneBaseY = y;
                    int loadW = (parts.Length >= 5 && int.TryParse(parts[4], out int rw)) ? rw : 385;
                    int loadH = (parts.Length >= 6 && int.TryParse(parts[5], out int rh)) ? rh : (int)(loadW * 76.0 / 385.0);
                    _g3kRudderTrimZone = new Rectangle(x, y, loadW, loadH);
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
                else if (type == "G3KAPZ")
                {
                    g3kApSliderBaseX = x;
                    g3kApSliderBaseY = y;
                    g3kApSliderZone  = new Rectangle(g3kApSliderBaseX, g3kApSliderBaseY, 30, 200);
                }
                else if (type == "G3KAPZIN")
                {
                    g3kApSliderInputBaseX = x;
                    g3kApSliderInputBaseY = y;
                    g3kApSliderInputZone  = new Rectangle(g3kApSliderInputBaseX, g3kApSliderInputBaseY, 70, 300);
                }
                else if (type == "LVLIND" && parts.Length >= 6 &&
                         int.TryParse(parts[4], out int lvlW) && int.TryParse(parts[5], out int lvlH))
                {
                    _apLvlIndBaseX = x; _apLvlIndBaseY = y;
                    _apLvlIndW = lvlW; _apLvlIndH = lvlH;
                    _apLvlIndRect = new Rectangle(_apLvlIndBaseX, _apLvlIndBaseY, _apLvlIndW, _apLvlIndH);
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
                else if (type == "GEARBTN")
                {
                    _gearBtn.Location = new Point(x, y);
                }
                else if (type == "OVGEARBTN")
                {
                    _overlayGearBtn.Location = new Point(x, y);
                }
                else if (type == "VOLGRP")
                {
                    _volGrpAnchor = new Point(x, y);
                    _volUpBtn.Location   = new Point(x, y);
                    _volDownBtn.Location = new Point(x, y + 70);
                }
                else if (type == "FSBTN")
                {
                    _fullscreenBtn.Location = new Point(x, y);
                }
                else if (type == "ENDBTN")
                {
                    _msfsEndBtn.Location = new Point(x, y);
                }
                else if (type == "FOCUSBTN")
                {
                    _focusBtn.Location = new Point(x, y);
                }
                else if (type == "ENGSTARTBTN")
                {
                    _engineStartBtn.Location = new Point(x, y);
                }
                else if (type == "ENGSTOPBTN")
                {
                    _engineStopBtn.Location = new Point(x, y);
                }
                else if (type == "G3KBTN")
                {
                    _g3000Btn.Location = new Point(x, y);
                }
                else if (type == "THRQBTN")
                {
                    _thrQuadrantBtn.Location = new Point(x, y);
                }
                else if (type == "FSCONN" && _fsConnPanel != null)
                {
                    _fsConnPanel.Location = new Point(x, y);
                }
            }
        }

        private Image? LoadOverlayImage(string filename)
        {
            var asm = System.Reflection.Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream($"panelAIR1000.Resources.{filename}");
            return stream != null ? Image.FromStream(stream) : null;
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

                if (s == closeButton) return;

                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            };

            foreach (Control child in control.Controls)
            {
                if (child == pbMain) continue;
                if (child == closeButton) continue;

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
