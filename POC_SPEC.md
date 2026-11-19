# POC Spec: iocomp -> Syncfusion WinForms Consolidated Demo

Status: **IMPLEMENTED.** Phases 0-6 are done, followed by a visual-fidelity pass (section 7, phase 7).
This file is the original plan with the deviations marked inline; the current results are in `GAP_ANALYSIS.md`,
and the next steps in `docs/IMPROVEMENT_PROPOSAL.md`.
Companion to `GAP_ANALYSIS.md` (control-by-control matrix and iocomp image observations).

## 1. Goal

One demo app covering **all 56 iocomp .NET WinForms controls**, so a presale audience can see:

1. What works as-is (A).
2. What works with customization (E), with default and restyled views side by side.
3. What does not exist (N), with the best available stand-in and an effort estimate to close the gap.
4. A **gap register** from which Syncfusion can log feature requests (FR).

The customer's real usage is unknown, so the demo covers the full catalog.

## 2. Decisions (all confirmed)

| # | Topic | Decision |
|---|---|---|
| 1 | SDK | **CHANGED in Phase 2:** preview.4 was uninstalled from this machine, so `global.json` now pins the stable **10.0.404** (rollForward latestPatch). Original decision: Pin **.NET SDK 10.0.100-preview.4** in `global.json`; target `net10.0-windows`. Early check in Phase 0: restore and build a blank WinForms app with the Syncfusion packages. If it fails, report to the user and switch to SDK 11.0.100 or a stable .NET 10 install (user decides). |
| 2 | Package source | **Public NuGet.org**. Latest stable Syncfusion WinForms version that supports net10.0, pinned in one place (`Directory.Packages.props`) so an internal build can be swapped in later. |
| 3 | N controls | **Build five as real controls, describe the rest.** Built in a small `HmiControls` library: **Led, Odometer, SwitchRotary, Pipe, Valve** (simple to complex, to calibrate effort). The other N controls get a demo card (iocomp image, "No equivalent" badge, closest stand-in or placeholder) and a register entry. Effort for the five built controls is **measured** (actual days spent); effort for the rest is **my estimate**, extrapolated from them, and labelled as such. **CHANGED:** person-days could not be measured, so every effort figure, including the five built controls, stays an estimate; the register records line counts instead. |
| 4 | Visual style | **E controls show default and restyled views side by side.** A controls: default look only. N controls: no styling. **CHANGED in phase 7:** A controls may be restyled with properties only (so the A rating stays honest); N stand-ins may use Syncfusion renderer interfaces or `OnPaint` overrides to look like iocomp. Every card now shows its Approach and Look. |
| 5 | Audience | **Two outputs from one catalog:** an internal register (everything) and a customer-facing summary (coverage, stand-ins, effort only). Per-field flag decides inclusion. |

Other constraints:
- License key is applied by the user later. The app reads `SYNCFUSION_LICENSE_KEY` from the environment at startup; it is never committed. Without it the app runs with the trial banner.
- Reference images: `iocomp-dotnet-images/*.png`, copied to the build output and shown beside each Syncfusion control.
- C# latest, nullable enabled.
- Out of scope: migration adapters, customer code porting, installer, custom implementations of N controls other than the five in decision 3 (a possible second phase if the customer asks), per-control unit tests. **Partly lifted in phase 7:** seven N controls got custom-drawn stand-ins on Syncfusion controls (Tank, SlidingScale, SlidingCompass, SwitchRocker, SwitchRocker3Way, SwitchLever, plus the Odometer stand-in), not new controls.

## 3. Solution layout

