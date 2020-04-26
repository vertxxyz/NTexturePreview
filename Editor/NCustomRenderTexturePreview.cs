using UnityEditor;
using UnityEngine;

namespace Vertx
{
	[CustomEditor(typeof(CustomRenderTexture), true), CanEditMultipleObjects]
	public class NCustomRenderTexturePreview : NRenderTexturePreview
	{
		protected override string DefaultEditorString => "UnityEditor.CustomRenderTextureEditor, UnityEditor";
	}
}