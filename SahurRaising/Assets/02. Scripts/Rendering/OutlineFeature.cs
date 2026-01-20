using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using System.Collections.Generic;

namespace SahurRaising.Rendering
{
    /// <summary>
    /// URP Renderer Feature - 특정 객체에 포스트프로세싱 아웃라인 적용
    /// PC_Renderer 또는 Mobile_Renderer에 추가하여 사용
    /// </summary>
    public class OutlineFeature : ScriptableRendererFeature
    {
        [System.Serializable]
        public class OutlineSettings
        {
            [Header("렌더링 설정")]
            [Tooltip("아웃라인이 렌더링될 시점")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
            
            [Header("기본 아웃라인 설정")]
            [Tooltip("기본 아웃라인 색상")]
            public Color defaultColor = Color.white;
            
            [Tooltip("기본 아웃라인 두께")]
            [Range(0.5f, 10f)]
            public float defaultWidth = 2f;
            
            [Header("품질 설정")]
            [Tooltip("아웃라인 샘플링 횟수 (높을수록 부드러움)")]
            [Range(4, 16)]
            public int sampleCount = 8;
        }

        public OutlineSettings settings = new OutlineSettings();
        
        private OutlineRenderPass _outlinePass;
        private Material _outlineMaterial;
        private Material _maskMaterial;

        public override void Create()
        {
            var outlineShader = Shader.Find("Hidden/Outline/PostProcess");
            var maskShader = Shader.Find("Hidden/Outline/Mask");
            
            if (outlineShader != null)
                _outlineMaterial = CoreUtils.CreateEngineMaterial(outlineShader);
            
            if (maskShader != null)
                _maskMaterial = CoreUtils.CreateEngineMaterial(maskShader);

            _outlinePass = new OutlineRenderPass(_outlineMaterial, _maskMaterial, settings);
            _outlinePass.renderPassEvent = settings.renderPassEvent;
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (OutlineObject.ActiveOutlines.Count == 0)
                return;
            
            if (_outlineMaterial == null || _maskMaterial == null)
            {
                Debug.LogWarning("[OutlineFeature] 아웃라인 셰이더를 찾을 수 없습니다.");
                return;
            }

            var cameraType = renderingData.cameraData.cameraType;
            if (cameraType == CameraType.Game || cameraType == CameraType.SceneView || cameraType == CameraType.Preview)
            {
                renderer.EnqueuePass(_outlinePass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_outlineMaterial != null)
                CoreUtils.Destroy(_outlineMaterial);
            if (_maskMaterial != null)
                CoreUtils.Destroy(_maskMaterial);
            
            _outlinePass?.Dispose();
        }
    }

    /// <summary>
    /// 아웃라인 렌더 패스 - Unity 6 RenderGraph API
    /// </summary>
    public class OutlineRenderPass : ScriptableRenderPass, System.IDisposable
    {
        private readonly Material _outlineMaterial;
        private readonly Material _maskMaterial;
        private readonly OutlineFeature.OutlineSettings _settings;
        
        private static readonly int _OutlineColor = Shader.PropertyToID("_OutlineColor");
        private static readonly int _OutlineWidth = Shader.PropertyToID("_OutlineWidth");
        private static readonly int _OutlineStyle = Shader.PropertyToID("_OutlineStyle");
        private static readonly int _SampleCount = Shader.PropertyToID("_SampleCount");
        private static readonly int _MaskTex = Shader.PropertyToID("_MaskTex");

        // 마스크 패스용 PassData
        private class MaskPassData
        {
            public Material MaskMaterial;
            public List<OutlineObject> Outlines;
            public TextureHandle MaskTexture;
        }

        // 복사 패스용 PassData
        private class CopyPassData
        {
            public TextureHandle Source;
            public TextureHandle Destination;
        }

        // 합성 패스용 PassData
        private class CompositePassData
        {
            public Material OutlineMaterial;
            public TextureHandle SourceTexture;
            public TextureHandle MaskTexture;
        }

        public OutlineRenderPass(Material outlineMaterial, Material maskMaterial, OutlineFeature.OutlineSettings settings)
        {
            _outlineMaterial = outlineMaterial;
            _maskMaterial = maskMaterial;
            _settings = settings;
            profilingSampler = new ProfilingSampler("OutlinePass");
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_outlineMaterial == null || _maskMaterial == null)
                return;

            var activeOutlines = OutlineObject.ActiveOutlines;
            if (activeOutlines.Count == 0)
                return;

            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            var cameraColorTarget = resourceData.activeColorTexture;
            var descriptor = cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.msaaSamples = 1;

