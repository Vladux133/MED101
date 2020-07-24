    Shader "Custom/rotationShader" {
        Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        [NoScaleOffset] _Normal ("Normal", 2D) = "bump" {}
        _NormalScale ("NormalScale", float) = 0.0
        [NoScaleOffset] _Height("Height", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _Parallax("Parallax", float) = 0
        _Rotation("UVRotationAngle", float) = 0.0
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
 
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows vertex:vert
 
        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
 
        sampler2D _MainTex;
        sampler2D _Normal;
        sampler2D _Height;
 
        struct Input {
            float2 uv_MainTex;
            float2 uv_Normal;
            float2 uv_Height;
            float3 viewDir;
        };
 
        float _NormalScale;
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _Parallax;
        float _Rotation;
 
        void vert (inout appdata_full v) {
            const float pi = 3.141592653589793238462;
            
            v.texcoord.xy -=0.5;
            float s = sin ( (_Rotation * (pi * 2 / 360)) );
            float c = cos ( (_Rotation * (pi * 2 / 360)) );
            float2x2 rotationMatrix = float2x2( c, -s, s, c);
            rotationMatrix *=0.5;
            rotationMatrix +=0.5;
            rotationMatrix = rotationMatrix * 2-1;
            v.texcoord.xy = mul ( v.texcoord.xy, rotationMatrix );
            v.texcoord.xy += 0.5;
        }

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)
 
        void surf (Input IN, inout SurfaceOutputStandard o) {
            // Albedo comes from a texture tinted by color
            float heightTex = tex2D(_Height, IN.uv_Height).r;
            float2 parallaxOffset = ParallaxOffset(heightTex, _Parallax, IN.viewDir);
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex + parallaxOffset) * _Color;
            o.Normal = UnpackScaleNormal(tex2D(_Normal, IN.uv_MainTex + parallaxOffset), _NormalScale);
            o.Albedo = c.rgb;
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
    
    
    
    
    //     Properties {
    //         _Color ("Color", Color) = (1,1,1,1)
    //         _MainTex ("Albedo (RGB)", 2D) = "white" {}
    //         [NoScaleOffset] _BumpMap ("NormalMap", 2D) = "bump" {}
    //         _BumpScale ("NormalScale", Float) = 1.000000
    //         _HeightMap ("HeightMap",2D) = "white" {}
    //         _HeightScale ("HeightScale", Float) = 1.000000

    //         _Rotation ("Rotation", Float) = 0.0
    //     }
    //     SubShader {
    //         Tags { "RenderType"="Opaque" }
    //         LOD 200
     
    //         CGPROGRAM
    //         #pragma surface surf Standard fullforwardshadows vertex:vert
    //         #pragma target 3.0
    //         #include "UnityStandardUtils.cginc"
    //         sampler2D _MainTex;
    //         sampler2D _BumpMap;
    //         sampler2D _HeightMap;
    //         struct Input {
    //             float2 uv_MainTex;
    //             float2 uv_HeightMap;
    //             float3 viewDir;
    //         };
    //         float _Rotation;
    //         float _BumpScale;
    //         float _HeightScale;

    //         void vert (inout appdata_full v) {
    //             const float pi = 3.141592653589793238462;
                
    //             v.texcoord.xy -=0.5;
    //             float s = sin ( (_Rotation * (pi * 2 / 360)) );
    //             float c = cos ( (_Rotation * (pi * 2 / 360)) );
    //             float2x2 rotationMatrix = float2x2( c, -s, s, c);
    //             rotationMatrix *=0.5;
    //             rotationMatrix +=0.5;
    //             rotationMatrix = rotationMatrix * 2-1;
    //             v.texcoord.xy = mul ( v.texcoord.xy, rotationMatrix );
    //             v.texcoord.xy += 0.5;
    //         }
     
    //         fixed4 _Color;
    //         void surf (Input IN, inout SurfaceOutputStandard o) {
                
    //             float heightTex = tex2D(_HeightMap, IN.uv_HeightMap).r;
    //             float2 parallaxOffset = ParallaxOffset(heightTex, _HeightScale, IN.viewDir);
                
    //             fixed4 c = tex2D (_MainTex, IN.uv_MainTex + parallaxOffset) * _Color;
    //             o.Albedo = c.rgb;
    //             o.Normal = UnpackScaleNormal (tex2D (_BumpMap, IN.uv_MainTex + parallaxOffset), _BumpScale);
    //         }
    //         ENDCG
    //     }
    //     FallBack "Diffuse"
    // }
     
