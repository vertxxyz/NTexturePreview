using UnityEngine;

namespace Vertx
{
	public class N3DTexturePreviewExample : N3DTexturePreview.I3DMaterialOverride
	{
		private Material m_material;
		public Material GetMaterial(Texture3D texture3D)
		{
			if (!texture3D.name.Equals("3DTexturePreviewExample"))
				return null;
			if (m_material == null)
				m_material = new Material(Resources.Load<Shader>("RGBA3DShader"));
			return m_material;
		}
	}
}