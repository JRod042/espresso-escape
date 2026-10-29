using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// Look step 01 r2: draws the menu UI (Screen Space - Camera canvases on FeltLook.UiLayer) AFTER post-processing,
/// so Bokeh DoF, the grade and the vignette never blur or tint menu text. The renderer's transparent pass skips
/// that layer (EscapeURP_Renderer m_TransparentLayerMask). Same camera, same frame: the Liquid Glass blur still reads
/// this camera's _CameraOpaqueTexture (UseAllGlobalTextures keeps it bound for this pass).
public class UiAfterPostFeature : ScriptableRendererFeature
{
    public LayerMask layerMask = 1 << 5;

    class UiPass : ScriptableRenderPass
    {
        public LayerMask mask;
        static readonly List<ShaderTagId> Tags = new List<ShaderTagId>
        {
            new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly")
        };

        class PassData { public RendererListHandle list; }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            var rendering = frameData.Get<UniversalRenderingData>();
            var camera = frameData.Get<UniversalCameraData>();
            var lights = frameData.Get<UniversalLightData>();
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("UI After Post", out var data))
            {
                var drawing = RenderingUtils.CreateDrawingSettings(Tags, rendering, camera, lights, SortingCriteria.CommonTransparent);
                var filtering = new FilteringSettings(RenderQueueRange.transparent, mask);
                data.list = renderGraph.CreateRendererList(new RendererListParams(rendering.cullResults, drawing, filtering));
                builder.UseRendererList(data.list);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.Write);
                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData d, RasterGraphContext ctx) => ctx.cmd.DrawRendererList(d.list));
            }
        }
    }

    UiPass _pass;

    public override void Create()
    {
        _pass = new UiPass { renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing, mask = layerMask };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        _pass.mask = layerMask;
        renderer.EnqueuePass(_pass);
    }
}
