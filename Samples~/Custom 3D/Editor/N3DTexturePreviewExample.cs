#if !UNITY_2020_1_OR_NEWER
using UnityEngine;

namespace Vertx
{
	// ReSharper disable once UnusedMember.Global
	public class N3DTexturePreviewExample : N3DTexturePreview.I3DMaterialOverride
	{
		public Material GetMaterial(Texture texture3D) =>
			!texture3D.name.Equals("3DTexturePreviewExample") ? null : new Material(Resources.Load<Shader>("RGBARaymarchShader"));

		public bool ImplementAxisSliders() => false;
	}
}
#endif