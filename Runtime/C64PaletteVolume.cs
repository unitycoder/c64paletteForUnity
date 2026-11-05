using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityCoder.C64Palette
{
    [Serializable]
    [VolumeComponentMenu("Post-processing/C64 Palette")]
    public sealed class C64PaletteVolume : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Enable the palette quantization effect. Check this parameter's override box in the Volume Profile.")]
        public BoolParameter enabled = new BoolParameter(false);

        [Tooltip("Optional palette PNG: 2 to 256 colors in one row or column (N x 1 or 1 x N). Leave empty for the bundled 16-color C64 palette.")]
        public TextureParameter palette = new TextureParameter(null);

        [Tooltip("0 = original camera colors, 1 = palette colors only.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Brightness offset applied before finding the nearest palette color.")]
        public ClampedFloatParameter brightness = new ClampedFloatParameter(0f, -0.5f, 0.5f);

        [Tooltip("Contrast applied before color matching. 1 = unchanged.")]
        public ClampedFloatParameter contrast = new ClampedFloatParameter(1f, 0f, 2f);

        [Tooltip("Saturation applied before color matching. 1 = unchanged.")]
        public ClampedFloatParameter saturation = new ClampedFloatParameter(1f, 0f, 2f);

        [Tooltip("0 = plain sRGB distance. 1 = weighted RGB distance (green differences count more).")]
        public ClampedFloatParameter colorWeighting = new ClampedFloatParameter(1f, 0f, 1f);

        [Tooltip("Ordered 4x4 Bayer dither strength. 0 = no dithering.")]
        public ClampedFloatParameter dithering = new ClampedFloatParameter(0f, 0f, 1f);

        [Tooltip("Dithering cell size in output pixels. 1 = finely patterned.")]
        public ClampedIntParameter ditherScale = new ClampedIntParameter(1, 1, 8);

        [Tooltip("Nearest-sample input from blocks of screen pixels. 1 = native resolution.")]
        public ClampedIntParameter pixelSize = new ClampedIntParameter(1, 1, 16);

        public bool IsActive() { return enabled.value && intensity.value > 0f; }

        public bool IsTileCompatible() { return false; }
    }
}
