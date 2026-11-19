# iocomp .NET WinForms -> Syncfusion WinForms: POC Report

Status: POC complete, including a visual-fidelity pass. Syncfusion WinForms 35.2.12, .NET 10 (SDK 10.0.404), NuGet.org packages, no license key applied.
Per-control detail lives in `src/PocApp/Infrastructure/ControlCatalog.cs` (single source of truth) and is exported to `docs/`:
- `docs/GAP_REGISTER_INTERNAL.md` (Syncfusion only: priorities, FR candidates, candid notes)
- `docs/MIGRATION_SUMMARY_CUSTOMER.md` (safe to share: rating, Syncfusion control, approach, look, gap, effort range)
- `docs/benchmark_default.csv`, `docs/benchmark_improveperformance.csv` (chart streaming numbers)
- `docs/IMPROVEMENT_PROPOSAL.md` (what to do next, in order)
- `snapshots/*.png` (one rendered page per category, regenerated after the fidelity pass)

## 1. Scope and how it was checked

The iocomp .NET WinForms catalog has **56 controls** (one per reference image). Every control has a card in the demo app: the iocomp image beside the Syncfusion control, a restyled view for extend-rated controls, a built prototype for five missing controls, and a stand-in or placeholder for the rest.

Each card header now shows three indicators, and the Gap register page has the same three as filterable columns:
- **Rating** (A / E / N): what Syncfusion offers.
- **Approach**: how the live view was achieved, from least to most custom code:
  properties only, composed (several controls together), custom renderer (Syncfusion's documented renderer interface), custom paint (a Syncfusion control with `OnPaint` overridden), new control (from scratch in `HmiControls`), placeholder.
- **Look**: how close the best live view is to the iocomp image: close, acceptable, different, or n/a (placeholder).

What "checked" means here, because it matters when quoting results:
- **Rendering:** every card was viewed in a rendered snapshot (`PocApp.exe --snapshots <folder>`) at 100% scaling and compared side by side with the iocomp image. Where it helped, colours were sampled from the iocomp PNGs pixel by pixel.
- **Look scores are one reviewer's judgement** from those snapshots, not a measurement. Treat them as a starting point for a second opinion.
- **Interaction:** only synthetic mouse messages and scripted calls, never a person with a mouse. Tested earlier: Knob click, ToggleButton, ButtonAdv, TrackBarEx, and the five built controls' events. **Not re-tested after the fidelity pass:** Knob (now custom-rendered), the new switch renderers (Toggle, LED, Rocker, Rocker3Way, Lever).
- **Not tested at all:** High DPI (125/150/200%), screen readers/accessibility, themes beyond those listed, behaviour with a valid license key, and the real iocomp Plot's performance (so there is no like-for-like comparison).

## 2. Result

| Rating | Count | Meaning |
|---|---|---|
| A | 15 | Works as-is with configuration |
| E | 22 | Works with customization or restyling |
| N | 19 | No equivalent in Syncfusion today (5 built as prototypes, 7 with a custom-drawn stand-in, 7 placeholders) |

| Look | Count | | Approach | Count |
|---|---|---|---|---|
| close | 31 | | properties only | 27 |
| acceptable | 11 | | composed | 2 |
| different | 7 | | custom renderer | 13 |
| n/a (placeholder) | 7 | | custom paint | 2 |
| | | | new control | 5 |
| | | | placeholder | 7 |

The headline: **49 of 56 controls now have a live view, and 42 of them look close or acceptable.** Most of that came from Syncfusion's own extension points (renderer interfaces and theme brushes), not from new controls.

**What changed in the fidelity pass** (all on the restyled or stand-in view; every default view is unchanged):

| Area | Controls | How |
|---|---|---|
| Built prototypes | Led, Odometer, SwitchRotary, Pipe, Valve | Redrawn from iocomp pixel samples (drum gradients, teal pipe shading, bow-tie valve, leader-line rotary layout). |
| Plot | Plot | ChartControl properties only: black plot, white axes, green grid, "0.0" labels, legend on top, red line / green columns / blue bubbles, scrolling X window. |
| Gauges | GaugeTube, Thermometer, Knob, Compass, LedBar, LedSpiral | `IRadialGaugeRenderer` / `ILinearGaugeRenderer`: glass tube, bulb, glossy knob, N/E/S/W ring, LED segments. GaugeAngular/GaugeLinear stayed properties-only so their A rating is honest. |
| Tank, SlidingScale, SlidingCompass (N) | | LinearGauge + custom `ILinearGaugeRenderer`. The gauge keeps Value/Min/Max; the renderer draws the vessel or the scrolling tape. |
| Switches | SwitchToggle, SwitchLed, SwitchRocker | ToggleButton + custom `IToggleButtonRenderer`. ToggleButton keeps state, click/keyboard and `ToggleStateChanged`. |
| Switches (N) | SwitchRocker3Way, SwitchLever | CheckBoxAdv (Tristate for the 3-way) with `OnPaint` overridden, because CheckBoxAdv has no renderer interface. |
| Digital | all seven-segment variants, LcdMatrix | `DigitalGaugeThemeBrush` with iocomp's sampled palette (black face, 00FF00 lit, 007F00 unlit, square corners; teal/cyan for LcdMatrix). |
| Clocks | ClockAnalog | `Clock.Renderer` with a custom `IClockRenderer` (tapered shaded hands, 1-12 numerals). Seven-segment clocks use the digital palette. |

Custom drawing code added: `Cards/GaugeRenderers.cs` 454 lines (9 renderers sharing helpers), `Cards/SwitchRenderers.cs` 217, `Cards/ClockRenderers.cs` 58. The five prototypes in `src/HmiControls` total 702 lines (Led 86, Odometer 131, SwitchRotary 230, Pipe 126, Valve 129).

**Still different (7):** PercentBar (iocomp's single 3D column vs quarterly stacked bars), DisplayDouble/Integer/String (only default colours differ: one property each), Slider and SwitchSlider (TrackBarEx has no tick labels or switch body), ObjectCanvas (Diagram is much heavier; text, spinner and animation not tried).

**Placeholders (7):** SwitchPanel, SwitchQuad, LedArrow, MatrixLeds, PipeJoint, Motor, RotationDisplay.

## 3. The main technical risk: real-time plotting

Unchanged by the fidelity pass. `ChartControl` in 35.2.12 has no FastLine series type; `Line` was used. Release build, 6 s per run, one 8-logical-processor machine, one run per cell, no warm-up, 2000-point window. UI frames per second, default / with `ImprovePerformance`:

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

The fidelity pass changes how these read: most of them can now be **shown** with Syncfusion's renderer interfaces, so the FR is about making the look a supported option (a property or style) instead of something each customer has to draw. The renderers in `src/PocApp/Cards/` are a ready starting point for that work.

Findings for Syncfusion (docs, samples or possible bugs):
- **Clock:** draws a square drop shadow behind the round face, which still shows inside the face even with a circular `Region`. It also does not size itself to its container.
- **IClockRenderer:** `ClockRenderer`'s hand methods are not virtual, so a custom renderer must re-implement the interface instead of overriding.
- **CheckBoxAdv** has no renderer interface (ToggleButton, RadialGauge, LinearGauge and Clock do), so custom switch looks need an `OnPaint` override.
- **DigitalGauge:** `:` and `.` take a character cell, so `CharacterCount` must include separators or the text is cut off silently.
- **ChartControl:** reading `series.LegendItem` before `Series.Add` throws `NullReferenceException` inside Syncfusion. A straight-line legend icon (`ChartLegendItemType.StraightLine`) had no visible effect.
- **Horizontal bar charts:** `PrimaryXAxis` must be set to a Double value axis; leaving it as Category silently squashes the bars.
- **Namespaces:** `LabelPlacement` and `TickPlacement` exist in both `...Gauge` and `...Tools`, so code that uses both needs full names. `LinearGauge.GaugeBaseColor` is obsolete (use `ScaleColor`).
- **Trial license dialog** is modal and blocks automated runs.
- **Help site:** several guessed URLs return 404; the docs' GitHub source (`syncfusion-content/WindowsForms-docs`) was the reliable way to find pages.

## 5. Effort figures: read this before sharing

All effort ranges are **estimates**. Nothing was timed. Line counts give a sense of scale, but everything is prototype quality: no theming, no accessibility work, no High DPI testing, simplified behaviour (horizontal pipes only, simplified odometer carry, display-only tapes, Rocker3Way cycles states on click instead of following which half was pressed). Production quality will cost more than the ranges suggest.

The fidelity pass gives one useful data point: reusable renderers were cheap. Once the shared helpers existed, each further gauge or switch renderer was roughly 30-80 lines. That suggests the remaining N controls with a gauge-like or switch-like shape are cheaper than their current estimates; it does not hold for the process symbols (Motor, PipeJoint) or anything that needs new interaction.

## 6. Presentation guide

1. Open the **Gap register** tab: the summary lines show 15 / 22 / 19, and the look and approach counts. Filter **Look = close** to show the 31.
2. **Gauges / Digital / Clocks**: default vs restyled side by side. Point at the Approach chip: most restyles are Syncfusion's own renderer interfaces, not new controls.
3. **Plot**, then **Benchmark**: show the restyled chart, then be upfront about streaming limits and ask for their real rates.
4. **Switches / Process**: click the LED, toggle, rocker and lever switches, turn the rotary, close the valve and watch the outlet pipe stop. Show the tank and sliding tapes as LinearGauge + renderer stand-ins.
5. Finish on the 7 placeholders and the 7 "different" controls: say what would be built next (see `docs/IMPROVEMENT_PROPOSAL.md`).
6. Share only `MIGRATION_SUMMARY_CUSTOMER.md`. Keep `GAP_REGISTER_INTERNAL.md` inside Syncfusion.
7. Say plainly what was not verified (section 1): look scores are one person's judgement, and interaction on the new switches was not re-tested.

Run it: `dotnet build IocompPoc.sln` then `src/PocApp/bin/Debug/net10.0-windows/PocApp.exe`. Optional switches: `--snapshots <folder>`, `--export <folder>`, `--benchmark <csv> [--improve]`. Set `SYNCFUSION_LICENSE_KEY` to remove the trial dialog.

## 7. Open items

- Get the customer's real control usage and data rates, then re-prioritize.
- Apply the license key and rerun everything, including the benchmark.
- Have a second person review the look scores.
- Re-test interaction on the Knob and the new switches with a real mouse and keyboard; test High DPI and accessibility.
- Decide whether to build the 7 placeholders and fix the 7 "different" views (second phase; see the proposal).
- Raise the Clock shadow, the `IClockRenderer` virtuality and the missing FastLine type with the product teams.
