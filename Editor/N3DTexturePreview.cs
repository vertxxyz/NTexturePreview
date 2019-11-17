using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx
{
	[CustomEditor(typeof(Texture3D))]
	[CanEditMultipleObjects]
	public class N3DTexturePreview : NTexturePreviewBase
	{
		private static readonly int R = Shader.PropertyToID("_R");
		private static readonly int G = Shader.PropertyToID("_G");
		private static readonly int B = Shader.PropertyToID("_B");
		private static readonly int X = Shader.PropertyToID("_X");
		private static readonly int Y = Shader.PropertyToID("_Y");
		private static readonly int Z = Shader.PropertyToID("_Z");
		
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

		/// <summary>
		/// An interface for overriding the Material used by this previewer
		/// </summary>
		public interface I3DMaterialOverride
		{
			/// <summary>
			/// Return a custom Material for N3DTexturePreview
			/// </summary>
			/// <returns>The Material used by the N3DTexturePreview if not null</returns>
			Material GetMaterial(Texture texture3D);

			bool ImplementAxisSliders();
		}

		protected override void OnEnable()
		{
			//When this inspector is created, also create the built-in inspector
			defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.Texture3DInspector, UnityEditor"));

			//Find all types of I3DMaterialOverride, and query whether there's a valid material for the current target.
			IEnumerable<Type> i3DMaterialOverrideTypes = (IEnumerable<Type>) Type.GetType("UnityEditor.EditorAssemblies, UnityEditor").GetMethod(
				"GetAllTypesWithInterface", BindingFlags.NonPublic | BindingFlags.Static, null, new[] {typeof(Type)}, null
			).Invoke(null, new object[] {typeof(I3DMaterialOverride)});

			//Safe cast to allow Render Texture 3D to fall-back to this class.
			Texture texture3D = target as Texture;
			if (texture3D != null)
			{
				foreach (Type i3DMaterialOverrideType in i3DMaterialOverrideTypes)
				{
					I3DMaterialOverride i3DMaterialOverride = (I3DMaterialOverride) Activator.CreateInstance(i3DMaterialOverrideType);
					override3DMaterial = i3DMaterialOverride.GetMaterial(texture3D);
					if (override3DMaterial == null)
						continue;
					materialOverride = i3DMaterialOverride;
					break;
				}
			}

			rCallback = r => {
				Material m = override3DMaterial != null ? override3DMaterial : material3D;
				m.SetFloat(R, r ? 1 : 0);
			};
			gCallback = g =>
			{
				Material m = override3DMaterial != null ? override3DMaterial : material3D;
				m.SetFloat(G, g ? 1 : 0);
			};
			bCallback = b =>
			{
				Material m = override3DMaterial != null ? override3DMaterial : material3D;
				m.SetFloat(B, b ? 1 : 0);
			};
			x = 1;
			y = 1;
			z = 1;
			SetXYZFloats();
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

			if (m_PreviewUtility != null)
			{
				m_PreviewUtility.Cleanup();
				m_PreviewUtility = null;
			}
			
			if(override3DMaterial != null)
				DestroyImmediate(override3DMaterial);
		}

		public override void OnInspectorGUI() => defaultEditor.OnInspectorGUI();

		public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height) => defaultEditor.RenderStaticPreview(assetPath, subAssets, width, height);

		private float zoom = 3f;

		protected float x = 1, y = 1, z = 1;

		enum Axis
		{
			X,
			Y,
			Z
		}

		private Axis axis = Axis.X;

		public override void OnPreviewSettings()
		{
			defaultEditor.OnPreviewSettings();
			PreviewSettings();
		}

		public void PreviewSettings()
		{
			bool hasR = false, hasG = false, hasB = false;
			// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
			foreach (Object texture in targets)
			{
				bool _hasR, _hasG, _hasB;
				switch (texture)
				{
					case Texture3D texture3D:
						NTexturePreview.CheckRGBFormats(texture3D.format, out _hasR, out _hasG, out _hasB);
						break;
					case RenderTexture renderTexture:
						NTexturePreview.CheckRGBFormats(renderTexture.format, out _hasR, out _hasG, out _hasB);
						break;
					default:
						continue;
				}


				hasR = hasR || _hasR;
				hasB = hasB || _hasB;
				hasG = hasG || _hasG;
			}

			if (ImplementAxisSliders() && (materialOverride == null || materialOverride.ImplementAxisSliders()))
			{
				int width, height, depth;

				switch (target)
				{
					case Texture3D texture3D:
						width = texture3D.width;
						height = texture3D.height;
						depth = texture3D.depth;
						break;
					case RenderTexture renderTexture:
						width = renderTexture.width;
						height = renderTexture.height;
						depth = renderTexture.volumeDepth;
						break;
					default:
						return;
				}

				using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
				{
					Vector3 size = new Vector3(width, height, depth);
					Vector3 sizeCurrent = new Vector3(x * (size.x - 1) + 1, y * (size.y - 1) + 1, z * (size.z - 1) + 1);
					#if UNITY_2019_3_OR_NEWER
					axis = (Axis) EditorGUILayout.EnumPopup(axis, s_Styles.previewDropDown, GUILayout.Width(30));
					#else
					axis = (Axis) EditorGUILayout.EnumPopup(axis, s_Styles.previewDropDown, GUILayout.Width(25));
					#endif
					switch (axis)
					{
						case Axis.X:
							x = Mathf.RoundToInt(GUILayout.HorizontalSlider((int) sizeCurrent.x, 1, (int) size.x, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.Width(200)) - 1) / Mathf.Max(1, size.x - 1);
							EditorGUILayout.LabelField(sizeCurrent.x.ToString(), s_Styles.previewLabel, GUILayout.Width(35));
							break;
						case Axis.Y:
							y = Mathf.RoundToInt(GUILayout.HorizontalSlider((int) sizeCurrent.y, 1, (int) size.y, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.Width(200)) - 1) / Mathf.Max(1, size.y - 1);
							EditorGUILayout.LabelField(sizeCurrent.y.ToString(), s_Styles.previewLabel, GUILayout.Width(35));
							break;
						case Axis.Z:
							z = Mathf.RoundToInt(GUILayout.HorizontalSlider((int) sizeCurrent.z, 1, (int) size.z, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.Width(200)) - 1) / Mathf.Max(1, size.z - 1);
							EditorGUILayout.LabelField(sizeCurrent.z.ToString(), s_Styles.previewLabel, GUILayout.Width(35));
							break;
					}

					if (changeCheckScope.changed)
						SetXYZFloats();
				}
			}

			using (var cCS = new EditorGUI.ChangeCheckScope())
			{
				bool to = GUILayout.Toggle(ContinuousRepaint, s_Styles.playIcon, s_Styles.previewButtonScale);
				if (cCS.changed)
					ContinuousRepaint = to;
			}

			if (GUILayout.Button(s_Styles.scaleIcon, s_Styles.previewButtonScale))
			{
				zoom = 3;
				Repaint();
			}

			DrawRGBToggles(hasR, hasB, hasG);
		}

		public virtual bool ImplementAxisSliders() => true;

		void SetXYZFloats()
		{
			Material m = override3DMaterial != null ? override3DMaterial : material3D;
			m.SetFloat(X, x);
			m.SetFloat(Y, y);
			m.SetFloat(Z, z);
			Repaint();
		}

		public Vector2 m_PreviewDir = new Vector2(30, -25);
		private PreviewRenderUtility m_PreviewUtility;

		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if (!ShaderUtil.hardwareSupportsRectRenderTexture || !SystemInfo.supports3DTextures)
			{
				if (Event.current.type == EventType.Repaint)
					EditorGUI.DropShadowLabel(new Rect(r.x, r.y, r.width, 40), "3D texture preview not supported");
				return;
			}

			m_PreviewDir = Drag2D(m_PreviewDir, r);

			Event e = Event.current;

			if (e.type == EventType.ScrollWheel)
			{
				zoom = Mathf.Clamp(zoom + e.delta.y * 0.1f, 0.05f, 5f);
				m_PreviewUtility.camera.nearClipPlane = Mathf.Lerp(0.05f, 2, Mathf.InverseLerp(0.05f, 3f, zoom));
				e.Use();
				Repaint();
			}

			if (e.type != EventType.Repaint)
				return;

			InitPreview();

			Material m = override3DMaterial != null ? override3DMaterial : material3D;
			if (target is CustomRenderTexture customRenderTexture)
				m.mainTexture = customRenderTexture;
			else
				m.mainTexture = target as Texture3D;

			m_PreviewUtility.BeginPreview(r, background);
			bool oldFog = RenderSettings.fog;
			Unsupported.SetRenderSettingsUseFogNoDirty(false);

			var cameraTransform = m_PreviewUtility.camera.transform;
			cameraTransform.position = -Vector3.forward * zoom;

			cameraTransform.rotation = Quaternion.identity;
			Quaternion rot = Quaternion.Euler(m_PreviewDir.y, 0, 0) * Quaternion.Euler(0, m_PreviewDir.x, 0);
			m_PreviewUtility.DrawMesh(Mesh, Vector3.zero, rot, m, 0);
			m_PreviewUtility.Render();

			Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
			m_PreviewUtility.EndAndDrawPreview(r);
			if (ContinuousRepaint)
				Repaint();
		}

		void InitPreview()
		{
			if (m_PreviewUtility != null)
				return;
			m_PreviewUtility = new PreviewRenderUtility();
			m_PreviewUtility.camera.fieldOfView = 30.0f;
		}

		private I3DMaterialOverride materialOverride;
		private Material override3DMaterial;

		#region PreviewGUI

		//This region contains code taken from the internal PreviewGUI class.
		private static readonly int sliderHash = "Slider".GetHashCode();

		public static Vector2 Drag2D(Vector2 scrollPosition, Rect position)
		{
			int controlId = GUIUtility.GetControlID(sliderHash, FocusType.Passive);
			Event current = Event.current;
			switch (current.GetTypeForControl(controlId))
			{
				case EventType.MouseDown:
					if (position.Contains(current.mousePosition) && (double) position.width > 50.0)
					{
						GUIUtility.hotControl = controlId;
						current.Use();
						EditorGUIUtility.SetWantsMouseJumping(1);
					}

					break;
				case EventType.MouseUp:
					if (GUIUtility.hotControl == controlId)
						GUIUtility.hotControl = 0;
					EditorGUIUtility.SetWantsMouseJumping(0);
					break;
				case EventType.MouseDrag:
					if (GUIUtility.hotControl == controlId)
					{
						scrollPosition -= 140f * (!current.shift ? 1f : 3f) / Mathf.Min(position.width, position.height) * current.delta;
						scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
						current.Use();
						GUI.changed = true;
					}

					break;
			}

			return scrollPosition;
		}

		#endregion

		public override bool HasPreviewGUI() => defaultEditor.HasPreviewGUI();
	}
}