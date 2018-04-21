using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx
{
	public class NTexturePreviewBase : Editor
	{
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
			if(rCallback != null)
				rCallback.Invoke(m_R);
			
			m_G = G;
			if(gCallback != null)
				gCallback.Invoke(m_G);
			m_B = B;
			if(bCallback != null)
				bCallback.Invoke(m_B);
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
							if(rCallback != null)
								rCallback.Invoke(m_R);
						} else
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
							if(gCallback != null)
								gCallback.Invoke(m_G);
						} else
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
							if(bCallback != null)
								bCallback.Invoke(m_B);
						} else
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

		public override string GetInfoString()
		{
			return defaultEditor.GetInfoString();
		}

		protected class Styles
		{
			public readonly GUIContent smallZoom, largeZoom, alphaIcon, RGBIcon, scaleIcon;
			public readonly GUIStyle previewButton, previewSlider, previewSliderThumb, previewLabel, previewDropDown;
			public readonly GUIStyle previewButton_R, previewButton_G, previewButton_B;

			public readonly GUIContent wrapModeLabel = TrTextContent("Wrap Mode");
			public readonly GUIContent wrapU = TrTextContent("U axis");
			public readonly GUIContent wrapV = TrTextContent("V axis");
			public readonly GUIContent wrapW = TrTextContent("W axis");

			public readonly GUIContent[] wrapModeContents =
			{
				TrTextContent("Repeat"),
				TrTextContent("Clamp"),
				TrTextContent("Mirror"),
				TrTextContent("Mirror Once"),
				TrTextContent("Per-axis")
			};

			public readonly int[] wrapModeValues =
			{
				(int) TextureWrapMode.Repeat,
				(int) TextureWrapMode.Clamp,
				(int) TextureWrapMode.Mirror,
				(int) TextureWrapMode.MirrorOnce,
				-1
			};

			public Styles()
			{
				smallZoom = EditorGUIUtility.IconContent("PreTextureMipMapLow");
				largeZoom = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
				alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
				scaleIcon = EditorGUIUtility.IconContent("ViewToolZoom On");
				RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
				previewButton = "preButton";
				previewSlider = "preSlider";
				previewSliderThumb = "preSliderThumb";
				previewDropDown = "PreDropDown";
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

			private static MethodInfo _TrTextContent;

			private static GUIContent TrTextContent(string s)
			{
				if (_TrTextContent == null)
					_TrTextContent = typeof(EditorGUIUtility).GetMethod("TrTextContent", BindingFlags.NonPublic | BindingFlags.Static, null, new[] {typeof(string), typeof(string), typeof(Texture)}, null);
				return (GUIContent) _TrTextContent.Invoke(null, new object[] {s, null, null});
			}
		}

		private static Styles _s_Styles;

		protected static Styles s_Styles
		{
			get { return _s_Styles ?? (_s_Styles = new Styles()); }
		}

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