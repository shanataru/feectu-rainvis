//Shader "Custom/DepthShader"
//{
//	Properties
//	{
//		_MainTex("Texture", 2D) = "white" {}
//	}
//	SubShader
//	{
//		Tags { "RenderType" = "Opaque" }
//
//		Pass
//		{
//
//			CGPROGRAM
//			#pragma target 3.0
//			#pragma vertex vert
//			#pragma fragment frag
//			#include "UnityCG.cginc"
//
//			struct v2f
//			{
//				float4 pos : SV_POSITION;
//				float4 uv : TEXCOORD0;
//				float4 projPos : TEXCOORD1; //Screen position of pos
//			};
//
//			v2f vert(appdata_base v)
//			{
//				v2f o;
//				o.pos = UnityObjectToClipPos(v.vertex);
//				o.uv = o.uv;
//				o.projPos = ComputeScreenPos(o.pos);
//				return o;
//			}
//
//
//			uniform sampler2D _CameraDepthTexture; //the depth texture
//			sampler2D _MainTex;
//
//			half4 frag(v2f i) : COLOR
//			{
//				//Grab the depth value from the depth texture
//				//Linear01Depth restricts this value to [0, 1]
//				float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r);
//				//half4 c = tex2Dproj(_MainTex, i.uv);
//
//				half4 c;
//				c.r = depth;
//				c.g = depth;
//				c.b = depth;
//				c.a = 1;
//
//				return c;
//			}
//
//			ENDCG
//		}
//	}
//}

Shader "Custom/Depth"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float4 projPos : TEXCOORD1;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 projPos : TEXCOORD1;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.projPos = ComputeScreenPos(o.vertex);
				return o;
			}

			sampler2D _MainTex;
			sampler2D _CameraDepthTexture; //the depth texture

			float4 frag(v2f i) : SV_Target
			{
				//Grab the depth value from the depth texture
				//Linear01Depth restricts this value to [0, 1]
				float depth = Linear01Depth(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)).r);
				half4 col;
				col.r = depth;
				col.g = depth;
				col.b = depth;
				col.a = 1.0f;

				return col;

				//float4 col = tex2D(_MainTex, i.uv);
				//return col;
			}
			ENDCG
		}
	}
}