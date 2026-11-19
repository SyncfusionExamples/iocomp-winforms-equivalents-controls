# iocomp .NET WinForms -> Syncfusion WinForms: POC Report

Status: POC complete, with a visual-fidelity pass and a second pass that closed the remaining visual gaps and added repeatable checks.
Syncfusion WinForms 35.2.12, .NET 10 (SDK 10.0.404), NuGet.org packages, no license key applied.

Per-control detail lives in `src/PocApp/Infrastructure/ControlCatalog.cs` (single source of truth) and is exported to `docs/`:
- `docs/GAP_REGISTER_INTERNAL.md` (Syncfusion only: priorities, FR candidates, candid notes)
- `docs/MIGRATION_SUMMARY_CUSTOMER.md` (safe to share: rating, Syncfusion control, approach, look, gap, effort range)
- `docs/INTERACTION_SELFTEST.md` (scripted interaction checks, 15 of 15 passing)
- `docs/benchmark_default.csv`, `docs/benchmark_improveperformance.csv` (chart streaming numbers)
- `docs/IMPROVEMENT_PROPOSAL.md` (what was done from it, and what is left)
- `snapshots/*.png` (one rendered page per category; also the baseline for the visual regression check)

## 1. Scope and how it was checked

The iocomp .NET WinForms catalog has **56 controls** (one per reference image). Every control has a card in the demo app: the iocomp image beside the live Syncfusion control, a restyled view for extend-rated controls, and a working prototype or a custom-drawn stand-in for every control Syncfusion does not have. **There are no placeholders left.**

