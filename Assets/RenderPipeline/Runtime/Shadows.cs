using UnityEngine;
using UnityEngine.Rendering;

public class Shadows {

	const string bufferName = "Shadows";
	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

	ScriptableRenderContext context;
	CullingResults cullingResults;
	ShadowSettings settings;
    const int maxShadowedDirectionalLightCount = 4;
    struct ShadowedDirectionalLight {
		public int visibleLightIndex;
	}
    int ShadowedDirectionalLightCount;
	ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[maxShadowedDirectionalLightCount];
    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas"),
			dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

	static Matrix4x4[]
		dirShadowMatrices = new Matrix4x4[maxShadowedDirectionalLightCount];
    

	public void Setup (ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings) 
    {
		this.context = context;
		this.cullingResults = cullingResults;
		this.settings = settings;

        ShadowedDirectionalLightCount = 0;
	}

    public void ReserveDirectionalShadows (Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowedDirectionalLightCount && 
        light.shadows != LightShadows.None && 
        light.shadowStrength > 0f &&
		cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) 
        {
			ShadowedDirectionalLights[ShadowedDirectionalLightCount++] = new ShadowedDirectionalLight 
            {
				visibleLightIndex = visibleLightIndex
			};
		}
    }

    public void Render () {
		if (ShadowedDirectionalLightCount > 0) {
			RenderDirectionalShadows();
		}
        else {
			buffer.GetTemporaryRT(
				dirShadowAtlasId, 1, 1,
				32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
			);
		}
	}
	void RenderDirectionalShadows ()
    {
        int atlasSize = (int)settings.directional.atlasSize;
		buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize,
		32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
		buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
		buffer.ClearRenderTarget(true, false, Color.clear);
		
		buffer.BeginSample(bufferName);
		ExecuteBuffer();

		int split = ShadowedDirectionalLightCount <= 1 ? 1 : 2;
		int tileSize = atlasSize / split;

		for(int i = 0; i < ShadowedDirectionalLightCount; i++) {
			RenderDirectionalShadows(i, tileSize, atlasSize);
    	}

		buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
		buffer.EndSample(bufferName);
		ExecuteBuffer();

	}

	void RenderDirectionalShadows(int index, int tileSize, int atlasSize)
	{
		ShadowedDirectionalLight light = ShadowedDirectionalLights[index];

		// Create shadow drawing settings (Unity 2022 requires projection type)
		var shadowSettings = new ShadowDrawingSettings(
			cullingResults,
			light.visibleLightIndex,
			BatchCullingProjectionType.Orthographic
		);

		// Compute view/projection matrices and culling data for the directional light
		cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
			light.visibleLightIndex,   // visible light index
			0,                          // cascade index
			1,                          // cascade count
			Vector3.zero,               // cascade ratios (unused for now)
			tileSize,                   // shadow map resolution
			0f,                         // shadow near plane
			out Matrix4x4 viewMatrix,
			out Matrix4x4 projectionMatrix,
			out ShadowSplitData splitData
		);

		// Apply culling data to shadow settings
		shadowSettings.splitData = splitData;

		// Set matrices for shadow rendering
		buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

		// Execute buffer and render shadow casters
		ExecuteBuffer();
		context.DrawShadows(ref shadowSettings); 
	}

	void SetTileViewport (int index, int split, float tileSize) {
		Vector2 offset = new Vector2(index % split, index / split);
		buffer.SetViewport(new Rect(
			offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
		));
	}


    public void Cleanup () {
		buffer.ReleaseTemporaryRT(dirShadowAtlasId);
		ExecuteBuffer();
	}


	void ExecuteBuffer () 
    {
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}
}