using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.FlightSimulator.SimConnect;

namespace SimVarWatcher
{
    public class MainForm : Form
    {
        private const int WM_USER_SIMCONNECT = 0x0402;

        // Mateix canal que SendGenericRpn de panelAIR1000 (Form1.cs) — permet enviar RPN de prova
        // sense haver de clicar res dins de panelAIR1000.exe.
        private const string CmdFilePath = @"C:\Users\usuario\AppData\Local\Temp\claude\c--Users-usuario-Desktop-panelAIR1000-j\0a6ec016-2204-4bf5-8292-5fa4ead20d38\scratchpad\simvarwatcher_cmd.txt";
        private const string LogFilePath = @"C:\Users\usuario\AppData\Local\Temp\claude\c--Users-usuario-Desktop-panelAIR1000-j\0a6ec016-2204-4bf5-8292-5fa4ead20d38\scratchpad\simvarwatcher_log.txt";

        private enum DEFINITIONS { Watch, GenericRpnClientDataDef }
        private enum REQUESTS { Watch }
        private enum CLIENT_DATA_ID { GenericRpnData }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct GenericRpnStruct
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 1024)]
            public string rpn;
        }

        private struct WatchStruct
        {
            public double generalEngStarter1;
            public double generalEngStarterActive1;
            public double turbEngIgnitionSwitchEx1_1;
            public double xmlvarStarter1;
            public double generalEngCombustion1;
            public double propRpm1;
            public double fuelsystemValveSwitch1;
            public double fuelsystemValveSwitch4;
        }

        private SimConnect? simconnect;
        private readonly TextBox log;
        private readonly Label status;
        private readonly System.Windows.Forms.Timer reconnectTimer;
        private readonly System.Windows.Forms.Timer cmdPollTimer;
        private int _rpnSeq = 0;
        private bool _clientDataReady = false;

        public MainForm()
        {
            Text = "SimVarWatcher — Caravan starter/ignition";
            Width = 900;
            Height = 600;

            status = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Text = "Connectant a MSFS...",
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            log = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new System.Drawing.Font("Consolas", 10),
                WordWrap = false
            };

            Controls.Add(log);
            Controls.Add(status);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogFilePath)!);
                File.WriteAllText(CmdFilePath, "");
            }
            catch { }

            reconnectTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            reconnectTimer.Tick += (s, e) => TryConnect();
            reconnectTimer.Start();

            cmdPollTimer = new System.Windows.Forms.Timer { Interval = 500 };
            cmdPollTimer.Tick += (s, e) => PollCommandFile();
            cmdPollTimer.Start();

            TryConnect();
        }

        private void TryConnect()
        {
            if (simconnect != null) return;

            try
            {
                simconnect = new SimConnect("SimVarWatcher", this.Handle, WM_USER_SIMCONNECT, null, 0);

                simconnect.OnRecvOpen += (s, d) =>
                {
                    status.Text = "Connectat a MSFS";
                    status.ForeColor = System.Drawing.Color.DarkGreen;
                };

                simconnect.OnRecvQuit += (s, d) =>
                {
                    status.Text = "MSFS tancat — reintentant...";
                    status.ForeColor = System.Drawing.Color.DarkRed;
                    _clientDataReady = false;
                    try { simconnect?.Dispose(); } catch { }
                    simconnect = null;
                };

                simconnect.OnRecvException += (s, d) =>
                {
                    AppendLog($"[EXCEPTION] {d.dwException} (Index={d.dwIndex})");
                };

                simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;

                simconnect.AddToDataDefinition(DEFINITIONS.Watch, "GENERAL ENG STARTER:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Watch, "GENERAL ENG STARTER ACTIVE:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Watch, "TURB ENG IGNITION SWITCH EX1:1", "Enum", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Watch, "L:XMLVAR_Starter1", "Number", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Watch, "GENERAL ENG COMBUSTION:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Watch, "PROP RPM:1", "Rpm", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Watch, "FUELSYSTEM VALVE SWITCH:1", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.Watch, "FUELSYSTEM VALVE SWITCH:4", "Bool", SIMCONNECT_DATATYPE.FLOAT64, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<WatchStruct>(DEFINITIONS.Watch);

                simconnect.RequestDataOnSimObject(REQUESTS.Watch, DEFINITIONS.Watch, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);

                // Mateix canal genèric que panelAIR1000 (SendGenericRpn) — el WASM SFS_Link ja el té
                // registrat, no cal tocar-lo ni recompilar-lo.
                simconnect.MapClientDataNameToID("SFS_LINK.GENERIC_RPN", CLIENT_DATA_ID.GenericRpnData);
                simconnect.AddToClientDataDefinition(DEFINITIONS.GenericRpnClientDataDef, 0, 1024, 0, SimConnect.SIMCONNECT_UNUSED);
                simconnect.RegisterDataDefineStruct<GenericRpnStruct>(DEFINITIONS.GenericRpnClientDataDef);
                _clientDataReady = true;

                AppendLog("--- Connectat. Variables vigilades: STARTER, STARTER ACTIVE, TURB IGNITION SWITCH, XMLVAR_Starter1, COMBUSTION, PROP RPM ---");
                AppendLog($"--- Canal de comandes actiu: escriu un RPN a {CmdFilePath} i s'enviarà automàticament ---");
            }
            catch (COMException)
            {
                simconnect = null;
                _clientDataReady = false;
                status.Text = "MSFS no trobat — reintentant cada 3s...";
                status.ForeColor = System.Drawing.Color.DarkOrange;
            }
        }

        private void PollCommandFile()
        {
            if (!_clientDataReady || simconnect == null) return;
            string rpn;
            try
            {
                rpn = File.ReadAllText(CmdFilePath).Trim();
                if (string.IsNullOrEmpty(rpn)) return;
                File.WriteAllText(CmdFilePath, "");
            }
            catch { return; }

            string fullRpn = $"{rpn} {_rpnSeq++ % 1000}";
            try
            {
                simconnect.SetClientData(CLIENT_DATA_ID.GenericRpnData, DEFINITIONS.GenericRpnClientDataDef,
                    SIMCONNECT_CLIENT_DATA_SET_FLAG.DEFAULT, 0, new GenericRpnStruct { rpn = fullRpn });
                AppendLog($">>> RPN ENVIAT: {fullRpn}");
            }
            catch (Exception ex)
            {
                AppendLog($"[ERROR enviant RPN] {ex.Message}");
            }
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID != (uint)REQUESTS.Watch) return;

            var v = (WatchStruct)data.dwData[0];
            AppendLog($"STARTER={v.generalEngStarter1:0.00}  STARTER_ACTIVE={v.generalEngStarterActive1:0.00}  " +
                      $"TURB_IGN_SWITCH={v.turbEngIgnitionSwitchEx1_1:0.00}  XMLVAR_Starter1={v.xmlvarStarter1:0.00}  " +
                      $"COMBUSTION={v.generalEngCombustion1:0.00}  PROP_RPM={v.propRpm1:0.0}  " +
                      $"FUELVALVE1={v.fuelsystemValveSwitch1:0.00}  FUELVALVE4={v.fuelsystemValveSwitch4:0.00}");
        }

        private void AppendLog(string line)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => AppendLog(line))); return; }
            string stamped = $"[{DateTime.Now:HH:mm:ss.fff}] {line}";
            log.AppendText(stamped + "\r\n");
            try { File.AppendAllText(LogFilePath, stamped + Environment.NewLine); } catch { }
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_USER_SIMCONNECT && simconnect != null)
            {
                try { simconnect.ReceiveMessage(); }
                catch { }
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try { simconnect?.Dispose(); } catch { }
            base.OnFormClosing(e);
        }
    }
}
