using System;
using System.Reflection;
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
			defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.RenderTextureEditor, UnityEditor"));
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			//When OnDisable is called, the default editor we created should be destroyed to avoid memory leakage.
			//Also, make sure to call any required methods like OnDisable
			MethodInfo disableMethod = defaultEditor.GetType().GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (disableMethod != null)
				disableMethod.Invoke(defaultEditor, null);
			DestroyImmediate(defaultEditor);
		}

		public override void OnInspectorGUI()
		{
			defaultEditor.OnInspectorGUI();
			RenderTexture rt = target as RenderTexture;
			if (rt != null && IsVolume() && rt.depth != 0)
			{
				EditorGUILayout.HelpBox(noSupportFor3DWithDepth, MessageType.Error);
			}
		}

		public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) => defaultEditor.RenderStaticPreview(assetPath, subAssets, width, height);
	}
}