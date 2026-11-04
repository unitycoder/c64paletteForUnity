using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace UnityCoder.C64Palette
{
    public sealed class C64PaletteRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private bool applyToSceneView = true;
        [SerializeField] private Texture2D defaultPalette;
        [SerializeField] private Shader effectShader;

        private Material material;
        private Texture2D generatedPalette;
        private C64PalettePass renderPass;

        public override void Create()
        {
            if (material != null) CoreUtils.Destroy(material);
            if (generatedPalette != null) CoreUtils.Destroy(generatedPalette);

            if (effectShader == null) effectShader = Resources.Load<Shader>("C64PaletteQuantize");
            if (effectShader == null) effectShader = Shader.Find("Hidden/UnityCoder/C64PaletteQuantize");
            generatedPalette = CreateBuiltinPalette();

            material = effectShader != null ? CoreUtils.CreateEngineMaterial(effectShader) : null;
            renderPass = material != null ? new C64PalettePass(material, defaultPalette != null ? defaultPalette : generatedPalette) : null;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderPass == null) return;

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Preview || cameraType == CameraType.Reflection) return;
            if (cameraType == CameraType.SceneView && !applyToSceneView) return;

            // Avoid applying the same quantization again to an overlay camera.
            if (renderingData.cameraData.renderType == CameraRenderType.Overlay) return;

            renderPass.SetDefaultPalette(defaultPalette != null ? defaultPalette : generatedPalette);
            renderer.EnqueuePass(renderPass);
        }

        protected override void Dispose(bool disposing)
        {
            renderPass = null;
            if (material != null) CoreUtils.Destroy(material);
            if (generatedPalette != null) CoreUtils.Destroy(generatedPalette);
            material = null;
            generatedPalette = null;
        }

        private static Texture2D CreateBuiltinPalette()
        {
            Color32[] colors =
            {
                new Color32(0, 0, 0, 255),
                new Color32(98, 98, 98, 255),
                new Color32(137, 137, 137, 255),
                new Color32(173, 173, 173, 255),
                new Color32(255, 255, 255, 255),
                new Color32(159, 78, 68, 255),
                new Color32(203, 126, 117, 255),
                new Color32(109, 84, 18, 255),
                new Color32(161, 104, 60, 255),
                new Color32(201, 212, 135, 255),
                new Color32(154, 226, 155, 255),
                new Color32(92, 171, 94, 255),
                new Color32(106, 191, 198, 255),
                new Color32(136, 126, 203, 255),
                new Color32(80, 69, 155, 255),
                new Color32(160, 87, 163, 255)
            };

            Texture2D texture = new Texture2D(16, 1, TextureFormat.RGBA32, false, false);
            texture.name = "C64 Palette (Runtime)";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixels32(colors);
            texture.Apply(false, true);
            return texture;
        }

        private sealed class C64PalettePass : ScriptableRenderPass
        {
            private static readonly int PaletteId = Shader.PropertyToID("_PaletteTex");
            private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
            private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
            private static readonly int ContrastId = Shader.PropertyToID("_Contrast");
            private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
            private static readonly int WeightingId = Shader.PropertyToID("_ColorWeighting");
            private static readonly int DitherId = Shader.PropertyToID("_Dithering");
            private static readonly int DitherScaleId = Shader.PropertyToID("_DitherScale");
            private static readonly int PixelSizeId = Shader.PropertyToID("_PixelSize");

            private readonly Material material;
            private Texture2D defaultPalette;

            public C64PalettePass(Material material, Texture2D defaultPalette)
            {
                this.material = material;
                this.defaultPalette = defaultPalette;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

                // Force a texture-backed color target so that the screen can be read.
                requiresIntermediateTexture = true;
            }

            public void SetDefaultPalette(Texture2D value) { defaultPalette = value; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                C64PaletteVolume settings = VolumeManager.instance.stack.GetComponent<C64PaletteVolume>();
                if (settings == null || !settings.IsActive()) return;

                Texture palette = settings.palette.value != null ? settings.palette.value : defaultPalette;
                if (palette == null || palette.width != 16 || palette.height != 1) return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (resourceData.isActiveTargetBackBuffer) return;

                TextureHandle source = resourceData.activeColorTexture;
                if (!source.IsValid()) return;

                RenderTextureDescriptor descriptor = cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                descriptor.msaaSamples = 1;
                descriptor.bindMS = false;
                descriptor.useMipMap = false;
                descriptor.autoGenerateMips = false;

                TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_C64PaletteResult", false);
                if (!destination.IsValid()) return;

                material.SetTexture(PaletteId, palette);
                material.SetFloat(IntensityId, settings.intensity.value);
                material.SetFloat(BrightnessId, settings.brightness.value);
                material.SetFloat(ContrastId, settings.contrast.value);
                material.SetFloat(SaturationId, settings.saturation.value);
                material.SetFloat(WeightingId, settings.colorWeighting.value);
                material.SetFloat(DitherId, settings.dithering.value);
                material.SetFloat(DitherScaleId, settings.ditherScale.value);
                material.SetFloat(PixelSizeId, settings.pixelSize.value);

                RenderGraphUtils.BlitMaterialParameters parameters = new RenderGraphUtils.BlitMaterialParameters(source, destination, material, 0);
                renderGraph.AddBlitPass(parameters, "C64 Palette Quantization");

                // The next URP pass (usually Final Blit) consumes this result.
                resourceData.cameraColor = destination;
            }
        }
    }
}
