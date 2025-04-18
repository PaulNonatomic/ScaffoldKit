using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace ScaffoldKit.Editor.Importers
{
	[ScriptedImporter(1, "skt")]
	public class ScaffoldImporter : ScriptedImporter
	{
		public override void OnImportAsset(AssetImportContext ctx)
		{
			var sktAsset = new TextAsset(File.ReadAllText(ctx.assetPath));
			var icon = Resources.Load<Texture2D>("Icons/sktIcon");

			if (icon != null)
			{
				ctx.AddObjectToAsset("main", sktAsset, icon);
			}
			else
			{
				ctx.AddObjectToAsset("main", sktAsset);
			}

			ctx.SetMainObject(sktAsset);
		}
	}

	[CustomEditor(typeof(TextAsset))]
	public class ScaffoldEditor : UnityEditor.Editor
	{
		private bool _isScaffoldFile;

		private void OnEnable()
		{
			var assetPath = AssetDatabase.GetAssetPath(target);
			_isScaffoldFile = assetPath.EndsWith(".skt");
		}

		public override void OnInspectorGUI()
		{
			if (!_isScaffoldFile)
			{
				base.OnInspectorGUI();
				return;
			}

			EditorGUILayout.LabelField("Scaffold Template", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			var textAsset = (TextAsset)target;
			var text = textAsset.text;

			// Show a preview of the JSON
			EditorGUILayout.LabelField("Content Preview:", EditorStyles.boldLabel);
			EditorGUILayout.TextArea(text.Length > 500 ? text.Substring(0, 500) + "..." : text,
				GUILayout.Height(200));

			if (GUILayout.Button("Edit Template"))
			{
				AssetDatabase.OpenAsset(target);
			}
		}
	}
}