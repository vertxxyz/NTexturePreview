using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Rendering;

namespace Vertx
{
	[CustomEditor(typeof(Texture2D), true), CanEditMultipleObjects]
	public class NTexturePreview : NTexturePreviewBase
	{
		protected void OnEnable()
		{
			//When this inspector is created, also create the built-in inspector
			if(defaultEditor == null)
				defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.TextureInspector, UnityEditor"));
			animatedPos = new AnimVector3(Vector3.zero, () =>
			{
				m_Pos = animatedPos.value;
				Repaint();
			})
			{
				speed = 1.5f
			};
			rCallback = r => {
				rGBAMaterial.SetFloat("_R", r ? 1 : 0);
				rGBATransparentMaterial.SetFloat("_R", r ? 1 : 0);
			};
			gCallback = g => {
				rGBAMaterial.SetFloat("_G", g ? 1 : 0);
				rGBATransparentMaterial.SetFloat("_G", g ? 1 : 0);
			};
			bCallback = b => {
				rGBAMaterial.SetFloat("_B", b ? 1 : 0);
				rGBATransparentMaterial.SetFloat("_B", b ? 1 : 0);
			};
		}

		[SerializeField] float m_MipLevel;

		[SerializeField] protected Vector2 m_Pos;
		
		private static Material _rGBAMaterial;

		protected static Material rGBAMaterial
		{
			get
			{
				if (_rGBAMaterial == null)
					_rGBAMaterial = Resources.Load<Material>("RGBMaterial");
				return _rGBAMaterial;
			}
		}

		private static Material _rGBATransparentMaterial;

		protected static Material rGBATransparentMaterial
		{
			get
			{
				if (_rGBATransparentMaterial == null)
					_rGBATransparentMaterial = Resources.Load<Material>("RGBAMaterial");
				return _rGBATransparentMaterial;
			}
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

		public override bool HasPreviewGUI()
		{
			return target != null;
		}

		private float zoomLevel;
		private float zoomMultiplier = 1;
		private const float maxZoomNormalized = 6;

		private bool hasDragged;

		//Using this for animated recentering
		private static AnimVector3 animatedPos;

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			Event e = Event.current;

			if (e.type == EventType.Repaint)
				background.Draw(r, false, false, false, false);

			// show texture
			Texture t = target as Texture;
			if (t == null) // texture might be gone by now, in case this code is used for floating texture preview
				return;

			PreviewTexture(r, t, background, e);
		}

		private void PreviewTexture(Rect r, Texture t, GUIStyle background, Event e)
		{
			// Render target must be created before we can display it (case 491797)
			RenderTexture rt = t as RenderTexture;
			if (rt != null)
			{
				if (!SystemInfo.SupportsRenderTextureFormat(rt.format))
					return; // can't do this RT format
				rt.Create();
			}

			if (IsCubemap())
			{
				//TODO perhaps support custom cubemap settings. Not currently!
				defaultEditor.OnPreviewGUI(r, background);
				return;
			}

			if (r.width == 32 && r.height == 32 || r.width == 1 && r.height == 1)
			{
				//There seems to be some unhelpful layout and repaint steps that provide rect scales that are unhelpful...
				return;
			}

			// target can report zero sizes in some cases just after a parameter change;
			// guard against that.
			int texWidth = Mathf.Max(t.width, 1);
			int texHeight = Mathf.Max(t.height, 1);

			float mipLevel = GetMipLevelForRendering();
			zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
			Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel * zoomMultiplier, texHeight * zoomLevel * zoomMultiplier);

			if (e.type == EventType.MouseDown)
			{
				hasDragged = false;
			}

			if (e.type == EventType.MouseDrag)
			{
				//Don't allow dragging for zoomMultiplier 1
				if (Math.Abs(zoomMultiplier - 1) < 0.001f)
				{
					e.Use();
					return;
				}

				hasDragged = true;
				m_Pos -= e.delta;
				m_Pos = ClampPos(m_Pos, r, texWidth, texHeight, zoomLevel);
				e.Use();
				Repaint();
			}

