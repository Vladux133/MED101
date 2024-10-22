//////////////////////////////////////////////////////////////////////////
//
//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!
//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!
//      DO NOT EDIT THIS FILE!! THIS IS AUTOMATICALLY GENERATED!!
//
//////////////////////////////////////////////////////////////////////////

#ifndef UNITY_TERRAIN_TOOL_NOISE_NoneVoronoi_INC
#define UNITY_TERRAIN_TOOL_NOISE_NoneVoronoi_INC

/*=========================================================================

    Includes

=========================================================================*/

#include "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/Implementation/VoronoiImpl.hlsl"
#include "Assets/Procedural Worlds/Gaia/Shaders/Unity/Terrain Tools/NoiseLib/NoiseCommon.hlsl"



/*=========================================================================
    
    NoneVoronoi Noise Functions - Non-Fractal, Non-Warped

=========================================================================*/

float noise_NoneVoronoi( float pos )
{
    return get_noise_Voronoi( pos );
}

float noise_NoneVoronoi( float2 pos )
{
    return get_noise_Voronoi( pos );
}

float noise_NoneVoronoi( float3 pos )
{
    return get_noise_Voronoi( pos );
}

float noise_NoneVoronoi( float4 pos )
{
    return get_noise_Voronoi( pos );
}

#endif // UNITY_TERRAIN_TOOL_NOISE_NoneVoronoi_INC