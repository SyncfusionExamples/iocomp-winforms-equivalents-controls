# Improvement proposal: status and next steps for the iocomp -> Syncfusion POC

Current state (from `GAP_ANALYSIS.md`): 56 controls, **all with a live view**. Look: 41 close, 15 acceptable, 0 different. 13 controls built as `HmiControls` prototypes. Effort figures are estimates; none of this is production code yet.

The proposal had four tracks. This page records what was done from it, what was done differently, and what is left. All day figures are estimates.

## Track 1: Make the results trustworthy

| # | Item | Status |
|---|---|---|
| 1.1 | Real-mouse and keyboard pass on every interactive card | **Half done.** Automated part built: `PocApp.exe --selftest <file.md>` runs 15 scripted checks through the controls' own mouse/keyboard handlers (Knob drag, all 10 switches, Led, MatrixLeds, Valve, Motor); 15 of 15 pass (`docs/INTERACTION_SELFTEST.md`). It found one real quirk (RadialGauge ignores a press before its first paint). **Left for a person:** try the cards with a real mouse and keyboard and record the feel, ~0.5 day. |
| 1.2 | Second reviewer for the Look scores | **Open** (needs a second person). ~0.5 day using `snapshots/`. |
| 1.3 | High DPI pass at 125 / 150 / 200% | **Open.** Needs display scaling changed on a real machine. ~1 day. |
| 1.4 | Visual regression check | **Done.** `PocApp.exe --compare snapshots <folder>`: repeatable rendering (fixed signals, fixed clock time, animations off), per-page pixel comparison, `COMPARE.md` plus magenta diff images, exit code 1 on failure. Two runs differ by 0%; a damaged baseline fails. It also exposed a real issue: the idle benchmark chart drew random sample columns (fixed). |
| 1.5 | Apply the license key and rerun everything | **Open** (needs the key). ~0.5 day. |
| 1.6 | Put the project under git | **Done.** Repository initialised with a `.gitignore` and a baseline commit. |

## Track 2: Close the visual gaps

| # | Item | Status |
|---|---|---|
| 2a | The seven "different" views | **Done.** Display x3 (properties) and Slider (composed scale panel) are close; SwitchSlider is a new control and close; PercentBar and ObjectCanvas moved to acceptable. |
| 2b | The seven placeholders | **Done.** LedArrow, MatrixLeds, SwitchQuad, SwitchPanel, Motor (close) and PipeJoint, RotationDisplay (acceptable), all as `HmiControls` prototypes. The three selector switches share one base class. |
| 2c | Known look issues | **Partly done.** Rocker3Way now follows which half was pressed. **Left:** see the list below. |

Remaining look items (all currently "acceptable"), cheapest first:

| Control | Fix | Est. |
|---|---|---|
| GaugeLinear | Scale labels as "20.0": LinearGauge has no label event, so either accept it or use a renderer (that changes the approach from properties-only, so it costs the honest A rating). | 0.25 |
| PercentBar | Legend order (Item 1 first) via custom legend items; sunken frame via the chart's `BorderAppearance`. | 0.25 |
| PipeJoint | Elbow and tee shapes, and vertical runs for `Pipe`. | 1 |
| ObjectCanvas | Open-arc spinner (a custom Diagram node or an `HmiControls` canvas); decide whether Diagram is too heavy for the customer's use. | 0.5-1 |
| LcdMatrix | One continuous dot field with lower-case text needs a custom dot-matrix control. | 1 |
| ClockAnalog | Square shadow: raise with the product team; workaround is a fully custom `IClockRenderer` background or an `HmiControls` clock. | 0.5-1 |
| Tank | Free-form vessel outline. | 0.5 |
| GaugeAngular | Long tapered needle: renderer needed (same A-rating trade-off as GaugeLinear). | 0.5 |

## Track 3: From POC code to reusable components (~8-12 days, unchanged)

Not started; it only makes sense once the customer says which controls they use.

1. Move the renderers (`src/PocApp/Cards/*Renderers.cs`) and the switch subclasses into `HmiControls` (or a new `IocompLook` library) with public, documented properties instead of constructor arguments.
2. Theme support: one palette object so the app switches between iocomp-classic and a Syncfusion theme.
3. Accessibility: `AccessibleRole`, state announcements, high-contrast mode. Today: accessible names, focus cues, and value-change notifications on the selector switches.
4. Designer support: serialization attributes, sensible defaults, toolbox bitmaps.
5. Unit tests for the value/state logic (the self-test covers wiring, not edge cases) plus the snapshot comparison in CI.
6. Performance with many instances (e.g. 50 animated pipes or motors, a 32 x 32 MatrixLeds): every animated control currently owns a timer.

## Track 4: Product and customer actions (no build effort, highest leverage)

1. **Ask the customer for their real control usage and data rates.** This decides which of track 2's leftovers and track 3 matter, and whether the Plot streaming limit is a blocker.
2. **Re-run the Plot benchmark at their rates**, licensed, on hardware like theirs, and try the mitigations not yet tried (batched updates, decimation, fixed ranges).
3. **Raise the findings with the product teams** (`GAP_ANALYSIS.md` section 4): Clock shadow, non-virtual `ClockRenderer` hand methods, no renderer interface on `CheckBoxAdv`, RadialGauge press-before-paint, ChartControl random sample columns with no series, DigitalGauge separator cells, `LegendItem` NRE before `Series.Add`, missing FastLine.
4. **Turn the strongest FR candidates into product proposals**, using the renderers and prototypes as evidence: segmented LED bar, text scale labels on RadialGauge, tank and tape gauges, switch skins, LED indicator and LED matrix.

## Recommended order now

1. Track 4.1 (ask the customer) and the open parts of track 1 (real mouse, second reviewer, High DPI, license key). These need people or a key, not code.
2. Once usage is known: the relevant track 2 leftovers, then track 3 for the controls the customer actually uses.