```
IocompPoc.sln
global.json                          # SDK 10.0.404 (was 10.0.100-preview.4, see decision 1)
Directory.Packages.props             # central Syncfusion version
src/HmiControls/                     # class library: the five custom controls (decision 3)
  (no shared base class was needed; each control sets its own double-buffered styles)
  Led.cs, Odometer.cs, SwitchRotary.cs, Pipe.cs, Valve.cs
src/PocApp/
  Program.cs                         # license from env var, High DPI (PerMonitorV2), theme
  MainForm.cs                        # navigation + category pages
  Pages/
    # AS BUILT: one generic CategoryPage per category instead of one page class each; cards come from Cards/*.cs.
    CategoryPage.cs                  # scrollable list of ControlCards for one category
    PlotPage.cs                      # (planned) Plot, SpectrumDisplay, PercentBar, PercentPie
    GaugesPage.cs                    # Angular, Linear, Tube, Thermometer, Knob, Compass, LedBar, LedSpiral, Tank
    DigitalPage.cs                   # seven-segment variants, LcdMatrix, Odometer
    ClocksPage.cs                    # ClockAnalog, SevenSegmentClock (+Smpte)
    InputsPage.cs                    # Display*/Edit*, ModeComboBox, Label, Panel, ImageDisplay, Slider
    SwitchesPage.cs                  # Toggle, Led, Rotary, Panel, Lever, Rocker, Rocker3Way, Slider, Quad
    LedsPage.cs                      # Led, LedArrow, MatrixLeds, MatrixButtons, PhonePad
    ProcessPage.cs                   # Pipe, PipeJoint, Valve, Motor, ObjectCanvas, SlidingScale/Compass, RotationDisplay
    BenchmarkPage.cs                 # Plot streaming benchmark
    GapRegisterPage.cs               # filterable grid of the catalog, export buttons
  Infrastructure/
    ControlCard.cs                   # iocomp image | Syncfusion control(s), rating badge, approach and look chips, note
    ControlEntry.cs                  # record + Rating / Approach / Fidelity enums
    CardFactories.cs                 # name -> default / restyled view builders
    SignalSimulator.cs               # sine / noise / step / ramp, adjustable rate
    ControlCatalog.cs                # single source of truth: 56 entries
    RegisterExporter.cs              # writes the two documents below
  Cards/                             # AS BUILT: live-view builders per phase, plus the custom drawing
    Phase2Cards.cs .. Phase5Cards.cs # gauges, digital, clocks, inputs / plot / HmiControls / switches, leds, process
    GaugeRenderers.cs                # IRadialGaugeRenderer / ILinearGaugeRenderer implementations (9)
    SwitchRenderers.cs               # IToggleButtonRenderer + CheckBoxAdv OnPaint switches
    ClockRenderers.cs                # IClockRenderer (hands, numerals)
docs/
  GAP_REGISTER_INTERNAL.md           # generated
  MIGRATION_SUMMARY_CUSTOMER.md      # generated
  IMPROVEMENT_PROPOSAL.md            # next steps
  benchmark_*.csv                    # Plot streaming results
```

Design rules:
- `ControlCatalog` drives the card captions, the register page and both exported documents, so they cannot drift.
- One `ControlCard` per iocomp control. Layout depends on rating:
  - **A:** iocomp image | default Syncfusion control.
  - **E:** iocomp image | default view | restyled view.
  - **N (built):** iocomp image | "No equivalent" badge | live `HmiControls` prototype.
  - **N (not built):** iocomp image | "No equivalent" badge | stand-in if one exists, else a placeholder panel.
- **AS BUILT:** every card header also shows an Approach chip (properties only / composed / custom renderer / custom paint / new control / placeholder) and a Look chip (close / acceptable / different / n/a). Both come from `ControlCatalog` and appear as filterable columns on the Gap register page and in both exports.
- `HmiControls` follows Syncfusion conventions (properties, events, theming-friendly) so the product team can use it as a prototype for an FR, not throwaway code.
- All live controls are driven by the shared `SignalSimulator`.

## 4. Mapping per page