			if (!hasDragged && e.type == EventType.MouseUp && e.button == 2)
			{
				animatedPos.value = m_Pos;
				Vector2 tgt = ClampPos(m_Pos + ConvertPositionToLocalTextureRect(r, e.mousePosition), r, texWidth, texHeight, zoomLevel);
				animatedPos.target = tgt;
				e.Use();
			}

			if (e.type == EventType.ScrollWheel)
			{
				float zoomMultiplierLast = zoomMultiplier;
				zoomMultiplier = Mathf.Max(1, zoomMultiplier - e.delta.y * 0.1f);
				//Maximum 2x texture zoom
				zoomMultiplier = Mathf.Clamp(zoomMultiplier, 1, maxZoomNormalized / zoomLevel);

				//if zoom has changed
				if (Math.Abs(zoomMultiplierLast - zoomMultiplier) > 0.001f)
				{
					//Focuses Center
					Vector2 posNormalized = m_Pos / new Vector2(wantedRect.width, wantedRect.height);
					Vector2 newPos = new Vector2(posNormalized.x * (texWidth * zoomLevel * zoomMultiplier), posNormalized.y * (texHeight * zoomLevel * zoomMultiplier));
					m_Pos = newPos;
					m_Pos = ClampPos(m_Pos, r, texWidth, texHeight, zoomLevel);
				}

				e.Use();
				Repaint();
			}


