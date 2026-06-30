# Content Safety Guard - Portfolio Showcase

Content Safety Guard is a Windows desktop application prototype built with C# and WPF. It demonstrates local content-safety features such as screen capture analysis, ONNX Runtime inference, configurable protection settings, and a blocking overlay.

This repository is a selected portfolio version. Some internal modules, datasets, blocking providers, category importers, and commercial logic are intentionally omitted.

## What This Showcase Demonstrates

- C#/.NET WPF desktop development
- Local AI inference with ONNX Runtime
- Screen capture scanning with configurable scan frequency
- App state and settings persistence with JSON
- Protection overlay workflow
- Separation between UI controls, state services, and background scanning services

## Included Code Excerpts

```text
src-excerpts/
  AI/
    NsfwDetector.cs
    ImagePreprocessor.cs
  Services/
    ScanTimerService.cs
    ScreenCaptureService.cs
    BlockOverlayService.cs
    ImageTiler.cs
  State/
    ProtectionStateManager.cs
  DetectionSettings/
    DetectionSettingsState.cs
    DetectionSettingsStateService.cs
  Views/
    BlockOverlayWindow.xaml
    BlockOverlayWindow.xaml.cs
```

## Intentionally Omitted

```text
Website category management
Blocklist import pipeline
DNS/hosts/WFP blocking providers
Generated blocklist data
Commercial product logic
Runtime user data and logs
Large model files
```

## Demo Build

A runnable demo build can be shared separately as a ZIP file. The public source code in this showcase is not the full production source tree.

## Tech Stack

- C#
- .NET / WPF
- ONNX Runtime
- System.Drawing screen capture pipeline
- JSON-based local settings

## Ownership

Copyright (c) Cekdar Akkurt. All rights reserved.

No license is granted for reuse, redistribution, or commercial use of the included code excerpts unless permission is given explicitly.
