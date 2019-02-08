Shader "Unlit/Polar"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Radius("Radius", Range(0.0, 360.0)) = 240.0 
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

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
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_TexelSize;
			float4 _MainTex_ST;
			float _Radius;


			inline float2 RadialCoords(float3 a_coords)
			{
				float FOV = UNITY_PI / 180 * _Radius; // FOV of the fisheye, eg: 240 degrees

				// Calculate fisheye angle and radius
				float theta = atan2(a_coords.z, a_coords.x);
				float phi = atan2(sqrt(a_coords.x*a_coords.x + a_coords.z*a_coords.z), a_coords.y);
				float r = phi / FOV;

				// Pixel in fisheye space
				return float2(0.5 + r * -sin(theta), 0.5 + r * cos(theta));
			}

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.vertex.y = 1 - o.vertex.y;
				//o.vertex.z = 1 - o.vertex.z;
				//o.vertex.z = 1 - o.vertex.z;
				o.uv = RadialCoords(v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}