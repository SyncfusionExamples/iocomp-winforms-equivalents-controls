namespace PocApp.Infrastructure;

public enum Rating { A, E, N }

public enum EffortSource { None, Estimate, Measured }

/// <summary>How close the best live view looks to the iocomp image. None = placeholder, nothing to compare.</summary>
public enum Fidelity { Unverified, Close, Acceptable, Different, None }

/// <summary>How the live view was achieved, from least to most custom code.</summary>
public enum Approach
{
    Properties,      // Syncfusion control configured through properties/events only
    Composition,     // several Syncfusion controls laid out together
    CustomRenderer,  // Syncfusion control + its documented renderer interface (painting only)
    CustomPaint,     // Syncfusion control subclassed with OnPaint overridden (no renderer interface)
    Prototype,       // new control written from scratch in HmiControls
    Placeholder,     // nothing live
}

/// <summary>One iocomp control and how it maps to Syncfusion. Single source of truth for cards and exports.</summary>
public sealed record ControlEntry(
    string Name,                 // matches iocomp-dotnet-images/{Name}.png
    string Category,             // page the card lives on
    Rating Rating,
    string SyncfusionControl,    // control or stand-in
    string GapSummary,           // neutral wording, safe for the customer
    double EffortMinDays,
    double EffortMaxDays,
    EffortSource EffortSource,
    string? Priority,            // internal only: P1..P3, null for A
    bool FrCandidate,            // internal only
    string? FrTitle,             // internal only
    string InternalNotes,        // internal only
    bool BuiltInPoc = false)     // a working HmiControls prototype exists
{
    public Fidelity Fidelity { get; init; } = Fidelity.Unverified;

    public Approach Approach { get; init; } = Approach.Placeholder;

    public string EffortText => EffortSource == EffortSource.None
        ? "-"
        : $"{EffortMinDays:0.#}-{EffortMaxDays:0.#} days ({EffortSource.ToString().ToLowerInvariant()})";
}
