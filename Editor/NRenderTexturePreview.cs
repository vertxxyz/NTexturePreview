using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx
{
	[CustomEditor(typeof(RenderTexture), true), CanEditMultipleObjects]
	public class NRenderTexturePreview : NTexturePreview
	{
		new void OnEnable()
		{
			//When this inspector is created, also create the built-in inspector
			if(defaultEditor == null)
				defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.RenderTextureEditor, UnityEditor"));
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			if (defaultEditor != null)
			{
				DestroyImmediate(defaultEditor);
				defaultEditor = null;
			}
		}

		public override void OnInspectorGUI()
		{
			defaultEditor.OnInspectorGUI();
			RenderTexture rt = target as RenderTexture;
			if (rt != null && IsVolume(rt) && rt.depth != 0)
			{
				EditorGUILayout.HelpBox(noSupportFor3DWithDepth, MessageType.Error);
			}
		}

		public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) => defaultEditor.RenderStaticPreview(assetPath, subAssets, width, height);
	}
}