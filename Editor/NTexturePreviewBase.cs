using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx
{
	public class NTexturePreviewBase : Editor
	{
		protected virtual void OnDisable()
		{
			SetRGBTo(true, true, true);
			if (_defaultEditor != null)
			{
				DestroyImmediate(_defaultEditor);
				_defaultEditor = null;
			}
		}

		#region Assets
		protected static T LoadResource<T>(string nameWithExtension) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"Packages/com.vertx.ntexturepreview/Editor Resources/{nameWithExtension}");

		private Mesh mesh;
		protected Mesh Mesh
		{
			get
			{
				if (mesh == null)
					mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
				return mesh;
			}
		}
		#endregion
		
		private const string continuousRepaintPrefsKey = "vertx_ContinuousRepaint";

		private bool continuousRepaint;
		//Render texture repaint (play)
		protected bool ContinuousRepaint
		{
			get => continuousRepaint;
			set
			{
				continuousRepaint = value;
				EditorPrefs.SetBool(continuousRepaintPrefsKey, continuousRepaint);
			}
		}

		protected virtual void OnEnable() => continuousRepaint = EditorPrefs.GetBool(continuousRepaintPrefsKey);

		protected enum TextureUsageMode
		{
			Default = 0,

			BakedLightmapDoubleLDR = 1,

			BakedLightmapRGBM = 2,

			NormalmapDXT5nm = 3,

			NormalmapPlain = 4,
			RGBMEncoded = 5,

			AlwaysPadded = 6,
			DoubleLDR = 7,

			BakedLightmapFullHDR = 8,
			RealtimeLightmapRGBM = 9,
		}

		protected virtual string DefaultEditorString => "UnityEditor.TextureInspector, UnityEditor";
		
		private Editor _defaultEditor;
		protected Editor defaultEditor
		{
			get
			{
				if (_defaultEditor != null) return _defaultEditor;
				return _defaultEditor = CreateEditor(targets, Type.GetType(DefaultEditorString));
			}
			set
			{
				if(_defaultEditor != null)
					DestroyImmediate(_defaultEditor);
				_defaultEditor = value;
			}
		}

		private bool toggleR = true, toggleG = true, toggleB = true;

		private void SetRGBTo(bool r, bool g, bool b)
		{
			if (r == toggleR && g == toggleG && b == toggleB)
			{
				r = true;
				g = true;
				b = true;
			}
			
			toggleR = r;
			rCallback?.Invoke(toggleR);
			toggleG = g;
			gCallback?.Invoke(toggleG);
			toggleB = b;
			bCallback?.Invoke(toggleB);
		}

		protected Action<bool> rCallback;
		protected Action<bool> gCallback;
		protected Action<bool> bCallback;

		/// <summary>
		/// Use the r, g, and b Callbacks to recieve feedback from this function.
		/// </summary>
		/// <param name="hasR">Show the R toggle?</param>
		/// <param name="hasG">Show the G toggle?</param>
		/// <param name="hasB">Show the B toggle?</param>
		protected void DrawRGBToggles(bool hasR, bool hasG, bool hasB)
		{
			bool allOff = true;
			if (hasR)
			{
				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
				{
					bool newR = !GUILayout.Toggle(!toggleR, "R", s_Styles.previewButton_R);
					if (changeCheckScope.changed)
					{
						if (!Event.current.control)
							SetRGBTo(true, false, false);
						else
						{
							rCallback?.Invoke(newR);
							toggleR = newR;
						}


						Repaint();
					}
				}

				if (toggleR)
					allOff = false;
			}

			if (hasG)
			{
				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
				{
					bool newG = !GUILayout.Toggle(!toggleG, "G", s_Styles.previewButton_G);
					if (changeCheckScope.changed)
					{
						if (!Event.current.control)
							SetRGBTo(false, true, false);
						else
						{
							gCallback?.Invoke(newG);
							toggleG = newG;
						}

						Repaint();
					}
				}

				if (toggleG)
					allOff = false;
			}

			if (hasB)
			{
				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
				{
					bool newB = !GUILayout.Toggle(!toggleB, "B", s_Styles.previewButton_B);
					if (changeCheckScope.changed)
					{
						if (!Event.current.control)
							SetRGBTo(false, false, true);
						else
						{
							bCallback?.Invoke(newB);
							toggleB = newB;
						}

						Repaint();
					}
				}

				if (toggleB)
					allOff = false;
			}

			if (allOff)
			{
				SetRGBTo(true, true, true);
				Repaint();
			}
		}

		public override string GetInfoString() => defaultEditor.GetInfoString();

		protected class Styles
		{
			public readonly GUIContent smallZoom, largeZoom, alphaIcon, RGBIcon, scaleIcon, playIcon;
			public readonly GUIStyle previewButton, previewSlider, previewSliderThumb, previewLabel, previewDropDown;
			public readonly GUIStyle previewButtonScale;
			public readonly GUIStyle previewButton_R, previewButton_G, previewButton_B;

			public Styles()
			{
				smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
				largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
				alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
				RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
				playIcon = new GUIContent(EditorGUIUtility.IconContent("d_Animation.Play")) {tooltip = "Continuous Repaint"};
				#if UNITY_2019_3_OR_NEWER
				previewButton = new GUIStyle("preButton")
				{
					stretchHeight = true,
					fixedHeight = 0
				};
				scaleIcon = new GUIContent(EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "ScaleTool On" : "ScaleTool")) {image = {filterMode = FilterMode.Bilinear}, tooltip = "Reset Zoom"};
				#else
				previewButton = "preButton";
				scaleIcon = new GUIContent(EditorGUIUtility.IconContent("ViewToolZoom On")) {image = {filterMode = FilterMode.Bilinear}, tooltip = "Reset Zoom"};
				#endif
				previewSlider = "preSlider";
				previewSliderThumb = "preSliderThumb";
				previewDropDown = "PreDropDown";

				previewButtonScale = new GUIStyle(previewButton)
				{
					padding = new RectOffset(0, 0, 3, 2),
					fixedWidth = 20
				};
				previewLabel = new GUIStyle("preLabel")
					#if UNITY_2019_3_OR_NEWER
					;
				if(!EditorGUIUtility.isProSkin)
					previewLabel.normal.textColor = new Color(0.22f, 0.22f, 0.22f);
				#else
				{
					// UpperCenter centers the mip icons vertically better than MiddleCenter
					alignment = TextAnchor.UpperCenter
				};
				#endif
				previewButton_R = new GUIStyle(previewButton)
				{
					padding = new RectOffset(5, 5, 0, 0),
					alignment = TextAnchor.MiddleCenter,
					normal = {textColor = new Color(1f, 0.28f, 0.33f)}
				};
				previewButton_G = new GUIStyle(previewButton_R) {normal =
				{
					#if UNITY_2019_3_OR_NEWER
					textColor = new Color(0f, 0.698f, 0.062f)
					#else
					textColor = new Color(0.45f, 1f, 0.28f)
					#endif
				}};
				previewButton_B = new GUIStyle(previewButton_R) {normal = {textColor = new Color(0f, 0.65f, 1f)}};

				UnifyGUIStyle(previewButton_R);
				UnifyGUIStyle(previewButton_G);
				UnifyGUIStyle(previewButton_B);
				
				void UnifyGUIStyle(GUIStyle style)
				{
					Color color = style.normal.textColor;
					//style.onNormal.textColor = color;
					style.active.textColor = color;
					style.onActive.textColor = color;
					style.focused.textColor = color;
					style.onFocused.textColor = color;
					style.hover.textColor = color;
					style.onHover.textColor = color;
					Texture2D[] backgrounds = style.normal.scaledBackgrounds;
					style.onNormal.scaledBackgrounds = backgrounds;
					style.focused.scaledBackgrounds = backgrounds;
					style.onFocused.scaledBackgrounds = backgrounds;
					style.hover.scaledBackgrounds = backgrounds;
					style.onHover.scaledBackgrounds = backgrounds;
				}
			}
		}

		private static Styles _s_Styles;
		protected static Styles s_Styles => _s_Styles ?? (_s_Styles = new Styles());

		protected static void DrawRect(Rect rect)
		{
			GL.Vertex(new Vector3(rect.xMin, rect.yMin, 0f));
			GL.Vertex(new Vector3(rect.xMax, rect.yMin, 0f));
			GL.Vertex(new Vector3(rect.xMax, rect.yMin, 0f));
			GL.Vertex(new Vector3(rect.xMax, rect.yMax, 0f));
			GL.Vertex(new Vector3(rect.xMax, rect.yMax, 0f));
			GL.Vertex(new Vector3(rect.xMin, rect.yMax, 0f));
			GL.Vertex(new Vector3(rect.xMin, rect.yMax, 0f));
			GL.Vertex(new Vector3(rect.xMin, rect.yMin, 0f));
		}
	}
}