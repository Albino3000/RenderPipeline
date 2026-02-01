using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class PixelRenderPipeline : RenderPipeline
{ 
    CameraRenderer renderer = new CameraRenderer();
    bool useDynamicBatching, useGPUInstancing;

    int pixelatedScreenHeight;
    ShadowSettings shadowSettings;

    public PixelRenderPipeline(int pixelHeight, bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadows)
    {
        pixelatedScreenHeight = pixelHeight;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        shadowSettings = shadows;
    }
    protected override void Render (ScriptableRenderContext context, Camera[] cameras)
    {
        
    }
    protected override void Render (ScriptableRenderContext context, List<Camera> cameras)
    {
        //For every camera render its display
        for(int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(context, cameras[i], pixelatedScreenHeight, useDynamicBatching, useGPUInstancing, shadowSettings);
        }
    }
}
