using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Immersal.AR
{
    public class ARBackgroundRenderPassFeature : ScriptableRendererFeature
    {
        [Serializable]
        public class Settings
        {
            public Material material;
            public RenderPassEvent Event = RenderPassEvent.BeforeRenderingOpaques;
        }

        public Settings settings = new Settings();
        CustomRenderPass m_ScriptablePass;

        public override void Create()
        {
            m_ScriptablePass = new CustomRenderPass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (Application.isEditor)
                return;
            
            renderer.EnqueuePass(m_ScriptablePass);
        }

        class CustomRenderPass : ScriptableRenderPass
        {
            const string k_CustomRenderPassName = "Huawei AR Background Pass (URP)";

            private Settings settings;

            public CustomRenderPass(Settings sts)
            {
                settings = sts;
                renderPassEvent = settings.Event;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (settings.material != null)
                {
                    CommandBuffer cmd = CommandBufferPool.Get(k_CustomRenderPassName);
                    cmd.Blit(settings.material.mainTexture, BuiltinRenderTextureType.CurrentActive, settings.material);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    CommandBufferPool.Release(cmd);
                }
            }
        }
    }
}