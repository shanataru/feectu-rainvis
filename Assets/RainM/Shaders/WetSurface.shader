	/*
<summary>

Surface shader to visually simulate dynamic change from dry to wet road. This shader works with C# script WaterSurfaceSimulation.cs, Reflection.cs

General wet road is defined by two parameters (float, 0.0-1.0): _Wetness and _WaterLevel.
    Wetness parameter changes the road uniformly - making diffuse color darker and the surface is smoother.
    Water level defines how much water is in puddle areas.

The shader takes input from WaterSurfaceSimulation.cs to visually simulates reflection and water waves. 
    The reflection is defined as emission, thus third parameter "_ReflIntensity (reflection intensity)" to soften the render texture. 

Based on research paper: A lighting model aiming at drive simulators (https://dl.acm.org/doi/10.1145/97879.97922)

</summary>
*/


Shader "Custom/WetSurface"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        //following Unity material standard: smoothness is in alpha channel of metallic map
		[NoScaleOffset] _MetallicGlossMap("Metallic", 2D) = "black" {}
		[NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
		_BumpScale("Bump Scale", float) = 1.0
		[NoScaleOffset]_HeightMap("Height Map", 2D) = "grey" {} //TODO [NoScaleOffset]
        _HeightIntensity("Height Intensity", Range(0.00,0.08)) = 0.015
		_HeightContrast("Height Contrast", Range(0.00, 100.00)) = 3.0 //should be hidden after adjustment - set only ONCE
		[NoScaleOffset] _OcclusionMap("Occlusion", 2D) = "white" {}

        //puddle map defines placements of puddles
        //puddle height is a modified height map - this is used to change puddle map so puddles are placed in lower areas of the road (and not completely randomly)
		_PuddleIntensity("Puddle intensity", Range(0.00, 1.50)) = 1.0
        [NoScaleOffset] _PuddleMap("Puddle Map", 2D) = "grey" {}
		_PuddleMapScale("Puddle Map Scale", Range(0.0,40.0)) = 20.0 //should be hidden after adjustment - set only ONCE

        //for "waviness" in puddles
        [NoScaleOffset] _WaveBumpMap("Wave Normal Map", 2D) = "bump"{}

		//for generating ripples - objects, rain
		[NoScaleOffset]_WaterCRT("Water CRT" , 2D) = "bump" {}
		[NoScaleOffset] _RainCRT("Rain CRT" , 2D) = "bump" {}
	}

		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0
		//#include "Tessellation.cginc"

		// Modified by RainManager
		uniform float _Wetness;
		uniform float _WaterLevel;
		
		//reflection render texture
		uniform sampler2D _ReflectionTex;
		uniform float _ReflIntensity;
		uniform float _ReflDistort;
        
		//ambient waves properties
		uniform float4 _WaveScale4;
        uniform float4 _WaveOffset;
		
		//water surface simulation, rain ripples
		uniform int _WaterCRTScale;
		uniform float _WaterCRTSize; //expect square texture
		uniform float _RainCRTSize;

        struct Input
        {
            float2 uv_MainTex;
            //float2 uv_BumpMap; //follow the UV of the main albedo map
            float3 worldPos;
            float4 screenPos;
            float3 viewDir;
        };

        fixed4 _Color;
        sampler2D _MainTex;
        sampler2D _BumpMap;
        sampler2D _HeightMap;
        sampler2D _MetallicGlossMap;
        sampler2D _OcclusionMap;
        sampler2D _PuddleMap;
        sampler2D _WaveBumpMap;
        sampler2D _WaterCRT;
        sampler2D _RainCRT;

        float _HeightIntensity;
		float _HeightContrast;
		float _PuddleIntensity;
		float _PuddleMapScale;

        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        //screen texture blend
        float Screen(float3 a, float3 b) {
            float r = 1.0 - (1.0 - a) * (1.0 - b);
            return r;
        }

		float AdjustContrast(float color, float contrast) {
			#if !UNITY_COLORSPACE_GAMMA
			color = LinearToGammaSpace(color);
			#endif

			color = saturate(lerp(0.5f, color, contrast));

			#if !UNITY_COLORSPACE_GAMMA
			color = GammaToLinearSpace(color);
			#endif
			return color;
		}

		//From: https://blog.selfshadow.com/publications/blending-in-detail/
        float3 UDNBlending(float3 n1, float3 n2) {
            //float3 r = normalize(float3(n1.xy + n2.xy*0.5, n2.z));
			float3 c = float3(2.0, 1.0, 0.0);
			float3 r = n2 * c.yyz + n1.xyz;
			r = r * c.xxx - c.xxy;
			return normalize(r);
            //return r;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			//custom render texture texel size
			//float3 ts_water_crt = float3(1.0f / _WaterCRTSize, 1.0f / _WaterCRTSize, 0);
			//float3 ts_rain_crt = float3(1.0f / _RainCRTSize, 1.0f / _RainCRTSize, 0);
			float3 ts_water_crt = float3 (1.0f / 512, 1.0f / 512, 0);
			float3 ts_rain_crt = float3 (1.0f / 256, 1.0f / 256, 0);

            float2 offset = ParallaxOffset(tex2D(_HeightMap, IN.uv_MainTex).r, _HeightIntensity, IN.viewDir);
            float occlusion = tex2D(_OcclusionMap, IN.uv_MainTex + offset).r;

            // Get the main PBR material
            float4 diffuse = tex2D(_MainTex, IN.uv_MainTex + offset);
			//float4 diffuse = tex2D(_PuddleMap, IN.worldPos.xz / _PuddleMapScale);
            float4 metallicSmooth = tex2D(_MetallicGlossMap, IN.uv_MainTex);
            float3 normal = UnpackNormal(tex2D(_BumpMap, IN.uv_MainTex + offset));
            float3 albedo = diffuse.rgb * _Color.rgb;
            float metallic = metallicSmooth.r;
            float smoothness = metallicSmooth.a; //inverted roughness map?

			//Wetness
			albedo = albedo * lerp(1.0, 0.35, _Wetness); //darken diffuse 
			smoothness = min((smoothness)* lerp(1.0, 2.5, _Wetness), 1.0); //smoother surface

			//puddle placement
			float puddleMap = tex2D(_PuddleMap, IN.worldPos.xz / _PuddleMapScale).r;
			float puddleHeight = AdjustContrast(tex2D(_HeightMap, IN.uv_MainTex).r, _HeightContrast); //raise contrast of height map
			float puddle = Screen(puddleHeight, puddleMap); //change puddles by height of puddles (adapt puddles to road height)

			//Water fills up holes (by puddle mask - Perlin noise)
			float puddleFill = max(0, (_WaterLevel - puddle));
			//float puddleFill = puddle >= _WaterLevel ? 0.0f : 1.0f;
			float a = puddleFill * (_PuddleIntensity + (1.0f - _WaterLevel) * 2.0f); //is water level high enough the fill the lower area?
			//float ip = (a >= 0.5f ? 1.0f : a) * _PuddleIntensity;
			//float p = puddle >= _WaterLevel ? 0.0f : 1.0f;
			albedo = lerp(albedo, albedo * 0.8, a);		//darken diffuse 
			smoothness = lerp(smoothness, 1, a);		//water very smooth

			/*//water surface simulation, car interaction
            float h0 = tex2D(_WaterCRT, IN.uv_MainTex + ts_water_crt.zy).r;
            float h1 = tex2D(_WaterCRT, IN.uv_MainTex - ts_water_crt.zy).r;
            float h2 = tex2D(_WaterCRT, IN.uv_MainTex + ts_water_crt.xz).r;
            float h3 = tex2D(_WaterCRT, IN.uv_MainTex - ts_water_crt.xz).r;*/
			
			//water surface simulation, car interaction -- extract normal map
			float h0 = tex2D(_WaterCRT, (IN.worldPos.xz / (float)_WaterCRTScale + ts_water_crt.zy)).r;
			float h1 = tex2D(_WaterCRT, (IN.worldPos.xz / (float)_WaterCRTScale - ts_water_crt.zy)).r;
			float h2 = tex2D(_WaterCRT, (IN.worldPos.xz / (float)_WaterCRTScale + ts_water_crt.xz)).r;
			float h3 = tex2D(_WaterCRT, (IN.worldPos.xz / (float)_WaterCRTScale - ts_water_crt.xz)).r;

            float3 p0 = normalize(float3(ts_water_crt.xz, h0 - h1));
            float3 p1 = normalize(float3(ts_water_crt.zy, h2 - h3));
            float3 waterBump = normalize(cross(p0, p1));

			//Rain splashes -- extract normal map
			float hr0 = tex2D(_RainCRT, (IN.worldPos.xz/2.0f + ts_rain_crt.zy)).r;
			float hr1 = tex2D(_RainCRT, (IN.worldPos.xz/2.0f - ts_rain_crt.zy)).r;
			float hr2 = tex2D(_RainCRT, (IN.worldPos.xz/2.0f + ts_rain_crt.xz)).r;
			float hr3 = tex2D(_RainCRT, (IN.worldPos.xz/2.0f - ts_rain_crt.xz)).r;

			float3 pr0 = normalize(float3(ts_rain_crt.xz, hr0 - hr1));
			float3 pr1 = normalize(float3(ts_rain_crt.zy, hr2 - hr3));
			float3 rainBump = normalize(cross(pr0, pr1));	

			//mix two types of normal map into one
			float3 waterSurfaceBump = UDNBlending(waterBump, rainBump);

            // scrolling bumpy ambient waves
            float4 temp = IN.worldPos.xzxz * _WaveScale4 + _WaveOffset;
            float3 waveBump0 = UnpackNormal(tex2D(_WaveBumpMap, temp.xy));
			float3 waveBump1 = UnpackNormal(tex2D(_WaveBumpMap, temp.wz));
            float3 waveBump = (waveBump0 + waveBump1)*0.5; // combine two scrolling bump maps into one
            
			//mix two types of normal map into one
			float3 finalWaterBump = UDNBlending(waveBump, waterSurfaceBump);

            //distort reflection by the final water normal map
            float4 uv1 = IN.screenPos;
            uv1.xy += finalWaterBump * _ReflDistort;
            float4 refl = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(uv1));
            float4 water = refl;

            //Normals: enable this for reflection
            //normal = lerp(normal, float3(0, 0, 1), a); // water normal face straight up, noise defined by reflection scroll
            normal = lerp(normal, float3(finalWaterBump.x, finalWaterBump.y, 1.0), a); //water normal defined by scrolling bump

            metallic = lerp(metallic, 0, a);	// water non-metalic
            occlusion = lerp(occlusion, 1, a); //remove occlusion in water
			water = lerp(0, water, _ReflIntensity * a); //reflection in puddles according to puddle depth

			o.Albedo = albedo;
            o.Normal = normal;
            o.Smoothness = smoothness;
            o.Metallic = metallic;
            o.Occlusion = occlusion;
            o.Emission = water; //Enable this for reflection

        }
        ENDCG
    }
    FallBack "Standard"
}
