# Changelog

## [1.1.0] - 2026-11-05

- Support custom palette strips containing **2 to 256 colors**, horizontal or vertical.
- Automatically determine the palette count and direction from the assigned texture.
- Replace the fixed 16-color shader search with a runtime-sized loop.
- Support raw and sRGB palette imports; raw imports avoid per-candidate sRGB conversions.
- Fall back to the built-in palette and warn once for unsupported texture dimensions.
- Preserve the original package ID, namespace and Volume component for compatibility.

## [1.0.0] - 2026-11-04

- First Unity 6 URP Render Graph release.
- Volume settings for palette quantization, dithering, pixelation and input color adjustments.
- Built-in 16-color C64 palette and bundled 16 x 1 palette PNG.
- Git URL installation and UPM-ready package structure.
