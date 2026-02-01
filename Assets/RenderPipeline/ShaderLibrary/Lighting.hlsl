#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED



float3 IncomingLight (Surface surface, Light light)
{
   
    float NdotL = saturate(dot(surface.normal, light.direction));

    
    float band = step(surface.shadowThreshold, NdotL);

    // 3. Map band to shadow/light values
    // band = 0 → SHADOW_INTENSITY
    // band = 1 → 1.0
    float cel = lerp(surface.shadowIntensity, 1.0, band);

    // 4. Apply light color
    return cel * light.color;
}


float3 GetLighting (Surface surface, Light light) {
	return IncomingLight(surface, light) * surface.color;
}

float3 GetLighting(Surface surface)
{
    float3 color = 0.0;
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		color += GetLighting(surface, GetDirectionalLight(i));
	}
	return color;
}

#endif