using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Vertx
{
	[CustomEditor(typeof(Texture2D), true), CanEditMultipleObjects]
	public class NTexturePreview : NTexturePreviewBase
	{
		private static readonly int LightX = Shader.PropertyToID("_LightX");
		private static readonly int LightY = Shader.PropertyToID("_LightY");
		private static readonly int LightZ = Shader.PropertyToID("_LightZ");
		private static readonly int R = Shader.PropertyToID("_R");
		private static readonly int G = Shader.PropertyToID("_G");
		private static readonly int B = Shader.PropertyToID("_B");

		protected override void OnEnable()
		{
			animatedPos = new AnimVector3(Vector3.zero, () =>
			{
				scrollPosition = animatedPos.value;
				Repaint();
			})
			{
				speed = 1.5f
			};
			rCallback = r =>
			{
				rGBMaterial.SetFloat(R, r ? 1 : 0);
				rGBAMaterial.SetFloat(R, r ? 1 : 0);
				normalsMaterial.SetFloat(R, r ? 1 : 0);
			};
			gCallback = g =>
			{
				rGBMaterial.SetFloat(G, g ? 1 : 0);
				rGBAMaterial.SetFloat(G, g ? 1 : 0);
				normalsMaterial.SetFloat(G, g ? 1 : 0);
			};
			bCallback = b =>
			{
				rGBMaterial.SetFloat(B, b ? 1 : 0);
				rGBAMaterial.SetFloat(B, b ? 1 : 0);
				normalsMaterial.SetFloat(B, b ? 1 : 0);
			};
			base.OnEnable();
		}

		[SerializeField] float m_MipLevel;

		[SerializeField] protected Vector2 scrollPosition;

		static Material LoadColorMaterial(string fileNameWithExtension)
		{
			Shader shader = LoadResource<Shader>(fileNameWithExtension);
			if (shader == null)
				return new Material(Shader.Find("Unlit/Texture"));
			var material = new Material(shader);
			#if UNITY_2019_3_OR_NEWER
			if (QualitySettings.activeColorSpace == ColorSpace.Linear)
				material.EnableKeyword("LINEAR");
			else
				material.DisableKeyword("LINEAR");
			#endif
			return material;
		}

		private static Material _rGBMaterial;

		protected static Material rGBMaterial => _rGBMaterial == null ? _rGBMaterial = LoadColorMaterial("RGBShader.shader") : _rGBMaterial;

		private static Material _rGBAMaterial;

		protected static Material rGBAMaterial => _rGBAMaterial == null ? _rGBAMaterial = LoadColorMaterial("RGBAShader.shader") : _rGBAMaterial;

		private static Material _normalsMaterial;

		protected static Material normalsMaterial => _normalsMaterial == null ? _normalsMaterial = new Material(LoadResource<Shader>("NormalsShader.shader")) : _normalsMaterial;

		private static Texture2D _lightCursor;

		protected static Texture2D LightCursor => _lightCursor == null ? _lightCursor = LoadResource<Texture2D>("LightCursor.png") : _lightCursor;

		private static Texture2D _pickerCursor;

		protected static Texture2D PickerCursor => _pickerCursor == null ? _pickerCursor = LoadResource<Texture2D>("PickerCursor.png") : _pickerCursor;

		private static GUIStyle _pickerLabelStyle;

		protected static GUIStyle PickerLabelStyle =>
			_pickerLabelStyle ?? (_pickerLabelStyle = new GUIStyle("PreOverlayLabel")
			{
				alignment = TextAnchor.MiddleLeft,
				#if UNITY_2019_1_OR_NEWER
				font = EditorResources.Load<Font>("consola.ttf"),
				#endif
				fontSize = 14
			});

		#region Notification

		private static GUIStyle _notificationTextStyle;
		private static GUIStyle _notificationBackgroundStyle;
		protected static GUIStyle NotificationTextStyle => _notificationTextStyle ?? (_notificationTextStyle = new GUIStyle("NotificationText"));
		protected static GUIStyle NotificationBackgroundStyle => _notificationBackgroundStyle ?? (_notificationBackgroundStyle = new GUIStyle("NotificationBackground"));

		private readonly GUIContent copiedHexContent = new GUIContent("Copied Hex Value");
		private readonly GUIContent copiedCodeContent = new GUIContent("Copied Code-Ready Colour Value");
		private GUIContent activeNotificationContent;
		private double notificationEndTime;
		private double notificationFadeoutTime;
		private Vector2 notificationSize;
		private bool notificationRepaint;

		void ShowNotification(GUIContent content, double duration)
		{
			activeNotificationContent = content;
			notificationEndTime = EditorApplication.timeSinceStartup + duration;
			notificationFadeoutTime = notificationEndTime - Math.Min(1, duration);
			notificationSize = NotificationTextStyle.CalcSize(activeNotificationContent);
			notificationRepaint = true;
		}

		void DrawNotification(Rect position)
		{
			if (activeNotificationContent == null) return;
			if (EditorApplication.timeSinceStartup > notificationEndTime)
			{
				activeNotificationContent = null;
				notificationEndTime = 0;
				notificationRepaint = false;
				return;
			}

			float targetWidth = position.width - NotificationTextStyle.margin.horizontal;
			float targetHeight = position.height - NotificationTextStyle.margin.vertical - 20;
			Vector2 warningSize = notificationSize;

			GUIStyle scaledNotificationText = NotificationTextStyle;
			if (targetWidth < notificationSize.x)
			{
				float scale = targetWidth / notificationSize.x;

				scaledNotificationText = new GUIStyle(NotificationTextStyle);
				scaledNotificationText.fontSize = Mathf.FloorToInt(scaledNotificationText.font.fontSize * scale);

				warningSize = scaledNotificationText.CalcSize(activeNotificationContent);
			}

			warningSize.x += 1; //we'll give the text a little room to breathe to avoid word-wrapping issues with drop shadows

			if (warningSize.y > targetHeight)
				warningSize.y = targetHeight;

			Rect r = new Rect((position.width - warningSize.x) * .5f, 20 + (position.height - 20 - warningSize.y) * .7f, warningSize.x, warningSize.y);

			double time = EditorApplication.timeSinceStartup;
			if (time > notificationFadeoutTime)
				GUI.color = new Color(1, 1, 1, 1 - (float) (time - notificationFadeoutTime));
			GUI.Label(r, GUIContent.none, NotificationBackgroundStyle);
			EditorGUI.LabelField(r, activeNotificationContent, scaledNotificationText);
			GUI.color = Color.white;
		}

		#endregion

		protected override void OnDisable()
		{
			base.OnDisable();

			if (_editor3D != null)
			{
				DestroyImmediate(_editor3D);
				_editor3D = null;
			}

			if (_sampleTexture != null)
			{
				DestroyImmediate(_sampleTexture);
				_sampleTexture = null;
			}
		}

		public override void OnInspectorGUI() => defaultEditor.OnInspectorGUI();

		public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) =>
			defaultEditor.RenderStaticPreview(assetPath, subAssets, width, height);

		public override bool HasPreviewGUI() => target != null;

		private float zoomLevel;
		private float zoomMultiplier = 1;
		private const float maxZoomNormalized = 10;

		private bool hasDragged;

		//Using this for animated recentering
		private static AnimVector3 animatedPos;

		protected const string noSupportFor3DWithDepth = "3D textures with depth are not supported!";

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			//Crude test to see if shaders have properly loaded.
			if (_rGBMaterial == null && LoadResource<Shader>("RGBShader.shader") == null)
			{
				defaultEditor.OnPreviewGUI(r, background);
				return;
			}
			
			#if VERTX_DEBUG_MODE
			if (onlyShowDefaultEditor)
			{
				defaultEditor.OnPreviewGUI(r, background);
				return;
			}
			#endif
			
			Event e = Event.current;

			if (e.type == EventType.Repaint)
				background.Draw(r, false, false, false, false);

			// show texture
			Texture t = target as Texture;
			if (t == null) // texture might be gone by now, in case this code is used for floating texture preview
				return;

			PreviewTexture(r, t, background, e);
		}

		//Variables for normal map diffuse preview.
		private bool continuousRepaintOverride;
		private float lightZ = 0.1f;

		//3D RenderTexture Support
		private N3DTexturePreview _editor3D;
		private N3DTexturePreview editor3D
		{
			get
			{
				if (_editor3D == null)
				{
					Debug.Log("GET");
					return _editor3D = (N3DTexturePreview) CreateEditor(targets, typeof(N3DTexturePreview));
				}
				else
					return _editor3D;
			}
		}

		//Colour sampling
		private bool samplingColour;

		private void PreviewTexture(Rect r, Texture t, GUIStyle background, Event e)
		{
			bool isVolume = IsVolume(t);

			// Render target must be created before we can display it (case 491797)
			RenderTexture rt = t as RenderTexture;
			if (rt != null)
			{
				if (!SystemInfo.SupportsRenderTextureFormat(rt.format))
					return; // can't do this RT format
				if (!isVolume)
					rt.Create();
			}

			if (IsCubemap())
			{
				//TODO perhaps support custom cubemap settings. Not currently!
				defaultEditor.OnPreviewGUI(r, background);
				return;
			}

			if (isVolume)
			{
				if (rt != null && rt.depth != 0)
				{
					float h = r.height / 2f;
					h -= 15;
					float y = r.y + h;
					h = 30;
					EditorGUI.HelpBox(new Rect(r.x, y, r.width, h), noSupportFor3DWithDepth, MessageType.Error);
					return;
				}

				editor3D.OnPreviewGUI(r, background);
				return;
			}

			if (r.width == 32 && r.height == 32 || r.width == 1 && r.height == 1)
			{
				//There seems to be some unhelpful layout and repaint steps that provide rect scales that are unhelpful...
				return;
			}

			bool isNormalMap = IsNormalMap(t);

			// target can report zero sizes in some cases just after a parameter change;
			// guard against that.
			int texWidth = Mathf.Max(t.width, 1);
			int texHeight = Mathf.Max(t.height, 1);

			float mipLevel = GetMipLevelForRendering();

			float GetMipLevelForRendering()
			{
				if (target == null)
					return 0.0f;

				if (IsCubemap())
				{
					throw new NotImplementedException();
					//This should never be called yet by this class, and is handled by the default editor.
					//TODO support cubemap rendering here too
					//return m_CubemapPreview.GetMipLevelForRendering(target as Texture);
				}

				return Mathf.Min(m_MipLevel, GetMipmapCount(target as Texture) - 1);
			}

			zoomLevel = Mathf.Min(Mathf.Min(r.width / texWidth, r.height / texHeight), 1);
			Rect wantedRect = new Rect(r.x, r.y, texWidth * zoomLevel * zoomMultiplier, texHeight * zoomLevel * zoomMultiplier);

			if (e.type == EventType.MouseDown)
			{
				hasDragged = false;
			}

			if (isNormalMap)
			{
				if (e.button == 1)
				{
					switch (e.type)
					{
						case EventType.MouseDown:
							normalsMaterial.EnableKeyword("PREVIEW_DIFFUSE");
							normalsMaterial.DisableKeyword("PREVIEW_NORMAL");
							Cursor.SetCursor(LightCursor, new Vector2(16, 16), CursorMode.Auto);
							continuousRepaintOverride = true;
							break;
						case EventType.MouseUp:
							normalsMaterial.DisableKeyword("PREVIEW_DIFFUSE");
							normalsMaterial.EnableKeyword("PREVIEW_NORMAL");
							continuousRepaintOverride = false;
							break;
					}

					if (e.type != EventType.Repaint && e.type != EventType.Layout)
						e.Use();
				}

				if (continuousRepaintOverride)
				{
					Vector2 pos = Event.current.mousePosition - r.position;
					pos -= r.size / 2f;
					pos += scrollPosition;
					pos += new Vector2(texWidth * zoomLevel * zoomMultiplier, texHeight * zoomLevel * zoomMultiplier) / 2f;
					normalsMaterial.SetFloat(LightX, pos.x / wantedRect.size.x);
					normalsMaterial.SetFloat(LightY, 1 - pos.y / wantedRect.size.y);
					EditorGUIUtility.AddCursorRect(r, MouseCursor.CustomCursor);
				}
			}
			else
			{
				if (e.button == 1)
				{
					switch (e.type)
					{
						case EventType.MouseDown:
							samplingColour = true;
							Cursor.SetCursor(PickerCursor, new Vector2(15, 15), CursorMode.Auto);
							break;
						case EventType.MouseUp:
							samplingColour = false;
							break;
					}

					if (e.type != EventType.Repaint && e.type != EventType.Layout && !(e.control || e.command || e.alt))
						e.Use();
				}
			}

			if (e.type == EventType.MouseDrag)
			{
				hasDragged = true;
				scrollPosition -= e.delta;
				scrollPosition = ClampPos(scrollPosition, r, texWidth, texHeight, zoomLevel);
				e.Use();
				Repaint();
			}

			if (!hasDragged && e.type == EventType.MouseUp && e.button == 2)
			{
				//Middle mouse button click re-centering
				animatedPos.value = scrollPosition;
				Vector2 tgt = ClampPos(scrollPosition + ConvertPositionToLocalTextureRect(r, e.mousePosition), r, texWidth, texHeight, zoomLevel);
				animatedPos.target = tgt;
				e.Use();
			}

			if (e.type == EventType.ScrollWheel)
			{
				if (continuousRepaintOverride)
				{
					lightZ = Mathf.Clamp(lightZ + e.delta.y * 0.01f, 0.01f, 1f);
					normalsMaterial.SetFloat(LightZ, lightZ);
				}
				else
				{
					float zoomMultiplierLast = zoomMultiplier;
					zoomMultiplier = Mathf.Max(1, zoomMultiplier - e.delta.y * zoomMultiplier * 0.1f);
					//Maximum 2x texture zoom
					zoomMultiplier = Mathf.Clamp(zoomMultiplier, 1, maxZoomNormalized / zoomLevel);

					//if zoom has changed
					if (Math.Abs(zoomMultiplierLast - zoomMultiplier) > 0.001f)
					{
						//Focuses Center
						Vector2 posNormalized = new Vector2(scrollPosition.x / wantedRect.width, scrollPosition.y / wantedRect.height);
						Vector2 newPos = new Vector2(posNormalized.x * (texWidth * zoomLevel * zoomMultiplier), posNormalized.y * (texHeight * zoomLevel * zoomMultiplier));
						scrollPosition = newPos;
						scrollPosition = ClampPos(scrollPosition, r, texWidth, texHeight, zoomLevel);
					}

					//Reset scroll position if we zoomed out at maximum zoom
					// ReSharper disable twice CompareOfFloatsByEqualityOperator
					if (zoomMultiplier == 1 && e.delta.y != 0)
						scrollPosition = Vector2.zero;
				}

				e.Use();
				Repaint();
			}

			using (new GUI.ClipScope(r))
			{
				DrawTexturespaceBackground();
			}

			void DrawTexturespaceBackground()
			{
				if (e.type != EventType.Repaint)
					return;
				float size = Mathf.Max(r.width, r.height);
				Vector2 offset = new Vector2(wantedRect.xMin, wantedRect.xMin);

				float halfSize = size * .5f;
				float alpha = EditorGUIUtility.isProSkin ? 0.15f : 0.08f;
				float maxSize = Mathf.Max(texHeight, texWidth);
				//Alter the grid size based on the texture size
				float gridBase = Mathf.LerpUnclamped(8, 32, InverseLerpUnclamped(512, 2048, Mathf.Max(512, maxSize)));

				float InverseLerpUnclamped(float a, float b, float value)
				{
					if (a != b)
						return (value - a) / (b - a);
					return 0.0f;
				}

				float gridSize = Mathf.Abs(gridBase * zoomLevel * zoomMultiplier);
				//Offset the grid by the scroll position
				offset.x -= scrollPosition.x % (gridSize * 2f);
				offset.y -= scrollPosition.y % (gridSize * 2f);

				BeginLines(new Color(0f, 0f, 0f, alpha));
				//Start at -gridSize to account for the above offset
				for (float v = -gridSize; v <= size; v += gridSize)
					DrawLine(new Vector2(-halfSize + v, halfSize + v) + offset, new Vector2(halfSize + v, -halfSize + v) + offset);
				EndLines();

				void DrawLine(Vector3 p1, Vector3 p2)
				{
					GL.Vertex(p1);
					GL.Vertex(p2);
				}

				void BeginLines(Color color)
				{
					ApplyWireMaterial();
					GL.PushMatrix();
					GL.MultMatrix(Handles.matrix);
					GL.Begin(GL.LINES);
					GL.Color(color);
				}

				void EndLines()
				{
					GL.End();
					GL.PopMatrix();
				}
			}


			EditorGUI.DrawRect(new Rect(r.xMin, r.yMax, r.width, 1), new Color(0.54f, 0.54f, 0.54f));


			Texture2D t2d = t as Texture2D;
			{
				//SCROLL VIEW -----------------------------------------------------------------------------
				PreviewGUIUtility.BeginScrollView(r, scrollPosition, wantedRect, "PreHorizontalScrollbar", "PreHorizontalScrollbarThumb");

//				FilterMode oldFilter = t.filterMode;
//				SetFilterModeNoDirty(t, FilterMode.Point);

				if (m_ShowAlpha)
				{
					#if UNITY_2018_1_OR_NEWER
					EditorGUI.DrawTextureAlpha(wantedRect, t, ScaleMode.StretchToFill, 0, mipLevel);
					#else
					EditorGUI.DrawTextureAlpha(wantedRect, t, ScaleMode.StretchToFill, 0);
					#endif
				}
				else if (t2d != null && t2d.alphaIsTransparency)
				{
					float imageAspect = t.width / (float) t.height;
					DrawTransparencyCheckerTexture(wantedRect, ScaleMode.StretchToFill, imageAspect);
					#if UNITY_2018_1_OR_NEWER
					EditorGUI.DrawPreviewTexture(wantedRect, t, rGBAMaterial, ScaleMode.StretchToFill, imageAspect, mipLevel);
					#else
					EditorGUI.DrawPreviewTexture(wantedRect, t, rGBATransparentMaterial, ScaleMode.StretchToFill, imageAspect);
					#endif
				}
				else
				{
					Material matToUse = isNormalMap ? normalsMaterial : rGBMaterial;
					#if UNITY_2018_1_OR_NEWER
					EditorGUI.DrawPreviewTexture(wantedRect, t, matToUse, ScaleMode.StretchToFill, 0, mipLevel);
					#else
					EditorGUI.DrawPreviewTexture(wantedRect, t, matToUse, ScaleMode.StretchToFill, 0);
					#endif
				}

				// TODO: Less hacky way to prevent sprite rects to not appear in smaller previews like icons.
				if (!samplingColour && wantedRect.width > 32 && wantedRect.height > 32)
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

						bool selected = e.type == EventType.MouseDown && e.button == 0 && e.clickCount > 1;
						Vector2 mP = e.mousePosition;
						foreach (SpriteMetaData sprite in spritesheet)
						{
							Rect spriteRect = sprite.rect;
							Rect spriteScreenRect = new Rect
							{
								xMin = screenRect.xMin + screenRect.width * (spriteRect.xMin / t.width * definitionScale),
								xMax = screenRect.xMin + screenRect.width * (spriteRect.xMax / t.width * definitionScale),
								yMax = screenRect.yMin + screenRect.height * (1f - spriteRect.yMin / t.height * definitionScale),
								yMin = screenRect.yMin + screenRect.height * (1f - spriteRect.yMax / t.height * definitionScale)
							};

							DrawRect(spriteScreenRect);

							if (!selected || !spriteScreenRect.Contains(mP))
								continue;
							Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
							string nameQuery = sprite.name;
							foreach (Object asset in assets)
							{
								if (!asset.name.Equals(nameQuery))
									continue;
								EditorGUIUtility.PingObject(asset);
								break;
							}
						}

						GL.End();
						GL.PopMatrix();
					}
				}

				//SetFilterModeNoDirty(t, oldFilter);

				scrollPosition = PreviewGUIUtility.EndScrollView();
			} //-----------------------------------------------------------------------------------------

			if (samplingColour && (t2d != null || rt != null))
			{
				EditorGUIUtility.AddCursorRect(r, MouseCursor.CustomCursor);
				Vector2 mousePosition = Event.current.mousePosition;
				Color pixel = t2d != null
					? GetColorFromMousePosition(mousePosition, r, wantedRect, texWidth, texHeight, t2d)
					: GetColorFromMousePosition(mousePosition, r, wantedRect, texWidth, texHeight, rt);

				//Copy shortcuts
				if ((e.button == 0 || e.control || e.command || e.alt) && e.type == EventType.MouseDown)
				{
					if (e.clickCount == 1)
					{
						EditorGUIUtility.systemCopyBuffer = ColorUtility.ToHtmlStringRGBA(pixel);
						ShowNotification(copiedHexContent, 1);
					}
					else
					{
						EditorGUIUtility.systemCopyBuffer = $"new Color({pixel.r}f, {pixel.g}f, {pixel.b}f, {pixel.a}f);";
						ShowNotification(copiedCodeContent, 1);
					}

					if (e.type != EventType.Repaint && e.type != EventType.Layout)
						e.Use();
				}
				else if ((e.control || e.command) && e.keyCode == KeyCode.C)
				{
					if (!e.shift)
					{
						EditorGUIUtility.systemCopyBuffer = ColorUtility.ToHtmlStringRGBA(pixel);
						ShowNotification(copiedHexContent, 1);
					}
					else
					{
						EditorGUIUtility.systemCopyBuffer = $"new Color({pixel.r}f, {pixel.g}f, {pixel.b}f, {pixel.a}f);";
						ShowNotification(copiedCodeContent, 1);
					}

					if (e.type != EventType.Repaint && e.type != EventType.Layout)
						e.Use();
				}
				//-----------

				//Picker label
				string label = $"({pixel.r:F3}, {pixel.g:F3}, {pixel.b:F3}, {pixel.a:F3})";
				Vector2 labelSize = PickerLabelStyle.CalcSize(new GUIContent(label));
				Rect labelRect;
				if (mousePosition.x > r.width - labelSize.x)
				{
					labelRect = new Rect(mousePosition.x - labelSize.x - 5, mousePosition.y + 5, labelSize.x, 16);
					PickerLabelStyle.alignment = TextAnchor.MiddleRight;
				}
				else
				{
					labelRect = new Rect(mousePosition.x + 5, mousePosition.y + 5, labelSize.x, 16);
					PickerLabelStyle.alignment = TextAnchor.MiddleLeft;
				}

				EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.2f));
				EditorGUI.LabelField(labelRect, label, PickerLabelStyle);

				//Picker swatch
				pixel.a = 1;
				EditorGUI.DrawRect(new Rect(mousePosition.x - 57, mousePosition.y - 57, 52, 52), Color.black);
				EditorGUI.DrawRect(new Rect(mousePosition.x - 56, mousePosition.y - 56, 50, 50), pixel);
			}

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (mipLevel != 0)
				EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 20), "Mip " + mipLevel);

			DrawNotification(r);

			//This approach is much smoother than using RequiresConstantRepaint
			if (continuousRepaintOverride || ContinuousRepaint || notificationRepaint || samplingColour)
				Repaint();
		}

		#region Texture Sampling

		private Color GetColorFromMousePosition(Vector2 mousePos, Rect r, Rect wantedRect, int texWidth, int texHeight, Texture2D t2d)
		{
			GetPixelPositionUnderCursor(mousePos, r, wantedRect, texWidth, texHeight, out int x, out int y);
			Texture2D sampleTexture = t2d.isReadable ? t2d : GetSampleTextureFor(t2d);
			#if UNITY_2019_1_OR_NEWER
			Color color = sampleTexture.GetPixel(x, y, 0);
			#else
            Color color = sampleTexture.GetPixel(x, y);
			#endif
			return HandleDelinearization(t2d, color);
		}

		private static Color HandleDelinearization(Texture texture, Color color)
		{
			#if UNITY_2020_1_OR_NEWER
			color = Delinearize(color);
			#else
			bool linear = GraphicsFormatUtility.GetLinearFormat(texture.graphicsFormat) == texture.graphicsFormat;
			if (!linear)
				color = Delinearize(color);
			#endif
			return color;
		}
		
		private static Color Delinearize(Color color)
			=> new Color(Mathf.Pow(color.r, 0.454545f), Mathf.Pow(color.g, 0.454545f), Mathf.Pow(color.b, 0.454545f), color.a);

		private Color GetColorFromMousePosition(Vector2 mousePos, Rect r, Rect wantedRect, int texWidth, int texHeight, RenderTexture rT)
		{
			GetPixelPositionUnderCursor(mousePos, r, wantedRect, texWidth, texHeight, out int x, out int y);
			return ReadFromRenderTexture(rT, x, y);
		}

		public void GetPixelPositionUnderCursor(Vector2 mousePos, Rect r, Rect wantedRect, int texWidth, int texHeight, out int x, out int y)
		{
			Vector2 pos = mousePos - r.position;
			pos -= r.size / 2f;
			pos += scrollPosition;
			pos += new Vector2(texWidth * zoomLevel * zoomMultiplier, texHeight * zoomLevel * zoomMultiplier) / 2f;
			pos /= wantedRect.size;
			pos.y = 1 - pos.y;
			pos.x *= texWidth;
			pos.y *= texHeight;

			x = Mathf.Clamp(Mathf.RoundToInt(pos.x - 0.5f), 0, texWidth - 1);
			y = Mathf.Clamp(Mathf.RoundToInt(pos.y - 0.5f), 0, texHeight - 1);
		}

		private static Texture2D _sampleTexture;
		private static Texture _sampleTextureKey;

		public Texture2D GetSampleTextureFor(Texture2D source)
		{
			if (_sampleTextureKey == source && _sampleTexture != null)
				return _sampleTexture;
			if (_sampleTextureKey == null && _sampleTexture != null)
				DestroyImmediate(_sampleTexture);
			// Create a temporary RenderTexture of the same size as the texture
			RenderTexture tmp = RenderTexture.GetTemporary(
				source.width,
				source.height,
				0,
				RenderTextureFormat.ARGBFloat,
				RenderTextureReadWrite.Linear);

			// Blit the pixels on texture to the RenderTexture
			Graphics.Blit(source, tmp);
			// Backup the currently set RenderTexture
			RenderTexture previous = RenderTexture.active;
			// Set the current RenderTexture to the temporary one we created
			RenderTexture.active = tmp;
			// Create a new readable Texture2D to copy the pixels to it
			_sampleTexture = new Texture2D(source.width, source.height, TextureFormat.RGBAFloat, false, true);
			// Copy the pixels from the RenderTexture to the new Texture
			_sampleTexture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
			_sampleTexture.Apply();
			// Reset the active RenderTexture
			RenderTexture.active = previous;
			// Release the temporary RenderTexture
			RenderTexture.ReleaseTemporary(tmp);
			_sampleTextureKey = source;
			return _sampleTexture;
		}

		public Color ReadFromRenderTexture(RenderTexture source, int x, int y)
		{
			//With Render Textures we need to keep them updated constantly.

			// Backup the currently set RenderTexture
			RenderTexture previous = RenderTexture.active;
			// Set the current RenderTexture to the temporary one we created
			RenderTexture.active = source;
			// Create a new readable Texture2D to copy the pixels to it if we need to
			if (_sampleTexture == null)
				_sampleTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false, true);
			// Copy the pixels from the RenderTexture to the new Texture
			_sampleTexture.ReadPixels(new Rect(x, source.height - y - 1, 1, 1), 0, 0);
			_sampleTexture.Apply();
			// Reset the active RenderTexture
			RenderTexture.active = previous;
			_sampleTextureKey = source;
			#if UNITY_2019_1_OR_NEWER
			Color color = _sampleTexture.GetPixel(0, 0, 0);
			#else
			Color color = _sampleTexture.GetPixel(0, 0);
			#endif
			return HandleDelinearization(source, color);
		}

		#endregion

		private static Vector2 ConvertPositionToLocalTextureRect(Rect r, Vector2 position)
		{
			Vector2 rectCenter = new Vector2(r.width / 2f, r.height / 2f);
			Vector2 localPos = new Vector2(position.x - r.x, position.y - r.y);
			localPos -= rectCenter;
			return localPos;
		}

		private const float smallestOnScreenNormalisedRect = 0.5f;

		private Vector2 ClampPos(Vector2 posLocal, Rect r, float textureWidth, float textureHeight, float zoomLevel)
		{
			float w2 = textureWidth * zoomLevel * zoomMultiplier;
			float h2 = textureHeight * zoomLevel * zoomMultiplier;

			w2 /= 2;
			h2 /= 2;
			w2 += r.width / 2f - smallestOnScreenNormalisedRect * r.width;
			h2 += r.height / 2f - smallestOnScreenNormalisedRect * r.height;
			return new Vector2(Mathf.Clamp(posLocal.x, -w2, w2), Mathf.Clamp(posLocal.y, -h2, h2));
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

		protected bool IsVolume(Texture t) => t != null && (t.dimension == TextureDimension.Tex3D || t.dimension == TextureDimension.Tex2DArray);

		private bool m_ShowAlpha;

		public override void OnPreviewSettings()
		{
			if (IsCubemap())
			{
				//TODO perhaps support custom cubemap settings. Not currently!
				defaultEditor.OnPreviewSettings();
				return;
			}

			RenderTexture rT = target as RenderTexture;
			if (rT != null && IsVolume(rT))
			{
				editor3D.PreviewSettings();
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
				if (t is Texture2D texture2D)
				{
					format = texture2D.format;
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

					CheckRGBFormats(format, out bool _hasR, out bool _hasG, out bool _hasB);
					hasR = hasR || _hasR;
					hasG = hasG || _hasG;
					hasB = hasB || _hasB;
				}

				mipCount = Mathf.Max(mipCount, GetMipmapCount(t));

				if (!checkFormat)
				{
					if (rT != null)
					{
						RenderTextureFormat renderTextureFormat = rT.format;
						CheckRGBFormats(renderTextureFormat, out bool _hasR, out bool _hasG, out bool _hasB);
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

			//if (rT != null)
			if (tex.isReadable)
			{
				using (var cCS = new EditorGUI.ChangeCheckScope())
				{
					bool to = GUILayout.Toggle(ContinuousRepaint, s_Styles.playIcon, s_Styles.previewButtonScale);
					if (cCS.changed)
						ContinuousRepaint = to;
				}
			}

			if (GUILayout.Button(s_Styles.scaleIcon, s_Styles.previewButtonScale))
			{
				//Switch between the default % zoom, and 100% zoom
				float p100 = 1 / zoomLevel;
				if (Math.Abs(zoomMultiplier - p100) > 0.001f)
				{
					int texWidth = Mathf.Max(tex.width, 1);
					int texHeight = Mathf.Max(tex.height, 1);
					Vector2 posNormalized = new Vector2(scrollPosition.x / (texWidth * zoomLevel * zoomMultiplier), scrollPosition.y / (texHeight * zoomLevel * zoomMultiplier));
					//Zooms to 100
					zoomMultiplier = p100;

					//Focuses Center
					Vector2 newPos = new Vector2(posNormalized.x * (texWidth * zoomLevel * zoomMultiplier), posNormalized.y * (texHeight * zoomLevel * zoomMultiplier));
					scrollPosition = newPos;
				}
				else
				{
					//Zooms to default
					zoomMultiplier = 1;
					scrollPosition = Vector2.zero;
				}

				Repaint();
			}

			bool isNormalMap = IsNormalMap(tex);

			if (!alphaOnly && !isNormalMap)
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

			if (showMode && !isNormalMap)
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
						rGBMaterial.SetFloat(Mip, m_MipLevel);
						rGBAMaterial.SetFloat(Mip, m_MipLevel);
						Repaint();
					}
				}

				GUILayout.Box(s_Styles.largeZoom, s_Styles.previewLabel);
			}
			
			#if VERTX_DEBUG_MODE
			if(GUILayout.Button(onlyShowDefaultEditor ? "VERTX" : "DEFAULT", EditorStyles.toolbarButton))
				onlyShowDefaultEditor = !onlyShowDefaultEditor;
			#endif
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

		public static void CheckRGBFormats(RenderTextureFormat renderTextureFormat, out bool hasR, out bool hasG, out bool hasB)
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

		private static MethodInfo _DrawTransparencyCheckerTexture;

		private static void DrawTransparencyCheckerTexture(Rect wantedRect, ScaleMode scaleMode, float imageAspect)
		{
			if (_DrawTransparencyCheckerTexture == null)
				_DrawTransparencyCheckerTexture = typeof(EditorGUI).GetMethod("DrawTransparencyCheckerTexture", BindingFlags.NonPublic | BindingFlags.Static);
			_DrawTransparencyCheckerTexture.Invoke(null, new object[] {wantedRect, scaleMode, imageAspect});
		}

		private static Type m_TextureUtilType;

		private static Type TextureUtilType => m_TextureUtilType ?? (m_TextureUtilType = Type.GetType("UnityEditor.TextureUtil, UnityEditor"));

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

		/*private static Type m_PreviewGUIType;

		private static Type PreviewGUIType => m_PreviewGUIType ?? (m_PreviewGUIType = Type.GetType("PreviewGUI, UnityEditor"));

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
		}*/

		private static MethodInfo m_ApplyWireMaterial;

		private static void ApplyWireMaterial()
		{
			if (m_ApplyWireMaterial == null)
				m_ApplyWireMaterial = typeof(HandleUtility).GetMethod("ApplyWireMaterial", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { }, null);
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
		private static readonly int Mip = Shader.PropertyToID("_Mip");

		private static void GUICalculateScaledTextureRects(Rect position, ScaleMode scaleMode, float imageAspect, ref Rect outScreenRect, ref Rect outSourceRect)
		{
			if (m_GUICalculateScaledTextureRects == null)
			{
				m_GUICalculateScaledTextureRects = typeof(GUI).GetMethod("CalculateScaledTextureRects", BindingFlags.NonPublic | BindingFlags.Instance);
				if (m_GUICalculateScaledTextureRects == null)
					m_GUICalculateScaledTextureRects = typeof(GUI).GetMethod("CalculateScaledTextureRects", BindingFlags.NonPublic | BindingFlags.Static);
			}

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