using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    // This tests is a variation of test 105.
    // It illustrates that ScriptableRenderer.ExecuteRenderPass will use the camera clearFlag if the camera target is one of the renderTargets in the MRT setup
    public sealed class Test106Renderer : ScriptableRenderer
    {
        RenderTargetHandle m_CameraColor;
        RenderTargetHandle m_CameraDepth;

        OutputColorsToMRTsRenderPass m_ColorsToMrtsPass;
        RenderTargetHandle[] m_ColorToMrtOutputs; // outputs of render pass "OutputColorsToMRTs"
        
        CopyToViewportRenderPass[] m_CopyToViewportPasses;
        Rect m_Viewport = new Rect(660, 200, 580, 320); // viewport to copy the results into

        FinalBlitPass m_FinalBlitPass;

        public Test106Renderer(Test106RendererData data) : base(data)
        {
            m_CameraColor.Init("_CameraColor");
            m_CameraDepth.Init("_CameraDepth");

            Material colorToMrtMaterial = CoreUtils.CreateEngineMaterial(data.shaders.colorToMrtPS);
            m_ColorsToMrtsPass = new OutputColorsToMRTsRenderPass(colorToMrtMaterial);

            m_ColorToMrtOutputs = new RenderTargetHandle[2];
          //m_ColorToMrtOutputs[0].Init("_ColorToMrtOutput0");
            m_ColorToMrtOutputs[1].Init("_ColorToMrtOutput1");

            Material copyToViewportMaterial = CoreUtils.CreateEngineMaterial(data.shaders.copyToViewportPS);
            m_CopyToViewportPasses = new CopyToViewportRenderPass[2];
          //m_CopyToViewportPasses[0] = new CopyToViewportRenderPass(copyToViewportMaterial);
            m_CopyToViewportPasses[1] = new CopyToViewportRenderPass(copyToViewportMaterial);

            Material blitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPS);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering, blitMaterial);
        }

        string m_profilerTag = "Test 106 Renderer";

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_profilerTag);

            cmd.GetTemporaryRT(m_CameraColor.id, 1280, 720);
            cmd.GetTemporaryRT(m_CameraDepth.id, 1280, 720, 16);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            ConfigureCameraTarget(m_CameraColor.Identifier(), m_CameraDepth.Identifier());


            // 1) Render different colors to the MRT outputs (render a blue quad to output#0 and a red quad to output#1)
            m_ColorToMrtOutputs[0] = m_CameraColor;
            m_ColorsToMrtsPass.Setup(ref renderingData, cameraColorTarget, m_ColorToMrtOutputs);
            EnqueuePass(m_ColorsToMrtsPass);
            // Notice that the renderPass clearColor (yellow) is applied for output#1
            // Notice that the camera     clearColor (green)  is applied for output#0 (because output#0 is the camera target)


            // 2) Copy results to the camera target - in test 106 we only copy output#1 since output#0 is the camera target itself

            // layout
            // x: <-040-><-580-><-040-><-580-><-040->
            // y: <-200-><-320-><-200->

            //m_Viewport.x = 40;
            //m_CopyToViewportPasses[0].Setup(m_ColorToMrtOutputs[0].Identifier(), m_CameraColor, m_Viewport);
            //EnqueuePass(m_CopyToViewportPasses[0]);

            //m_Viewport.x = 660;
            m_CopyToViewportPasses[1].Setup(m_ColorToMrtOutputs[1].Identifier(), m_CameraColor, m_Viewport);
            EnqueuePass(m_CopyToViewportPasses[1]);


            // 3) Final blit to the backbuffer
            m_FinalBlitPass.Setup(renderingData.cameraData.cameraTargetDescriptor, m_CameraColor);
            EnqueuePass(m_FinalBlitPass);
        }

        /// <inheritdoc />
        public override void FinishRendering(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_CameraColor.id);
            cmd.ReleaseTemporaryRT(m_CameraDepth.id);
        }
    }
}