            // 마스크 텍스처 (R8)
            var maskDescriptor = descriptor;
            maskDescriptor.colorFormat = RenderTextureFormat.R8;
            var maskTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, maskDescriptor, "_OutlineMaskTex", false);

            // 소스 복사용 텍스처
            var sourceCopy = UniversalRenderer.CreateRenderGraphTexture(renderGraph, descriptor, "_OutlineSourceCopy", false);

            // 활성 아웃라인 객체 수집
            var validOutlines = new List<OutlineObject>();
            OutlineObject firstOutline = null;
            OutlineStyle style = OutlineStyle.Silhouette;

            foreach (var outline in activeOutlines)
            {
                if (outline == null || outline.Renderer == null || !outline.Renderer.enabled)
                    continue;
                
                validOutlines.Add(outline);
                if (firstOutline == null)
                {
                    firstOutline = outline;
                    style = outline.Style;
                }
            }

            if (validOutlines.Count == 0)
                return;

            // Step 1: 마스크 렌더링 (UnsafePass - DrawRenderer 필요)
            RecordMaskPass(renderGraph, maskTexture, validOutlines);

            // Step 2: 소스 복사
            RecordCopyPass(renderGraph, cameraColorTarget, sourceCopy);

            // Step 3: 머티리얼 속성 설정 (메인 스레드에서)
            _outlineMaterial.SetColor(_OutlineColor, firstOutline.OutlineColor);
            _outlineMaterial.SetFloat(_OutlineWidth, firstOutline.OutlineWidth);
            _outlineMaterial.SetInt(_OutlineStyle, (int)style);
            _outlineMaterial.SetInt(_SampleCount, _settings.sampleCount);

            // Step 4: 아웃라인 합성 (RasterPass + Blitter 사용)
            RecordCompositePass(renderGraph, cameraColorTarget, sourceCopy, maskTexture);
        }

        /// <summary>
        /// 마스크 텍스처에 아웃라인 대상 객체 렌더링
        /// </summary>
        private void RecordMaskPass(RenderGraph renderGraph, TextureHandle maskTexture, List<OutlineObject> outlines)
        {
            using (var builder = renderGraph.AddUnsafePass<MaskPassData>("OutlineMaskPass", out var passData, profilingSampler))
            {
                passData.MaskMaterial = _maskMaterial;
                passData.Outlines = new List<OutlineObject>(outlines);
                passData.MaskTexture = maskTexture;

                builder.UseTexture(maskTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((MaskPassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.MaskTexture);
                    context.cmd.ClearRenderTarget(true, true, Color.clear);

                    foreach (var outline in data.Outlines)
                    {
                        if (outline == null || outline.Renderer == null)
                            continue;

                        for (int i = 0; i < outline.Renderer.sharedMaterials.Length; i++)
                        {
                            context.cmd.DrawRenderer(outline.Renderer, data.MaskMaterial, i);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// 소스 텍스처를 복사
        /// </summary>
        private void RecordCopyPass(RenderGraph renderGraph, TextureHandle source, TextureHandle destination)
        {
            using (var builder = renderGraph.AddRasterRenderPass<CopyPassData>("OutlineCopyPass", out var passData, profilingSampler))
            {
                passData.Source = source;
                passData.Destination = destination;

                builder.UseTexture(source, AccessFlags.Read);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((CopyPassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }

        /// <summary>
        /// 아웃라인 합성 - RasterPass 사용
        /// </summary>
        private void RecordCompositePass(RenderGraph renderGraph, TextureHandle destination, TextureHandle source, TextureHandle mask)
        {
            using (var builder = renderGraph.AddRasterRenderPass<CompositePassData>("OutlineCompositePass", out var passData, profilingSampler))
            {
                passData.OutlineMaterial = _outlineMaterial;
                passData.SourceTexture = source;
                passData.MaskTexture = mask;

                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(mask, AccessFlags.Read);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((CompositePassData data, RasterGraphContext context) =>
                {
                    // 마스크 텍스처를 머티리얼에 설정
                    data.OutlineMaterial.SetTexture(_MaskTex, data.MaskTexture);
                    
                    // Blitter를 사용하여 합성
                    Blitter.BlitTexture(context.cmd, data.SourceTexture, new Vector4(1, 1, 0, 0), data.OutlineMaterial, 0);
                });
            }
        }

        [System.Obsolete]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) { }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) { }

        public override void OnCameraCleanup(CommandBuffer cmd) { }

        public void Dispose() { }
    }
}
