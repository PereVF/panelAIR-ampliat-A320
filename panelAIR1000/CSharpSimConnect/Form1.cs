


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

        // Ritme fix de lectura per als 40 interruptors/bombes/etc. + Struct1 (flaps/trim/rodes de
        // trim): abans cadascuna era una subscripció contínua a SIM_FRAME/VISUAL_FRAME (MSFS les
        // havia de servir a CADA fotograma, per sempre, independentment dels FPS reals ni de si
        // panelAIR1000 en feia res) — això sortia a l'auditoria de rendiment com la causa principal
        // de perdre ~15 FPS amb l'app oberta. Ara és aquest timer qui, cada 200ms (5/s, fix,
        // independent dels FPS de MSFS), torna a demanar cada valor amb SIMCONNECT_PERIOD.ONCE.
        // Decisió de Pere, 2026-10-01: provar primer només amb això (switches + Struct1) en mode
        // G1000 abans de decidir res sobre flaps/trim.
        private System.Windows.Forms.Timer? _dataPollTimer;


        // Flaps — FLAPS NUM HANDLE POSITIONS / FLAPS HANDLE INDEX separades l'una de l'altra
        // (2026-10-03, Pere: no té sentit ajuntar-les; la primera és pràcticament constant per avió,
        // la segona és la posició real). NUM POSITIONS es llegeix 1 cop en sincronitzar i prou. HANDLE
        // INDEX (pas 3 de la unificació) té floor al grup dels 124 (200ms, per tothom) i puja a 50ms
        // durant 2s quan el propi handler detecta un canvi real — igual que throttle i selector TBM,
        // tant si l'hem clicat nosaltres com si ha canviat des de fora.
        private System.Windows.Forms.Timer? _flapsActiveReadTimer;
        private DateTime _flapsActiveReadUntilUtc = DateTime.MinValue;

        // Trim (elevator + rudder, G1000 i G3000) — lectura independent (prova de Pere, 2026-10-02):
        // sync 1 cop (connexió / canvi d'avió) i, fora d'això, NO es torna a llegir MAI MÉS, excepte
        // mentre cal seguir el valor real: elevator (vertical, l'únic motoritzat per l'AP) quan
        // AUTOPILOT MASTER és actiu O s'arrossega la seva roda (G1000/G3000); rudder NOMÉS quan
        // s'arrossega la seva pròpia roda (mai per l'AP, confirmat per Pere: el rudder trim no és
        // motoritzat). Dues definicions/timers independents perquè cadascuna s'engega per motius
        // diferents.
        private System.Windows.Forms.Timer? _elevatorTrimActiveReadTimer;
        private System.Windows.Forms.Timer? _rudderTrimActiveReadTimer;

        private bool _suppressCloseOnConnectFail = false;
        private bool showAllClickAreas = false;
        private UiSurface mainSurface;

        private bool sfsMonitorRunning = false;
        private Form? _sfsMissingForm;
        private System.Threading.Timer? _sfsCheckTimer;
        private System.Windows.Forms.Label _aircraftTitleLbl = null!;

        // Mode demo (sense hardware panelAIR): finestra redimensionable a escala en comptes
        // de fullscreen natiu 1:1 sobre una pantalla 4K real. Decidit a Program.cs abans de
        // construir frmMain (necessari perquè el constructor fa servir screen4K de seguida).
        internal static bool IsDemoMode = false;
        private System.Threading.Timer? _demoTimer;
        private DateTime _demoStartTime;




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
        bool _isMsfs2024; // true si connectat a MSFS2024 (sistema de combustible nou)
        string _currentAircraftTitle = "";

        bool IsDA40() =>
            _currentAircraftTitle.IndexOf("DA40", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("DA 40", StringComparison.OrdinalIgnoreCase) >= 0;

        // Cessna 400 Corvalis (TTx) — selector únic Left/Right/Off, confirmat que respon a
        // K:2:FUELSYSTEM_JUNCTION_SET amb junction=1 (opcions: 0=Off, 1=Left, 2=Right).
        bool IsC400() =>
            _currentAircraftTitle.IndexOf("C400", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("Corvalis", StringComparison.OrdinalIgnoreCase) >= 0;

        // Cessna 172 — selector únic Left/Both/Right (sense Off), confirmat que respon a
        // K:2:FUELSYSTEM_JUNCTION_SET amb junction=1 (opcions: 1=Left, 2=Both, 3=Right).
        // 2026-09-05: el paquet de tercers "FS Connection" ven diverses variants del C172SP amb panells
        // diferents (p.ex. "C172SP Classic Aerial Advertising" = instruments clàssics, "C172SP IFD Aerial
        // Advertising" = GPS Avidyne IFD, "C172SP G1000 Tow" = G1000 real) — totes contenen "172", així
        // que calen exclusions explícites per no tractar les no-G1000 com a compatibles.
        bool IsC172() =>
            (_currentAircraftTitle.IndexOf("C172", StringComparison.OrdinalIgnoreCase) >= 0 ||
             _currentAircraftTitle.IndexOf("172", StringComparison.OrdinalIgnoreCase) >= 0 ||
             // Títol real del C172 per defecte a MSFS2020: "Cessna Skyhawk G1000 Asobo" — sense "172"
             // enlloc (confirmat per Pere, 2026-09-01), per això no es detectava cap element propi.
             _currentAircraftTitle.IndexOf("Skyhawk", StringComparison.OrdinalIgnoreCase) >= 0) &&
            _currentAircraftTitle.IndexOf("Classic", StringComparison.OrdinalIgnoreCase) < 0 &&
            _currentAircraftTitle.IndexOf("IFD", StringComparison.OrdinalIgnoreCase) < 0;

        // Text de les 3 etiquetes del selector únic (SW_SE_LBL_1/2/3), segons l'avió monomotor actiu.
        string SeSelectorLabelText(int slot)
        {
            if (IsC400())
                return slot switch { 1 => "OFF", 2 => "LEFT", _ => "RIGHT" };
            if (IsC172())
                return slot switch { 1 => "LEFT", 2 => "BOTH", _ => "RIGHT" };
            if (IsBonanzaG36())
                return slot switch { 1 => "OFF", 2 => "LEFT", _ => "RIGHT" };
            if (IsCirrusSR22())
                return slot switch { 1 => "OFF", 2 => "LEFT", _ => "RIGHT" };
            if (IsDA40())
                // Etiquetes reals NORMAL/EMERGENCY (2026-09-01: també aplicat a 2020, abans deia LEFT/RIGHT).
                return slot switch { 1 => "OFF", 2 => "NORMAL", _ => "EMERGENCY" };
            if (IsC408())
                return slot switch { 1 => "L TANK", 2 => "TRANSF OFF", _ => "R TANK" };
            return "";
        }

        bool IsBaronG58() =>
            _currentAircraftTitle.IndexOf("Baron", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("G58", StringComparison.OrdinalIgnoreCase) >= 0;

        bool IsCaravan() =>
            _currentAircraftTitle.IndexOf("Caravan", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("208", StringComparison.OrdinalIgnoreCase) >= 0;

        bool IsCirrusSR22() =>
            _currentAircraftTitle.IndexOf("SR22", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("Cirrus", StringComparison.OrdinalIgnoreCase) >= 0;

        // Caravan i Cirrus SR22 comparteixen el mateix esquema de bomba (mateixes B: vars al WASM) — pendent de confirmar en vol.
        // Caravan només fa servir l'esquema WASM propi (SFS.FuelPump1_On/Auto/Off) sota MSFS2024 —
        // sota MSFS2020 cau a l'esquema genèric (SWITCH_FUELPUMP1 natiu) ja existent per defecte.
        bool UsesCaravanPumpScheme() => (IsCaravan() && _isMsfs2024) || IsCirrusSR22();

        // Avions amb bomba de 3 posicions (HIGH/LOW/OFF) gestionada 100% localment via StepFuelPumpBaron,
        // sense dependre de cap lectura del simulador per l'estat visual (2026-09-01, Caravan 2020 afegida
        // després de confirmar K:ELECT_FUEL_PUMP1_SET via revòlver — mateix event que Baron/Bonanza 2020).
        bool UsesPump1_3StateUi() => (IsBaronG58() && _isMsfs2024) || UsesCaravanPumpScheme() || (IsBonanzaG36() && _isMsfs2024) || (IsCaravan() && !_isMsfs2024);

        // Monomotor — bomba única (pump 1), mateix esquema HIGH/LOW/OFF que la Baron (B:FUEL_Pump_1_High/_Low/_Off,
        // confirmat a Behaviors, idèntic al de la Baron — reutilitza els mateixos events WASM, no en calen de nous).
        bool IsBonanzaG36() =>
            _currentAircraftTitle.IndexOf("Bonanza", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("G36", StringComparison.OrdinalIgnoreCase) >= 0;

        bool IsDA62() =>
            _currentAircraftTitle.IndexOf("DA62", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("DA 62", StringComparison.OrdinalIgnoreCase) >= 0;

        // C408 SkyCourier: un sol selector FUEL_TRANSFER (Left/Off/Right), no selectors independents.
        // PROVA: FUELSYSTEM_PUMP_ON/OFF són events SimConnect estàndard d'1 sol paràmetre (l'índex de
        // bomba), a diferència de FUELSYSTEM_VALVE_SET/JUNCTION_SET/TRIGGER_SET (2 paràmetres, per això
        // calia WASM). Si això funciona, NO cal WASM per aquest avió.
        bool IsC408() =>
            _currentAircraftTitle.IndexOf("C408", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("SkyCourier", StringComparison.OrdinalIgnoreCase) >= 0;

        bool IsTBM930() =>
            _currentAircraftTitle.IndexOf("TBM930", StringComparison.OrdinalIgnoreCase) >= 0 ||
            _currentAircraftTitle.IndexOf("TBM 930", StringComparison.OrdinalIgnoreCase) >= 0;

        // A320neo de FlyByWire (A32NX). Segons la versió/simulador el títol és p.ex. "Airbus A320 Neo FlyByWire"
        // o conté "A32NX"; es demana "A320" + "FlyByWire"/"FBW" per no confondre'l amb altres A320.
        bool IsA320FBW() =>
            _currentAircraftTitle.IndexOf("A32NX", StringComparison.OrdinalIgnoreCase) >= 0 ||
            (_currentAircraftTitle.IndexOf("A320", StringComparison.OrdinalIgnoreCase) >= 0 &&
             (_currentAircraftTitle.IndexOf("FlyByWire", StringComparison.OrdinalIgnoreCase) >= 0 ||
              _currentAircraftTitle.IndexOf("FBW", StringComparison.OrdinalIgnoreCase) >= 0));

        // Estat de 3 posicions (0=HIGH, 1=LOW, 2=OFF) per bombes que ho necessiten (Baron G58)
        int _fuelPump1State3 = 0;
        int _fuelPump2State3 = 0;
        // Caravan — interruptor STARTER (0=START, 1=OFF, 2=MOTOR, mateixa codificació que L:XMLVAR_Starter1)
        int _caravanStarter1Pos = 1;
        private DateTime _lastCaravanStarter1ClickUtc = DateTime.MinValue;
        bool pbrakeActive;
        double _lastFuelSelLeftDebugValue = double.MinValue; // temporal, per diagnosticar la lectura del selector esquerre (veure Output/Debug de VS)
        double _propRpm1 = 0; // PROP RPM:1 — usat per sincronitzar el selector de combustible de la DA-62
        double _propRpm2 = 0; // PROP RPM:2 — idem, motor dret
        double _daFuelTankSelector = double.MinValue; // A:FUEL TANK SELECTOR:1 — verificació directa DA-40 (veure Output/Debug de VS)
        double _daFuelTankSelector2 = double.MinValue; // A:FUEL TANK SELECTOR:2 — idem, per si l'índex real no és l'1
        double _daFuelTankSelector3 = double.MinValue; // A:FUEL TANK SELECTOR:3 — idem (la bomba de la DA-40 fa servir índex 3)
        double _daFuelTankSelector4 = double.MinValue; // A:FUELSYSTEM VALVE SWITCH:4 — hipòtesi: vàlvula dreta de la Caravan
        // FUEL TANK SELECTOR:1/2 — lectura pròpia i independent (2026-10-03, Pere: separar-les d'1 a
        // 1, no té sentit ajuntar-les), fora de Struct1 i del grup de flaps/trim. Fan servir aquest
        // simvar genèric: Bonanza, Cirrus SR22T, DA-40 (2020), C172 Skyhawk (2020), Baron (2020),
        // Caravan (2020), DA-62 (2020), i el "fallback" SINGLE/TWIN ENGINE genèric (resta d'avions
        // G1000) + el rotatiu animat del TBM930/G3000 — són 11-12 consumidors del mateix camp.
        private int _fuelTankSelector1 = 0;
        private int _fuelTankSelector2 = 0;

        // FUEL TANK SELECTOR:1/2 (2026-10-03, Pere, pas 2 de la unificació): floor unificat al grup
        // dels 124 (200ms, per tothom incloent la TBM, que el gira sola automàticament); si qualsevol
        // dels 2 handlers detecta un canvi real, aquest timer puja a 50ms durant 2s — mateix patró que
        // el throttle (pas 1). Res de "només si cliquem" ni "només si és la TBM".
        private System.Windows.Forms.Timer? _fuelTankSelectorActiveReadTimer;
        private DateTime _fuelTankSelectorActiveReadUntilUtc = DateTime.MinValue;

        int _bonanzaFuelSelectorRawLast = int.MinValue; // diagnòstic: valor cru de _fuelTankSelector1 vist per la Bonanza (veure Output/Debug de VS)
        int _cirrusFuelSelectorRawLast = int.MinValue; // diagnòstic: valor cru de _fuelTankSelector1 vist per la Cirrus SR22T (veure Output/Debug de VS)
        int _da40FuelSelectorRawLast = int.MinValue; // diagnòstic: valor cru de _fuelTankSelector1 vist per la DA-40 (2020, veure Output/Debug de VS)
        int _c172FuelSelectorRawLast = int.MinValue; // diagnòstic: valor cru de _fuelTankSelector1 vist per la C172 Skyhawk (2020, veure Output/Debug de VS)
        double _da62AvionicsLineConn = double.MinValue; // A:LINE CONNECTION ON:rh_main_bustoavionics_bus — estat real del bus d'avionics de la DA-62
        double _c172Avionics1LineConn = double.MinValue; // A:LINE CONNECTION ON:BUS_1_To_AVIONICS_BUS_1
        double _c172Avionics2LineConn = double.MinValue; // A:LINE CONNECTION ON:BUS_2_To_AVIONICS_BUS_2
        double _caravanAvionics1LineConn = double.MinValue; // A:LINE CONNECTION ON:AVIONICS_BUS_1_To_PWR_BUS_DISTRIB
        double _caravanAvionics2LineConn = double.MinValue; // A:LINE CONNECTION ON:AVIONICS_BUS_2_To_PWR_BUS_DISTRIB
        double _caravanAltGen2LineConn = double.MinValue; // A:LINE CONNECTION ON:BATT_BUS_To_ALT_BUS
        double _c400JunctionSetting = double.MinValue; // A:FUELSYSTEM JUNCTION SETTING:1 — opció real seleccionada al C400
        double _da62EngineMaster1 = double.MinValue; // A:RECIP ENG ENGINE MASTER SWITCH:1 — feedback real DA-62, motor esquerre
        double _da62EngineMaster2 = double.MinValue; // A:RECIP ENG ENGINE MASTER SWITCH:2 — feedback real DA-62, motor dret
        double _da40EssentialBus2 = double.MinValue; // L:ELECTRICAL_EssentialBus_2 — feedback real ALT/GEN 2 DA-40
        double _da40BusConnOn3 = double.MinValue; // A:BUS CONNECTION ON:3 — estat viu del relè, per calcular la comanda ESS BUS 2020 sense endevinar
        double _da40CircuitSwitchOn7 = double.MinValue; // A:CIRCUIT SWITCH ON:7 — feedback real del FUEL TRANSFER switch (DA-40 2020)
        double _da40EngineMaster1 = double.MinValue; // RECIP ENG ENGINE MASTER SWITCH:1 — feedback real Engine Master DA-40
        double _cirrusDeiceMaster = double.MinValue; // L:XMLVAR_DEICE_MASTER — feedback real deice airframe Cirrus SR22T
        int _c408Idx3 = int.MinValue; // FUELSYSTEM PUMP SWITCH:3 (via Struct1/read_fuelpump_idx3_2024) — feedback real selector C408
        int _c408Idx4 = int.MinValue; // FUELSYSTEM PUMP SWITCH:4 (lectura dedicada) — feedback real selector C408
        DateTime _fuelSel1IgnoreFollowUntil = DateTime.MinValue; // evita el "rebot" quan la lectura encara no ha seguit el clic
        DateTime _fuelSel2IgnoreFollowUntil = DateTime.MinValue;
        DateTime _seSelectorIgnoreFollowUntil = DateTime.MinValue; // idem, pel selector únic (C400)
        bool masterBatteryOff;
        bool avionics1Off;
        bool avionics2Off;
        bool altGen1Off;
        bool altGen2Off;
        bool starter1Active;
        bool starter2Active;
        // Magnetos/starter — lectures pròpies i independents (2026-10-03, Pere), fora de Struct1. Cadascun
        // dels 3 camps per motor (left/right/starter) arriba per separat; es guarden aquí i ign1Pos/ign2Pos
        // es recalculen junts a cada arribada (UpdateIgnition1State/UpdateIgnition2State).
        int _ign1Left = 0, _ign1Right = 0, _ign1Starter = 0;
        int _ign2Left = 0, _ign2Right = 0, _ign2Starter = 0;
        // Fuel pump 1/2 — mateix tractament (2026-10-03, Pere). Es guarden els 5 camps crus (2020 i 2024,
        // incloent l'índex 3 compartit amb el selector de la C408) i fuelpump1Active/2Active es recalculen
        // junts a UpdateFuelPumpState().
        int _readFuelpump1 = 0, _readFuelpump2 = 0;
        int _readFuelpump1_2024 = 0, _readFuelpump2_2024 = 0, _readFuelpumpIdx3_2024 = 0;
        bool ignition1Active;
        bool ignition2Active;
        // Caravan IGNITION (NORM/ON) — estat LOCAL, no derivat de la lectura d'A:TURB ENG IGNITION
        // SWITCH EX1:1 (compartida amb STARTER). Investigació real 2026-08-26: al VC físic no hi ha cap
        // lligam mecànic entre STARTER i IGNITION — moure STARTER mai mou visualment IGNITION, encara
        // que per sota afecti la mateixa variable elèctrica. Si deixéssim IGNITION 100% feedback-driven
        // (com la resta de switches), es mouria sol cada cop que STARTER canvia, cosa que Pere ha
        // confirmat que NO passa al VC real — per això aquí, excepcionalment, cal estat local.
        bool _caravanIgnitionOn;
        bool _da40FuelTransferOn; // estat local (sense lectura real encara) del FUEL TRANSFER switch — DA-40 2020
        // Debounce: quan IGNITION escriu al mateix K:TURBINE_IGNITION_SWITCH_SET1 que fa servir
        // StepCaravanStarter1, el propi MSFS actualitza L:XMLVAR_Starter1 com a efecte colateral
        // (electrico, real) — però Pere ha confirmat que al VC físic això MAI mou visualment el
        // selector de STARTER. Ignorem la següent lectura de STARTER durant una finestra curta
        // just després d'un clic d'IGNITION, mateix patró que _fuelSel1IgnoreFollowUntil/_thrIgnoreFollowUntil.
        DateTime _caravanStarter1IgnoreFollowUntil = DateTime.MinValue;
        // ALTGEN1 (generator) — animació purament cosmètica de RESET (2026-08-26): confirmat que la
        // posició 3D real al VC és inabastable des de fora (veure [[project_wasm_pending_changes]]), però
        // Pere vol igualment el "flaix" d'1 segon a l'app quan es clica amunt. NO envia cap comanda pròpia
        // — és estat 100% local, l'única excepció comptada a "mai animar sense feedback".
        bool _altGen1ResetAnim;
        bool da62EngineMaster1On; // feedback real (RECIP ENG ENGINE MASTER SWITCH:1) — DA-62
        bool da62EngineMaster2On; // feedback real (RECIP ENG ENGINE MASTER SWITCH:2) — DA-62
        bool da40EngineMaster1On; // feedback real (RECIP ENG ENGINE MASTER SWITCH:1) — DA-40, monomotor
        private int ign1Pos = 2; // 0=OFF, 1=RIGHT, 2=LEFT(default), 3=BOTH, 4=START
        private int ign2Pos = 2;
        private int fuel1Pos = 0; // 0=OFF(-30°), 1=ON(0°), 2=CROSSFEED(+30°)
        private int fuel2Pos = 0; // 0=CROSSFEED(-30°), 1=ON(0°), 2=OFF(+30°)
        // Selector únic de 3 posicions per monomotors (DA-40 de moment; altres single engines reutilitzaran
        // aquest mateix camp/element amb la convenció Left/Both/Right). DA-40: 0=Normal(-30°), 1=Emergency(0°), 2=OFF(+30°).
        // Comença a 1 (mig) perquè les 2 direccions siguin operables des del primer clic (a 0, "esquerra" quedava
        // bloquejat pel guard `if (seSelectorPos <= 0) return;`, cosa que podia semblar "no fa res").
        private int seSelectorPos = 1;
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

        // Timer dedicat només per a l'animació de l'indicador de flaps, independent de les lectures
        // SimConnect (fixes a 200ms, intocades) i del propi Paint — el mecanisme anterior
        // (auto-repintar-se des de dins de Paint amb un llindar de 50ms) es podia trencar en silenci
        // si un cicle de Paint arribava abans dels 50ms: aquell cicle no programava el següent
        // repintat, i com que res més invalida l'indicador, l'animació es quedava "a salts" seguint
        // només el ritme de 200ms de les lectures de Struct1 (confirmat per Pere, 2026-10-01, provat
        // primer només a G1000). Amb un Timer propi, el ritme de repintat (33ms, ~30/s) no depèn de
        // res més. Un per panell perquè només un dels dos camins de Paint s'executa per frame, però
        // cadascun té el seu propi estat d'animació (_g3kFlapsXxx vs flapsXxx).
        private System.Windows.Forms.Timer? _flapsAnimTimerG1000;
        private System.Windows.Forms.Timer? _flapsAnimTimerG3000;

        // Indicadors de trim (el cercle que mostra el valor real, no la roda que mous amb el dit):
        // sense interpolació — es dibuixen directament al valor llegit (2026-10-02, Pere: ara que
        // només es llegeix activament mentre cal — arrossegant o amb l'AP actiu — qualsevol
        // suavitzat només afegeix latència respecte al dit, es nota molt).

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

        private int nav1SwapCnt = 0; // comptador B5 (NAV1_SWAP)
        private int nav2SwapCnt = 0; // comptador B38 (NAV2_SWAP)

        enum DEFINITIONS
        {
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
            GenericRpnClientDataDef,
            AircraftTitle,
            A320BaroRead,        // A320 FBW: QNH del capità (valor, mode, unitat, alimentació)
            A320BaroUnitWrite,   // A320 FBW: L:A32NX_FCU_EFIS_L_BARO_IS_INHG
            FlapsNumPosRead,
            FlapsHandleIndexRead,
            ElevatorTrimRead,
            RudderTrimRead,
            FuelTankSelector1Read,
            FuelTankSelector2Read,
            ThrottleLeverPosRead,
            LightBeaconRead,
            LightLandRead,
            LightTaxiRead,
            LightNavRead,
            LightStrobeRead,
            LightRecognRead,
            LightPanelRead,
            LightLogoRead,
            LightingLogo1Read,
            LightingWing1Read,
            ParkingBrakeRead,
            BatteryVoltageRead,
            FuelSelLeftRead,
            FuelSelRightRead,
            PropRpm1Read,
            PropRpm2Read,
            DA40FuelTankSelectorRead,
            DA40FuelTankSelector2Read,
            DA40FuelTankSelector3Read,
            DA40FuelTankSelector4Read,
            C400JunctionSettingRead,
            DA62AvionicsLineConnRead,
            C172Avionics1LineConnRead,
            C172Avionics2LineConnRead,
            C172PanelLightPotRead,
            CaravanAvionics1LineConnRead,
            CaravanAvionics2LineConnRead,
            CaravanAltGen2LineConnRead,
            DA62EngineMaster1Read,
            DA62EngineMaster2Read,
            DA40EssentialBus2Read,
            DA40BusConnOn3Read,
            DA40CircuitSwitch7Read,
            DA40EngineMaster1Read,
            CirrusDeiceMasterRead,
            C408FuelPump4Read,
            C408Ignition1Read,
            C408Ignition2Read,
            CaravanIgnition1Read,
            CaravanStarter1Read,
            CaravanDeiceRead,
            BonanzaAntiIcePosRead,
            TbmCrashbar2024Read,
            TbmElecSource2024Read,
            TbmElecGenerator2024Read,
            TbmFuelSelection2024Read,
            TbmApTrim2024Read,
            TbmDeiceCycle1_2024Read,
            TbmDeiceCycle2_2024Read,
            TbmInertSep2024Read,
            MasterBatteryRead,
            Avionics1Read,
            Avionics2Read,
            AltGen1Read,
            AltGen2Read,
            Ign1LeftRead,
            Ign1RightRead,
            Ign1StarterRead,
            Ign2LeftRead,
            Ign2RightRead,
            Ign2StarterRead,
            FuelPump1Read,
            FuelPump2Read,
            FuelPump1_2024Read,
            FuelPump2_2024Read,
            FuelPumpIdx3_2024Read,
            Com1Read,
            Com2Read,
            ComReceiveAllRead,
            DmeSoundRead,
            SpkrSoundRead,
            MkrMuteRead,
            Nav1SoundRead,
            AdfSoundRead,
            Nav2SoundRead,
            ApVsHoldRead,
            ApFlcActiveRead,
            ApWingLevelerRead,
            ApHdgLockRead,
            ApAprHoldRead,
            ApGsHoldRead,
            ApNav1LockRead,
            ApFdActiveRead,
            ApAltLockRead,
            ApVnavBtnRead,
            ApYawDamperRead,
            ApBcHoldRead,
            XfrPushLRead,
            MasterCautionActiveRead,
            MasterWarningActiveRead,
            ElecStarterRead,
            ElecIgnitionRead,
            ApDisengagedRead,
            RudderTrimDisabledRead,
            AileronTrimDisabledRead,
            PropDeiceRead,
            WindshieldDeiceRead,
            Pitot1Read,
            Pitot2Read,
            AntiIce1Read,
            GearCenterRead,
            GearLeftRead,
            GearRightRead,
            GearHandleRead,
            DisplayBackupActiveRead,
            ElecSourceSwitchRead,
            ElecGeneratorSwitchRead,
            ElecCrashbarStateRead,
            Kap140StateRead,
            ApMaxBankRead,
            GyroRead,
            DeiceRead,
            PitotHeatSwitchRead,
            NumEnginesRead,
            CowlFlapLRead,
            CowlFlapRRead,
            CameraStateRead,
            ElecFuelPumpRead,
            PropLeverPosRead,
            FuelValveRead,
            ApMasterRead,
            AileronTrimFracRead,
            IsDeiceAirframeRead,
            IsDeiceAirframe2Read,
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ElecSrcStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct A320BaroStruct { public double value, mode, isInhg, powered; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct A320BaroUnitStruct { public double isInhg; }
        private A320BaroStruct? _a320Baro;
        private XPlaneLink? _xp;                                      // enllaç amb X-Plane 12 (ToLiss)
        private (bool p, int m, double v, bool hg)? _xpBaro;          // última lectura del QNH des d'X-Plane
        private panelAIR1000.FcuState? _xpFcu;
        private bool? _xpAltStep;
        private System.Windows.Forms.Timer? _xpPfdTimer;

        private readonly Dictionary<string, bool> _xpLights = new();
        private readonly Dictionary<string, int> _xpPos = new();       // posicions de selectors des d'X-Plane   // llums de tecles/polsadors des d'X-Plane                                     // selector ALT 100/1000 des d'X-Plane (true = 1000)                       // última lectura de la pantalla del FCU des d'X-Plane   // última lectura del QNH de l'A320 (veure RequestA320Baro)
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct AircraftInfoStruct
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string title;
        }

        // Flaps, separades l'una de l'altra (2026-10-03, Pere: no té sentit ajuntar-les): FLAPS NUM
        // HANDLE POSITIONS és pràcticament constant per avió, es llegeix només en sincronitzar; FLAPS
        // HANDLE INDEX és la posició real, amb floor al grup dels 124 i burst a _flapsActiveReadTimer
        // quan el seu handler detecta un canvi.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FlapsNumPosStruct { public int value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FlapsHandleIndexStruct { public int value; }

        // Trim (ELEVATOR TRIM INDICATOR / RUDDER TRIM PCT), lectura independent de Struct1 i, a més,
        // independent l'una de l'altra (2026-10-02, prova de Pere): cada una es llegeix un cop en
        // sincronitzar i, després, només activament a 33ms quan cal — elevator (vertical, l'únic
        // motoritzat per l'AP) quan AUTOPILOT MASTER és actiu o es mou la seva roda; rudder només quan
        // es mou la seva pròpia roda (mai per l'AP). Veure _elevatorTrimActiveReadTimer /
        // _rudderTrimActiveReadTimer.
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ElevatorTrimStruct { public double value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct RudderTrimStruct { public double value; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FuelTankSelector1Struct { public int value; } // FUEL TANK SELECTOR:1 (Enum)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FuelTankSelector2Struct { public int value; } // FUEL TANK SELECTOR:2 (Enum)

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ThrottleLeverPosStruct { public double value; } // GENERAL ENG THROTTLE LEVER POSITION:1

        // Llums — 10 interruptors independitzats d'1 a 1 (2026-10-03, Pere), fora de Struct1. Cap
        // llum canvia sola, així que no els cal cap "terra" de lectura de fons com el trim/fuel
        // selector de la TBM: sync 1 cop (connexió/canvi d'avió) i, en enviar nosaltres la comanda,
        // una lectura immediata + una altra ~300ms després per confirmar — no calen 2s d'animació
        // activa, una llum és ON/OFF, no té cap recorregut a seguir. Dos structs compartits (mateix
        // layout, registrats per separat a cada definició — "1 a 1" vol dir cap SimVar compartint
        // petició amb una altra, no que cada una necessiti un tipus C# diferent).
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct LightBoolStruct { public int value; }
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct LightNumberStruct { public double value; }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct ParkingBrakeStruct { public int value; } // BRAKE PARKING POSITION (Bool)

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct BatteryVoltageStruct { public double value; } // ELECTRICAL BATTERY VOLTAGE (Volts) — "ELECTRICAL MAIN BUS VOLTAGE" no funciona en aquest avió (sempre 0)

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FuelSelLeftStruct { public double value; } // L:FUEL_Switch_Cutoff_Crossfeed_1 (0=Off, 1=On, 2=Xfeed) — Baron G58. El x100 és només l'animació, no el valor cru.

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct FuelSelRightStruct { public double value; } // L:FUEL_Switch_Cutoff_Crossfeed_2 (0=Off, 1=On, 2=Xfeed) — Baron G58

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PropRpm1Struct { public double value; } // A:PROP RPM:1,Rpm — sincronització DA-62 (RPM>50 => ON)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PropRpm2Struct { public double value; } // A:PROP RPM:2,Rpm — sincronització DA-62, motor dret

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA40FuelTankSelectorStruct { public double value; } // A:FUEL TANK SELECTOR:1,Enum — verificació directa del selector DA-40
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA40FuelTankSelector2Struct { public double value; } // A:FUEL TANK SELECTOR:2,Enum
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA40FuelTankSelector3Struct { public double value; } // A:FUEL TANK SELECTOR:3,Enum — la DA-40 fa servir índex 3 per la bomba, per si el selector també
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA40FuelTankSelector4Struct { public double value; } // A:FUELSYSTEM VALVE SWITCH:4,Enum — hipòtesi: vàlvula dreta de la Caravan (SWITCH:3 no hi correspon)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct C400JunctionSettingStruct { public double value; } // A:FUELSYSTEM JUNCTION SETTING:1,Enum — opció seleccionada del junction del C400 (1=Off,2=Left,3=Right)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA62AvionicsLineConnStruct { public double value; } // A:LINE CONNECTION ON:rh_main_bustoavionics_bus,Bool — estat real del bus d'avionics de la DA-62
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct C172Avionics1LineConnStruct { public double value; } // A:LINE CONNECTION ON:BUS_1_To_AVIONICS_BUS_1,Bool
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct C172Avionics2LineConnStruct { public double value; } // A:LINE CONNECTION ON:BUS_2_To_AVIONICS_BUS_2,Bool
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct C172PanelLightPotStruct { public double value; } // A:LIGHT POTENTIOMETER:3,Percent over 100 — confirmat 2026-08-27: 0=OFF, 100=ON (llum de panell real de la C172)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CaravanAvionics1LineConnStruct { public double value; } // A:LINE CONNECTION ON:AVIONICS_BUS_1_To_PWR_BUS_DISTRIB,Bool
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CaravanAvionics2LineConnStruct { public double value; } // A:LINE CONNECTION ON:AVIONICS_BUS_2_To_PWR_BUS_DISTRIB,Bool
        struct CaravanAltGen2LineConnStruct { public double value; } // A:LINE CONNECTION ON:BATT_BUS_To_ALT_BUS,Bool — interruptor "R ALT GEN 2" (standby alternator)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA62EngineMaster1Struct { public double value; } // A:RECIP ENG ENGINE MASTER SWITCH:1,Bool — feedback real del master engine esquerre DA-62
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA62EngineMaster2Struct { public double value; } // A:RECIP ENG ENGINE MASTER SWITCH:2,Bool — feedback real del master engine dret DA-62
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA40EssentialBus2Struct { public double value; } // L:ELECTRICAL_EssentialBus_2 — interruptor "ALT/GEN 2" real de la DA-40 (no fa res via GENERAL ENG MASTER ALTERNATOR:2)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA40BusConnOn3Struct { public double value; } // A:BUS CONNECTION ON:3,Bool — relè del ESS BUS, valor viu 1 o 2
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA40CircuitSwitch7Struct { public double value; } // A:CIRCUIT SWITCH ON:7,Bool — feedback real del FUEL TRANSFER switch (DA-40 2020)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct DA40EngineMaster1Struct { public double value; } // A:RECIP ENG ENGINE MASTER SWITCH:1,Bool — feedback real del master engine de la DA-40 (monomotor)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TbmCrashbar2024Struct { public double value; } // L:1:TBM_Elec_CrashBar_State,Bool — feedback real del crashbar (TBM930 2024, Inspector Init)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TbmElecSource2024Struct { public double value; } // L:1:TBM_Elec_Source_Switch_State,Number (0/1/2) — feedback real de l'Elec Source (TBM930 2024, Inspector Init)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TbmElecGenerator2024Struct { public double value; } // L:1:TBM_Elec_Generator_Switch_State,Number (0/1/2) — feedback real de l'Elec Generator (TBM930 2024, Inspector Init)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TbmFuelSelection2024Struct { public double value; } // L:1:TBM_Fuel_Selection_Switch_Position,Bool (1=AUTO, 0=MANUAL) — feedback real del Fuel Selection (TBM930 2024, Inspector Init)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TbmApTrim2024Struct { public double value; } // L:1:TBM_AP_Trim_Switch_Position,Number (0=ON/UP, 1=AP OFF/CTR, 2=OFF/DN) — feedback real de l'AP Trim (TBM930 2024, Inspector Init)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TbmDeiceCycle1_2024Struct { public double value; } // L:1:TBM_Airframe_Deice_Cycle_1_Active,Bool — LED esquerra del cicle de deice (TBM930 2024, Updates de DEICE_SYSTEM)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TbmDeiceCycle2_2024Struct { public double value; } // L:1:TBM_Airframe_Deice_Cycle_2_Active,Bool — LED dreta del cicle de deice (TBM930 2024, Updates de DEICE_SYSTEM)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct TbmInertSep2024Struct { public double value; } // L:1:TBM_Inert_Sep_Switch_Position,Bool — feedback real de l'Inert Sep (TBM930 2024, Inspector Init)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CirrusDeiceMasterStruct { public double value; } // L:XMLVAR_DEICE_MASTER,Bool — variable real del deice airframe de la Cirrus SR22T (confirmada al Behavior viewer, secció "Variable(s)")
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct C408FuelPump4Struct { public double value; } // A:FUELSYSTEM PUMP SWITCH:4,Bool — bomba de transferència dreta de la C408 (l'esquerra, índex 3, ja es llegeix via Struct1/read_fuelpump_idx3_2024)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct C408Ignition1Struct { public double value; } // L:XMLVAR_IGNITION_1_1,Bool — variable real de l'AUTO IGNITION L de la C408 (nom complet confirmat a Variables > Global del Behavior viewer)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct C408Ignition2Struct { public double value; } // L:XMLVAR_IGNITION_2_2,Bool — variable real de l'AUTO IGNITION R de la C408
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CaravanIgnition1Struct { public double value; } // A:TURB ENG IGNITION SWITCH EX1:1,Enum — interruptor real d'ignition de la Caravan (0=OFF, !=0=actiu), confirmat al Behavior viewer (Data>Set de l'InputEvent ENGINE_IGNITION_1 la llegeix per decidir si cal cridar K:TURBINE_IGNITION_SWITCH_SET1)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CaravanStarter1Struct { public double value; } // L:XMLVAR_Starter1,Number — interruptor STARTER de la Caravan (0=START, 1=OFF, 2=MOTOR), variable global llegida pel Mouserect real
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct CaravanDeiceStruct { public double value; } // L:DEICE_Airframe_1,Number — posició real del switch de deice de fluid (0=HIGH,1=NORM,2=OFF), confirmat al Set complet de l'InputEvent DEICE_Airframe_1 (2026-08-26, MSFS2020)
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct BonanzaAntiIcePosStruct { public double value; } // GENERAL ENG ANTI ICE POSITION:1,Percent — "Defrost Pull ON" de la Bonanza a MSFS2020, progressiu (no 0/1)

        enum REQUESTS
        {
            RequestAircraftTitle,
            RequestA320Baro,
            RequestFlapsNumPos,
            RequestFlapsHandleIndex,
            RequestElevatorTrimPos,
            RequestRudderTrimPos,
            RequestFuelTankSelector1,
            RequestFuelTankSelector2,
            RequestThrottleLeverPos,
            RequestLightBeacon,
            RequestLightLand,
            RequestLightTaxi,
            RequestLightNav,
            RequestLightStrobe,
            RequestLightRecogn,
            RequestLightPanel,
            RequestLightLogo,
            RequestLightingLogo1,
            RequestLightingWing1,
            RequestParkingBrake,
            RequestBatteryVoltage,
            RequestFuelSelLeft,
            RequestFuelSelRight,
            RequestPropRpm1,
            RequestPropRpm2,
            RequestDA40FuelTankSelector,
            RequestDA40FuelTankSelector2,
            RequestDA40FuelTankSelector3,
            RequestDA40FuelTankSelector4,
            RequestC400JunctionSetting,
            RequestDA62AvionicsLineConn,
            RequestC172Avionics1LineConn,
            RequestC172Avionics2LineConn,
            RequestC172PanelLightPot,
            RequestCaravanAvionics1LineConn,
            RequestCaravanAvionics2LineConn,
            RequestCaravanAltGen2LineConn,
            RequestDA62EngineMaster1,
            RequestDA62EngineMaster2,
            RequestDA40EssentialBus2,
            RequestDA40BusConnOn3,
            RequestDA40CircuitSwitch7,
            RequestDA40EngineMaster1,
            RequestCirrusDeiceMaster,
            RequestC408FuelPump4,
            RequestC408Ignition1,
            RequestC408Ignition2,
            RequestCaravanIgnition1,
            RequestCaravanStarter1,
            RequestCaravanDeice,
            RequestBonanzaAntiIcePos,
            RequestTbmCrashbar2024,
            RequestTbmElecSource2024,
            RequestTbmElecGenerator2024,
            RequestTbmFuelSelection2024,
            RequestTbmApTrim2024,
            RequestTbmDeiceCycle1_2024,
            RequestTbmDeiceCycle2_2024,
            RequestTbmInertSep2024,
            RequestMasterBattery,
            RequestAvionics1,
            RequestAvionics2,
            RequestAltGen1,
            RequestAltGen2,
            RequestIgn1Left,
            RequestIgn1Right,
            RequestIgn1Starter,
            RequestIgn2Left,
            RequestIgn2Right,
            RequestIgn2Starter,
            RequestFuelPump1,
            RequestFuelPump2,
            RequestFuelPump1_2024,
            RequestFuelPump2_2024,
            RequestFuelPumpIdx3_2024,
            RequestCom1,
            RequestCom2,
            RequestComReceiveAll,
            RequestDmeSound,
            RequestSpkrSound,
            RequestMkrMute,
            RequestNav1Sound,
            RequestAdfSound,
            RequestNav2Sound,
            RequestApVsHold,
            RequestApFlcActive,
            RequestApWingLeveler,
            RequestApHdgLock,
            RequestApAprHold,
            RequestApGsHold,
            RequestApNav1Lock,
            RequestApFdActive,
            RequestApAltLock,
            RequestApVnavBtn,
            RequestApYawDamper,
            RequestApBcHold,
            RequestXfrPushL,
            RequestMasterCautionActive,
            RequestMasterWarningActive,
            RequestElecStarter,
            RequestElecIgnition,
            RequestApDisengaged,
            RequestRudderTrimDisabled,
            RequestAileronTrimDisabled,
            RequestPropDeice,
            RequestWindshieldDeice,
            RequestPitot1,
            RequestPitot2,
            RequestAntiIce1,
            RequestGearCenter,
            RequestGearLeft,
            RequestGearRight,
            RequestGearHandle,
            RequestDisplayBackupActive,
            RequestElecSourceSwitch,
            RequestElecGeneratorSwitch,
            RequestElecCrashbarState,
            RequestKap140State,
            RequestApMaxBank,
            RequestGyro,
            RequestDeice,
            RequestPitotHeatSwitch,
            RequestNumEngines,
            RequestCowlFlapL,
            RequestCowlFlapR,
            RequestCameraState,
            RequestElecFuelPump,
            RequestPropLeverPos,
            RequestFuelValve,
            RequestApMaster,
            RequestAileronTrimFrac,
            RequestIsDeiceAirframe,
            RequestIsDeiceAirframe2,
        }
        // Missatger genèric RPN — Client Data Area compartida amb el WASM (SFS_Link/Module.cpp).
        // Permet enviar qualsevol text RPN (K:, H:, B:, índexs, 'nom'_n, etc.) sense recompilar el
        // WASM per cada interruptor nou: el WASM només l'executa tal qual amb execute_calculator_code.
        enum CLIENT_DATA_ID
        {
            GenericRpnData
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        struct GenericRpnStruct
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string rpn;
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
            FUELSYSTEM_PUMP_ON,
            FUELSYSTEM_PUMP_OFF,
            SFS_FUELPUMP1_HIGH,
            SFS_FUELPUMP1_LOW,
            SFS_FUELPUMP1_OFF,
            SFS_FUELPUMP2_HIGH,
            SFS_FUELPUMP2_LOW,
            SFS_FUELPUMP2_OFF,
            SFS_CARAVAN_FUELPUMP1_ON,
            SFS_CARAVAN_FUELPUMP1_AUTO,
            SFS_CARAVAN_FUELPUMP1_OFF,
            SFS_FUELSEL_LEFT_OFF,
            SFS_FUELSEL_LEFT_ON,
            SFS_FUELSEL_LEFT_XFEED,
            SFS_FUELSEL_RIGHT_OFF,
            SFS_FUELSEL_RIGHT_ON,
            SFS_FUELSEL_RIGHT_XFEED,
            DA62_FUELVALVE_LEFT_SET,
            SFS_DA62_FUELVALVE_LEFT_ON,
            SFS_DA62_FUELVALVE_LEFT_CROSS,
            SFS_DA62_FUELVALVE_LEFT_OFF,
            SFS_DA62_FUELVALVE_RIGHT_ON,
            SFS_DA62_FUELVALVE_RIGHT_CROSS,
            SFS_DA62_FUELVALVE_RIGHT_OFF,
            SFS_DA40_FUELSEL_NORMAL,
            SFS_DA40_FUELSEL_EMERGENCY,
            SFS_DA40_FUELSEL_OFF,
            SFS_DA40_FUELSEL_TEST,
            SFS_FUELTRIGGER_TEST,
            SFS_DA62_AVIONICS_TOGGLE,
            SFS_C172_AVIONICS1_SET,
            SFS_C172_AVIONICS2_SET,
            SFS_CARAVAN_AVIONICS1_SET,
            SFS_CARAVAN_AVIONICS2_SET,
            SFS_CARAVAN_ALTGEN1_SET,
            SFS_CARAVAN_ALTGEN2_SET,
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
            STARTER2_SET,
            ENGINE_MASTER_1_SET,
            ENGINE_MASTER_2_SET,
            AUTOPILOT_DISENGAGE_TOGGLE,
            A32NX_BARO_L_INC, A32NX_BARO_L_DEC, A32NX_BARO_L_PUSH, A32NX_BARO_L_PULL,
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
            FUELSYSTEM_TRIGGER_ON,
            FUELSYSTEM_TRIGGER_OFF,

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
            public Func<Rectangle, bool>? IsHiddenByCover;
            public Func<Point, Point>? TransformMouse;
            public bool ShowAllAreas = false;
            // PROVA PROVISIONAL (2026-10-03, Pere): avisa frmMain que s'ha enviat una comanda, pel
            // rectangle negre/verd de l'AUX.
            public Action? OnAnyClick;

            public UiSurface(PictureBox pb)
            {
                Pb = pb;
                pb.MouseDown += (s, e) => OnMouseDown(s, TranslateForSurface(e));
                pb.MouseMove += (s, e) => OnMouseMove(s, TranslateForSurface(e));
                pb.Paint += OnPaint;
            }

            private MouseEventArgs TranslateForSurface(MouseEventArgs e)
            {
                if (TransformMouse == null) return e;
                var pt = TransformMouse(e.Location);
                return new MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta);
            }

            private void OnMouseDown(object? sender, MouseEventArgs e)
            {
                if (SuppressInput?.Invoke() == true) return;
                if (e.Button != MouseButtons.Left) return;

                foreach (var b in Buttons)
                {
                    if (!b.IsVisible()) continue;
                    if (b.ClickRect.Contains(e.Location))
                    {
                        b.PressedUntilUtc = DateTime.UtcNow.AddMilliseconds(b.PressVisualMs);
                        b.OnClick?.Invoke();
                        OnAnyClick?.Invoke();
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
                using var allAreasPen = new Pen(Color.Orange, 2);
                var now = DateTime.UtcNow;
                foreach (var b in Buttons)
                {
                    if (!b.IsVisible()) continue;
                    if (ShowAllAreas && !(HideAreasWhenActive?.Invoke() ?? false)
                        && !(IsHiddenByCover?.Invoke(b.ClickRect) ?? false))
                    {
                        if (b.IsRound)
                        {
                            e.Graphics.FillEllipse(allAreasBrush, b.ClickRect);
                            e.Graphics.DrawEllipse(allAreasPen, b.ClickRect);
                        }
                        else
                        {
                            e.Graphics.FillRectangle(allAreasBrush, b.ClickRect);
                            e.Graphics.DrawRectangle(allAreasPen, b.ClickRect);
                        }
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
            // Si no és null i torna false, el dispatch de clics salta aquest overlay (no "roba" el clic)
            // encara que Bounds el contingui, deixant-lo passar cap a overlays de sota amb la mateixa zona.
            public Func<bool>? OnClickEnabled;
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
        private double _cameraState = 0;
        private int _masterCautionActive = 0;
        private int _masterWarningActive = 0;
        private int _rudderTrimDisabled = 0;
        private int _aileronTrimDisabled = 0;
        private double _lightingLogo1 = 0;
        private bool _tbmPulseLight2024On = false;
        private double _lightingWing1 = 0;
        private int _lightLogo = 0;
        private int _circuitConn17 = 0;
        private double _fuelSelectionSet = 2;

        // Prova provisional (2026-10-02, Pere): desactiva només el DIBUIX dels 4 indicadors (cercle)
        // de trim, deixant la roda (imatge) intacta — per aïllar si el cost de FPS en arrossegar/AP
        // ve de l'indicador o de la roda. Treure quan s'acabi la prova.
        private const bool DEBUG_DISABLE_TRIM_INDICATORS = false; // prova acabada: la roda era el cost real, no l'indicador

        private bool calibrationMode = false;
        private System.Windows.Forms.Button? _configBtn;
        private bool  _configBtnDragging = false;
        private Point _configBtnDragOffset;
        private ConfigurationForm? _configForm;
        private bool  _closeBtnDragging = false;
        private Point _closeBtnDragOffset;
        private bool  _showAreasBtnDragging = false;
        private Point _showAreasBtnDragOffset;
        private Point _showAreasBtnPos;
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

        private System.Windows.Forms.Button _g3000Btn = null!;
        private bool  _g3000BtnDragging;
        private Point _g3000BtnDragOffset;
        private System.Windows.Forms.Button _g1000Btn = null!;
        private bool  _g1000BtnDragging;
        private Point _g1000BtnDragOffset;
        // Mode A320: overhead dibuixat 100% vectorial (A320Overhead.cs), sense PNG. Es crea la
        // primera vegada que s'activa; en sortir s'amaga i allibera el seu bitmap.
        private System.Windows.Forms.Button _a320Btn = null!;
        private bool  _a320BtnDragging;
        private Point _a320BtnDragOffset;
        private bool  _a320Active = false;
        private panelAIR1000.A320OverheadControl? _a320Overhead;
        // Posició (calibració F12) dels botons de zoom + / − de l'overhead; null = per defecte a baix a la dreta.
        private Point? _a320ZoomPlusPos, _a320ZoomMinusPos, _a320PageBtnPos, _a320AreasBtnPos;

        private bool  _tb21Dragging;
        private Point _tb21DragOffset;
        private DateTime _lastRepaint = DateTime.MinValue;
        // Llindar de repintat per a la roda de trim (2026-10-02): la dada es llegeix tan sovint com
        // cal (33ms actiu / immediata en cada comanda), però el repintat es limita i es retalla a la
        // zona de la roda — sense això, arrossegant o amb l'AP actiu, cada lectura disparava un
        // Invalidate() sencer sense cap límit de freqüència (fins a 30+/s), exactament el mateix
        // problema dels flaps d'abans, confirmat per Pere amb mesura real (~12/8 FPS perduts).
        private DateTime _lastElevatorTrimInvalidate = DateTime.MinValue;
        private DateTime _lastRudderTrimInvalidate = DateTime.MinValue;
        // Timer dedicat pel rotatiu de combustible animat del TBM/G3000 (G3K_FSEL_SW) — trobat
        // 2026-10-03 (Pere): primer ho vaig arreglar amb retallat+llindar dins del mateix Paint
        // (com la primera prova dels flaps), però és el mateix patró fràgil que ja vam descartar
        // allà: si un cicle de Paint arriba abans del llindar, no programa el següent i la cadena
        // es trenca en silenci — l'únic que feia avançar la rotació eren repintats incidentals
        // d'altres llocs de l'app (d'aquí els salts de 5-10° cada 1-4s que notava Pere). Mateixa
        // solució definitiva que els flaps: un Timer propi, no lligat al Paint.
        private System.Windows.Forms.Timer? _g3kFuelSelAnimTimer;
        // Trobat 2026-10-03 (Pere): SendTrimTick/SendRudderTrimTick feien una lectura immediata a
        // CADA tic (cada 8/48px de dit), sense cap límit — això s'executa moltes vegades per cada
        // MouseMove si el dit es mou ràpid, ignorant per complet l'interval del timer actiu (per
        // això canviar-lo de 33 a 100ms no feia res: el coll d'ampolla real era aquest, no el
        // timer). Ara només es fa la immediata al primer tic de cada arrossegament; la resta els
        // porta el timer actiu al seu ritme.
        private bool _elevatorTrimAwaitingFirstTick = false;
        private bool _rudderTrimAwaitingFirstTick = false;

        // PROVA PROVISIONAL (2026-10-03, Pere): marca el moment de l'última comanda enviada a MSFS,
        // per al rectangle negre/verd de l'AUX i per pujar el floor dels 124 de 1000ms a 200ms a
        // l'instant (en lloc d'esperar fins a 1000ms que el propi timer se n'adoni sol). Veure
        // comentari on es crea el rectangle per l'abast exacte (no cobreix tots els punts d'enviament
        // del codi, només els principals). Torna a 1000ms sol als 2s sense cap comanda nova — veure
        // PollLowFrequencySimData.
        private DateTime _lastCommandSentUtc = DateTime.MinValue;
        private void MarkCommandSent()
        {
            _lastCommandSentUtc = DateTime.UtcNow;
            if (_dataPollTimer != null && _dataPollTimer.Interval != 200)
            {
                _dataPollTimer.Interval = 200;
                _dataPollTimer.Stop();
                _dataPollTimer.Start();
            }
        }

        private Form? _popoutReminderForm;
        private Label? _popoutReminderMsgLbl;
        private Form? _noAircraftMessageForm;
        private string? _noAircraftMessageDismissedFor;

        // Overlay genèric dins frmMain per a avisos que abans feien servir MessageBox.Show
        // (p. ex. el recordatori del mod TBM930): un MessageBox modal pot quedar amagat darrere
        // de MSFS en Full Screen i penjar en aparença l'app. Es col·loca sempre a la part de dalt
        // de la pantalla, lluny de l'AUX (que viu a Y≈1160 en endavant) i del botó ⚙ (Y=1040).
        private Panel _mainOverlayCard = null!;
        private Label _mainOverlayMsgLbl = null!;
        private Button _mainOverlayPrimaryBtn = null!;
        private Button _mainOverlaySecondaryBtn = null!;
        private Action? _mainOverlayOnPrimaryClick;
        private Form? _overlayPanel;
        private Panel? _fsConnPanel;
        private Panel? _appVersionPanel;
        private int _demoPaintDebugCount = 0;
        private System.Windows.Forms.Timer _demoDisconnectedRefreshTimer = null!;
        private bool _appVersionDragging = false;
        private Point _appVersionDragOffset;

        // Versió de panelAIR App mostrada a l'overlay. Bump manual a cada compilació de distribuïble.
        private const string AppVersion = "00.00";
        private bool _taxiPropActive = false;
        private bool _thrSliderFollow = false;
        private bool _thrQuadrantVisible = false;
        private System.Windows.Forms.Button _thrQuadrantBtn = null!;
        private bool _thrHandleDragging = false;
        private int _thrHandleDragOffsetY;
        private int _thrHandleDragOffsetX;
        private int _thrHandleDefaultY = -1;

        // GENERAL ENG THROTTLE LEVER POSITION:1 — lectura pròpia i independent de Struct1
        // (2026-10-03, Pere: abans anava dins Struct1, cada 10s; un joystick extern movent el
        // throttle només es veia 1 cop cada 10s). Model unificat: 1Hz de fons sempre (cal per
        // detectar moviments d'un joystick extern, que no passen per cap OnClick nostre) i, en
        // detectar un canvi real, activa a 50ms amb la regla dels 2s (com els flaps): s'allarga 2s
        // cada cop que torna a canviar, es para sola als 2s sense cap canvi.
        private double _thrLeverPos = 0;
        // Llindar de repintat pel handle visual del throttle (2026-10-03, Pere: moure la palanca
        // costava ~10 FPS) — mateix bug que flaps/trim/rotatiu de combustible: el codi original
        // (d'abans d'avui) feia pbMain.Invalidate() sencer sense cap límit a cada lectura; abans no
        // es notava perquè thrLeverPos només arribava cada 10s, ara que arriba cada 50ms calia
        // retallar-ho i limitar-ho igual que a tot arreu més avui.
        private DateTime _lastThrHandleInvalidate = DateTime.MinValue;
        // Mateix llindar per als 2 últims "arrossegables" trobats sense retallar/limitar (2026-10-03,
        // Pere): la roda FLC/VS de l'AP (purament visual, no llegeix res de MSFS) i el handle del
        // throttle quan s'arrossega directament amb el dit (diferent del cas ja arreglat, que era
        // la lectura des de MSFS).
        private DateTime _lastG3kApSliderInvalidate = DateTime.MinValue;
        private DateTime _lastThrHandleDragInvalidate = DateTime.MinValue;
        // Rotatius de clic (2026-10-03, Pere): claus d'ignició i selectors de combustible — cada clic
        // repintava el panell sencer sense cap retall ni llindar; igual que la resta, invisible amb un
        // clic sol però car si el maquinari físic envia molts clics ràpids seguits en girar.
        private DateTime _lastIgn1Invalidate = DateTime.MinValue;
        private DateTime _lastIgn2Invalidate = DateTime.MinValue;
        private DateTime _lastFuel1Invalidate = DateTime.MinValue;
        private DateTime _lastFuel2Invalidate = DateTime.MinValue;
        private DateTime _lastSeSelectorInvalidate = DateTime.MinValue;
        private double _propLeverPosCached = 0;   // mirall de s1.propLeverPos, només per aquest ús
        private int _fuelValveCached = 0;          // mirall de s1.fuelValve, només per aquest ús
        private System.Windows.Forms.Timer? _thrLeverActiveReadTimer;
        private DateTime _thrLeverActiveReadUntilUtc = DateTime.MinValue;
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



        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct XPNDRWrite
        {
            public int xpdr_state;   // 0=OFF,1=STBY,2=TEST,3=ON,4=ALT
        }
        struct CameraStateWrite { public double value; }
        public frmMain()
        {
            InitializeComponent();

            // DoubleBuffered del formulari (Designer) només evita parpelleig del propi Form — NO
            // es propaga a controls fills com pbMain, on es fa tot el dibuix pesat. Sense això,
            // en mode DEMO (l'únic lloc on la finestra es redimensiona de debò) cada fotograma
            // intermedi de l'arrossegament quedava "apilat" en comptes de substituir l'anterior.
            // DoubleBuffered és protected a Control: cal activar-ho per reflexió des de fora.
            typeof(Control).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, pbMain, new object[] { true });

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
            using (var s = asm.GetManifestResourceStream("panelAIR1000.Resources.main GA_1000 4K panel_h.png"))
                if (s != null) { using var src = Image.FromStream(s); _bgImage = new Bitmap(src); }
            using (var s = asm.GetManifestResourceStream("panelAIR1000.Resources.main GA_3000 4K panel_m.png"))
                if (s != null) { using var src = Image.FromStream(s); _bgG3000Image = new Bitmap(src); }

            pbMain.Paint += PbMain_Paint;
            // En mode demo la finestra es redimensiona i el dibuix es fa a escala (veure PbMain_Paint);
            // aquí traduïm la posició del clic "real" a la posició equivalent al full virtual 3840x2160
            // abans que arribi a la lògica de detecció de botons, que no canvia gens.
            pbMain.MouseDown += (s, e) => PbMain_MouseDown(s, TranslateMouseEventArgs(e));
            pbMain.MouseMove += (s, e) => PbMain_MouseMove(s, TranslateMouseEventArgs(e));
            pbMain.MouseUp += (s, e) => PbMain_MouseUp(s, TranslateMouseEventArgs(e));

            mainSurface = new UiSurface(pbMain);
            mainSurface.OnAnyClick = MarkCommandSent; // PROVA PROVISIONAL (2026-10-03, Pere): rectangle CMD
            mainSurface.TransformMouse = ToVirtualCoords;
            mainSurface.HideAreasWhenActive = () => _g3000Active;
            mainSurface.IsHiddenByCover = IsHiddenByActiveCover;
            uiSurfaces.Add(mainSurface);
            mainSurface.SuppressInput = () => calibrationMode || _g3000Active;

            this.KeyPreview = true;
            Action<KeyEventArgs> handleF12 = (e) => { if (e.KeyCode == Keys.F12) { calibrationMode = !calibrationMode; calibSelectedBtn = null; calibSelectedTri = null; calibSelectedOverlay = null; calibZoomBox.Visible = false; pbMain.Invalidate(); if (_a320Overhead != null) { _a320Overhead.CalibrationMode = calibrationMode; _a320Overhead.Invalidate(); } } };
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

            if (IsDemoMode)
            {
                // Finestra normal, redimensionable, oberta al 80% de l'alçada del monitor
                // principal de Windows (proporció 3840:2160). Un cop oberta, l'usuari la pot
                // moure/redimensionar/maximitzar lliurement a qualsevol dels seus monitors.
                var wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080);
                double aspect = 3840.0 / 2160.0;
                int targetH = (int)(wa.Height * 0.8);
                int targetW = (int)(targetH * aspect);
                if (targetW > wa.Width * 0.95)
                {
                    targetW = (int)(wa.Width * 0.95);
                    targetH = (int)(targetW / aspect);
                }
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.MaximizeBox = true;
                this.MinimumSize = new Size(800, 450);
                this.StartPosition = FormStartPosition.Manual;
                this.Size = new Size(targetW, targetH);
                this.Location = new Point(wa.X + (wa.Width - targetW) / 2, wa.Y + (wa.Height - targetH) / 2);
                pbMain.Dock = DockStyle.Fill;
                this.Text = "panelAIR — DEMO";
                // Diagnòstic temporal: la finestra demo surt trencada en algun entorn concret
                // (monitor únic, no 4K) — registrem els valors reals calculats per saber si el
                // problema és una mida/posició incorrecta abans de seguir endevinant a cegues.
                try
                {
                    System.IO.File.AppendAllText("panelAIR_App_crash.txt",
                        $"\r\n[DEMO DEBUG] AllScreens={Screen.AllScreens.Length} PrimaryScreen={(Screen.PrimaryScreen == null ? "NULL" : "OK")} " +
                        $"wa={wa} targetW={targetW} targetH={targetH} ThisSize={this.Size} ThisLocation={this.Location} " +
                        $"ControlBox={this.ControlBox} MinimizeBox={this.MinimizeBox} MaximizeBox={this.MaximizeBox} FormBorderStyle={this.FormBorderStyle}");
                }
                catch { }
                this.Shown += (s, e) =>
                {
                    try
                    {
                        System.IO.File.AppendAllText("panelAIR_App_crash.txt",
                            $"\r\n[DEMO DEBUG Shown] ThisBounds={this.Bounds} WindowState={this.WindowState} pbMainSize={pbMain.Size}");
                    }
                    catch { }
                };
                // Els forats del PFD/MFD (i TSC en G3000) depenen de l'escala actual — recalcular-los
                // cada vegada que la finestra es redimensiona (arrossegar-la sola no cal: la Region
                // és relativa al propi Form, es mou sola amb la finestra).
                pbMain.SizeChanged += (s, e) => ApplyCurrentRegion();
                // El primer càlcul (fet just aquí, dins el constructor) pot agafar encara la mida
                // "de fàbrica" de pbMain (3840x2160) abans que el Dock=Fill s'apliqui de veritat.
                // Es recalcula un cop més quan la finestra ja s'ha mostrat, amb la mida definitiva.
                this.Shown += (s, e) => ApplyCurrentRegion();
            }
            else
            {
                var screen4k = Screen.AllScreens.FirstOrDefault(s => !s.Primary && s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                    ?? Screen.AllScreens.First(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160);

                this.StartPosition = FormStartPosition.Manual;
                this.Location = screen4k.Bounds.Location;
                this.Size = screen4k.Bounds.Size;
                this.TopMost = true;
            }

            // En mode DEMO, cridar-ho aquí (abans que la finestra tingui una posició real a la
            // pantalla) calcula malament l'offset de pbMain (pbMain.PointToScreen torna un valor
            // fals pre-Shown) i deixa un "forat" amb coordenades disbaratades aplicat a la Region
            // — la finestra es veu trencada des del primer fotograma. El handler de Shown (més
            // amunt) ja el torna a calcular bé un cop la finestra és real; no cal fer-ho aquí també.
            if (!IsDemoMode) ApplyPfdRegion();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { System.IO.File.WriteAllText("panelAIR_App_crash.txt", e.ExceptionObject.ToString()); } catch { }
            };
            Application.ThreadException += (s, e) =>
            {
                try { System.IO.File.AppendAllText("panelAIR_App_crash.txt", "\r\n[UI] " + e.Exception.ToString()); } catch { }
            };

            this.Icon = Resources.logo;

            _overlayPanel = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                BackColor = Color.FromArgb(40, 40, 40),
                ShowInTaskbar = false,
                Size = new Size(3440, 275),
                Location = new Point(this.Left + 200, this.Top + 1160)
            };
            _overlayPanel.KeyPreview = true;
            _overlayPanel.KeyDown += (s, e) => handleF12(e);
            _overlayPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.Orange, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _overlayPanel.Width - 1, _overlayPanel.Height - 1);
            };

            _configBtn = new System.Windows.Forms.Button
            {
                Text = "⚙  SETUP WIZARD",
                Location = new Point(900, 180),
                Size = new Size(363, 63),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 26f, FontStyle.Bold, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            _configBtn.FlatAppearance.BorderSize = 2;
            _configBtn.FlatAppearance.BorderColor = Color.White;
            _configBtn.Click += (s, e) =>
            {
                if (calibrationMode) return;
                OpenConfigurationScreen();
            };
            _configBtn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _configBtnDragging = true;
                _configBtnDragOffset = e.Location;
            };
            _configBtn.MouseMove += (s, e) =>
            {
                if (!_configBtnDragging) return;
                var screenPt = _configBtn.PointToScreen(e.Location);
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _configBtn.Location = new Point(formPt.X - _configBtnDragOffset.X, formPt.Y - _configBtnDragOffset.Y);
            };
            _configBtn.MouseUp += (s, e) => { _configBtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _overlayPanel.Controls.Add(_configBtn);

            _fsConnPanel = new Panel
            {
                Location = new Point(1870, 30),
                Size = new Size(363, 186),
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
            _aircraftTitleLbl = new Label
            {
                Text = "—",
                Location = new Point(5, 121),
                Size = new Size(353, 55),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Emoji", 18f, FontStyle.Bold, GraphicsUnit.Pixel),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(40, 40, 40),
                AutoEllipsis = true
            };
            _fsConnPanel.Controls.Add(_fsConnTitle);
            _fsConnPanel.Controls.Add(textBox21);
            _fsConnPanel.Controls.Add(_aircraftTitleLbl);

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

            _appVersionPanel = new Panel { BackColor = Color.Transparent };
            var appVerFont = new Font("Segoe UI Emoji", 26f, FontStyle.Bold, GraphicsUnit.Pixel);
            var _appVersionPrefixLbl = new Label
            {
                Text = "panelAIR App version: v",
                AutoSize = true,
                Location = new Point(0, 0),
                Font = appVerFont,
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            _appVersionPanel.Controls.Add(_appVersionPrefixLbl);
            var _appVersionNumberLbl = new Label
            {
                Text = AppVersion,
                AutoSize = true,
                Location = new Point(_appVersionPrefixLbl.Right, 0),
                Font = appVerFont,
                ForeColor = Color.DodgerBlue,
                BackColor = Color.Transparent
            };
            _appVersionPanel.Controls.Add(_appVersionNumberLbl);
            _appVersionPanel.Size = new Size(_appVersionNumberLbl.Right, _appVersionPrefixLbl.Height);
            _appVersionPanel.Location = new Point(
                _fsConnPanel.Location.X + (_fsConnPanel.Width - _appVersionPanel.Width) / 2,
                _fsConnPanel.Location.Y + _fsConnPanel.Height + 6);

            void AppVersionDragStart(object? s, MouseEventArgs e)
            {
                if (!calibrationMode) return;
                _appVersionDragging = true;
                _appVersionDragOffset = e.Location;
            }
            void AppVersionDragMove(object? s, MouseEventArgs e)
            {
                if (!_appVersionDragging) return;
                var screenPt = ((Control)s!).PointToScreen(e.Location);
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _appVersionPanel.Location = new Point(formPt.X - _appVersionDragOffset.X, formPt.Y - _appVersionDragOffset.Y);
            }
            void AppVersionDragEnd(object? s, MouseEventArgs e) { _appVersionDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } }

            foreach (Control c in new Control[] { _appVersionPanel, _appVersionPrefixLbl, _appVersionNumberLbl })
            {
                c.MouseDown += AppVersionDragStart;
                c.MouseMove += AppVersionDragMove;
                c.MouseUp += AppVersionDragEnd;
            }
            _overlayPanel.Controls.Add(_appVersionPanel);

            _g3000Btn = MakeGroupBtn("TBM 930 Cockpit", 363, 63);
            _g3000Btn.Location = new Point(3100, 100);
            _g3000Btn.Click += (s, e) => { SetA320Active(false); SetG3000Active(true); };
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

            _g1000Btn = MakeGroupBtn("G1000 Cockpit", 363, 63);
            _g1000Btn.Location = new Point(_g3000Btn.Location.X, _g3000Btn.Location.Y + _g3000Btn.Height + 10);
            _g1000Btn.Click += (s, e) => { SetA320Active(false); SetG3000Active(false); };
            _g1000Btn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _g1000BtnDragging = true;
                _g1000BtnDragOffset = e.Location;
            };
            _g1000Btn.MouseMove += (s, e) =>
            {
                if (!_g1000BtnDragging) return;
                var screenPt = _g1000Btn.PointToScreen(e.Location);
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _g1000Btn.Location = new Point(formPt.X - _g1000BtnDragOffset.X, formPt.Y - _g1000BtnDragOffset.Y);
            };
            _g1000Btn.MouseUp += (s, e) => { _g1000BtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _overlayPanel.Controls.Add(_g1000Btn);

            _a320Btn = MakeGroupBtn("A320", 363, 63);
            _a320Btn.Location = new Point(_g1000Btn.Location.X, _g1000Btn.Location.Y + _g1000Btn.Height + 10);
            _a320Btn.Click += (s, e) => SetA320Active(true);
            _a320Btn.MouseDown += (s, e) =>
            {
                if (!calibrationMode) return;
                _a320BtnDragging = true;
                _a320BtnDragOffset = e.Location;
            };
            _a320Btn.MouseMove += (s, e) =>
            {
                if (!_a320BtnDragging) return;
                var screenPt = _a320Btn.PointToScreen(e.Location);
                var formPt = _overlayPanel!.PointToClient(screenPt);
                _a320Btn.Location = new Point(formPt.X - _a320BtnDragOffset.X, formPt.Y - _a320BtnDragOffset.Y);
            };
            _a320Btn.MouseUp += (s, e) => { _a320BtnDragging = false; try { ExportCalibrationPositions(silent: true); } catch { } };
            _overlayPanel.Controls.Add(_a320Btn);
            UpdateCockpitModeButtons();

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
            // Posició per defecte: just a sota del botó "SETUP WIZARD" (900,180 + 63 d'alçada + 10),
            // perquè sigui sempre visible i previsible encara que el calibratge guardat falli per
            // qualsevol motiu. Pere el reposicionarà amb F12 un cop confirmat que es veu.
            closeButton.Location = new Point(900, 253);
            closeButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _overlayPanel.Controls.Add(closeButton);


            _gearBtn = new System.Windows.Forms.Button
            {
                Text = "⚙",
                Location = new Point(3477, 1040),
                Size = new Size(63, 63),
                BackColor = Color.FromArgb(40, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Emoji", 34f, FontStyle.Bold, GraphicsUnit.Pixel),
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(6, 2, 0, 0),
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
            // 2026-10-14 (Pere): amagat de nou en mode demo (es va provar a mostrar-lo dibuixat
            // en coordenades virtuals, com DrawShowAreasIcon, i funcionava — Pere ho ha deixat
            // de banda per ara per tancar la primera versió de l'app sense seguir-hi dedicant temps).
            if (IsDemoMode) _gearBtn.Visible = false;

            // ── Overlay genèric d'avisos (substitueix MessageBox.Show per avisos que poden sortir
            // en ple vol, com el recordatori del mod TBM930) ──
            _mainOverlayCard = new Panel
            {
                BackColor = Color.White,
                Size = new Size(2200, 420),
                Location = new Point((this.ClientSize.Width - 2200) / 2, 80),
                Visible = false
            };
            _mainOverlayCard.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(217, 223, 229), 1);
                e.Graphics.DrawRectangle(pen, 0, 0, _mainOverlayCard.Width - 1, _mainOverlayCard.Height - 1);
            };
            this.Controls.Add(_mainOverlayCard);

            _mainOverlayMsgLbl = new Label
            {
                Font = new Font("Segoe UI", 16f),
                ForeColor = Color.FromArgb(22, 32, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 30),
                Size = new Size(_mainOverlayCard.Width - 80, 300)
            };
            _mainOverlayCard.Controls.Add(_mainOverlayMsgLbl);

            _mainOverlaySecondaryBtn = new Button
            {
                Text = "Close",
                Size = new Size(220, 64),
                Location = new Point(880, 340),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(230, 230, 230),
                ForeColor = Color.FromArgb(22, 32, 42),
                Font = new Font("Segoe UI", 15f, FontStyle.Bold)
            };
            _mainOverlaySecondaryBtn.FlatAppearance.BorderSize = 2;
            _mainOverlaySecondaryBtn.FlatAppearance.BorderColor = Color.FromArgb(180, 186, 194);
            _mainOverlaySecondaryBtn.Click += (s, e) => _mainOverlayCard.Visible = false;
            _mainOverlayCard.Controls.Add(_mainOverlaySecondaryBtn);

            _mainOverlayPrimaryBtn = new Button
            {
                Size = new Size(400, 64),
                Location = new Point(1120, 340),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 15f, FontStyle.Bold)
            };
            _mainOverlayPrimaryBtn.FlatAppearance.BorderSize = 0;
            _mainOverlayPrimaryBtn.Click += (s, e) =>
            {
                _mainOverlayCard.Visible = false;
                _mainOverlayOnPrimaryClick?.Invoke();
            };
            _mainOverlayCard.Controls.Add(_mainOverlayPrimaryBtn);

            // ── "Botó" mostrar àrees clicables — dibuixat directament sobre pbMain (no Control, per
            // transparència real; els Button/Label de WinForms no la garanteixen de forma fiable
            // com a fills d'un PictureBox amb contingut dibuixat a mà) — just sota l'engranatge.
            _showAreasBtnPos = new Point(_gearBtn.Location.X, _gearBtn.Location.Y + _gearBtn.Height + 10);

            // ── Helpers de posicionament de grups ──────────────────────────
            void UpdateVolGroupPositions()
            {
                _volUpBtn.Location   = new Point(_volGrpAnchor.X, _volGrpAnchor.Y);
                _volDownBtn.Location = new Point(_volGrpAnchor.X, _volGrpAnchor.Y + 70);
            }
            // ── Helpers MSFS focus + tecles ────────────────────────────────
            // "FlightSimulator" = MSFS2020, "FlightSimulator2024" = MSFS2024 (executables diferents).
            System.Diagnostics.Process? GetMSFSProcess() =>
                System.Diagnostics.Process.GetProcessesByName("FlightSimulator").FirstOrDefault()
                ?? System.Diagnostics.Process.GetProcessesByName("FlightSimulator2024").FirstOrDefault();

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
                int currentCam = (int)_cameraState;
                System.Diagnostics.Debug.WriteLine($"CAMERA STATE actual: {_cameraState}");
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

            //this.TopMost = true;
            this.Load += frmMain_Load;
            this.Shown += (s, e) =>
            {
                // En mode DEMO no hem detectat panell real: no té sentit obrir el Setup Wizard
                // (configura hardware que no hi és). No marquem FirstConfigShown tampoc, perquè
                // s'obri de debò el dia que hi hagi panell real detectat en aquest mateix PC.
                if (IsDemoMode) return;
                if (AppSettings.Instance.FirstConfigShown) return;
                AppSettings.Instance.FirstConfigShown = true;
                AppSettings.Instance.Save();
                OpenConfigurationScreen();
            };
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
            pfdAudio.Buttons.Add(new UiButton { Name = "MKR_MUT",     BasePos = new Point(1867, 442),  OnClick = () => { if (!MKRmute_bool) { simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_MKR_MUTE_ON, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); MKRmute_bool = true; } else { simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_MKR_MUTE_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); MKRmute_bool = false; } } });
            pfdAudio.Buttons.Add(new UiButton { Name = "NAV1",        BasePos = new Point(1972, 520),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_NAV1,         0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "DME",         BasePos = new Point(1867, 520),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_DME,          0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "NAV2",        BasePos = new Point(1972, 598),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_NAV2,         0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
            pfdAudio.Buttons.Add(new UiButton { Name = "ADF",         BasePos = new Point(1867, 598),  OnClick = () => simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AUDIO_ADF,          0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY) });
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
            // DA-40, Cirrus SR22, C408, C400, C172 i Caravan (2026-09-05, confirmat per Pere): exclosos
            // del ressaltat. Nota: el criteri general de "gear només per avions retràctils" queda pendent
            // de repassar per a la resta d'avions (veure conversa sobre l gear).
            cockpitRight.Buttons.Add(new UiButton { Name="LGEAR-UP", BasePos=new Point(2843, 1590), W=110, H=115, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA40() && !IsCirrusSR22() && !IsC408() && !IsC400() && !IsC172() && !IsCaravan(),
                OnClick = () => {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.LGEAR_UP, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="LGEAR-DN", BasePos=new Point(2843, 1750), W=110, H=115, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA40() && !IsCirrusSR22() && !IsC408() && !IsC400() && !IsC172() && !IsCaravan(),
                OnClick = () => {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.LGEAR_DN, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // Avionics switches — single toggle area each
            cockpitRight.Buttons.Add(new UiButton { Name="AVIONICS1-TOGGLE", BasePos=new Point(1770, 1220), W=100, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (IsDA62() && !_isMsfs2024)
                    {
                        // DA-62 (2020, 2026-08-28): el WASM 'rh_main_bustoavionics_bus'_n (confirmat pel
                        // 2024) no fa res aquí. Confirmat via revòlver SEND: K:AVIONICS_MASTER_1_SET natiu
                        // (1=ON, 0=OFF), sense WASM.
                        uint target = avionics1Off ? 1u : 0u;
                        SendGenericRpn($"{target} (>K:AVIONICS_MASTER_1_SET)");
                        pbMain.Invalidate();
                        return;
                    }
                    if (IsDA62())
                    {
                        // DA-62 (2024): sistema elèctric modular, K:ELECTRICAL_LINE_CONNECTION_TOGGLE (nom
                        // de bus, no índex numèric) via WASM — és un TOGGLE real, no ON/OFF, per això no
                        // cal triar event.
                        Debug.WriteLine($">>> DA62 AVIONICS TOGGLE premut, simconnect null? {simconnect == null}");
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_DA62_AVIONICS_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        pbMain.Invalidate();
                        return;
                    }
                    if (IsC172() && _isMsfs2024)
                    {
                        // C172 (MSFS2024): sistema elèctric modular igual que la DA-62, però amb
                        // K:2:ELECTRICAL_LINE_CONNECTION_SET (valor explícit 0/1 + nom de bus
                        // 'BUS_1_To_AVIONICS_BUS_1', via WASM).
                        // MSFS2020: sense branca pròpia — cau a AVIONICS_MASTER_1_ON/OFF de més avall,
                        // confirmat funcional via MobiFlight (K:AVIONICS_MASTER_1_SET).
                        uint val = avionics1Off ? 1u : 0u;
                        Debug.WriteLine($">>> C172 AVIONICS1 SET premut, val={val}, simconnect null? {simconnect == null}");
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_C172_AVIONICS1_SET, val, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        pbMain.Invalidate();
                        return;
                    }
                    if (IsCaravan() && _isMsfs2024)
                    {
                        // Caravan (MSFS2024): mateix mecanisme que la C172, bus 'AVIONICS_BUS_1_To_PWR_BUS_DISTRIB'.
                        // MSFS2020: sense branca pròpia — cau a l'event natiu AVIONICS_MASTER_1_ON/OFF de
                        // més avall, el mecanisme original d'abans d'aquest descobriment.
                        uint val = avionics1Off ? 1u : 0u;
                        Debug.WriteLine($">>> CARAVAN AVIONICS1 SET premut, val={val}, simconnect null? {simconnect == null}");
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_CARAVAN_AVIONICS1_SET, val, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        pbMain.Invalidate();
                        return;
                    }
                    var ev = avionics1Off ? SIMCONNECT_EVENTS.AVIONICS_MASTER_1_ON : SIMCONNECT_EVENTS.AVIONICS_MASTER_1_OFF;
                    simconnect?.TransmitClientEvent(0, ev, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="AVIONICS2-TOGGLE", BasePos=new Point(1970, 1220), W=100, H=120, Resizable=true, NoShadow=true,
                // Cirrus SR22 (2026-09-05, confirmat per Pere, 2020 i 2024): un sol interruptor
                // d'avionics físic, el 2 no existeix — la branca de 2020 ja era exclosa de la lectura;
                // la de 2024 cau al genèric sense cap codi propi, tampoc real.
                // C408 (2026-09-05, confirmat per Pere): no fa res de rellevant en aquest avió (turboprop,
                // no té aquest segon bus).
                // DA-62 (2026-09-05, confirmat per Pere): sistema d'un sol bus combinat d'avionics — no hi
                // ha "avionics 2" real, aquesta branca no té cap codi propi per la DA-62 (cau al genèric).
                // DA-40 (2026-09-05, confirmat per Pere, 2020 i 2024): un sol interruptor d'avionics físic
                // — la branca de 2020 ja era un no-op explícit; la de 2024 cau al genèric sense cap codi
                // propi, tampoc real.
                // C400 (2026-09-05, confirmat per Pere): tampoc té cap codi propi per aquesta branca.
                // Baron G58 (2026-09-05, confirmat per Pere): tampoc en té.
                // Bonanza G36 (2026-09-05, confirmat per Pere): tampoc en té.
                ShowWhen = () => !IsCirrusSR22() && !IsC408() && !IsDA62() && !IsDA40() && !IsC400() && !IsBaronG58() && !IsBonanzaG36(),
                OnClick = () => {
                    if (IsDA40() && !_isMsfs2024)
                    {
                        // DA-40 (2020): l'avió real només té UN interruptor d'avionics — el de la dreta
                        // queda totalment inactiu (ni comanda ni feedback, veure exclusió de lectura més
                        // avall).
                        return;
                    }
                    if (IsC172() && _isMsfs2024)
                    {
                        // MSFS2020: cau a AVIONICS_MASTER_2_ON/OFF de més avall (K:AVIONICS_MASTER_2_SET).
                        uint val = avionics2Off ? 1u : 0u;
                        Debug.WriteLine($">>> C172 AVIONICS2 SET premut, val={val}, simconnect null? {simconnect == null}");
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_C172_AVIONICS2_SET, val, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        pbMain.Invalidate();
                        return;
                    }
                    if (IsCaravan() && _isMsfs2024)
                    {
                        // MSFS2020: cau a AVIONICS_MASTER_2_ON/OFF de més avall (mecanisme original).
                        uint val = avionics2Off ? 1u : 0u;
                        Debug.WriteLine($">>> CARAVAN AVIONICS2 SET premut, val={val}, simconnect null? {simconnect == null}");
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_CARAVAN_AVIONICS2_SET, val, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        pbMain.Invalidate();
                        return;
                    }
                    var ev = avionics2Off ? SIMCONNECT_EVENTS.AVIONICS_MASTER_2_ON : SIMCONNECT_EVENTS.AVIONICS_MASTER_2_OFF;
                    simconnect?.TransmitClientEvent(0, ev, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // ALT/GEN switch 1 — single toggle area (tots els avions excepte la Caravan, que té un
            // interruptor momentani de 3 posicions: veure ALTGEN1-UP/DN just a sota).
            cockpitRight.Buttons.Add(new UiButton { Name="ALTGEN1-TOGGLE", BasePos=new Point(1420, 1220), W=100, H=120, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsCaravan(),
                OnClick = () => {
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // Caravan — interruptor momentani de 3 posicions (RESET/centre/TRIP), repòs al centre.
            // K:TOGGLE_ALTERNATOR1 directe (sense WASM) — confirmat funcional per l'efecte elèctric.
            // Provat DOS camins pel moviment visual, cap dels dos funciona: (1) escriure
            // O:ELECTRICAL_Alternator_1_Position via WASM (les O: d'aquest InputEvent no són accessibles
            // des de fora), (2) disparar (>B:ELECTRICAL_Alternator_1_Reset) tal com fa el Mouserect real —
            // tampoc mou l'agulla. Abandonat el moviment visual (com l'ALTGEN2 en el seu moment); ens
            // quedem només amb l'efecte elèctric real via K:TOGGLE_ALTERNATOR1.
            cockpitRight.Buttons.Add(new UiButton { Name="ALTGEN1-UP", BasePos=new Point(1420, 1170), W=100, H=60, Resizable=true, NoShadow=true,
                ShowWhen = () => IsCaravan(),
                OnClick = () => {
                    // UP -> RESET: engega només si estava apagat.
                    if (altGen1Off)
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    // Flaix cosmètic d'1s a RESET (2026-08-26, a petició de Pere) — la posició 3D real és
                    // inabastable des de fora (veure comentari de sobre), així que això és NOMÉS visual,
                    // no envia cap comanda pròpia ni representa cap estat real.
                    _altGen1ResetAnim = true;
                    pbMain.Invalidate();
                    Task.Delay(1000).ContinueWith(_ => this.BeginInvoke(() => { _altGen1ResetAnim = false; pbMain.Invalidate(); }));
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="ALTGEN1-DN", BasePos=new Point(1420, 1280), W=100, H=60, Resizable=true, NoShadow=true,
                ShowWhen = () => IsCaravan(),
                OnClick = () => {
                    // DN -> TRIP: apaga només si estava engegat.
                    if (!altGen1Off)
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_ALTERNATOR1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    pbMain.Invalidate();
                }});
            // ALT/GEN switch 2 — single toggle area
            cockpitRight.Buttons.Add(new UiButton { Name="ALTGEN2-TOGGLE", BasePos=new Point(1620, 1220), W=100, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (IsCaravan() && _isMsfs2024)
                    {
                        // Caravan (MSFS2024): aquest interruptor no és el segon alternador (monomotor),
                        // és el "R ALT GEN 2" / standby alternator (ELECTRICAL_SWITCH_ALTERNATOR_STBY_1),
                        // ON/OFF real via LINE CONNECTION 'BATT_BUS_To_ALT_BUS'_n.
                        // MSFS2020: sense branca pròpia — cau a l'event natiu TOGGLE_ALTERNATOR2 de més
                        // avall, el mecanisme original d'abans d'aquest descobriment.
                        uint val = altGen2Off ? 1u : 0u;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_CARAVAN_ALTGEN2_SET, val, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        pbMain.Invalidate();
                        return;
                    }
                    if (IsDA40())
                    {
                        if (_isMsfs2024)
                        {
                            // 2024: intacte, no tocat. "ALT/GEN 2" real és L:ELECTRICAL_EssentialBus_2 (bus
                            // interconnect), no GENERAL ENG MASTER ALTERNATOR:2. RPN complet transcrit del
                            // Behavior viewer (tanca la connexió bus principal/essencial si cal, redirigeix
                            // el relè segons l'estat actual de BUS CONNECTION ON:3, i escriu l'estat final).
                            int target = altGen2Off ? 1 : 0;
                            string rpn =
                                "1 (A:1:BUS CONNECTION ON:2, Bool) == if{ 2 1 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) } " +
                                $"{target} if{{ 1 (A:1:BUS CONNECTION ON:3, Bool) if{{ 1 3 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} 2 (A:1:BUS CONNECTION ON:3, Bool) ! if{{ 2 3 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} }} " +
                                $"els{{ 1 (A:1:BUS CONNECTION ON:3, Bool) ! if{{ 1 3 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} 2 (A:1:BUS CONNECTION ON:3, Bool) if{{ 2 3 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} }} " +
                                $"{target} (>L:ELECTRICAL_EssentialBus_2)";
                            SendGenericRpn(rpn);
                        }
                        else
                        {
                            // 2020 — mitja solució (2026-08-28), seqüència de 4 temps confirmada per Pere
                            // (un clic = una crida, alternant "2 3"/"1 3"; el cicle real complet és:
                            // 2 actives -> sense canvi -> esquerra activa/dreta apagada -> les 2 apagades ->
                            // 2 actives). Comanda "real" (només la dreta, sense passar per la resta del
                            // RESOLT DE VERITAT (2026-08-28): Pere ha trobat el preset real de MobiFlight/
                            // HubHop ("Electrical Essential Bus On/Off", Jaime Leon, DA40NG). La peça que
                            // faltava: cal escriure (>A:BUS LOOKUP INDEX, Number) ABANS de cada lectura o
                            // toggle de BUS CONNECTION ON:2/3 — és el que fèiem servir malament com un
                            // prefix "A:1:..." inline (que no fa res de veritat); ara escrivim l'índex de
                            // debò abans de cada comprovació, igual que el preset.
                            int target = altGen2Off ? 1 : 0;
                            string rpn =
                                $"{target} 1 (>A:BUS LOOKUP INDEX, Number) (A:BUS CONNECTION ON:2, Bool) == if{{ 2 1 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} " +
                                $"{target} if{{ 1 (>A:BUS LOOKUP INDEX, Number) (A:BUS CONNECTION ON:3, Bool) if{{ 1 3 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} 2 (>A:BUS LOOKUP INDEX, Number) (A:BUS CONNECTION ON:3, Bool) ! if{{ 2 3 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} }} " +
                                $"els{{ 1 (>A:BUS LOOKUP INDEX, Number) (A:BUS CONNECTION ON:3, Bool) ! if{{ 1 3 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} 2 (>A:BUS LOOKUP INDEX, Number) (A:BUS CONNECTION ON:3, Bool) if{{ 2 3 (>K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE) }} }} " +
                                $"{target} (>L:ELECTRICAL_EssentialBus_2)";
                            SendGenericRpn(rpn);
                        }
                        pbMain.Invalidate();
                        return;
                    }
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
                    InvalidateRotarySwitch("SW_IGNITION", ref _lastIgn1Invalidate);
                }});
            // IGN1 rotate left (counter-clockwise = DECR)
            cockpitRight.Buttons.Add(new UiButton { Name="IGN1-LEFT", BasePos=new Point(1880, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (ign1Pos <= 0) return;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MAGNETO1_DECR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    InvalidateRotarySwitch("SW_IGNITION", ref _lastIgn1Invalidate);
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="IGN2-RIGHT", BasePos=new Point(2170, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (ign2Pos >= 4) return;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MAGNETO2_INCR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    InvalidateRotarySwitch("SW_IGNITION2", ref _lastIgn2Invalidate);
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="IGN2-LEFT", BasePos=new Point(2080, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (ign2Pos <= 0) return;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.MAGNETO2_DECR, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    InvalidateRotarySwitch("SW_IGNITION2", ref _lastIgn2Invalidate);
                }});
            // SW_FUEL1 — left and right areas
            cockpitRight.Buttons.Add(new UiButton { Name="FUEL1-LEFT", BasePos=new Point(1620, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA40() && !IsC400() && !IsC172() && !IsBonanzaG36() && !IsCirrusSR22() && !IsC408(),
                OnClick = () => {
                    if (fuel1Pos <= 0) return;
                    int newPos = fuel1Pos - 1;
                    if (IsCaravan() && _isMsfs2024) {
                        // Caravan (C208B) a MSFS2024: trigger booleà independent, índex 3=Left. Sense
                        // crossfeed (2 estats Off/On), per això aquest selector va de 0 a 1 (vegeu bound
                        // del botó RIGHT). Confirmat via docs oficials: FUELSYSTEM_TRIGGER_ON/OFF són la
                        // variant d'1 paràmetre (només índex) de K:2:FUELSYSTEM_TRIGGER_SET — enviable
                        // directe des de C#, sense WASM. Feedback real confirmat (FUELSYSTEM VALVE
                        // SWITCH:1) — no toquem fuel1Pos aquí, el determina només la lectura.
                        // MSFS2020: sense branca pròpia — cau al fallback genèric monomotor de més avall
                        // (SingleEngineFuelVal + FUEL_SELECTOR_SET), el mecanisme original.
                        var evt = newPos == 1 ? SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_ON : SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_OFF;
                        simconnect?.TransmitClientEvent(0, evt, 3, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    } else if (_isMsfs2024 && IsDA62()) {
                        // DA-62: confirmat amb "Execute" a Behaviors — K:2:FUELSYSTEM_VALVE_SET +
                        // K:2:FUELSYSTEM_TRIGGER_SET (índex 1), NO junctions ni B:FUEL_VALVE_LEFT directe.
                        // Bloquejador del guard (2026-08-31, NOMÉS 2024): a OFF cal desbloquejar ABANS de
                        // moure la vàlvula (el guard físic tancat impedeix el moviment) — per això aquí
                        // esperem 250ms després de desbloquejar abans d'enviar da62Evt. Sortint d'OFF no cal
                        // esperar, es bloqueja després de moure. Per revertir: veure historial git d'aquest bloc.
                        if (newPos == 0) {
                            SendGenericRpn("(>B:FUEL_BLOCKER_LEFT_ON)");
                            Task.Run(async () => {
                                await Task.Delay(250);
                                this.BeginInvoke(new Action(() => {
                                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    fuel1Pos = 0;
                                    _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                                }));
                            });
                        } else {
                            var da62Evt = newPos == 1 ? SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_ON : SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_CROSS;
                            simconnect?.TransmitClientEvent(0, da62Evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            fuel1Pos = newPos;
                            _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                            SendGenericRpn("(>B:FUEL_BLOCKER_LEFT_OFF)");
                        }
                    } else if (IsDA62() && !_isMsfs2024) {
                        // DA-62 (2020, 2026-08-28) — preset MobiFlight/HubHop real (Imadunatic): K: events
                        // natius sense paràmetres, sense WASM. FUEL_SELECTOR_OFF/_LEFT_MAIN(=ON)/_RIGHT_MAIN
                        // (=CROSSFEED per la palanca esquerra).
                        string da62Rpn = newPos switch { 0 => "1 (>K:FUEL_SELECTOR_OFF)", 1 => "1 (>K:FUEL_SELECTOR_LEFT_MAIN)", _ => "1 (>K:FUEL_SELECTOR_RIGHT_MAIN)" };
                        SendGenericRpn(da62Rpn);
                        fuel1Pos = newPos;
                        _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    } else if (_isMsfs2024 && numEngines >= 2) {
                        var evt = newPos switch { 0 => SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_OFF, 1 => SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_ON, _ => SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_XFEED };
                        simconnect?.TransmitClientEvent(0, evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        fuel1Pos = newPos;
                        _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    } else if (numEngines >= 2) {
                        uint[] fuel1Enum = { 0, 2, 3 };
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, fuel1Enum[newPos], SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if (IsBaronG58() && !_isMsfs2024) {
                            // Estat optimista + debounce (2026-08-26): mateix rebot transitori que la
                            // Caravan 2020 — veure guard al bloc TWIN ENGINE de Simconnect_OnRecvSimobjectData.
                            fuel1Pos = newPos;
                            _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                        }
                    } else {
                        uint msfsVal = SingleEngineFuelVal(newPos, fuel2Pos);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, msfsVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if (IsCaravan()) {
                            // Estat optimista + debounce (2026-08-26): la lectura real "rebota" un
                            // instant després del clic — veure guard a Simconnect_OnRecvSimobjectData.
                            fuel1Pos = newPos;
                            _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                        }
                    }
                    InvalidateRotarySwitch("SW_FUEL1", ref _lastFuel1Invalidate);
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="FUEL1-RIGHT", BasePos=new Point(1720, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA40() && !IsC400() && !IsC172() && !IsBonanzaG36() && !IsCirrusSR22() && !IsC408(),
                OnClick = () => {
                    // Caravan: 2 posicions reals (OFF/ON), sense CROSSFEED — límit a 1, no a 2 (2026-08-26,
                    // abans només aplicava a 2024, ara també a 2020).
                    if (IsCaravan() ? fuel1Pos >= 1 : fuel1Pos >= 2) return;
                    int newPos = fuel1Pos + 1;
                    if (IsCaravan() && _isMsfs2024) {
                        // Feedback real confirmat — no toquem fuel1Pos aquí. MSFS2020: cau al fallback
                        // genèric monomotor de més avall (mecanisme original).
                        var evt = newPos == 1 ? SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_ON : SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_OFF;
                        simconnect?.TransmitClientEvent(0, evt, 3, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    } else if (_isMsfs2024 && IsDA62()) {
                        // Bloquejador del guard (2026-08-31) — veure comentari a FUEL1-LEFT.
                        if (newPos == 0) {
                            SendGenericRpn("(>B:FUEL_BLOCKER_LEFT_ON)");
                            Task.Run(async () => {
                                await Task.Delay(250);
                                this.BeginInvoke(new Action(() => {
                                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    fuel1Pos = 0;
                                    _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                                }));
                            });
                        } else {
                            var da62Evt = newPos == 1 ? SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_ON : SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_CROSS;
                            simconnect?.TransmitClientEvent(0, da62Evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            fuel1Pos = newPos;
                            _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                            SendGenericRpn("(>B:FUEL_BLOCKER_LEFT_OFF)");
                        }
                    } else if (IsDA62() && !_isMsfs2024) {
                        // DA-62 (2020) — veure comentari a FUEL1-LEFT.
                        string da62Rpn = newPos switch { 0 => "1 (>K:FUEL_SELECTOR_OFF)", 1 => "1 (>K:FUEL_SELECTOR_LEFT_MAIN)", _ => "1 (>K:FUEL_SELECTOR_RIGHT_MAIN)" };
                        SendGenericRpn(da62Rpn);
                        fuel1Pos = newPos;
                        _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    } else if (_isMsfs2024 && numEngines >= 2) {
                        var evt = newPos switch { 0 => SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_OFF, 1 => SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_ON, _ => SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_XFEED };
                        simconnect?.TransmitClientEvent(0, evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        fuel1Pos = newPos;
                        _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    } else if (numEngines >= 2) {
                        uint[] fuel1Enum = { 0, 2, 3 };
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, fuel1Enum[newPos], SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if (IsBaronG58() && !_isMsfs2024) {
                            // Estat optimista + debounce (2026-08-26): mateix rebot transitori que la
                            // Caravan 2020 — veure guard al bloc TWIN ENGINE de Simconnect_OnRecvSimobjectData.
                            fuel1Pos = newPos;
                            _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                        }
                    } else {
                        uint msfsVal = SingleEngineFuelVal(newPos, fuel2Pos);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, msfsVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if (IsCaravan()) {
                            // Estat optimista + debounce (2026-08-26): la lectura real "rebota" un
                            // instant després del clic — veure guard a Simconnect_OnRecvSimobjectData.
                            fuel1Pos = newPos;
                            _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                        }
                    }
                    InvalidateRotarySwitch("SW_FUEL1", ref _lastFuel1Invalidate);
                }});
            // SW_FUEL2 — left and right areas
            cockpitRight.Buttons.Add(new UiButton { Name="FUEL2-LEFT", BasePos=new Point(2120, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA40() && !IsC400() && !IsC172() && !IsBonanzaG36() && !IsCirrusSR22() && !IsC408(),
                OnClick = () => {
                    if (IsCaravan() && _isMsfs2024) {
                        // Caravan a MSFS2024: zones invertides respecte la resta d'avions — l'esquerra
                        // ara ENCÉN. MSFS2020: sense branca pròpia, cau al fallback genèric de més avall.
                        if (fuel2Pos >= 1) return;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_ON, 4, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        InvalidateRotarySwitch("SW_FUEL2", ref _lastFuel2Invalidate);
                        return;
                    }
                    // Caravan: 2 posicions reals (ON/OFF), sense CROSSFEED — el seu ON és 1, no 0
                    // (2026-08-26).
                    if (IsCaravan() ? fuel2Pos <= 1 : fuel2Pos <= 0) return;
                    int newPos = fuel2Pos - 1;
                    if (_isMsfs2024 && IsDA62()) {
                        // DA-62: mateix mecanisme que l'esquerra, índex 2 (K:2:FUELSYSTEM_TRIGGER_SET/VALVE_SET). Confirmat.
                        // Bloquejador del guard (2026-08-31) — veure comentari a FUEL1-LEFT. Aquí OFF és
                        // newPos==2 (encoding invertit de la palanca dreta, igual que da62Evt de sobre).
                        if (newPos == 2) {
                            SendGenericRpn("(>B:FUEL_BLOCKER_RIGHT_ON)");
                            Task.Run(async () => {
                                await Task.Delay(250);
                                this.BeginInvoke(new Action(() => {
                                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    fuel2Pos = 2;
                                    _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                                }));
                            });
                        } else {
                            var da62Evt = newPos == 1 ? SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_ON : SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_CROSS;
                            simconnect?.TransmitClientEvent(0, da62Evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            fuel2Pos = newPos;
                            _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                            SendGenericRpn("(>B:FUEL_BLOCKER_RIGHT_OFF)");
                        }
                    } else if (IsDA62() && !_isMsfs2024) {
                        // DA-62 (2020, 2026-08-28) — preset MobiFlight/HubHop real (Imadunatic), palanca
                        // dreta (A:FUEL TANK SELECTOR:2): 0=OFF, 20=ON(Right Main, confirmat), 19=CROSSFEED
                        // (Left Main, inferit per simetria amb l'esquerra — pendent de confirmar).
                        string da62Rpn = newPos switch { 2 => "1 (>K:FUEL_SELECTOR_2_OFF)", 1 => "1 (>K:FUEL_SELECTOR_2_RIGHT_MAIN)", _ => "1 (>K:FUEL_SELECTOR_2_LEFT_MAIN)" };
                        SendGenericRpn(da62Rpn);
                        fuel2Pos = newPos;
                        _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    } else if (_isMsfs2024 && numEngines >= 2) {
                        // pos: 0=Xfeed, 1=On, 2=Off (ordre invertit respecte al selector esquerre)
                        var evt = newPos switch { 2 => SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_OFF, 1 => SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_ON, _ => SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_XFEED };
                        simconnect?.TransmitClientEvent(0, evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        fuel2Pos = newPos;
                        _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    } else if (numEngines >= 2) {
                        uint[] fuel2Enum = { 13, 3, 0 };
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_2_SET, fuel2Enum[newPos], SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if (IsBaronG58() && !_isMsfs2024) {
                            // Estat optimista + debounce (2026-08-26): mateix rebot transitori que la
                            // Caravan 2020 — veure guard al bloc TWIN ENGINE de Simconnect_OnRecvSimobjectData.
                            fuel2Pos = newPos;
                            _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                        }
                    } else {
                        uint msfsVal = SingleEngineFuelVal(fuel1Pos, newPos);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, msfsVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if (IsCaravan()) {
                            // Estat optimista + debounce (2026-08-26): la lectura real "rebota" un
                            // instant després del clic — veure guard a Simconnect_OnRecvSimobjectData.
                            fuel2Pos = newPos;
                            _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                        }
                    }
                    InvalidateRotarySwitch("SW_FUEL2", ref _lastFuel2Invalidate);
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="FUEL2-RIGHT", BasePos=new Point(2220, 1080), W=80, H=120, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA40() && !IsC400() && !IsC172() && !IsBonanzaG36() && !IsCirrusSR22() && !IsC408(),
                OnClick = () => {
                    if (IsCaravan() && _isMsfs2024) {
                        // Caravan a MSFS2024: zones invertides respecte la resta d'avions — la dreta ara
                        // APAGA. MSFS2020: sense branca pròpia, cau al fallback genèric de més avall.
                        if (fuel2Pos <= 0) return;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_OFF, 4, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        InvalidateRotarySwitch("SW_FUEL2", ref _lastFuel2Invalidate);
                        return;
                    }
                    if (fuel2Pos >= 2) return;
                    int newPos = fuel2Pos + 1;
                    if (_isMsfs2024 && IsDA62()) {
                        // Bloquejador del guard (2026-08-31) — veure comentari a FUEL1-LEFT / FUEL2-LEFT.
                        if (newPos == 2) {
                            SendGenericRpn("(>B:FUEL_BLOCKER_RIGHT_ON)");
                            Task.Run(async () => {
                                await Task.Delay(250);
                                this.BeginInvoke(new Action(() => {
                                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                                    fuel2Pos = 2;
                                    _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                                }));
                            });
                        } else {
                            var da62Evt = newPos == 1 ? SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_ON : SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_CROSS;
                            simconnect?.TransmitClientEvent(0, da62Evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            fuel2Pos = newPos;
                            _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                            SendGenericRpn("(>B:FUEL_BLOCKER_RIGHT_OFF)");
                        }
                    } else if (IsDA62() && !_isMsfs2024) {
                        // DA-62 (2020) — veure comentari a FUEL2-LEFT.
                        string da62Rpn = newPos switch { 2 => "1 (>K:FUEL_SELECTOR_2_OFF)", 1 => "1 (>K:FUEL_SELECTOR_2_RIGHT_MAIN)", _ => "1 (>K:FUEL_SELECTOR_2_LEFT_MAIN)" };
                        SendGenericRpn(da62Rpn);
                        fuel2Pos = newPos;
                        _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    } else if (_isMsfs2024 && IsBaronG58()) {
                        var evt = newPos switch { 2 => SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_OFF, 1 => SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_ON, _ => SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_XFEED };
                        simconnect?.TransmitClientEvent(0, evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        fuel2Pos = newPos;
                        _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                    } else if (numEngines >= 2) {
                        uint[] fuel2Enum = { 13, 3, 0 };
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_2_SET, fuel2Enum[newPos], SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if (IsBaronG58() && !_isMsfs2024) {
                            // Estat optimista + debounce (2026-08-26): mateix rebot transitori que la
                            // Caravan 2020 — veure guard al bloc TWIN ENGINE de Simconnect_OnRecvSimobjectData.
                            fuel2Pos = newPos;
                            _fuelSel2IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                        }
                    } else {
                        uint msfsVal = SingleEngineFuelVal(fuel1Pos, newPos);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, msfsVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        if (IsCaravan()) {
                            // Estat optimista + debounce (2026-08-26): la lectura real "rebota" un
                            // instant després del clic — veure guard a Simconnect_OnRecvSimobjectData.
                            fuel2Pos = newPos;
                            _fuelSel1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                        }
                    }
                    InvalidateRotarySwitch("SW_FUEL2", ref _lastFuel2Invalidate);
                }});

            // SW_SE_SELECTOR — selector únic de 3 posicions per monomotors (DA-40 de moment).
            // Comanda confirmada via Behaviors "Set" (FUEL_SWITCH_SELECTOR / FUEL_SELECTOR_1) — veure SeSelectorMsfsVal().
            // Posicions per defecte = últim calibratge F12 de Pere (calibration_export.txt), es carreguen igualment via LoadCalibrationPositions.
            cockpitRight.Buttons.Add(new UiButton { Name="SE_SELECTOR-LEFT", BasePos=new Point(1572, 1967), W=186, H=253, Resizable=true, NoShadow=true,
                ShowWhen = () => IsDA40() || IsC400() || IsC172() || IsBonanzaG36() || IsCirrusSR22() || IsC408(),
                OnClick = () => {
                    if (seSelectorPos <= 0) return;
                    seSelectorPos--;
                    SendSeSelectorCommand(seSelectorPos);
                    InvalidateRotarySwitch("SW_SE_SELECTOR", ref _lastSeSelectorInvalidate);
                }});
            cockpitRight.Buttons.Add(new UiButton { Name="SE_SELECTOR-RIGHT", BasePos=new Point(1772, 1967), W=177, H=255, Resizable=true, NoShadow=true,
                ShowWhen = () => IsDA40() || IsC400() || IsC172() || IsBonanzaG36() || IsCirrusSR22() || IsC408(),
                OnClick = () => {
                    if (seSelectorPos >= 2) return;
                    seSelectorPos++;
                    SendSeSelectorCommand(seSelectorPos);
                    InvalidateRotarySwitch("SW_SE_SELECTOR", ref _lastSeSelectorInvalidate);
                }});

            cockpitRight.Buttons.Add(new UiButton { Name="BTN_STARTER1", BasePos=new Point(1564, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                // A la Caravan (2020 i 2024) aquesta zona la gestionen BTN_CARAVAN_STARTER1_UP/DN (2 zones
                // separades) — cal amagar-la del tot, si no el ClickRect (que ocupa tota l'àrea)
                // interceptaria sempre el clic abans que arribi a les 2 zones noves (Buttons s'itera en
                // ordre i para al primer match). Confirmat en viu (2026-08-09) que L:XMLVAR_Starter1 i els
                // triggers B:ENGINE_Starter_Ignition_1_Start/_Off/_Motor funcionen igual a MSFS2020.
                ShowWhen = () => !IsCaravan(),
                OnClick = () => {
                    if (IsDA62()) {
                        if (starter1Active) return;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.STARTER1_SET, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        Task.Run(async () => {
                            await Task.Delay(4000);
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.STARTER1_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        });
                        return;
                    }
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_STARTER1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                } });
            // Caravan — 2 zones clicables (amunt cap a START, avall cap a MOTOR). Mida i distància entre
            // elles preses de les zones reals dels cowl flaps (BTN_COWLFLAP_L_INC/DEC a
            // calibration_export.txt: ~126-128 ample, ~125-132 alt, ~148px entre centres), centrades sobre
            // el switch real BTN_STARTER1 (X=704,Y=1676).
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_CARAVAN_STARTER1_UP", BasePos=new Point(704, 1602), W=127, H=128, Resizable=true, NoShadow=true,
                ShowWhen = () => IsCaravan(),
                OnClick = () => { if (IsCaravan()) StepCaravanStarter1(-1); } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_CARAVAN_STARTER1_DN", BasePos=new Point(704, 1750), W=127, H=128, Resizable=true, NoShadow=true,
                ShowWhen = () => IsCaravan(),
                OnClick = () => { if (IsCaravan()) StepCaravanStarter1(1); } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_IGNITION1", BasePos=new Point(1624, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (IsDA62()) {
                        uint target = da62EngineMaster1On ? 0u : 1u;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ENGINE_MASTER_1_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        return;
                    }
                    if (IsDA40()) {
                        if (_isMsfs2024)
                        {
                            // 2024: intacte, no tocat (no confirmat en directe si calia el mateix fix que 2020).
                            // "Engine Master" real és B:ENGINE_Master_1 (InputEvent) — el Mouserect només fa
                            // el toggle si la tapa de seguretat (B:ENGINE_Master_1_Cover) està oberta.
                            SendGenericRpn("1 (>B:ENGINE_Master_1_Cover) (>B:ENGINE_Master_1_Toggle)");
                        }
                        else
                        {
                            // 2020 (2026-08-28): ni el Toggle ni els triggers B: directes feien res.
                            // Confirmat via revòlver SEND: el K: natiu ENGINE_MASTER_1_SET (el mateix event
                            // ja usat per la DA-62, mateix fabricant) SÍ funciona.
                            uint target = da40EngineMaster1On ? 0u : 1u;
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ENGINE_MASTER_1_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        }
                        return;
                    }
                    if (IsC408()) {
                        // C408: "AUTO IGNITION L" real és ENGINE_SWITCH_IGNITION_1 — el Set complet fa
                        // control de flux intern (s0/l0/g1, no transcriptible directe a RPN pla), però la
                        // secció "Variable(s)" del Behavior viewer confirma que la variable global real és
                        // L:XMLVAR_IGNITION_1_1 (no una O: privada) — nom complet confirmat a Variables >
                        // Global del Behavior viewer. L'escrivim directament pel missatger genèric.
                        uint c408target = ignition1Active ? 0u : 1u;
                        SendGenericRpn($"{c408target} (>L:XMLVAR_IGNITION_1_1)");
                        return;
                    }
                    if (IsCaravan()) {
                        // Caravan — IGNITION (turboprop). Confirmat 2026-08-26 llegint el Set complet de
                        // l'InputEvent ASOBO_ENGINE_Ignition_SubTemplate al Behavior Debug: NO és un B:
                        // trigger pla (B:ENGINE_Ignition_1_Toggle/_Auto/_On provats abans, cap funcionava
                        // des de fora) — és EXACTAMENT el mateix mecanisme del STARTER (mateixa variable
                        // A:TURB ENG IGNITION SWITCH EX1:1, mateix event K:TURBINE_IGNITION_SWITCH_SET1),
                        // físicament el mateix interruptor combinat, només amb un valor nou mai fet
                        // servir: 0=OFF/MOTOR, 1=AUTO (=START del STARTER), 2=ON/OVERRIDE (ignició
                        // contínua). IGNITION alterna entre AUTO(1) i ON(2); mai toca 0 (MOTOR).
                        // Recerca real de Pere 2026-08-26 (PT6, família Air Tractor/King Air/Thrush):
                        // STARTER i IGNITION són 2 interruptors físicament separats, SENSE cap lligam
                        // mecànic — la relació és NOMÉS elèctrica i unidireccional (STARTER a START
                        // "encén" la ignició si IGNITION és NORM, però mai mou el selector d'IGNITION;
                        // IGNITION a ON força ignició contínua però mai mou el selector de STARTER).
                        // Per això la part VISUAL de cada un és local (_caravanIgnitionOn aquí,
                        // _caravanStarter1Pos protegit amb el debounce de sota) — mai es mouen l'un a
                        // l'altre, encara que per sota comparteixin la mateixa variable elèctrica real.
                        _caravanIgnitionOn = !_caravanIgnitionOn;
                        int target = _caravanIgnitionOn ? 2 : (_caravanStarter1Pos == 0 ? 1 : 0);
                        // Permanent (no temporal): MSFS canvia L:XMLVAR_Starter1 de veritat i s'hi queda
                        // (confirmat 2026-08-26, un debounce de 2s no n'hi havia prou). Deixem de confiar
                        // en la lectura de STARTER fins que l'usuari torni a fer clic al STARTER mateix
                        // (StepCaravanStarter1 ho reactiva).
                        _caravanStarter1IgnoreFollowUntil = DateTime.MaxValue;
                        SendGenericRpn($"{target} (>K:TURBINE_IGNITION_SWITCH_SET1)");
                        pbMain.Invalidate();
                        return;
                    }
                    simconnect?.TransmitClientEvent(0,
                    ignition1Active ? SIMCONNECT_EVENTS.KINGAIR_IGN1_OFF : SIMCONNECT_EVENTS.KINGAIR_IGN1_ARM,
                    0u, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_STARTER2", BasePos=new Point(1684, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (IsDA62()) {
                        if (starter2Active) return;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.STARTER2_SET, 1, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        Task.Run(async () => {
                            await Task.Delay(4000);
                            simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.STARTER2_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        });
                        return;
                    }
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TOGGLE_STARTER2, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_IGNITION2", BasePos=new Point(1744, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                OnClick = () => {
                    if (IsDA62()) {
                        uint target = da62EngineMaster2On ? 0u : 1u;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.ENGINE_MASTER_2_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        return;
                    }
                    if (IsC408()) {
                        // C408: "AUTO IGNITION R" — mateix patró que AUTO IGNITION L, variable real
                        // L:XMLVAR_IGNITION_2_2 (confirmada a Variables > Global del Behavior viewer).
                        uint c408target2 = ignition2Active ? 0u : 1u;
                        SendGenericRpn($"{c408target2} (>L:XMLVAR_IGNITION_2_2)");
                        return;
                    }
                    simconnect?.TransmitClientEvent(0,
                    ignition2Active ? SIMCONNECT_EVENTS.KINGAIR_IGN2_OFF : SIMCONNECT_EVENTS.KINGAIR_IGN2_ARM,
                    0u, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            // C408 (2026-09-05, confirmat per Pere): no fa res — exclosa del ressaltat. C172 (2024,
            // 2026-09-05, confirmat per Pere): tampoc fa res — NOMÉS exclosa a 2024, ja que la 2020 té
            // codi propi confirmat (potenciòmetre A:LIGHT POTENTIOMETER:3, veure OnClick de sota).
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_PANELLIGHT", BasePos=new Point(1924, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsC408() && !(IsC172() && _isMsfs2024),
                OnClick = () => {
                    if (IsC172() && !_isMsfs2024)
                    {
                        // C172 (2020): el llum de panell és un potenciòmetre (A:LIGHT POTENTIOMETER:3,
                        // 0=OFF, 100=ON — confirmat per Pere al material emissive) girat amb un pom
                        // (LIGHTING_Knob_Instruments) que NO té un Set de valor absolut — només
                        // incrementa/decrementa via (>B:LIGHTING_PANEL_1_Inc) / (>B:LIGHTING_PANEL_1_Dec)
                        // (confirmat al Behavior Debug del pom: Mouserect LeftDrag i rodeta només fan
                        // Inc/Dec, sense cap Set directe). Repetim el trigger prou vegades per saturar
                        // el rang (0 o 100%) independentment del valor de partida.
                        string evt = panelLightActive ? "B:LIGHTING_PANEL_1_Dec" : "B:LIGHTING_PANEL_1_Inc";
                        SendGenericRpn(string.Join(" ", Enumerable.Repeat($"1 (>{evt})", 30)));
                        pbMain.Invalidate();
                        return;
                    }
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_PANELLIGHT, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_DEICE", BasePos=new Point(1984, 1653), W=75, H=75, Resizable=true, NoShadow=true,
                // DA-62 (2024): confirmat "not supported" — veure comentari ABANDONAT més avall. DA-40 i
                // C172 (2026-09-05, confirmat per Pere, 2020 i 2024): tampoc fan res. Caravan (2026-09-05,
                // confirmat per Pere): NOMÉS a 2024 — la branca de 2020 té codi propi confirmat (llegeix
                // el Set complet de l'InputEvent DEICE_Airframe_1, veure OnClick de sota).
                ShowWhen = () => !(IsDA62() && _isMsfs2024) && !IsDA40() && !IsC172() && !(IsCaravan() && _isMsfs2024),
                OnClick = () => {
                    if (IsCirrusSR22() || (IsCaravan() && _isMsfs2024)) {
                        // Cirrus SR22T: el Set de l'InputEvent només toca una O: privada (inaccessible des
                        // de fora) — el "Variable(s)" del Behavior viewer confirma que la variable real que
                        // anima el switch és L:XMLVAR_DEICE_MASTER (global). L'escrivim directament pel
                        // missatger genèric, saltant-nos l'O: intermèdia.
                        // Caravan (MSFS2024 NOMÉS): mecanisme antic, mai confirmat de veritat però tampoc
                        // trencat — es queda igual fins que es provi. MSFS2020: cau al fallback genèric
                        // SWITCH_DEICE de més avall (confirmat 2026-08-26, veure comentari allà).
                        int target = deiceActive ? 0 : 1;
                        SendGenericRpn($"{target} (>L:XMLVAR_DEICE_MASTER)");
                        pbMain.Invalidate();
                        return;
                    }
                    if (IsBonanzaG36() && _isMsfs2024) {
                        // Bonanza G36 a MSFS2024: l'únic switch de "deice" real és el de l'hèlix (prop
                        // de-ice elèctric, no té botes estructurals). Mouserect confirmat:
                        // (>B:DEICE_Propeller_1_Toggle), animació basada en (B:DEICE_Propeller_1) — mateix
                        // patró de trigger B: pla que l'ignition de la Caravan. Confirmat funcionant, NO TOCAR.
                        SendGenericRpn("1 (>B:DEICE_Propeller_1_Toggle)");
                        return;
                    }
                    if (IsBonanzaG36() && !_isMsfs2024) {
                        // Bonanza G36 a MSFS2020: cap de les vies pel prop de-ice (B:DEICE_Propeller_1_Set,
                        // K:TOGGLE_PROPELLER_DEICE, etc.) mouen res — provat i descartat (prova/error amb 8
                        // candidates). En canvi, K:ANTI_ICE_TOGGLE SÍ funciona real (confirmat per Pere), però
                        // controla un altre interruptor: "Defrost Pull ON". Ens hi passem — Pere ho accepta
                        // com a bloc d'ice protection d'aquest avió en lloc del prop de-ice.
                        SendGenericRpn("1 (>K:ANTI_ICE_TOGGLE)");
                        return;
                    }
                    if (IsCaravan()) {
                        // Caravan (MSFS2020): confirmat llegint el Set complet de l'InputEvent
                        // DEICE_Airframe_1 al Behavior Debug — la posició real (HIGH/NORM/OFF) es guarda a
                        // L:DEICE_Airframe_1 (0=HIGH,1=NORM,2=OFF), NO al booleà A:STRUCTURAL DEICE SWITCH
                        // (que només distingeix ON/OFF, no HIGH/NORM — per això el clic de l'app sincronitzava
                        // però el de la VC no, quan Pere movia entre HIGH i NORM el booleà no canviava mai).
                        // Repliquem el SET_EVENT real: escrivim L:DEICE_Airframe_1 i sincronitzem el booleà
                        // elèctric amb K:TOGGLE_STRUCTURAL_DEICE només quan cal canviar-lo (com fa l'original).
                        int target = deiceActive ? 2 : 1; // ON→NORM(1) (repòs normal), OFF→2
                        string rpn = target == 2
                            ? "2 (>L:DEICE_Airframe_1) (A:STRUCTURAL DEICE SWITCH, Bool) if{ (>K:TOGGLE_STRUCTURAL_DEICE) } 0 (>L:XMLVAR_IsDeiceAirFrame)"
                            : "1 (>L:DEICE_Airframe_1) (A:STRUCTURAL DEICE SWITCH, Bool) ! if{ (>K:TOGGLE_STRUCTURAL_DEICE) } 0 (>L:XMLVAR_IsDeiceAirFrame)";
                        SendGenericRpn(rpn);
                        pbMain.Invalidate();
                        return;
                    }
                    // DA-62 (2024): ABANDONAT (2026-08-31) — cap dels mecanismes provats
                    // (B:DEICE_AIRFRAME_OFF/_HIGH en les dues direccions, repeticions, ni el toggle pla
                    // K:TOGGLE_STRUCTURAL_DEICE) ha donat un comportament fiable 1 clic = 1 moviment. Es
                    // marca "X / not supported" a l'Excel de compatibilitat. No reprendre sense que Pere ho
                    // demani explícitament. Cau al fallback genèric de sota (mateix event pla que abans de
                    // qualsevol intervenció d'aquesta sessió).
                    // Resta d'avions: fallback genèric.
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_DEICE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                } });
            // DA-62 (2026-09-05, confirmat per recerca — motors Austro AE330 amb FADEC): cowl flaps
            // totalment automàtics, sense control manual del pilot. DA-40, Cirrus SR22, C408, C400, C172
            // i Caravan (2026-09-05, confirmat per Pere): tampoc en tenen (ni L ni R).
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_COWLFLAP_L_INC", BasePos=new Point(1500, 1560), W=75, H=40, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA62() && !IsDA40() && !IsCirrusSR22() && !IsC408() && !IsC400() && !IsC172() && !IsCaravan(),
                OnClick = () => { uint target = cowlFlapL == 0 ? 8191u : 16383u;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COWL_FLAP_L_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_COWLFLAP_L_DEC", BasePos=new Point(1500, 1640), W=75, H=40, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA62() && !IsDA40() && !IsCirrusSR22() && !IsC408() && !IsC400() && !IsC172() && !IsCaravan(),
                OnClick = () => { uint target = cowlFlapL == 2 ? 8191u : 0u;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COWL_FLAP_L_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            // Bonanza G36 (2026-09-05, confirmat per Pere): monomotor — només el cowl flap "1" (L) és
            // real, el "2" (R) no existeix. Exclòs NOMÉS aquí, no a L_INC/L_DEC de sobre.
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_COWLFLAP_R_INC", BasePos=new Point(1600, 1560), W=75, H=40, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA62() && !IsDA40() && !IsCirrusSR22() && !IsC408() && !IsC400() && !IsC172() && !IsCaravan() && !IsBonanzaG36(),
                OnClick = () => { uint target = cowlFlapR == 0 ? 8191u : 16383u;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COWL_FLAP_R_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });
            cockpitRight.Buttons.Add(new UiButton { Name="BTN_COWLFLAP_R_DEC", BasePos=new Point(1600, 1640), W=75, H=40, Resizable=true, NoShadow=true,
                ShowWhen = () => !IsDA62() && !IsDA40() && !IsCirrusSR22() && !IsC408() && !IsC400() && !IsC172() && !IsCaravan() && !IsBonanzaG36(),
                OnClick = () => { uint target = cowlFlapR == 2 ? 8191u : 0u;
                    simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.COWL_FLAP_R_SET, target, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); } });


            var swWhiteImg = LoadOverlayImage("SW DN white.png");
            var swGreenImg = LoadOverlayImage("SW DN green.png");
            var swBlueImg  = LoadOverlayImage("SW DN blue.png");
            var swCenterBlueImg = LoadOverlayImage("SW CENTER  blue.png");
            var ledGreenImg = LoadOverlayImage("led green.png");
            var ledRedImg   = LoadOverlayImage("led red.png");
            var swLgearImg  = LoadOverlayImage("SW landing gear.png");
            var swPbrakeImg  = LoadOverlayImage("Parking Brake switch_b.png");
            var swIgnitionImg = LoadOverlayImage("SW ignition.png");
            var swFuelImg     = LoadOverlayImage("SW fuel_r.png");
            var swBatteryImg  = LoadOverlayImage("SW battery.png");
            var swAvionicsImg = LoadOverlayImage("SW avionics.png");
            var swDnRedImg    = LoadOverlayImage("SW DN red.png");
            var swCenterRedImg = LoadOverlayImage("SW CENTER red.png");
            // No hi ha cap "SW UP red.png" (ni "ON") perquè el fons 4K ja mostra tots els interruptors
            // en aquesta posició per defecte — els overlays només calen per DESVIAR-se'n (CENTER/OFF).
            // Per l'animació de RESET (veure ALTGEN1-UP) no cal cap imatge nova: només amagar els
            // overlays existents perquè es vegi el fons tal qual.
            var swCenterWhiteImg = LoadOverlayImage("SW CENTER white_b.png");
            var swCenterWhiteImgC = LoadOverlayImage("SW CENTER white_c.png");
            var swWhiteImgB = LoadOverlayImage("SW DN white-b.png");
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
                        ShowWhen = () => IsCaravan() ? false : !starter1Active, OnClick=null },
                // Caravan (2020 i 2024) — posició MOTOR (baix) del STARTER, imatge dedicada "SW DN white-b.png"
                // (no la compartida swWhiteImg, per no afectar la resta de switches que la fan servir).
                new() { Name="SW_DN_STARTER1_B", TypeKey="SW_DN_STARTER1_B", BasePos=new Point(673, 1619), ZoomWidth=72, Img=swWhiteImgB,
                        ShowWhen = () => IsCaravan() && _caravanStarter1Pos == 2, OnClick=null },
                // Caravan (2020 i 2024) — posició OFF (centre) del STARTER de 3 posicions. Mateixa imatge que
                // fem servir al cowl flap (SW CENTER white_b.png), posicionada amb el mateix offset relatiu
                // que hi ha entre SW_CN_COWLFLAP_L i SW_DN_COWLFLAP_L (~+2,-3 respecte a la posició DN real,
                // calibration_export.txt) aplicat sobre la posició real de SW_DN_STARTER1 (673,1611).
                new() { Name="SW_CN_STARTER1", TypeKey="SW_CN_STARTER1", BasePos=new Point(674, 1617), ZoomWidth=69, Img=swCenterWhiteImgC,
                        ShowWhen = () => IsCaravan() && _caravanStarter1Pos == 1, OnClick=null },
                new() { Name="SW_DN_IGNITION1", TypeKey="SW_DN_IGNITION1", BasePos=new Point(1624, 1653), ZoomWidth=75, Img=swWhiteImg,
                        // Caravan: NO fem servir ignition1Active (compartida amb STARTER) — visual local
                        // _caravanIgnitionOn, independent de STARTER, igual que al VC real (veure OnClick).
                        ShowWhen = () => IsDA62() ? !da62EngineMaster1On : IsDA40() ? !da40EngineMaster1On : IsC408() ? !ignition1Active : IsCaravan() ? !_caravanIgnitionOn : true, OnClick=null },
                new() { Name="SW_DN_STARTER2",  TypeKey="SW_DN_STARTER2",  BasePos=new Point(1684, 1653), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => !starter2Active, OnClick=null },
                new() { Name="SW_DN_IGNITION2", TypeKey="SW_DN_IGNITION2", BasePos=new Point(1744, 1653), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => IsDA62() ? !da62EngineMaster2On : IsC408() ? !ignition2Active : true, OnClick=null },
                new() { Name="SW_DN_PANELLIGHT", TypeKey="SW DN white", BasePos=new Point(1924, 1653), ZoomWidth=75, Img=swWhiteImg,
                        ShowWhen = () => panelLightActive,
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
                        ShowWhen = () => UsesPump1_3StateUi() ? _fuelPump1State3 == 2 : !fuelpump1Active,
                        OnClickEnabled = () => !UsesPump1_3StateUi(),
                        OnClick  = () => {
                            if (UsesPump1_3StateUi())
                            {
                                // Baron/Caravan/Cirrus/Bonanza: la interacció es fa amb les 2 àrees dedicades SW_FUELPUMP1_UP/DN, no amb aquest switch.
                            }
                            else if ((IsBonanzaG36() || IsBaronG58()) && !_isMsfs2024)
                            {
                                // Bonanza G36 / Baron G58 a MSFS2020: mateix model ON/OFF genèric que la
                                // C172 (1 sola zona clicable, sense PNG center ni etiquetes). Bonanza fa
                                // servir K:ELECT_FUEL_PUMP1_SET + L:XMLVAR_FUEL_Pump1_High_ON; pel Baron
                                // (confirmat 2026-08-27 via SEND) cal el K: SOL, la L:XMLVAR bloqueja l'efecte.
                                int onOff = fuelpump1Active ? 0 : 1;
                                string rpn = IsBaronG58()
                                    ? $"{onOff} (>K:ELECT_FUEL_PUMP1_SET)"
                                    : $"{onOff} (>K:ELECT_FUEL_PUMP1_SET) {onOff} (>L:XMLVAR_FUEL_Pump1_High_ON)";
                                SendGenericRpn(rpn);
                            }
                            else if (_isMsfs2024)
                            {
                                uint pumpIdx = IsDA40() ? 3u : 1u;
                                simconnect?.TransmitClientEvent(0, fuelpump1Active ? SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_OFF : SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_ON, pumpIdx, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            }
                            else
                                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_FUELPUMP1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        } },
                new() { Name="SW_DN_FUELPUMP2", TypeKey="SW DN blue",  BasePos=new Point(1864, 1653), ZoomWidth=75, Img=swBlueImg,
                        ShowWhen = () => (IsBaronG58() && _isMsfs2024) ? _fuelPump2State3 == 2 : (UsesCaravanPumpScheme() || IsBonanzaG36()) ? true : (IsDA40() && !_isMsfs2024) ? !_da40FuelTransferOn : !fuelpump2Active,
                        OnClickEnabled = () => !((IsBaronG58() && _isMsfs2024) || UsesCaravanPumpScheme() || IsBonanzaG36()),
                        OnClick  = () => {
                            if (IsDA40() && !_isMsfs2024)
                            {
                                // DA-40 (2020): aquesta posició NO és una bomba, és el FUEL TRANSFER switch
                                // (veure SW_DA40_FUELTRANSF_LBL). Confirmat via revòlver SEND (2026-08-28):
                                // K:ELECTRICAL_CIRCUIT_TOGGLE amb índex 7 (vist a l'Update 0 de l'InputEvent,
                                // "(A:CIRCUIT SWITCH ON:7, Bool) if{ 7 (>K:ELECTRICAL_CIRCUIT_TOGGLE) }") és
                                // un toggle real que activa/desactiva correctament — ni FUEL_Transfer_1/
                                // _Toggle/_On/_Off ni O:FUEL_Transfer_1_Position ni K:SET_FUEL_TRANSFER_CUSTOM
                                // feien res. L'estat visual el porta ara la lectura real (A:CIRCUIT SWITCH
                                // ON:7, veure RequestDA40CircuitSwitch7) — el clic només envia la comanda.
                                SendGenericRpn("7 (>K:ELECTRICAL_CIRCUIT_TOGGLE)");
                            }
                            else if (_isMsfs2024 && IsBaronG58())
                            {
                                // Baron: la interacció es fa amb les 2 àrees dedicades SW_FUELPUMP2_UP/DN, no amb aquest switch.
                            }
                            else if (IsBaronG58() && !_isMsfs2024)
                            {
                                // Baron G58 a MSFS2020 (2026-08-27) — mateix model senzill ON/OFF que
                                // l'esquerra (LOW inabastable, mateix motiu arquitectònic que l'ALTGEN1).
                                int onOff = fuelpump2Active ? 0 : 1;
                                SendGenericRpn($"{onOff} (>K:ELECT_FUEL_PUMP2_SET)");
                            }
                            else if (_isMsfs2024)
                                simconnect?.TransmitClientEvent(0, fuelpump2Active ? SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_OFF : SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_ON, 2, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            else
                                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SWITCH_FUELPUMP2, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        } },
                // Baron G58 / Cessna Caravan / Cirrus SR22 / Bonanza G36 — posició LOW/AUTO (centre) de les bombes de 3 posicions
                new() { Name="SW_CN_FUELPUMP1", TypeKey="SW CN blue", BasePos=new Point(1804, 1653), ZoomWidth=75, Img=swCenterBlueImg,
                        ShowWhen = () => UsesPump1_3StateUi() && _fuelPump1State3 == 1, OnClick=null },
                new() { Name="SW_CN_FUELPUMP2", TypeKey="SW CN blue", BasePos=new Point(1864, 1653), ZoomWidth=75, Img=swCenterBlueImg,
                        ShowWhen = () => IsBaronG58() && _fuelPump2State3 == 1, OnClick=null },
                // Baron G58 / Cessna Caravan / Cirrus SR22 / Bonanza G36 — 2 àrees clicables per bomba (pujar cap a HIGH/ON / baixar cap a OFF),
                // redimensionables i movibles amb F12. No depenen de cap lectura: cada clic avança un pas l'estat local.
                new() { Name="SW_FUELPUMP1_UP", TypeKey="SFS_PUMP_ZONE", BasePos=new Point(1804, 1720), ZoomWidth=75, ZoomHeight=40, Img=null,
                        OnClickEnabled = () => UsesPump1_3StateUi(),
                        OnClick = () => { if (UsesPump1_3StateUi()) StepFuelPumpBaron(1, -1); } },
                new() { Name="SW_FUELPUMP1_DN", TypeKey="SFS_PUMP_ZONE", BasePos=new Point(1804, 1770), ZoomWidth=75, ZoomHeight=40, Img=null,
                        OnClickEnabled = () => UsesPump1_3StateUi(),
                        OnClick = () => { if (UsesPump1_3StateUi()) StepFuelPumpBaron(1, 1); } },
                // Baron G58 (2026-09-05, confirmat per Pere): la bomba 2 només és de 3 posicions a 2024
                // — a 2020 és un simple ON/OFF (SW_DN_FUELPUMP2), sense UP/DN. Gatejat igual que el
                // ShowWhen/OnClickEnabled de SW_DN_FUELPUMP2 de sobre.
                new() { Name="SW_FUELPUMP2_UP", TypeKey="SFS_PUMP_ZONE", BasePos=new Point(1864, 1720), ZoomWidth=75, ZoomHeight=40, Img=null,
                        OnClickEnabled = () => IsBaronG58() && _isMsfs2024,
                        OnClick = () => { if (IsBaronG58() && _isMsfs2024) StepFuelPumpBaron(2, -1); } },
                new() { Name="SW_FUELPUMP2_DN", TypeKey="SFS_PUMP_ZONE", BasePos=new Point(1864, 1770), ZoomWidth=75, ZoomHeight=40, Img=null,
                        OnClickEnabled = () => IsBaronG58() && _isMsfs2024,
                        OnClick = () => { if (IsBaronG58() && _isMsfs2024) StepFuelPumpBaron(2, 1); } },
                // DA-40 — l'interruptor 2 és el fuel transfer switch, no una bomba. Etiqueta movible amb F12.
                new() { Name="SW_DA40_FUELTRANSF_LBL", TypeKey="DA40_LBL", BasePos=new Point(1804, 1598), ZoomWidth=160, ZoomHeight=60, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsDA40() },
                // Caravan — etiqueta "STBY / ALT PWR" per l'interruptor ALTGEN2-TOGGLE (standby alternator).
                // Mateix estil/mida que SW_DA40_FUELTRANSF_LBL. Arrossegable amb F12, mida fixa.
                new() { Name="SW_CARAVAN_ALTGEN2_LBL", TypeKey="CARAVAN_ALTGEN2_LBL", BasePos=new Point(1620, 1350), ZoomWidth=160, ZoomHeight=60, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                // DA-40 — etiqueta "ESS / BUS" per l'interruptor ALT/GEN 2 (essential bus). Idèntica mida/
                // posició/estil que SW_CARAVAN_ALTGEN2_LBL.
                new() { Name="SW_DA40_ESSBUS_LBL", TypeKey="DA40_ESSBUS_LBL", BasePos=new Point(1620, 1350), ZoomWidth=150, ZoomHeight=80, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsDA40() },
                // Caravan — etiquetes RESET/ON/TRIP per l'interruptor momentani ALTGEN1 (UP/centre/DN).
                // Mateix estil que SW_CARAVAN_ALTGEN2_LBL. Arrossegables amb F12, mida fixa.
                new() { Name="SW_CARAVAN_ALTGEN1_LBL_RESET", TypeKey="CARAVAN_ALTGEN1_LBL", BasePos=new Point(1550, 1170), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CARAVAN_ALTGEN1_LBL_ON", TypeKey="CARAVAN_ALTGEN1_LBL", BasePos=new Point(1550, 1220), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CARAVAN_ALTGEN1_LBL_TRIP", TypeKey="CARAVAN_ALTGEN1_LBL", BasePos=new Point(1550, 1280), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                // Caravan — fuel boost pump: etiqueta "FUEL BOOST" (la tapa pròpia s'ha eliminat, ara es
                // reutilitza SW_BONANZA_FUELSEL_R_COVER per Bonanza/Caravan/Cirrus alhora).
                // Mateixa mida que SW_CARAVAN_ALTGEN2_LBL (150x80). Arrossegables amb F12, mida fixa.
                new() { Name="SW_CARAVAN_FUELBOOST_LBL", TypeKey="CARAVAN_FUELBOOST_LBL", BasePos=new Point(1780, 1260), ZoomWidth=150, ZoomHeight=80, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                // Caravan — etiqueta "GENE / RATOR", mateix estil/mida que SW_CARAVAN_ALTGEN2_LBL (STBY/ALT PWR).
                new() { Name="SW_CARAVAN_GENERATOR_LBL", TypeKey="CARAVAN_GENERATOR_LBL", BasePos=new Point(1780, 1350), ZoomWidth=150, ZoomHeight=80, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                // DA-62 — etiqueta "MASTER", mateix estil que FUEL BOOST però d'una sola línia (alçada
                // adaptada a 1 línia de text en lloc de 2).
                new() { Name="SW_DA62_MASTER_LBL", TypeKey="DA62_MASTER_LBL", BasePos=new Point(1624, 1600), ZoomWidth=150, ZoomHeight=40, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsDA62() },
                // DA-62 — capa opaca sense text, mateixa alçada que MASTER però 15% més estreta (150 -> 128).
                new() { Name="SW_DA62_MASTER_COVER", TypeKey="DA62_MASTER_COVER", BasePos=new Point(1794, 1604), ZoomWidth=128, ZoomHeight=40, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsDA62() },
                // DA-62 — duplicat de MASTER_LBL/MASTER_COVER, 200px més a l'esquerra: aquesta parella és la
                // que Pere ha calibrat justa sobre BTN_IGNITION1 (veure calibration_export.txt), per això
                // també es mostra a la DA-40.
                new() { Name="SW_DA62_MASTER_LBL_2", TypeKey="DA62_MASTER_LBL_2", BasePos=new Point(1424, 1600), ZoomWidth=150, ZoomHeight=40, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsDA62() || IsDA40() },
                new() { Name="SW_DA62_MASTER_COVER_2", TypeKey="DA62_MASTER_COVER_2", BasePos=new Point(1594, 1604), ZoomWidth=128, ZoomHeight=40, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsDA62() || IsDA40() },
                // DA-62 — etiqueta "Diamond DA-62" en cursiva.
                new() { Name="SW_DA62_TITLE_LBL", TypeKey="DA62_TITLE_LBL", BasePos=new Point(1424, 1500), ZoomWidth=300, ZoomHeight=40, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsDA62() },
                // DA-40 — idèntica a SW_DA62_TITLE_LBL (mateixa mida/posició/estil), text "Diamond DA40".
                new() { Name="SW_DA40_TITLE_LBL", TypeKey="DA40_TITLE_LBL", BasePos=new Point(1424, 1500), ZoomWidth=300, ZoomHeight=40, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsDA40() },
                // Caravan — idèntica a SW_DA62_TITLE_LBL (mateixa mida/posició/estil), text "Cessna Caravan".
                new() { Name="SW_CARAVAN_TITLE_LBL", TypeKey="CARAVAN_TITLE_LBL", BasePos=new Point(1424, 1500), ZoomWidth=300, ZoomHeight=40, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                // DA-62 — capa "tapa" amb relleu (efecte sortint, no enfonsat), mateix color que la resta
                // d'etiquetes. Arrossegable I redimensionable amb F12.
                new() { Name="SW_DA62_COVER_RELIEF", TypeKey="DA62_COVER_RELIEF", BasePos=new Point(1424, 1450), ZoomWidth=150, ZoomHeight=80, Img=null,
                        ShowWhen = () => IsDA62() || IsDA40() || IsC408() || IsCaravan() },
                // DA-40 — tapa amb relleu independent (Pere l'ha posicionat al damunt dels selectors de
                // combustible via F12, veure calibration_export.txt). Mateix efecte que SW_DA62_COVER_RELIEF.
                new() { Name="SW_DA40_COVER_RELIEF", TypeKey="DA40_COVER_RELIEF", BasePos=new Point(1424, 1450), ZoomWidth=150, ZoomHeight=80, Img=null,
                        // Calibrada (1283,1767,713,334) just a sobre del selector de combustible únic
                        // (SE_SELECTOR) — treta per la DA-40 a 2020 (2026-08-28) i ara també a 2024
                        // (2026-08-30, selector resolt via B:FUEL_Selector_1_Left/_Right/_Off).
                        ShowWhen = () => false },
                // 2 tapes més amb el mateix efecte de relleu — posició/mida per defecte provisionals,
                // Pere les col·locarà amb F12. ShowWhen = DA-40 per defecte (context actual); demanar-li
                // si en vol una altra en veure-les.
                new() { Name="SW_DA40_COVER_RELIEF_3", TypeKey="DA40_COVER_RELIEF_3", BasePos=new Point(1424, 1450), ZoomWidth=150, ZoomHeight=80, Img=null,
                        ShowWhen = () => IsDA40() },
                new() { Name="SW_DA40_COVER_RELIEF_4", TypeKey="DA40_COVER_RELIEF_4", BasePos=new Point(916, 1510), ZoomWidth=283, ZoomHeight=227, Img=null,
                        ShowWhen = () => IsDA40() },
                // Cirrus SR22T / C400 — tapa amb relleu, mateix efecte que les de DA-62/DA-40. Posició/mida
                // per defecte provisionals, Pere la col·locarà amb F12.
                new() { Name="SW_CIRRUS_COVER_RELIEF", TypeKey="CIRRUS_COVER_RELIEF", BasePos=new Point(1424, 1450), ZoomWidth=150, ZoomHeight=80, Img=null,
                        ShowWhen = () => IsCirrusSR22() || IsC400() || IsC172() || IsBonanzaG36() },
                // Caravan — tapa del selector d'ignition/start DRET (la Caravan només fa servir l'esquerre).
                // NO reutilitza SW_CIRRUS_COVER_RELIEF (compartida amb Cirrus/C400/C172, coordenades pròpies
                // d'aquell grup) perquè hi quedava damunt del rotatiu de 5 posicions. Posició presa de
                // SW_DA40_COVER_RELIEF_4 (920,1514,283,227), confirmada per Pere com a ben posada per aquest
                // mateix propòsit (ignition/start dret) a la DA-40.
                new() { Name="SW_CARAVAN_COVER_IGNITION_R", TypeKey="CARAVAN_COVER_IGNITION_R", BasePos=new Point(916, 1510), ZoomWidth=283, ZoomHeight=227, Img=null,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CIRRUS_COVER_RELIEF_2", TypeKey="CIRRUS_COVER_RELIEF_2", BasePos=new Point(1424, 1450), ZoomWidth=150, ZoomHeight=80, Img=null,
                        ShowWhen = () => IsCirrusSR22() || IsC400() || IsC172() || IsBaronG58() || IsBonanzaG36() },
                // C172 — tapa amb relleu per l'interruptor "R ALT/GEN 2" (ALTGEN2-TOGGLE), no aplicable en
                // aquest avió (alternador únic). Posició/mida per defecte provisionals, Pere la col·locarà
                // amb F12.
                new() { Name="SW_C172_ALTGEN2_COVER", TypeKey="C172_ALTGEN2_COVER", BasePos=new Point(1620, 1220), ZoomWidth=150, ZoomHeight=80, Img=null,
                        ShowWhen = () => IsC172() },
                // Cirrus SR22T — etiquetes PRIME/OFF/BOOST (bomba de combustible), mateix estil/mida
                // reduïda que SW_BONANZA_FUELPUMP_LBL_OFF/_LO/_HI (77x41, Arial Narrow 22f).
                new() { Name="SW_CIRRUS_FUELPUMP_LBL_PRIME", TypeKey="CIRRUS_FUELPUMP_LBL", BasePos=new Point(1980, 1170), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCirrusSR22() },
                new() { Name="SW_CIRRUS_FUELPUMP_LBL_OFF", TypeKey="CIRRUS_FUELPUMP_LBL", BasePos=new Point(1980, 1220), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCirrusSR22() },
                new() { Name="SW_CIRRUS_FUELPUMP_LBL_BOOST", TypeKey="CIRRUS_FUELPUMP_LBL", BasePos=new Point(1980, 1280), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCirrusSR22() },
                // Caravan — 3 capes opaques sense text, mateix color de fons, arrossegables i redimensionables.
                new() { Name="SW_CARAVAN_COVER_1", TypeKey="CARAVAN_COVER_1", BasePos=new Point(1980, 1350), ZoomWidth=150, ZoomHeight=80, Img=null,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CARAVAN_COVER_2", TypeKey="CARAVAN_COVER_2", BasePos=new Point(2180, 1350), ZoomWidth=150, ZoomHeight=80, Img=null,
                        ShowWhen = () => IsCaravan() },
                // Caravan — etiqueta "FUEL TANK / SELECTORS", mateix color que SW_CARAVAN_COVER_1/2, text 1.5x
                // de FUEL BOOST (32f). Arrossegable I redimensionable amb F12 (a diferència de les altres etiquetes).
                new() { Name="SW_CARAVAN_FUELTANK_SELECTORS_LBL", TypeKey="CARAVAN_FUELTANK_SELECTORS_LBL", BasePos=new Point(1780, 1450), ZoomWidth=150, ZoomHeight=80, Img=null,
                        ShowWhen = () => IsCaravan() },
                // Bonanza G36 / Caravan / Cirrus SR22T / C400 — capa opaca sense text compartida (mateix
                // Name per totes => mateixa posició/mida sempre, un únic element reutilitzat), mateix estil
                // que SW_CARAVAN_COVER_1/2. Abans hi havia una tapa pròpia per Caravan
                // (SW_CARAVAN_FUELBOOST_COVER, eliminada) i una altra per Cirrus (SW_CIRRUS_COVER_RELIEF_3,
                // eliminada) — ara totes reutilitzen aquesta mateixa capa. Arrossegable i redimensionable
                // amb F12.
                new() { Name="SW_BONANZA_FUELSEL_R_COVER", TypeKey="BONANZA_FUELSEL_R_COVER", BasePos=new Point(1780, 1962), ZoomWidth=150, ZoomHeight=240, Img=null,
                        ShowWhen = () => IsBonanzaG36() || IsCaravan() || IsCirrusSR22() || IsC400() || IsC172() },
                // Bonanza G36 — etiquetes OFF/LO/HI, mateix estil/mida que RESET/ON/TRIP de la Caravan.
                new() { Name="SW_BONANZA_FUELPUMP_LBL_OFF", TypeKey="BONANZA_FUELPUMP_LBL", BasePos=new Point(2002, 1923), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBonanzaG36() && _isMsfs2024 },
                new() { Name="SW_BONANZA_FUELPUMP_LBL_LO", TypeKey="BONANZA_FUELPUMP_LBL", BasePos=new Point(2003, 1881), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBonanzaG36() && _isMsfs2024 },
                new() { Name="SW_BONANZA_FUELPUMP_LBL_HI", TypeKey="BONANZA_FUELPUMP_LBL", BasePos=new Point(2005, 1838), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBonanzaG36() && _isMsfs2024 },
                // Baron G58 — etiquetes OFF/LO/HI per la bomba de combustible ESQUERRA. SW_DN_FUELPUMP1
                // (el switch real, compartit entre avions) ja no és al BasePos original d'aquest fitxer de
                // codi (1804,1653): Pere l'ha calibrat en real a (2074,1828) — veure calibration_export.txt.
                // Les coordenades d'aquí sota reprodueixen l'offset EXACTE que ja té la Bonanza respecte
                // al MATEIX switch compartit (SW_BONANZA_FUELPUMP_LBL_HI/LO/OFF calibrades a
                // 1997/1995/1994, 1840/1883/1925 — uns 78px a l'esquerra del switch). Com que Baron i
                // Bonanza comparteixen el mateix SW_DN_FUELPUMP1, aquest valor cau exactament al mateix
                // lloc relatiu. Ordre DE BAIX CAP A DALT: OFF, LO, HI.
                new() { Name="SW_BARON_FUELPUMP1_LBL_HI", TypeKey="BARON_FUELPUMP1_LBL", BasePos=new Point(2005, 1838), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBaronG58() && _isMsfs2024 },
                new() { Name="SW_BARON_FUELPUMP1_LBL_LO", TypeKey="BARON_FUELPUMP1_LBL", BasePos=new Point(2003, 1881), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBaronG58() && _isMsfs2024 },
                new() { Name="SW_BARON_FUELPUMP1_LBL_OFF", TypeKey="BARON_FUELPUMP1_LBL", BasePos=new Point(2002, 1923), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBaronG58() && _isMsfs2024 },
                // Bomba DRETA — mateix offset respecte SW_DN_FUELPUMP2/SW_CN_FUELPUMP2 (switch real,
                // calibrat a x=2226,y=1828) que el que ja tenen les etiquetes de la bomba esquerra respecte
                // SW_DN_FUELPUMP1 (x=2074,y=1828, mateixa Y que la dreta). Ordre DE BAIX CAP A DALT: OFF, LO, HI.
                new() { Name="SW_BARON_FUELPUMP2_LBL_HI", TypeKey="BARON_FUELPUMP2_LBL", BasePos=new Point(2157, 1838), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBaronG58() && _isMsfs2024 },
                new() { Name="SW_BARON_FUELPUMP2_LBL_LO", TypeKey="BARON_FUELPUMP2_LBL", BasePos=new Point(2155, 1881), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBaronG58() && _isMsfs2024 },
                new() { Name="SW_BARON_FUELPUMP2_LBL_OFF", TypeKey="BARON_FUELPUMP2_LBL", BasePos=new Point(2154, 1923), ZoomWidth=71, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsBaronG58() && _isMsfs2024 },
                // Caravan — etiquetes ON/OFF/NORM pel fuel boost pump. Mateix estil/mida que les RESET/ON/TRIP.
                new() { Name="SW_CARAVAN_FUELBOOST_LBL_ON", TypeKey="CARAVAN_FUELBOOST_POS_LBL", BasePos=new Point(1950, 1170), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CARAVAN_FUELBOOST_LBL_OFF", TypeKey="CARAVAN_FUELBOOST_POS_LBL", BasePos=new Point(1950, 1220), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CARAVAN_FUELBOOST_LBL_NORM", TypeKey="CARAVAN_FUELBOOST_POS_LBL", BasePos=new Point(1950, 1280), ZoomWidth=77, ZoomHeight=41, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                // Caravan (2020 i 2024) — etiquetes MOTOR/OFF/START pel STARTER (3 posicions), mateixes mida i
                // offset relatius al seu switch (BTN_STARTER1, centre real 704,1676) que ja té FUEL BOOST
                // respecte al seu propi switch (SW_DN_FUELPUMP1, 2074,1828): START (dalt, com ON) offset
                // (-79,+11), OFF (centre, com NORM) offset (-80,+53), MOTOR (baix, com OFF) offset (-79,+97).
                new() { Name="SW_CARAVAN_STARTER1_LBL_START", TypeKey="CARAVAN_STARTER1_POS_LBL", BasePos=new Point(605, 1612), ZoomWidth=69, ZoomHeight=37, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CARAVAN_STARTER1_LBL_OFF", TypeKey="CARAVAN_STARTER1_POS_LBL", BasePos=new Point(604, 1654), ZoomWidth=69, ZoomHeight=37, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CARAVAN_STARTER1_LBL_MOTOR", TypeKey="CARAVAN_STARTER1_POS_LBL", BasePos=new Point(605, 1698), ZoomWidth=69, ZoomHeight=37, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                // Caravan — etiquetes NORM/ON per l'IGNITION (2 posicions), mateix patró/mida que les del
                // STARTER, relatives al centre real de BTN_IGNITION1 (843,1671): ON (dalt, com START)
                // offset (-99,-64), NORM (centre, com OFF) offset (-100,-22).
                new() { Name="SW_CARAVAN_IGNITION1_LBL_ON", TypeKey="CARAVAN_IGNITION1_POS_LBL", BasePos=new Point(744, 1607), ZoomWidth=69, ZoomHeight=37, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                new() { Name="SW_CARAVAN_IGNITION1_LBL_NORM", TypeKey="CARAVAN_IGNITION1_POS_LBL", BasePos=new Point(743, 1649), ZoomWidth=69, ZoomHeight=37, Img=null,
                        NoResize = true,
                        ShowWhen = () => IsCaravan() },
                // Monomotors — rectangle negre que tapa els 2 selectors físics de tank (LEFT/RIGHT) mentre
                // preparem el selector únic de 3 posicions. Arrossegable/redimensionable amb F12.
                // De moment només actiu a la DA-40; s'anirà activant per altres monomotors més endavant.
                new() { Name="SW_SE_SELECTOR_COVER", TypeKey="SE_COVER", BasePos=new Point(1280, 1757), ZoomWidth=753, ZoomHeight=350, Img=null,
                        // Revertit (2026-08-28): aquesta tapa NO calia treure-la — s'amaga l'art de fons
                        // (les 2 manetes velles fotografiades) i es dibuixa ABANS del selector nou (mateix
                        // patró que la Cirrus). El problema real era SW_DA40_COVER_RELIEF (a sota).
                        ShowWhen = () => IsDA40() || IsC400() || IsC172() || IsBonanzaG36() || IsCirrusSR22() || IsC408() },
                // Selector únic de 3 posicions, centrat sobre el rectangle de tapa. Reutilitza la mateixa
                // gràfica que els selectors twin-engine (mateix estil de pom vermell). Posició provisional,
                // a reposicionar/redimensionar amb F12 un cop calibrat el rectangle de sobre.
                new() { Name="SW_SE_SELECTOR", TypeKey="SE_SELECTOR", BasePos=new Point(1606, 1877), ZoomWidth=180,
                        Img=swFuelImg, ShowWhen = () => IsDA40() || IsC400() || IsC172() || IsBonanzaG36() || IsCirrusSR22() || IsC408() },
                // Etiquetes de les 3 posicions — mateix estil/mida que SW_DA40_FUELTRANSF_LBL (fons fosc +
                // "Arial Narrow" 27px bold). 3 elements independents perquè Pere els posicioni per separat amb F12.
                // El text es tria a SeSelectorLabelText() segons l'avió (DA-40: Normal/Emergency/Off; C400: Left/Right/Off).
                new() { Name="SW_SE_LBL_1", TypeKey="SE_LBL_1", BasePos=new Point(1450, 1877), ZoomWidth=160, ZoomHeight=60, Img=null,
                        NoResize = true, ShowWhen = () => IsDA40() || IsC400() || IsC172() || IsBonanzaG36() || IsCirrusSR22() || IsC408() },
                new() { Name="SW_SE_LBL_2", TypeKey="SE_LBL_2", BasePos=new Point(1620, 1780), ZoomWidth=160, ZoomHeight=60, Img=null,
                        NoResize = true, ShowWhen = () => IsDA40() || IsC400() || IsC172() || IsBonanzaG36() || IsCirrusSR22() || IsC408() },
                new() { Name="SW_SE_LBL_3", TypeKey="SE_LBL_3", BasePos=new Point(1790, 1877), ZoomWidth=160, ZoomHeight=60, Img=null,
                        NoResize = true, ShowWhen = () => IsDA40() || IsC400() || IsC172() || IsBonanzaG36() || IsCirrusSR22() || IsC408() },
                // Títol "TANKS FUEL SELECTOR" a sota del selector únic, compartit pels mateixos avions.
                // Arial normal (no Narrow, a diferència de la resta d'etiquetes), 25% més gran que les
                // SW_SE_LBL_1/2/3 (27px -> ~34px). Posició provisional, a ajustar amb F12.
                new() { Name="SW_SE_TITLE_LBL", TypeKey="SE_TITLE_LBL", BasePos=new Point(1450, 1990), ZoomWidth=500, ZoomHeight=60, Img=null,
                        ShowWhen = () => IsDA40() || IsC400() || IsC172() || IsBonanzaG36() || IsCirrusSR22() || IsC408() },
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
                        Img=swFuelImg, ShowWhen=() => !IsDA40() && !IsC400() && !IsC172() && !IsBonanzaG36() && !IsCirrusSR22() && !IsC408(), OnClick=null },
                new() { Name="SW_FUEL2", TypeKey="SW fuel2", BasePos=new Point(2170, 1080), ZoomWidth=100,
                        Img=swFuelImg, ShowWhen=() => !IsDA40() && !IsC400() && !IsC172() && !IsBonanzaG36() && !IsCirrusSR22() && !IsC408(), OnClick=null },
                new() { Name="SW_BATTERY",    TypeKey="SW battery",    BasePos=new Point(1320, 1080), ZoomWidth=100,
                        Img=swBatteryImg,  ShowWhen = () => masterBatteryOff, OnClick=null },
                new() { Name="SW_AVIONICS1",  TypeKey="SW avionics1",  BasePos=new Point(1770, 1280), ZoomWidth=100,
                        Img=swAvionicsImg, ShowWhen = () => avionics1Off,     OnClick=null },
                new() { Name="SW_AVIONICS2",  TypeKey="SW avionics2",  BasePos=new Point(1970, 1280), ZoomWidth=100,
                        Img=swAvionicsImg, ShowWhen = () => avionics2Off,     OnClick=null },
                new() { Name="SW_ALT1", TypeKey="SW altgen1", BasePos=new Point(1420, 1280), ZoomWidth=100,
                        Img=swDnRedImg, ShowWhen = () => altGen1Off && !_altGen1ResetAnim, OnClick=null },
                new() { Name="SW_ALT2", TypeKey="SW altgen2", BasePos=new Point(1620, 1280), ZoomWidth=100,
                        Img=swDnRedImg, ShowWhen = () => altGen2Off, OnClick=null },
                // Caravan — posició de repòs/ON (centre) del switch d'alternador, mateix patró que
                // SW_CN_FUELPUMP1/SW_DN_FUELPUMP1: es va alternant amb SW_ALT1 segons altGen1Off.
                new() { Name="SW_ALT1_CENTER", TypeKey="SW CN red", BasePos=new Point(1420, 1280), ZoomWidth=75,
                        Img=swCenterRedImg, ShowWhen = () => IsCaravan() && !altGen1Off && !_altGen1ResetAnim, OnClick=null },
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
                            if (_isMsfs2024) {
                                // TBM930 (2024, 2026-08-29) — preset MobiFlight/HubHop real (Dylan):
                                // B:TBM930_LIGHTING_TAXI_LANDING_LIGHT_SWITCH_Set, 0=OFF/1=TAXI/2=LDG.
                                if (!landActive && !taxiActive) SendGenericRpn("1 (>B:TBM930_LIGHTING_TAXI_LANDING_LIGHT_SWITCH_Set)");
                                else if (taxiActive && !landActive) SendGenericRpn("2 (>B:TBM930_LIGHTING_TAXI_LANDING_LIGHT_SWITCH_Set)");
                                return;
                            }
                            if (!landActive && !taxiActive) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_LIGHT_TAXI, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                            else if (taxiActive && !landActive) simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.TBM930_LIGHT_LDG, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        } },
                new() { Name="G3K_LIGHT_DN",     TypeKey="G3K light click", BasePos=new Point(30, 200), ZoomWidth=97, ZoomHeight=97,
                        OnClick = () => {
                            if (_isMsfs2024) {
                                // TBM930 (2024) — veure comentari a G3K_LIGHT_UP.
                                if (landActive) SendGenericRpn("1 (>B:TBM930_LIGHTING_TAXI_LANDING_LIGHT_SWITCH_Set)");
                                else if (taxiActive) SendGenericRpn("0 (>B:TBM930_LIGHTING_TAXI_LANDING_LIGHT_SWITCH_Set)");
                                return;
                            }
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
                        OnClick = () => { SetElecStarter(2); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_STARTER1_HELD, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.STARTER1_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY); Task.Run(async () => { await Task.Delay(1500); SetElecStarter(1); });
                        } },
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
                        ShowWhen = () => _isMsfs2024 ? !_tbmPulseLight2024On : _lightingLogo1 < 0.5 },
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
                            if (_isMsfs2024)
                            {
                                // TBM930 (2024) — Behaviors: Mouserect crida (>B:TBM930_FUEL_Selection_Toggle)
                                // directament (sense valor, és un toggle real). Feedback real via
                                // L:1:TBM_Fuel_Selection_Switch_Position (RequestTbmFuelSelection2024).
                                SendGenericRpn("(>B:TBM930_FUEL_Selection_Toggle)");
                                return;
                            }
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
                            void DoLowIdleClick() {
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
                                else MoveThrHandle(_thrHandleDefaultY);
                            }
                            DoLowIdleClick();
                            if (_isMsfs2024 && IsTBM930())
                            {
                                // TBM930 (2024) — mateix truc que G3K_THR_HIGHIDLE/G3K_THR_TAXI: repetir el
                                // click sencer 100ms després (demanat per Pere, 2026-08-29).
                                Task.Run(async () => { await Task.Delay(100); this.BeginInvoke(new Action(DoLowIdleClick)); });
                            }
                        } },
                new() { Name="G3K_THR_HIGHIDLE", TypeKey="G3K thr highidle click", BasePos=new Point(3700, 1500), ZoomWidth=100, ZoomHeight=80,
                        OnClick = () => {
                            void DoHighIdleClick() {
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
                                MoveThrHandle(_thrHandleDefaultY - 170);
                            }
                            DoHighIdleClick();
                            if (_isMsfs2024 && IsTBM930())
                            {
                                // TBM930 (2024) — repetir el click sencer 100ms després, com si l'usuari
                                // hagués clicat 2 vegades seguides (demanat per Pere, 2026-08-29).
                                Task.Run(async () => { await Task.Delay(100); this.BeginInvoke(new Action(DoHighIdleClick)); });
                            }
                        } },
                new() { Name="G3K_THR_TAXI", TypeKey="G3K thr taxi click", BasePos=new Point(3700, 1400), ZoomWidth=100, ZoomHeight=80,
                        OnClick = () => {
                            void DoTaxiClick() {
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
                                }
                            }
                            DoTaxiClick();
                            if (_isMsfs2024 && IsTBM930())
                            {
                                // TBM930 (2024) — mateix truc que G3K_THR_HIGHIDLE: repetir el click sencer
                                // 100ms després (demanat per Pere, 2026-08-29).
                                Task.Run(async () => { await Task.Delay(100); this.BeginInvoke(new Action(DoTaxiClick)); });
                            }
                        } },

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
                // L'animació de flaps ja es repinta sola (retallada a la seva zona, veure
                // PbMain_Paint) — no cal que aquest timer hi afegeixi un Invalidate() sencer a sobre.
                bool needsRepaint = false;
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

            // _thrSyncTimer/SyncThrHandleFromMsfs() ja no s'engega (2026-10-03, Pere: "rebots" quan el
            // throttle ja estava al carril esquerre en obrir l'app) — feia servir _lastThrLeverPos,
            // que es queda congelat en entrar al carril esquerre (mai es torna a actualitzar), i cada
            // 3s reposicionava el handle amb aquell valor vell, barallant-se amb UpdateThrHandleFromLeverPos
            // (que ara té dades fresques, independents de Struct1). Abans no es notava perquè
            // thrLeverPos tampoc s'actualitzava gaire més sovint; ara sí, i xocaven. Cicles manuals
            // taxi/idle/taxi "arreglaven" accidentalment el problema posant _lastThrLeverPos a NaN
            // (el que fa aturar-se SyncThrHandleFromMsfs), però la causa real era aquesta duplicitat.

        }
        private void closeButton_Click(object sender, EventArgs e)
        {
            if (calibrationMode) return;
            this.Close(); // Això dispara Form1_FormClosing i para els threads
        }

        private void OpenConfigurationScreen()
        {
            if (_configForm != null && !_configForm.IsDisposed)
            {
                _configForm.Activate();
                return;
            }
            _configForm = new ConfigurationForm();
            _configForm.FormClosed += (cs, ce) => RefreshNoCompatibleAircraftMessage(forceRecheck: true);
            _configForm.Show(this);
        }

        private void ApplyPfdRegion()
        {
            // FORAT PFD
            Rectangle pfdHole = new Rectangle(
                238,   // X
                47,    // Y
                1323,  // Width
                992    // Height
            );

            // FORAT MFD
            Rectangle mfdHole = new Rectangle(
                2278,  // X
                47,    // Y
                1323,  // Width
                992    // Height
            );

            if (IsDemoMode) { ApplyDemoRegionHoles(pfdHole, mfdHole); return; }

            // Regió completa del Form
            Region formRegion = new Region(new Rectangle(
                0, 0, this.Width, this.Height));
            formRegion.Exclude(pfdHole);
            formRegion.Exclude(mfdHole);

            // Aplica la regió al Form
            this.Region = formRegion;
        }

        private void ApplyG3000Region()
        {
            // Valors ajustats (2026-09-12) perquè coincideixin EXACTAMENT amb la zona transparent
            // real de "main GA_3000 4K panel_m.png" (mesurat píxel a píxel) — abans el forat de
            // MFD i el de TSC esquerra eren una mica més grans que la transparència de la imatge i
            // es menjaven part del bisell opac. Veure [[project_demo_mode]] / conversa 2026-09-12.
            Rectangle pfdHole = new Rectangle(200, 458, 1595, 995);
            Rectangle mfdHole = new Rectangle(2040, 459, 1594, 993);
            Rectangle tscLeftHole = new Rectangle(1867, 1654, 721, 439);
            Rectangle tscRightHole = new Rectangle(2821, 1653, 724, 442);

            if (IsDemoMode) { ApplyDemoRegionHoles(pfdHole, mfdHole, tscLeftHole, tscRightHole); return; }

            Region formRegion = new Region(new Rectangle(
                0, 0, this.Width, this.Height));
            formRegion.Exclude(pfdHole);
            formRegion.Exclude(mfdHole);
            formRegion.Exclude(tscLeftHole);
            formRegion.Exclude(tscRightHole);

            this.Region = formRegion;
        }

        // Mode demo: els mateixos forats que en hardware real (PFD/MFD, i TSC pel G3000), però
        // recalculats a la mida/escala actuals de la finestra redimensionable — deixen veure de
        // veritat el que hi hagi darrere de tota l'app (p. ex. un pop-out real del G1000 de MSFS,
        // o l'escriptori). Cal recalcular-ho cada vegada que la finestra es redimensiona.
        private void ApplyDemoRegionHoles(params Rectangle[] virtualHoles)
        {
            var (scale, offX, offY) = GetDemoTransform();
            if (scale <= 0f) { this.Region = null; return; }

            // this.Region treballa en coordenades relatives a TOTA la finestra (barra de títol
            // i vores incloses) — cal sumar l'offset de pbMain dins la finestra, que en mode
            // demo té vora/títol (a diferència del kiosk sense vores del hardware real).
            Point pbScreenOrigin = pbMain.PointToScreen(Point.Empty);
            int chromeDx = pbScreenOrigin.X - this.Location.X;
            int chromeDy = pbScreenOrigin.Y - this.Location.Y;

            Rectangle ToWindow(Rectangle v) => new Rectangle(
                chromeDx + (int)(offX + v.X * scale), chromeDy + (int)(offY + v.Y * scale),
                Math.Max(1, (int)(v.Width * scale)), Math.Max(1, (int)(v.Height * scale)));

            var formRegion = new Region(new Rectangle(0, 0, this.Width, this.Height));
            foreach (var hole in virtualHoles)
                formRegion.Exclude(ToWindow(hole));

            this.Region = formRegion;

            // Diagnòstic temporal (veure PbMain_Paint/constructor): la finestra demo surt com una
            // tireta mínima des del primer frame en un entorn concret (un sol monitor ultrawide).
            // Registrem els valors reals d'aquest càlcul de "forats" per confirmar si és aquí on
            // es trenca, sense seguir endevinant a cegues.
            try
            {
                System.IO.File.AppendAllText("panelAIR_App_crash.txt",
                    $"\r\n[REGION DEBUG] scale={scale} offX={offX} offY={offY} pbScreenOrigin={pbScreenOrigin} " +
                    $"thisLocation={this.Location} chromeDx={chromeDx} chromeDy={chromeDy} thisSize={this.Width}x{this.Height} " +
                    $"holes=[{string.Join(" | ", virtualHoles.Select(h => $"{h}->{ToWindow(h)}"))}]");
            }
            catch { }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            if (IsDemoMode)
            {
                // MessageBox normal no permet negreta parcial — mateix patró que el diàleg
                // "verification failed" (FlowLayoutPanel + AutoSize, robust a qualsevol DPI).
                using (var welcomeForm = new Form
                {
                    AutoScaleDimensions = new SizeF(96f, 96f),
                    AutoScaleMode = AutoScaleMode.Dpi,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    Text = "panelAIR App — DEMO",
                    BackColor = Color.White,
                    StartPosition = FormStartPosition.CenterParent,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Padding = new Padding(30)
                })
                {
                    var welcomeFlow = new FlowLayoutPanel
                    {
                        FlowDirection = FlowDirection.TopDown,
                        WrapContents = false,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        Dock = DockStyle.Fill
                    };

                    var welcomeFont = new Font("Segoe UI", 11f);
                    var welcomeBoldFont = new Font("Segoe UI", 11f, FontStyle.Bold);
                    var welcomeItalicFont = new Font("Segoe UI", 11f, FontStyle.Italic);
                    var welcomeColor = Color.Black;

                    FlowLayoutPanel WelcomeLine(int marginBottom, params (string text, FontStyle style)[] segments)
                    {
                        var line = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, marginBottom) };
                        foreach (var (text, style) in segments)
                        {
                            var font = style == FontStyle.Bold ? welcomeBoldFont : style == FontStyle.Italic ? welcomeItalicFont : welcomeFont;
                            line.Controls.Add(new Label { Text = text, AutoSize = true, Font = font, ForeColor = welcomeColor, Margin = Padding.Empty });
                        }
                        return line;
                    }

                    welcomeFlow.Controls.Add(WelcomeLine(16,
                        ("You're running ", FontStyle.Regular), ("panelAIR App", FontStyle.Bold), (" in DEMO mode (no ", FontStyle.Regular), ("panelAIR", FontStyle.Bold), (" panel detected).", FontStyle.Regular)));
                    welcomeFlow.Controls.Add(WelcomeLine(4,
                        ("You have 10 minutes to explore the app. This demo runs in a resizable window — resize it or move it to any monitor you like.", FontStyle.Regular)));
                    welcomeFlow.Controls.Add(WelcomeLine(16,
                        ("The full version runs full-screen, fixed to your panelAIR panel.", FontStyle.Italic)));
                    welcomeFlow.Controls.Add(WelcomeLine(24,
                        ("IMPORTANT: Before testing this demo, please download our SFS Link add-on from our website and install it in your MSFS Community folder. You can find it in the ", FontStyle.Regular), ("panelAIR", FontStyle.Bold), (" Downloads tab.", FontStyle.Regular)));

                    var welcomeOkButton = new Button
                    {
                        Text = "OK",
                        Size = new Size(160, 48),
                        FlatStyle = FlatStyle.Flat,
                        BackColor = Color.FromArgb(235, 91, 37),
                        ForeColor = Color.White,
                        Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                        DialogResult = DialogResult.OK
                    };
                    welcomeOkButton.FlatAppearance.BorderSize = 0;
                    welcomeFlow.Controls.Add(welcomeOkButton);

                    welcomeForm.Controls.Add(welcomeFlow);
                    welcomeForm.AcceptButton = welcomeOkButton;
                    welcomeForm.ShowDialog(this);
                }

                _demoStartTime = DateTime.Now;
                int demoTotalSeconds = 600;
                bool demoExpiryHandled = false;   // guard contra callbacks duplicats

                // Compte enrere en una finestreta a part (mateix patró que el "Trial Mode"), NO al
                // títol de la finestra: actualitzar this.Text cada segon feia parpellejar el marc
                // de Windows (barra de títol), que és "chrome" del sistema operatiu, no nostre.
                Form? demoCountdownForm = null;
                Label? demoCountdownLabel = null;

                _demoTimer = new System.Threading.Timer(_ => {
                    int elapsed = (int)(DateTime.Now - _demoStartTime).TotalSeconds;
                    int secondsLeft = demoTotalSeconds - elapsed;
                    if (IsDisposed || !IsHandleCreated) return;
                    try
                    {
                        BeginInvoke(new Action(() => {
                            if (demoExpiryHandled) return;   // descarta callbacks duplicats
                            if (secondsLeft <= 0)
                            {
                                demoExpiryHandled = true;
                                try { demoCountdownForm?.Close(); } catch { }
                                try { Process.Start(new ProcessStartInfo { FileName = "https://simflightservices.com/shop/air-panels/", UseShellExecute = true }); } catch { }

                                // Mateix patró que "verification failed": MessageBox normal no
                                // permet negreta i no aguanta bé qualsevol DPI. Custom Form,
                                // centrat a la pantalla PRINCIPAL (no toca res de screen4K).
                                using (var endedForm = new Form
                                {
                                    AutoScaleDimensions = new SizeF(96f, 96f),
                                    AutoScaleMode = AutoScaleMode.Dpi,
                                    FormBorderStyle = FormBorderStyle.FixedDialog,
                                    MaximizeBox = false,
                                    MinimizeBox = false,
                                    Text = "panelAIR — DEMO ended",
                                    BackColor = Color.White,
                                    StartPosition = FormStartPosition.Manual,
                                    TopMost = true, // s'obre el navegador alhora: cal quedar per davant
                                    AutoSize = true,
                                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                    Padding = new Padding(30)
                                })
                                {
                                    var endedFlow = new FlowLayoutPanel
                                    {
                                        FlowDirection = FlowDirection.TopDown,
                                        WrapContents = false,
                                        AutoSize = true,
                                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                        Dock = DockStyle.Fill
                                    };

                                    var endedFont = new Font("Segoe UI", 11f);
                                    var endedBoldFont = new Font("Segoe UI", 11f, FontStyle.Bold);
                                    var endedColor = Color.Black;

                                    FlowLayoutPanel EndedLine(int marginBottom, params (string text, bool bold)[] segments)
                                    {
                                        var line = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = new Padding(0, 0, 0, marginBottom) };
                                        foreach (var (text, bold) in segments)
                                            line.Controls.Add(new Label { Text = text, AutoSize = true, Font = bold ? endedBoldFont : endedFont, ForeColor = endedColor, Margin = Padding.Empty });
                                        return line;
                                    }

                                    endedFlow.Controls.Add(EndedLine(16, ("Thanks for trying ", false), ("panelAIR", true), (" panel!", false)));
                                    endedFlow.Controls.Add(EndedLine(24, ("We've opened our shop page so you can get the real thing.", false)));

                                    var endedOkButton = new Button
                                    {
                                        Text = "OK",
                                        Size = new Size(160, 48),
                                        FlatStyle = FlatStyle.Flat,
                                        BackColor = Color.FromArgb(235, 91, 37),
                                        ForeColor = Color.White,
                                        Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                                        DialogResult = DialogResult.OK
                                    };
                                    endedOkButton.FlatAppearance.BorderSize = 0;
                                    endedFlow.Controls.Add(endedOkButton);

                                    endedForm.Controls.Add(endedFlow);
                                    endedForm.AcceptButton = endedOkButton;
                                    endedForm.Shown += (es, ee) =>
                                    {
                                        var primary = Screen.PrimaryScreen!.Bounds;
                                        endedForm.Location = new Point(
                                            primary.X + (primary.Width - endedForm.Width) / 2,
                                            primary.Y + (primary.Height - endedForm.Height) / 2);
                                    };
                                    endedForm.ShowDialog(this);
                                }

                                Process.GetCurrentProcess().Kill();
                            }
                            else
                            {
                                // Mateix disseny exacte que la finestreta del Trial Mode (setup
                                // incomplet) — no se n'inventa un de nou.
                                if (demoCountdownForm == null || demoCountdownForm.IsDisposed)
                                {
                                    demoCountdownForm = new Form
                                    {
                                        Text = "panelAIR - DEMO",
                                        Size = new Size(280, 110),
                                        FormBorderStyle = FormBorderStyle.FixedDialog,
                                        StartPosition = FormStartPosition.Manual,
                                        TopMost = true,
                                        ControlBox = false
                                    };
                                    var primary = Screen.PrimaryScreen!.Bounds;
                                    demoCountdownForm.Location = new Point(
                                        primary.X + (primary.Width - demoCountdownForm.Width) / 2,
                                        primary.Y + (primary.Height - demoCountdownForm.Height) / 2);
                                    demoCountdownLabel = new Label
                                    {
                                        Dock = DockStyle.Fill,
                                        TextAlign = ContentAlignment.MiddleCenter,
                                        Font = new Font("Arial", 9f)
                                    };
                                    demoCountdownForm.Controls.Add(demoCountdownLabel);
                                    demoCountdownForm.Show();
                                }
                                demoCountdownLabel!.Text = $"panelAIR DEMO\nClosing in {secondsLeft / 60}:{secondsLeft % 60:D2}...";
                            }
                        }));
                    }
                    catch { }
                }, null, 0, 1000);
            }
            else
            {
                // Touch screen HID check (VID=0457, PID=0819)
                bool hasPanel = false;
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%VID_0457%PID_0819%' AND ConfigManagerErrorCode = 0 AND Present = TRUE"))
                {
                    hasPanel = searcher.Get().Count > 0;
                }
                bool hasMonitor = false;
                // La pantalla panelAIR real reporta com a RTK2775 (confirmat 2026-10-18 amb WMI al
                // mateix hardware físic en 3 PC's diferents). XMI3446 ("Mi Monitor") NO és el panell
                // panelAIR — és un monitor normal que Pere té al seu escriptori, no relacionat.
                using (var searcher = new System.Management.ManagementObjectSearcher(
                    @"SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE '%RTK2775%' AND ConfigManagerErrorCode = 0"))
                {
                    hasMonitor = searcher.Get().Count > 0;
                }

                // Mòduls rotAIR (VID 0x16D0): n'hi ha prou amb detectar-ne 1 dels 5 tipus.
                bool hasRotAIR = false;
                int[] rotAirPids = { 0x128F, 0x13E5, 0x1290, 0x13E4, 0x13E3 }; // Type-1 GC, Type-2 GCAM, Type-1 ANA, Type-2 ANA, Type-2 GC
                foreach (int rotAirPid in rotAirPids)
                {
                    if (DeviceList.Local.GetHidDevices(0x16D0, rotAirPid).Any())
                    {
                        hasRotAIR = true;
                        break;
                    }
                }

                if (!hasPanel || !hasMonitor || !hasRotAIR)
                {
                    var screen4K = Screen.AllScreens.FirstOrDefault(s => !s.Primary && s.Bounds.Width == 3840 && s.Bounds.Height == 2160)
                        ?? Screen.AllScreens.First(s => s.Bounds.Width == 3840 && s.Bounds.Height == 2160);
                    using (var verifyFailedForm = new Form
                    {
                        AutoScaleDimensions = new SizeF(96f, 96f),
                        AutoScaleMode = AutoScaleMode.Dpi,
                        FormBorderStyle = FormBorderStyle.Sizable,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        ControlBox = false,
                        Text = "panelAIR",
                        BackColor = Color.White,
                        StartPosition = FormStartPosition.Manual,
                        AutoSize = true,
                        AutoSizeMode = AutoSizeMode.GrowAndShrink,
                        Padding = new Padding(30)
                    })
                    {
                        // Deixem que el propi motor de layout de WinForms (FlowLayoutPanel + AutoSize
                        // en cascada, AutoScaleMode.Dpi) calculi mides/posicions, en comptes de
                        // calcular-les nosaltres a mà en píxels — dos intents manuals (AutoSize+Bottom/
                        // Right, i després GetPreferredSize() explícit) van fallar igualment en canviar
                        // de DPI. Sizable: si tot i així queda retallat, es pot arrossegar la vora.
                        var flow = new FlowLayoutPanel
                        {
                            FlowDirection = FlowDirection.TopDown,
                            WrapContents = false,
                            AutoSize = true,
                            AutoSizeMode = AutoSizeMode.GrowAndShrink,
                            Dock = DockStyle.Fill
                        };

                        var titleLbl = new Label
                        {
                            Text = "panelAIR App - verification failed",
                            AutoSize = true,
                            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
                            ForeColor = Color.FromArgb(22, 32, 42),
                            Margin = new Padding(0, 0, 0, 20)
                        };
                        flow.Controls.Add(titleLbl);

                        var bodyFont = new Font("Segoe UI", 14f);
                        var bodyBoldFont = new Font("Segoe UI", 14f, FontStyle.Bold);
                        var bodyColor = Color.FromArgb(60, 60, 60);

                        FlowLayoutPanel MakeBoldLine(string before, string bold, string after, int marginBottom)
                        {
                            var line = new FlowLayoutPanel
                            {
                                FlowDirection = FlowDirection.LeftToRight,
                                WrapContents = false,
                                AutoSize = true,
                                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                                Margin = new Padding(0, 0, 0, marginBottom)
                            };
                            line.Controls.Add(new Label { Text = before, AutoSize = true, Font = bodyFont, ForeColor = bodyColor, Margin = Padding.Empty });
                            line.Controls.Add(new Label { Text = bold, AutoSize = true, Font = bodyBoldFont, ForeColor = bodyColor, Margin = Padding.Empty });
                            line.Controls.Add(new Label { Text = after, AutoSize = true, Font = bodyFont, ForeColor = bodyColor, Margin = Padding.Empty });
                            return line;
                        }
                        Label MakeLine(string text, int marginTop, int marginBottom)
                            => new Label { Text = text, AutoSize = true, Font = bodyFont, ForeColor = bodyColor, Margin = new Padding(0, marginTop, 0, marginBottom) };

                        flow.Controls.Add(MakeBoldLine("If you own a ", "panelAIR", " panel, please ensure that:", 14));
                        flow.Controls.Add(MakeLine("1.  The panelAIR screen is powered ON.", 0, 2));
                        flow.Controls.Add(MakeLine("2.  The panelAIR USB \"touch\" cable is connected to the computer.", 0, 2));
                        flow.Controls.Add(MakeLine("3.  The RotAIR module USB cables are connected to the computer.", 0, 20));
                        flow.Controls.Add(MakeLine("Then restart the application.", 0, 20));
                        flow.Controls.Add(MakeLine("(Until these 3 points are resolved, the application will remain functional for 10 minutes only.)", 0, 30));

                        var okButton = new Button
                        {
                            Text = "OK",
                            Size = new Size(160, 48),
                            FlatStyle = FlatStyle.Flat,
                            BackColor = Color.FromArgb(235, 91, 37),
                            ForeColor = Color.White,
                            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                            DialogResult = DialogResult.OK
                        };
                        okButton.FlatAppearance.BorderSize = 0;
                        flow.Controls.Add(okButton);

                        verifyFailedForm.Controls.Add(flow);
                        verifyFailedForm.AcceptButton = okButton;

                        // Centrat a la pantalla PRINCIPAL (no la panelAIR): si el client té el cable
                        // de vídeo connectat però la pantalla panelAIR apagada, mai veuria l'avís si
                        // sortís allà. Cal fer-ho a Shown (no abans), perquè amb AutoSize=true la mida
                        // final de la finestra només es coneix amb certesa un cop ja s'ha fet el
                        // primer layout complet.
                        verifyFailedForm.Shown += (s, e) =>
                        {
                            var primary = Screen.PrimaryScreen!.Bounds;
                            verifyFailedForm.Location = new Point(
                                primary.X + (primary.Width - verifyFailedForm.Width) / 2,
                                primary.Y + (primary.Height - verifyFailedForm.Height) / 2);
                        };

                        verifyFailedForm.ShowDialog();
                    }
                    var countdownForm = new Form
                    {
                        Text = "panelAIR - Trial Mode",
                        Size = new Size(280, 110),
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        StartPosition = FormStartPosition.Manual,
                        TopMost = true,
                        ControlBox = false
                    };
                    // Pantalla PRINCIPAL, no la panelAIR (mateix motiu que el diàleg "verification
                    // failed": si la pantalla panelAIR està apagada, mai es veuria aquí).
                    var countdownPrimary = Screen.PrimaryScreen!.Bounds;
                    countdownForm.Location = new Point(
                        countdownPrimary.X + (countdownPrimary.Width - countdownForm.Width) / 2,
                        countdownPrimary.Y + (countdownPrimary.Height - countdownForm.Height) / 2
                    );
                    var label = new Label
                    {
                        Text = "No panelAIR detected.\nClosing in 10:00...",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = new Font("Arial", 9f)
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
                                    label.Text = $"No panelAIR detected.\nClosing in {secondsLeft / 60}:{secondsLeft % 60:D2}...";
                                }
                            }));
                        }
                        catch { }
                    }, null, 0, 200);
                }
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

            _dataPollTimer = new System.Windows.Forms.Timer();
            // Floor a 1000ms en repòs (sense cost, confirmat), puja a 200ms quan s'envia una comanda
            // (veure MarkCommandSent) i hi torna als 2s sense cap comanda nova (2026-10-03, Pere).
            _dataPollTimer.Interval = 1000;
            _dataPollTimer.Tick += (s3, ev3) => PollLowFrequencySimData();

            // Mode DEMO: quan MSFS SÍ està connectat, les dades que arriben contínuament ja forcen
            // repintats freqüents que "amaguen" qualsevol problema de repintat parcial en redimensionar.
            // Quan MSFS NO hi és, no arriba res i la finestra es queda congelada amb el que hi hagi
            // mig dibuixat. Aquest timer substitueix aquell refresc continu només quan fa falta
            // (2026-10-14, a petició de Pere, en comptes de seguir perseguint la causa arrel).
            _demoDisconnectedRefreshTimer = new System.Windows.Forms.Timer();
            _demoDisconnectedRefreshTimer.Interval = 50;
            _demoDisconnectedRefreshTimer.Tick += (s3b, ev3b) =>
            {
                if (IsDemoMode && simconnect == null) pbMain.Invalidate();
            };
            _demoDisconnectedRefreshTimer.Start();

            _flapsActiveReadTimer = new System.Windows.Forms.Timer();
            _flapsActiveReadTimer.Interval = 50; // veure comentari del camp
            _flapsActiveReadTimer.Tick += (s3c, ev3c) =>
            {
                if (DateTime.UtcNow >= _flapsActiveReadUntilUtc)
                {
                    _flapsActiveReadTimer!.Stop();
                    return;
                }
                SyncFlapsHandleIndexOnce(); // num_positions no cal re-llegir-lo activament, és constant
            };

            _elevatorTrimActiveReadTimer = new System.Windows.Forms.Timer();
            _elevatorTrimActiveReadTimer.Interval = 50; // sobreescrit per UpdateElevatorTrimActiveRate() en Start()
            _elevatorTrimActiveReadTimer.Tick += (s3d, ev3d) => SyncElevatorTrimOnce();

            _rudderTrimActiveReadTimer = new System.Windows.Forms.Timer();
            _rudderTrimActiveReadTimer.Interval = 50; // prova 2026-10-03
            _rudderTrimActiveReadTimer.Tick += (s3e, ev3e) => SyncRudderTrimOnce();

            _flapsAnimTimerG1000 = new System.Windows.Forms.Timer();
            _flapsAnimTimerG1000.Interval = 33; // veure comentari del camp
            _flapsAnimTimerG1000.Tick += (s4, ev4) =>
            {
                if (flapsAnimDurationSec <= 0f)
                {
                    _flapsAnimTimerG1000!.Stop();
                    return;
                }
                const int yUp = 450 + 1144;
                const int yFull = 680 + 1144;
                const int diameter = 20;
                const int margin = 20;
                var flapsIndRect = new Rectangle(
                    flapsIndX - margin,
                    yUp + flapsIndYOffset - margin,
                    diameter + margin * 2,
                    (yFull - yUp) + diameter + margin * 2);
                pbMain.Invalidate(ToRealInvalidateRect(flapsIndRect));
            };

            _flapsAnimTimerG3000 = new System.Windows.Forms.Timer();
            _flapsAnimTimerG3000.Interval = 33; // veure comentari del camp
            _flapsAnimTimerG3000.Tick += (s5, ev5) =>
            {
                if (_g3kFlapsAnimDurationSec <= 0f)
                {
                    _flapsAnimTimerG3000!.Stop();
                    return;
                }
                const int G3K_FLI_RANGE = 193;
                const int G3K_FLI_DIAM = 17;
                const int G3K_FLI_BASE_Y = 450;
                const int margin = 20;
                int yUp = G3K_FLI_BASE_Y + _g3kFlapsIndYOffset;
                var g3kFlapsIndRect = new Rectangle(
                    _g3kFlapsIndX - margin,
                    yUp - margin,
                    G3K_FLI_DIAM + margin * 2,
                    G3K_FLI_RANGE + G3K_FLI_DIAM + margin * 2);
                pbMain.Invalidate(ToRealInvalidateRect(g3kFlapsIndRect));
            };

            _g3kFuelSelAnimTimer = new System.Windows.Forms.Timer();
            _g3kFuelSelAnimTimer.Interval = 33; // veure comentari del camp
            _g3kFuelSelAnimTimer.Tick += (s10, ev10) =>
            {
                if (_g3kFuelSelAngle == _g3kFuelSelTargetAngle)
                {
                    _g3kFuelSelAnimTimer!.Stop();
                    return;
                }
                var sw = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_FSEL_SW");
                if (sw?.Img == null) { _g3kFuelSelAnimTimer!.Stop(); return; }
                int margin = Math.Max(sw.Img.Width, sw.Img.Height) / 2;
                var swRect = new Rectangle(
                    sw.BasePos.X - margin,
                    sw.BasePos.Y - margin,
                    sw.Img.Width + margin * 2,
                    sw.Img.Height + margin * 2);
                pbMain.Invalidate(ToRealInvalidateRect(swRect));
            };

            _fuelTankSelectorActiveReadTimer = new System.Windows.Forms.Timer();
            _fuelTankSelectorActiveReadTimer.Interval = 50; // veure comentari del camp
            _fuelTankSelectorActiveReadTimer.Tick += (s11, ev11) =>
            {
                if (DateTime.UtcNow >= _fuelTankSelectorActiveReadUntilUtc)
                {
                    _fuelTankSelectorActiveReadTimer!.Stop();
                    return;
                }
                PollFuelTankSelectors();
            };

            _thrLeverActiveReadTimer = new System.Windows.Forms.Timer();
            _thrLeverActiveReadTimer.Interval = 50; // veure comentari del camp
            _thrLeverActiveReadTimer.Tick += (s13, ev13) =>
            {
                if (DateTime.UtcNow >= _thrLeverActiveReadUntilUtc)
                {
                    _thrLeverActiveReadTimer!.Stop();
                    return;
                }
                SyncThrLeverPosOnce();
            };

            ConnectToSimConnect();

            // SFSmonitor detection — no aplica en mode demo (sense hardware, no té sentit exigir-ho)
            if (!IsDemoMode)
            {
                sfsMonitorRunning = Process.GetProcesses().Any(p => p.ProcessName.StartsWith("SFSmonitor"));
                if (!sfsMonitorRunning && AppSettings.Instance.SfsMonitorAutoLaunch
                    && !string.IsNullOrWhiteSpace(AppSettings.Instance.SfsMonitorPath)
                    && System.IO.File.Exists(AppSettings.Instance.SfsMonitorPath))
                {
                    try
                    {
                        var sfsProc = Process.Start(new ProcessStartInfo { FileName = AppSettings.Instance.SfsMonitorPath, UseShellExecute = true });
                        if (sfsProc != null) ProcessWindowMover.MoveToPrimaryScreenWhenReady(sfsProc);
                    }
                    catch { }
                }
                if (!sfsMonitorRunning) ShowSfsWarning();
                _sfsCheckTimer = new System.Threading.Timer(_ =>
                {
                    if (IsDisposed || !IsHandleCreated) return;
                    bool running = Process.GetProcesses().Any(p => p.ProcessName.StartsWith("SFSmonitor"));
                    try { Invoke(() => { sfsMonitorRunning = running; if (running) HideSfsWarning(); else ShowSfsWarning(); }); }
                    catch (ObjectDisposedException) { }
                }, null, 3000, 3000);
            }

            // Si s'ha obert RAM abans que MSFS, quedem en "waiting" i reintentem sols.
            if (simconnect == null)
            {
                textBox21.Text = "waiting";
                textBox21.ForeColor = Color.Orange;

                simReconnectPending = true;
                simReconnectTimer.Start();
            }

            this.Text = "panelAIR";

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

            // X-Plane 12 (ToLiss A320): enllaç per l'API web d'X-Plane. Es busca sol cada 3 s; quan hi ha un ToLiss
            // carregat activa el mode A320 i les ordres del panell van a X-Plane en lloc de l'MSFS.
            _xp = new XPlaneLink();
            _xp.A320Changed += on =>
            {
                if (on) SetA320Active(true);
                else if (!IsA320FBW()) SetA320Active(false);
            };
            _xp.BaroChanged += (p, m, v, hg) =>
            {
                _xpBaro = (p, m, v, hg);
                _a320Overhead?.SetBaro(p, m, v, hg);
            };
            _xp.FcuChanged += s => { _xpFcu = s; _a320Overhead?.SetFcu(s); };
            _xp.AltStepChanged += k => { _xpAltStep = k; _a320Overhead?.SetKnobRing("MIP_FCU_ALT", k); };
            _xp.LightChanged += (id, on) => { _xpLights[id] = on; _a320Overhead?.SetLight(id, on); };
            _xp.PositionChanged += (id, i) => { _xpPos[id] = i; _a320Overhead?.SetPosition(id, i); };
            _xp.A320Changed += on => { if (!on) { _xpLights.Clear(); _xpPos.Clear(); } if (_a320Overhead != null) _a320Overhead.SimDriven = on; };
            _xp.A320Changed += on => { if (!on) { _xpFcu = null; _a320Overhead?.SetFcu(new panelAIR1000.FcuState(false, 0, false, false, false, 0, false, false, false, 0, false, 0, 0, false)); } };
            _xp.Start();

            // PFD del ToLiss (pop-out d'X-Plane) encaixat al forat del PFD de l'app, automàticament
            _xpPfdTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _xpPfdTimer.Tick += (s, ev) => PlaceXPlanePfd();
            _xpPfdTimer.Start();
            _xp.A320Changed += on => { if (!on) Array.Clear(_xpScreenAsked); };

            // Timer per refrescar estat de connexió rotAIR
            var statusTimer = new System.Windows.Forms.Timer();
            statusTimer.Interval = 1000; // cada 1 segon
            statusTimer.Tick += (s, ev) => UpdateRotAIRStatus();
            statusTimer.Start();
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try { CloseXPlaneScreens(); } catch { }
            _xp?.Dispose();
            _sfsCheckTimer?.Dispose();
            stopHidThread1 = true;
            stopHidThread2 = true;
            stopHidThread3 = true;
            stopHidThread4 = true;
            stopHidThread5 = true;

            try
            {
                foreach (var p in Process.GetProcesses().Where(p => p.ProcessName.StartsWith("SFSmonitor")))
                {
                    try { p.CloseMainWindow(); } catch { }
                }
            }
            catch { }
        }
        private void SetElecSourceSwitch(double value)
        {
            if (simconnect == null) return;
            if (_isMsfs2024)
            {
                // TBM930 (2024) — B:ELECTRICAL_SOURCE_SWITCH_Set (0=OFF, 1=BATT, 2=GPU).
                // Feedback real via L:1:TBM_Elec_Source_Switch_State (RequestTbmElecSource2024).
                SendGenericRpn($"{(int)Math.Round(value)} (>B:ELECTRICAL_SOURCE_SWITCH_Set)");
                return;
            }
            simconnect.SetDataOnSimObject(DEFINITIONS.ElecSrcWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new ElecSrcStruct { value = value });
        }

        private void ToggleElecCrashbar()
        {
            if (simconnect == null) return;
            double newVal = _elecCrashbar < 0.5 ? 1.0 : 0.0;
            if (_isMsfs2024)
            {
                // TBM930 (2024) — B:ELECTRICAL_CRASHBAR_Set (0=tapat/visible, 1=tret/amagat, confirmat
                // per Pere). Feedback real via L:1:TBM_Elec_CrashBar_State (RequestTbmCrashbar2024).
                SendGenericRpn($"{(newVal >= 0.5 ? 1 : 0)} (>B:ELECTRICAL_CRASHBAR_Set)");
            }
            else
            {
                simconnect.SetDataOnSimObject(DEFINITIONS.ElecCrashbarWrite,
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                    new ElecCrashbarStruct { value = newVal });
            }
            if (newVal < 0.5)
            {
                SetElecSourceSwitch(0);
                SetElecGeneratorSwitch(0);
            }
        }

        // Xarxa de seguretat (abans vivia dins el handler de Struct1, cridada cada 10s): si el
        // crashbar és visible (tapat) però source/generator encara marquen ON, els torna a 0. Ara que
        // els 3 camps tenen lectura pròpia independent, es crida des de cadascun dels 3 handlers.
        private void EnforceElecCrashbarConsistency()
        {
            if (_elecCrashbar < 0.5 && (_elecSourceSwitch > 0.5 || _elecGeneratorSwitch > 0.5))
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

        private int _genericRpnSeq = 0;

        // Missatger genèric RPN — envia qualsevol text RPN al WASM (SFS_Link) via Client Data Area,
        // perquè l'executi directe amb execute_calculator_code. Buffer de 1024 bytes; truncar per sobre
        // seria perillós (RPN a mitges), per això avisem en comptes de tallar en silenci.
        // Hipòtesi (2026-08-08, no confirmada): alguns botons (p.ex. IGNITION de la Caravan) envien
        // SEMPRE el mateix text de RPN independentment de l'estat (un toggle pla), i de tant en tant no
        // fan efecte al primer clic — possible que el WASM només reaccioni quan el contingut del Client
        // Data CANVIA de valor, i 2 clics seguits amb el mateix text no generin cap canvi detectable.
        // S'afegeix un número incremental al final (valor sobrant a la pila, inofensiu — les crides
        // K:/B: ja han consumit els seus propis paràmetres abans d'arribar-hi) per garantir que el text
        // sempre sigui diferent del de l'enviament anterior.
        private void SendGenericRpn(string rpn)
        {
            if (simconnect == null) return;
            string fullRpn = $"{rpn} {_genericRpnSeq++ % 1000}";
            if (fullRpn.Length > 1023)
            {
                Debug.WriteLine($"[GenericRPN] RPN massa llarg ({fullRpn.Length} chars, màxim 1023) — NO enviat: {fullRpn}");
                return;
            }
            simconnect.SetClientData(CLIENT_DATA_ID.GenericRpnData, DEFINITIONS.GenericRpnClientDataDef,
                SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new GenericRpnStruct { rpn = fullRpn });
            Debug.WriteLine($"[GenericRPN] enviat: {fullRpn}");
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
            if (_isMsfs2024)
            {
                // TBM930 (2024) — B:ENGINE_IGNITION_1_Set: 0=AUTO, 1=ON, 2=OFF (coincideix amb pos directament).
                SendGenericRpn($"{pos} (>B:ENGINE_IGNITION_1_Set)");
                return;
            }
            double lval = pos + 1;
            simconnect.SetDataOnSimObject(DEFINITIONS.ElecIgnitionWrite,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_DATA_SET_FLAG.DEFAULT,
                new ElecIgnitionStruct { value = lval });
        }

        private void SetAPTrimPos(int pos)
        {
            if (simconnect == null) return;
            if (_isMsfs2024)
            {
                // TBM930 (2024) — Behaviors: B:TBM930_HANDLING_AP_Trim_Set (0=ON/UP, 1=AP OFF/CTR,
                // 2=OFF/DN, mateixa codificació que L:XMLVAR_APTrim a 2020). L'avió gestiona sol
                // l'AUTOPILOT_DISENGAGE_SET des del seu propi Init — no cal replicar la resta de la
                // lògica de 2020. Feedback real via L:1:TBM_AP_Trim_Switch_Position (RequestTbmApTrim2024).
                SendGenericRpn($"{pos} (>B:TBM930_HANDLING_AP_Trim_Set)");
                return;
            }
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
            if (_isMsfs2024)
            {
                // TBM930 (2024) — Behaviors: B:TBM930_LIGHTING_Pulse_Light_Switch_Set (0=OFF/1=ON).
                // No es pot llegir per SimConnect (ni el B: ni la L: companya) — patró "sense lectura":
                // ForceTbmPulseLightOff() sincronitza a OFF quan es detecta l'avió, i des d'aquí el
                // toggle local es manté fiable perquè sempre partim d'un estat real conegut.
                _tbmPulseLight2024On = !_tbmPulseLight2024On;
                uint target = _tbmPulseLight2024On ? 1u : 0u;
                SendGenericRpn($"{target} (>B:TBM930_LIGHTING_PULSE_LIGHT_SWITCH_Set)");
                return;
            }
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
            if (_isMsfs2024 && IsTBM930())
            {
                // TBM930 (2024) — B:DEICE_PROPELLER_1_Set: 1=ON, 0=OFF.
                SendGenericRpn("1 (>B:DEICE_PROPELLER_1_Set)");
                return;
            }
            if (!propDeiceActive)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROPDEICE_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            simconnect.SetDataOnSimObject(DEFINITIONS.PropDeiceWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PropDeiceWriteStruct { value = 1 });
        }

        private void SendPropDeiceOff()
        {
            if (simconnect == null) return;
            if (_isMsfs2024 && IsTBM930())
            {
                // TBM930 (2024) — B:DEICE_PROPELLER_1_Set: 1=ON, 0=OFF.
                SendGenericRpn("0 (>B:DEICE_PROPELLER_1_Set)");
                return;
            }
            if (propDeiceActive)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROPDEICE_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            simconnect.SetDataOnSimObject(DEFINITIONS.PropDeiceWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new PropDeiceWriteStruct { value = 0 });
        }

        private void SendInertSepOn()
        {
            if (simconnect == null) return;
            if (_isMsfs2024 && IsTBM930())
            {
                // TBM930 (2024) — B:TBM930_DEICE_INERT_SEP_SWITCH_Set: 1=ON, 0=OFF.
                SendGenericRpn("1 (>B:TBM930_DEICE_INERT_SEP_SWITCH_Set)");
                return;
            }
            // TBM930 (2020) — mecanisme original, exacte a la comissió "acabada TBM 930 per a MSFS2020"
            // (2026-09-02: restaurat després que les proves d'avui amb l'XML i amb B:DEICE_Engine_1_Toggle
            // ho empitjoressin — Pere confirma que això és el que anava perfecte).
            if (!antiIceEng1Active)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.INERT_SEP_TOGGLE, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void SendInertSepOff()
        {
            if (simconnect == null) return;
            if (_isMsfs2024 && IsTBM930())
            {
                // TBM930 (2024) — B:TBM930_DEICE_INERT_SEP_SWITCH_Set: 1=ON, 0=OFF.
                SendGenericRpn("0 (>B:TBM930_DEICE_INERT_SEP_SWITCH_Set)");
                return;
            }
            // TBM930 (2020) — veure comentari a SendInertSepOn: mecanisme original restaurat.
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
            if (_isMsfs2024 && IsTBM930())
            {
                // TBM930 (2024) — B:TBM930_DEICE_WINDSHIELD_SWITCH_Set: 1=ON, 0=OFF.
                SendGenericRpn($"{newState} (>B:TBM930_DEICE_WINDSHIELD_SWITCH_Set)");
                return;
            }
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.WINDSHIELD_DEICE_SET, newState, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        private void SetFuelPump(double pos)
        {
            if (simconnect == null) return;
            // pos: 0=UP=AUTO(K:2), 1=CENTER=ON(K:1), 2=DN=OFF(K:0)
            uint kval = (uint)(2 - (int)pos);
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.ELECT_FUEL_PUMP1_SET, kval, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }

        // Bomba de 3 posicions (0=HIGH/ON, 1=LOW/AUTO, 2=OFF) per avions sense esquema estàndard universal:
        // Baron G58 (pump 1 i 2, via WASM propi SFS.FuelPumpN_High/Low/Off), Bonanza G36 (monomotor, només
        // pump 1, mateixos events que la Baron — B:FUEL_Pump_1_High/_Low/_Off confirmat idèntic a Behaviors)
        // i Cessna Caravan / Cirrus SR22 (només pump 1, via WASM SFS.FuelPump1_On/Auto/Off).
        // L'estat el porta la pròpia app segons els clics (direction: -1 = cap a HIGH/ON, +1 = cap a OFF),
        // NO depèn de cap lectura del simulador.
        private DateTime _lastPump1ClickUtc = DateTime.MinValue;
        private DateTime _lastPump2ClickUtc = DateTime.MinValue;
        private void StepFuelPumpBaron(int pumpNum, int direction)
        {
            if (simconnect == null) return;
            // Debounce: un sol toc de dit pot generar 2 clics gairebé seguits (rebot tàctil),
            // fent avançar l'estat 2 posicions en comptes d'1. Ignorem clics massa propers.
            var now = DateTime.UtcNow;
            ref DateTime lastClick = ref (pumpNum == 1 ? ref _lastPump1ClickUtc : ref _lastPump2ClickUtc);
            if ((now - lastClick).TotalMilliseconds < 250) return;
            lastClick = now;
            int cur = pumpNum == 1 ? _fuelPump1State3 : _fuelPump2State3;
            int next = Math.Clamp(cur + direction, 0, 2);
            if (pumpNum == 1) _fuelPump1State3 = next; else _fuelPump2State3 = next;
            if (IsCirrusSR22() && !_isMsfs2024 && pumpNum == 1)
            {
                // Cirrus SR22 (MSFS2020, 2026-08-27) — el WASM SFS_CARAVAN_FUELPUMP1_* de sota no fa res
                // en aquest sim (ni command ni feedback real a la VC). Confirmat al Behavior Debug complet
                // de l'InputEvent FUEL_Pump_1: SET_PUMP/SET_STATE_OFF/ON/AUTO fan totes
                // "l0 (>K:ELECT_FUEL_PUMP1_SET)" directament sobre (A:GENERAL ENG FUEL PUMP SWITCH EX1:1,
                // Enum), amb valors reals 0=OFF, 1=PRIME, 2=BOOST. IMPORTANT: aquest interruptor NO segueix
                // la convenció habitual (OFF a baix) — confirmat per Pere: dalt=BOOST, mig=OFF, baix=PRIME.
                int val = next switch { 0 => 2, 1 => 0, _ => 1 }; // local: 0=dalt(BOOST), 1=centre(OFF), 2=baix(PRIME)
                SendGenericRpn($"{val} (>K:ELECT_FUEL_PUMP1_SET)");
                pbMain.Invalidate();
                return;
            }
            if (IsCaravan() && !_isMsfs2024 && pumpNum == 1)
            {
                // Caravan 208B (MSFS2020 NOMÉS, 2026-09-01) — confirmat via revòlver SEND: el "Set" complet
                // de l'InputEvent FUEL_SWITCH_PUMP_1 mostra que SET_STATE_ON/AUTO/OFF criden totes
                // K:ELECT_FUEL_PUMP1_SET amb un valor directe sobre (A:GENERAL ENG FUEL PUMP SWITCH
                // EX1:1, Enum) — mateix event ja confirmat per Baron/Bonanza 2020, aquí amb els seus propis
                // valors: 1=ON, 2=AUTO(NORM), 0=OFF. A diferència de Baron/Bonanza, aquí SÍ distingim els 3
                // estats (no cal simplificar a ON/OFF).
                int val = next switch { 0 => 1, 1 => 2, _ => 0 };
                SendGenericRpn($"{val} (>K:ELECT_FUEL_PUMP1_SET)");
                pbMain.Invalidate();
                return;
            }
            if (UsesCaravanPumpScheme() && pumpNum == 1)
            {
                // WASM propi: SFS.FuelPump1_On/Auto/Off (B:FUEL_PUMP_1_ON/_AUTO/_OFF) — confirmats via MobiFlight.
                // A la Cirrus SR22 el mateix trio de events resulta en la posició física rotada 1 pas
                // respecte a la Caravan (On->OFF visual, Auto->ON visual, Off->AUTO visual), així que
                // per aquest avió es compensa enviant l'event contrari al que "tocaria" per l'estat local.
                var caravanEvt = IsCirrusSR22()
                    ? next switch
                    {
                        0 => SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_AUTO,
                        1 => SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_OFF,
                        _ => SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_ON,
                    }
                    : next switch
                    {
                        0 => SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_ON,
                        1 => SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_AUTO,
                        _ => SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_OFF,
                    };
                simconnect.TransmitClientEvent(0, caravanEvt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                pbMain.Invalidate();
                return;
            }
            if (IsBonanzaG36() && !_isMsfs2024 && pumpNum == 1)
            {
                // Bonanza G36 a MSFS2020 — simplificat a ON/OFF (LOW no es distingeix, es tracta igual que
                // HIGH/ON). MobiFlight "Execute" no fa servir cap API moderna d'Input Events — el seu propi
                // preset (comunitat HubHop) mostra que és calculator code normal, disparable pel nostre
                // missatger genèric: "1 (>K:ELECT_FUEL_PUMP1_SET) 1 (>L:XMLVAR_FUEL_Pump1_High_ON)" per ON.
                // OFF per simetria amb 0. HIGH/LOW (next 0 o 1) es tracten igual, només OFF (next 2) difereix.
                int onOff = next == 2 ? 0 : 1;
                SendGenericRpn($"{onOff} (>K:ELECT_FUEL_PUMP1_SET) {onOff} (>L:XMLVAR_FUEL_Pump1_High_ON)");
                pbMain.Invalidate();
                return;
            }
            if (IsBaronG58() && !_isMsfs2024 && pumpNum == 1)
            {
                // Baron G58 a MSFS2020 (2026-08-27) — mateixa limitació que la Bonanza: LOW inabastable
                // (confirmat llegint el Set complet de l'InputEvent FUEL_SWITCH_PUMP_1 al Behavior Debug —
                // la posició real depèn d'una O:SwitchState privada, només tocable des del gestor
                // d'arrossegament del ratolí, mateix patró que l'ALTGEN1 de la Caravan). Simplificat a
                // ON(=HIGH)/OFF. Confirmat amb 8 candidates via SEND: K:ELECT_FUEL_PUMP1_SET sol (SENSE
                // L:XMLVAR_FUEL_Pump1_High_ON, que aquí bloquejava l'efecte) — valor 0=OFF, 1/2/3=HIGH.
                int onOff = next == 2 ? 0 : 1;
                SendGenericRpn($"{onOff} (>K:ELECT_FUEL_PUMP1_SET)");
                pbMain.Invalidate();
                return;
            }
            var evt = next switch
            {
                0 => pumpNum == 1 ? SIMCONNECT_EVENTS.SFS_FUELPUMP1_HIGH : SIMCONNECT_EVENTS.SFS_FUELPUMP2_HIGH,
                1 => pumpNum == 1 ? SIMCONNECT_EVENTS.SFS_FUELPUMP1_LOW  : SIMCONNECT_EVENTS.SFS_FUELPUMP2_LOW,
                _ => pumpNum == 1 ? SIMCONNECT_EVENTS.SFS_FUELPUMP1_OFF  : SIMCONNECT_EVENTS.SFS_FUELPUMP2_OFF,
            };
            simconnect.TransmitClientEvent(0, evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            if (IsBonanzaG36() && pumpNum == 1 && cur == 2 && next == 1)
            {
                // Bonanza G36: enviant "Low" des d'OFF, l'avió primer "salta" a HIGH (comportament propi del
                // seu sistema de combustible, confirmat manualment des de Behaviors sense passar per l'app ni
                // pel WASM). Enviant un segon "Low" DESPRÉS d'un petit marge real de temps (100ms, perquè
                // l'avió assenti aquest primer canvi) sí que acaba baixant correctament a LOW — enviar-los
                // exactament alhora no funciona. L'animació local de l'app només fa 1 pas (no es toca l'estat).
                var bonanzaLowRetryTimer = new System.Windows.Forms.Timer { Interval = 100 };
                bonanzaLowRetryTimer.Tick += (s, e2) =>
                {
                    bonanzaLowRetryTimer.Stop();
                    bonanzaLowRetryTimer.Dispose();
                    simconnect?.TransmitClientEvent(0, evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                };
                bonanzaLowRetryTimer.Start();
            }
            pbMain.Invalidate();
        }

        private void StepCaravanStarter1(int direction)
        {
            // Caravan — interruptor STARTER (turboprop, 0=START/dalt, 1=OFF/centre, 2=MOTOR/baix).
            // Mecanisme REAL confirmat en viu 2026-08-09 (via SimVarWatcher + molta prova/error —
            // NO és el que deien els presets de MobiFlight/HubHop, que feien servir valors 10/15 per
            // l'ignition switch i mai van funcionar de debò): 2 variables independents governen la
            // posició — A:GENERAL ENG STARTER:1 (Bool, via K:SET_STARTER1_HELD/K:TOGGLE_STARTER1) decideix
            // OFF vs actiu, i A:TURB ENG IGNITION SWITCH EX1:1 (Enum, via K:TURBINE_IGNITION_SWITCH_SET1
            // amb valor 0 o 1 — NO 10/15) decideix MOTOR (0) vs START (1) quan el starter és actiu.
            var now = DateTime.UtcNow;
            if ((now - _lastCaravanStarter1ClickUtc).TotalMilliseconds < 250) return;
            _lastCaravanStarter1ClickUtc = now;
            int next = Math.Clamp(_caravanStarter1Pos + direction, 0, 2);
            if (next == _caravanStarter1Pos) return;
            string rpn = next switch
            {
                0 => "1 (>K:TURBINE_IGNITION_SWITCH_SET1) (A:GENERAL ENG STARTER:1, Bool) ! if{ 1 (>K:SET_STARTER1_HELD) }",
                1 => "(A:GENERAL ENG STARTER:1, Bool) if{ 0 (>K:SET_STARTER1_HELD) (>K:TOGGLE_STARTER1) }",
                _ => "0 (>K:TURBINE_IGNITION_SWITCH_SET1) (A:GENERAL ENG STARTER:1, Bool) ! if{ 1 (>K:SET_STARTER1_HELD) }",
            };
            SendGenericRpn(rpn);
            // Estat optimista + debounce curt (mateix patró que els selectors de combustible), en lloc de
            // confiar immediatament en la lectura real: confirmat 2026-08-26 que, si l'enum compartit
            // s'havia quedat a 2 (ON/override, per un clic previ d'IGNITION mentre STARTER era a MOTOR),
            // la lectura de MSFS flaixa un instant cap a START abans d'assentar-se — aquesta finestra
            // curta evita que ho arribem a mostrar. També reactiva la confiança en la lectura (per si
            // IGNITION l'havia deixat congelada — veure BTN_IGNITION1/_caravanStarter1IgnoreFollowUntil).
            _caravanStarter1Pos = next;
            _caravanStarter1IgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
            pbMain.Invalidate();
        }

        // Cada vegada que es detecta la Baron G58 (a l'engegar l'app o en canviar-hi des d'un altre avió),
        // forcem les 2 bombes a OFF a MSFS i sincronitzem l'estat/animació locals, per garantir que
        // app i simulador comencen sempre alineats sense necessitat de llegir cap variable.
        // Cada vegada que es detecta la DA-62 (a l'engegar l'app o en canviar-hi des d'un altre avió),
        // com que no podem llegir la posició real del selector (ni B: ni O: es poden llegir per aquesta
        // vàlvula), l'estimem per les RPM del motor corresponent: >50 = ON, <=50 = OFF. Forcem l'estat
        // real a MSFS (com ForceBaronFuelPumpsOff) i sincronitzem la posició/animació local.
        private void SyncDA62FuelValvesFromRpm()
        {
            if (simconnect == null) return;
            fuel1Pos = _propRpm1 > 50 ? 1 : 0;
            fuel2Pos = _propRpm2 > 50 ? 1 : 2; // fuel2Pos: 0=Xfeed, 1=On, 2=Off (invertit respecte fuel1Pos)
            simconnect.TransmitClientEvent(0, fuel1Pos == 1 ? SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_ON : SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            simconnect.TransmitClientEvent(0, fuel2Pos == 1 ? SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_ON : SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            pbMain.Invalidate();
        }

        // Cada vegada que es detecta la C408 (a l'engegar l'app o en canviar-hi des d'un altre avió):
        // l'estat normal real del FUEL_TRANSFER és la posició central "FUEL TRANSFER OFF" (sense
        // transferència, cada motor amb la seva pròpia bomba) — forcem aquesta posició a MSFS i
        // sincronitzem la local.
        private void ForceC408FuelTransferOff()
        {
            seSelectorPos = 1;
            TransmitC408FuelTransfer(1);
            pbMain.Invalidate();
        }

        // C408 SkyCourier — FUEL_TRANSFER (selector únic): pos 0=L TANK, 1=FUEL TRANSFER OFF, 2=R TANK.
        // Reprodueix la fórmula "Set" de Behaviors: idx3/idx4 són les 2 bombes de transferència.
        // Events directes, sense WASM.
        private void TransmitC408FuelTransfer(int pos)
        {
            System.Diagnostics.Debug.WriteLine($"[C408 DEBUG] TransmitC408FuelTransfer cridat, pos={pos}, simconnect null? {simconnect == null}");
            if (simconnect == null) return;
            switch (pos)
            {
                case 0: // LEFT
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_OFF, 3, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_ON, 4, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    break;
                case 2: // RIGHT
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_ON, 3, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_OFF, 4, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    break;
                default: // OFF (1)
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_OFF, 3, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_OFF, 4, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    break;
            }
        }

        // TBM930 (2024) — Pulse Light: B:TBM930_LIGHTING_Pulse_Light_Switch no es pot llegir per
        // SimConnect (ni directament ni via L:TBM_PULSE_LIGHT_SWITCH_POSITION, provat i no funciona).
        // Mateix patró que les bombes de combustible sense lectura: en detectar l'avió, forcem OFF
        // real + animació local a joc, i a partir d'aquí el toggle local es manté sincronitzat sol.
        // Canvi de panell TBM930/G1000 — extret del Click del _g3000Btn perquè es pugui invocar també
        // des de la detecció automàtica d'avió sense passar per PerformClick() (el botó viu a la
        // pantalla AUX, que sempre està amagada — CanSelect pot ser false i fer que PerformClick no faci res).
        /// <summary>Ressalta el botó del cockpit actiu (TBM 930 / G1000) i apaga l'altre.</summary>
        private void UpdateCockpitModeButtons()
        {
            // 2026-09-05: la vora blanca prima (1px) de l'inactiu es confonia amb l'activa (4px) — massa
            // subtil per distingir-los d'un cop d'ull. Ara l'actiu porta vora taronja gruixuda (color
            // d'accent de l'app) i l'inactiu es queda amb una vora blanca fina, sense tocar el color de
            // fons del botó (mateix criteri d'abans: mai un farciment de color).
            var accent = Color.FromArgb(235, 91, 37);
            bool tbmOn = _g3000Active && !_a320Active;
            bool g1000On = !_g3000Active && !_a320Active;
            _g3000Btn.FlatAppearance.BorderSize = tbmOn ? 7 : 1;
            _g3000Btn.FlatAppearance.BorderColor = tbmOn ? accent : Color.White;
            _g1000Btn.FlatAppearance.BorderSize = g1000On ? 7 : 1;
            _g1000Btn.FlatAppearance.BorderColor = g1000On ? accent : Color.White;
            if (_a320Btn != null)
            {
                _a320Btn.FlatAppearance.BorderSize = _a320Active ? 7 : 1;
                _a320Btn.FlatAppearance.BorderColor = _a320Active ? accent : Color.White;
            }
        }

        /// <summary>Forats de la Region segons el mode actual (A320 no en té: l'overhead ocupa tota la pantalla).</summary>
        private void ApplyCurrentRegion()
        {
            if (_a320Active) { ApplyA320Region(); return; }
            if (_g3000Active) ApplyG3000Region(); else ApplyPfdRegion();
        }

        /// <summary>
        /// A320: forats a la finestra sobre el vidre de les pantalles PFD, ND i ECAM (s'hi veuen les finestres
        /// de les pantalles del simulador col·locades a sota). La part que tapa l'overhead obert no és forat.
        /// Només es torna a aplicar si els forats han canviat (l'overhead es mou molts cops per segon).
        /// </summary>
        /// <summary>
        /// Cada segon, amb el ToLiss carregat: busca la finestra del PFD del capità ("ToLiss Captain Left DU") i
        /// l'encaixa sense marc al forat del PFD, just darrere de l'app. Si no hi és, la demana un sol cop amb
        /// AirbusFBW/PopUpPFD1 (surt com a finestra de Windows si s'ha fet pop-out un cop en aquesta sessió d'X-Plane).
        /// </summary>
        /// <summary>
        /// Pantalles del ToLiss (pop-out d'X-Plane) → forat de l'app: índex del vidre (0 PFD, 1 ND, 2 ECAM superior,
        /// 3 ECAM inferior), títol de la finestra de Windows i ordre que l'obre / tanca. Títol null = encara no el sabem.
        /// </summary>
        private static readonly (int glass, string? title, string cmd)[] XpScreens =
        {
            (0, "ToLiss Captain Left DU", "XP_POPUP_PFD1"),
            (1, "ToLiss Captain Right DU", "XP_POPUP_ND1"),
            (2, "ToLiss Upper ECAM", "XP_POPUP_EWD"),
            (3, "ToLiss Lower ECAM", "XP_POPUP_SD"),
        };
        private readonly bool[] _xpScreenAsked = new bool[4];

        private void PlaceXPlanePfd()
        {
            // els forats de les pantalles de l'A320 es revisen cada segon (només es tornen a aplicar si han canviat)
            if (_a320Active) ApplyA320Region();
            if (_xp == null || !_xp.A320Loaded || !_a320Active || _a320Overhead == null || !_a320Overhead.Visible) return;
            foreach (var (glass, title, cmd) in XpScreens)
            {
                if (title == null) continue;
                try
                {
                    var h = XPlaneWindows.Find(title);
                    if (h == IntPtr.Zero)
                    {
                        // la demana un sol cop per sessió del ToLiss (l'ordre obre / tanca: no es repeteix)
                        if (!_xpScreenAsked[glass]) { _xpScreenAsked[glass] = true; _xp.SendCommand(cmd, "PUSH"); }
                        continue;
                    }
                    _xpScreenAsked[glass] = true;
                    if (_a320Overhead.GlassScreenRect(glass) is Rectangle rc) XPlaneWindows.Place(h, rc, this.Handle);
                }
                catch { }
            }
        }

        /// <summary>En tancar l'app: tanca a X-Plane les pantalles del ToLiss que l'app tenia encaixades.</summary>
        private void CloseXPlaneScreens()
        {
            if (_xp == null || !_xp.A320Loaded) return;
            foreach (var (_, title, cmd) in XpScreens)
                if (title != null && XPlaneWindows.Find(title) != IntPtr.Zero) _xp.SendCommandAndWait(cmd, "PUSH");
        }

        private void ApplyA320Region()
        {
            using var holes = _a320Overhead != null && _a320Overhead.Visible ? _a320Overhead.ScreenHoles() : null;
            if (holes == null) { if (this.Region != null) this.Region = null; _a320HolesKey = ""; _a320RegionSet = null; return; }
            // control → coordenades del Form
            var off = this.PointToClient(_a320Overhead!.PointToScreen(Point.Empty));
            holes.Translate(off.X, off.Y);
            string key;
            using (var m = new System.Drawing.Drawing2D.Matrix())
                key = string.Join(";", holes.GetRegionScans(m).Select(r => $"{r.X},{r.Y},{r.Width},{r.Height}"));
            // (si una altra part de l'app ha canviat la regió de la finestra, es torna a posar la de l'A320)
            if (key == _a320HolesKey && _a320RegionSet != null && ReferenceEquals(this.Region, _a320RegionSet)) return;
            _a320HolesKey = key;
            var formRegion = new Region(new Rectangle(0, 0, this.Width, this.Height));
            formRegion.Exclude(holes);
            this.Region = formRegion;
            _a320RegionSet = this.Region;
        }
        private string _a320HolesKey = "";
        private Region? _a320RegionSet;

        /// <summary>
        /// Ordres del panell A320 cap a l'avió (FlyByWire). L'estat que es veu al panell torna sempre des
        /// del simulador (bidireccional): aquí només s'envia l'acció.
        /// </summary>
        private void OnA320Command(string id, string cmd)
        {
            // amb un ToLiss carregat a X-Plane, les ordres van cap a X-Plane
            if (_xp != null && _xp.A320Loaded) { _xp.SendCommand(id, cmd); return; }
            if (simconnect == null) return;
            if (id == "MIP_BARO")
            {
                SIMCONNECT_EVENTS? ev = cmd switch
                {
                    "INC" => SIMCONNECT_EVENTS.A32NX_BARO_L_INC,
                    "DEC" => SIMCONNECT_EVENTS.A32NX_BARO_L_DEC,
                    "PUSH" => SIMCONNECT_EVENTS.A32NX_BARO_L_PUSH,
                    "PULL" => SIMCONNECT_EVENTS.A32NX_BARO_L_PULL,
                    _ => null
                };
                try
                {
                    if (ev is SIMCONNECT_EVENTS e)
                        simconnect.TransmitClientEvent(SimConnect.SIMCONNECT_OBJECT_ID_USER, e, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                    else if (cmd == "INHG" || cmd == "HPA")
                        simconnect.SetDataOnSimObject(DEFINITIONS.A320BaroUnitWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                            SIMCONNECT_DATA_SET_FLAG.DEFAULT, new A320BaroUnitStruct { isInhg = cmd == "INHG" ? 1 : 0 });
                }
                catch { }
            }
        }

        /// <summary>
        /// Entra/surt del mode A320. L'overhead és un control propi que se situa just a sobre de pbMain
        /// (la resta de botons del Form, com ⚙, queden per sobre). pbMain s'amaga mentre dura el mode
        /// perquè no consumeixi CPU repintant-se per sota.
        /// </summary>
        private void SetA320Active(bool active)
        {
            if (calibrationMode) return;
            if (_a320Active == active) { UpdateCockpitModeButtons(); return; }
            _a320Active = active;
            if (active)
            {
                if (_a320Overhead == null)
                {
                    _a320Overhead = new panelAIR1000.A320OverheadControl
                    {
                        Visible = false,
                        PlusLocation = _a320ZoomPlusPos,
                        MinusLocation = _a320ZoomMinusPos,
                        PageLocation = _a320PageBtnPos,
                        AreasLocation = _a320AreasBtnPos
                    };
                    _a320Overhead.Command += OnA320Command;
                    _a320Overhead.HolesChanged += () => { if (_a320Active) ApplyA320Region(); };
                    if (_xp != null && _xp.A320Loaded && _xpBaro is var (xp, xm, xv, xhg))
                        _a320Overhead.SetBaro(xp, xm, xv, xhg);
                    if (_xp != null && _xp.A320Loaded && _xpFcu != null) _a320Overhead.SetFcu(_xpFcu);
                    if (_xp != null && _xp.A320Loaded && _xpAltStep is bool xs) _a320Overhead.SetKnobRing("MIP_FCU_ALT", xs);
                    if (_xp != null && _xp.A320Loaded)
                    {
                        _a320Overhead.SimDriven = true;
                        foreach (var kv in _xpLights) _a320Overhead.SetLight(kv.Key, kv.Value);
                        foreach (var kv in _xpPos) _a320Overhead.SetPosition(kv.Key, kv.Value);
                    }
                    else if (_a320Baro is A320BaroStruct ab)
                        _a320Overhead.SetBaro(ab.powered > 0.5, (int)Math.Round(ab.mode), ab.value, ab.isInhg > 0.5);
                    _a320Overhead.ZoomButtonsMoved += () =>
                    {
                        _a320ZoomPlusPos = _a320Overhead.PlusLocation;
                        _a320ZoomMinusPos = _a320Overhead.MinusLocation;
                        _a320PageBtnPos = _a320Overhead.PageLocation;
                        _a320AreasBtnPos = _a320Overhead.AreasLocation;
                        try { ExportCalibrationPositions(silent: true); } catch { }
                    };
                    this.Controls.Add(_a320Overhead);
                }
                _a320Overhead.CalibrationMode = calibrationMode;
                _a320Overhead.OpenAtBottom(); // la vora de baix de l'overhead coincideix amb la de la pantalla
                _a320Overhead.Dock = pbMain.Dock;
                if (pbMain.Dock == DockStyle.None) _a320Overhead.Bounds = pbMain.Bounds;
                this.Controls.SetChildIndex(_a320Overhead, this.Controls.GetChildIndex(pbMain));
                _a320Overhead.Visible = true;
                pbMain.Visible = false;
                // els forats depenen de la mida i posició finals del panell: es tornen a calcular quan ja s'ha col·locat
                BeginInvoke(new Action(() => { if (_a320Active) ApplyA320Region(); }));
                _thrQuadrantBtn.Visible = false;
                if (_popoutReminderForm != null && !_popoutReminderForm.IsDisposed) _popoutReminderForm.Close();
            }
            else
            {
                if (_a320Overhead != null) _a320Overhead.Visible = false; // allibera el bitmap (OnVisibleChanged)
                if (_tintPanel != null) _tintPanel.Visible = false;
                pbMain.Visible = true;
                _thrQuadrantBtn.Visible = _g3000Active;
                pbMain.Invalidate();
            }
            ApplyCurrentRegion();
            UpdateCockpitModeButtons();
        }

        // ── PROVISIONAL (2026-11-20, Pere): regulador d'homogeneïtat de la textura de la planxa A320 (overhead i main) ──
        private Panel? _tintPanel;
        private System.Windows.Forms.Timer? _tintTimer;

        private void EnsureTintSlider()
        {
            if (_tintPanel != null) return;
            var font = new Font("Segoe UI", 18f, FontStyle.Bold, GraphicsUnit.Pixel);
            _tintPanel = new Panel { BackColor = Color.FromArgb(30, 34, 40), Size = new Size(560, 96) };
            var lbl = new Label { AutoSize = false, Location = new Point(12, 6), Size = new Size(536, 28), Font = font, ForeColor = Color.White };
            var tb = new TrackBar { Minimum = 0, Maximum = 100, Value = (int)Math.Round(panelAIR1000.Oh.Texture * 100), TickFrequency = 25, Location = new Point(8, 38), Size = new Size(544, 50), BackColor = _tintPanel.BackColor };
            lbl.Text = $"TEXTURA: GRANELLUDA ◀▶ FINA   {tb.Value}";
            _tintTimer = new System.Windows.Forms.Timer { Interval = 250 };
            _tintTimer.Tick += (s, e) => { _tintTimer.Stop(); _a320Overhead?.SetTexture(tb.Value / 100f); };
            tb.ValueChanged += (s, e) => { lbl.Text = $"TEXTURA: GRANELLUDA ◀▶ FINA   {tb.Value}"; _tintTimer.Stop(); _tintTimer.Start(); };
            _tintPanel.Controls.Add(lbl);
            _tintPanel.Controls.Add(tb);
            _tintPanel.Location = new Point(Math.Max(0, this.ClientSize.Width - _tintPanel.Width - 40), 40);
            this.Controls.Add(_tintPanel);
        }

        private void SetG3000Active(bool active)
        {
            if (calibrationMode) return;
            bool wasG3000Active = _g3000Active;
            _g3000Active = active;
            UpdateCockpitModeButtons();
            _thrQuadrantBtn.Visible = _g3000Active;
            if (!_g3000Active) { _thrQuadrantVisible = false; }
            if (_g3000Active)
            {
                // 2026-09-05: si es torna a TBM930 abans que el compte enrere de 10s del recordatori de
                // pop-outs (obert en entrar a G1000) acabi, es quedava obert i semblava que el mode
                // TBM930 també el disparava.
                if (_popoutReminderForm != null && !_popoutReminderForm.IsDisposed) _popoutReminderForm.Close();
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
                    Task.Run(async () => { await Task.Delay(500); this.BeginInvoke(new Action(() => {
                        SyncThrHandleFromMsfs();
                    })); });
                }
                CheckTBM930Mod();
            }
            else
            {
                ApplyPfdRegion();
                if (_popoutReminderForm != null && !_popoutReminderForm.IsDisposed) _popoutReminderForm.Close();
                // Només té sentit avisar de tancar els pop-outs G3000 si realment veníem de mode
                // G3000/TBM930 — abans sortia sempre que es premia el botó G1000, encara que ja
                // s'estigués en G1000 (2026-09-15).
                if (wasG3000Active)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(1000);
                        this.BeginInvoke(new Action(ShowPopoutReminder));
                        for (int i = 9; i >= 0; i--)
                        {
                            await Task.Delay(1000);
                            this.BeginInvoke(new Action(() =>
                            {
                                if (_popoutReminderForm == null || _popoutReminderForm.IsDisposed) return;
                                if (i <= 0)
                                    _popoutReminderForm.Close();
                                else if (_popoutReminderMsgLbl != null)
                                    _popoutReminderMsgLbl.Text = "If you no longer need G3000 mode, close the MSFS G3000 pop-out windows to restore full G1000 panel functionality.  [" + i + "]";
                            }));
                        }
                    });
                }
            }
            pbMain.Invalidate();
        }

        /// <summary>Finestra normal (no modal) que recorda tancar els pop-outs G3000, amb botó de tancar; el compte enrere de 10s el porta el bucle de SetG3000Active.</summary>
        private void ShowPopoutReminder()
        {
            var form = new Form
            {
                AutoScaleMode = AutoScaleMode.None,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                Text = "panelAIR",
                Size = new Size(1440, 200),
                BackColor = Color.White,
                StartPosition = FormStartPosition.Manual
            };
            form.Location = this.PointToScreen(new Point((this.ClientSize.Width - form.Width) / 2, 570));

            var msgLbl = new Label
            {
                Text = "If you no longer need G3000 mode, close the MSFS G3000 pop-out windows to restore full G1000 panel functionality.  [10]",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 18f),
                ForeColor = Color.FromArgb(22, 32, 42),
                Location = new Point(24, 20),
                Size = new Size(form.ClientSize.Width - 48, 110)
            };
            form.Controls.Add(msgLbl);

            var closeButton = new Button
            {
                Text = "Close",
                Size = new Size(160, 48),
                Location = new Point((form.ClientSize.Width - 160) / 2, 138),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 14f, FontStyle.Bold)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (s, e) => form.Close();
            form.Controls.Add(closeButton);

            _popoutReminderMsgLbl = msgLbl;
            _popoutReminderForm = form;
            form.Show(this);
        }

        /// <summary>
        /// Mostra/tanca el missatge "You're all set!" segons si l'avió actual és un dels suportats.
        /// Es crida contínuament (cada segon, via Simconnect_OnRecvSimobjectData) i també en tancar
        /// Configuration. Un cop l'usuari prem "Start Flying" per a un avió concret, no torna a sortir
        /// per aquell mateix avió (evita parpelleig/repetició contínua) — només reapareix en un nou
        /// event real: un avió diferent carregat, o tancar Configuration altre cop (forceRecheck).
        /// </summary>
        private void RefreshNoCompatibleAircraftMessage(bool forceRecheck = false)
        {
            if (string.IsNullOrEmpty(_currentAircraftTitle)) return; // encara no s'ha rebut cap títol real

            bool compatible = IsC172() || IsDA40() || IsDA62() || IsCirrusSR22() || IsBonanzaG36()
                || IsBaronG58() || IsCaravan() || IsC400() || IsC408() || IsTBM930() || IsA320FBW();

            if (compatible)
            {
                if (_noAircraftMessageForm != null && !_noAircraftMessageForm.IsDisposed)
                    _noAircraftMessageForm.Close();
                return;
            }

            if (forceRecheck) _noAircraftMessageDismissedFor = null;
            if (_noAircraftMessageDismissedFor == _currentAircraftTitle) return; // ja descartat per aquest avió

            if (_noAircraftMessageForm != null && !_noAircraftMessageForm.IsDisposed) return; // ja mostrat
            ShowNoCompatibleAircraftMessage();
        }

        /// <summary>Finestra normal (no modal, sense TopMost) que explica que panelAIR s'adapta automàticament
        /// a qualsevol avió equipat amb G1000, la TBM 930 o l'A320neo de FlyByWire. Un únic botó "Start Flying" per tancar-la.</summary>
        private void ShowNoCompatibleAircraftMessage()
        {
            var form = new Form
            {
                AutoScaleMode = AutoScaleMode.None,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ControlBox = false,
                Text = "panelAIR",
                Size = new Size(1500, 420),
                BackColor = Color.White,
                StartPosition = FormStartPosition.Manual
            };
            form.Location = this.PointToScreen(new Point((this.ClientSize.Width - form.Width) / 2, 400));

            var titleLbl = new Label
            {
                Text = "You're all set!",
                Font = new Font("Segoe UI", 32f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 32, 42),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 30),
                Size = new Size(form.ClientSize.Width - 80, 70)
            };
            form.Controls.Add(titleLbl);

            var msgLbl = new Label
            {
                Text = "panelAIR automatically adapts to any aircraft equipped with G1000, the TBM 930, or the FlyByWire A320neo. Choose one of those to get started.",
                Font = new Font("Segoe UI", 16f),
                ForeColor = Color.FromArgb(102, 114, 127),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(40, 110),
                Size = new Size(form.ClientSize.Width - 80, 140)
            };
            form.Controls.Add(msgLbl);

            var startButton = new Button
            {
                Text = "Start Flying",
                Size = new Size(320, 64),
                Location = new Point((form.ClientSize.Width - 320) / 2, 280),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 91, 37),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 16f, FontStyle.Bold)
            };
            startButton.FlatAppearance.BorderSize = 0;
            startButton.Click += (s, e) =>
            {
                _noAircraftMessageDismissedFor = _currentAircraftTitle;
                form.Close();
            };
            form.Controls.Add(startButton);

            _noAircraftMessageForm = form;
            form.Show(this);
        }

        private void ForceTbmPulseLightOff()
        {
            if (simconnect == null) return;
            SendGenericRpn("0 (>B:TBM930_LIGHTING_PULSE_LIGHT_SWITCH_Set)");
            _tbmPulseLight2024On = false;
            pbMain.Invalidate();
        }

        private void ForceBaronFuelPumpsOff()
        {
            if (simconnect == null) return;
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_FUELPUMP1_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_FUELPUMP2_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            _fuelPump1State3 = 2;
            _fuelPump2State3 = 2;
            pbMain.Invalidate();
        }

        // Cada vegada que es detecta la Cessna Caravan / Cirrus SR22 (monomotor, només pump 1), mateix mecanisme que la Baron.
        private void ForceCaravanFuelPumpOff()
        {
            if (simconnect == null) return;
            if (IsCirrusSR22() && !_isMsfs2024)
            {
                // Cirrus SR22 (MSFS2020): native K:ELECT_FUEL_PUMP1_SET, valor real 0=OFF (veure
                // StepFuelPumpBaron). OFF és la posició CENTRAL (local 1) en aquest interruptor, no la de
                // baix — confirmat per Pere: dalt=BOOST, mig=OFF, baix=PRIME.
                SendGenericRpn("0 (>K:ELECT_FUEL_PUMP1_SET)");
                _fuelPump1State3 = 1;
                pbMain.Invalidate();
                return;
            }
            if (IsCaravan() && !_isMsfs2024)
            {
                // Caravan 208B (MSFS2020 NOMÉS, 2026-09-01) — mateix K:ELECT_FUEL_PUMP1_SET confirmat via
                // revòlver (veure StepFuelPumpBaron): valor 0=OFF.
                SendGenericRpn("0 (>K:ELECT_FUEL_PUMP1_SET)");
                _fuelPump1State3 = 2;
                pbMain.Invalidate();
                return;
            }
            // Cirrus (2024): l'event "On" és el que resulta en OFF visual (veure comentari a StepFuelPumpBaron).
            var evt = IsCirrusSR22() ? SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_ON : SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_OFF;
            simconnect.TransmitClientEvent(0, evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            _fuelPump1State3 = 2;
            pbMain.Invalidate();
        }

        // Cada vegada que es detecta la Bonanza G36 (monomotor, només pump 1, mateix esquema HIGH/LOW/OFF
        // que la Baron — reutilitza els mateixos events WASM SFS.FuelPump1_High/Low/Off).
        private void ForceBonanzaFuelPumpOff()
        {
            if (simconnect == null) return;
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_FUELPUMP1_OFF, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            _fuelPump1State3 = 2;
            pbMain.Invalidate();
        }

        private void SetElecGeneratorSwitch(double value)
        {
            if (simconnect == null) return;
            if (_isMsfs2024)
            {
                // TBM930 (2024) — B:ELECTRICAL_GENERATOR_SWITCH_Set (0=OFF, 1=MAIN, 2=STBY).
                // Feedback real via L:1:TBM_Elec_Generator_Switch_State (RequestTbmElecGenerator2024).
                SendGenericRpn($"{(int)Math.Round(value)} (>B:ELECTRICAL_GENERATOR_SWITCH_Set)");
                return;
            }
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
                _mainOverlayMsgLbl.Text =
                    "MSFS 2020 users: for full G3000 panel functionality, we recommend installing the free mod " +
                    "'TBM930 - LVars for Bleed Air, Inert Sep and Cabin Lights' by FlyingDutch.\n\n" +
                    "Download it at:\nhttps://flightsim.to/addon/57918/tbm930-lvars-for-bleed-inert-and-cabin-lights\n\n" +
                    "Once downloaded, copy the contents to your MSFS 2020 Community folder.\n\n" +
                    "Would you like to open the download page?";
                _mainOverlayPrimaryBtn.Text = "Open Download Page";
                _mainOverlayOnPrimaryClick = () =>
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://flightsim.to/addon/57918/tbm930-lvars-for-bleed-inert-and-cabin-lights",
                        UseShellExecute = true
                    }); } catch { }
                };
                _mainOverlayCard.Visible = true;
                _mainOverlayCard.BringToFront();
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
                simconnect.OnRecvException += (s, d) =>
                {
                    try { System.IO.File.AppendAllText("panelAIR_App_crash.txt", $"\r\n[SIMCONNECT EXCEPTION] {d.dwException}  (SendID={d.dwSendID} Index={d.dwIndex})"); } catch { }
                };

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

                // MSFS2024 — mateix interruptor físic, sistema de combustible nou (index 1/2 passat com a dwData al clic)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_ON, "FUELSYSTEM_PUMP_ON");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_OFF, "FUELSYSTEM_PUMP_OFF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FUELSYSTEM_PUMP_OFF, false);

                // WASM propi (SFS) — per avions amb bomba de 3 posicions no controlables pels events genèrics (p. ex. Baron G58)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELPUMP1_HIGH, "SFS.FuelPump1_High");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELPUMP1_HIGH, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELPUMP1_LOW, "SFS.FuelPump1_Low");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELPUMP1_LOW, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELPUMP1_OFF, "SFS.FuelPump1_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELPUMP1_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELPUMP2_HIGH, "SFS.FuelPump2_High");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELPUMP2_HIGH, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELPUMP2_LOW, "SFS.FuelPump2_Low");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELPUMP2_LOW, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELPUMP2_OFF, "SFS.FuelPump2_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELPUMP2_OFF, false);

                // Cessna Grand Caravan (208B) — bomba de 3 posicions via WASM propi (B:FUEL_PUMP_1_ON/_AUTO/_OFF, confirmats amb MobiFlight)
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_ON, "SFS.FuelPump1_On");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_AUTO, "SFS.FuelPump1_Auto");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_AUTO, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_OFF, "SFS.FuelPump1_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_CARAVAN_FUELPUMP1_OFF, false);

                // Baron G58 (MSFS2024) — selectors de combustible esquerre/dret via WASM propi, RPN confirmat
                // amb MobiFlight: "{estat} {costat} (>K:2:FUELSYSTEM_JUNCTION_SET)" — 1=Off, 2=On, 3=Xfeed;
                // costat 1=esquerre, 2=dret. La SimVar clàssica "FUEL TANK SELECTOR" no reflecteix aquest sistema.
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_OFF, "SFS.FuelSelLeft_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_ON, "SFS.FuelSelLeft_On");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_XFEED, "SFS.FuelSelLeft_Xfeed");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELSEL_LEFT_XFEED, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_OFF, "SFS.FuelSelRight_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_ON, "SFS.FuelSelRight_On");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_XFEED, "SFS.FuelSelRight_Xfeed");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELSEL_RIGHT_XFEED, false);

                // DA-62 — l'event clàssic "FUEL_VALVE_LEFT_SET" (Input Event alias) NO fa res des de fora,
                // confirmat en vol — mateix problema que ja vam trobar amb el Caravan. Es manté mapejat
                // per si mai cal, però l'app fa servir el WASM (B:FUEL_VALVE_LEFT) de sota.
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.DA62_FUELVALVE_LEFT_SET, "FUEL_VALVE_LEFT_SET");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.DA62_FUELVALVE_LEFT_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_ON, "SFS.FuelValveLeft_On");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_CROSS, "SFS.FuelValveLeft_Cross");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_CROSS, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_OFF, "SFS.FuelValveLeft_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_LEFT_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_ON, "SFS.FuelValveRight_On");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_CROSS, "SFS.FuelValveRight_Cross");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_CROSS, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_OFF, "SFS.FuelValveRight_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA62_FUELVALVE_RIGHT_OFF, false);
                // DA-40 — selector únic de 3 posicions. K:FUEL_SELECTOR_SET enviat directe (sense WASM) NO fa
                // res (confirmat amb 3 botons de prova a la pantalla AUX): igual que DA-62, el fuel system nou
                // de MSFS2024 no escolta l'event clàssic des d'un client extern. Cal WASM (execute_calculator_code).
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_NORMAL, "SFS.FuelSelDA40_Normal");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_NORMAL, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_EMERGENCY, "SFS.FuelSelDA40_Emergency");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_EMERGENCY, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_OFF, "SFS.FuelSelDA40_Off");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_OFF, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_TEST, "SFS.FuelSelDA40_Test");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_TEST, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_FUELTRIGGER_TEST, "SFS.FuelTrigger_Test");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_FUELTRIGGER_TEST, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_DA62_AVIONICS_TOGGLE, "SFS.DA62_Avionics_Toggle");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_DA62_AVIONICS_TOGGLE, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_C172_AVIONICS1_SET, "SFS.C172_Avionics1_Set");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_C172_AVIONICS1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_C172_AVIONICS2_SET, "SFS.C172_Avionics2_Set");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_C172_AVIONICS2_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_CARAVAN_AVIONICS1_SET, "SFS.Caravan_Avionics1_Set");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_CARAVAN_AVIONICS1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_CARAVAN_AVIONICS2_SET, "SFS.Caravan_Avionics2_Set");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_CARAVAN_AVIONICS2_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_CARAVAN_ALTGEN1_SET, "SFS.Caravan_AltGen1_Set");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_CARAVAN_ALTGEN1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.SFS_CARAVAN_ALTGEN2_SET, "SFS.Caravan_AltGen2_Set");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.SFS_CARAVAN_ALTGEN2_SET, false);

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
                // Caravan (C208B): 2 triggers de fuel shutoff independents (índex 3=Left, 4=Right), confirmats
                // a Behaviors via K:2:FUELSYSTEM_TRIGGER_SET. FUELSYSTEM_TRIGGER_ON/OFF són la variant d'1 sol
                // paràmetre (només índex) del mateix event — es poden enviar directes des de C#, sense WASM.
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_ON, "FUELSYSTEM_TRIGGER_ON");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_ON, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_OFF, "FUELSYSTEM_TRIGGER_OFF");
                simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.FUELSYSTEM_TRIGGER_OFF, false);
                simconnect.SetNotificationGroupPriority(SIMCONNECT_GROUPS.GROUP0, SimConnect.SIMCONNECT_GROUP_PRIORITY_HIGHEST);


                // Struct1/Request1 ja no s'usa — tots els camps que hi quedaven (gyro, deice, pitot heat,
                // num engines, cowl flaps, camera state, fuel pump elèctric, prop lever/fuel valve del
                // throttle, AP master, aileron trim frac, deice airframe) han passat a definicions pròpies
                // i separades (2026-10-03, Pere: "passa-les totes"). Els camps que no tenien cap consumidor
                // real (title, hdg, crs1/2, adf act/stby, baro in/hpa, alt sel, xpdr code/state/ident,
                // cdi_scr, nav_sel, fuel cross feed, apTrim, circuit conn 17, fuelSelectionSet —ja llegit
                // per separat via TBM—, propRpm, fuel pump EX1:1/2) simplement s'han deixat de llegir.
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
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.STARTER2_SET, "STARTER2_SET");
                //simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.STARTER2_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ENGINE_MASTER_1_SET, "ENGINE_MASTER_1_SET");
                //simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ENGINE_MASTER_1_SET, false);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.ENGINE_MASTER_2_SET, "ENGINE_MASTER_2_SET");
                //simconnect.AddClientEventToNotificationGroup(SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENTS.ENGINE_MASTER_2_SET, false);
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

                // Avió carregat — mostrat a la pantalla AUX
                simconnect.AddToDataDefinition(DEFINITIONS.AircraftTitle, "TITLE", null, SIMCONNECT_DATATYPE.STRING256, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<AircraftInfoStruct>(DEFINITIONS.AircraftTitle);
                simconnect.RequestDataOnSimObject(REQUESTS.RequestAircraftTitle, DEFINITIONS.AircraftTitle, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

                // A320 FBW — QNH del capità (bidireccional): es llegeix el que mostra la finestra de l'EFIS
                // (valor, mode 0=STD/1=QNH/2=QFE, unitat) i si el bus DC ESS (FCU) té corrent; només arriba
                // quan canvia. Ordres: esdeveniments A32NX.FCU_EFIS_L_BARO_* i escriptura de la unitat.
                simconnect.AddToDataDefinition(DEFINITIONS.A320BaroRead, "L:A32NX_FCU_EFIS_L_DISPLAY_BARO_VALUE", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.A320BaroRead, "L:A32NX_FCU_EFIS_L_DISPLAY_BARO_MODE", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.A320BaroRead, "L:A32NX_FCU_EFIS_L_BARO_IS_INHG", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.A320BaroRead, "L:A32NX_ELEC_DC_ESS_BUS_IS_POWERED", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<A320BaroStruct>(DEFINITIONS.A320BaroRead);
                simconnect.RequestDataOnSimObject(REQUESTS.RequestA320Baro, DEFINITIONS.A320BaroRead, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.VISUAL_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                simconnect.AddToDataDefinition(DEFINITIONS.A320BaroUnitWrite, "L:A32NX_FCU_EFIS_L_BARO_IS_INHG", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<A320BaroUnitStruct>(DEFINITIONS.A320BaroUnitWrite);
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.A32NX_BARO_L_INC, "A32NX.FCU_EFIS_L_BARO_INC");
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.A32NX_BARO_L_DEC, "A32NX.FCU_EFIS_L_BARO_DEC");
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.A32NX_BARO_L_PUSH, "A32NX.FCU_EFIS_L_BARO_PUSH");
                simconnect.MapClientEventToSimEvent(SIMCONNECT_EVENTS.A32NX_BARO_L_PULL, "A32NX.FCU_EFIS_L_BARO_PULL");

                // Flaps — definicions pròpies i separades (veure comentari a FlapsNumPosStruct).
                simconnect.AddToDataDefinition(DEFINITIONS.FlapsNumPosRead, "FLAPS NUM HANDLE POSITIONS", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<FlapsNumPosStruct>(DEFINITIONS.FlapsNumPosRead);
                simconnect.AddToDataDefinition(DEFINITIONS.FlapsHandleIndexRead, "FLAPS HANDLE INDEX", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<FlapsHandleIndexStruct>(DEFINITIONS.FlapsHandleIndexRead);

                // Trim — definicions pròpies i independents (veure comentari a ElevatorTrimStruct).
                simconnect.AddToDataDefinition(DEFINITIONS.ElevatorTrimRead, "ELEVATOR TRIM INDICATOR", "Position", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<ElevatorTrimStruct>(DEFINITIONS.ElevatorTrimRead);
                simconnect.AddToDataDefinition(DEFINITIONS.RudderTrimRead, "RUDDER TRIM PCT", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<RudderTrimStruct>(DEFINITIONS.RudderTrimRead);

                // FUEL TANK SELECTOR:1/2 — definicions pròpies i separades (veure comentari al camp
                // _fuelTankSelector1/2). Continuen al ritme fix de 200ms (_lowFreqPolls), com els altres
                // switches — no els cal el patró "sync + actiu" dels flaps/trim.
                simconnect.AddToDataDefinition(DEFINITIONS.FuelTankSelector1Read, "FUEL TANK SELECTOR:1", "Enum", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<FuelTankSelector1Struct>(DEFINITIONS.FuelTankSelector1Read);
                simconnect.AddToDataDefinition(DEFINITIONS.FuelTankSelector2Read, "FUEL TANK SELECTOR:2", "Enum", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<FuelTankSelector2Struct>(DEFINITIONS.FuelTankSelector2Read);

                // Throttle lever — definició pròpia (veure comentari al camp _thrLeverPos).
                simconnect.AddToDataDefinition(DEFINITIONS.ThrottleLeverPosRead, "GENERAL ENG THROTTLE LEVER POSITION:1", "Position 16k", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<ThrottleLeverPosStruct>(DEFINITIONS.ThrottleLeverPosRead);

                // Llums — definicions pròpies i separades (veure comentari a LightBoolStruct).
                simconnect.AddToDataDefinition(DEFINITIONS.LightBeaconRead, "LIGHT BEACON", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.LightBeaconRead);
                simconnect.AddToDataDefinition(DEFINITIONS.LightLandRead, "LIGHT LANDING", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.LightLandRead);
                simconnect.AddToDataDefinition(DEFINITIONS.LightTaxiRead, "LIGHT TAXI", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.LightTaxiRead);
                simconnect.AddToDataDefinition(DEFINITIONS.LightNavRead, "LIGHT NAV", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.LightNavRead);
                simconnect.AddToDataDefinition(DEFINITIONS.LightStrobeRead, "LIGHT STROBE", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.LightStrobeRead);
                simconnect.AddToDataDefinition(DEFINITIONS.LightRecognRead, "LIGHT RECOGNITION", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.LightRecognRead);
                simconnect.AddToDataDefinition(DEFINITIONS.LightPanelRead, "LIGHT PANEL", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.LightPanelRead);
                simconnect.AddToDataDefinition(DEFINITIONS.LightLogoRead, "LIGHT LOGO", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.LightLogoRead);
                simconnect.AddToDataDefinition(DEFINITIONS.LightingLogo1Read, "L:LIGHTING_LOGO_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.LightingLogo1Read);
                simconnect.AddToDataDefinition(DEFINITIONS.LightingWing1Read, "L:LIGHTING_WING_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.LightingWing1Read);

                // Bateria, avionics, alternators, magnetos/starter i fuel pump — definicions pròpies i
                // separades (2026-10-03, Pere: prova de treure més camps del paquet gran de Struct1 cap
                // al ritme fix de 200ms, reutilitzant LightBoolStruct com a estructura compartida).
                simconnect.AddToDataDefinition(DEFINITIONS.MasterBatteryRead, "ELECTRICAL MASTER BATTERY", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.MasterBatteryRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Avionics1Read, "AVIONICS MASTER SWITCH:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Avionics1Read);
                simconnect.AddToDataDefinition(DEFINITIONS.Avionics2Read, "AVIONICS MASTER SWITCH:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Avionics2Read);
                simconnect.AddToDataDefinition(DEFINITIONS.AltGen1Read, "GENERAL ENG MASTER ALTERNATOR:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.AltGen1Read);
                simconnect.AddToDataDefinition(DEFINITIONS.AltGen2Read, "GENERAL ENG MASTER ALTERNATOR:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.AltGen2Read);
                simconnect.AddToDataDefinition(DEFINITIONS.Ign1LeftRead, "RECIP ENG LEFT MAGNETO:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Ign1LeftRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Ign1RightRead, "RECIP ENG RIGHT MAGNETO:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Ign1RightRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Ign1StarterRead, "GENERAL ENG STARTER:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Ign1StarterRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Ign2LeftRead, "RECIP ENG LEFT MAGNETO:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Ign2LeftRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Ign2RightRead, "RECIP ENG RIGHT MAGNETO:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Ign2RightRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Ign2StarterRead, "GENERAL ENG STARTER:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Ign2StarterRead);
                simconnect.AddToDataDefinition(DEFINITIONS.FuelPump1Read, "GENERAL ENG FUEL PUMP SWITCH:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.FuelPump1Read);
                simconnect.AddToDataDefinition(DEFINITIONS.FuelPump2Read, "GENERAL ENG FUEL PUMP SWITCH:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.FuelPump2Read);
                simconnect.AddToDataDefinition(DEFINITIONS.FuelPump1_2024Read, "FUELSYSTEM PUMP SWITCH:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.FuelPump1_2024Read);
                simconnect.AddToDataDefinition(DEFINITIONS.FuelPump2_2024Read, "FUELSYSTEM PUMP SWITCH:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.FuelPump2_2024Read);
                simconnect.AddToDataDefinition(DEFINITIONS.FuelPumpIdx3_2024Read, "FUELSYSTEM PUMP SWITCH:3", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.FuelPumpIdx3_2024Read);
                // Interruptors d'àudio del panell de ràdio (2026-10-03, Pere) — mateixa prova, definicions
                // pròpies i separades.
                simconnect.AddToDataDefinition(DEFINITIONS.Com1Read, "COM TRANSMIT:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Com1Read);
                simconnect.AddToDataDefinition(DEFINITIONS.Com2Read, "COM TRANSMIT:2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Com2Read);
                simconnect.AddToDataDefinition(DEFINITIONS.ComReceiveAllRead, "COM RECEIVE ALL", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ComReceiveAllRead);
                simconnect.AddToDataDefinition(DEFINITIONS.DmeSoundRead, "DME SOUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.DmeSoundRead);
                simconnect.AddToDataDefinition(DEFINITIONS.SpkrSoundRead, "SPEAKER ACTIVE", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.SpkrSoundRead);
                simconnect.AddToDataDefinition(DEFINITIONS.MkrMuteRead, "MARKER BEACON TEST MUTE", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.MkrMuteRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Nav1SoundRead, "NAV1 SOUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Nav1SoundRead);
                simconnect.AddToDataDefinition(DEFINITIONS.AdfSoundRead, "ADF SOUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.AdfSoundRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Nav2SoundRead, "NAV2 SOUND", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Nav2SoundRead);
                // Anunciadors de l'autopilot i llums d'avís (2026-10-03, Pere) — mateixa prova, definicions
                // pròpies i separades. AUTOPILOT MASTER (DEFINITIONS.ApMasterRead, més avall) manté
                // l'efecte secundari sobre la lectura activa del trim al seu propi handler.
                simconnect.AddToDataDefinition(DEFINITIONS.ApVsHoldRead, "AUTOPILOT VERTICAL HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApVsHoldRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApFlcActiveRead, "AUTOPILOT FLIGHT LEVEL CHANGE", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApFlcActiveRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApWingLevelerRead, "AUTOPILOT WING LEVELER", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApWingLevelerRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApHdgLockRead, "AUTOPILOT HEADING LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApHdgLockRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApAprHoldRead, "AUTOPILOT APPROACH HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApAprHoldRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApGsHoldRead, "AUTOPILOT GLIDESLOPE HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApGsHoldRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApNav1LockRead, "AUTOPILOT NAV1 LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApNav1LockRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApFdActiveRead, "AUTOPILOT FLIGHT DIRECTOR ACTIVE:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApFdActiveRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApAltLockRead, "AUTOPILOT ALTITUDE LOCK", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApAltLockRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApVnavBtnRead, "L:XMLVAR_VNAVButtonValue", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApVnavBtnRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApYawDamperRead, "AUTOPILOT YAW DAMPER", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApYawDamperRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApBcHoldRead, "AUTOPILOT BACKCOURSE HOLD", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApBcHoldRead);
                simconnect.AddToDataDefinition(DEFINITIONS.XfrPushLRead, "L:XMLVAR_PushXFR", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.XfrPushLRead);
                simconnect.AddToDataDefinition(DEFINITIONS.MasterCautionActiveRead, "L:Generic_Master_Caution_Active", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.MasterCautionActiveRead);
                simconnect.AddToDataDefinition(DEFINITIONS.MasterWarningActiveRead, "L:Generic_Master_Warning_Active", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.MasterWarningActiveRead);
                // Starter/ignition (rotatius elèctrics), guardes AP/trim i deice/pitot (2026-10-03, Pere)
                // — mateixa prova, definicions pròpies i separades.
                simconnect.AddToDataDefinition(DEFINITIONS.ElecStarterRead, "L:XMLVAR_Starter", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.ElecStarterRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ElecIgnitionRead, "L:XMLVAR_Ignition", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.ElecIgnitionRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApDisengagedRead, "AUTOPILOT DISENGAGED", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApDisengagedRead);
                simconnect.AddToDataDefinition(DEFINITIONS.RudderTrimDisabledRead, "RUDDER TRIM DISABLED", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.RudderTrimDisabledRead);
                simconnect.AddToDataDefinition(DEFINITIONS.AileronTrimDisabledRead, "AILERON TRIM DISABLED", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.AileronTrimDisabledRead);
                simconnect.AddToDataDefinition(DEFINITIONS.PropDeiceRead, "PROP DEICE SWITCH:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.PropDeiceRead);
                simconnect.AddToDataDefinition(DEFINITIONS.WindshieldDeiceRead, "WINDSHIELD DEICE SWITCH", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.WindshieldDeiceRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Pitot1Read, "L:DEICE_Pitot_1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Pitot1Read);
                simconnect.AddToDataDefinition(DEFINITIONS.Pitot2Read, "L:DEICE_Pitot_2", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Pitot2Read);
                simconnect.AddToDataDefinition(DEFINITIONS.AntiIce1Read, "ENG ANTI ICE:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.AntiIce1Read);
                // Gear, display backup, elèctric (source/generator/crashbar), KAP140 i AP max bank
                // (2026-10-03, Pere) — mateixa prova, definicions pròpies i separades.
                simconnect.AddToDataDefinition(DEFINITIONS.GearCenterRead, "GEAR CENTER POSITION", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.GearCenterRead);
                simconnect.AddToDataDefinition(DEFINITIONS.GearLeftRead, "GEAR LEFT POSITION", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.GearLeftRead);
                simconnect.AddToDataDefinition(DEFINITIONS.GearRightRead, "GEAR right POSITION", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.GearRightRead);
                simconnect.AddToDataDefinition(DEFINITIONS.GearHandleRead, "GEAR HANDLE POSITION", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.GearHandleRead);
                simconnect.AddToDataDefinition(DEFINITIONS.DisplayBackupActiveRead, "L:AS1000_MID_Display_Backup_Active", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.DisplayBackupActiveRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ElecSourceSwitchRead, "L:XMLVAR_Elec_Source_Switch_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.ElecSourceSwitchRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ElecGeneratorSwitchRead, "L:XMLVAR_Elec_Generator_Switch_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.ElecGeneratorSwitchRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ElecCrashbarStateRead, "L:XMLVAR_Elec_CrashBar_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.ElecCrashbarStateRead);
                simconnect.AddToDataDefinition(DEFINITIONS.Kap140StateRead, "L:KAP140_State", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.Kap140StateRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApMaxBankRead, "AUTOPILOT MAX BANK", "Radians", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.ApMaxBankRead);
                // Últims camps de Struct1 (2026-10-03, Pere: "passa-les totes") — definicions pròpies i
                // separades; Struct1/Request1 ja no existeix.
                simconnect.AddToDataDefinition(DEFINITIONS.GyroRead, "HEADING INDICATOR", "Degrees", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.GyroRead);
                simconnect.AddToDataDefinition(DEFINITIONS.DeiceRead, "STRUCTURAL DEICE SWITCH", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.DeiceRead);
                simconnect.AddToDataDefinition(DEFINITIONS.PitotHeatSwitchRead, "PITOT HEAT", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.PitotHeatSwitchRead);
                simconnect.AddToDataDefinition(DEFINITIONS.NumEnginesRead, "NUMBER OF ENGINES", "Number", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.NumEnginesRead);
                simconnect.AddToDataDefinition(DEFINITIONS.CowlFlapLRead, "RECIP ENG COWL FLAP POSITION:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.CowlFlapLRead);
                simconnect.AddToDataDefinition(DEFINITIONS.CowlFlapRRead, "RECIP ENG COWL FLAP POSITION:2", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.CowlFlapRRead);
                simconnect.AddToDataDefinition(DEFINITIONS.CameraStateRead, "CAMERA STATE", "number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.CameraStateRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ElecFuelPumpRead, "L:XMLVAR_BoostFuelPump", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.ElecFuelPumpRead);
                simconnect.AddToDataDefinition(DEFINITIONS.PropLeverPosRead, "GENERAL ENG PROPELLER LEVER POSITION:1", "Position 16k", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.PropLeverPosRead);
                simconnect.AddToDataDefinition(DEFINITIONS.FuelValveRead, "GENERAL ENG FUEL VALVE:1", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.FuelValveRead);
                simconnect.AddToDataDefinition(DEFINITIONS.ApMasterRead, "AUTOPILOT MASTER", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightBoolStruct>(DEFINITIONS.ApMasterRead);
                simconnect.AddToDataDefinition(DEFINITIONS.AileronTrimFracRead, "AILERON TRIM PCT", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.AileronTrimFracRead);
                simconnect.AddToDataDefinition(DEFINITIONS.IsDeiceAirframeRead, "L:XMLVAR_IsDeiceAirFrame", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.IsDeiceAirframeRead);
                simconnect.AddToDataDefinition(DEFINITIONS.IsDeiceAirframe2Read, "L:XMLVAR_IsDeiceAirFrame2", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<LightNumberStruct>(DEFINITIONS.IsDeiceAirframe2Read);

                // Parking brake — definició pròpia i petita (no dins Struct1): amb ~119 camps compartits,
                // SimConnect deixava de reflectir bé els camps afegits al final. Confirmat amb un mini-programa
                // de prova aïllat que BRAKE PARKING POSITION es llegeix perfectament sol.
                simconnect.AddToDataDefinition(DEFINITIONS.ParkingBrakeRead, "BRAKE PARKING POSITION", "Bool", SIMCONNECT_DATATYPE.INT32, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<ParkingBrakeStruct>(DEFINITIONS.ParkingBrakeRead);

                // Voltatge de bateria — "ELECTRICAL MAIN BUS VOLTAGE" sempre torna 0 en aquest avió, tot i
                // tenir bateria; "ELECTRICAL BATTERY VOLTAGE" sí que reflecteix el valor real (confirmat amb
                // el mateix mini-programa de prova: ~24V amb bateria bé, cau a 0 en apagar-la).
                simconnect.AddToDataDefinition(DEFINITIONS.BatteryVoltageRead, "ELECTRICAL BATTERY VOLTAGE", "Volts", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<BatteryVoltageStruct>(DEFINITIONS.BatteryVoltageRead);

                // Selector combustible esquerre (Baron G58, 2024) — L:FUEL_Switch_Cutoff_Crossfeed_1 és la
                // variable real que mou l'animació del switch (confirmat a Behaviors i en viu: valor cru
                // 0=Off, 1=On, 2=Xfeed). Lectura real, a diferència de les bombes.
                simconnect.AddToDataDefinition(DEFINITIONS.FuelSelLeftRead, "L:FUEL_Switch_Cutoff_Crossfeed_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<FuelSelLeftStruct>(DEFINITIONS.FuelSelLeftRead);

                // Selector combustible dret — mateix esquema, L:FUEL_Switch_Cutoff_Crossfeed_2.
                simconnect.AddToDataDefinition(DEFINITIONS.FuelSelRightRead, "L:FUEL_Switch_Cutoff_Crossfeed_2", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<FuelSelRightStruct>(DEFINITIONS.FuelSelRightRead);

                // PROP RPM:1/2 — per la sincronització del selector de combustible de la DA-62 (RPM>50 => ON).
                simconnect.AddToDataDefinition(DEFINITIONS.PropRpm1Read, "PROP RPM:1", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<PropRpm1Struct>(DEFINITIONS.PropRpm1Read);

                simconnect.AddToDataDefinition(DEFINITIONS.PropRpm2Read, "PROP RPM:2", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<PropRpm2Struct>(DEFINITIONS.PropRpm2Read);

                // FUELSYSTEM VALVE SWITCH:1/2/3 — "FUEL TANK SELECTOR" confirmat que NO es mou ni des de l'app
                // ni movent el selector físic al VC. Provem l'esquema nou (junctions/valves), el mateix que
                // finalment va funcionar a la DA-62, per si la DA-40 el comparteix per sota.
                simconnect.AddToDataDefinition(DEFINITIONS.DA40FuelTankSelectorRead, "FUELSYSTEM VALVE SWITCH:1", "Enum", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA40FuelTankSelectorStruct>(DEFINITIONS.DA40FuelTankSelectorRead);

                simconnect.AddToDataDefinition(DEFINITIONS.DA40FuelTankSelector2Read, "FUELSYSTEM VALVE SWITCH:2", "Enum", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA40FuelTankSelector2Struct>(DEFINITIONS.DA40FuelTankSelector2Read);

                simconnect.AddToDataDefinition(DEFINITIONS.DA40FuelTankSelector3Read, "FUELSYSTEM VALVE SWITCH:3", "Enum", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA40FuelTankSelector3Struct>(DEFINITIONS.DA40FuelTankSelector3Read);

                simconnect.AddToDataDefinition(DEFINITIONS.DA40FuelTankSelector4Read, "FUELSYSTEM VALVE SWITCH:4", "Enum", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA40FuelTankSelector4Struct>(DEFINITIONS.DA40FuelTankSelector4Read);

                // C400 Corvalis — lectura directa de l'opció seleccionada del junction (1=Off,2=Left,3=Right).
                simconnect.AddToDataDefinition(DEFINITIONS.C400JunctionSettingRead, "FUELSYSTEM JUNCTION SETTING:1", "Enum", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<C400JunctionSettingStruct>(DEFINITIONS.C400JunctionSettingRead);

                // DA-62 — estat real del bus d'avionics (sistema elèctric modular, "LINE CONNECTION"),
                // NO l'"AVIONICS MASTER SWITCH:1" clàssic que ja llegim a Struct1 (no reflecteix aquest avió).
                simconnect.AddToDataDefinition(DEFINITIONS.DA62AvionicsLineConnRead, "LINE CONNECTION ON:'rh_main_bustoavionics_bus'_n", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA62AvionicsLineConnStruct>(DEFINITIONS.DA62AvionicsLineConnRead);

                // C172 — mateix mecanisme, un bus per cada avionics (mateix nom que a la RPN de Behaviors).
                simconnect.AddToDataDefinition(DEFINITIONS.C172Avionics1LineConnRead, "LINE CONNECTION ON:'BUS_1_To_AVIONICS_BUS_1'_n", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<C172Avionics1LineConnStruct>(DEFINITIONS.C172Avionics1LineConnRead);
                simconnect.AddToDataDefinition(DEFINITIONS.C172Avionics2LineConnRead, "LINE CONNECTION ON:'BUS_2_To_AVIONICS_BUS_2'_n", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<C172Avionics2LineConnStruct>(DEFINITIONS.C172Avionics2LineConnRead);

                // C172 (MSFS2020) — llum de panell real: A:LIGHT POTENTIOMETER:3 (0=OFF, 100=ON), confirmat
                // per Pere al material emissive del panell. El booleà genèric "LIGHT PANEL" no reflecteix
                // aquest avió (mateix patró que l'avionics).
                simconnect.AddToDataDefinition(DEFINITIONS.C172PanelLightPotRead, "LIGHT POTENTIOMETER:3", "Percent over 100", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<C172PanelLightPotStruct>(DEFINITIONS.C172PanelLightPotRead);

                // Caravan — mateix mecanisme, un bus per cada avionics.
                simconnect.AddToDataDefinition(DEFINITIONS.CaravanAvionics1LineConnRead, "LINE CONNECTION ON:'AVIONICS_BUS_1_To_PWR_BUS_DISTRIB'_n", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CaravanAvionics1LineConnStruct>(DEFINITIONS.CaravanAvionics1LineConnRead);
                simconnect.AddToDataDefinition(DEFINITIONS.CaravanAvionics2LineConnRead, "LINE CONNECTION ON:'AVIONICS_BUS_2_To_PWR_BUS_DISTRIB'_n", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CaravanAvionics2LineConnStruct>(DEFINITIONS.CaravanAvionics2LineConnRead);
                simconnect.AddToDataDefinition(DEFINITIONS.CaravanAltGen2LineConnRead, "LINE CONNECTION ON:'BATT_BUS_To_ALT_BUS'_n", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CaravanAltGen2LineConnStruct>(DEFINITIONS.CaravanAltGen2LineConnRead);

                // DA-62 — feedback real del "engine master" (piston), per sincronitzar l'animació dels botons
                // que abans feien d'AUTO IGNITION (turboprop).
                simconnect.AddToDataDefinition(DEFINITIONS.DA62EngineMaster1Read, "RECIP ENG ENGINE MASTER SWITCH:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA62EngineMaster1Struct>(DEFINITIONS.DA62EngineMaster1Read);
                simconnect.AddToDataDefinition(DEFINITIONS.DA62EngineMaster2Read, "RECIP ENG ENGINE MASTER SWITCH:2", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA62EngineMaster2Struct>(DEFINITIONS.DA62EngineMaster2Read);

                // DA-40 — "ALT/GEN 2" real: L: var global (no una O: privada de l'InputEvent), confirmat
                // al Behavior viewer. Només lectura (feedback): l'escriptura real es fa amb el RPN complet
                // via el missatger genèric (K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE necessita 2 params).
                // GENERAL ENG MASTER ALTERNATOR:2 (via Struct1) no reflecteix aquest interruptor en aquest avió.
                simconnect.AddToDataDefinition(DEFINITIONS.DA40EssentialBus2Read, "L:ELECTRICAL_EssentialBus_2", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA40EssentialBus2Struct>(DEFINITIONS.DA40EssentialBus2Read);

                // DA-40 (2020) — estat viu del relè (BUS CONNECTION ON:3), per calcular en C# (no a cegues
                // per RPN) quina crida de K:2:ELECTRICAL_BUS_TO_BUS_CONNECTION_TOGGLE cal per l'ESS BUS.
                simconnect.AddToDataDefinition(DEFINITIONS.DA40BusConnOn3Read, "BUS CONNECTION ON:3", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA40BusConnOn3Struct>(DEFINITIONS.DA40BusConnOn3Read);

                // DA-40 (2020) — feedback real del FUEL TRANSFER switch: A:CIRCUIT SWITCH ON:7 (el mateix
                // que commuta K:ELECTRICAL_CIRCUIT_TOGGLE amb índex 7).
                simconnect.AddToDataDefinition(DEFINITIONS.DA40CircuitSwitch7Read, "CIRCUIT SWITCH ON:7", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA40CircuitSwitch7Struct>(DEFINITIONS.DA40CircuitSwitch7Read);

                // DA-40 — "Engine Master" real: al Behavior viewer l'InputEvent escriu/llegeix B:ENGINE_Master_1
                // (privat del template, no llegible per SimConnect fora d'un WASM). Per feedback fem servir el
                // mateix simvar real que ja funciona per aquest concepte a la DA-62 (RECIP ENG ENGINE MASTER
                // SWITCH:1) — igual que ALT/GEN 2, la variable de template només mou el switch, el simvar real
                // és el que reflecteix l'estat del sistema. L'escriptura es fa amb el missatger genèric (B: no
                // s'hi arriba amb un TransmitClientEvent normal).
                simconnect.AddToDataDefinition(DEFINITIONS.DA40EngineMaster1Read, "RECIP ENG ENGINE MASTER SWITCH:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<DA40EngineMaster1Struct>(DEFINITIONS.DA40EngineMaster1Read);

                // Cirrus SR22T — "DEICE SWITCH AIRFRAME" real: al Behavior viewer, secció "Variable(s)",
                // confirma que aquest InputEvent es reflecteix a L:XMLVAR_DEICE_MASTER (global, no una O:
                // privada com la del binding Set). L'escriptura pel canal genèric perquè la lògica interna
                // del Set només toca una O: var inaccessible des de fora.
                simconnect.AddToDataDefinition(DEFINITIONS.CirrusDeiceMasterRead, "L:XMLVAR_DEICE_MASTER", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CirrusDeiceMasterStruct>(DEFINITIONS.CirrusDeiceMasterRead);

                // C408 SkyCourier — feedback real del selector únic FUEL_TRANSFER: combinació de
                // FUELSYSTEM PUMP SWITCH:3 (ja llegit via Struct1/read_fuelpump_idx3_2024) i :4 (nova,
                // dedicada). (3=0,4=1)=L TANK, (3=1,4=0)=R TANK, (3=0,4=0)=FUEL TRANSFER OFF.
                simconnect.AddToDataDefinition(DEFINITIONS.C408FuelPump4Read, "FUELSYSTEM PUMP SWITCH:4", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<C408FuelPump4Struct>(DEFINITIONS.C408FuelPump4Read);

                // C408 SkyCourier — "AUTO IGNITION L" real: al Behavior viewer, secció "Variable(s)",
                // confirma que ENGINE_SWITCH_IGNITION_1 es reflecteix a L:XMLVAR_IGNITION_1_1 (global, nom
                // complet trobat via Variables > Global filtrant "IGNITION"). L'escriptura pel canal genèric
                // perquè el Set complet fa servir control de flux (s0/l0/g1) no transcriptible directe a
                // RPN pla.
                simconnect.AddToDataDefinition(DEFINITIONS.C408Ignition1Read, "L:XMLVAR_IGNITION_1_1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<C408Ignition1Struct>(DEFINITIONS.C408Ignition1Read);

                // C408 SkyCourier — "AUTO IGNITION R", mateix patró que AUTO IGNITION L.
                simconnect.AddToDataDefinition(DEFINITIONS.C408Ignition2Read, "L:XMLVAR_IGNITION_2_2", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<C408Ignition2Struct>(DEFINITIONS.C408Ignition2Read);

                // Caravan — únic interruptor d'ignition (turboprop). Al Behavior viewer, l'InputEvent
                // ENGINE_IGNITION_1 escriu a una O: privada i només crida K:TURBINE_IGNITION_SWITCH_SET1 si
                // cal canviar A:TURB ENG IGNITION SWITCH EX1:1 (simvar estàndard, llegible directe). Com que
                // TURBINE_IGNITION_SWITCH_TOGGLE ja és un K: event registrat (no un InputEvent), l'enviem
                // directe amb TransmitClientEvent normal, sense missatger genèric.
                simconnect.AddToDataDefinition(DEFINITIONS.CaravanIgnition1Read, "TURB ENG IGNITION SWITCH EX1:1", "Enum", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CaravanIgnition1Struct>(DEFINITIONS.CaravanIgnition1Read);

                // Caravan — STARTER (3 posicions). L:XMLVAR_Starter1 és una L: global normal, llegible directe.
                simconnect.AddToDataDefinition(DEFINITIONS.CaravanStarter1Read, "L:XMLVAR_Starter1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CaravanStarter1Struct>(DEFINITIONS.CaravanStarter1Read);

                // Caravan — DEICE de fluid (2026-08-26, MSFS2020). L:DEICE_Airframe_1 és la posició real
                // (0=HIGH,1=NORM,2=OFF), confirmada al Set complet de l'InputEvent — NO el booleà genèric
                // STRUCTURAL DEICE SWITCH (que no distingeix HIGH de NORM).
                simconnect.AddToDataDefinition(DEFINITIONS.CaravanDeiceRead, "L:DEICE_Airframe_1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<CaravanDeiceStruct>(DEFINITIONS.CaravanDeiceRead);

                // Bonanza G36 a MSFS2020 — "Defrost Pull ON" és progressiu (posició, no booleà 0/1).
                simconnect.AddToDataDefinition(DEFINITIONS.BonanzaAntiIcePosRead, "GENERAL ENG ANTI ICE POSITION:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<BonanzaAntiIcePosStruct>(DEFINITIONS.BonanzaAntiIcePosRead);

                // TBM930 (2024) Crashbar — feedback real (Inspector Init: L:1:TBM_Elec_CrashBar_State).
                simconnect.AddToDataDefinition(DEFINITIONS.TbmCrashbar2024Read, "L:1:TBM_Elec_CrashBar_State", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<TbmCrashbar2024Struct>(DEFINITIONS.TbmCrashbar2024Read);

                // TBM930 (2024) Elec Source — feedback real (Inspector Init: L:1:TBM_Elec_Source_Switch_State).
                simconnect.AddToDataDefinition(DEFINITIONS.TbmElecSource2024Read, "L:1:TBM_Elec_Source_Switch_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<TbmElecSource2024Struct>(DEFINITIONS.TbmElecSource2024Read);

                // TBM930 (2024) Elec Generator — feedback real (Inspector Init: L:1:TBM_Elec_Generator_Switch_State).
                simconnect.AddToDataDefinition(DEFINITIONS.TbmElecGenerator2024Read, "L:1:TBM_Elec_Generator_Switch_State", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<TbmElecGenerator2024Struct>(DEFINITIONS.TbmElecGenerator2024Read);

                // TBM930 (2024) Fuel Selection — feedback real (Inspector Init: L:1:TBM_Fuel_Selection_Switch_Position).
                simconnect.AddToDataDefinition(DEFINITIONS.TbmFuelSelection2024Read, "L:1:TBM_Fuel_Selection_Switch_Position", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<TbmFuelSelection2024Struct>(DEFINITIONS.TbmFuelSelection2024Read);

                // TBM930 (2024) AP Trim — feedback real (Inspector Init: L:1:TBM_AP_Trim_Switch_Position).
                simconnect.AddToDataDefinition(DEFINITIONS.TbmApTrim2024Read, "L:1:TBM_AP_Trim_Switch_Position", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<TbmApTrim2024Struct>(DEFINITIONS.TbmApTrim2024Read);

                // TBM930 (2024) Airframe Deice LEDs — feedback real (Updates de DEICE_SYSTEM:
                // L:1:TBM_Airframe_Deice_Cycle_1_Active / _2_Active, substitueixen el genèric
                // L:XMLVAR_IsDeiceAirFrame/_2 que aquest avió ja no actualitza correctament).
                simconnect.AddToDataDefinition(DEFINITIONS.TbmDeiceCycle1_2024Read, "L:1:TBM_Airframe_Deice_Cycle_1_Active", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<TbmDeiceCycle1_2024Struct>(DEFINITIONS.TbmDeiceCycle1_2024Read);
                simconnect.AddToDataDefinition(DEFINITIONS.TbmDeiceCycle2_2024Read, "L:1:TBM_Airframe_Deice_Cycle_2_Active", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<TbmDeiceCycle2_2024Struct>(DEFINITIONS.TbmDeiceCycle2_2024Read);

                // TBM930 (2024) Inert Sep — feedback real (Inspector Init: L:1:TBM_Inert_Sep_Switch_Position).
                simconnect.AddToDataDefinition(DEFINITIONS.TbmInertSep2024Read, "L:1:TBM_Inert_Sep_Switch_Position", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<TbmInertSep2024Struct>(DEFINITIONS.TbmInertSep2024Read);

                // Missatger genèric RPN — Client Data Area compartida amb el WASM (SFS_Link). El WASM ja
                // ha cridat CreateClientData; aquí només ens hi enganxem amb el mateix nom i definició.
                simconnect.MapClientDataNameToID("SFS_LINK.GENERIC_RPN", CLIENT_DATA_ID.GenericRpnData);
                simconnect.AddToClientDataDefinition(DEFINITIONS.GenericRpnClientDataDef, 0, 1024, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<GenericRpnStruct>(DEFINITIONS.GenericRpnClientDataDef);

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
                Text = "panelAIR - WARNING",
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
            const string msg = "Please activate the SFSmonitor plugin to complete the functionality of this panelAIR App.";
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
                        System.IO.File.AppendAllText("panelAIR_App_crash.txt",
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
                            try { System.IO.File.AppendAllText("panelAIR_App_crash.txt", "\r\n[SIMCONNECT KeyNotFound] " + ex.Message); } catch { }
                        }
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        // Desconnexió brusc (0xC000014B): no tanquem; marquem "disconnected" i reintentem
                        try
                        {
                            System.IO.File.AppendAllText("panelAIR_App_crash.txt",
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
                        try { System.IO.File.AppendAllText("panelAIR_App_crash.txt", "\r\n[SIMCONNECT Dispatch] " + ex.ToString()); } catch { }
                    }
                }
            }
            else
            {
                base.WndProc(ref m);
            }

        }

        // Totes les peticions que abans eren subscripcions contínues a SIM_FRAME (40) o VISUAL_FRAME
        // (Struct1) — ara es tornen a demanar una a una (SIMCONNECT_PERIOD.ONCE) des de
        // PollLowFrequencySimData(), cridat pel timer _dataPollTimer cada 200ms. Res més canvia: els
        // handlers a Simconnect_OnRecvSimobjectData reben les dades igual, només que com a màxim
        // 5 cops/segon en lloc de cada fotograma de MSFS.
        // REVERTIT (2026-10-01): l'agrupació en 7 definicions combinades produïa valors corruptes
        // (dobles denormalitzats tipus 1.4E-310, probablement un problema de marshaling/alineació
        // en mesclar camps dins una mateixa estructura) i no va millorar els FPS — tornem als 41
        // individuals (Struct1 + 40), que ja sabíem que funcionaven correctament.
        // Struct1/Request1 (el paquet gran original) ja no existeix (2026-10-03, Pere: "passa-les
        // totes") — tots els seus camps han passat a definicions pròpies dins aquest mateix array.
        private static readonly (REQUESTS req, DEFINITIONS def)[] _lowFreqPolls = new (REQUESTS, DEFINITIONS)[]
        {
            (REQUESTS.RequestParkingBrake, DEFINITIONS.ParkingBrakeRead),
            (REQUESTS.RequestBatteryVoltage, DEFINITIONS.BatteryVoltageRead),
            (REQUESTS.RequestFuelSelLeft, DEFINITIONS.FuelSelLeftRead),
            (REQUESTS.RequestFuelSelRight, DEFINITIONS.FuelSelRightRead),
            (REQUESTS.RequestPropRpm1, DEFINITIONS.PropRpm1Read),
            (REQUESTS.RequestPropRpm2, DEFINITIONS.PropRpm2Read),
            (REQUESTS.RequestDA40FuelTankSelector, DEFINITIONS.DA40FuelTankSelectorRead),
            (REQUESTS.RequestDA40FuelTankSelector2, DEFINITIONS.DA40FuelTankSelector2Read),
            (REQUESTS.RequestDA40FuelTankSelector3, DEFINITIONS.DA40FuelTankSelector3Read),
            (REQUESTS.RequestDA40FuelTankSelector4, DEFINITIONS.DA40FuelTankSelector4Read),
            (REQUESTS.RequestC400JunctionSetting, DEFINITIONS.C400JunctionSettingRead),
            (REQUESTS.RequestDA62AvionicsLineConn, DEFINITIONS.DA62AvionicsLineConnRead),
            (REQUESTS.RequestC172Avionics1LineConn, DEFINITIONS.C172Avionics1LineConnRead),
            (REQUESTS.RequestC172Avionics2LineConn, DEFINITIONS.C172Avionics2LineConnRead),
            (REQUESTS.RequestC172PanelLightPot, DEFINITIONS.C172PanelLightPotRead),
            (REQUESTS.RequestCaravanAvionics1LineConn, DEFINITIONS.CaravanAvionics1LineConnRead),
            (REQUESTS.RequestCaravanAvionics2LineConn, DEFINITIONS.CaravanAvionics2LineConnRead),
            (REQUESTS.RequestCaravanAltGen2LineConn, DEFINITIONS.CaravanAltGen2LineConnRead),
            (REQUESTS.RequestDA62EngineMaster1, DEFINITIONS.DA62EngineMaster1Read),
            (REQUESTS.RequestDA62EngineMaster2, DEFINITIONS.DA62EngineMaster2Read),
            (REQUESTS.RequestDA40EssentialBus2, DEFINITIONS.DA40EssentialBus2Read),
            (REQUESTS.RequestDA40BusConnOn3, DEFINITIONS.DA40BusConnOn3Read),
            (REQUESTS.RequestDA40CircuitSwitch7, DEFINITIONS.DA40CircuitSwitch7Read),
            (REQUESTS.RequestDA40EngineMaster1, DEFINITIONS.DA40EngineMaster1Read),
            (REQUESTS.RequestCirrusDeiceMaster, DEFINITIONS.CirrusDeiceMasterRead),
            (REQUESTS.RequestC408FuelPump4, DEFINITIONS.C408FuelPump4Read),
            (REQUESTS.RequestC408Ignition1, DEFINITIONS.C408Ignition1Read),
            (REQUESTS.RequestC408Ignition2, DEFINITIONS.C408Ignition2Read),
            (REQUESTS.RequestCaravanIgnition1, DEFINITIONS.CaravanIgnition1Read),
            (REQUESTS.RequestCaravanStarter1, DEFINITIONS.CaravanStarter1Read),
            (REQUESTS.RequestCaravanDeice, DEFINITIONS.CaravanDeiceRead),
            (REQUESTS.RequestBonanzaAntiIcePos, DEFINITIONS.BonanzaAntiIcePosRead),
            (REQUESTS.RequestTbmCrashbar2024, DEFINITIONS.TbmCrashbar2024Read),
            (REQUESTS.RequestTbmElecSource2024, DEFINITIONS.TbmElecSource2024Read),
            (REQUESTS.RequestTbmElecGenerator2024, DEFINITIONS.TbmElecGenerator2024Read),
            (REQUESTS.RequestTbmFuelSelection2024, DEFINITIONS.TbmFuelSelection2024Read),
            (REQUESTS.RequestTbmApTrim2024, DEFINITIONS.TbmApTrim2024Read),
            (REQUESTS.RequestTbmDeiceCycle1_2024, DEFINITIONS.TbmDeiceCycle1_2024Read),
            (REQUESTS.RequestTbmDeiceCycle2_2024, DEFINITIONS.TbmDeiceCycle2_2024Read),
            (REQUESTS.RequestTbmInertSep2024, DEFINITIONS.TbmInertSep2024Read),
            // Lights (2026-10-03, Pere): provades fora de Struct1 amb sync+confirmació puntual, ara
            // passades al ritme fix de 200ms com la resta d'aquest grup, sense cap tractament especial.
            (REQUESTS.RequestLightBeacon, DEFINITIONS.LightBeaconRead),
            (REQUESTS.RequestLightLand, DEFINITIONS.LightLandRead),
            (REQUESTS.RequestLightTaxi, DEFINITIONS.LightTaxiRead),
            (REQUESTS.RequestLightNav, DEFINITIONS.LightNavRead),
            (REQUESTS.RequestLightStrobe, DEFINITIONS.LightStrobeRead),
            (REQUESTS.RequestLightRecogn, DEFINITIONS.LightRecognRead),
            (REQUESTS.RequestLightPanel, DEFINITIONS.LightPanelRead),
            (REQUESTS.RequestLightLogo, DEFINITIONS.LightLogoRead),
            (REQUESTS.RequestLightingLogo1, DEFINITIONS.LightingLogo1Read),
            (REQUESTS.RequestLightingWing1, DEFINITIONS.LightingWing1Read),
            // Bateria, avionics, alternators, magnetos/starter, fuel pump (2026-10-03, Pere): mateixa
            // prova que els llums, treure'ls del paquet gran de Struct1 cap al ritme fix de 200ms.
            (REQUESTS.RequestMasterBattery, DEFINITIONS.MasterBatteryRead),
            (REQUESTS.RequestAvionics1, DEFINITIONS.Avionics1Read),
            (REQUESTS.RequestAvionics2, DEFINITIONS.Avionics2Read),
            (REQUESTS.RequestAltGen1, DEFINITIONS.AltGen1Read),
            (REQUESTS.RequestAltGen2, DEFINITIONS.AltGen2Read),
            (REQUESTS.RequestIgn1Left, DEFINITIONS.Ign1LeftRead),
            (REQUESTS.RequestIgn1Right, DEFINITIONS.Ign1RightRead),
            (REQUESTS.RequestIgn1Starter, DEFINITIONS.Ign1StarterRead),
            (REQUESTS.RequestIgn2Left, DEFINITIONS.Ign2LeftRead),
            (REQUESTS.RequestIgn2Right, DEFINITIONS.Ign2RightRead),
            (REQUESTS.RequestIgn2Starter, DEFINITIONS.Ign2StarterRead),
            (REQUESTS.RequestFuelPump1, DEFINITIONS.FuelPump1Read),
            (REQUESTS.RequestFuelPump2, DEFINITIONS.FuelPump2Read),
            (REQUESTS.RequestFuelPump1_2024, DEFINITIONS.FuelPump1_2024Read),
            (REQUESTS.RequestFuelPump2_2024, DEFINITIONS.FuelPump2_2024Read),
            (REQUESTS.RequestFuelPumpIdx3_2024, DEFINITIONS.FuelPumpIdx3_2024Read),
            (REQUESTS.RequestCom1, DEFINITIONS.Com1Read),
            (REQUESTS.RequestCom2, DEFINITIONS.Com2Read),
            (REQUESTS.RequestComReceiveAll, DEFINITIONS.ComReceiveAllRead),
            (REQUESTS.RequestDmeSound, DEFINITIONS.DmeSoundRead),
            (REQUESTS.RequestSpkrSound, DEFINITIONS.SpkrSoundRead),
            (REQUESTS.RequestMkrMute, DEFINITIONS.MkrMuteRead),
            (REQUESTS.RequestNav1Sound, DEFINITIONS.Nav1SoundRead),
            (REQUESTS.RequestAdfSound, DEFINITIONS.AdfSoundRead),
            (REQUESTS.RequestNav2Sound, DEFINITIONS.Nav2SoundRead),
            (REQUESTS.RequestApVsHold, DEFINITIONS.ApVsHoldRead),
            (REQUESTS.RequestApFlcActive, DEFINITIONS.ApFlcActiveRead),
            (REQUESTS.RequestApWingLeveler, DEFINITIONS.ApWingLevelerRead),
            (REQUESTS.RequestApHdgLock, DEFINITIONS.ApHdgLockRead),
            (REQUESTS.RequestApAprHold, DEFINITIONS.ApAprHoldRead),
            (REQUESTS.RequestApGsHold, DEFINITIONS.ApGsHoldRead),
            (REQUESTS.RequestApNav1Lock, DEFINITIONS.ApNav1LockRead),
            (REQUESTS.RequestApFdActive, DEFINITIONS.ApFdActiveRead),
            (REQUESTS.RequestApAltLock, DEFINITIONS.ApAltLockRead),
            (REQUESTS.RequestApVnavBtn, DEFINITIONS.ApVnavBtnRead),
            (REQUESTS.RequestApYawDamper, DEFINITIONS.ApYawDamperRead),
            (REQUESTS.RequestApBcHold, DEFINITIONS.ApBcHoldRead),
            (REQUESTS.RequestXfrPushL, DEFINITIONS.XfrPushLRead),
            (REQUESTS.RequestMasterCautionActive, DEFINITIONS.MasterCautionActiveRead),
            (REQUESTS.RequestMasterWarningActive, DEFINITIONS.MasterWarningActiveRead),
            (REQUESTS.RequestElecStarter, DEFINITIONS.ElecStarterRead),
            (REQUESTS.RequestElecIgnition, DEFINITIONS.ElecIgnitionRead),
            (REQUESTS.RequestApDisengaged, DEFINITIONS.ApDisengagedRead),
            (REQUESTS.RequestRudderTrimDisabled, DEFINITIONS.RudderTrimDisabledRead),
            (REQUESTS.RequestAileronTrimDisabled, DEFINITIONS.AileronTrimDisabledRead),
            (REQUESTS.RequestPropDeice, DEFINITIONS.PropDeiceRead),
            (REQUESTS.RequestWindshieldDeice, DEFINITIONS.WindshieldDeiceRead),
            (REQUESTS.RequestPitot1, DEFINITIONS.Pitot1Read),
            (REQUESTS.RequestPitot2, DEFINITIONS.Pitot2Read),
            (REQUESTS.RequestAntiIce1, DEFINITIONS.AntiIce1Read),
            (REQUESTS.RequestGearCenter, DEFINITIONS.GearCenterRead),
            (REQUESTS.RequestGearLeft, DEFINITIONS.GearLeftRead),
            (REQUESTS.RequestGearRight, DEFINITIONS.GearRightRead),
            (REQUESTS.RequestGearHandle, DEFINITIONS.GearHandleRead),
            (REQUESTS.RequestDisplayBackupActive, DEFINITIONS.DisplayBackupActiveRead),
            (REQUESTS.RequestElecSourceSwitch, DEFINITIONS.ElecSourceSwitchRead),
            (REQUESTS.RequestElecGeneratorSwitch, DEFINITIONS.ElecGeneratorSwitchRead),
            (REQUESTS.RequestElecCrashbarState, DEFINITIONS.ElecCrashbarStateRead),
            (REQUESTS.RequestKap140State, DEFINITIONS.Kap140StateRead),
            (REQUESTS.RequestApMaxBank, DEFINITIONS.ApMaxBankRead),
            (REQUESTS.RequestGyro, DEFINITIONS.GyroRead),
            (REQUESTS.RequestDeice, DEFINITIONS.DeiceRead),
            (REQUESTS.RequestPitotHeatSwitch, DEFINITIONS.PitotHeatSwitchRead),
            (REQUESTS.RequestNumEngines, DEFINITIONS.NumEnginesRead),
            (REQUESTS.RequestCowlFlapL, DEFINITIONS.CowlFlapLRead),
            (REQUESTS.RequestCowlFlapR, DEFINITIONS.CowlFlapRRead),
            (REQUESTS.RequestCameraState, DEFINITIONS.CameraStateRead),
            (REQUESTS.RequestElecFuelPump, DEFINITIONS.ElecFuelPumpRead),
            (REQUESTS.RequestPropLeverPos, DEFINITIONS.PropLeverPosRead),
            (REQUESTS.RequestFuelValve, DEFINITIONS.FuelValveRead),
            (REQUESTS.RequestApMaster, DEFINITIONS.ApMasterRead),
            (REQUESTS.RequestAileronTrimFrac, DEFINITIONS.AileronTrimFracRead),
            (REQUESTS.RequestIsDeiceAirframe, DEFINITIONS.IsDeiceAirframeRead),
            (REQUESTS.RequestIsDeiceAirframe2, DEFINITIONS.IsDeiceAirframe2Read),
            // Throttle lever (2026-10-03, Pere): floor unificat amb la resta — abans tenia el seu
            // propi timer de 1Hz (_thrLeverIdlePollTimer, ja eliminat); ara el detecta aquest mateix
            // grup de 200ms, igual que totes les altres. El handler de RequestThrottleLeverPos ja
            // feia "si canvia, 2s actius a 50ms" pel seu compte — no ha calgut tocar-hi res més.
            (REQUESTS.RequestThrottleLeverPos, DEFINITIONS.ThrottleLeverPosRead),
            // FUEL TANK SELECTOR:1/2 (2026-10-03, Pere, pas 2 de la unificació): el floor torna al
            // mateix ritme de 200ms per tothom, incloent la TBM — abans la TBM tenia un timer propi
            // de 1Hz perquè el seu sistema automàtic gira el selector sol; ara el detecta aquest
            // mateix grup, igual que el throttle. Els handlers de RequestFuelTankSelector1/2 porten
            // la regla "si canvia, 2s actius a 50ms" pel seu compte (_fuelTankSelectorActiveReadTimer).
            (REQUESTS.RequestFuelTankSelector1, DEFINITIONS.FuelTankSelector1Read),
            (REQUESTS.RequestFuelTankSelector2, DEFINITIONS.FuelTankSelector2Read),
            // FLAPS HANDLE INDEX (2026-10-03, Pere, pas 3): floor unificat — abans només es llegia
            // activament quan NOSALTRES clicàvem amunt/avall (StartFlapsActiveRead, ja eliminat).
            (REQUESTS.RequestFlapsHandleIndex, DEFINITIONS.FlapsHandleIndexRead),
        };

        private void PollLowFrequencySimData()
        {
            if (simconnect == null) return;
            // PROVA PROVISIONAL (2026-10-03, Pere): si fa 2s que no s'envia cap comanda (veure
            // MarkCommandSent), tornem al floor lent de 1000ms.
            if (_dataPollTimer != null && _dataPollTimer.Interval != 1000 && (DateTime.UtcNow - _lastCommandSentUtc).TotalSeconds >= 2.0)
                _dataPollTimer.Interval = 1000;
            foreach (var (req, def) in _lowFreqPolls)
            {
                try
                {
                    simconnect.RequestDataOnSimObject(req, def, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                }
                catch { }
            }
        }

        private void PollFuelTankSelectors()
        {
            if (simconnect == null) return;
            try
            {
                simconnect.RequestDataOnSimObject(REQUESTS.RequestFuelTankSelector1, DEFINITIONS.FuelTankSelector1Read, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                simconnect.RequestDataOnSimObject(REQUESTS.RequestFuelTankSelector2, DEFINITIONS.FuelTankSelector2Read, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch { }
        }

        // Sync puntual del throttle lever: en obrir connexió, canvi d'avió, pel floor de 200ms
        // (_lowFreqPolls) i a cada tick de _thrLeverActiveReadTimer (50ms) mentre hi ha moviment.
        private void SyncThrLeverPosOnce()
        {
            if (simconnect == null) return;
            try
            {
                simconnect.RequestDataOnSimObject(REQUESTS.RequestThrottleLeverPos, DEFINITIONS.ThrottleLeverPosRead, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch { }
        }

        // Lectura genèrica d'1 llum (veure comentari a LightBoolStruct) — un sol mètode per no
        // repetir 10 cops el mateix try/catch; cada llum continua tenint la seva pròpia definició
        // SimConnect, això només evita duplicar el codi C# que la demana.
        private void SyncLightOnce(REQUESTS req, DEFINITIONS def)
        {
            if (simconnect == null) return;
            try
            {
                simconnect.RequestDataOnSimObject(req, def, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch { }
        }

        // Crida's en obrir connexió i en canvi d'avió — sync puntual de les 10 llums.
        private void SyncAllLightsOnce()
        {
            SyncLightOnce(REQUESTS.RequestLightBeacon, DEFINITIONS.LightBeaconRead);
            SyncLightOnce(REQUESTS.RequestLightLand, DEFINITIONS.LightLandRead);
            SyncLightOnce(REQUESTS.RequestLightTaxi, DEFINITIONS.LightTaxiRead);
            SyncLightOnce(REQUESTS.RequestLightNav, DEFINITIONS.LightNavRead);
            SyncLightOnce(REQUESTS.RequestLightStrobe, DEFINITIONS.LightStrobeRead);
            SyncLightOnce(REQUESTS.RequestLightRecogn, DEFINITIONS.LightRecognRead);
            SyncLightOnce(REQUESTS.RequestLightPanel, DEFINITIONS.LightPanelRead);
            SyncLightOnce(REQUESTS.RequestLightLogo, DEFINITIONS.LightLogoRead);
            SyncLightOnce(REQUESTS.RequestLightingLogo1, DEFINITIONS.LightingLogo1Read);
            SyncLightOnce(REQUESTS.RequestLightingWing1, DEFINITIONS.LightingWing1Read);
        }

        // Mou el "handle" visual del throttle (G3K_THR_HANDLE) segons _thrLeverPos/_propLeverPosCached/
        // _fuelValveCached — els 3 tenen lectura pròpia i independent (2026-10-03, Pere: "passa-les
        // totes"). El "carril dret" (comparació per valor exacte amb propLeverPos/fuelValve) només
        // s'avalua quan ve de RequestPropLeverPos/RequestFuelValve (includeRightRailCheck=true) —
        // propLeverPos/fuelValve canvien poc, així que reavaluar-ho cada cop que arriben és barat.
        // thrLeverPos (fins a cada 50ms durant un arrossegament) crida amb includeRightRailCheck=false:
        // fer-ho a cada tick seu seria car i, abans de tenir lectura pròpia, ja vam confirmar que
        // barrejar un thrLeverPos fresquíssim amb un propLeverPos/fuelValve vell (quan encara anava
        // dins Struct1, cada 10s) podia disparar un salt fals sense que ningú hagués tocat res.
        private void UpdateThrHandleFromLeverPos(bool includeRightRailCheck)
        {
            if (!_g3000Active || !pbMain.IsHandleCreated) return;
            pbMain.BeginInvoke(new Action(() =>
            {
                // Sync inicial directe: amb dades fresques i coherents (Struct1) podem saber del
                // tot si hauríem d'estar al carril esquerre (power) encara que el throttle no s'hagi
                // mogut MAI des que l'app ha arrencat — sense això, si l'app arrenca amb el throttle
                // ja quiet al carril esquerre, mai es detecta cap "salt" i es queda per sempre a la
                // posició per defecte del carril dret (confirmat per Pere, 2026-10-03).
                if (includeRightRailCheck && !_thrHandleLeftRail && _thrQuadrantVisible && !_thrHandleDragging && DateTime.UtcNow > _thrIgnoreFollowUntil)
                {
                    bool looksLikeRightRail = _fuelValveCached == 0
                        || ((int)_propLeverPosCached == -4096 && ((int)_thrLeverPos == 4915 || (int)_thrLeverPos == 0));
                    if (!looksLikeRightRail)
                    {
                        var taxiA6 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_TAXI");
                        var thrH6 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                        if (taxiA6 != null && thrH6 != null)
                        {
                            _thrHandleLeftRailCenterY = taxiA6.BasePos.Y + taxiA6.Bounds.Height / 2 - thrH6.Bounds.Height / 2;
                            _thrHandleLeftRail = true;
                            _taxiPropActive = true;
                            _lastThrLeverPos = _thrLeverPos;
                        }
                    }
                }
                if (!_thrHandleLeftRail && _thrQuadrantVisible && !_thrHandleDragging && DateTime.UtcNow > _thrIgnoreFollowUntil)
                {
                    if (!double.IsNaN(_lastThrLeverPos) && Math.Abs(_thrLeverPos - _lastThrLeverPos) > 200)
                    {
                        var taxiA5 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_TAXI");
                        var thrH5 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                        if (taxiA5 != null && thrH5 != null)
                            _thrHandleLeftRailCenterY = taxiA5.BasePos.Y + taxiA5.Bounds.Height / 2 - thrH5.Bounds.Height / 2;
                        _thrHandleLeftRail = true;
                        _taxiPropActive = true;
                    }
                    _lastThrLeverPos = _thrLeverPos;
                }
                if (_thrQuadrantVisible && !_thrHandleDragging && DateTime.UtcNow > _thrIgnoreFollowUntil && _thrHandleLeftRail)
                {
                    var thrH4 = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                    if (thrH4 != null)
                    {
                        int read = (int)_thrLeverPos;
                        int centerY = _thrHandleLeftRailCenterY;
                        int handleY;
                        if (read >= 0)
                            handleY = centerY - (int)((long)read * 375 / 16383);
                        else
                            handleY = centerY - (int)((long)read * 250 / 2048);
                        handleY = Math.Max(centerY - 375, Math.Min(centerY + 250, handleY));
                        thrH4.BasePos = new Point(3694, handleY);
                        if ((DateTime.UtcNow - _lastThrHandleInvalidate).TotalMilliseconds >= 50)
                        {
                            _lastThrHandleInvalidate = DateTime.UtcNow;
                            const int margin = 30;
                            var thrRect = new Rectangle(
                                3694 - margin,
                                centerY - 375 - margin,
                                thrH4.Bounds.Width + margin * 2,
                                625 + thrH4.Bounds.Height + margin * 2);
                            pbMain.BeginInvoke(new Action(() => pbMain.Invalidate(ToRealInvalidateRect(thrRect))));
                        }
                    }
                }
                // Seguiment carril dret des de MSFS (fuel valve / prop+thr) — només amb valors coherents
                // entre ells (veure comentari del mètode).
                if (includeRightRailCheck && _thrQuadrantVisible && !_thrHandleDragging && DateTime.UtcNow > _thrIgnoreRightRailUntil) // anti-rebot
                {
                    if (_fuelValveCached == 0)
                    {
                        if (_thrHandleLeftRail) { _thrHandleLeftRail = false; _lastThrLeverPos = double.NaN; }
                        MoveThrHandle(_thrHandleDefaultY + 150);
                    }
                    else if ((int)_propLeverPosCached == -4096 && (int)_thrLeverPos == 4915)
                    {
                        if (_thrHandleLeftRail) { _thrHandleLeftRail = false; _lastThrLeverPos = double.NaN; }
                        MoveThrHandle(_thrHandleDefaultY - 170);
                    }
                    else if ((int)_propLeverPosCached == -4096 && (int)_thrLeverPos == 0)
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

        // Sync puntual de FLAPS NUM HANDLE POSITIONS: pràcticament constant per avió, només cal en
        // obrir connexió i en detectar canvi d'avió — mai activament (veure SyncFlapsHandleIndexOnce).
        private void SyncFlapsNumPosOnce()
        {
            if (simconnect == null) return;
            try
            {
                simconnect.RequestDataOnSimObject(REQUESTS.RequestFlapsNumPos, DEFINITIONS.FlapsNumPosRead, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch { }
        }

        // Sync de FLAPS HANDLE INDEX (la posició real): en obrir connexió, en canvi d'avió, pel floor
        // de 200ms (_lowFreqPolls) i a cada tick de _flapsActiveReadTimer (50ms) mentre hi ha moviment.
        private void SyncFlapsHandleIndexOnce()
        {
            if (simconnect == null) return;
            try
            {
                simconnect.RequestDataOnSimObject(REQUESTS.RequestFlapsHandleIndex, DEFINITIONS.FlapsHandleIndexRead, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch { }
        }

        // Sync puntual de trim: es criden en obrir connexió, en detectar canvi d'avió, i a cada tick
        // dels timers actius respectius (veure StartXxxTrimActiveRead/StopXxxTrimActiveReadIfNotNeeded).
        private void SyncElevatorTrimOnce()
        {
            if (simconnect == null) return;
            try
            {
                simconnect.RequestDataOnSimObject(REQUESTS.RequestElevatorTrimPos, DEFINITIONS.ElevatorTrimRead, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch { }
        }
        private void SyncRudderTrimOnce()
        {
            if (simconnect == null) return;
            try
            {
                simconnect.RequestDataOnSimObject(REQUESTS.RequestRudderTrimPos, DEFINITIONS.RudderTrimRead, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch { }
        }

        // Ritme de lectura activa de l'elevator trim: ràpid (50ms) mentre el dit arrossega la roda
        // (cal màxima resposta, és un gest curt), molt més lent (150ms) quan només l'AP la mou sol
        // (no hi ha dit a seguir, i en turbulència l'AP pot corregir contínuament durant minuts — a
        // 33ms costava fins a 10 FPS volant de veritat, confirmat per Pere, 2026-10-02).
        private void UpdateElevatorTrimActiveRate()
        {
            if (_elevatorTrimActiveReadTimer == null) return;
            _elevatorTrimActiveReadTimer.Interval = (trimDragging || _g3kTrimDragging) ? 50 : 150; // prova 2026-10-03
        }

        // Mentre es mou la roda d'elevator trim amb el dit, o mentre l'AP està actiu, cal lectura
        // activa perquè l'indicador (cercle) pugui seguir el valor real (confirmat per Pere,
        // 2026-10-02: sense això l'indicador es queda congelat durant l'arrossegament).
        private void StartElevatorTrimActiveRead()
        {
            UpdateElevatorTrimActiveRate();
            // La lectura immediata de debò la fa SendTrimTick en el primer tic real (veure
            // _elevatorTrimAwaitingFirstTick) — aquí, en tocar la roda, encara no ha canviat res.
            _elevatorTrimAwaitingFirstTick = true;
            _elevatorTrimActiveReadTimer?.Start();
        }

        // Es crida en deixar anar el ratolí/dit. Només para si l'AP no està actiu ni cap de les dues
        // rodes d'elevator trim (G1000/G3000) s'està arrossegant; si l'AP encara el necessita, el
        // deixa en marxa però baixa el ritme (ja no cal seguir el dit).
        private void StopElevatorTrimActiveReadIfNotNeeded()
        {
            if (_apMaster != 1 && !trimDragging && !_g3kTrimDragging)
                _elevatorTrimActiveReadTimer?.Stop();
            else
                UpdateElevatorTrimActiveRate();
        }

        // Mateix mecanisme pel rudder trim, però MAI per l'AP (no és motoritzat) — només mentre
        // s'arrossega la seva pròpia roda.
        private void StartRudderTrimActiveRead()
        {
            // Mateix motiu que StartElevatorTrimActiveRead.
            _rudderTrimAwaitingFirstTick = true;
            _rudderTrimActiveReadTimer?.Start();
        }

        private void StopRudderTrimActiveReadIfNotNeeded()
        {
            if (!rudderTrimDragging && !_g3kRudderTrimDragging)
                _rudderTrimActiveReadTimer?.Stop();
        }

        // Decideix fuel1Pos/fuel2Pos (ganivets) i l'angle objectiu del rotatiu G3K_FSEL_SW (TBM) a
        // partir de _fuelTankSelector1/2 (FUEL TANK SELECTOR:1/2, lectura pròpia — veure comentari
        // del camp). Es crida des dels handlers de RequestFuelTankSelector1/2 quan qualsevol dels
        // dos canvia; abans aquesta lògica vivia dins del handler de Struct1.
        private void UpdateFuelSelectorState()
        {
            bool needsRepaint = false;
            int newFuel1Pos, newFuel2Pos;

            if (IsBonanzaG36())
            {
                // Selector únic (SW_SE_SELECTOR), no els 2 ganivets fuel1Pos/fuel2Pos.
                // ASIMETRIA confirmada: escriure (K:FUEL_SELECTOR_SET) i llegir (FUEL TANK SELECTOR:1)
                // fan servir codis DIFERENTS per Right — escriure 1 mou a Right, però llegit torna 3
                // (verificat movent el selector a mà al VC): lectura real 0=Off, 2=Left, 3=Right.
                if (_fuelTankSelector1 != _bonanzaFuelSelectorRawLast)
                {
                    Debug.WriteLine($"[BONANZA FUEL TANK SELECTOR:1] {_bonanzaFuelSelectorRawLast} -> {_fuelTankSelector1}");
                    _bonanzaFuelSelectorRawLast = _fuelTankSelector1;
                }
                int newPosFromRead = _fuelTankSelector1 switch { 0 => 0, 2 => 1, 3 => 2, _ => seSelectorPos };
                if (newPosFromRead != seSelectorPos && DateTime.UtcNow >= _seSelectorIgnoreFollowUntil)
                {
                    seSelectorPos = newPosFromRead;
                    needsRepaint = true;
                }
                newFuel1Pos = fuel1Pos;
                newFuel2Pos = fuel2Pos;
            }
            else if (IsCirrusSR22())
            {
                // Selector únic, mateixa lectura reutilitzada que Bonanza/C400. ASIMETRIA confirmada
                // per Pere movent el selector a mà al VC i mirant el Debug Output (igual que la
                // Bonanza: escriptura i lectura NO fan servir el mateix codi): raw 0=OFF, raw 2=LEFT,
                // raw 3=RIGHT (valors reals confirmats, no coincideixen amb els d'escriptura 0/1/2).
                if (_fuelTankSelector1 != _cirrusFuelSelectorRawLast)
                {
                    Debug.WriteLine($"[CIRRUS FUEL TANK SELECTOR:1] {_cirrusFuelSelectorRawLast} -> {_fuelTankSelector1}");
                    _cirrusFuelSelectorRawLast = _fuelTankSelector1;
                }
                int newPosFromRead = _fuelTankSelector1 switch { 0 => 0, 2 => 1, 3 => 2, _ => seSelectorPos };
                if (newPosFromRead != seSelectorPos && DateTime.UtcNow >= _seSelectorIgnoreFollowUntil)
                {
                    seSelectorPos = newPosFromRead;
                    needsRepaint = true;
                }
                newFuel1Pos = fuel1Pos;
                newFuel2Pos = fuel2Pos;
            }
            else if (IsDA40() && !_isMsfs2024)
            {
                // DA-40 (2020 NOMÉS) — mateix patró que Bonanza/Cirrus: selector únic (SW_SE_SELECTOR).
                // Aquí escriptura (K:FUEL_SELECTOR_SET) i lectura fan servir la MATEIXA codificació
                // (0=OFF, 2=LEFT, 3=RIGHT), confirmada via revòlver SEND (2026-08-28).
                if (_fuelTankSelector1 != _da40FuelSelectorRawLast)
                {
                    Debug.WriteLine($"[DA40 FUEL TANK SELECTOR:1] {_da40FuelSelectorRawLast} -> {_fuelTankSelector1}");
                    _da40FuelSelectorRawLast = _fuelTankSelector1;
                }
                int newPosFromRead = _fuelTankSelector1 switch { 0 => 0, 2 => 1, 3 => 2, _ => seSelectorPos };
                if (newPosFromRead != seSelectorPos && DateTime.UtcNow >= _seSelectorIgnoreFollowUntil)
                {
                    seSelectorPos = newPosFromRead;
                    needsRepaint = true;
                }
                newFuel1Pos = fuel1Pos;
                newFuel2Pos = fuel2Pos;
            }
            else if (IsC172() && !_isMsfs2024)
            {
                // C172 Skyhawk (2020 NOMÉS, 2026-09-01) — mateix patró que Bonanza/Cirrus/DA40-2020.
                // Enum clàssic (0=Off, 1=All, 2=Left, 3=Right) — el Skyhawk no té Off, per això
                // qualsevol valor que no sigui 2(Left)/3(Right) es tracta com a BOTH.
                if (_fuelTankSelector1 != _c172FuelSelectorRawLast)
                {
                    Debug.WriteLine($"[C172 SKYHAWK 2020 FUEL TANK SELECTOR:1] {_c172FuelSelectorRawLast} -> {_fuelTankSelector1}");
                    _c172FuelSelectorRawLast = _fuelTankSelector1;
                }
                int newPosFromRead = _fuelTankSelector1 switch { 2 => 0, 3 => 2, _ => 1 };
                if (newPosFromRead != seSelectorPos && DateTime.UtcNow >= _seSelectorIgnoreFollowUntil)
                {
                    seSelectorPos = newPosFromRead;
                    needsRepaint = true;
                }
                newFuel1Pos = fuel1Pos;
                newFuel2Pos = fuel2Pos;
            }
            else if (IsDA40() && _isMsfs2024)
            {
                // DA-40 (2024 NOMÉS, 2026-08-30) — feedback real: FUELSYSTEM VALVE SWITCH:1 (=1 quan
                // NORMAL/Left actiu) i SWITCH:2 (=1 quan EMERGENCY/Right actiu), ja llegits per
                // RequestDA40FuelTankSelector/2 (registrats a Simconnect_OnRecvOpen). Ambdós a 0 = OFF.
                int newPosFromRead = _daFuelTankSelector2 >= 0.5 ? 2 : (_daFuelTankSelector >= 0.5 ? 1 : 0);
                if (newPosFromRead != seSelectorPos && DateTime.UtcNow >= _seSelectorIgnoreFollowUntil)
                {
                    seSelectorPos = newPosFromRead;
                    needsRepaint = true;
                }
                newFuel1Pos = fuel1Pos;
                newFuel2Pos = fuel2Pos;
            }
            else if (IsCaravan() && _isMsfs2024)
            {
                // Caravan (MSFS2024 NOMÉS): monomotor (numEngines==1), per això cauria al bloc "SINGLE
                // ENGINE" de sota, que sobreescriu fuel1Pos/fuel2Pos amb la SimVar legacy "FUEL TANK
                // SELECTOR:1" — irrellevant per aquest avió a 2024 (fa servir FUELSYSTEM VALVE
                // SWITCH:1/4). Exclòs: fuel1Pos/fuel2Pos ja els actualitzen aquells handlers dedicats.
                // MSFS2020 (confirmat 2026-08-26): la Caravan NO té lectura dedicada — cau al bloc
                // "SINGLE ENGINE" de sota, que SÍ és la lectura real correcta.
                newFuel1Pos = fuel1Pos;
                newFuel2Pos = fuel2Pos;
            }
            else if (IsDA62() && !_isMsfs2024)
            {
                // DA-62 (2020, 2026-08-28) — preset MobiFlight/HubHop real (Imadunatic) confirma que
                // l'enum de A:FUEL TANK SELECTOR:1/2 per aquestes palanques NO és el 0/2/3 genèric
                // d'altres bimotors. Esquerra (selector:1): 0=OFF, 19=ON(Left Main), 20=CROSSFEED
                // (Right Main). Dreta (selector:2): 20=ON(Right Main, confirmat), 0=OFF i 19=CROSSFEED
                // (Left Main) inferits per simetria — pendent de confirmar del tot.
                if (_fuelTankSelector1 == 0)       newFuel1Pos = 0; // OFF
                else if (_fuelTankSelector1 == 19) newFuel1Pos = 1; // ON (Left Main)
                else if (_fuelTankSelector1 == 20) newFuel1Pos = 2; // CROSSFEED (Right Main)
                else                                newFuel1Pos = fuel1Pos;
                if (_fuelTankSelector2 == 19)       newFuel2Pos = 0; // CROSSFEED (Left Main)
                else if (_fuelTankSelector2 == 20)  newFuel2Pos = 1; // ON (Right Main)
                else if (_fuelTankSelector2 == 0)   newFuel2Pos = 2; // OFF
                else                                 newFuel2Pos = fuel2Pos;
            }
            else if (_isMsfs2024 && numEngines >= 2)
            {
                // Bimotors MSFS2024: estat actualitzat pels handlers dedicats RequestFuelSelLeft/Right
                // (L:FUEL_Switch_Cutoff_Crossfeed_1/_2) — la SimVar "FUEL TANK SELECTOR" no reflecteix
                // el sistema de junctions (FUELSYSTEM_JUNCTION_SET). Confirmat al Baron G58; si un altre
                // bimotor 2024 no es sincronitza bé, probablement necessita el seu propi nom de L:Var
                // i/o número de junction — no assumir que són els mateixos sense verificar-ho.
                newFuel1Pos = fuel1Pos;
                newFuel2Pos = fuel2Pos;
            }
            else if (numEngines >= 2)
            {
                // TWIN ENGINE — selectors independents
                if (IsBaronG58() && !_isMsfs2024 && DateTime.UtcNow < _fuelSel1IgnoreFollowUntil)
                {
                    // Baron G58 (2020, confirmat 2026-08-26): mateix rebot transitori just després
                    // del clic que ja vam veure a la Caravan — ignorem la lectura una finestra curta.
                    newFuel1Pos = fuel1Pos;
                }
                else if (_fuelTankSelector1 == 0 || _fuelTankSelector1 == 20) newFuel1Pos = 0; // OFF
                else if (_fuelTankSelector1 == 2)                              newFuel1Pos = 1; // ON
                else if (_fuelTankSelector1 == 3)                              newFuel1Pos = 2; // CF
                else                                                            newFuel1Pos = fuel1Pos;

                if (IsBaronG58() && !_isMsfs2024 && DateTime.UtcNow < _fuelSel2IgnoreFollowUntil)
                {
                    newFuel2Pos = fuel2Pos;
                }
                else if (_fuelTankSelector2 == 2)                              newFuel2Pos = 0; // CF
                else if (_fuelTankSelector2 == 3)                              newFuel2Pos = 1; // ON
                else if (_fuelTankSelector2 == 0 || _fuelTankSelector2 == 20) newFuel2Pos = 2; // OFF
                else                                                            newFuel2Pos = fuel2Pos;
            }
            else if (IsCaravan() && DateTime.UtcNow < _fuelSel1IgnoreFollowUntil)
            {
                // Caravan (2020, confirmat 2026-08-26): just després d'un clic, la lectura real
                // "rebota" per un instant abans d'assentar-se (mateix patró que STARTER/IGNITION) —
                // ignorem la lectura durant una finestra curta, mantenint l'estat local del clic.
                newFuel1Pos = fuel1Pos;
                newFuel2Pos = fuel2Pos;
            }
            else
            {
                // SINGLE ENGINE — 1 selector, 2 knobs sincronitzats
                // MSFS: 0=OFF, 1=ALL/BOTH, 2=LEFT, 3=RIGHT, 16=BOTH
                switch (_fuelTankSelector1)
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

            // Rotatiu animat G3K_FSEL_SW (TBM930/G3000) — només l'objectiu canvia aquí; el pas de
            // l'angle es calcula al Paint, però qui el repinta és _g3kFuelSelAnimTimer (timer
            // dedicat, no un auto-repintat des de dins del mateix Paint — aquell patró es podia
            // trencar en silenci, veure comentari del camp).
            float fselTarget = _fuelTankSelector1 == 2 ? 245f : (_fuelTankSelector1 == 3 ? 113f : 0f);
            if (_g3kFuelSelAngle < 0f)
            {
                _g3kFuelSelAngle = fselTarget;
                _g3kFuelSelTargetAngle = fselTarget;
            }
            else if (fselTarget != _g3kFuelSelTargetAngle)
            {
                _g3kFuelSelTargetAngle = fselTarget;
                _g3kFuelSelLastPaint = DateTime.MinValue;
                _g3kFuelSelAnimTimer?.Start();
                needsRepaint = true;
            }

            if (needsRepaint && pbMain.IsHandleCreated && (DateTime.UtcNow - _lastRepaint).TotalMilliseconds > 200)
            {
                _lastRepaint = DateTime.UtcNow;
                pbMain.BeginInvoke(new Action(() => pbMain.Invalidate()));
            }
        }

        // Magnetos/starter del motor 1 (RECIP ENG LEFT/RIGHT MAGNETO:1, GENERAL ENG STARTER:1) — 3
        // lectures independents, recalculem ign1Pos i starter1Active junts cada cop que n'arriba una.
        private void UpdateIgnition1State()
        {
            bool newStarter1 = _ign1Starter != 0;
            if (newStarter1 != starter1Active)
            {
                starter1Active = newStarter1;
                pbMain.Invalidate();
            }
            int newIgn1Pos;
            if      (_ign1Left == 0 && _ign1Right == 0 && _ign1Starter == 0) newIgn1Pos = 0; // OFF
            else if (_ign1Left == 0 && _ign1Right == 1 && _ign1Starter == 0) newIgn1Pos = 1; // RIGHT
            else if (_ign1Left == 1 && _ign1Right == 0 && _ign1Starter == 0) newIgn1Pos = 2; // LEFT
            else if (_ign1Left == 1 && _ign1Right == 1 && _ign1Starter == 0) newIgn1Pos = 3; // BOTH
            else if (_ign1Left == 1 && _ign1Right == 1 && _ign1Starter == 1) newIgn1Pos = 4; // START
            else newIgn1Pos = ign1Pos; // mantenim l'últim valor conegut
            if (newIgn1Pos != ign1Pos) { ign1Pos = newIgn1Pos; pbMain.Invalidate(); }
        }

        // Idem motor 2.
        private void UpdateIgnition2State()
        {
            bool newStarter2 = _ign2Starter != 0;
            if (newStarter2 != starter2Active)
            {
                starter2Active = newStarter2;
                pbMain.Invalidate();
            }
            int newIgn2Pos;
            if      (_ign2Left == 0 && _ign2Right == 0 && _ign2Starter == 0) newIgn2Pos = 0;
            else if (_ign2Left == 0 && _ign2Right == 1 && _ign2Starter == 0) newIgn2Pos = 1;
            else if (_ign2Left == 1 && _ign2Right == 0 && _ign2Starter == 0) newIgn2Pos = 2;
            else if (_ign2Left == 1 && _ign2Right == 1 && _ign2Starter == 0) newIgn2Pos = 3;
            else if (_ign2Left == 1 && _ign2Right == 1 && _ign2Starter == 1) newIgn2Pos = 4;
            else newIgn2Pos = ign2Pos;
            if (newIgn2Pos != ign2Pos) { ign2Pos = newIgn2Pos; pbMain.Invalidate(); }
        }

        // Fuel pump 1/2 (GENERAL ENG FUEL PUMP SWITCH:1/2 a 2020, FUELSYSTEM PUMP SWITCH:1/2/3 a 2024)
        // — 5 lectures independents (2 de 2020, 3 de 2024, l'índex 3 compartit amb el selector C408),
        // recalculem fuelpump1Active/2Active junts cada cop que n'arriba una.
        private void UpdateFuelPumpState()
        {
            bool newFuelpump1, newFuelpump2;
            if (UsesPump1_3StateUi())
            {
                // Estat gestionat localment pels clics (StepFuelPumpBaron), no per lectura del simulador.
                newFuelpump1 = _fuelPump1State3 != 2; // != OFF
                newFuelpump2 = _fuelPump2State3 != 2;
            }
            else if (_isMsfs2024)
            {
                newFuelpump1 = IsDA40() ? (_readFuelpumpIdx3_2024 & 1) != 0 : (_readFuelpump1_2024 & 1) != 0;
                newFuelpump2 = (_readFuelpump2_2024 & 1) != 0;
            }
            else
            {
                newFuelpump1 = (_readFuelpump1 & 1) != 0;
                newFuelpump2 = (_readFuelpump2 & 1) != 0;
            }
            if (newFuelpump1 != fuelpump1Active)
            {
                fuelpump1Active = newFuelpump1;
                pbMain.Invalidate();
            }
            if (newFuelpump2 != fuelpump2Active)
            {
                fuelpump2Active = newFuelpump2;
                pbMain.Invalidate();
            }
            if (IsC408())
            {
                int newIdx3 = (_readFuelpumpIdx3_2024 & 1) != 0 ? 1 : 0;
                if (newIdx3 != _c408Idx3)
                {
                    Debug.WriteLine($"[C408 FUELSYSTEM PUMP SWITCH:3] {_c408Idx3} -> {newIdx3}");
                    _c408Idx3 = newIdx3;
                    UpdateC408SeSelectorFromPumpSwitches();
                }
            }
        }

        // Repaint retallat i llindat (2026-10-03, Pere) per als rotatius de clic (claus d'ignició,
        // selectors de combustible): en lloc de pbMain.Invalidate() sencer a cada clic, només la zona
        // de l'overlay que gira, i com a màxim un repaint cada 50ms — igual que la resta d'elements
        // arrossegables arreglats avui. overlayName és el nom de l'OverlayImage que es dibuixa rotat
        // (p.ex. "SW_IGNITION", "SW_FUEL1"); lastInvalidateUtc és el camp de llindar propi de cada switch.
        private void InvalidateRotarySwitch(string overlayName, ref DateTime lastInvalidateUtc)
        {
            if ((DateTime.UtcNow - lastInvalidateUtc).TotalMilliseconds < 50) return;
            lastInvalidateUtc = DateTime.UtcNow;
            var sw = overlayImages.FirstOrDefault(o => o.Name == overlayName);
            if (sw?.Img == null) { pbMain.Invalidate(); return; }
            float baseW = sw.ZoomWidth;
            float baseH = baseW / sw.AspectRatio;
            int margin = (int)Math.Max(baseW, baseH) / 2 + 10;
            var rect = new Rectangle((int)sw.BasePos.X - margin, (int)sw.BasePos.Y - margin, (int)baseW + margin * 2, (int)baseH + margin * 2);
            pbMain.Invalidate(ToRealInvalidateRect(rect));
        }

        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine("SimConnect connection established");

            _isMsfs2024 = data.dwApplicationVersionMajor >= 12;
            textBox21.Text = _isMsfs2024 ? "MSFS 2024" : "MSFS 2020";
            textBox21.ForeColor = Color.DodgerBlue;
            // Ens acabem de connectar: aturem reintents i forcem un "refresh" inicial.
            simReconnectPending = false;
            if (simReconnectTimer.Enabled) simReconnectTimer.Stop();

            // Restablim els flags perquè el primer paquet pinti immediatament
            

            // Els 40 switches ja no es tornen a subscriure aquí (period continu) — els demana
            // PollLowFrequencySimData() cada 200ms via _dataPollTimer. Cridem-lo ja un cop, ara
            // mateix, perquè la primera dada arribi de seguida en comptes d'esperar fins al primer
            // tick del timer.
            PollLowFrequencySimData();
            _dataPollTimer?.Start();

            // Flaps: sync puntual aquí (connexió feta / MSFS ja actiu), les dues per separat — no es
            // tornen a llegir fins que es mogui el selector (i, aleshores, només el handle index).
            SyncFlapsNumPosOnce();
            SyncFlapsHandleIndexOnce();

            // Trim: mateix sync puntual (elevator + rudder, independents). La lectura activa de
            // l'elevator (100ms) només s'engega si l'AP ja està actiu quan arriba la primera dada
            // (veure Simconnect_OnRecvSimobjectData, canvi d'_apMaster); la del rudder només amb drag.
            SyncElevatorTrimOnce();
            SyncRudderTrimOnce();

            // Throttle lever: sync puntual — el floor continu ara el porta el grup dels 124
            // (RequestThrottleLeverPos dins _lowFreqPolls), igual que la resta.
            SyncThrLeverPosOnce();

            // Llums: sync puntual de les 10.
            SyncAllLightsOnce();

            simconnect.RequestDataOnSimObject(
                REQUESTS.RequestAircraftTitle,
                DEFINITIONS.AircraftTitle,
                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                SIMCONNECT_PERIOD.SECOND,
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
            if (_aircraftTitleLbl != null) _aircraftTitleLbl.Text = "—";
            _currentAircraftTitle = ""; // força re-detecció (i re-sync fuel pumps Baron) a la propera connexió

            _dataPollTimer?.Stop();
            _flapsActiveReadTimer?.Stop();
            _elevatorTrimActiveReadTimer?.Stop();
            _rudderTrimActiveReadTimer?.Stop();
            _fuelTankSelectorActiveReadTimer?.Stop();
            _thrLeverActiveReadTimer?.Stop();

            simReconnectPending = true;
            simReconnectTimer.Start();

            try
            {
                System.IO.File.AppendAllText("panelAIR_App_crash.txt",
                "\r\n[SimConnect] Quit received -> will try reconnect...");
            }
            catch { }

            DisconnectFromSimConnect();
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == (uint)REQUESTS.RequestA320Baro)
            {
                var b = (A320BaroStruct)data.dwData[0];
                _a320Baro = b;   // només arriba quan canvia: es guarda per aplicar-lo si el panell A320 es crea després
                if (_xp == null || !_xp.A320Loaded) _a320Overhead?.SetBaro(b.powered > 0.5, (int)Math.Round(b.mode), b.value, b.isInhg > 0.5);
                return;
            }
            //Debug.WriteLine(">>> OnRecvSimobjectData CALLED");
            if (data.dwRequestID == (uint)REQUESTS.RequestAircraftTitle)
            {
                var info = (AircraftInfoStruct)data.dwData[0];
                string newTitle = info.title ?? "";
                bool aircraftChanged = newTitle != _currentAircraftTitle;
                bool wasBaron = IsBaronG58();
                bool wasCaravanScheme = IsCaravan() || IsCirrusSR22();
                bool wasBonanza = IsBonanzaG36();
                bool wasDA62 = IsDA62();
                bool wasC408 = IsC408();
                bool wasTBM930 = IsTBM930();
                bool wasA320 = IsA320FBW();
                _currentAircraftTitle = newTitle;
                if (_aircraftTitleLbl != null && _aircraftTitleLbl.Text != newTitle)
                    _aircraftTitleLbl.Text = newTitle;
                if (IsBaronG58() && !wasBaron)
                    ForceBaronFuelPumpsOff();
                else if ((IsCaravan() || IsCirrusSR22()) && !wasCaravanScheme)
                    ForceCaravanFuelPumpOff();
                else if (IsBonanzaG36() && !wasBonanza && _isMsfs2024)
                    ForceBonanzaFuelPumpOff();
                else if (IsDA62() && !wasDA62)
                    SyncDA62FuelValvesFromRpm();
                else if (IsC408() && !wasC408)
                    ForceC408FuelTransferOff();
                else if (IsTBM930() && !wasTBM930 && _isMsfs2024)
                    ForceTbmPulseLightOff();
                // Selecció automàtica panell TBM/G1000 segons l'avió detectat — el selector manual
                // (_g3000Btn) es manté intacte, però ara truquem SetG3000Active() directament (no
                // PerformClick — el botó viu a la pantalla AUX, sempre amagada, i CanSelect hi pot ser
                // false, fent que PerformClick no faci res).
                // A320 de FlyByWire: en carregar-lo s'activa sol l'overhead A320; en canviar a un altre avió
                // es deixa (i la lògica TBM/G1000 de sota tria el cockpit que toca). Va primer perquè la
                // condició TBM depèn de _a320Active.
                bool isA320Now = IsA320FBW();
                if (isA320Now != wasA320 && _a320Active != isA320Now && !calibrationMode)
                    SetA320Active(isA320Now);
                bool isTBM930Now = IsTBM930();
                if (isTBM930Now != wasTBM930 && _g3000Active != isTBM930Now && !calibrationMode && !_a320Active)
                    SetG3000Active(isTBM930Now);
                RefreshNoCompatibleAircraftMessage();
                // Canvi d'avió real (no només una altra lectura del mateix títol cada segon): re-sync
                // puntual de flaps (veure comentari a SyncFlapsNumPosOnce/SyncFlapsHandleIndexOnce).
                if (aircraftChanged) { SyncFlapsNumPosOnce(); SyncFlapsHandleIndexOnce(); SyncElevatorTrimOnce(); SyncRudderTrimOnce(); SyncThrLeverPosOnce(); SyncAllLightsOnce(); PollFuelTankSelectors(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFlapsNumPos)
            {
                var fnp = (FlapsNumPosStruct)data.dwData[0];
                if (flapsNumPositions != fnp.value)
                {
                    flapsNumPositions = fnp.value;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFlapsHandleIndex)
            {
                var fhi = (FlapsHandleIndexStruct)data.dwData[0];
                if (flapsPoscPalanca != fhi.value)
                {
                    flapsPoscPalanca = fhi.value;
                    // Hi ha hagut un canvi real: allarguem la finestra de lectura activa 2s més
                    // (debounce — mateixa regla que el throttle i el selector TBM).
                    _flapsActiveReadUntilUtc = DateTime.UtcNow.AddSeconds(2);
                    _flapsActiveReadTimer?.Start();
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestElevatorTrimPos)
            {
                var et = (ElevatorTrimStruct)data.dwData[0];
                if (elevatorTrim != et.value)
                {
                    double delta = et.value - elevatorTrim;
                    elevatorTrim = et.value;
                    // La roda només l'anima el dit (veure MouseMove) — aquesta lectura només arriba
                    // en sincronitzar, quan l'AP està actiu, o mentre s'arrossega la pròpia roda (és
                    // quan sí cal seguir el valor real).
                    if (!trimDragging && !_g3kTrimDragging)
                    {
                        float frameDelta = (float)(delta / 2.0) * TRIM_FRAME_COUNT * 35f;
                        trimFramePos -= frameDelta;
                        trimFramePos = ((trimFramePos % TRIM_FRAME_COUNT) + TRIM_FRAME_COUNT) % TRIM_FRAME_COUNT;
                    }
                    // La dada es llegeix tan sovint com cal, però el repintat es retalla a la zona
                    // de la roda i es limita de freqüència — un Invalidate() sencer sense límit a
                    // cada lectura (fins a 30+/s durant el drag/AP) costava ~12 FPS reals (confirmat
                    // per Pere, 2026-10-02; mateix problema que els flaps, mateixa solució). Llindar
                    // més ample quan només l'AP la mou (sense dit a seguir, i en turbulència pot durar
                    // minuts — a 50ms encara costava fins a 10 FPS volant de veritat).
                    double elevatorRepaintThrottleMs = (trimDragging || _g3kTrimDragging) ? 50 : 200; // prova 2026-10-03
                    if (pbMain.IsHandleCreated && (DateTime.UtcNow - _lastElevatorTrimInvalidate).TotalMilliseconds >= elevatorRepaintThrottleMs)
                    {
                        _lastElevatorTrimInvalidate = DateTime.UtcNow;
                        var rect = Rectangle.Union(Rectangle.Inflate(trimZone, 80, 80), Rectangle.Inflate(_g3kTrimZone, 80, 80));
                        pbMain.Invalidate(ToRealInvalidateRect(rect));
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestRudderTrimPos)
            {
                var rt = (RudderTrimStruct)data.dwData[0];
                double newRudderTrim = rt.value / 100.0;
                if (rudderTrim != newRudderTrim)
                {
                    double delta = newRudderTrim - rudderTrim;
                    rudderTrim = newRudderTrim;
                    if (!rudderTrimDragging && !_g3kRudderTrimDragging)
                    {
                        float frameDelta = (float)(delta / 2.0) * RUDDER_TRIM_FRAME_COUNT * 35f;
                        rudderTrimFramePos -= frameDelta;
                        rudderTrimFramePos = ((rudderTrimFramePos % RUDDER_TRIM_FRAME_COUNT) + RUDDER_TRIM_FRAME_COUNT) % RUDDER_TRIM_FRAME_COUNT;
                    }
                    // Mateix motiu que l'elevator trim, veure comentari allà. Prova 2026-10-03: 50->33.
                    if (pbMain.IsHandleCreated && (DateTime.UtcNow - _lastRudderTrimInvalidate).TotalMilliseconds >= 50)
                    {
                        _lastRudderTrimInvalidate = DateTime.UtcNow;
                        var rect = Rectangle.Union(Rectangle.Inflate(rudderTrimZone, 80, 80), Rectangle.Inflate(_g3kRudderTrimZone, 80, 80));
                        pbMain.Invalidate(ToRealInvalidateRect(rect));
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelTankSelector1)
            {
                var fts1 = (FuelTankSelector1Struct)data.dwData[0];
                if (_fuelTankSelector1 != fts1.value)
                {
                    _fuelTankSelector1 = fts1.value;
                    UpdateFuelSelectorState();
                    // Hi ha hagut un canvi real: allarguem la finestra de lectura activa 2s més
                    // (debounce — mateixa regla que el throttle), tant si l'hem clicat nosaltres com
                    // si l'ha girat sol el sistema automàtic de la TBM.
                    _fuelTankSelectorActiveReadUntilUtc = DateTime.UtcNow.AddSeconds(2);
                    _fuelTankSelectorActiveReadTimer?.Start();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelTankSelector2)
            {
                var fts2 = (FuelTankSelector2Struct)data.dwData[0];
                if (_fuelTankSelector2 != fts2.value)
                {
                    _fuelTankSelector2 = fts2.value;
                    UpdateFuelSelectorState();
                    _fuelTankSelectorActiveReadUntilUtc = DateTime.UtcNow.AddSeconds(2);
                    _fuelTankSelectorActiveReadTimer?.Start();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestThrottleLeverPos)
            {
                var tlp = (ThrottleLeverPosStruct)data.dwData[0];
                if (_thrLeverPos != tlp.value)
                {
                    _thrLeverPos = tlp.value;
                    // Hi ha hagut un canvi real: allarguem la finestra de lectura activa 2s més
                    // (debounce — mateixa regla que els flaps).
                    _thrLeverActiveReadUntilUtc = DateTime.UtcNow.AddSeconds(2);
                    _thrLeverActiveReadTimer?.Start();
                    UpdateThrHandleFromLeverPos(includeRightRailCheck: false);
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightBeacon)
            {
                bool newBeacon = (((LightBoolStruct)data.dwData[0]).value & 1) != 0;
                if (newBeacon != beaconActive) { beaconActive = newBeacon; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightLand)
            {
                bool newLand = (((LightBoolStruct)data.dwData[0]).value & 1) != 0;
                if (newLand != landActive) { landActive = newLand; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightTaxi)
            {
                bool newTaxi = (((LightBoolStruct)data.dwData[0]).value & 1) != 0;
                if (newTaxi != taxiActive) { taxiActive = newTaxi; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightNav)
            {
                bool newNav = (((LightBoolStruct)data.dwData[0]).value & 1) != 0;
                if (newNav != navActive) { navActive = newNav; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightStrobe)
            {
                bool newStrobe = (((LightBoolStruct)data.dwData[0]).value & 1) != 0;
                if (newStrobe != strobeActive) { strobeActive = newStrobe; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightRecogn)
            {
                bool newRecogn = (((LightBoolStruct)data.dwData[0]).value & 1) != 0;
                if (newRecogn != recognActive) { recognActive = newRecogn; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightPanel)
            {
                // Mateixa asimetria que abans (Baron 2020 invertida; C172 2020 exclosa, fa servir el
                // potenciòmetre RequestC172PanelLightPot) — veure comentari original més avall.
                int raw = ((LightBoolStruct)data.dwData[0]).value;
                bool newPanelLight = (IsC172() && !_isMsfs2024)
                    ? panelLightActive
                    : (IsBaronG58() && !_isMsfs2024)
                        ? (raw & 1) == 0
                        : (raw & 1) != 0;
                if (newPanelLight != panelLightActive) { panelLightActive = newPanelLight; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightLogo)
            {
                _lightLogo = ((LightBoolStruct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightingLogo1)
            {
                double newLogo1 = ((LightNumberStruct)data.dwData[0]).value;
                if (newLogo1 != _lightingLogo1) { _lightingLogo1 = newLogo1; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestLightingWing1)
            {
                _lightingWing1 = ((LightNumberStruct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestMasterBattery)
            {
                bool newMasterBatteryOff = ((LightBoolStruct)data.dwData[0]).value == 0;
                if (newMasterBatteryOff != masterBatteryOff)
                {
                    masterBatteryOff = newMasterBatteryOff;
                    pbMain.Invalidate();
                }
                return;
            }
            // DA-62/C172/Caravan (MSFS2024): exclosos — "AVIONICS MASTER SWITCH:1/2" no reflecteix el
            // seu sistema elèctric modular (LINE CONNECTION); cadascun té la seva pròpia lectura
            // dedicada (mateix bug recurrent que ja ha passat amb els selectors de combustible).
            if (data.dwRequestID == (uint)REQUESTS.RequestAvionics1)
            {
                if (!(IsDA62() && _isMsfs2024) && !(IsC172() && _isMsfs2024) && !(IsCaravan() && _isMsfs2024))
                {
                    bool newAvionics1Off = ((LightBoolStruct)data.dwData[0]).value == 0;
                    if (newAvionics1Off != avionics1Off)
                    {
                        avionics1Off = newAvionics1Off;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            // Cirrus SR22 / DA-40 (2020): l'avió només té UN interruptor d'avionics físic — exclosa la
            // lectura del 2 perquè no mogui l'interruptor dret, que aquí no existeix.
            if (data.dwRequestID == (uint)REQUESTS.RequestAvionics2)
            {
                if (!(IsC172() && _isMsfs2024) && !(IsCaravan() && _isMsfs2024) && !(IsCirrusSR22() && !_isMsfs2024) && !(IsDA40() && !_isMsfs2024))
                {
                    bool newAvionics2Off = ((LightBoolStruct)data.dwData[0]).value == 0;
                    if (newAvionics2Off != avionics2Off)
                    {
                        avionics2Off = newAvionics2Off;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestAltGen1)
            {
                bool newAltGen1Off = ((LightBoolStruct)data.dwData[0]).value == 0;
                if (newAltGen1Off != altGen1Off)
                {
                    altGen1Off = newAltGen1Off;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestAltGen2)
            {
                bool newAltGen2Off = ((LightBoolStruct)data.dwData[0]).value == 0;
                if (!(IsCaravan() && _isMsfs2024) && !IsDA40() && newAltGen2Off != altGen2Off)
                {
                    altGen2Off = newAltGen2Off;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestIgn1Left)
            {
                _ign1Left = ((LightBoolStruct)data.dwData[0]).value;
                UpdateIgnition1State();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestIgn1Right)
            {
                _ign1Right = ((LightBoolStruct)data.dwData[0]).value;
                UpdateIgnition1State();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestIgn1Starter)
            {
                _ign1Starter = ((LightBoolStruct)data.dwData[0]).value;
                UpdateIgnition1State();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestIgn2Left)
            {
                _ign2Left = ((LightBoolStruct)data.dwData[0]).value;
                UpdateIgnition2State();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestIgn2Right)
            {
                _ign2Right = ((LightBoolStruct)data.dwData[0]).value;
                UpdateIgnition2State();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestIgn2Starter)
            {
                _ign2Starter = ((LightBoolStruct)data.dwData[0]).value;
                UpdateIgnition2State();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelPump1)
            {
                _readFuelpump1 = ((LightBoolStruct)data.dwData[0]).value;
                UpdateFuelPumpState();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelPump2)
            {
                _readFuelpump2 = ((LightBoolStruct)data.dwData[0]).value;
                UpdateFuelPumpState();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelPump1_2024)
            {
                _readFuelpump1_2024 = ((LightBoolStruct)data.dwData[0]).value;
                UpdateFuelPumpState();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelPump2_2024)
            {
                _readFuelpump2_2024 = ((LightBoolStruct)data.dwData[0]).value;
                UpdateFuelPumpState();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelPumpIdx3_2024)
            {
                _readFuelpumpIdx3_2024 = ((LightBoolStruct)data.dwData[0]).value;
                UpdateFuelPumpState();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCom1)
            {
                bool newCom1Active = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newCom1Active != com1Active || newCom1Active != com1Tx)
                {
                    com1Active = newCom1Active;
                    com1Tx = newCom1Active;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCom2)
            {
                bool newCom2Active = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newCom2Active != com2Active || newCom2Active != com2Tx)
                {
                    com2Active = newCom2Active;
                    com2Tx = newCom2Active;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestComReceiveAll)
            {
                bool newComReceiveAll = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newComReceiveAll != comReceiveAll) { comReceiveAll = newComReceiveAll; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDmeSound)
            {
                bool newDmeActive = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newDmeActive != dmeActive) { dmeActive = newDmeActive; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestSpkrSound)
            {
                bool newSpkrActive = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newSpkrActive != spkrActive) { spkrActive = newSpkrActive; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestMkrMute)
            {
                bool newMkrMute = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newMkrMute != mkrMute) { mkrMute = newMkrMute; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestNav1Sound)
            {
                bool newNav1Active = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newNav1Active != nav1Active) { nav1Active = newNav1Active; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestAdfSound)
            {
                bool newAdfActive = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newAdfActive != adfMute) { adfMute = newAdfActive; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestNav2Sound)
            {
                bool newNav2Active = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newNav2Active != nav2Active) { nav2Active = newNav2Active; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApVsHold)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apVsHold) { _apVsHold = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApFlcActive)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apFlcActive) { _apFlcActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApWingLeveler)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apWingLeveler) { _apWingLeveler = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApHdgLock)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apHdgLock) { _apHdgLock = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApAprHold)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apAprHold) { _apAprHold = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApGsHold)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apGsHold) { _apGsHold = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApNav1Lock)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apNav1Lock) { _apNav1Lock = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApFdActive)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apFdActive) { _apFdActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApAltLock)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apAltLock) { _apAltLock = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApVnavBtn)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apVnavBtn) { _apVnavBtn = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApYawDamper)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apYawDamper) { _apYawDamper = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApBcHold)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apBcHold) { _apBcHold = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestXfrPushL)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _xfrPushL) { _xfrPushL = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestMasterCautionActive)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _masterCautionActive) { _masterCautionActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestMasterWarningActive)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _masterWarningActive) { _masterWarningActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestElecStarter)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (newVal != _elecStarter) { _elecStarter = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestElecIgnition)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (newVal != _elecIgnition) { _elecIgnition = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApDisengaged)
            {
                _apDisengaged = ((LightBoolStruct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestRudderTrimDisabled)
            {
                _rudderTrimDisabled = ((LightBoolStruct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestAileronTrimDisabled)
            {
                _aileronTrimDisabled = ((LightBoolStruct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestPropDeice)
            {
                bool newPropDeice = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newPropDeice != propDeiceActive) { propDeiceActive = newPropDeice; pbMain.Invalidate(); }
                // Bonanza G36 a MSFS2024: BTN_DEICE/SW_DN_DEICE (deiceActive) fan servir el switch real
                // de prop de-ice — veure comentari original a Simconnect_OnRecvSimobjectData (abans
                // d'independitzar-lo).
                if (IsBonanzaG36() && _isMsfs2024 && newPropDeice != deiceActive)
                {
                    deiceActive = newPropDeice;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestWindshieldDeice)
            {
                bool newVal = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newVal != windshieldDeiceActive) { windshieldDeiceActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestPitot1)
            {
                bool newVal = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newVal != pitotHeatLActive) { pitotHeatLActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestPitot2)
            {
                bool newVal = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newVal != pitotHeatRActive) { pitotHeatRActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestAntiIce1)
            {
                bool newVal = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (!(IsTBM930() && _isMsfs2024) && newVal != antiIceEng1Active) { antiIceEng1Active = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestGearCenter)
            {
                int newVal = (int)((LightNumberStruct)data.dwData[0]).value;
                if (newVal != gearNosePercent) { gearNosePercent = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestGearLeft)
            {
                int newVal = (int)((LightNumberStruct)data.dwData[0]).value;
                if (newVal != gearLeftPercent) { gearLeftPercent = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestGearRight)
            {
                int newVal = (int)((LightNumberStruct)data.dwData[0]).value;
                if (newVal != gearRightPercent) { gearRightPercent = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestGearHandle)
            {
                bool newGearHandleUp = (((LightBoolStruct)data.dwData[0]).value & 1) == 0; // 0=UP, 1=DN
                if (newGearHandleUp != lgearHandleUp) { lgearHandleUp = newGearHandleUp; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDisplayBackupActive)
            {
                bool newVal = ((LightNumberStruct)data.dwData[0]).value > 0.5;
                if (newVal != displayBackupActive_bool) { displayBackupActive_bool = newVal; pbMain.Invalidate(); }
                return;
            }
            // TBM930 (2024): igual que el crashbar, _elecSourceSwitch el porta SetElecSourceSwitch()
            // fins que tinguem la L: real de lectura.
            if (data.dwRequestID == (uint)REQUESTS.RequestElecSourceSwitch)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (!(IsTBM930() && _isMsfs2024) && newVal != _elecSourceSwitch) { _elecSourceSwitch = newVal; pbMain.Invalidate(); }
                EnforceElecCrashbarConsistency();
                return;
            }
            // TBM930 (2024): _elecGeneratorSwitch el porta RequestTbmElecGenerator2024 (L: real).
            if (data.dwRequestID == (uint)REQUESTS.RequestElecGeneratorSwitch)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (!(IsTBM930() && _isMsfs2024) && newVal != _elecGeneratorSwitch) { _elecGeneratorSwitch = newVal; pbMain.Invalidate(); }
                EnforceElecCrashbarConsistency();
                return;
            }
            // TBM930 (2024): no llegim L:XMLVAR_Elec_CrashBar_State (pot no aplicar-se), _elecCrashbar
            // el porta el toggle local de ToggleElecCrashbar().
            if (data.dwRequestID == (uint)REQUESTS.RequestElecCrashbarState)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (!(IsTBM930() && _isMsfs2024) && newVal != _elecCrashbar) { _elecCrashbar = newVal; pbMain.Invalidate(); }
                EnforceElecCrashbarConsistency();
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestKap140State)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _kap140State) { _kap140State = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApMaxBank)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (newVal != _apMaxBank) { _apMaxBank = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestGyro)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (newVal != _headingIndicator) { _headingIndicator = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDeice)
            {
                bool newVal = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (!IsCirrusSR22() && !IsBonanzaG36() && !IsCaravan() && newVal != deiceActive) { deiceActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestPitotHeatSwitch)
            {
                bool newVal = ((LightBoolStruct)data.dwData[0]).value != 0;
                if (newVal != pitotheatActive) { pitotheatActive = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestNumEngines)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != numEngines) { numEngines = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCowlFlapL)
            {
                double raw = ((LightNumberStruct)data.dwData[0]).value;
                int newVal = raw < 10 ? 0 : raw < 70 ? 1 : 2;
                if (newVal != cowlFlapL) { cowlFlapL = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCowlFlapR)
            {
                double raw = ((LightNumberStruct)data.dwData[0]).value;
                int newVal = raw < 10 ? 0 : raw < 70 ? 1 : 2;
                if (newVal != cowlFlapR) { cowlFlapR = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCameraState)
            {
                _cameraState = ((LightNumberStruct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestElecFuelPump)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (newVal != _elecFuelPump) { _elecFuelPump = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestPropLeverPos)
            {
                _propLeverPosCached = ((LightNumberStruct)data.dwData[0]).value;
                UpdateThrHandleFromLeverPos(includeRightRailCheck: true);
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelValve)
            {
                _fuelValveCached = ((LightBoolStruct)data.dwData[0]).value;
                UpdateThrHandleFromLeverPos(includeRightRailCheck: true);
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestApMaster)
            {
                int newVal = ((LightBoolStruct)data.dwData[0]).value;
                if (newVal != _apMaster)
                {
                    _apMaster = newVal;
                    // L'elevator/vertical trim és l'únic motoritzat per l'AP (el rudder, mai) — mentre
                    // estigui actiu, mantenim una lectura activa contínua a 33ms; en desactivar-se,
                    // parem (tret que s'estigui arrossegant la roda en aquell moment).
                    if (_apMaster == 1) StartElevatorTrimActiveRead();
                    else StopElevatorTrimActiveReadIfNotNeeded();
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestAileronTrimFrac)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (newVal != _aileronTrimFrac) { _aileronTrimFrac = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestIsDeiceAirframe)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (!(IsTBM930() && _isMsfs2024) && newVal != _isDeiceAirframe) { _isDeiceAirframe = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestIsDeiceAirframe2)
            {
                double newVal = ((LightNumberStruct)data.dwData[0]).value;
                if (!(IsTBM930() && _isMsfs2024) && newVal != _isDeiceAirframe2) { _isDeiceAirframe2 = newVal; pbMain.Invalidate(); }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestParkingBrake)
            {
                var pb = (ParkingBrakeStruct)data.dwData[0];
                bool newParkingbrake = (pb.value & 1) != 0;
                if (newParkingbrake != pbrakeActive)
                {
                    pbrakeActive = newParkingbrake;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestBatteryVoltage)
            {
                var bv = (BatteryVoltageStruct)data.dwData[0];
                if (Math.Abs(bv.value - mainBusVolts) > 0.05)
                {
                    mainBusVolts = bv.value;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelSelLeft)
            {
                var fsl = (FuelSelLeftStruct)data.dwData[0];
                if (Math.Abs(fsl.value - _lastFuelSelLeftDebugValue) > 0.5)
                {
                    _lastFuelSelLeftDebugValue = fsl.value;
                    System.Diagnostics.Debug.WriteLine($"[FUELSEL DEBUG] L:FUEL_Switch_Cutoff_Crossfeed_1 = {fsl.value}");
                }
                // Confirmat: la DA-62 NO fa servir L:FUEL_Switch_Cutoff_Crossfeed_1 (fa servir
                // O:XMLVAR_FUEL_VALVE_LEFT_Value) — exclosa, es queda amb la posició local optimista.
                // La C408 tampoc: és un altre selector (FUEL_TRANSFER), aquesta L:Var no li correspon.
                // 2026-08-02: la Caravan a MSFS2024 tampoc — fa servir FUELSYSTEM VALVE SWITCH:1/4
                // (lectura dedicada més amunt); aquesta L:Var hi competia i causava l'oscil·lació infinita
                // de fuel1Pos/fuel2Pos (el mateix bug recurrent d'excloure un avió nou d'una lectura
                // genèrica — veure memòria).
                // 2026-08-26: confirmat que la Caravan a MSFS2020 TAMPOC la fa servir — el mecanisme real
                // és A:FUEL TANK SELECTOR:1 via el bloc "SINGLE ENGINE" de sota (mateix K:FUEL_SELECTOR_SET
                // ja usat per escriure). Aquesta L:Var hi competia igual que a 2024 i causava els mateixos
                // rebots — exclosa la Caravan del tot, independent del sim.
                // Baron G58 (2020 NOMÉS, confirmat 2026-08-26): exclòs igual que la Caravan — el bloc
                // TWIN ENGINE (s1.fuel_selector_1/_2) de Simconnect_OnRecvSimobjectData ja és la lectura
                // correcta i confirmada; aquesta L:Var hi competia i causava rebots. A 2024 es queda
                // (és la que ja fa servir el mecanisme dedicat existent, no la toquem).
                if (!IsDA62() && !IsC408() && !IsCaravan() && !(IsBaronG58() && !_isMsfs2024))
                {
                    int newFuel1PosFromRead = fsl.value < 0.5 ? 0 : (fsl.value < 1.5 ? 1 : 2);
                    if (newFuel1PosFromRead != fuel1Pos && DateTime.UtcNow >= _fuelSel1IgnoreFollowUntil)
                    {
                        fuel1Pos = newFuel1PosFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestFuelSelRight)
            {
                var fsr = (FuelSelRightStruct)data.dwData[0];
                // Baron G58 (2020 NOMÉS, confirmat 2026-08-26): exclòs igual que la Caravan — el bloc
                // TWIN ENGINE (s1.fuel_selector_1/_2) de Simconnect_OnRecvSimobjectData ja és la lectura
                // correcta i confirmada; aquesta L:Var hi competia i causava rebots. A 2024 es queda
                // (és la que ja fa servir el mecanisme dedicat existent, no la toquem).
                if (!IsDA62() && !IsC408() && !IsCaravan() && !(IsBaronG58() && !_isMsfs2024))
                {
                    // Aquesta variable ja ve en l'ordre físic del nostre selector (0=Xfeed, 1=On, 2=Off) — sense invertir.
                    int newFuel2PosFromRead = fsr.value < 0.5 ? 0 : (fsr.value < 1.5 ? 1 : 2);
                    if (newFuel2PosFromRead != fuel2Pos && DateTime.UtcNow >= _fuelSel2IgnoreFollowUntil)
                    {
                        fuel2Pos = newFuel2PosFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestPropRpm1)
            {
                _propRpm1 = ((PropRpm1Struct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestPropRpm2)
            {
                _propRpm2 = ((PropRpm2Struct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA40FuelTankSelector)
            {
                double newVal = ((DA40FuelTankSelectorStruct)data.dwData[0]).value;
                if (Math.Abs(newVal - _daFuelTankSelector) > 0.5)
                {
                    Debug.WriteLine($"[DA40 FUELSYSTEM VALVE SWITCH:1] {_daFuelTankSelector} -> {newVal}");
                    _daFuelTankSelector = newVal;
                }
                if (IsCaravan() && _isMsfs2024)
                {
                    // Feedback real (confirmat 2026-08-02, MSFS2024 només): fuel1Pos (LEFT) el determina
                    // només aquesta lectura, mai el click local. Caravan: booleà pur, 0=OFF, 1=ON (sense
                    // crossfeed). MSFS2020: cau a la lectura genèrica FUEL_Switch_Cutoff_Crossfeed_1 de més
                    // amunt (mecanisme original).
                    int newFuel1PosFromRead = newVal < 0.5 ? 0 : 1;
                    if (newFuel1PosFromRead != fuel1Pos)
                    {
                        Debug.WriteLine($"[CARAVAN fuel1Pos] {fuel1Pos} -> {newFuel1PosFromRead}");
                        fuel1Pos = newFuel1PosFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA40FuelTankSelector2)
            {
                double newVal = ((DA40FuelTankSelector2Struct)data.dwData[0]).value;
                if (Math.Abs(newVal - _daFuelTankSelector2) > 0.5)
                {
                    Debug.WriteLine($"[DA40 FUELSYSTEM VALVE SWITCH:2] {_daFuelTankSelector2} -> {newVal}");
                    _daFuelTankSelector2 = newVal;
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA40FuelTankSelector3)
            {
                double newVal = ((DA40FuelTankSelector3Struct)data.dwData[0]).value;
                if (Math.Abs(newVal - _daFuelTankSelector3) > 0.5)
                {
                    Debug.WriteLine($"[DA40 FUELSYSTEM VALVE SWITCH:3] {_daFuelTankSelector3} -> {newVal}");
                    _daFuelTankSelector3 = newVal;
                }
                // 2026-08-02: confirmat que SWITCH:3 NO és la vàlvula dreta de la Caravan (movent-la a mà
                // al VC, l'app no reacciona) — provem SWITCH:4 (handler de sota) en lloc d'aquest.
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA40FuelTankSelector4)
            {
                double newVal = ((DA40FuelTankSelector4Struct)data.dwData[0]).value;
                if (Math.Abs(newVal - _daFuelTankSelector4) > 0.5)
                {
                    Debug.WriteLine($"[DA40 FUELSYSTEM VALVE SWITCH:4] {_daFuelTankSelector4} -> {newVal}");
                    _daFuelTankSelector4 = newVal;
                }
                if (IsCaravan() && _isMsfs2024)
                {
                    // Hipòtesi 2026-08-02 (MSFS2024 només): fuel2Pos (RIGHT) ve de SWITCH:4, no SWITCH:3
                    // (confirmar a VC: mou el switch dret a mà i mira si l'app el segueix i si aquest log
                    // canvia). MSFS2020: cau a la lectura genèrica de més amunt (mecanisme original).
                    int newFuel2PosFromRead = newVal < 0.5 ? 0 : 1;
                    if (newFuel2PosFromRead != fuel2Pos)
                    {
                        Debug.WriteLine($"[CARAVAN fuel2Pos] {fuel2Pos} -> {newFuel2PosFromRead}");
                        fuel2Pos = newFuel2PosFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestC400JunctionSetting)
            {
                double newVal = ((C400JunctionSettingStruct)data.dwData[0]).value;
                if (newVal != _c400JunctionSetting)
                {
                    Debug.WriteLine($"[C400 FUELSYSTEM JUNCTION SETTING:1] {_c400JunctionSetting} -> {newVal}");
                    _c400JunctionSetting = newVal;
                }
                // Sincronització real: 1=Off,2=Left,3=Right,4=Off (el selector físic té 2 Off,
                // un a cada extrem del recorregut horari Off-Left-Right-Off) -> seSelectorPos 0/1/2.
                if (IsC400())
                {
                    int rounded = (int)Math.Round(newVal);
                    int newPosFromRead = rounded switch { 2 => 1, 3 => 2, _ => 0 }; // 1 i 4 (i qualsevol altre) = Off
                    if (newPosFromRead != seSelectorPos && DateTime.UtcNow >= _seSelectorIgnoreFollowUntil)
                    {
                        seSelectorPos = newPosFromRead;
                        pbMain.Invalidate();
                    }
                }
                // C172 (mateix junction=1): 1=Left,2=Both,3=Right, sense Off -> seSelectorPos 0/1/2.
                // NOMÉS 2024 — el Skyhawk per defecte de 2020 no fa servir el sistema de junction, té la
                // seva pròpia lectura clàssica més avall (bloc "SINGLE ENGINE" amb s1.fuel_selector_1).
                else if (IsC172() && _isMsfs2024)
                {
                    int rounded = (int)Math.Round(newVal);
                    int newPosFromRead = rounded switch { 1 => 0, 3 => 2, _ => 1 }; // 2 (i qualsevol altre) = Both
                    if (newPosFromRead != seSelectorPos && DateTime.UtcNow >= _seSelectorIgnoreFollowUntil)
                    {
                        seSelectorPos = newPosFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA62AvionicsLineConn)
            {
                double newVal = ((DA62AvionicsLineConnStruct)data.dwData[0]).value;
                if (newVal != _da62AvionicsLineConn)
                {
                    Debug.WriteLine($"[DA62 LINE CONNECTION ON:rh_main_bustoavionics_bus] {_da62AvionicsLineConn} -> {newVal}");
                    _da62AvionicsLineConn = newVal;
                }
                if (IsDA62() && _isMsfs2024)
                {
                    bool newAvionics1OffFromRead = newVal < 0.5;
                    if (newAvionics1OffFromRead != avionics1Off)
                    {
                        avionics1Off = newAvionics1OffFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestC172Avionics1LineConn)
            {
                double newVal = ((C172Avionics1LineConnStruct)data.dwData[0]).value;
                if (newVal != _c172Avionics1LineConn)
                {
                    Debug.WriteLine($"[C172 LINE CONNECTION ON:BUS_1_To_AVIONICS_BUS_1] {_c172Avionics1LineConn} -> {newVal}");
                    _c172Avionics1LineConn = newVal;
                }
                if (IsC172() && _isMsfs2024)
                {
                    bool newAvionics1OffFromRead = newVal < 0.5;
                    if (newAvionics1OffFromRead != avionics1Off)
                    {
                        avionics1Off = newAvionics1OffFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestC172Avionics2LineConn)
            {
                double newVal = ((C172Avionics2LineConnStruct)data.dwData[0]).value;
                if (newVal != _c172Avionics2LineConn)
                {
                    Debug.WriteLine($"[C172 LINE CONNECTION ON:BUS_2_To_AVIONICS_BUS_2] {_c172Avionics2LineConn} -> {newVal}");
                    _c172Avionics2LineConn = newVal;
                }
                if (IsC172() && _isMsfs2024)
                {
                    bool newAvionics2OffFromRead = newVal < 0.5;
                    if (newAvionics2OffFromRead != avionics2Off)
                    {
                        avionics2Off = newAvionics2OffFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestC172PanelLightPot)
            {
                double newVal = ((C172PanelLightPotStruct)data.dwData[0]).value;
                if (IsC172() && !_isMsfs2024)
                {
                    bool newPanelLightFromRead = newVal >= 50.0;
                    if (newPanelLightFromRead != panelLightActive)
                    {
                        panelLightActive = newPanelLightFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCaravanAvionics1LineConn)
            {
                double newVal = ((CaravanAvionics1LineConnStruct)data.dwData[0]).value;
                if (newVal != _caravanAvionics1LineConn)
                {
                    Debug.WriteLine($"[CARAVAN LINE CONNECTION ON:AVIONICS_BUS_1_To_PWR_BUS_DISTRIB] {_caravanAvionics1LineConn} -> {newVal}");
                    _caravanAvionics1LineConn = newVal;
                }
                if (IsCaravan() && _isMsfs2024)
                {
                    bool newAvionics1OffFromRead = newVal < 0.5;
                    if (newAvionics1OffFromRead != avionics1Off)
                    {
                        avionics1Off = newAvionics1OffFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCaravanAvionics2LineConn)
            {
                double newVal = ((CaravanAvionics2LineConnStruct)data.dwData[0]).value;
                if (newVal != _caravanAvionics2LineConn)
                {
                    Debug.WriteLine($"[CARAVAN LINE CONNECTION ON:AVIONICS_BUS_2_To_PWR_BUS_DISTRIB] {_caravanAvionics2LineConn} -> {newVal}");
                    _caravanAvionics2LineConn = newVal;
                }
                if (IsCaravan() && _isMsfs2024)
                {
                    bool newAvionics2OffFromRead = newVal < 0.5;
                    if (newAvionics2OffFromRead != avionics2Off)
                    {
                        avionics2Off = newAvionics2OffFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCaravanAltGen2LineConn)
            {
                double newVal = ((CaravanAltGen2LineConnStruct)data.dwData[0]).value;
                if (newVal != _caravanAltGen2LineConn)
                {
                    Debug.WriteLine($"[CARAVAN LINE CONNECTION ON:BATT_BUS_To_ALT_BUS] {_caravanAltGen2LineConn} -> {newVal}");
                    _caravanAltGen2LineConn = newVal;
                }
                if (IsCaravan() && _isMsfs2024)
                {
                    bool newAltGen2OffFromRead = newVal < 0.5;
                    if (newAltGen2OffFromRead != altGen2Off)
                    {
                        altGen2Off = newAltGen2OffFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA62EngineMaster1)
            {
                double newVal = ((DA62EngineMaster1Struct)data.dwData[0]).value;
                if (newVal != _da62EngineMaster1)
                {
                    Debug.WriteLine($"[DA62 RECIP ENG ENGINE MASTER SWITCH:1] {_da62EngineMaster1} -> {newVal}");
                    _da62EngineMaster1 = newVal;
                }
                bool newMaster1On = newVal >= 0.5;
                if (newMaster1On != da62EngineMaster1On)
                {
                    da62EngineMaster1On = newMaster1On;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA62EngineMaster2)
            {
                double newVal = ((DA62EngineMaster2Struct)data.dwData[0]).value;
                if (newVal != _da62EngineMaster2)
                {
                    Debug.WriteLine($"[DA62 RECIP ENG ENGINE MASTER SWITCH:2] {_da62EngineMaster2} -> {newVal}");
                    _da62EngineMaster2 = newVal;
                }
                bool newMaster2On = newVal >= 0.5;
                if (newMaster2On != da62EngineMaster2On)
                {
                    da62EngineMaster2On = newMaster2On;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA40EssentialBus2)
            {
                double newVal = ((DA40EssentialBus2Struct)data.dwData[0]).value;
                if (newVal != _da40EssentialBus2)
                {
                    Debug.WriteLine($"[DA40 L:ELECTRICAL_EssentialBus_2] {_da40EssentialBus2} -> {newVal}");
                    _da40EssentialBus2 = newVal;
                }
                if (IsDA40())
                {
                    bool newAltGen2OffFromRead = newVal < 0.5;
                    if (newAltGen2OffFromRead != altGen2Off)
                    {
                        altGen2Off = newAltGen2OffFromRead;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA40BusConnOn3)
            {
                _da40BusConnOn3 = ((DA40BusConnOn3Struct)data.dwData[0]).value;
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA40CircuitSwitch7)
            {
                double newVal = ((DA40CircuitSwitch7Struct)data.dwData[0]).value;
                if (newVal != _da40CircuitSwitchOn7)
                {
                    Debug.WriteLine($"[DA40 A:CIRCUIT SWITCH ON:7] {_da40CircuitSwitchOn7} -> {newVal}");
                    _da40CircuitSwitchOn7 = newVal;
                }
                if (IsDA40() && !_isMsfs2024)
                {
                    bool newFuelTransferOn = newVal >= 0.5;
                    if (newFuelTransferOn != _da40FuelTransferOn)
                    {
                        _da40FuelTransferOn = newFuelTransferOn;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestDA40EngineMaster1)
            {
                double newVal = ((DA40EngineMaster1Struct)data.dwData[0]).value;
                if (newVal != _da40EngineMaster1)
                {
                    Debug.WriteLine($"[DA40 RECIP ENG ENGINE MASTER SWITCH:1] {_da40EngineMaster1} -> {newVal}");
                    _da40EngineMaster1 = newVal;
                }
                bool newDa40Master1On = newVal >= 0.5;
                if (newDa40Master1On != da40EngineMaster1On)
                {
                    da40EngineMaster1On = newDa40Master1On;
                    pbMain.Invalidate();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCirrusDeiceMaster)
            {
                double newVal = ((CirrusDeiceMasterStruct)data.dwData[0]).value;
                if (newVal != _cirrusDeiceMaster)
                {
                    Debug.WriteLine($"[CIRRUS L:XMLVAR_DEICE_MASTER] {_cirrusDeiceMaster} -> {newVal}");
                    _cirrusDeiceMaster = newVal;
                }
                if (IsCirrusSR22() || (IsCaravan() && _isMsfs2024))
                {
                    bool newDeiceOn = newVal >= 0.5;
                    if (newDeiceOn != deiceActive)
                    {
                        deiceActive = newDeiceOn;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestTbmCrashbar2024)
            {
                // Guarda afegida (2026-09-02): faltava aquí, feia que aquest handler sobreescrivís
                // _elecCrashbar sempre (cada frame, independent d'avió/versió), competint amb la lectura
                // genèrica de la línia ~7934 i provocant rebots quan es volava amb la TBM 2020.
                if (IsTBM930() && _isMsfs2024)
                {
                    double newVal = ((TbmCrashbar2024Struct)data.dwData[0]).value >= 0.5 ? 1.0 : 0.0;
                    if (newVal != _elecCrashbar)
                    {
                        _elecCrashbar = newVal;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            // Guardes IsTBM930() && _isMsfs2024 afegides a tots els handlers TBM 2024 d'aquí avall
            // (2026-09-02) — el mateix oblit que al crashbar: la petició es registra sense condició
            // d'avió/versió a Simconnect_OnRecvOpen, així que sense guarda aquí sobreescrivien la
            // variable compartida sempre, competint amb la lectura genèrica de 2020 (rebots a Source,
            // Generator, De-ice Cycle 1/2 i Inert Sep confirmats per Pere; Fuel Selection i AP Trim no
            // tenen font competidora coneguda per 2020, però es corregeixen igual per consistència).
            if (data.dwRequestID == (uint)REQUESTS.RequestTbmElecSource2024)
            {
                if (IsTBM930() && _isMsfs2024)
                {
                    double newVal = Math.Round(((TbmElecSource2024Struct)data.dwData[0]).value);
                    if (newVal != _elecSourceSwitch)
                    {
                        _elecSourceSwitch = newVal;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestTbmElecGenerator2024)
            {
                if (IsTBM930() && _isMsfs2024)
                {
                    double newVal = Math.Round(((TbmElecGenerator2024Struct)data.dwData[0]).value);
                    if (newVal != _elecGeneratorSwitch)
                    {
                        _elecGeneratorSwitch = newVal;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestTbmFuelSelection2024)
            {
                if (IsTBM930() && _isMsfs2024)
                {
                    double newVal = ((TbmFuelSelection2024Struct)data.dwData[0]).value >= 0.5 ? 2.0 : 1.0;
                    if (newVal != _fuelSelectionSet)
                    {
                        _fuelSelectionSet = newVal;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestTbmApTrim2024)
            {
                if (IsTBM930() && _isMsfs2024)
                {
                    double newVal = Math.Round(((TbmApTrim2024Struct)data.dwData[0]).value);
                    if (newVal != _apTrim)
                    {
                        _apTrim = newVal;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestTbmDeiceCycle1_2024)
            {
                if (IsTBM930() && _isMsfs2024)
                {
                    double newVal = ((TbmDeiceCycle1_2024Struct)data.dwData[0]).value >= 0.5 ? 1.0 : 0.0;
                    if (newVal != _isDeiceAirframe)
                    {
                        _isDeiceAirframe = newVal;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestTbmDeiceCycle2_2024)
            {
                if (IsTBM930() && _isMsfs2024)
                {
                    double newVal = ((TbmDeiceCycle2_2024Struct)data.dwData[0]).value >= 0.5 ? 1.0 : 0.0;
                    if (newVal != _isDeiceAirframe2)
                    {
                        _isDeiceAirframe2 = newVal;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestTbmInertSep2024)
            {
                if (IsTBM930() && _isMsfs2024)
                {
                    bool newVal = ((TbmInertSep2024Struct)data.dwData[0]).value >= 0.5;
                    if (newVal != antiIceEng1Active)
                    {
                        antiIceEng1Active = newVal;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestC408FuelPump4)
            {
                double newVal = ((C408FuelPump4Struct)data.dwData[0]).value;
                int newIdx4 = newVal >= 0.5 ? 1 : 0;
                if (newIdx4 != _c408Idx4)
                {
                    Debug.WriteLine($"[C408 FUELSYSTEM PUMP SWITCH:4] {_c408Idx4} -> {newIdx4}");
                    _c408Idx4 = newIdx4;
                    UpdateC408SeSelectorFromPumpSwitches();
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestC408Ignition1)
            {
                double newVal = ((C408Ignition1Struct)data.dwData[0]).value;
                if (IsC408())
                {
                    bool newIgnition1On = newVal >= 0.5;
                    if (newIgnition1On != ignition1Active)
                    {
                        ignition1Active = newIgnition1On;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestC408Ignition2)
            {
                double newVal = ((C408Ignition2Struct)data.dwData[0]).value;
                if (IsC408())
                {
                    bool newIgnition2On = newVal >= 0.5;
                    if (newIgnition2On != ignition2Active)
                    {
                        ignition2Active = newIgnition2On;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCaravanIgnition1)
            {
                double newVal = ((CaravanIgnition1Struct)data.dwData[0]).value;
                if (IsCaravan())
                {
                    bool newIgnitionOn = newVal >= 1.5; // A:TURB ENG IGNITION SWITCH EX1:1: 0/1=OFF·NORM, 2=ON (OVERRIDE) — mateixa convenció que el target del Set
                    if (newIgnitionOn != ignition1Active)
                    {
                        ignition1Active = newIgnitionOn;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCaravanStarter1)
            {
                double newVal = ((CaravanStarter1Struct)data.dwData[0]).value;
                if (IsCaravan())
                {
                    int newPos = Math.Clamp((int)Math.Round(newVal), 0, 2);
                    // Ignorem el "rebot" que MSFS provoca a L:XMLVAR_Starter1 quan IGNITION escriu la
                    // mateixa A:TURB ENG IGNITION SWITCH EX1:1 — Pere ha confirmat al VC real que STARTER
                    // mai es mou visualment per culpa d'IGNITION (relació purament elèctrica).
                    if (newPos != _caravanStarter1Pos && DateTime.UtcNow >= _caravanStarter1IgnoreFollowUntil)
                    {
                        _caravanStarter1Pos = newPos;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestCaravanDeice)
            {
                double newVal = ((CaravanDeiceStruct)data.dwData[0]).value;
                if (IsCaravan() && !_isMsfs2024)
                {
                    // 0=HIGH,1=NORM -> ON (deiceActive=true); 2=OFF -> false.
                    bool newDeiceOn = newVal < 1.5;
                    if (newDeiceOn != deiceActive)
                    {
                        deiceActive = newDeiceOn;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
            if (data.dwRequestID == (uint)REQUESTS.RequestBonanzaAntiIcePos)
            {
                double newVal = ((BonanzaAntiIcePosStruct)data.dwData[0]).value;
                if (IsBonanzaG36() && !_isMsfs2024)
                {
                    bool newDeiceOn = newVal >= 0.5; // progressiu — "actiu" quan la posició supera la meitat
                    if (newDeiceOn != deiceActive)
                    {
                        deiceActive = newDeiceOn;
                        pbMain.Invalidate();
                    }
                }
                return;
            }
        }

        /// <summary>
        /// Cert si `bounds` queda majoritàriament amagat sota una tapa 3D activa per a l'avió actual
        /// (qualsevol overlay amb "COVER" al Name i ShowWhen() actualment cert). Fet servir per "show
        /// all areas": una zona de clic que existeix però està tapada visualment mai s'ha de ressaltar.
        /// </summary>
        private bool IsHiddenByActiveCover(Rectangle bounds)
        {
            bool CoveredIn(List<OverlayImage> list)
            {
                foreach (var ov in list)
                {
                    if (!ov.Name.Contains("COVER")) continue;
                    // SW_SE_SELECTOR_COVER (2026-09-05): excepció — no amaga cap control, només tapa
                    // l'art de fons vell (2 manetes fotografiades) perquè el selector únic nou
                    // (SW_SE_SELECTOR / SE_SELECTOR-LEFT / SE_SELECTOR-RIGHT) es dibuixi a sobre, ben
                    // actiu. Tractar-la com una tapa normal amagava per error aquests controls reals.
                    if (ov.Name == "SW_SE_SELECTOR_COVER") continue;
                    if (!(ov.ShowWhen?.Invoke() ?? false)) continue;
                    var overlap = Rectangle.Intersect(ov.Bounds, bounds);
                    if ((long)overlap.Width * overlap.Height * 2 >= (long)bounds.Width * bounds.Height)
                        return true;
                }
                return false;
            }
            return CoveredIn(overlayImages) || CoveredIn(g3000OverlayImages);
        }

        // Dibuixa una "tapa" opaca amb efecte de relleu (vora clara dalt/esquerra, fosca baix/dreta) a la
        // posició/mida de l'overlay indicat. Reutilitzat per totes les SW_*_COVER_RELIEF*.
        private void DrawReliefCover(Graphics g, string overlayName)
        {
            var cover = overlayImages.FirstOrDefault(o => o.Name == overlayName);
            if (cover == null || !(calibrationMode || (cover.ShowWhen?.Invoke() ?? false))) return;
            var rb = cover.Bounds;
            using var reliefBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
            g.FillRectangle(reliefBrush, rb);
            int bevel = 3;
            using var highlightPen = new Pen(Color.FromArgb(70, 70, 70), bevel);
            using var shadowPen = new Pen(Color.FromArgb(10, 10, 10), bevel);
            // Vora clara (dalt + esquerra)
            g.DrawLine(highlightPen, rb.Left, rb.Bottom - 1, rb.Left, rb.Top);
            g.DrawLine(highlightPen, rb.Left, rb.Top, rb.Right - 1, rb.Top);
            // Vora fosca (baix + dreta)
            g.DrawLine(shadowPen, rb.Right - 1, rb.Top, rb.Right - 1, rb.Bottom - 1);
            g.DrawLine(shadowPen, rb.Right - 1, rb.Bottom - 1, rb.Left, rb.Bottom - 1);
        }

        /// <summary>Icona "mostrar àrees clicables" — cridat des dels dos camins de PbMain_Paint (G1000 i G3000/TBM).</summary>
        private void DrawShowAreasIcon(Graphics g)
        {
            var showAreasRect = new Rectangle(_showAreasBtnPos, new Size(63, 63));
            if (mainSurface.ShowAllAreas)
            {
                using var showAreasFill = new SolidBrush(Color.FromArgb(140, Color.Orange));
                g.FillRectangle(showAreasFill, showAreasRect);
            }
            using (var showAreasBorder = new Pen(Color.Orange, 2))
                g.DrawRectangle(showAreasBorder, showAreasRect.X + 1, showAreasRect.Y + 1, showAreasRect.Width - 3, showAreasRect.Height - 3);
            using (var showAreasFont = new Font("Segoe UI Emoji", 34f, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var showAreasFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString("👆", showAreasFont, Brushes.Orange, showAreasRect, showAreasFmt);
        }

        // Mode demo: pbMain es dibuixa sempre pensant en un full virtual de 3840x2160 (igual que
        // amb hardware real); aquí calculem quin factor cal per encaixar-lo dins la mida real
        // (redimensionable) de la finestra, centrat (efecte "letterbox").
        private (float scale, float offX, float offY) GetDemoTransform()
        {
            if (!IsDemoMode || pbMain.Width <= 0 || pbMain.Height <= 0) return (1f, 0f, 0f);
            float scale = Math.Min(pbMain.Width / 3840f, pbMain.Height / 2160f);
            float offX = (pbMain.Width - 3840f * scale) / 2f;
            float offY = (pbMain.Height - 2160f * scale) / 2f;
            return (scale, offX, offY);
        }

        // Tradueix un punt de la finestra real (mode demo) al punt equivalent al full virtual
        // 3840x2160, perquè la detecció de clics (pensada sempre en coordenades virtuals) no
        // s'hagi de tocar. Fora de mode demo, retorna el punt tal qual (zero canvi de comportament).
        private Point ToVirtualCoords(Point p)
        {
            if (!IsDemoMode) return p;
            var (scale, offX, offY) = GetDemoTransform();
            if (scale <= 0f) return p;
            return new Point((int)((p.X - offX) / scale), (int)((p.Y - offY) / scale));
        }

        private MouseEventArgs TranslateMouseEventArgs(MouseEventArgs e)
        {
            if (!IsDemoMode) return e;
            var pt = ToVirtualCoords(e.Location);
            return new MouseEventArgs(e.Button, e.Clicks, pt.X, pt.Y, e.Delta);
        }

        // Tradueix un rectangle en coordenades virtuals (3840x2160) a la mida/posició reals de
        // pbMain, per poder invalidar només un tros petit (p. ex. una roda de trim) en comptes de
        // tota la finestra. Sense això, en mode demo (finestra petita) el rectangle "virtual" cau
        // fora dels límits reals del control i no es repinta res.
        private Rectangle ToRealInvalidateRect(Rectangle virtualRect)
        {
            if (!IsDemoMode) return virtualRect;
            var (scale, offX, offY) = GetDemoTransform();
            if (scale <= 0f) return virtualRect;
            return new Rectangle(
                (int)(offX + virtualRect.X * scale) - 4,
                (int)(offY + virtualRect.Y * scale) - 4,
                (int)(virtualRect.Width * scale) + 8,
                (int)(virtualRect.Height * scale) + 8);
        }



        private void PbMain_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
            if (IsDemoMode)
            {
                // Diagnòstic temporal: confirmar si Windows només marca per repintar una franja
                // petita (no tot pbMain) durant el redimensionament — explicaria per què Clear()
                // no n'hi havia prou (Clear() només neteja dins del clip actual).
                if (_demoPaintDebugCount < 40)
                {
                    _demoPaintDebugCount++;
                    try
                    {
                        System.IO.File.AppendAllText("panelAIR_App_crash.txt",
                            $"\r\n[PAINT DEBUG #{_demoPaintDebugCount}] ClipRectangle={e.ClipRectangle} pbMainClientRect={pbMain.ClientRectangle}");
                    }
                    catch { }
                }
                // Força el clip a TOT el control abans de netejar/dibuixar: sense això, Clear()
                // només netejava la franja petita que Windows marcava com a "per repintar" durant
                // l'arrossegament, deixant la resta amb el contingut vell apilat a sota.
                e.Graphics.Clip = new Region(pbMain.ClientRectangle);
                e.Graphics.Clear(Color.Black);
                var (demoScale, demoOffX, demoOffY) = GetDemoTransform();
                e.Graphics.TranslateTransform(demoOffX, demoOffY);
                e.Graphics.ScaleTransform(demoScale, demoScale);

                // Amb un escalat actiu (encara que sigui gairebé 1:1), GDI+ interpola cada imatge
                // dibuixada — car de calcular i el cost creix amb la mida de sortida (per això anava
                // lent a mida gran/4K). En mode demo no cal la mateixa qualitat que en hardware real:
                // prioritzem velocitat.
                e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
                e.Graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
                e.Graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            }

            var activeBg = (_g3000Active && _bgG3000Image != null) ? _bgG3000Image : _bgImage;
            if (IsDemoMode && activeBg != null)
            {
                // En mode demo, fons + marca d'aigua es combinen en una sola imatge ja feta
                // (regenerada només quan canvia l'avió/mode o l'esglaó d'opacitat, no cada
                // fotograma) — dibuixar-les per separat cada vegada doblava el cost de repintat
                // i anava molt lent a resolucions grans (4K real).
                // Invisible els primers 30 segons; a partir d'aquí, puja de manera lineal fins
                // arribar del tot opaca al minut 10.
                double demoElapsedSeconds = (DateTime.Now - _demoStartTime).TotalSeconds;
                double demoWatermarkProgress = demoElapsedSeconds < 30.0
                    ? 0.0
                    : Math.Clamp((demoElapsedSeconds - 30.0) / (600.0 - 30.0), 0.0, 1.0);
                int alphaByte = (int)Math.Round(255 * demoWatermarkProgress);
                if (_demoCompositeBg == null || _demoCompositeBgSource != activeBg || _demoCompositeBgAlpha != alphaByte)
                {
                    _demoCompositeBg?.Dispose();
                    _demoCompositeBg = BuildDemoComposite(activeBg, alphaByte);
                    _demoCompositeBgSource = activeBg;
                    _demoCompositeBgAlpha = alphaByte;
                }
                e.Graphics.DrawImage(_demoCompositeBg, new Rectangle(0, 0, 3840, 2160));
            }
            else if (activeBg != null)
                e.Graphics.DrawImage(activeBg, new Rectangle(0, 0, 3840, 2160));

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
                        // El repintat mentre dura la rotació el porta _g3kFuelSelAnimTimer (timer
                        // dedicat, veure comentari del camp i la seva inicialització) — aquí només
                        // calculem el pas de l'angle cap a l'objectiu.
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
                    if (!DEBUG_DISABLE_TRIM_INDICATORS)
                    {
                        using var g3kMarkerBr = new SolidBrush(Color.FromArgb(180, 180, 180));
                        e.Graphics.FillRectangle(g3kMarkerBr, new Rectangle(_g3kTrimIndX - 27, g3kMarkerY, 17, 2));
                        e.Graphics.FillEllipse(Brushes.White, new Rectangle(_g3kTrimIndX, g3kTrimY - 8, 17, 17));
                    }
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
                    if (!DEBUG_DISABLE_TRIM_INDICATORS)
                    {
                        using var g3kRtCenterBr = new SolidBrush(Color.FromArgb(180, 180, 180));
                        e.Graphics.FillRectangle(g3kRtCenterBr, new Rectangle(rtCenter - 1, _g3kRudderTrimIndY - 36, 2, 17));
                        e.Graphics.FillEllipse(Brushes.White, new Rectangle(rtX - 8, _g3kRudderTrimIndY - 8, 17, 17));
                    }
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
                            _flapsAnimTimerG3000?.Start();
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

                    // El repintat mentre dura l'animació ara el porta _flapsAnimTimerG3000 (veure
                    // comentari del camp i la seva inicialització) — mateix mecanisme que al G1000.
                    // Ja no cal fer res aquí.

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
                    using var overlayPen = new Pen(Color.Orange, 2);
                    foreach (var ov in g3000OverlayImages)
                    {
                        if (ov.OnClick == null) continue;
                        // OJO: NO filtrem per ov.ShowWhen aquí — en molts interruptors (llums, etc.)
                        // ShowWhen no indica aplicabilitat per avió sinó la posició visual actual del
                        // switch (p.ex. "() => !beaconActive"), i amagaria el ressaltat quan la llum
                        // està encesa. L'aplicabilitat real es llegeix d'OnClickEnabled quan existeix
                        // (senyal ja usat pel dispatch de clics: "aquest OnClick fa alguna cosa de
                        // veritat ara mateix"), i amb tapes 3D (IsHiddenByActiveCover).
                        if (!(ov.OnClickEnabled?.Invoke() ?? true)) continue;
                        if (ov.Name.Contains("RECOGN")) continue; // llum recognition: no la fem servir mai
                        if (IsHiddenByActiveCover(ov.Bounds)) continue; // tapat per una tapa 3D
                        if (ov.IsRound)
                        {
                            e.Graphics.FillEllipse(overlayBrush, ov.Bounds);
                            e.Graphics.DrawEllipse(overlayPen, ov.Bounds);
                        }
                        else
                        {
                            e.Graphics.FillRectangle(overlayBrush, ov.Bounds);
                            e.Graphics.DrawRectangle(overlayPen, ov.Bounds);
                        }
                    }
                    e.Graphics.FillRectangle(overlayBrush, g3kApSliderInputZone);
                    e.Graphics.DrawRectangle(overlayPen, g3kApSliderInputZone);

                    // Zones arrossegables (throttle, trim vertical, trim de rudder)
                    var thrHandleOv = g3000OverlayImages.FirstOrDefault(o => o.Name == "G3K_THR_HANDLE");
                    if (thrHandleOv != null && _thrQuadrantVisible)
                    {
                        e.Graphics.FillRectangle(overlayBrush, thrHandleOv.Bounds);
                        e.Graphics.DrawRectangle(overlayPen, thrHandleOv.Bounds);
                    }
                    e.Graphics.FillRectangle(overlayBrush, _g3kTrimZone);
                    e.Graphics.DrawRectangle(overlayPen, _g3kTrimZone);
                    e.Graphics.FillRectangle(overlayBrush, _g3kRudderTrimZone);
                    e.Graphics.DrawRectangle(overlayPen, _g3kRudderTrimZone);
                }

                DrawShowAreasIcon(e.Graphics);
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
                        _flapsAnimTimerG1000?.Start();
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

                // El repintat mentre dura l'animació ara el porta _flapsAnimTimerG1000 (veure
                // comentari del camp i la seva inicialització) — un timer propi a 33ms, no lligat a
                // aquest Paint ni a les lectures SimConnect, que es para sol quan flapsAnimDurationSec
                // torna a 0. Ja no cal fer res aquí.
            }

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
            if (!DEBUG_DISABLE_TRIM_INDICATORS)
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
                if (!DEBUG_DISABLE_TRIM_INDICATORS)
                {
                    using var rtCenterBr = new SolidBrush(Color.FromArgb(180, 180, 180));
                    e.Graphics.FillRectangle(rtCenterBr, new Rectangle(rtCenter - 1, rudderTrimIndY - 42, 2, 20));
                    // cercle indicador
                    e.Graphics.FillEllipse(Brushes.White, new Rectangle(rtX - 10, rudderTrimIndY - 10, 20, 20));
                }
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
                if (ov.Name == "SW_SE_SELECTOR") continue;
                if (ov.Img != null && (calibrationMode || (ov.ShowWhen?.Invoke() ?? true)))
                    e.Graphics.DrawImage(ov.Img, ov.AnimatedBounds);
            }

            // DA-40 — l'interruptor 2 no és una bomba de combustible, és el fuel transfer switch.
            // Sobreescrivim l'etiqueta impresa al fons ("FUEL PUMP R") amb el nom real.
            // Movible amb F12 (element SW_DA40_FUELTRANSF_LBL a overlayImages, es guarda al txt igual que la resta.
            {
                var daLbl = overlayImages.FirstOrDefault(o => o.Name == "SW_DA40_FUELTRANSF_LBL");
                if (daLbl != null && (calibrationMode || (daLbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var daBgBrush = new SolidBrush(Color.FromArgb(28, 28, 28));
                    e.Graphics.FillRectangle(daBgBrush, daLbl.Bounds);
                    using var daFont = new Font("Arial Narrow", 27f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var daFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("FUEL TRANSF", daFont, Brushes.White, daLbl.Bounds, daFmt);
                }
            }

            // Caravan — etiqueta "STBY / ALT PWR" (2 files), mateix estil que SW_DA40_FUELTRANSF_LBL.
            {
                var altGen2Lbl = overlayImages.FirstOrDefault(o => o.Name == "SW_CARAVAN_ALTGEN2_LBL");
                if (altGen2Lbl != null && (calibrationMode || (altGen2Lbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var lblBgBrush = new SolidBrush(Color.FromArgb(28, 28, 28));
                    e.Graphics.FillRectangle(lblBgBrush, altGen2Lbl.Bounds);
                    using var lblFont = new Font("Arial Narrow", 32f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("STBY\nALT PWR", lblFont, Brushes.White, altGen2Lbl.Bounds, lblFmt);
                }
            }

            // DA-40 — etiqueta "ESS / BUS" (2 files), idèntica a SW_CARAVAN_ALTGEN2_LBL.
            {
                var essBusLbl = overlayImages.FirstOrDefault(o => o.Name == "SW_DA40_ESSBUS_LBL");
                if (essBusLbl != null && (calibrationMode || (essBusLbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var lblBgBrush = new SolidBrush(Color.FromArgb(28, 28, 28));
                    e.Graphics.FillRectangle(lblBgBrush, essBusLbl.Bounds);
                    using var lblFont = new Font("Arial Narrow", 32f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("ESS\nBUS", lblFont, Brushes.White, essBusLbl.Bounds, lblFmt);
                }
            }

            // Caravan — etiquetes RESET/ON/TRIP de l'interruptor ALTGEN1.
            {
                var altGen1Labels = new (string Name, string Text)[] {
                    ("SW_CARAVAN_ALTGEN1_LBL_RESET", "RESET"),
                    ("SW_CARAVAN_ALTGEN1_LBL_ON", "ON"),
                    ("SW_CARAVAN_ALTGEN1_LBL_TRIP", "TRIP"),
                };
                foreach (var (name, text) in altGen1Labels)
                {
                    var lbl = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (lbl != null && (calibrationMode || (lbl.ShowWhen?.Invoke() ?? false)))
                    {
                        using var lblBgBrush = new SolidBrush(Color.FromArgb(28, 28, 28));
                        e.Graphics.FillRectangle(lblBgBrush, lbl.Bounds);
                        using var lblFont = new Font("Arial Narrow", 22f, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(text, lblFont, Brushes.White, lbl.Bounds, lblFmt);
                    }
                }
            }

            // Caravan — fuel boost pump: etiqueta "FUEL BOOST", mateix estil que SW_CARAVAN_ALTGEN2_LBL
            // (STBY/ALT PWR). La tapa pròpia s'ha eliminat — reutilitza SW_BONANZA_FUELSEL_R_COVER.
            {
                var fbLbl = overlayImages.FirstOrDefault(o => o.Name == "SW_CARAVAN_FUELBOOST_LBL");
                if (fbLbl != null && (calibrationMode || (fbLbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var fbBgBrush = new SolidBrush(Color.FromArgb(28, 28, 28));
                    e.Graphics.FillRectangle(fbBgBrush, fbLbl.Bounds);
                    using var fbFont = new Font("Arial Narrow", 32f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var fbFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("FUEL BOOST", fbFont, Brushes.White, fbLbl.Bounds, fbFmt);
                }
            }

            // DA-62 — etiqueta "MASTER", mateix estil que FUEL BOOST però d'una sola línia.
            {
                var masterLbl = overlayImages.FirstOrDefault(o => o.Name == "SW_DA62_MASTER_LBL");
                if (masterLbl != null && (calibrationMode || (masterLbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var masterBgBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
                    e.Graphics.FillRectangle(masterBgBrush, masterLbl.Bounds);
                    using var masterFont = new Font("Arial Narrow", 32f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var masterFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("MASTER", masterFont, Brushes.White, masterLbl.Bounds, masterFmt);
                }
            }

            // DA-62 — capa opaca sense text, mateixa alçada que MASTER, 15% més estreta.
            {
                var masterCover = overlayImages.FirstOrDefault(o => o.Name == "SW_DA62_MASTER_COVER");
                if (masterCover != null && (calibrationMode || (masterCover.ShowWhen?.Invoke() ?? false)))
                {
                    using var masterCoverBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
                    e.Graphics.FillRectangle(masterCoverBrush, masterCover.Bounds);
                }
            }

            // DA-62 — duplicat de MASTER_LBL/MASTER_COVER, 200px més a l'esquerra.
            {
                var masterLbl2 = overlayImages.FirstOrDefault(o => o.Name == "SW_DA62_MASTER_LBL_2");
                if (masterLbl2 != null && (calibrationMode || (masterLbl2.ShowWhen?.Invoke() ?? false)))
                {
                    using var masterBgBrush2 = new SolidBrush(Color.FromArgb(30, 30, 30));
                    e.Graphics.FillRectangle(masterBgBrush2, masterLbl2.Bounds);
                    using var masterFont2 = new Font("Arial Narrow", 32f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var masterFmt2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("MASTER", masterFont2, Brushes.White, masterLbl2.Bounds, masterFmt2);
                }
                var masterCover2 = overlayImages.FirstOrDefault(o => o.Name == "SW_DA62_MASTER_COVER_2");
                if (masterCover2 != null && (calibrationMode || (masterCover2.ShowWhen?.Invoke() ?? false)))
                {
                    using var masterCoverBrush2 = new SolidBrush(Color.FromArgb(30, 30, 30));
                    e.Graphics.FillRectangle(masterCoverBrush2, masterCover2.Bounds);
                }
            }

            // DA-62 — etiqueta "Diamond DA-62" en cursiva.
            {
                var titleLbl = overlayImages.FirstOrDefault(o => o.Name == "SW_DA62_TITLE_LBL");
                if (titleLbl != null && (calibrationMode || (titleLbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var titleBgBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
                    e.Graphics.FillRectangle(titleBgBrush, titleLbl.Bounds);
                    using var titleFont = new Font("Arial Narrow", 40f, FontStyle.Italic, GraphicsUnit.Pixel);
                    using var titleFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Diamond DA-62", titleFont, Brushes.White, titleLbl.Bounds, titleFmt);
                }
            }

            // DA-40 — etiqueta "Diamond DA40", idèntica a la de la DA-62.
            {
                var titleLblDa40 = overlayImages.FirstOrDefault(o => o.Name == "SW_DA40_TITLE_LBL");
                if (titleLblDa40 != null && (calibrationMode || (titleLblDa40.ShowWhen?.Invoke() ?? false)))
                {
                    using var titleBgBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
                    e.Graphics.FillRectangle(titleBgBrush, titleLblDa40.Bounds);
                    using var titleFont = new Font("Arial Narrow", 40f, FontStyle.Italic, GraphicsUnit.Pixel);
                    using var titleFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Diamond DA40", titleFont, Brushes.White, titleLblDa40.Bounds, titleFmt);
                }
            }

            // Caravan — etiqueta "Cessna Caravan", idèntica a la de la DA-62.
            {
                var titleLblCaravan = overlayImages.FirstOrDefault(o => o.Name == "SW_CARAVAN_TITLE_LBL");
                if (titleLblCaravan != null && (calibrationMode || (titleLblCaravan.ShowWhen?.Invoke() ?? false)))
                {
                    using var titleBgBrush = new SolidBrush(Color.FromArgb(30, 30, 30));
                    e.Graphics.FillRectangle(titleBgBrush, titleLblCaravan.Bounds);
                    using var titleFont = new Font("Arial Narrow", 40f, FontStyle.Italic, GraphicsUnit.Pixel);
                    using var titleFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("Cessna Caravan", titleFont, Brushes.White, titleLblCaravan.Bounds, titleFmt);
                }
            }

            // Caravan — etiqueta "GENE / RATOR", mateix estil que SW_CARAVAN_ALTGEN2_LBL (STBY/ALT PWR).
            {
                var genLbl = overlayImages.FirstOrDefault(o => o.Name == "SW_CARAVAN_GENERATOR_LBL");
                if (genLbl != null && (calibrationMode || (genLbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var genBgBrush = new SolidBrush(Color.FromArgb(28, 28, 28));
                    e.Graphics.FillRectangle(genBgBrush, genLbl.Bounds);
                    using var genFont = new Font("Arial Narrow", 32f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var genFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("GENE\nRATOR", genFont, Brushes.White, genLbl.Bounds, genFmt);
                }
            }

            // Caravan — etiquetes ON/OFF/NORM del fuel boost pump, mateix estil que RESET/ON/TRIP.
            {
                var fbPosLabels = new (string Name, string Text)[] {
                    ("SW_CARAVAN_FUELBOOST_LBL_ON", "ON"),
                    ("SW_CARAVAN_FUELBOOST_LBL_OFF", "OFF"),
                    ("SW_CARAVAN_FUELBOOST_LBL_NORM", "NORM"),
                };
                foreach (var (name, text) in fbPosLabels)
                {
                    var lbl = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (lbl != null && (calibrationMode || (lbl.ShowWhen?.Invoke() ?? false)))
                    {
                        using var lblBgBrush = new SolidBrush(Color.FromArgb(28, 28, 28));
                        e.Graphics.FillRectangle(lblBgBrush, lbl.Bounds);
                        using var lblFont = new Font("Arial Narrow", 22f, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(text, lblFont, Brushes.White, lbl.Bounds, lblFmt);
                    }
                }
            }

            // Caravan — etiquetes MOTOR/OFF/START del STARTER, mateix estil que les del fuel boost.
            {
                var starterPosLabels = new (string Name, string Text)[] {
                    ("SW_CARAVAN_STARTER1_LBL_START", "START"),
                    ("SW_CARAVAN_STARTER1_LBL_OFF", "OFF"),
                    ("SW_CARAVAN_STARTER1_LBL_MOTOR", "MOTOR"),
                };
                foreach (var (name, text) in starterPosLabels)
                {
                    var lbl = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (lbl != null && (calibrationMode || (lbl.ShowWhen?.Invoke() ?? false)))
                    {
                        using var lblFont = new Font("Arial Narrow", 19.8f, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(text, lblFont, Brushes.White, lbl.Bounds, lblFmt);
                    }
                }
            }

            // Caravan — etiquetes NORM/ON de l'IGNITION, mateix estil que les del STARTER.
            {
                var ignitionPosLabels = new (string Name, string Text)[] {
                    ("SW_CARAVAN_IGNITION1_LBL_ON", "ON"),
                    ("SW_CARAVAN_IGNITION1_LBL_NORM", "NORM"),
                };
                foreach (var (name, text) in ignitionPosLabels)
                {
                    var lbl = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (lbl != null && (calibrationMode || (lbl.ShowWhen?.Invoke() ?? false)))
                    {
                        using var lblFont = new Font("Arial Narrow", 19.8f, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(text, lblFont, Brushes.White, lbl.Bounds, lblFmt);
                    }
                }
            }

            // Caravan — etiqueta "FUEL TANK / SELECTORS", mateix color que SW_CARAVAN_COVER_1/2, text 48px
            // (1.5x FUEL BOOST). Dibuixada AL DAVANT DE TOT (última del grup Caravan) perquè quedi per sobre
            // de la resta d'etiquetes/capes si es solapen.
            {
                var ftsLbl = overlayImages.FirstOrDefault(o => o.Name == "SW_CARAVAN_FUELTANK_SELECTORS_LBL");
                if (ftsLbl != null && (calibrationMode || (ftsLbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var ftsBgBrush = new SolidBrush(Color.FromArgb(25, 25, 25));
                    e.Graphics.FillRectangle(ftsBgBrush, ftsLbl.Bounds);
                    using var ftsFont = new Font("Arial Narrow", 48f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var ftsFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("FUEL TANK\nSELECTORS", ftsFont, Brushes.White, ftsLbl.Bounds, ftsFmt);
                }
            }

            // Bonanza G36 / Caravan / Cirrus SR22T — SW_BONANZA_FUELSEL_R_COVER: convertida a tapa amb
            // relleu, dibuixada al final (veure bloc DrawReliefCover a baix de tot).

            // Monomotors — rectangle negre que tapa els selectors LEFT/RIGHT (referència de mides/posició
            // pel futur selector únic de 3 posicions, centrat en aquest requadre).
            {
                var seCover = overlayImages.FirstOrDefault(o => o.Name == "SW_SE_SELECTOR_COVER");
                if (seCover != null && (calibrationMode || (seCover.ShowWhen?.Invoke() ?? false)))
                {
                    using var seCoverBrush = new SolidBrush(Color.FromArgb(23, 23, 23));
                    e.Graphics.FillRectangle(seCoverBrush, seCover.Bounds);
                }
            }

            // SW_SE_LBL_1/2/3 — etiquetes independents del selector únic, mateix estil que SW_DA40_FUELTRANSF_LBL.
            {
                for (int slot = 1; slot <= 3; slot++)
                {
                    var lbl = overlayImages.FirstOrDefault(o => o.Name == $"SW_SE_LBL_{slot}");
                    if (lbl != null && (calibrationMode || (lbl.ShowWhen?.Invoke() ?? false)))
                    {
                        using var lblBgBrush = new SolidBrush(Color.FromArgb(23, 23, 23));
                        e.Graphics.FillRectangle(lblBgBrush, lbl.Bounds);
                        using var lblFont = new Font("Arial Narrow", 27f, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(SeSelectorLabelText(slot), lblFont, Brushes.White, lbl.Bounds, lblFmt);
                    }
                }
            }

            // SW_SE_TITLE_LBL — "TANKS FUEL SELECTOR", Arial normal (no Narrow), 25% més gran que les
            // SW_SE_LBL_1/2/3 (27px * 1.25 ≈ 34px).
            {
                var seTitleLbl = overlayImages.FirstOrDefault(o => o.Name == "SW_SE_TITLE_LBL");
                if (seTitleLbl != null && (calibrationMode || (seTitleLbl.ShowWhen?.Invoke() ?? false)))
                {
                    using var seTitleFont = new Font("Arial", 34f, FontStyle.Bold, GraphicsUnit.Pixel);
                    using var seTitleFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    e.Graphics.DrawString("TANK FUEL SELECTOR", seTitleFont, Brushes.White, seTitleLbl.Bounds, seTitleFmt);
                }
            }

            // SW_SE_SELECTOR: pos0=Normal(-30°), pos1=Emergency(0°), pos2=OFF(+30°) — DA-40 de moment.
            {
                var sw = overlayImages.FirstOrDefault(o => o.Name == "SW_SE_SELECTOR");
                if (sw?.Img != null && (calibrationMode || (sw.ShowWhen?.Invoke() ?? false)))
                {
                    float angle = (seSelectorPos - 1) * 30f + 30f + 1f; // mateix conveni que SW_FUEL1/2 (l'asset porta un +30° de base), +1° addicional demanat per Pere
                    float baseW = sw.ZoomWidth;
                    float baseH = baseW / sw.AspectRatio;
                    float cx = sw.BasePos.X + baseW / 2f;
                    float cy = sw.BasePos.Y + baseH / 2f;
                    var state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(cx, cy);
                    e.Graphics.RotateTransform(angle);
                    e.Graphics.DrawImage(sw.Img, -baseW / 2f, -baseH / 2f, baseW, baseH);
                    e.Graphics.Restore(state);

                    // 3 ratlletes fixes de referència de posició (-30°/0°/+30°, respecte la vertical),
                    // NO roten amb el selector — són marques estàtiques del panell, no de l'estat actual.
                    // La de +30° és simètrica de la de -30°.
                    using var tickPen = new Pen(Color.FromArgb(200, 200, 200), 5f);
                    float rInner = baseW / 2f + 8f;
                    float rOuter = rInner + baseW * 0.18f;
                    foreach (float tickAngle in new[] { -30f, 0f, 30f })
                    {
                        double rad = tickAngle * Math.PI / 180.0;
                        float dx = (float)Math.Sin(rad);
                        float dy = -(float)Math.Cos(rad);
                        var p1 = new PointF(cx + dx * rInner, cy + dy * rInner);
                        var p2 = new PointF(cx + dx * rOuter, cy + dy * rOuter);
                        e.Graphics.DrawLine(tickPen, p1, p2);
                    }
                }
            }

            // Bonanza G36 — etiquetes OFF/LO/HI, mateix estil que RESET/ON/TRIP. Dibuixades DESPRÉS del
            // selector únic i les seves etiquetes perquè quedin al davant de tot (no tapades).
            {
                var bonPumpLabels = new (string Name, string Text)[] {
                    ("SW_BONANZA_FUELPUMP_LBL_OFF", "OFF"),
                    ("SW_BONANZA_FUELPUMP_LBL_LO", "LO"),
                    ("SW_BONANZA_FUELPUMP_LBL_HI", "HI"),
                };
                foreach (var (name, text) in bonPumpLabels)
                {
                    var lbl = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (lbl != null && (calibrationMode || (lbl.ShowWhen?.Invoke() ?? false)))
                    {
                        using var lblBgBrush = new SolidBrush(Color.FromArgb(23, 23, 23));
                        e.Graphics.FillRectangle(lblBgBrush, lbl.Bounds);
                        using var lblFont = new Font("Arial Narrow", 22f, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(text, lblFont, Brushes.White, lbl.Bounds, lblFmt);
                    }
                }
            }

            // Baron G58 — etiquetes OFF/LO/HI per cada bomba (motor 1 i motor 2), mateix estil que la Bonanza.
            {
                var baronPumpLabels = new (string Name, string Text)[] {
                    ("SW_BARON_FUELPUMP1_LBL_OFF", "OFF"),
                    ("SW_BARON_FUELPUMP1_LBL_LO", "LO"),
                    ("SW_BARON_FUELPUMP1_LBL_HI", "HI"),
                    ("SW_BARON_FUELPUMP2_LBL_OFF", "OFF"),
                    ("SW_BARON_FUELPUMP2_LBL_LO", "LO"),
                    ("SW_BARON_FUELPUMP2_LBL_HI", "HI"),
                };
                foreach (var (name, text) in baronPumpLabels)
                {
                    var lbl = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (lbl != null && (calibrationMode || (lbl.ShowWhen?.Invoke() ?? false)))
                    {
                        using var lblBgBrush = new SolidBrush(Color.FromArgb(23, 23, 23));
                        e.Graphics.FillRectangle(lblBgBrush, lbl.Bounds);
                        using var lblFont = new Font("Arial Narrow", 22f, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(text, lblFont, Brushes.White, lbl.Bounds, lblFmt);
                    }
                }
            }

            // Cirrus SR22T — etiquetes PRIME/OFF/BOOST, mateix estil/mida que les OFF/LO/HI de la Bonanza.
            {
                var cirrusPumpLabels = new (string Name, string Text)[] {
                    ("SW_CIRRUS_FUELPUMP_LBL_PRIME", "PRIME"),
                    ("SW_CIRRUS_FUELPUMP_LBL_OFF", "OFF"),
                    ("SW_CIRRUS_FUELPUMP_LBL_BOOST", "BOOST"),
                };
                foreach (var (name, text) in cirrusPumpLabels)
                {
                    var lbl = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (lbl != null && (calibrationMode || (lbl.ShowWhen?.Invoke() ?? false)))
                    {
                        using var lblBgBrush = new SolidBrush(Color.FromArgb(23, 23, 23));
                        e.Graphics.FillRectangle(lblBgBrush, lbl.Bounds);
                        using var lblFont = new Font("Arial Narrow", 22f, FontStyle.Bold, GraphicsUnit.Pixel);
                        using var lblFmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                        e.Graphics.DrawString(text, lblFont, Brushes.White, lbl.Bounds, lblFmt);
                    }
                }
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

            // Caravan — SW_CARAVAN_COVER_1/2: capes opaques sense text, planes (sense relleu), dibuixades
            // AQUÍ (abans dels selectors rotatius SW_FUEL1/2) perquè quedin DARRERA seu, tal com Pere les
            // havia posicionat originalment.
            {
                var caravanCoverNames = new[] { "SW_CARAVAN_COVER_1", "SW_CARAVAN_COVER_2" };
                foreach (var name in caravanCoverNames)
                {
                    var cov = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (cov != null && (calibrationMode || (cov.ShowWhen?.Invoke() ?? false)))
                    {
                        using var covBrush = new SolidBrush(Color.FromArgb(27, 27, 27));
                        e.Graphics.FillRectangle(covBrush, cov.Bounds);
                    }
                }
            }

            // SW_FUEL1: pos0=OFF(-30°), pos1=ON(0°), pos2=CROSSFEED(+30°)
            {
                var sw = overlayImages.FirstOrDefault(o => o.Name == "SW_FUEL1");
                if (sw?.Img != null && !IsDA40() && !IsC400() && !IsC172() && !IsBonanzaG36() && !IsCirrusSR22() && !IsC408())
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
                if (sw?.Img != null && !IsDA40() && !IsC400() && !IsC172() && !IsBonanzaG36() && !IsCirrusSR22() && !IsC408())
                {
                    // Caravan: fuel2Pos només és 0=OFF/1=ON (sense crossfeed), a diferència de la resta
                    // d'avions (0=CROSSFEED/1=ON/2=OFF) — la fórmula genèrica de sota confondria OFF(0)
                    // amb CROSSFEED. Mapegem OFF a la mateixa posició visual que "pos2" (+30°) i ON a "pos1" (0°).
                    float angle = IsCaravan()
                        ? (fuel2Pos == 1 ? 30f : 60f)
                        : (fuel2Pos - 1) * 30f + 30f;
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
                using var overlayPen = new Pen(Color.Orange, 2);

                // OverlayImages amb OnClick (G1000 — el G3000 ja ha fet return abans d'arribar aquí)
                foreach (var ov in overlayImages)
                {
                    if (ov.OnClick == null) continue;
                    // OJO: NO filtrem per ov.ShowWhen aquí — veure comentari equivalent al bloc G3000
                    // (ShowWhen sovint és estat visual del switch, no aplicabilitat per avió).
                    if (!(ov.OnClickEnabled?.Invoke() ?? true)) continue;
                    if (ov.Name.Contains("RECOGN")) continue; // llum recognition: no la fem servir mai
                    if (IsHiddenByActiveCover(ov.Bounds)) continue; // tapat per una tapa 3D
                    e.Graphics.FillRectangle(overlayBrush, ov.Bounds);
                    e.Graphics.DrawRectangle(overlayPen, ov.Bounds);
                }

                // Trim wheel vertical
                e.Graphics.FillRectangle(overlayBrush, trimZone);
                e.Graphics.DrawRectangle(overlayPen, trimZone);

                // Rudder trim wheel horitzontal — DA-40, C172 i Bonanza G36 (2026-09-05, confirmat per
                // Pere): exclosa.
                if (!IsDA40() && !IsC172() && !IsBonanzaG36())
                {
                    e.Graphics.FillRectangle(overlayBrush, rudderTrimZone);
                    e.Graphics.DrawRectangle(overlayPen, rudderTrimZone);
                }
            }


            // Tapes amb relleu (efecte sortint del panell): mateix gris de fons que la resta d'etiquetes,
            // amb una vora clara a dalt/esquerra i una vora fosca a baix/dreta simulant llum des de
            // dalt-esquerra sobre una superfície en relleu. Dibuixades AL FINAL de tot (per sobre del 4K
            // i de qualsevol animació PNG), just abans de l'overlay de calibratge.
            DrawReliefCover(e.Graphics, "SW_DA62_COVER_RELIEF");
            DrawReliefCover(e.Graphics, "SW_DA40_COVER_RELIEF");
            DrawReliefCover(e.Graphics, "SW_DA40_COVER_RELIEF_3");
            DrawReliefCover(e.Graphics, "SW_DA40_COVER_RELIEF_4");
            DrawReliefCover(e.Graphics, "SW_CIRRUS_COVER_RELIEF");
            DrawReliefCover(e.Graphics, "SW_CIRRUS_COVER_RELIEF_2");
            DrawReliefCover(e.Graphics, "SW_CARAVAN_COVER_IGNITION_R");
            DrawReliefCover(e.Graphics, "SW_BONANZA_FUELSEL_R_COVER");
            DrawReliefCover(e.Graphics, "SW_C172_ALTGEN2_COVER");

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

            DrawShowAreasIcon(e.Graphics);


            }
            catch (OverflowException) { }
        }

        // Fons + marca d'aigua "DEMO" (mesura anti-abús) precombinats en una sola imatge, per no
        // haver de fer dos dibuixos grans (fons i marca d'aigua per separat) a cada fotograma —
        // això anava molt lent a resolucions grans (4K real). Es regenera només quan canvia
        // l'avió/mode de cabina o l'esglaó d'opacitat (cada uns 15 segons), mai a cada repintat.
        private Bitmap? _demoCompositeBg;
        private Image? _demoCompositeBgSource;
        private int _demoCompositeBgAlpha = -1;

        private Bitmap BuildDemoComposite(Image bg, int alphaByte)
        {
            var bmp = new Bitmap(3840, 2160, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bg, new Rectangle(0, 0, 3840, 2160));
                if (alphaByte > 0)
                {
                    using var font = new Font("Segoe UI", 90f, FontStyle.Bold);
                    using var brush = new SolidBrush(Color.FromArgb(alphaByte, 255, 255, 255));
                    var state = g.Save();
                    g.RotateTransform(-30f);
                    for (int y = -2000; y < 4500; y += 340)
                    {
                        for (int x = -2500; x < 6500; x += 620)
                        {
                            g.DrawString("DEMO", font, brush, x, y);
                        }
                    }
                    g.Restore(state);
                }
            }
            return bmp;
        }

        private void PbMain_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Zona de clic més gran que la icona dibuixada (63x63): amb el dit és fàcil quedar-se
                // just fora del quadrat exacte. La icona en si NO canvia de mida ni posició —
                // DrawShowAreasIcon (PbMain_Paint) segueix fent servir el rectangle original de 63x63,
                // aquest és només per detectar el clic.
                var showAreasRect = new Rectangle(_showAreasBtnPos, new Size(63, 63));
                showAreasRect.Inflate(25, 25);
                if (showAreasRect.Contains(e.Location))
                {
                    if (calibrationMode)
                    {
                        _showAreasBtnDragging = true;
                        _showAreasBtnDragOffset = new Point(e.X - _showAreasBtnPos.X, e.Y - _showAreasBtnPos.Y);
                    }
                    else
                    {
                        mainSurface.ShowAllAreas = !mainSurface.ShowAllAreas;
                        pbMain.Invalidate();
                    }
                    return;
                }
            }
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
                    MarkCommandSent();
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
                    StartElevatorTrimActiveRead();
                }
                if (_g3kRudderTrimZone.Contains(e.Location))
                {
                    _g3kRudderTrimDragging = true;
                    _g3kRudderTrimLastMouseX = e.X;
                    StartRudderTrimActiveRead();
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
                    { ov.PressedUntilUtc = DateTime.UtcNow.AddMilliseconds(ov.PressVisualMs); ov.OnClick.Invoke(); MarkCommandSent(); pbMain.Invalidate(); return; }

            foreach (var ov in overlayImages)
                if (ov.OnClick != null && (ov.OnClickEnabled?.Invoke() ?? true) && ov.Bounds.Contains(e.Location))
                { ov.OnClick.Invoke(); MarkCommandSent(); return; }

            if (trimZone.Contains(e.Location))
            {
                trimDragging = true;
                trimLastMouseY = e.Y;
                StartElevatorTrimActiveRead();
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
                StartRudderTrimActiveRead();
            }
        }

        private void PbMain_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_showAreasBtnDragging)
            {
                _showAreasBtnPos = new Point(e.X - _showAreasBtnDragOffset.X, e.Y - _showAreasBtnDragOffset.Y);
                pbMain.Invalidate();
                return;
            }
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
                        MarkCommandSent();
                    }
                    int topLimit = minY;
                    int botLimit = maxY;
                    if (!_thrHandleLeftRail && newY <= topLimit + 10)
                    {
                        _taxiPropActive = false; _thrSliderFollow = false;
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.AXIS_THROTTLE_SET, unchecked((uint)(-11000)), SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.PROP_PITCH_SET, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        MarkCommandSent();
                    }
                    if (!_thrHandleLeftRail && newY >= botLimit - 10)
                    {
                        _taxiPropActive = false; _thrSliderFollow = false;
                        simconnect?.SetDataOnSimObject(DEFINITIONS.CondLeverCutoffWrite, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, new CondLeverCutoffStruct { value = 0 });
                        simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SET_FUEL_VALVE_ENG1, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                        MarkCommandSent();
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
                            MarkCommandSent();
                        }
                    }
                    // Retallat al recorregut del handle i limitat a com a màxim cada 50ms — mateix
                    // motiu que la roda FLC/VS, veure comentari allà.
                    if ((DateTime.UtcNow - _lastThrHandleDragInvalidate).TotalMilliseconds >= 50)
                    {
                        _lastThrHandleDragInvalidate = DateTime.UtcNow;
                        const int margin = 30;
                        var thrDragRect = new Rectangle(
                            thrHandle.BasePos.X - margin,
                            minY - margin,
                            thrHandle.Bounds.Width + margin * 2,
                            (maxY - minY) + thrHandle.Bounds.Height + margin * 2);
                        pbMain.Invalidate(ToRealInvalidateRect(thrDragRect));
                    }
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
                // La posició es desa a cada MouseMove (gratis, és matemàtica local), però el
                // repintat es limita — MouseMove pot disparar-se molt més sovint que qualsevol
                // timer (al ritme natiu del ratolí/tàctil), i aquest Invalidate() no tenia CAP
                // límit — era el cost real "ocult" que quedava fora de tots els arreglaments
                // d'avui (confirmat per Pere, 2026-10-03: la roda seguia anant a tota màquina).
                if ((DateTime.UtcNow - _lastElevatorTrimInvalidate).TotalMilliseconds >= 50)
                {
                    _lastElevatorTrimInvalidate = DateTime.UtcNow;
                    pbMain.Invalidate(ToRealInvalidateRect(Rectangle.Inflate(trimZone, 80, 80)));
                }
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
                // Mateix motiu que el bloc G1000 d'amunt.
                if ((DateTime.UtcNow - _lastElevatorTrimInvalidate).TotalMilliseconds >= 50)
                {
                    _lastElevatorTrimInvalidate = DateTime.UtcNow;
                    pbMain.Invalidate(ToRealInvalidateRect(Rectangle.Inflate(_g3kTrimZone, 80, 80)));
                }
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
                // Mateix motiu que l'elevator, veure comentari allà.
                if ((DateTime.UtcNow - _lastRudderTrimInvalidate).TotalMilliseconds >= 50)
                {
                    _lastRudderTrimInvalidate = DateTime.UtcNow;
                    pbMain.Invalidate(ToRealInvalidateRect(Rectangle.Inflate(_g3kRudderTrimZone, 80, 80)));
                }
            }
            if (g3kApSliderDragging)
            {
                int deltaY = e.Y - g3kApSliderLastMouseY; // positiu = swipe avall = nose up (invertit)
                g3kApSliderLastMouseY = e.Y;
                _g3kApWheelOffset -= deltaY * 0.5f;
                g3kApSliderAccumDelta += deltaY;
                while (g3kApSliderAccumDelta >= G3K_AP_SLIDER_TICK_PIXELS)  { SendG3kApSliderTick(+1); g3kApSliderAccumDelta -= G3K_AP_SLIDER_TICK_PIXELS; }
                while (g3kApSliderAccumDelta <= -G3K_AP_SLIDER_TICK_PIXELS) { SendG3kApSliderTick(-1); g3kApSliderAccumDelta += G3K_AP_SLIDER_TICK_PIXELS; }
                // Retallat a la zona de la roda i limitat a com a màxim cada 50ms — mateixa tècnica
                // d'avui (2026-10-03, Pere), MouseMove pot disparar-se molt més sovint que qualsevol
                // timer i aquest Invalidate() no tenia cap límit.
                if ((DateTime.UtcNow - _lastG3kApSliderInvalidate).TotalMilliseconds >= 50)
                {
                    _lastG3kApSliderInvalidate = DateTime.UtcNow;
                    pbMain.Invalidate(ToRealInvalidateRect(Rectangle.Inflate(g3kApSliderZone, 20, 20)));
                }
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
                // Mateix motiu que l'elevator, veure comentari al bloc G1000 d'amunt.
                if ((DateTime.UtcNow - _lastRudderTrimInvalidate).TotalMilliseconds >= 50)
                {
                    _lastRudderTrimInvalidate = DateTime.UtcNow;
                    pbMain.Invalidate(ToRealInvalidateRect(Rectangle.Inflate(rudderTrimZone, 80, 80)));
                }
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
            MarkCommandSent();
            // Confirmació immediata NOMÉS al primer tic de l'arrossegament (evita el retard
            // d'arrencada) — els següents tics NO disparen lectura pròpia, els porta el timer actiu
            // al seu ritme. Fer-ho a cada tic (com abans) ignorava per complet l'interval del timer
            // perquè un sol MouseMove ràpid pot cridar SendTrimTick moltes vegades (confirmat per
            // Pere, 2026-10-03: canviar l'interval de 33 a 100ms no canviava res, aquest era el motiu).
            if (_elevatorTrimAwaitingFirstTick)
            {
                _elevatorTrimAwaitingFirstTick = false;
                SyncElevatorTrimOnce();
            }
        }

        private void SendRudderTrimTick(int dir)
        {
            if (simconnect == null) return;
            if (dir > 0)
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.RUDDER_TRIM_RIGHT, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            else
                simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.RUDDER_TRIM_LEFT,  0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            MarkCommandSent();
            // Mateix motiu que SendTrimTick: només el primer tic fa lectura immediata.
            if (_rudderTrimAwaitingFirstTick)
            {
                _rudderTrimAwaitingFirstTick = false;
                SyncRudderTrimOnce();
            }
        }

        private void SendAileronTrimSet(int axisVal)
        {
            if (simconnect == null) return;
            simconnect.TransmitClientEvent(0, SIMCONNECT_EVENTS.AILERON_TRIM_SET_EX1, (uint)axisVal, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
            MarkCommandSent();
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
            MarkCommandSent();
        }


        private void PbMain_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_showAreasBtnDragging)
            {
                _showAreasBtnDragging = false;
                try { ExportCalibrationPositions(silent: true); } catch { }
                return;
            }
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
            StopElevatorTrimActiveReadIfNotNeeded();
            StopRudderTrimActiveReadIfNotNeeded();
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

        // Ubicació estable, independent de si s'executa des de bin\Debug o bin\Release (2026-09-01):
        // abans es guardava al costat de l'exe (Application.StartupPath), fent que Debug i Release
        // tinguessin cadascun el seu propi fitxer desincronitzat. Migració automàtica un sol cop des
        // de la ubicació antiga si la nova encara no existeix.
        private static string GetCalibrationFilePath()
        {
            string dir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "panelAIR1000");
            System.IO.Directory.CreateDirectory(dir);
            string path = System.IO.Path.Combine(dir, "calibration_export.txt");

            if (!System.IO.File.Exists(path))
            {
                string legacyPath = System.IO.Path.Combine(Application.StartupPath, "calibration_export.txt");
                if (System.IO.File.Exists(legacyPath))
                {
                    try { System.IO.File.Copy(legacyPath, path); } catch { }
                }
            }
            return path;
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
                sb.AppendLine(ov.ZoomHeight > 0
                    ? $"IMG:{ov.Name}:{ov.BasePos.X}:{ov.BasePos.Y}:{(int)ov.ZoomWidth}:{(int)ov.ZoomHeight}"
                    : $"IMG:{ov.Name}:{ov.BasePos.X}:{ov.BasePos.Y}:{(int)ov.ZoomWidth}");
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
            if (_configBtn != null) sb.AppendLine($"CONFBTN:conf:{_configBtn.Location.X}:{_configBtn.Location.Y}");
            sb.AppendLine($"CLOSEBTN:close:{closeButton.Location.X}:{closeButton.Location.Y}");
            sb.AppendLine($"SHOWBTN:show:{_showAreasBtnPos.X}:{_showAreasBtnPos.Y}");
            sb.AppendLine($"GEARBTN:gear:{_gearBtn.Location.X}:{_gearBtn.Location.Y}");
            sb.AppendLine($"OVGEARBTN:ovgear:{_overlayGearBtn.Location.X}:{_overlayGearBtn.Location.Y}");
            sb.AppendLine($"VOLGRP:vg:{_volGrpAnchor.X}:{_volGrpAnchor.Y}");
            sb.AppendLine($"FSBTN:fs:{_fullscreenBtn.Location.X}:{_fullscreenBtn.Location.Y}");
            sb.AppendLine($"ENDBTN:end:{_msfsEndBtn.Location.X}:{_msfsEndBtn.Location.Y}");
            sb.AppendLine($"FOCUSBTN:focus:{_focusBtn.Location.X}:{_focusBtn.Location.Y}");
            sb.AppendLine($"G3KBTN:g3k:{_g3000Btn.Location.X}:{_g3000Btn.Location.Y}");
            sb.AppendLine($"G1000BTN:g1000:{_g1000Btn.Location.X}:{_g1000Btn.Location.Y}");
            sb.AppendLine($"A320BTN:a320:{_a320Btn.Location.X}:{_a320Btn.Location.Y}");
            if (_a320ZoomPlusPos is Point zp) sb.AppendLine($"A320ZOOMPLUS:a320zoom:{zp.X}:{zp.Y}");
            if (_a320ZoomMinusPos is Point zm) sb.AppendLine($"A320ZOOMMINUS:a320zoom:{zm.X}:{zm.Y}");
            if (_a320PageBtnPos is Point pgp) sb.AppendLine($"A320PAGEBTN:a320page:{pgp.X}:{pgp.Y}");
            if (_a320AreasBtnPos is Point arp) sb.AppendLine($"A320AREASBTN:a320areas:{arp.X}:{arp.Y}");
            sb.AppendLine($"THRQBTN:tq:{_thrQuadrantBtn.Location.X}:{_thrQuadrantBtn.Location.Y}:0:0");
            if (_fsConnPanel != null) sb.AppendLine($"FSCONN:fsc:{_fsConnPanel.Location.X}:{_fsConnPanel.Location.Y}");
            if (_appVersionPanel != null) sb.AppendLine($"APPVER:apv:{_appVersionPanel.Location.X}:{_appVersionPanel.Location.Y}");
            string path = GetCalibrationFilePath();
            System.IO.File.WriteAllText(path, sb.ToString());
            if (!silent)
                MessageBox.Show($"Exportat a:\n{path}", "Calibratge", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadCalibrationPositions()
        {
            string path = GetCalibrationFilePath();
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
                else if (type == "IMG" && parts.Length >= 5 && int.TryParse(parts[4], out int w))
                {
                    var ov = overlayImages.FirstOrDefault(o => o.Name == name);
                    if (ov != null)
                    {
                        ov.BasePos = new Point(x, y);
                        string tk = ov.TypeKey;
                        int imgH = (parts.Length >= 6 && int.TryParse(parts[5], out int h)) ? h : 0;
                        foreach (var same in overlayImages.Where(o => o.TypeKey == tk))
                        {
                            same.ZoomWidth = w;
                            if (imgH > 0) same.ZoomHeight = imgH;
                        }
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
                else if (type == "CONFBTN")
                {
                    if (_configBtn != null) _configBtn.Location = new Point(x, y);
                }
                else if (type == "CLOSEBTN")
                {
                    closeButton.Location = new Point(x, y);
                }
                else if (type == "SHOWBTN")
                {
                    _showAreasBtnPos = new Point(x, y);
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
                else if (type == "G3KBTN")
                {
                    _g3000Btn.Location = new Point(x, y);
                }
                else if (type == "G1000BTN")
                {
                    _g1000Btn.Location = new Point(x, y);
                }
                else if (type == "A320BTN")
                {
                    _a320Btn.Location = new Point(x, y);
                }
                else if (type == "A320ZOOMPLUS")
                {
                    _a320ZoomPlusPos = new Point(x, y);
                    if (_a320Overhead != null) _a320Overhead.PlusLocation = _a320ZoomPlusPos;
                }
                else if (type == "A320ZOOMMINUS")
                {
                    _a320ZoomMinusPos = new Point(x, y);
                    if (_a320Overhead != null) _a320Overhead.MinusLocation = _a320ZoomMinusPos;
                }
                else if (type == "A320PAGEBTN")
                {
                    _a320PageBtnPos = new Point(x, y);
                    if (_a320Overhead != null) _a320Overhead.PageLocation = _a320PageBtnPos;
                }
                else if (type == "A320AREASBTN")
                {
                    _a320AreasBtnPos = new Point(x, y);
                    if (_a320Overhead != null) _a320Overhead.AreasLocation = _a320AreasBtnPos;
                }
                else if (type == "THRQBTN")
                {
                    _thrQuadrantBtn.Location = new Point(x, y);
                }
                else if (type == "FSCONN" && _fsConnPanel != null)
                {
                    _fsConnPanel.Location = new Point(x, y);
                }
                else if (type == "APPVER" && _appVersionPanel != null)
                {
                    _appVersionPanel.Location = new Point(x, y);
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

        // C408 SkyCourier — recalcula seSelectorPos a partir de l'últim parell conegut (idx3, idx4) de
        // FUELSYSTEM PUMP SWITCH. (0,1)=L TANK, (1,0)=R TANK, (0,0)=FUEL TRANSFER OFF. Cridat cada vegada
        // que qualsevol dels 2 valors arriba, ja que venen per 2 sol·licituds SimConnect independents.
        private void UpdateC408SeSelectorFromPumpSwitches()
        {
            if (!IsC408()) return;
            if (_c408Idx3 == int.MinValue || _c408Idx4 == int.MinValue) return;
            int? newPos = (_c408Idx3, _c408Idx4) switch { (0, 1) => 0, (1, 0) => 2, (0, 0) => 1, _ => (int?)null };
            if (newPos.HasValue && newPos.Value != seSelectorPos && DateTime.UtcNow >= _seSelectorIgnoreFollowUntil)
            {
                seSelectorPos = newPos.Value;
                pbMain.Invalidate();
            }
        }

        private void SendSeSelectorCommand(int pos)
        {
            // C408 SkyCourier: interruptor físic FUEL_TRANSFER, sentit horari L TANK / FUEL TRANSFER OFF /
            // R TANK — pos0=L TANK, pos1=FUEL TRANSFER OFF, pos2=R TANK. Ja teníem la comanda real
            // resolta (TransmitC408FuelTransfer, FUELSYSTEM_PUMP_ON/OFF idx3/4, sense WASM) des de quan
            // encara es feia servir el truc dels 2 selectors bimotor enllaçats — ara reutilitzada tal qual
            // pel selector únic compartit.
            if (IsC408())
            {
                TransmitC408FuelTransfer(pos);
                return;
            }
            // DA-40 (2020 NOMÉS, 2026-08-28) — RESOLT: K:FUEL_SELECTOR_SET pel missatger genèric, valors
            // directes 0=OFF, 2=LEFT, 3=RIGHT (mateixa codificació que la Cirrus, confirmada via revòlver
            // SEND — sense guarda, sense WASM). 2024 continua amb el mecanisme de sota (no tocat).
            if (IsDA40() && !_isMsfs2024)
            {
                uint val = pos switch { 0 => 0u, 1 => 2u, _ => 3u };
                SendGenericRpn($"{val} (>K:FUEL_SELECTOR_SET)");
                _seSelectorIgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                return;
            }
            // DA-40 (2024 NOMÉS, 2026-08-30) — RESOLT: el Mouserect real (clic al VC) NO fa servir
            // K:FUEL_SELECTOR_SET (aquest és nomes per l'Input Events API moderna, sense efecte des de
            // fora, confirmat via revòlver) — crida 3 events B: sense valor, confirmats via revòlver +
            // Debug Output (FUELSYSTEM VALVE SWITCH:1/2 canvien de veritat). Etiquetes reals per Pere:
            // OFF / NORMAL(Left) / EMERGENCY(Right) — diferents de les de 2020 (OFF/LEFT/RIGHT).
            if (IsDA40() && _isMsfs2024)
            {
                string da40Rpn = pos switch { 0 => "(>B:FUEL_Selector_1_Off)", 1 => "(>B:FUEL_Selector_1_Left)", _ => "(>B:FUEL_Selector_1_Right)" };
                SendGenericRpn(da40Rpn);
                _seSelectorIgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                return;
            }
            //
            // C400 Corvalis (TTx): CONFIRMAT que junction=1 sí funciona (moviment real verificat al VC).
            // Options REALS del junction (confirmades una a una, NO coincideixen amb els valors dels
            // bindings de Behaviors): 1=Off, 2=Left, 3=Right (l'opció 0 no fa res). Com que necessitem
            // l'opció 3 (fora del rang 0-2 que cobreixen els 3 events DA-40 fixos), ho enviem tot via
            // l'event de prova flexible SFS_DA40_FUELSEL_TEST (codi = junction*10+opció) — ja compilat,
            // no cal tocar el WASM més. Ordre físic real en sentit horari: Off-Left-Right(-Off, redundant,
            // no la fem servir). pos0=OFF(opt1), pos1=LEFT(opt2), pos2=RIGHT(opt3).
            //
            // C172 Skyhawk (2020 NOMÉS, 2026-09-01): el mecanisme per junction de sota NO funciona al
            // Skyhawk per defecte de 2020 (avió amb títol "Cessna Skyhawk G1000 Asobo", sense "172" —
            // veure IsC172()). Confirmat per Pere via revòlver: events clàssics de SimConnect per
            // selector d'un sol motor (SDK oficial, no trobats a Behaviors ni MobiFlight):
            // K:FUEL_SELECTOR_LEFT / _ALL / _RIGHT. 2024 continua amb el mecanisme de junction de sota,
            // no tocat.
            if (IsC172() && !_isMsfs2024)
            {
                string c172Evt = pos switch { 0 => "FUEL_SELECTOR_LEFT", 1 => "FUEL_SELECTOR_ALL", _ => "FUEL_SELECTOR_RIGHT" };
                SendGenericRpn($"(>K:{c172Evt})");
                _seSelectorIgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                return;
            }
            // C172: mateix junction=1, confirmat opcions 1=Left,2=Both,3=Right (sense Off) — mateixos
            // codis numèrics que el C400 (11/12/13), només canvia el significat/etiquetes.
            // Ordre físic real en sentit horari: Left-Both-Right. pos0=LEFT(opt1), pos1=BOTH(opt2), pos2=RIGHT(opt3).
            if (IsC400() || IsC172())
            {
                uint code = pos switch { 0 => 11u, 1 => 12u, _ => 13u };
                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_TEST, code, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                _seSelectorIgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                return;
            }
            // Bonanza G36: mecanisme legacy simple, K:FUEL_SELECTOR_SET (1 sol paràmetre, ja mapejat,
            // sense WASM) — confirmat funcional (l'engine s'atura a OFF). ELS VALORS DELS BINDINGS DE
            // BEHAVIORS NO COINCIDEIXEN AMB EL VC REAL (mateixa sorpresa que el C400/C172) — confirmat
            // empíricament al VC: val=0→Off, val=1→Right, val=2→Left.
            // Ordre físic triat per Pere (igual que el C400): pos0=OFF, pos1=LEFT, pos2=RIGHT.
            if (IsBonanzaG36())
            {
                uint val = pos switch { 0 => 0u, 1 => 2u, _ => 1u };
                simconnect?.TransmitClientEvent(0, SIMCONNECT_EVENTS.FUEL_SELECTOR_SET, val, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
                _seSelectorIgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                return;
            }
            // Cirrus SR22T: K:FUEL_SELECTOR_SET pel missatger genèric confirmat OFF(val=0) i LEFT(val=2) —
            // exactament els mateixos valors que ja teníem confirmats per LECTURA (raw 0=Off, 2=Left,
            // 3=Right). RIGHT amb val=1 (l'esquema de la Bonanza) no movia res; adoptem val=3 per RIGHT,
            // igual que la lectura, ja que aquí escriptura i lectura semblen compartir la mateixa
            // codificació (a diferència de la Bonanza, on eren diferents).
            if (IsCirrusSR22())
            {
                uint val = pos switch { 0 => 0u, 1 => 2u, _ => 3u };
                SendGenericRpn($"{val} (>K:FUEL_SELECTOR_SET)");
                _seSelectorIgnoreFollowUntil = DateTime.UtcNow.AddMilliseconds(400);
                return;
            }
            var evt = pos switch { 0 => SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_NORMAL, 1 => SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_EMERGENCY, _ => SIMCONNECT_EVENTS.SFS_DA40_FUELSEL_OFF };
            simconnect?.TransmitClientEvent(0, evt, 0, SIMCONNECT_GROUPS.GROUP0, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
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
                                        double hdgReal = _headingIndicator; // ja tens HEADING INDICATOR
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
