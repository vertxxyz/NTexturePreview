using System;
using UnityEditor;
using UnityEngine;

namespace Vertx
{
	[CustomEditor(typeof(CustomRenderTexture), true), CanEditMultipleObjects]
	public class NCustomRenderTexturePreview : NRenderTexturePreview
	{
		protected override void OnEnable()
		{
			//When this inspector is created, also create the built-in inspector
			if (defaultEditor == null)
				defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.CustomRenderTextureEditor, UnityEditor"));

			base.OnEnable();
		}
	}
}