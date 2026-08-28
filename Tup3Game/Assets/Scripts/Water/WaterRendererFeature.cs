using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace SimWater
{
public class WaterRendererFeature : ScriptableRendererFeature
{
    public Material passMaterial;

    private FullScreenRenderPass _pass;

    public override void Create() => _pass = new FullScreenRenderPass(name);

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview
            || renderingData.cameraData.cameraType == CameraType.Reflection
            || UniversalRenderer.IsOffscreenDepthTexture(ref renderingData.cameraData))
            return;

        if (passMaterial == null)
            return;

        _pass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        _pass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        _pass.SetupMembers(passMaterial);

        _pass.requiresIntermediateTexture = true;

        renderer.EnqueuePass(_pass);
    }

    internal class FullScreenRenderPass : ScriptableRenderPass
    {
        private static MaterialPropertyBlock s_SharedPropertyBlock = new MaterialPropertyBlock();
        private Material m_Material;
        public static readonly int blitTexture = Shader.PropertyToID("_BlitTexture");
        public static readonly int blitScaleBias = Shader.PropertyToID("_BlitScaleBias");
        public FullScreenRenderPass(string passName) { profilingSampler = new ProfilingSampler(passName); }

        public void SetupMembers(Material material)
        {
            m_Material = material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourcesData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            TextureHandle source, destination;

            Debug.Assert(resourcesData.cameraColor.IsValid());
            
            var targetDesc = renderGraph.GetTextureDesc(resourcesData.cameraColor);
            targetDesc.name = "_CameraColorFullScreenPass";
            targetDesc.clearBuffer = false;

            source = resourcesData.activeColorTexture;
            destination = renderGraph.CreateTexture(targetDesc);

            renderGraph.AddBlitPass(source, destination, Vector2.one, Vector2.zero,
                passName: "Copy Color Full Screen");

            source = destination;
            destination = resourcesData.activeColorTexture;
            
            AddFullscreenRenderPassInputPass(renderGraph, resourcesData, cameraData, source, destination);
        }

        private void AddFullscreenRenderPassInputPass(RenderGraph renderGraph, UniversalResourceData resourcesData,
            UniversalCameraData cameraData, TextureHandle source, TextureHandle destination)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler);
            
            passData.material = m_Material;
            passData.inputTexture = source;

            if (passData.inputTexture.IsValid())
                builder.UseTexture(passData.inputTexture);
            
            Debug.Assert(resourcesData.cameraDepthTexture.IsValid());
            builder.UseTexture(resourcesData.cameraDepthTexture);
            builder.SetRenderAttachment(destination, 0);
                

            builder.SetRenderFunc(static (PassData data, RasterGraphContext rgContext) =>
            {
                ExecuteMainPass(rgContext.cmd, data.inputTexture, data.material, 0);
            });
        }
        private static void ExecuteMainPass(RasterCommandBuffer cmd, RTHandle sourceTexture, Material material, int passIndex)
        {
            s_SharedPropertyBlock.Clear();
            s_SharedPropertyBlock.SetTexture(blitTexture, sourceTexture);
            s_SharedPropertyBlock.SetVector(blitScaleBias, new Vector4(1, 1, 0, 0));


            cmd.DrawProcedural(Matrix4x4.identity, material, passIndex, MeshTopology.Triangles, 3, 1, s_SharedPropertyBlock);
        }

        class PassData
        {
            public Material material;
            public TextureHandle inputTexture;
        }
    }
}
}

/* [파일 노트]
 * Tavern_Gamejam_CAU_SSU 프로젝트(Assets/Scripts/Water/WaterRendererFeature.cs)에서 이식한
 * URP RenderGraph 풀스크린 블릿 패스. passMaterial 이 비어 있으면 아무 것도 하지 않는다.
 * 원본 프로젝트에서도 렌더러 에셋에 등록만 안 된 상태(전용 머티리얼/셰이더 없음)였다 —
 * 물 비주얼은 SpriteShape(Sprite-Lit-Default)가 담당하고, 이 피처는 후처리용 확장 지점이다.
 * 수정 사항: namespace SimWater 로 감쌌다(내용 동일).
 */