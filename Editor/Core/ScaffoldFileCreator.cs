using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace ScaffoldKit.Editor.Core
{
	public class ScaffoldFileCreator
	{
		[MenuItem("Assets/Create/ScaffoldKit/New Scaffold Template", false, 80)]
		public static void CreateScaffoldFile()
		{
			// Get the path to save the file
			var folderPath = GetSelectedFolderPath();
			var fileName = "NewScaffold.skt";
			var fullPath = Path.Combine(folderPath, fileName);

			// Create a default Scaffold data structure
			var sktData = new ScaffoldData
			{
				TemplateName = "New Template",
				TemplateVersion = "1.0"
			};

			// Serialize to JSON
			var jsonContent = JsonConvert.SerializeObject(sktData, Formatting.Indented);

			// Write to file
			File.WriteAllText(fullPath, jsonContent);
			AssetDatabase.Refresh();
			Debug.Log($"Created Scaffold file at: {fullPath}");

			// Select the created file in the Project window
			var createdAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(GetRelativePath(fullPath));
			if (createdAsset != null)
			{
				Selection.activeObject = createdAsset;
			}
		}

		private static string GetSelectedFolderPath()
		{
			var path = "Assets";
			if (Selection.activeObject == null)
			{
				return path;
			}

			var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			if (AssetDatabase.IsValidFolder(selectedPath))
			{
				path = selectedPath;
			}
			else if (!string.IsNullOrEmpty(selectedPath))
			{
				path = Path.GetDirectoryName(selectedPath);
			}

			return path;
		}

		private static string GetRelativePath(string fullPath)
		{
			// Convert full path to project-relative path for AssetDatabase
			var projectPath = Application.dataPath[..(Application.dataPath.Length - 6)];
			return fullPath[projectPath.Length..];
		}
	}
}