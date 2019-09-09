using System;
using System.Reflection;
using UnityEngine;

namespace Vertx
{
	public static class PreviewGUIUtility
	{
		private static Rect s_Position;
		private static Rect s_ViewRect;
		private static Vector2 s_ScrollPos;
		private static readonly int sliderHash = "Slider".GetHashCode();

		private static Type guiClipType;
		public static Type GUIClipType => guiClipType ?? (guiClipType = Type.GetType("UnityEngine.GUIClip,UnityEngine"));
		private static MethodInfo popMI;
		public static MethodInfo PopMI => popMI ?? GUIClipType.GetMethod("Pop", BindingFlags.Static | BindingFlags.NonPublic);
		public static void Pop() => PopMI.Invoke(null, null);

		private static MethodInfo pushMI;
		public static MethodInfo PushMI => pushMI ?? GUIClipType.GetMethod("Push", BindingFlags.Static | BindingFlags.NonPublic);

		private static readonly object[] pushArray = new object[4];

		public static void Push(Rect screenRect, Vector2 scrollOffset, Vector2 renderOffset, bool resetOffset)
		{
			pushArray[0] = screenRect;
			pushArray[1] = scrollOffset;
			pushArray[2] = renderOffset;
			pushArray[3] = resetOffset;
			PushMI.Invoke(null, pushArray);
		}

		internal static void BeginScrollView(Rect position, Vector2 scrollPosition, Rect viewRect, GUIStyle horizontalScrollbar, GUIStyle verticalScrollbar)
		{
			s_ScrollPos = scrollPosition;
			s_ViewRect = viewRect;
			s_Position = position;
			Push(position, new Vector2(Mathf.Round(-scrollPosition.x - viewRect.x - (viewRect.width - position.width) * .5f), Mathf.Round(-scrollPosition.y - viewRect.y - (viewRect.height - position.height) * .5f)), Vector2.zero, false);
		}

		public static Vector2 EndScrollView()
		{
			Pop();

			Rect clipRect = s_Position, position = s_Position, viewRect = s_ViewRect;

			Vector2 scrollPosition = s_ScrollPos;
			switch (Event.current.type)
			{
				case EventType.Layout:
					GUIUtility.GetControlID(sliderHash, FocusType.Passive);
					GUIUtility.GetControlID(sliderHash, FocusType.Passive);
					break;
				case EventType.Used:
					break;
				default:
					//These lines are incorrect in the Unity Source code.
					//I have submitted a report to get this fixed,
					//but as of now re-writing the code here correctly fixes the problem.
					bool needsVerticalScrollbar = (int) viewRect.height > (int) clipRect.height;
					bool needsHorizontalScrollbar = (int) viewRect.width > (int) clipRect.width;
					int id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);

					if (needsHorizontalScrollbar)
					{
						GUIStyle horizontalScrollbar = "PreHorizontalScrollbar";
						GUIStyle horizontalScrollbarThumb = "PreHorizontalScrollbarThumb";
						float offset = (viewRect.width - clipRect.width) * .5f;
						scrollPosition.x = GUI.Slider(new Rect(position.x, position.yMax - horizontalScrollbar.fixedHeight, clipRect.width - (needsVerticalScrollbar ? horizontalScrollbar.fixedHeight : 0), horizontalScrollbar.fixedHeight),
							scrollPosition.x, clipRect.width + offset, -offset, viewRect.width,
							horizontalScrollbar, horizontalScrollbarThumb, true, id);
					}
					else
					{
						// Get the same number of Control IDs so the ID generation for children don't depend on number of things above
						scrollPosition.x = 0;
					}

					id = GUIUtility.GetControlID(sliderHash, FocusType.Passive);

					if (needsVerticalScrollbar)
					{
						GUIStyle verticalScrollbar = "PreVerticalScrollbar";
						GUIStyle verticalScrollbarThumb = "PreVerticalScrollbarThumb";
						float offset = (viewRect.height - clipRect.height) * .5f;
						scrollPosition.y = GUI.Slider(new Rect(clipRect.xMax - verticalScrollbar.fixedWidth, clipRect.y, verticalScrollbar.fixedWidth, clipRect.height),
							scrollPosition.y, clipRect.height + offset, -offset, viewRect.height,
							verticalScrollbar, verticalScrollbarThumb, false, id);
					}
					else
					{
						scrollPosition.y = 0;
					}

					break;
			}

			return scrollPosition;
		}
	}
}