Each card header shows three indicators, and the Gap register page has them as filterable columns:
- **Rating** (A / E / N): what Syncfusion offers.
- **Approach**: how the live view was achieved, from least to most custom code: properties only, composed (several controls together), custom renderer (Syncfusion's documented renderer interface), custom paint (a Syncfusion control with `OnPaint` overridden), new control (from scratch in `HmiControls`), placeholder.
- **Look**: how close the best live view is to the iocomp image: close, acceptable, different, or n/a.

What "checked" means here, because it matters when quoting results:

| Check | How | Status |
|---|---|---|
| Rendering | `PocApp.exe --snapshots <folder>`: every card rendered and compared side by side with the iocomp image, at 100% scaling. Colours sampled from the iocomp PNGs where it helped. | Done for all 56 |
| Look scores | One reviewer's judgement from those snapshots, not a measurement. | **Needs a second reviewer** |
| Visual regression | `PocApp.exe --compare snapshots <folder>`: renders with fixed signals, fixed clock time and animations off, then compares each page with `snapshots/` (a pixel changes when a channel differs by more than 24; a page fails above 0.5%). Writes `COMPARE.md` and magenta diff images; exit code 1 on failure. | Two runs give 0% difference on all 10 pages. A deliberately damaged baseline fails as expected (17% changed). |
| Interaction | `PocApp.exe --selftest <file.md>`: 15 scripted checks call the controls' own mouse and keyboard handlers on a hidden form and assert state and events (Knob drag, all 10 switches, Led, MatrixLeds, Valve, Motor). | 15 of 15 pass. This proves the wiring, **not how the controls feel with a real mouse**. |
| Not tested at all | High DPI (125/150/200%), screen readers/accessibility, themes beyond those listed, behaviour with a valid license key, the real iocomp Plot's performance. | Open |

## 2. Result

| Rating | Count | Meaning |
|---|---|---|
| A | 15 | Works as-is with configuration |
| E | 22 | Works with customization or restyling |
| N | 19 | No equivalent in Syncfusion today: 13 built as prototypes, 6 with a custom-drawn stand-in on a Syncfusion control |

| Look | Count | | Approach | Count |
|---|---|---|---|---|
| close | 41 | | properties only | 25 |
| acceptable | 15 | | composed | 3 |
| different | 0 | | custom renderer | 13 |
| n/a | 0 | | custom paint | 2 |
| | | | new control | 13 |
| | | | placeholder | 0 |

The headline: **all 56 controls have a live view, 41 look close and 15 acceptable.** Of the 43 controls that did not need a new control, every one was done with Syncfusion's own properties, renderer interfaces or theme brushes.

### What each pass changed

All changes are on the restyled or stand-in view; every default view is unchanged.

**Fidelity pass (first):**

| Area | Controls | How |
|---|---|---|
| First prototypes | Led, Odometer, SwitchRotary, Pipe, Valve | Redrawn from iocomp pixel samples. |
| Plot | Plot | ChartControl properties only: black plot, green grid, "0.0" labels, legend on top, red line / green columns / blue bubbles, scrolling X window. |
| Gauges | GaugeTube, Thermometer, Knob, Compass, LedBar, LedSpiral | `IRadialGaugeRenderer` / `ILinearGaugeRenderer`. GaugeAngular/GaugeLinear stayed properties-only so their A rating is honest. |
| Gauge-shaped N | Tank, SlidingScale, SlidingCompass | LinearGauge + custom `ILinearGaugeRenderer` (the gauge keeps Value/Min/Max). |
| Switches | SwitchToggle, SwitchLed, SwitchRocker | ToggleButton + custom `IToggleButtonRenderer`. |
| Switches (N) | SwitchRocker3Way, SwitchLever | CheckBoxAdv with `OnPaint` overridden (no renderer interface exists). |
| Digital, clocks | seven-segment variants, LcdMatrix, ClockAnalog | `DigitalGaugeThemeBrush` with the sampled palette; `Clock.Renderer` with tapered hands. |

**Second pass (this round):**

| Area | Controls | How | Look |
|---|---|---|---|
| Former "different" | DisplayDouble / Integer / String | Cyan-on-black `TextBoxExt` (BackColor/ForeColor/BorderColor). Properties only. | different -> close |
| | PercentBar | One `StackingColumn100` column with PhongCylinder shading, legend right, axes hidden. | different -> acceptable |
| | Slider | Vertical `TrackBarEx` beside an owner-drawn "0.0".."100.0" scale panel (composed). | different -> close |
| | ObjectCanvas | Diagram `TextNode`, rectangle and a dashed circle rotated by the simulator; grid and scroll bars off. | different -> acceptable |
| | SwitchSlider | New `HmiControls.SwitchSlider` replaces the TrackBarEx stand-in. | different -> close |
| Former placeholders | LedArrow, MatrixLeds, SwitchQuad, SwitchPanel, Motor | New `HmiControls` prototypes. | n/a -> close |
| | PipeJoint, RotationDisplay | New `HmiControls` prototypes. | n/a -> acceptable |
| Behaviour | SwitchRocker3Way | Pressing the top or bottom half now steps one position toward it (as a real rocker); Up/Down keys too. | close |

**Code size:** `src/HmiControls` now holds 13 prototypes in 1,371 lines (Led 86, LedArrow 71, MatrixLeds 96, Motor 104, Odometer 132, Pipe 126, PipeJoint 55, RotationDisplay 53, SwitchRotary 230, Valve 129, and `SelectorSwitches.cs` 281 for SwitchQuad, SwitchPanel and SwitchSlider on one shared base, plus an 8-line animation switch). Renderers in `src/PocApp/Cards/`: GaugeRenderers 454, SwitchRenderers 248, ClockRenderers 58.

**Still acceptable rather than close (15), and why:** SpectrumDisplay and PercentPie (flat Syncfusion styling, no 3D shading), PercentBar (legend order follows stacking order; no option found), GaugeAngular (short needle) and GaugeLinear ("20" labels, no format event), Tank (rectangular interior), LcdMatrix (character cells, not one dot field), ClockAnalog (Syncfusion's square shadow shows inside the face), Label and Panel (Syncfusion defaults), MatrixButtons and PhonePad (Syncfusion buttons, not iocomp's flat keys), ObjectCanvas (no open-arc spinner), PipeJoint (straight sections only, no elbows or tees), RotationDisplay (rotates a sample image, not iocomp's artwork).

## 3. The main technical risk: real-time plotting

Unchanged by either pass. `ChartControl` in 35.2.12 has no FastLine series type; `Line` was used. Release build, 6 s per run, one 8-logical-processor machine, one run per cell, no warm-up, 2000-point window. UI frames per second, default / with `ImprovePerformance`:

| Series | 10 pts/s | 100 pts/s | 1000 pts/s | 10000 pts/s |
|---|---|---|---|---|
| 1 | 10.2 / 13.5 | 51 / 62 | 21 / 32 | 6.9 / 10.0 |
| 4 | 10.1 / 13.5 | 31 / 46 | 5.8 / 12.9 | 1.3 / 2.0 |
| 8 | 10.2 / 13.5 | 20 / 35 | 2.6 / 7.2 | 0.8 / 0.5 |

(Rates are per series; the 10 pts/s column is limited by the data rate itself, not the chart.)
Light loads are fine. Many series at 1000+ points/s were slow, and one run stalled the UI (a 6 s test took 15.6 s). Treat these as rough and re-measure before quoting. Whether this matters depends on the customer's real data rates, which we do not have. This is still the first thing to ask them.

## 4. Gap register highlights (internal)

Candidate feature requests, in the catalog's proposed priority (product owners decide):
- **P1:** interactive pointer/knob on RadialGauge; LED indicator; rotary switch; process pipe and valve symbols; strip-chart/sweep mode for ChartControl.
- **P2:** custom scale text labels on RadialGauge; segmented LED bar mode on LinearGauge; tank/vessel indicator; scrolling tape gauges; motor, pipe joint; remaining switch types; rolling odometer.

Every one of these can now be **shown** in the demo, either through Syncfusion's renderer interfaces or with an `HmiControls` prototype. The FR is therefore about making the look a supported option (a property, style or control) instead of code each customer writes. The renderers and prototypes are a ready starting point.

Findings for Syncfusion (docs, samples or possible bugs):
- **Clock:** draws a square drop shadow behind the round face, which still shows inside the face even with a circular `Region`. It also does not size itself to its container, and reads its own clock (use `ShowCustomTimeClock` + `CustomTime` + `StopTimer` for a fixed time).
- **IClockRenderer:** `ClockRenderer`'s hand methods are not virtual, so a custom renderer must re-implement the interface instead of overriding.
- **CheckBoxAdv** has no renderer interface (ToggleButton, RadialGauge, LinearGauge and Clock do), so custom switch looks need an `OnPaint` override, and per-region clicks need `AutoCheck = false`.
- **RadialGauge:** a mouse press before the first paint is ignored (its geometry is computed while painting). Harmless in an app, but it tripped the scripted test until the gauge was painted first.
- **ChartControl:** with no series it draws random sample columns at run time (the benchmark tab showed different bars on every launch; fixed with an empty series). Reading `series.LegendItem` before `Series.Add` throws `NullReferenceException`. `ChartLegendItemType.StraightLine` had no visible effect. No legend-order option was found for stacked series.
- **DigitalGauge:** `:` and `.` take a character cell, so `CharacterCount` must include separators or the text is cut off silently.
- **Horizontal bar charts:** `PrimaryXAxis` must be set to a Double value axis; leaving it as Category silently squashes the bars.
- **Namespaces:** `LabelPlacement`, `TickPlacement` and `Action` collide between `...Gauge`, `...Tools` and `System`; `LinearGauge.GaugeBaseColor` is obsolete (use `ScaleColor`).
- **Trial license dialog** is modal and blocks automated runs (one ad-hoc probe hung on it this round).
- **Help site:** several guessed URLs return 404; the docs' GitHub source (`syncfusion-content/WindowsForms-docs`) was the reliable way to find pages.

## 5. Effort figures: read this before sharing

All effort ranges are **estimates**. Nothing was timed. Line counts give a sense of scale, but everything is prototype quality: no theming, no accessibility work beyond names and focus cues, no High DPI testing, simplified behaviour (horizontal pipes only, straight pipe joints, simplified odometer carry, display-only tapes).

Two data points from the passes:
- **Reusable renderers were cheap.** Once the shared helpers existed, each further gauge or switch renderer was roughly 30-80 lines.
- **New controls were cheap when they shared a base.** The eight second-pass prototypes total 660 lines; the three selector switches share one 80-line base for items, selection, events and keyboard. Production versions (designer support, theming, accessibility, tests) will still cost several times more; see `docs/IMPROVEMENT_PROPOSAL.md` track 3.

## 6. Presentation guide

1. Open the **Gap register** tab: the summary lines show 15 / 22 / 19, 41 close / 15 acceptable, and no placeholders. Filter **Approach = New control** to show the 13 prototypes.
2. **Gauges / Digital / Clocks**: default vs restyled side by side. Point at the Approach chip: most restyles are Syncfusion's own renderer interfaces, not new controls.
3. **Plot**, then **Benchmark**: show the restyled chart, then be upfront about streaming limits and ask for their real rates.
4. **Switches / Leds / Process**: click the switches (each card shows the selected state), press the top and bottom of the 3-way rocker, click MatrixLeds cells, close the valve and watch the outlet pipe stop, watch the motor run with the step signal.
5. Share only `MIGRATION_SUMMARY_CUSTOMER.md`. Keep `GAP_REGISTER_INTERNAL.md` inside Syncfusion.
6. Say plainly what was not verified (section 1): look scores are one person's judgement, interaction was scripted rather than tried with a real mouse, and High DPI was not tested.

Run it: `dotnet build IocompPoc.sln`, then `src/PocApp/bin/Debug/net10.0-windows/PocApp.exe`. Options:

| Option | Does |
|---|---|
| `--snapshots <folder>` | Renders every page to PNG (repeatable: fixed signals, fixed time, animations off) |
| `--compare <baseline> <folder>` | Renders into `<folder>`, compares with `<baseline>`, writes `COMPARE.md` and diff images, exit code 1 on failure |
| `--selftest <file.md>` | Runs the 15 scripted interaction checks, exit code 1 on failure |
| `--export <folder>` | Writes both register documents |
| `--benchmark <csv> [--improve]` | Runs the plot streaming matrix |

Set `SYNCFUSION_LICENSE_KEY` to remove the trial dialog. To accept intended look changes, re-run `--snapshots snapshots` and commit the new baseline.

The project is now a git repository (`.gitignore` excludes `bin/`, `obj/`, `.vs/`, `snapshots_current/`).

## 7. Open items

- Get the customer's real control usage and data rates, then re-prioritize.
- Apply the license key and rerun everything, including the benchmark.
- Have a second person review the look scores.
- Try the interactive cards with a real mouse and keyboard; test High DPI (125/150/200%) and accessibility.
- Raise the section 4 findings with the product teams.
- If the customer wants to keep any of it: move renderers and prototypes into a reusable library with designer support, theming and tests (proposal track 3).
