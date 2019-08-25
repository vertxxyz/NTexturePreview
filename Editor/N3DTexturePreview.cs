using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

#if !UNITY_2018_1_OR_NEWER
using System.Linq;
#endif

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
		
		/// <summary>
		/// An interface for overriding the Material used by this previewer
		/// </summary>
		public interface I3DMaterialOverride
		{
			/// <summary>
			/// Return a custom Material for N3DTexturePreview
			/// </summary>
			/// <returns>The Material used by the N3DTexturePreview if not null</returns>
			Material GetMaterial(Texture3D texture3D);

			bool ImplementAxisSliders();
		}

		void OnEnable()
		{
			//When this inspector is created, also create the built-in inspector
			defaultEditor = CreateEditor(targets, Type.GetType("UnityEditor.Texture3DInspector, UnityEditor"));

			//Find all types of I3DMaterialOverride, and query whether there's a valid material for the current target.

			#if UNITY_2018_1_OR_NEWER
			IEnumerable<Type> i3DMaterialOverrideTypes = (IEnumerable<Type>) Type.GetType("UnityEditor.EditorAssemblies, UnityEditor").GetMethod(
				"GetAllTypesWithInterface", BindingFlags.NonPublic | BindingFlags.Static, null, new[] {typeof(Type)}, null
			).Invoke(null, new object[] {typeof(I3DMaterialOverride)});
			#else
			IEnumerable<Type> i3DMaterialOverrideTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes()).Where(p => p != typeof(I3DMaterialOverride) && typeof(I3DMaterialOverride).IsAssignableFrom(p));
			#endif

			foreach (Type i3DMaterialOverrideType in i3DMaterialOverrideTypes)
			{
				I3DMaterialOverride i3DMaterialOverride = (I3DMaterialOverride) Activator.CreateInstance(i3DMaterialOverrideType);
				m_Material = i3DMaterialOverride.GetMaterial((Texture3D) target);
				if (m_Material != null)
				{
					materialOverride = i3DMaterialOverride;
					break;
				}
			}

			rCallback = r => { material.SetFloat(R, r ? 1 : 0); };
			gCallback = g => { material.SetFloat(G, g ? 1 : 0); };
			bCallback = b => { material.SetFloat(B, b ? 1 : 0); };
			x = 1;
			y = 1;
			z = 1;
			SetXYZFloats();
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

			if (m_PreviewUtility != null)
			{
				m_PreviewUtility.Cleanup();
				m_PreviewUtility = null;
			}
		}

		public override void OnInspectorGUI() => defaultEditor.OnInspectorGUI();

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
			bool hasR = false, hasG = false, hasB = false;
			// ReSharper disable once PossibleInvalidCastExceptionInForeachLoop
			foreach (Texture3D texture3D in targets)
			{
				if (texture3D == null) // texture might have disappeared while we're showing this in a preview popup
					continue;
				NTexturePreview.CheckRGBFormats(texture3D.format, out bool _hasR, out bool _hasG, out bool _hasB);
				hasR = hasR || _hasR;
				hasB = hasB || _hasB;
				hasG = hasG || _hasG;
			}

			if (ImplementAxisSliders() && (materialOverride == null || materialOverride.ImplementAxisSliders()))
			{
				Texture3D defaultTex3D = target as Texture3D;
				if (defaultTex3D != null)
				{
					using (EditorGUI.ChangeCheckScope changeCheckScope = new EditorGUI.ChangeCheckScope())
					{
						Vector3 size = new Vector3(defaultTex3D.width, defaultTex3D.height, defaultTex3D.depth);
						Vector3 sizeCurrent = new Vector3(x * (size.x - 1) + 1, y * (size.y - 1) + 1, z * (size.z - 1) + 1);
						axis = (Axis) EditorGUILayout.EnumPopup(axis, s_Styles.previewDropDown, GUILayout.Width(25));
						switch (axis)
						{
							case Axis.X:
								x = Mathf.RoundToInt(GUILayout.HorizontalSlider((int) sizeCurrent.x, 1, (int) size.x, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.Width(200)) - 1) / (size.x - 1);
								EditorGUILayout.LabelField(sizeCurrent.x.ToString(), s_Styles.previewLabel, GUILayout.Width(25));
								break;
							case Axis.Y:
								y = Mathf.RoundToInt(GUILayout.HorizontalSlider((int) sizeCurrent.y, 1, (int) size.y, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.Width(200)) - 1) / (size.y - 1);
								EditorGUILayout.LabelField(sizeCurrent.y.ToString(), s_Styles.previewLabel, GUILayout.Width(25));
								break;
							case Axis.Z:
								z = Mathf.RoundToInt(GUILayout.HorizontalSlider((int) sizeCurrent.z, 1, (int) size.z, s_Styles.previewSlider, s_Styles.previewSliderThumb, GUILayout.Width(200)) - 1) / (size.z - 1);
								EditorGUILayout.LabelField(sizeCurrent.z.ToString(), s_Styles.previewLabel, GUILayout.Width(25));
								break;
						}

						if (changeCheckScope.changed)
							SetXYZFloats();
					}
				}
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
			material.SetFloat(X, x);
			material.SetFloat(Y, y);
			material.SetFloat(Z, z);
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
			material.mainTexture = target as Texture;

			m_PreviewUtility.BeginPreview(r, background);
			bool oldFog = RenderSettings.fog;
			Unsupported.SetRenderSettingsUseFogNoDirty(false);

			var cameraTransform = m_PreviewUtility.camera.transform;
			cameraTransform.position = -Vector3.forward * zoom;

			cameraTransform.rotation = Quaternion.identity;
			Quaternion rot = Quaternion.Euler(m_PreviewDir.y, 0, 0) * Quaternion.Euler(0, m_PreviewDir.x, 0);
			m_PreviewUtility.DrawMesh(Mesh, Vector3.zero, rot, material, 0);
			m_PreviewUtility.Render();

			Unsupported.SetRenderSettingsUseFogNoDirty(oldFog);
			m_PreviewUtility.EndAndDrawPreview(r);
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

		private Material material
		{
			get
			{
				if (m_Material == null)
					m_Material = new Material(Resources.Load<Shader>("RGB3DShader"));
				return m_Material;
			}
		}

		private Material m_Material;

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

		public override bool HasPreviewGUI()
		{
			return defaultEditor.HasPreviewGUI();
		}
	}
}