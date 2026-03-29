using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// URP Renderer Feature (Unity 6 RenderGraph):
/// Renders the "OccludedPass" of shaders on Character layer,
/// which draws semi-transparent where stencil == 1.
/// No override material needed — uses the object's own material + shader pass.
/// </summary>
public class OccludedCharacterFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingTransparents;
        public LayerMask characterLayer = 0;
    }

    public Settings settings = new Settings();
    private OccludedCharacterPass m_Pass;

    public override void Create()
    {
        m_Pass = new OccludedCharacterPass(settings);
        m_Pass.renderPassEvent = settings.renderEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.characterLayer == 0)
            return;

        renderer.EnqueuePass(m_Pass);
    }

    private class OccludedCharacterPass : ScriptableRenderPass
    {
        // Only look for passes tagged "OccludedPass"
        private static readonly List<ShaderTagId> s_ShaderTagIds = new List<ShaderTagId>
        {
            new ShaderTagId("OccludedPass"),
        };

        private readonly Settings m_Settings;

        public OccludedCharacterPass(Settings settings)
        {
            m_Settings = settings;
        }

        private class PassData
        {
            public RendererListHandle rendererListHandle;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            // Draw objects using their own material, but only the "OccludedPass" pass
            var drawingSettings = RenderingUtils.CreateDrawingSettings(
                s_ShaderTagIds, renderingData, cameraData, lightData, SortingCriteria.CommonTransparent);

            var filteringSettings = new FilteringSettings(
                RenderQueueRange.transparent, m_Settings.characterLayer);

            var rendererListParams = new RendererListParams(
                renderingData.cullResults, drawingSettings, filteringSettings);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "OccludedCharacterPass", out var passData))
            {
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);

                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParams);
                builder.UseRendererList(passData.rendererListHandle);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.DrawRendererList(data.rendererListHandle);
                });
            }
        }
    }
}
