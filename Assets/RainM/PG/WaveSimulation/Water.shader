Shader "Custom/Water"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
		_BumpMap("Normal Map", 2D) = "bump" {}

		//_FoamMap("FoamMap", 2D) = "white"{}

		[NoScaleOffset] _WaterBumpMap("Water Normal Map", 2D) = "bump"{}		//for "waviness" in puddles
		_WaterDispTex("Water Displacement Texture" , 2D) = "gray" {}		//for generating ripples
		_RainDispTex("Rain Displacement Texture" , 2D) = "" {}		//for generating raindrop ripples
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

		uniform sampler2D _ReflectionTex;
		uniform float _ReflIntensity;
		uniform float _ReflDistort;
		uniform float4 _WaveScale4;
		uniform float4 _WaveOffset;
		uniform float _RainDisplacementScale;

        struct Input
        {
			float2 uv_MainTex;
			float2 uv_BumpMap;
			float3 worldPos;
			float4 screenPos;
			float3 viewDir;
        };


		fixed4 _Color;
		half _Glossiness;
		half _Metallic;
		sampler2D _MainTex;
		sampler2D _BumpMap;
		//sampler2D _FoamMap;
		sampler2D _WaterBumpMap;
		sampler2D _WaterDispTex;
		sampler2D _RainDispTex;

		float3 UDNBlending(float3 n1, float3 n2) {
			float3 r = normalize(float3(n1.xy + n2.xy*0.5, n2.z));
			return r;
		}

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			//water surface simulation
			float crt_width = 256.0f;
			float3 ts = float3 (1/crt_width, 1 /crt_width, 0);
			//float h0 = tex2D(_WaterDispTex, IN.uv_BumpMap + ts.zy).r;
			//float h1 = tex2D(_WaterDispTex, IN.uv_BumpMap - ts.zy).r;
			//float h2 = tex2D(_WaterDispTex, IN.uv_BumpMap + ts.xz).r;
			//float h3 = tex2D(_WaterDispTex, IN.uv_BumpMap - ts.xz).r;

			//float3 p0 = normalize(float3(ts.xz, h0 - h1));
			//float3 p1 = normalize(float3(ts.zy, h2 - h3));
			//float3 bump = normalize(cross(p0, p1));

			// scroll bump waves
			float4 temp = IN.worldPos.xzxz * _WaveScale4 + _WaveOffset;
			float3 waveBump0 = UnpackNormal(tex2D(_BumpMap, temp.xy));
			float3 waveBump1 = UnpackNormal(tex2D(_BumpMap, temp.wz));
			float3 waveBump = (waveBump0 + waveBump1)*0.5; // combine two scrolling bump maps into one
			//float3 finalWaterBump = UDNBlending(waveBump, bump);

			//float hrain0 = tex2D(_RainDispTex, IN.uv_BumpMap + ts.zy).r;
			//float hrain1 = tex2D(_RainDispTex, IN.uv_BumpMap - ts.zy).r;
			//float hrain2 = tex2D(_RainDispTex, IN.uv_BumpMap + ts.xz).r;
			//float hrain3 = tex2D(_RainDispTex, IN.uv_BumpMap - ts.xz).r;

			float hrain0 = tex2D(_RainDispTex, (IN.worldPos.xz  + ts.zy)).r;
			float hrain1 = tex2D(_RainDispTex, (IN.worldPos.xz - ts.zy)).r;
			float hrain2 = tex2D(_RainDispTex, (IN.worldPos.xz + ts.xz)).r;
			float hrain3 = tex2D(_RainDispTex, (IN.worldPos.xz - ts.xz)).r;

			float3 prain0 = normalize(float3(ts.xz, hrain0 - hrain1));
			float3 prain1 = normalize(float3(ts.zy, hrain2 - hrain3));
			float3 rainBump = normalize(cross(prain0, prain1));

			float3 finalWaterBump = UDNBlending(waveBump, rainBump);

			//distort reflection by bump waves
			float4 uv1 = IN.screenPos;
			uv1.xy += finalWaterBump * _ReflDistort;
			float4 refl = tex2Dproj(_ReflectionTex, UNITY_PROJ_COORD(uv1));
			float4 water = refl;

			float3 normal = UnpackNormal(tex2D(_WaterBumpMap, IN.uv_BumpMap));
			normal = lerp(normal, float3(finalWaterBump.x, finalWaterBump.y, 1.0), 0); //water normal defined by scrolling bump

			water = lerp(0, water, _ReflIntensity); //reflection in puddles

            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

			//foam?
			//float3 foam = UnpackNormal(tex2D(_FoamMap, IN.uv_MainTex));

			//if (h0 >= 0.8f) {
			//	//c.rgb = foam;
			//	water = float4(foam, 1.0f);
			//}

			o.Albedo = c.rgb;
			o.Normal = normal;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;

			//Enable this for reflection
			o.Emission = water;
        }
        ENDCG
    }
    FallBack "Standard"
}
