using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx
{
	[CustomEditor(typeof(Texture3D))]
	[CanEditMultipleObjects]
	public class N3DTexturePreview : NTexturePreviewBase {
		void OnEnable()
		{
			//When this inspector is created, also create the built-in inspector
			defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.Texture3DInspector, UnityEditor"));
		}

		void OnDisable()
		{
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
		}

		public override void OnPreviewSettings()
		{
			defaultEditor.OnPreviewSettings();
			DrawRGBToggles(true, true, true);
		}

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			defaultEditor.OnPreviewGUI(r, background);
		}

		public override bool HasPreviewGUI()
		{
			return defaultEditor.HasPreviewGUI();
		}
	}
}