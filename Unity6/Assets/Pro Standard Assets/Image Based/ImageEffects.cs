using UnityEngine;

/// Blending modes use by the ImageEffects.Blit functions.
public enum BlendMode {
	Copy,			
	Multiply, 
	MultiplyDouble, 
	Add, 
	AddSmoooth, 
	Blend
}

/// A Utility class for performing various image based rendering tasks.
[AddComponentMenu("")]
public class ImageEffects {
	static Material[] m_BlitMaterials = {null, null, null, null, null, null};
	
	static public Material GetBlitMaterial (BlendMode mode) {
		int index = (int)mode;
		
		if (m_BlitMaterials[index] != null)
			return m_BlitMaterials[index];
			
		// The blit shaders were originally compiled from source strings, one per
		// blend mode. They now share one shader asset with per-material blend factors.
		Shader shader = Resources.Load<Shader>("ImageEffectsBlit");
		UnityEngine.Rendering.BlendMode[,] factors = {
			{ UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero },                     // Copy
			{ UnityEngine.Rendering.BlendMode.DstColor, UnityEngine.Rendering.BlendMode.Zero },                // Multiply
			{ UnityEngine.Rendering.BlendMode.DstColor, UnityEngine.Rendering.BlendMode.SrcColor },            // MultiplyDouble
			{ UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.One },                      // Add
			{ UnityEngine.Rendering.BlendMode.OneMinusDstColor, UnityEngine.Rendering.BlendMode.One },         // AddSmoooth
			{ UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha },    // Blend
		};
		for( int i = 0; i < m_BlitMaterials.Length; ++i ) {
			m_BlitMaterials[i] = new Material (shader);
			m_BlitMaterials[i].SetFloat ("_SrcBlend", (float)factors[i, 0]);
			m_BlitMaterials[i].SetFloat ("_DstBlend", (float)factors[i, 1]);
			m_BlitMaterials[i].hideFlags = HideFlags.HideAndDontSave;
		}
		return m_BlitMaterials[index];
	}
	
		
	/// Copies one render texture onto another.
	///  This function copies /source/ onto /dest/, optionally using a custom blend mode.
	/// If /blendMode/ is left out, the default operation is simply to copy one texture on to another.
	/// This function will copy the whole source texture on to the whole destination texture. If the sizes differ, 
	/// the image in the source texture will get stretched to fit.
	/// The source and destination textures cannot be the same.
	public static void Blit (RenderTexture source, RenderTexture dest, BlendMode blendMode) {
		Blit (source, new Rect (0,0,1,1), dest, new Rect (0,0,1,1), blendMode);
	}
	public static void Blit (RenderTexture source, RenderTexture dest) {		
		Blit (source, dest, BlendMode.Copy);
	}

	/// Copies one render texture onto another.
	public static void Blit (RenderTexture source, Rect sourceRect, RenderTexture dest, Rect destRect, BlendMode blendMode) {
		// Make the destination texture the target for all rendering
		RenderTexture.active = dest;  		
		// Assign the source texture to a property from a shader
		Shader.SetGlobalTexture ("_RenderTex", source);
		bool invertY = source.texelSize.y < 0.0f;
		// Set up the simple Matrix
		GL.PushMatrix ();
		GL.LoadOrtho ();
		Material blitMaterial = GetBlitMaterial(blendMode);
		for (int i = 0; i < blitMaterial.passCount; i++) {
			blitMaterial.SetPass (i);
			DrawQuad(invertY);
		}
		GL.PopMatrix ();
	}
	
	public static void BlitWithMaterial (Material material, RenderTexture source, RenderTexture destination)
	{
		Graphics.Blit (source, destination, material);
	}
	
	
	public static void RenderDistortion (Material material, RenderTexture source, RenderTexture destination, float angle, Vector2 center, Vector2 radius)
	{
		bool invertY = source.texelSize.y < 0.0f;
		if (invertY) {
			center.y = 1.0f-center.y;
			angle = -angle;
		}
		
		Matrix4x4 rotationMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0, 0, angle), Vector3.one);
				
		material.SetMatrix("_RotationMatrix", rotationMatrix);
		material.SetVector("_CenterRadius", new Vector4(center.x,center.y,radius.x,radius.y) );
		material.SetFloat("_Angle", angle * Mathf.Deg2Rad);
		
		Graphics.Blit (source, destination, material);
	}
	
	
	public static void DrawQuad(bool invertY)
	{
		GL.Begin (GL.QUADS);
		float y1, y2;
		if (invertY) {
			y1 = 1.0f; y2 = 0.0f;
		} else {
			y1 = 0.0f; y2 = 1.0f;
		}
		GL.TexCoord2( 0.0f, y1 ); GL.Vertex3( 0.0f, 0.0f, 0.1f );
		GL.TexCoord2( 1.0f, y1 ); GL.Vertex3( 1.0f, 0.0f, 0.1f );
		GL.TexCoord2( 1.0f, y2 ); GL.Vertex3( 1.0f, 1.0f, 0.1f );
		GL.TexCoord2( 0.0f, y2 ); GL.Vertex3( 0.0f, 1.0f, 0.1f );
		GL.End();
	}
	
	public static void DrawGrid (int xSize, int ySize)
	{
		GL.Begin (GL.QUADS);
		
		float xDelta = 1.0F / xSize;
		float yDelta = 1.0F / ySize;
		
		for (int y=0;y<xSize;y++)
		{
			for (int x=0;x<ySize;x++)
			{
				GL.TexCoord2 ((x+0) * xDelta, (y+0) * yDelta); GL.Vertex3 ((x+0) * xDelta, (y+0) * yDelta, 0.1f);
				GL.TexCoord2 ((x+1) * xDelta, (y+0) * yDelta); GL.Vertex3 ((x+1) * xDelta, (y+0) * yDelta, 0.1f);
				GL.TexCoord2 ((x+1) * xDelta, (y+1) * yDelta); GL.Vertex3 ((x+1) * xDelta, (y+1) * yDelta, 0.1f);
				GL.TexCoord2 ((x+0) * xDelta, (y+1) * yDelta); GL.Vertex3 ((x+0) * xDelta, (y+1) * yDelta, 0.1f);
			}
		}
		GL.End();
	}
}
