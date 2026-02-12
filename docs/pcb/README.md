# PCB Drafts (KiCad)

These PCB files are **placement skeletons** only. They include board outline, mounting holes, and connector footprints laid out for a 100x70mm board.

Important: there are **no nets or routed traces** in these files yet, so headers are not connected.

Open in KiCad and complete:
- Assign nets (from schematic)
- Place remaining footprints (buck modules, ESP32 headers, relay, etc.)
- Route traces + pour GND
- DRC and update footprints as needed

Files:
- `pump-station.kicad_pcb`
- `level-station.kicad_pcb`

Notes:
- Mounting holes: 4x M3 (3.2mm)
- Connectors: 5.08mm screw terminals for 12V/pump, 2.54mm pin headers for sensors
- ESP32: dual 1x19 2.54mm headers (DevKitC-1 style)