| Page | iocomp control | Syncfusion implementation | Rating |
|---|---|---|---|
| Plot | Plot | `ChartControl`: Line + Column + Bubble, legend, iocomp-style dark restyle (no FastLine type in 35.2.12) | E |
| Plot | SpectrumDisplay | `ChartControl` column, simulated FFT | A |
| Plot | PercentBar / PercentPie | Chart stacked column / pie | E / A |
| Gauges | GaugeAngular | `RadialGauge`, ranges, needle | A |
| Gauges | GaugeLinear | `LinearGauge`, ranges | A |
| Gauges | GaugeTube / Thermometer | `LinearGauge` + custom `ILinearGaugeRenderer` (tube, bulb) | E |
| Gauges | Knob | `RadialGauge` (ReadOnly=false) + custom `IRadialGaugeRenderer` (glossy knob) | E |
| Gauges | Compass | `RadialGauge` 0-360 + custom `IRadialGaugeRenderer` (N/E/S/W ring) | E |
| Gauges | LedBar / LedSpiral | `LinearGauge` / `RadialGauge` + custom renderers (LED segments) | E |
| Gauges | Tank | `LinearGauge` + custom `ILinearGaugeRenderer` stand-in (vessel and level) | N |
| Digital | SevenSegment Integer/Analog/Binary/Character/Hex | `DigitalGauge` plus formatting, iocomp palette via `DigitalGaugeThemeBrush` | A/E |
| Digital | LcdMatrix | `DigitalGauge` dot-matrix | E |
| Digital | Odometer | **Built:** `HmiControls.Odometer` (rolling digits); `DigitalGauge` shown as the closest stand-in | N (built) |
| Clocks | ClockAnalog | Syncfusion `Clock` + custom `IClockRenderer` (hands, numerals) | E |
| Clocks | SevenSegmentClock / Smpte | `DigitalGauge` + timer | E |
| Inputs | Display Double/Integer/String | read-only `TextBoxExt` / `Label` | A |
| Inputs | Edit Double/Integer/String | `DoubleTextBox`, `IntegerTextBox`, `TextBoxExt` | A |
| Inputs | ModeComboBox, Label, Panel, ImageDisplay | `ComboBoxAdv`, `Label`, `GradientPanel`, `PictureBox` | A |
| Inputs | Slider | `TrackBarEx` | E |
| Switches | SwitchToggle / SwitchLed | `ToggleButton` + custom `IToggleButtonRenderer` (paddle / LED caption) | E |
| Switches | SwitchRotary | **Built:** `HmiControls.SwitchRotary` (mouse/keyboard, `ValueChanged`) | N (built) |
| Switches | Panel, Lever, Rocker, Rocker3Way, Slider, Quad | Rocker: `ToggleButton` + renderer; Rocker3Way / Lever: `CheckBoxAdv` + `OnPaint`; Slider: `TrackBarEx`; Panel, Quad: placeholder | N |
| LEDs | Led | **Built:** `HmiControls.Led` (color, on/off, glossy style) | N (built) |
| LEDs | LedArrow, MatrixLeds | placeholder (optionally a colored `Panel`) | N |
| LEDs | MatrixButtons, PhonePad | `ButtonAdv` grid in `TableLayoutPanel` | E |
| Process | Pipe, Valve | **Built:** `HmiControls.Pipe` (animated flow direction/speed) and `HmiControls.Valve` (open/closed, click), combined in one small process scene | N (built) |
| Process | PipeJoint, Motor | placeholder | N |
| Process | ObjectCanvas | `Diagram` (shapes, text, rotation) | E |
| Process | SlidingScale / SlidingCompass | `LinearGauge` + custom `ILinearGaugeRenderer` (scrolling tape) | N |
| Process | RotationDisplay | needs small custom drawing, placeholder | N |

Note: RotationDisplay is rated N because it needs custom drawing and is not one of the five built controls.
Counts: 5 N controls are built, about 14 N controls are described only (grouped rows in `GAP_ANALYSIS.md` mean the exact number is confirmed when the catalog has its 56 rows).

## 5. Gap register

`ControlCatalog` entry per iocomp control. Each field has an audience flag.

| Field | Internal | Customer |
|---|---|---|
| Name, Category | yes | yes |
| Rating (A/E/N) | yes | yes |
| SyncfusionControl / stand-in | yes | yes |
| GapSummary (neutral wording) | yes | yes |
| EffortDays (measured for the 5 built controls, estimate for the rest; labelled) | yes | yes, as a range |
| Priority (P1-P3) | yes | no |
| FRCandidate, FRTitle | yes | no |
| Internal notes (candid weaknesses) | yes | no |
| Visual fidelity (Close / Acceptable / Different / n/a) - AS BUILT: scored for all 56 | yes | yes |
| Approach (properties only ... placeholder) - added in phase 7 | yes | yes |

**Internal register** groups entries as:
1. Product FR candidates (e.g. LED indicator, rotary switch, rocker/lever skins, rolling odometer, tank/valve/pipe symbol set, scrolling tape gauge, interactive RadialGauge pointer, segmented LED bar).
2. Documentation/sample gaps (control exists, how-to missing: strip-chart mode, alarm ranges, unlit segments).
3. Customer-side customization (property mapping, brand skins).

**Customer summary** has coverage counts, a per-control table (rating, Syncfusion control, gap summary, effort range) and a short list of recommended next steps. No priorities, no FR language.

Priority rule: P1 = common in HMI/instrument apps and blocks migration; P2 = visible but has a workable stand-in; P3 = niche. Priorities and FR flags are my proposals; Syncfusion product owners decide.

## 6. Plot benchmark

- `BenchmarkPage`: series count (1/4/8), points per second (10/100/1000/10000), window size, series type (FastLine vs Line), mode (rolling strip / sweep).
- Measure achieved refresh rate, dropped updates, process CPU %, working set after 60 s. Show live, write CSV.
- Default pass target: 8 series x 1000 pts/s at >= 30 fps. This is a placeholder until the customer's iocomp rate is known.
- Mitigations to try on failure: batched updates, `BeginUpdate/EndUpdate`, fixed axis ranges, FastLine, no animation/anti-alias, decimation. Each failure goes in the register.

## 7. Phases and acceptance

