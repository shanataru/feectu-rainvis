/*
<summary>

Calculates 2D wave equations, previous frame results (up to two frames) are stored in red and green channel of the custom render texture.
Source: http://tips.hecomi.com/entry/2017/05/17/020037, accessed 20 May, 2020

Added blue noise to disrupt the perfection of a ripple
	- used to modify phase velocity (pass0)
	- used to define size of a splash (pass1)
 
</summary>
*/

Shader "Custom/WaterSurfaceSimulation"
{

    Properties
    {
        _S2("PhaseVelocity ^ 2" , Range(0.0 , 0.5)) = 0.3
        //[PowerSlider(0.01)]
        _Atten("Attenuation" , Range(0.1 , 1.0)) = 0.92
        //_DeltaUV("Delta UV" , Float) = 3

		[NoScaleOffset] _BlueNoise("Noise", 2D) = "grey" {}
    }

    CGINCLUDE
    #include "UnityCustomRenderTexture.cginc"

    half _S2;
    half _Atten;
    //float _DeltaUV;
	sampler2D _BlueNoise;

    float4 frag(v2f_customrendertexture i) : SV_Target
    {
		
        float2 uv = i.globalTexcoord;

        float du = 1.0 / _CustomRenderTextureWidth; //texel size
        float dv = 1.0 / _CustomRenderTextureHeight;
        //float3 duv = float3 (du, dv, 0) * _DeltaUV;
        float3 duv = float3 (du, dv, 0);
		
		float noise = tex2D(_BlueNoise, uv); 
		_S2 = _S2 - (noise*0.01f);

        float2 c = tex2D(_SelfTexture2D, uv); //previous
        float p = (2 * c.r - c.g + _S2 * (
            tex2D(_SelfTexture2D, uv - duv.zy).r +
            tex2D(_SelfTexture2D, uv + duv.zy).r +
            tex2D(_SelfTexture2D, uv - duv.xz).r +
            tex2D(_SelfTexture2D, uv + duv.xz).r
			- 4 * c.r)) * _Atten;

		//Hugo Elias
        //float p = ((
        //    tex2D(_SelfTexture2D, uv - duv.zy).r +
        //    tex2D(_SelfTexture2D, uv + duv.zy).r +
        //    tex2D(_SelfTexture2D, uv - duv.xz).r +
        //    tex2D(_SelfTexture2D, uv + duv.xz).r)/2 - c.g) * _Atten;

        return float4 (p, c.r, 0 , 0);
    }

		//pass 1
		float4 frag_contact_ripple(v2f_customrendertexture i) : SV_Target
    { 
        return float4(-0.5, 0, 0, 0);
    }

        ENDCG

        SubShader
    {
        Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            Name "Update"
            CGPROGRAM
            #pragma vertex CustomRenderTextureVertexShader 
            #pragma fragment frag
            ENDCG
        }

        Pass
        {
            Name "ContactRipple"
            CGPROGRAM
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag_contact_ripple
            ENDCG
        }
    }
}