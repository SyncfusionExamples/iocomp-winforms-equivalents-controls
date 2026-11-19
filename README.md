# iocomp-winforms-equivalents-controls

This sample shows Syncfusion WinForms equivalents for the 56 iocomp .NET WinForms controls. Each control has a card with the iocomp reference image, a live Syncfusion (or custom) version next to it, and notes on how close the match is.

## Projects

| Project | Description |
|---|---|
| `src/PocApp` | WinForms sample app. It has one tab per category (Plot, Gauges, Digital, Clocks, Inputs, Switches, Leds, Process) and a **Gap register** tab that lists each iocomp control with its Syncfusion equivalent, rating and gaps. |
| `src/HmiControls` | Small custom controls for iocomp features that have no Syncfusion equivalent: `SwitchRotary`, the selector switches (`SwitchPanel`, `SwitchQuad`, `SwitchSlider`), `Led`, `LedArrow`, `MatrixLeds`, `Odometer`, `Pipe`, `PipeJoint`, `Valve`, `Motor`, `RotationDisplay`. |
| `iocomp-dotnet-images` | iocomp reference images, shown beside each control. |

## Requirements

- Windows
- .NET SDK 10.0.404 or a later patch (see `global.json`)
- Syncfusion Essential Studio for WinForms 35.2.12 (Chart, Gauge, Tools, Diagram), restored from NuGet

## Build and run

```cmd
dotnet build IocompPoc.sln -c Release
dotnet run --project src\PocApp -c Release
```

To register a Syncfusion license key, set the `SYNCFUSION_LICENSE_KEY` environment variable before running. Without a key, Syncfusion shows its license notice.

## Command-line options

| Option | Purpose |
|---|---|
| `--selftest <report.md>` | Runs scripted mouse and keyboard checks on the interactive controls on a hidden form, writes a Markdown report, and exits with code 0 if every check passes. |
| `--snapshots <dir>` | Saves each tab as a PNG, using fixed signal values so the output is the same every run. |
| `--compare <baselineDir> <currentDir>` | Saves new snapshots to `currentDir`, compares them with `baselineDir`, and writes `COMPARE.md`. |
| `--export <dir>` | Writes the gap register as `GAP_REGISTER_INTERNAL.md` and `MIGRATION_SUMMARY_CUSTOMER.md`. |
