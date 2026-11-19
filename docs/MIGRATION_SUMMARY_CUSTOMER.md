# iocomp to Syncfusion WinForms: Migration Summary

A = available as-is, E = available with customization, N = no direct equivalent today.
Approach = how the demo achieves it. Effort ranges are estimates.
Look = how close the best live view is to the iocomp image (close / acceptable / different / n/a for placeholders). It is one reviewer's judgement from rendered snapshots at 100% scaling, not a measurement.

Total controls: 56 | As-is: 15 | Extend: 22 | No equivalent: 19 (13 built as prototypes) | Look: close 41, acceptable 15, different 0, n/a 0

Approach: properties only 25, composed 3, custom renderer 13, custom paint 2, new control 13, placeholder 0

| iocomp control | Rating | Syncfusion control | Approach | Look | Notes | Effort to close |
|---|---|---|---|---|---|---|
| Plot | E | ChartControl (Line, Column, Bubble) | Properties only | close | Mixed series supported. Strip-chart behaviour is hand-written; heavy streaming (many series at 1000+ points/s) was slow in our test - see benchmark. | 3-6 days (estimate) |
| SpectrumDisplay | A | ChartControl (column series) | Properties only | acceptable | Feed FFT output as data. | - |
| PercentBar | E | ChartControl (stacked column) | Properties only | acceptable | Chart configured as a percent bar. | 0.5-1 days (estimate) |
| PercentPie | A | ChartControl (pie) | Properties only | acceptable | Pie series with legend. | - |
| GaugeAngular | A | RadialGauge | Properties only | acceptable | Ranges, ticks and needle are supported. | - |
| GaugeLinear | A | LinearGauge | Properties only | acceptable | Scale, ranges and pointer are supported. | - |
| GaugeTube | E | LinearGauge (restyled) | Custom renderer | close | Glass tube frame needs custom styling. | 1-2 days (estimate) |
| Thermometer | E | LinearGauge (restyled) | Custom renderer | close | Bulb shape needs custom styling. | 1-2 days (estimate) |
| Knob | E | RadialGauge (+ mouse-drag pointer) | Custom renderer | close | Interactive pointer and glossy cap need custom code. | 2-4 days (estimate) |
| Compass | E | RadialGauge (0-360, text labels) | Custom renderer | close | Numeric 0-360 scale only; N/E/S/W labels and centre readout need custom code. | 1.5-3 days (estimate) |
| LedBar | E | LinearGauge (segmented style) | Custom renderer | close | Segmented look via ticks/ranges. | 1.5-3 days (estimate) |
| LedSpiral | E | RadialGauge (segmented style) | Custom renderer | close | Segmented ring via ranges; may need custom drawing. | 2-4 days (estimate) |
| Tank | N | LinearGauge + custom renderer (stand-in) | Custom renderer | acceptable | No vessel/liquid-fill control. | 3-5 days (estimate) |
| SevenSegmentAnalog | E | DigitalGauge + formatting | Properties only | close | Format by code. | 0.5-1 days (estimate) |
| SevenSegmentBinary | E | DigitalGauge + formatting | Properties only | close | Format by code. | 0.5-1 days (estimate) |
| SevenSegmentCharacter | E | DigitalGauge + formatting | Properties only | close | Depends on supported character set. | 0.5-1 days (estimate) |
| SevenSegmentHexadecimal | E | DigitalGauge + formatting | Properties only | close | Format by code. | 0.5-1 days (estimate) |
| SevenSegmentInteger | A | DigitalGauge | Properties only | close | Integer display with segment style. | - |
| LcdMatrix | E | DigitalGauge (dot-matrix) | Properties only | acceptable | Dot-matrix character type is available; upper-case text and digits tested. | 1-2 days (estimate) |
| Odometer | N | DigitalGauge (stand-in) | New control | close | No rolling-digit display. | 2-4 days (estimate) |
| ClockAnalog | E | Clock (restyled) | Custom renderer | acceptable | Hand colours and a dark style are available; the face is a plain clock face. | 1-2 days (estimate) |
| SevenSegmentClock | E | DigitalGauge + timer | Properties only | close | Time formatting by code. | 0.5-1 days (estimate) |
| SevenSegmentClockSmpte | E | DigitalGauge + timer | Properties only | close | Frame-rate formatting by code. | 0.5-1 days (estimate) |
| DisplayDouble | A | Label / TextBoxExt (read-only) | Properties only | close | Alarm colours and formatting by code. | - |
| DisplayInteger | A | Label / TextBoxExt (read-only) | Properties only | close | Alarm colours and formatting by code. | - |
| DisplayString | A | Label / TextBoxExt (read-only) | Properties only | close | Read-only text. | - |
| EditDouble | A | DoubleTextBox | Properties only | close | Min/max validation is built in. | - |
| EditInteger | A | IntegerTextBox | Properties only | close | Min/max validation is built in. | - |
| EditString | A | TextBoxExt | Properties only | close | Text entry. | - |
| ModeComboBox | A | ComboBoxAdv | Properties only | close | Drop-down selection. | - |
| Label | A | Label / AutoLabel | Properties only | acceptable | Text label. | - |
| Panel | A | GradientPanel | Properties only | acceptable | Titled container. | - |
| ImageDisplay | A | PictureBox | Properties only | close | Static image. | - |
| Slider | E | TrackBarEx (restyled) | Composed | close | No numeric tick labels or scale on the slider today. | 0.5-1 days (estimate) |
| SwitchToggle | E | ToggleButton | Custom renderer | close | Visual style differs. | 0.5-1 days (estimate) |
| SwitchLed | E | ToggleButton + IToggleButtonRenderer | Custom renderer | close | Label with coloured indicator by styling. | 0.5-1 days (estimate) |
| SwitchRotary | N | (none) | New control | close | No multi-position rotary switch. | 3-5 days (estimate) |
| SwitchPanel | N | (none) | New control | close | No panel-dial switch. | 3-5 days (estimate) |
| SwitchLever | N | CheckBoxAdv + custom paint (stand-in) | Custom paint | close | No lever switch. | 3-5 days (estimate) |
| SwitchRocker | N | ToggleButton + IToggleButtonRenderer (stand-in) | Custom renderer | close | No rocker switch. | 2-4 days (estimate) |
| SwitchRocker3Way | N | CheckBoxAdv (Tristate) + custom paint (stand-in) | Custom paint | close | No three-way rocker switch. | 2-4 days (estimate) |
| SwitchSlider | N | (none) | New control | close | No labelled multi-position slider switch. | 2-3 days (estimate) |
| SwitchQuad | N | (none) | New control | close | No four-way directional switch. | 2-4 days (estimate) |
| Led | N | (none) | New control | close | No LED indicator. | 1-2 days (estimate) |
| LedArrow | N | (none) | New control | close | No arrow indicator. | 1-2 days (estimate) |
| MatrixLeds | N | (none) | New control | close | No LED matrix. | 2-3 days (estimate) |
| MatrixButtons | E | ButtonAdv grid in TableLayoutPanel | Composed | acceptable | Composition, not a drop-in. | 1-2 days (estimate) |
| PhonePad | E | ButtonAdv grid in TableLayoutPanel | Composed | acceptable | Composition, not a drop-in. | 1-2 days (estimate) |
| Pipe | N | (none) | New control | close | No animated pipe. | 3-6 days (estimate) |
| PipeJoint | N | (none) | New control | acceptable | No pipe joint. | 1-2 days (estimate) |
| Valve | N | (none) | New control | close | No valve symbol. | 3-5 days (estimate) |
| Motor | N | (none) | New control | close | No motor symbol. | 3-5 days (estimate) |
| ObjectCanvas | E | Diagram (shapes, text, rotation) | Properties only | acceptable | Drawing surface via Diagram. | 2-4 days (estimate) |
| SlidingScale | N | LinearGauge + custom renderer (stand-in) | Custom renderer | close | No scrolling tape scale. | 3-5 days (estimate) |
| SlidingCompass | N | LinearGauge + custom renderer (stand-in) | Custom renderer | close | No scrolling heading tape. | 3-5 days (estimate) |
| RotationDisplay | N | (none) | New control | acceptable | Needs custom drawing at an arbitrary angle. | 1-2 days (estimate) |
