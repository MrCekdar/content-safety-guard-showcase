# Architecture Overview

The selected code excerpts show the content-scanning workflow used by the desktop application.

## Runtime Flow

```text
User starts protection
  -> ProtectionStateManager stores active/inactive state
  -> ScanTimerService starts periodic scanning
  -> ScreenCaptureService captures the current desktop frame
  -> NsfwDetector preprocesses the image and runs ONNX inference
  -> BlockOverlayService shows the blocking overlay when content is detected
```

## Key Design Decisions

- The AI model runs locally, so screen frames are not sent to an external API.
- Settings are persisted as local JSON state.
- Scanning behavior is controlled by a timer service instead of UI event handlers.
- UI components update state services, and services apply changes to the scanner.

## Notes For Reviewers

This showcase is intentionally reduced. It is designed to demonstrate software structure and implementation style without publishing the full product codebase.

The `source-preview/ContentSafetyGuard` folder contains a broader project snapshot for orientation. It is not intended to replace the runnable demo ZIP, because model files, generated datasets, and selected internal modules are omitted from the public repository.
