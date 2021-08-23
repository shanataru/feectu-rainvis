Shader "Octane/Test"
{
	Properties
	{
		orbxUid ("orbx", Int) = 1
		displayTexture("", 2D) = "white" {}
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		
		#pragma surface surf StandardSpecular fullforwardshadows
		#pragma target 4.0
		
		struct Input
		{
			float2 uvdisplayTexture;
		};

		sampler2D displayTexture;
		
		void surf (Input IN, inout SurfaceOutputStandardSpecular o)
		{
			o.Albedo = tex2D(displayTexture, IN.uvdisplayTexture).xyz;
		}
		ENDCG
	}
	
	FallBack "StandardSpecular"
	CustomEditor "OctaneUserShaderGUI"
}

