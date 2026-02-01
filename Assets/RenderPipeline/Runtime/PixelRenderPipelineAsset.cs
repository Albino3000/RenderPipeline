using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Pixel Render Pipeline Asset")]
public class PixelRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
	bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;
    [SerializeField] int pixelatedScreenHeight;
    [SerializeField]ShadowSettings shadows = default;

    protected override RenderPipeline CreatePipeline() 
    {
		  return new PixelRenderPipeline(pixelatedScreenHeight, useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
	}
    
}