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
		}

		#region Assets
		protected static T LoadResource<T>(string nameWithExtension) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"Packages/com.vertx.ntexturepreview/Editor Resources/{nameWithExtension}");
		
		protected static Material m_Material3D;
		protected static Material material3D
		{
			get
			{
				if (m_Material3D == null)
					m_Material3D = new Material(LoadResource<Shader>("RGB3DShader.shader"));
				return m_Material3D;
			}
		}

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

		protected Editor defaultEditor;
		private bool m_R = true, m_G = true, m_B = true;

		private void SetRGBTo(bool R, bool G, bool B)
		{
			m_R = R;
			rCallback?.Invoke(m_R);
			m_G = G;
			gCallback?.Invoke(m_G);
			m_B = B;
			bCallback?.Invoke(m_B);
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
					m_R = !GUILayout.Toggle(!m_R, "R", s_Styles.previewButton_R);
					if (changeCheckScope.changed)
					{
						if (!Event.current.control)
						{
							rCallback?.Invoke(m_R);
						}
						else
							SetRGBTo(true, false, false);

						Repaint();
					}
				}

				if (m_R)
					allOff = false;
			}

			if (hasG)
			{
				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
				{
					m_G = !GUILayout.Toggle(!m_G, "G", s_Styles.previewButton_G);
					if (changeCheckScope.changed)
					{
						if (!Event.current.control)
						{
							gCallback?.Invoke(m_G);
						}
						else
							SetRGBTo(false, true, false);

						Repaint();
					}
				}

				if (m_G)
					allOff = false;
			}

			if (hasB)
			{
				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
				{
					m_B = !GUILayout.Toggle(!m_B, "B", s_Styles.previewButton_B);
					if (changeCheckScope.changed)
					{
						if (!Event.current.control)
						{
							bCallback?.Invoke(m_B);
						}
						else
							SetRGBTo(false, false, true);

						Repaint();
					}
				}

				if (m_B)
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
				scaleIcon = new GUIContent(EditorGUIUtility.IconContent("ViewToolZoom On")) {image = {filterMode = FilterMode.Bilinear}, tooltip = "Reset Zoom"};
				RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
				playIcon = new GUIContent(EditorGUIUtility.IconContent("d_Animation.Play")) {tooltip = "Continuous Repaint"};
				previewButton = "preButton";
				previewSlider = "preSlider";
				previewSliderThumb = "preSliderThumb";
				previewDropDown = "PreDropDown";

				previewButtonScale = new GUIStyle(previewButton)
				{
					padding = new RectOffset(0, 0, 3, 2),
					fixedWidth = 20
				};
				previewLabel = new GUIStyle("preLabel")
				{
					// UpperCenter centers the mip icons vertically better than MiddleCenter
					alignment = TextAnchor.UpperCenter
				};
				previewButton_R = new GUIStyle(previewButton)
				{
					padding = new RectOffset(5, 5, 0, 0),
					alignment = TextAnchor.MiddleCenter,
					normal = {textColor = new Color(1f, 0.28f, 0.33f)}
				};
				previewButton_G = new GUIStyle(previewButton_R) {normal = {textColor = new Color(0.45f, 1f, 0.28f)}};
				previewButton_B = new GUIStyle(previewButton_R) {normal = {textColor = new Color(0f, 0.65f, 1f)}};
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