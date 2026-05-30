# panelAIR1000 — panelAIR1000-j

## Propòsit
Aplicació Windows Forms en C# que connecta hardware físic personalitzat (RotAIR) amb Microsoft Flight Simulator 2020 (MSFS) via SimConnect. Funciona com a pont entre botons/encoders físics i events del cockpit virtual.

## Arquitectura
- **Llenguatge**: C# (.NET 6.0-windows7.0), x64 únic
- **UI**: Windows Forms (single-form app)
- **Fitxer principal**: `panelAIR1000/CSharpSimConnect/Form1.cs` (~3.651 línies)
- **Entry point**: `Program.cs`
- **Solució VS**: `panelAIR1000/panelAIR1000.sln`

## Dependències clau
| Llibreria | Propòsit |
|-----------|----------|
| `Microsoft.FlightSimulator.SimConnect.dll` | SDK oficial MSFS — rep/envia dades al simulador |
| `HidSharp v2.1.0` | Llegeix dispositius HID (botons, encoders del hardware RotAIR) |
| `FSUIPCClient.dll` | Client FSUIPC (interfície alternativa MSFS) |

## Patrons de codi importants
- **5 threads HID paral·lels** (`hidThread1`–`hidThread5`) per llegir dispositius físics simultàniament
- **Event-driven**: SimConnect events + HID events + UI events + timers
- **Custom graphics**: mètodes de dibuix propis per PFD/MFD (trim, gear, llums, triangles)
- **Connexió/reconexió**: lògica de retry per SimConnect

## Dispositius suportats
- Type 1 GC, Type 1 ANA
- Type 2 GC, Type 2 ANA, Type 2 GC AM (Audio Module)

## Fitxers de referència
- `vars.txt` — mapatge events SimConnect ↔ accions (G1000, autopilot, etc.)
- `llista Offsets.xlsx` — offsets del hardware
- `errors.docx` — log d'errors documentats

## Com compilar
Visual Studio → obrir `panelAIR1000.sln` → configuració x64 (Debug o Release) → Build.
Output: `bin/x64/Debug/panelAIR1000.exe`

## Estat del projecte (abril 2026)
- En desenvolupament actiu (última modificació: 19 abril 2026)
- **Sense git configurat** (hi ha `.gitignore` però no repositori inicialitzat)

---

> **Recorda, Pere:** Si en algun moment de la conversa hem après o decidit alguna cosa important, digues a Claude: *"guarda això a les memòries"*. Claude ho intentarà fer sol de manera proactiva, però el teu avís ajuda a no perdre res clau.
