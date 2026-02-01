using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;


public partial class CameraRenderer 
{
    #region variables
    static ShaderTagId 
    unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
    cellShaderTagId = new ShaderTagId("CellShaded");

    const string bufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = bufferName };

    CullingResults cullingResults;
    ScriptableRenderContext context; 
    Camera camera;
    int pixelatedScreenHeight;
    Lighting lighting = new Lighting();

    // Pixelation
    int pixelRTId;
    bool isGameView = false;
    #endregion
    public void Render(ScriptableRenderContext context, Camera camera, int pixelHeight, bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings) 
    {
        this.context = context;
        this.camera = camera;
        pixelatedScreenHeight = pixelHeight;

        PrepareBuffer();
        PrepareForSceneWindow();

        if (!Cull(shadowSettings.maxDistance))
            return;

        buffer.BeginSample(SampleName);
		ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);
        Setup();
        buffer.EndSample(SampleName);
        isGameView = camera.cameraType == CameraType.Game;

        if (isGameView)
        {
            AllocatePixelRT();
            BeginPixelRendering();
        }

        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();

        if (isGameView)
        {
            EndPixelRendering();
            ReleasePixelRT();
        }
        lighting.Cleanup();
        Submit();
    }
    #region Pixelation

    void AllocatePixelRT()
    {
        // Compute pixel resolution
        
        int pixelWidth = Mathf.RoundToInt(camera.aspect * pixelatedScreenHeight);

        // Create render target descriptor
        var desc = new RenderTextureDescriptor(
            pixelWidth, pixelatedScreenHeight,
            RenderTextureFormat.ARGB32, 24
        );
        desc.useMipMap = false;
        desc.autoGenerateMips = false;

        // Allocate temporary RT
        pixelRTId = Shader.PropertyToID("_PixelRT");
        buffer.GetTemporaryRT(pixelRTId, desc);

        ExecuteBuffer();
    }
    void BeginPixelRendering()
    {
        // Redirect all rendering into the pixel RT
        buffer.SetRenderTarget(pixelRTId);
        buffer.ClearRenderTarget(true, true, camera.backgroundColor);
        ExecuteBuffer();
    }

    void EndPixelRendering()
    {
        // Blit the pixelated texture to the final camera target
        buffer.Blit(pixelRTId, BuiltinRenderTextureType.CameraTarget);
        ExecuteBuffer();

        // Optional: Make texture available to shaders
        buffer.SetGlobalTexture("_LowResTexture", pixelRTId);
        ExecuteBuffer();
    }
    void ReleasePixelRT()
    {
        buffer.ReleaseTemporaryRT(pixelRTId);
        ExecuteBuffer();
    }

    #endregion


    void Setup()
    {
        context.SetupCameraProperties(camera);

        CameraClearFlags flags = camera.clearFlags;
        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags <= CameraClearFlags.Color,
            (flags == CameraClearFlags.Color) ? 
                camera.backgroundColor.linear : Color.clear
        );

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }
    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }
    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(
			unlitShaderTagId, sortingSettings
		) {
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.SetShaderPassName(1, cellShaderTagId);

        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        // Opaque
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        context.DrawSkybox(camera);

        // Transparent
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}

public partial class CameraRenderer
{
    partial void DrawUnsupportedShaders();
    partial void PrepareForSceneWindow();
    partial void DrawGizmos();
    partial void PrepareBuffer();

    #if UNITY_EDITOR
    static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};
    static Material errorMaterial;


    string SampleName {get; set;}

    partial void DrawUnsupportedShaders()
    {
        if (errorMaterial == null) 
        {
			errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
		}
        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)) {overrideMaterial = errorMaterial};
        for (int i = 1; i < legacyShaderTagIds.Length; i++) 
        {
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }
    partial void DrawGizmos()
    {
        if(Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }
    partial void PrepareForSceneWindow()
    {
        if(camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }
    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editor Only");
        buffer.name = SampleName = camera.name;
        Profiler.EndSample();
    }
    #else
        string SampleName => bufferName;
    
    #endif
}