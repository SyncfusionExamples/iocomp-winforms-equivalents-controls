# Improvement proposal: next steps for the iocomp -> Syncfusion POC

Current state (from `GAP_ANALYSIS.md`): 56 controls, 49 with a live view. Look scores: 31 close, 11 acceptable, 7 different, 7 placeholders. Effort figures are estimates; none of this is production code yet.

The proposal is in four tracks, ordered by value to the deal. All day figures below are estimates for one developer who knows the code base, at prototype quality unless stated.

## Track 1: Make the current results trustworthy (do first, ~3-4 days)

The POC is visually convincing, but several claims rest on one person's judgement or on synthetic input. Fix that before the demo is shown widely.

| # | Item | Why | Est. |
|---|---|---|---|
| 1.1 | **Real-mouse and keyboard pass** on every interactive card (Knob, all switches, SwitchRotary, Valve, Led, Slider, editors). Record pass/fail in the catalog. | The Knob and five switches were restyled after the last interaction test. Synthetic messages missed the Knob's drag gap once already. | 0.5 |
| 1.2 | **Second reviewer for the Look scores**, using the snapshots. Record disagreements. | Scores are one person's judgement. | 0.5 |
| 1.3 | **High DPI pass at 125 / 150 / 200%.** | Never tested. Every renderer computes geometry from the client size, so it should scale, but fonts and the fixed-size Clock may not. | 1 |
| 1.4 | **Visual regression check**: compare `--snapshots` output against committed baselines and fail on large pixel differences. | Keeps the look from drifting silently as the code changes; also makes snapshots/ self-updating. | 1 |
| 1.5 | **Apply the license key and rerun everything**, including the benchmark. | The trial dialog blocked automation, and all numbers are from trial mode. | 0.5 |
| 1.6 | **Put the project under git.** | It is not a repository today, so there is no history or rollback. | 0.1 |

## Track 2: Close the visual gaps (~6-9 days)

### 2a. The seven "different" views (~2-3 days)

| Control | Fix | Approach | Est. |
|---|---|---|---|
| DisplayDouble / Integer / String | Cyan-on-black colours and font; one property each. | properties only | 0.25 |
| PercentBar | Single vertical column with a side legend (`StackingColumn100`, one category) plus the column's 3D shading. | properties | 0.5 |
| Slider | Draw the numeric scale beside `TrackBarEx` (a small owner-drawn panel), or subclass and paint. | custom paint | 0.75 |
| SwitchSlider | `CheckBoxAdv`-style custom paint like the lever: labelled positions and a switch body. Needs a 3+ position value, so a small `Control` subclass may be cleaner. | custom paint | 0.75 |
| ObjectCanvas | Try Diagram text nodes and a timer-driven rotation; if it stays heavy, recommend a lightweight custom canvas instead. | properties / new control | 0.5-1 |

### 2b. The seven placeholders (~4-6 days)

Ordered by how much existing code they can reuse:

| Control | Proposed approach | Reuses | Est. |
|---|---|---|---|
| LedArrow | Subclass `HmiControls.Led` with an arrow path instead of an ellipse | Led | 0.5 |
| MatrixLeds | Grid of the Led drawing, one control painting N x M cells (not N x M child controls) | Led | 0.75 |
| SwitchQuad | ToggleButton-style renderer on a 4-state control, or a small `Control` with four hit zones | SwitchDraw helpers | 0.75 |
| SwitchPanel | Radial selector: SwitchRotary's layout with a panel-style face | SwitchRotary | 0.75 |
| RotationDisplay | `Control` that draws an image or text with `Graphics.RotateTransform` | none | 0.5 |
| PipeJoint | Elbow/tee shapes reusing Pipe's teal cylinder shading | Pipe | 0.75 |
| Motor | Tri-colour rotor with timer animation and running/stopped states | Pipe's timer pattern | 1 |

### 2c. Known look issues on "close" or "acceptable" controls (~1-2 days)

- **ClockAnalog:** the Clock control paints a square shadow inside the round face. Raise it with the product team; as a workaround, a full custom `IClockRenderer` background, or an `HmiControls` clock.
- **GaugeAngular:** iocomp's long tapered needle along the bottom needs a custom renderer. That would move it from properties-only to custom renderer; decide whether keeping the honest A rating matters more than the look.
- **LcdMatrix:** iocomp draws one continuous dot field with lower-case text; DigitalGauge draws character cells. A custom dot-matrix control is roughly 1 day.
- **Tank:** free-form vessel outline (currently a rectangular interior).
- **Rocker3Way:** select the state from which half of the rocker was pressed, not CheckBoxAdv's cycle order.

## Track 3: From POC code to reusable components (~8-12 days)

The renderers in `src/PocApp/Cards/` are demo code: they live in the app, read colours ad hoc, and have no public API. If Syncfusion or the customer wants to keep them:

1. **Move renderers and switch controls into `HmiControls`** (or a new `IocompLook` library) with public, documented properties (colours, segment count, tape span, labels) instead of constructor arguments.
2. **Theme support:** read colours from a single palette object so one switch flips the whole app between iocomp-classic and a Syncfusion theme.
3. **Accessibility:** `AccessibleRole`, state announcements on change, keyboard focus cues, high-contrast mode. Today only accessible names are set.
4. **Designer support:** make the controls usable from the Visual Studio designer (serialization, sensible defaults, toolbox bitmaps).
5. **Unit tests** for value/state logic (Odometer roll, SwitchRotary hit testing, Rocker3Way states, tape label wrapping) plus the snapshot tests from 1.4.
6. **Performance check** with many instances (e.g. 50 animated pipes, 100 LEDs on one form), since every animated control currently owns a timer.

Estimate: ~8-12 days for the controls the customer actually uses; less if they only need a subset.

## Track 4: Product and customer actions (no build effort, highest leverage)

1. **Ask the customer for their real control usage and data rates.** This decides which of tracks 2-3 matter at all, and whether the Plot streaming limit (section 3 of the report) is a blocker.
2. **Re-run the Plot benchmark at their rates**, licensed, on hardware like theirs, and try the mitigations not yet tried (batched updates, decimation, fixed ranges).
3. **Raise the findings with the product teams** (list in report section 4): Clock shadow, non-virtual `ClockRenderer` hand methods, no renderer interface on `CheckBoxAdv`, DigitalGauge separator cells, `LegendItem` NRE before `Series.Add`, missing FastLine.
4. **Turn the strongest FR candidates into product proposals using the renderers as evidence:** segmented LED bar, text scale labels on RadialGauge, tank/tape gauges, switch skins. The POC shows each one can be drawn; the FR is to make it a supported style instead of customer code.

## Recommended order

1. Track 1 (trust), and Track 4.1 in parallel (ask the customer).
2. Track 2a: cheap fixes that move 7 "different" to close/acceptable.
3. Once usage is known: the relevant parts of 2b, 2c and Track 3.

Effect if tracks 1 and 2 are completed: about 52-54 of 56 controls close or acceptable, all with a live view, and every claim backed by a repeatable check rather than a one-off review.
