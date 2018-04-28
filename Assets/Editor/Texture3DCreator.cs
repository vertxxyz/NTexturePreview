using UnityEditor;
using UnityEngine;

public class Texture3DCreator : EditorWindow
{
	[MenuItem("Window/Texture3D Creator")]
	public static void Open()
	{
		Texture3DCreator window = GetWindow<Texture3DCreator>();
		window.titleContent = new GUIContent("T3D Creator");
		window.Show();
	}

	private int size = 64;

	private void OnGUI()
	{
		size = EditorGUILayout.IntField("Size", size);
		if (GUILayout.Button("Create Texture3D"))
		{
			Texture3D texture3D = new Texture3D(size, size, size, TextureFormat.RGBAFloat, false);
			Color[] colors = new Color[size * size * size];
			float sizeArray = size - 1;
			int idx = 0;
			for (int z = 0; z < size; z++)
			{
				for (int y = 0; y < size; y++)
				{
					for (int x = 0; x < size; x++, idx++)
					{
						colors[idx] = new Color(x/sizeArray, y/sizeArray, z/sizeArray);
					}
				}
			}
			texture3D.SetPixels(colors);
			texture3D.Apply();
			AssetDatabase.CreateAsset(texture3D, AssetDatabase.GenerateUniqueAssetPath("Assets/Texture3D.asset"));
		}
	}
}