| # | Phase | Output | Acceptance |
|---|---|---|---|
| 0 | Prereqs **(DONE)**: SDK 10.0.100-preview.4 builds net10.0-windows; Syncfusion 35.2.12 (Chart, Gauge, Tools, Shared, Diagram) restored from NuGet.org; smoke app launched. Added repo `nuget.config` (nuget.org only) because the user-level config has Avalonia feeds returning 401. | `global.json`, confirmed package IDs/versions | Blank WinForms app with Syncfusion packages restores and builds on preview.4 SDK for net10.0-windows. If not, stop and ask. |
| 1 | Scaffold **(DONE: 56 rows, not 56; app launches)** | Solution, shell, `ControlCard`, `SignalSimulator`, `ControlCatalog` (56 rows), `RegisterExporter` | App starts; register page lists 56 rows |
| 2 | A and simple E **(DONE for Gauges, Digital, Clocks, Inputs; verified from snapshots, interaction untested)** | Inputs, Gauges, Digital, Clocks | Each card shows live control(s) next to the iocomp image; E cards show default and restyled |
| 3 | Plot and benchmark **(DONE with caveats: Plot/Spectrum/PercentPie verified from snapshots, PercentBar restyled view still broken, sweep mode not implemented, benchmark = 12 runs x 6 s, default and ImprovePerformance, results in docs/)** | Plot page, Spectrum, Percent*, BenchmarkPage | CSV written; numbers in register |
| 4 | **(DONE: all five built and scripted-tested. Deviation: person-days cannot be measured here, so the register records lines of code and test results and keeps effort labelled "estimate".)** HmiControls (five) | `Led`, `Odometer`, `SwitchRotary`, `Pipe`, `Valve` in `HmiControls`, hosted on their cards and in one process scene | Each control animates/responds to input and raises its events; actual days spent recorded per control |
| 5 | **(DONE: all 56 catalog entries now have a card; new E cards verified by render only; register exports generated into docs/)** Remaining N cards | Badge, stand-in or placeholder, and register entry for every other N control | Every N card has one of each; estimates extrapolated from the measured five |
| 6 | **(DONE: GAP_ANALYSIS.md rewritten as the final report; PercentBar fixed; interaction tested with synthetic input only)** Register and report | Both generated documents, updated `GAP_ANALYSIS.md` with verified ratings and screenshots | No "(verify)" left; effort source (measured vs estimate) labelled on every N entry |
| 7 | **(DONE)** Visual-fidelity pass | Prototypes redrawn from iocomp pixel samples; Plot, gauges, switches, digital and clocks restyled; Tank and sliding tapes as LinearGauge + renderer; Approach and Look per control on cards, register page and exports; snapshots and docs regenerated | Every card scored for look. **Open:** second reviewer for the scores; real-mouse re-test of Knob and new switches; High DPI |

Each phase ends with a clean `dotnet build` and the app launched. Work proceeds one page at a time.

## 8. Verification

- `dotnet build -c Release`, no warnings from our code.
- Launch and walk every page. All 56 entries have a card; each is a live control, a built prototype, a stand-in, or a placeholder with a reason.
- High DPI at 100/150/200%.
- Benchmark CSV for the section 6 matrix.
- Each card marked for visual fidelity against its iocomp image. **DONE (phase 7):** 31 close, 11 acceptable, 7 different, 7 n/a; one reviewer, 100% scaling.
- Customer summary reviewed to confirm it contains no internal fields.

## 9. Risks

- **Preview SDK:** preview.4 may not work with current Syncfusion packages built for final .NET 10. Phase 0 tests this first; fallback options are SDK 11.0.100 or a stable .NET 10 install.
- **Unverified ratings:** several E ratings (DigitalGauge dot-matrix and unlit segments, RadialGauge interactive pointer, LinearGauge tube/bulb styling, Clock styling) are assumptions until built. **RESOLVED:** all built and verified by render; tube/bulb and Clock needed custom renderers.
- **Effort estimates for unbuilt N controls are extrapolated** from five built ones. Label them clearly and give ranges to the customer.
- **Weaker demo for unbuilt N controls:** about 14 show stand-ins or placeholders, not working controls. A second phase can build more if the customer asks. **REDUCED:** 7 now have close-looking stand-ins; 7 placeholders remain.
- **Look of the built controls:** matching iocomp's glossy style costs more drawing time. Decide per control how close to go, and record that in the effort figure.
- **Visual fidelity:** iocomp is glossy and skeuomorphic; Syncfusion themes are flatter. Set expectations as "equivalent function, not pixel identical".
- **Scope:** 56 cards is large. Phase order puts the highest-risk items (Plot, gauges) first.
- **Package IDs and versions** are unconfirmed until Phase 0. **RESOLVED:** Syncfusion 35.2.12 from NuGet.org, pinned in `Directory.Packages.props`.

## 10. Open questions

None blocking. Pending: Syncfusion license key (applied by the user later), the customer's real data rate (replaces the benchmark default when known), and which controls the customer actually uses (drives the second phase in `docs/IMPROVEMENT_PROPOSAL.md`).