			{
				//SCROLL VIEW -----------------------------------------------------------------------------
				PreviewGUIBeginScrollView(r, m_Pos, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

//            FilterMode oldFilter = t.filterMode;
//            SetFilterModeNoDirty(t, FilterMode.Point);

				Texture2D t2d = t as Texture2D;
				if (m_ShowAlpha)
					EditorGUI.DrawTextureAlpha(wantedRect, t, ScaleMode.StretchToFill, 0, mipLevel);
				else if (t2d != null && t2d.alphaIsTransparency)
				{
					RenderTexture renderTexture = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
					renderTexture.filterMode = t.filterMode;
					RenderTexture rTActive = RenderTexture.active;
					Graphics.Blit(t, renderTexture, rGBATransparentMaterial);
					RenderTexture.active = rTActive;
					EditorGUI.DrawTextureTransparent(wantedRect, renderTexture, ScaleMode.StretchToFill, 0, mipLevel);
					RenderTexture.ReleaseTemporary(renderTexture);
				}
				else
				{
					RenderTexture renderTexture = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
					renderTexture.filterMode = t.filterMode;
					RenderTexture rTActive = RenderTexture.active;
					Graphics.Blit(t, renderTexture, rGBAMaterial);
					RenderTexture.active = rTActive;
					EditorGUI.DrawPreviewTexture(wantedRect, renderTexture, null, ScaleMode.StretchToFill, 0, mipLevel); //
					RenderTexture.ReleaseTemporary(renderTexture);
				}

				// TODO: Less hacky way to prevent sprite rects to not appear in smaller previews like icons.
				if (wantedRect.width > 32 && wantedRect.height > 32)
				{
					string path = AssetDatabase.GetAssetPath(t);
					TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
					SpriteMetaData[] spritesheet = textureImporter != null ? textureImporter.spritesheet : null;

					if (spritesheet != null && textureImporter.spriteImportMode == SpriteImportMode.Multiple)
					{
						Rect screenRect = new Rect();
						Rect sourceRect = new Rect();
						GUICalculateScaledTextureRects(wantedRect, ScaleMode.StretchToFill, t.width / (float) t.height, ref screenRect, ref sourceRect);

						int origWidth = t.width;
						int origHeight = t.height;
						TextureImporterGetWidthAndHeight(textureImporter, ref origWidth, ref origHeight);
						float definitionScale = t.width / (float) origWidth;

						ApplyWireMaterial();
						GL.PushMatrix();
						GL.MultMatrix(Handles.matrix);
						GL.Begin(GL.LINES);
						GL.Color(new Color(1f, 1f, 1f, 0.5f));
						foreach (SpriteMetaData sprite in spritesheet)
						{
							Rect spriteRect = sprite.rect;
							Rect spriteScreenRect = new Rect
							{
								xMin = screenRect.xMin + screenRect.width * (spriteRect.xMin / t.width * definitionScale),
								xMax = screenRect.xMin + screenRect.width * (spriteRect.xMax / t.width * definitionScale),
								yMin = screenRect.yMin + screenRect.height * (1f - spriteRect.yMin / t.height * definitionScale),
								yMax = screenRect.yMin + screenRect.height * (1f - spriteRect.yMax / t.height * definitionScale)
							};
							DrawRect(spriteScreenRect);
						}

						GL.End();
						GL.PopMatrix();
					}
				}

				//SetFilterModeNoDirty(t, oldFilter);

				m_Pos = PreviewGUIEndScrollView();
			} //-----------------------------------------------------------------------------------------

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (mipLevel != 0)
				EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 20), "Mip " + mipLevel);
		}

		private static Vector2 ConvertPositionToLocalTextureRect(Rect r, Vector2 position)
		{
			Vector2 rectCenter = new Vector2(r.width / 2f, r.height / 2f);
			Vector2 localPos = new Vector2(position.x - r.x, position.y - r.y);
			localPos -= rectCenter;
			return localPos;
		}

		private const float smallestOnScreenNormalisedRect = 0.5f;

		private Vector2 ClampPos(Vector2 m_PosLocal, Rect r, float textureWidth, float textureHeight, float zoomLevel)
		{
			float w2 = textureWidth * zoomLevel * zoomMultiplier;
			float h2 = textureHeight * zoomLevel * zoomMultiplier;

			w2 /= 2;
			h2 /= 2;
			w2 += r.width / 2f - smallestOnScreenNormalisedRect * r.width;
			h2 += r.height / 2f - smallestOnScreenNormalisedRect * r.height;
			return new Vector2(Mathf.Clamp(m_PosLocal.x, -w2, w2), Mathf.Clamp(m_PosLocal.y, -h2, h2));
		}

		private float GetMipLevelForRendering()
		{
			if (target == null)
				return 0.0f;

			if (IsCubemap())
			{
				throw new NotImplementedException();
				//This should never be called yet by this class, and is handled by the default editor.
				//TODO support cubemap rendering here too
//				return m_CubemapPreview.GetMipLevelForRendering(target as Texture);
			}

			return Mathf.Min(m_MipLevel, GetMipmapCount(target as Texture) - 1);
		}

		private static bool IsNormalMap(Texture t)
		{
			TextureUsageMode mode = GetUsageMode(t);
			return mode == TextureUsageMode.NormalmapPlain || mode == TextureUsageMode.NormalmapDXT5nm;
		}

		bool IsCubemap()
		{
			var t = target as Texture;
			return t != null && t.dimension == TextureDimension.Cube;
		}

		bool IsVolume()
		{
			var t = target as Texture;
			return t != null && t.dimension == TextureDimension.Tex3D;
		}

		private bool m_ShowAlpha;

		public override void OnPreviewSettings()
		{
			if (IsCubemap())
			{
				//TODO perhaps support custom cubemap settings. Not currently!
				defaultEditor.OnPreviewSettings();
				return;
			}

			// TextureInspector code is reused for RenderTexture and Cubemap inspectors.
			// Make sure we can handle the situation where target is just a Texture and
			// not a Texture2D. It's also used for large popups for mini texture fields,
			// and while it's being shown the actual texture object might disappear --
			// make sure to handle null targets.
			Texture tex = target as Texture;

			if (tex == null)
				return;

			bool showMode = true;
			bool alphaOnly = false;
			bool hasAlpha = true;
			int mipCount = 1;

			if (target is Texture2D)
			{
				alphaOnly = true;
				hasAlpha = false;
			}

			bool hasR = false, hasG = false, hasB = false;

			// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
			foreach (Texture t in targets)
			{
				if (t == null) // texture might have disappeared while we're showing this in a preview popup
					continue;

				TextureFormat format = 0;
				bool checkFormat = false;
				if (t is Texture2D)
				{
					format = (t as Texture2D).format;
					checkFormat = true;
				}

				if (checkFormat)
				{
					if (!IsAlphaOnlyTextureFormat(format))
						alphaOnly = false;
					if (HasAlphaTextureFormat(format))
					{
						TextureUsageMode mode = GetUsageMode(t);
						if (mode == TextureUsageMode.Default) // all other texture usage modes don't displayable alpha
							hasAlpha = true;
					}
					bool _hasR, _hasG, _hasB;
					CheckRGBFormats(format, out _hasR, out _hasG, out _hasB);
					hasR = hasR || _hasR;
					hasG = hasG || _hasG;
					hasB = hasB || _hasB;
				}

				mipCount = Mathf.Max(mipCount, GetMipmapCount(t));

				if (!checkFormat)
				{
					if (t is RenderTexture)
					{
						RenderTextureFormat renderTextureFormat = (t as RenderTexture).format;
						bool _hasR, _hasG, _hasB;
						CheckRGBFormats(renderTextureFormat, out _hasR, out _hasG, out _hasB);
						hasR = hasR || _hasR;
						hasG = hasG || _hasG;
						hasB = hasB || _hasB;
					}
					else
					{
						//If we cannot validate whether the texture has RGB, lets assume it does by default.
						hasR = true;
						hasG = true;
						hasB = true;
					}
				}
			}

//			if (GUILayout.Button("PreviewColor2D.shader"))
//			{
//				
//				Shader previewShader = (Shader)EditorGUIUtility.LoadRequired("Previews/PreviewColor2D.shader");
//				Selection.activeObject = previewShader;
//			}

			if (GUILayout.Button(s_Styles.scaleIcon, s_Styles.previewButton))
			{
				//Switch between the default % zoom, and 100% zoom
				float p100 = 1 / zoomLevel;
				if (Math.Abs(zoomMultiplier - p100) > 0.001f)
				{
					int texWidth = Mathf.Max(tex.width, 1);
					int texHeight = Mathf.Max(tex.height, 1);
					Vector2 posNormalized = m_Pos / new Vector2(texWidth * zoomLevel * zoomMultiplier, texHeight * zoomLevel * zoomMultiplier);
					//Zooms to 100
					zoomMultiplier = p100;

					//Focuses Center
					Vector2 newPos = new Vector2(posNormalized.x * (texWidth * zoomLevel * zoomMultiplier), posNormalized.y * (texHeight * zoomLevel * zoomMultiplier));
					m_Pos = newPos;
				}
				else
				{
					//Zooms to default
					zoomMultiplier = 1;
					m_Pos = Vector2.zero;
				}

				Repaint();
			}

			if (!alphaOnly)
				DrawRGBToggles(hasR, hasG, hasB);

			if (alphaOnly)
			{
				m_ShowAlpha = true;
				showMode = false;
			}
			else if (!hasAlpha)
			{
				m_ShowAlpha = false;
				showMode = false;
			}

			if (showMode && !IsNormalMap(tex))
				m_ShowAlpha = GUILayout.Toggle(m_ShowAlpha, m_ShowAlpha ? s_Styles.alphaIcon : s_Styles.RGBIcon, s_Styles.previewButton);

			if (mipCount > 1)
			{
				GUILayout.Box(s_Styles.smallZoom, s_Styles.previewLabel);
				GUI.changed = false;
				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
				{
					m_MipLevel = Mathf.Round(GUILayout.HorizontalSlider(m_MipLevel, mipCount - 1, 0, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.MaxWidth(64)));
					if (changeCheckScope.changed)
					{
						rGBAMaterial.SetFloat("_Mip", m_MipLevel);
						rGBATransparentMaterial.SetFloat("_Mip", m_MipLevel);
						Repaint();
					}
				}

				GUILayout.Box(s_Styles.largeZoom, s_Styles.previewLabel);
			}
		}

		public static void CheckRGBFormats(TextureFormat textureFormat, out bool hasR, out bool hasG, out bool hasB)
		{
			hasR = true;
			hasG = true;
			hasB = true;
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (textureFormat)
			{
				case TextureFormat.Alpha8:
					hasR = false;
					hasG = false;
					hasB = false;
					break;
				case TextureFormat.RGHalf:
				case TextureFormat.RGFloat:
				case TextureFormat.BC5:
				case TextureFormat.EAC_RG:
				case TextureFormat.EAC_RG_SIGNED:
				case TextureFormat.RG16:
					hasB = false;
					break;
				case TextureFormat.R16:
				case TextureFormat.RHalf:
				case TextureFormat.RFloat:
				case TextureFormat.BC4:
				case TextureFormat.EAC_R:
				case TextureFormat.EAC_R_SIGNED:
				case TextureFormat.R8:
					hasB = false;
					hasG = false;
					break;
			}
		}

		private static void CheckRGBFormats(RenderTextureFormat renderTextureFormat, out bool hasR, out bool hasG, out bool hasB)
		{
			hasR = true;
			hasG = true;
			hasB = true;
			// ReSharper disable once SwitchStatementMissingSomeCases
			switch (renderTextureFormat)
			{
				case RenderTextureFormat.Depth:
					hasR = false;
					hasG = false;
					hasB = false;
					break;
				case RenderTextureFormat.RG16:
				case RenderTextureFormat.RG32:
				case RenderTextureFormat.RGInt:
				case RenderTextureFormat.RGHalf:
				case RenderTextureFormat.RGFloat:
					hasB = false;
					break;
				case RenderTextureFormat.RInt:
				case RenderTextureFormat.R8:
				case RenderTextureFormat.RHalf:
				case RenderTextureFormat.RFloat:
					hasG = false;
					hasB = false;
					break;
			}
		}

		private static Type m_TextureUtilType;

		private static Type TextureUtilType
		{
			get { return m_TextureUtilType ?? (m_TextureUtilType = Type.GetType("UnityEditor.TextureUtil, UnityEditor")); }
		}

		private static MethodInfo m_IsAlphaOnlyTextureFormat;

		private static bool IsAlphaOnlyTextureFormat(TextureFormat format)
		{
			if (m_IsAlphaOnlyTextureFormat == null)
				m_IsAlphaOnlyTextureFormat = TextureUtilType.GetMethod("IsAlphaOnlyTextureFormat", BindingFlags.Public | BindingFlags.Static);
			return (bool) m_IsAlphaOnlyTextureFormat.Invoke(null, new object[] {format});
		}

		private static MethodInfo m_HasAlphaTextureFormat;

		private static bool HasAlphaTextureFormat(TextureFormat format)
		{
			if (m_HasAlphaTextureFormat == null)
				m_HasAlphaTextureFormat = TextureUtilType.GetMethod("HasAlphaTextureFormat", BindingFlags.Public | BindingFlags.Static);
			return (bool) m_HasAlphaTextureFormat.Invoke(null, new object[] {format});
		}

		private static MethodInfo m_GetUsageMode;

		private static TextureUsageMode GetUsageMode(Texture texture)
		{
			if (m_GetUsageMode == null)
				m_GetUsageMode = TextureUtilType.GetMethod("GetUsageMode", BindingFlags.Public | BindingFlags.Static);
			return (TextureUsageMode) m_GetUsageMode.Invoke(null, new object[] {texture});
		}

		private static MethodInfo m_GetMipmapCount;

		private static int GetMipmapCount(Texture texture)
		{
			if (m_GetMipmapCount == null)
				m_GetMipmapCount = TextureUtilType.GetMethod("GetMipmapCount", BindingFlags.Public | BindingFlags.Static);
			return (int) m_GetMipmapCount.Invoke(null, new object[] {texture});
		}

		private static MethodInfo m_SetFilterModeNoDirty;

		private static void SetFilterModeNoDirty(Texture texture, FilterMode mode)
		{
			if (m_SetFilterModeNoDirty == null)
				m_SetFilterModeNoDirty = TextureUtilType.GetMethod("SetFilterModeNoDirty", BindingFlags.Public | BindingFlags.Static);
			m_SetFilterModeNoDirty.Invoke(null, new object[] {texture, mode});
		}

		private static Type m_PreviewGUIType;

		private static Type PreviewGUIType
		{
			get { return m_PreviewGUIType ?? (m_PreviewGUIType = Type.GetType("PreviewGUI, UnityEditor")); }
		}

		private static MethodInfo m_PreviewGUIBeginScrollView;

		private static void PreviewGUIBeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
		{
			if (m_PreviewGUIBeginScrollView == null)
				m_PreviewGUIBeginScrollView = PreviewGUIType.GetMethod("BeginScrollView", BindingFlags.NonPublic | BindingFlags.Static);
			object[] results = {position, scrollPosition, viewRect, horizontalScrollbar, verticalScrollbar};
			m_PreviewGUIBeginScrollView.Invoke(null, results);
		}

		private static MethodInfo m_PreviewGUIEndScrollView;

		private static Vector2 PreviewGUIEndScrollView()
		{
			if (m_PreviewGUIEndScrollView == null)
				m_PreviewGUIEndScrollView = PreviewGUIType.GetMethod("EndScrollView", BindingFlags.Public | BindingFlags.Static);
			return (Vector2) m_PreviewGUIEndScrollView.Invoke(null, null);
		}

		private static MethodInfo m_ApplyWireMaterial;

		private static void ApplyWireMaterial()
		{
			if (m_ApplyWireMaterial == null)
				m_ApplyWireMaterial = typeof(HandleUtility).GetMethod("ApplyWireMaterial", BindingFlags.NonPublic | BindingFlags.Static);
			m_ApplyWireMaterial.Invoke(null, null);
		}

		private static MethodInfo m_TextureImporterGetWidthAndHeight;

		private static void TextureImporterGetWidthAndHeight(TextureImporter textureImporter, ref int origWidth, ref int origHeight)
		{
			if (m_TextureImporterGetWidthAndHeight == null)
				m_TextureImporterGetWidthAndHeight = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
			object[] results = {origWidth, origHeight};
			m_TextureImporterGetWidthAndHeight.Invoke(textureImporter, results);
			origWidth = (int) results[0];
			origHeight = (int) results[1];
		}

		private static MethodInfo m_GUICalculateScaledTextureRects;

		private static void GUICalculateScaledTextureRects(Rect position, ScaleMode scaleMode, float imageAspect, ref Rect outScreenRect, ref Rect outSourceRect)
		{
			if (m_GUICalculateScaledTextureRects == null)
				m_GUICalculateScaledTextureRects = typeof(GUI).GetMethod("CalculateScaledTextureRects", BindingFlags.NonPublic | BindingFlags.Instance);
			object[] results = {position, scaleMode, imageAspect, outScreenRect, outSourceRect};
			m_GUICalculateScaledTextureRects.Invoke(null, results);
			outScreenRect = (Rect) results[3];
			outSourceRect = (Rect) results[4];
		}

		/*private static MethodInfo m_DrawPreviewTextureInternal;
		private static void DrawPreviewTexture (Rect position, Texture image, Material mat, ScaleMode scaleMode, float imageAspect, float mipLevel)
		{
			if(m_DrawPreviewTextureInternal == null)
				m_DrawPreviewTextureInternal = typeof(EditorGUI).GetMethod("DrawPreviewTextureInternal", BindingFlags.NonPublic | BindingFlags.Static, null,
					new[]{typeof(Rect), typeof(Texture), typeof(Material), typeof(ScaleMode), typeof(float), typeof(float)}, null);
			m_DrawPreviewTextureInternal.Invoke(null, new object[] {position, image, mat, scaleMode, imageAspect, mipLevel});
		}*/
	}
}