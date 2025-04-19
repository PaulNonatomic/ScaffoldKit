using System;
using System.Collections.Generic;
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
				TemplateVersion = "1.0",
				PlaceholderDefinitions = new (),
				SubDirectories = new (),
				Files = new ()
			};

			// Serialize to JSON settings to handle default values and formatting
			var jsonSettings = new JsonSerializerSettings
			{
				Formatting = Formatting.Indented,
				NullValueHandling = NullValueHandling.Ignore, 
				DefaultValueHandling = DefaultValueHandling.Ignore 
			};
			var jsonContent = JsonConvert.SerializeObject(sktData, jsonSettings);

			// Write to file
			File.WriteAllText(fullPath, jsonContent);
			AssetDatabase.Refresh();
			Debug.Log($"Created Scaffold file at: {fullPath}");

			// Select the created file in the Project window
			var createdAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(GetRelativePath(fullPath));
			if (createdAsset == null) return;
			
			Selection.activeObject = createdAsset;
			EditorGUIUtility.PingObject(createdAsset);
		}

		private static string GetSelectedFolderPath()
		{
			var path = "Assets";
			var activeObject = Selection.activeObject;

			if (activeObject == null)
			{
				return string.IsNullOrEmpty(path) 
					? "Assets" 
					: path;
			}

			var selectedPath = AssetDatabase.GetAssetPath(activeObject);
			if (string.IsNullOrEmpty(selectedPath))
			{
				return string.IsNullOrEmpty(path) 
					? "Assets" 
					: path;
			}

			if (AssetDatabase.IsValidFolder(selectedPath))
			{
				path = selectedPath;
			}
			else
			{
				path = Path.GetDirectoryName(selectedPath);
			}

			return string.IsNullOrEmpty(path) ? "Assets" : path;
		}


		private static string GetRelativePath(string fullPath)
		{
			var projectRoot = Path.GetFullPath(Application.dataPath + Path.DirectorySeparatorChar + "..");
			projectRoot = projectRoot.Replace("\\", "/");
			fullPath = fullPath.Replace("\\", "/");

			if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
			{
				// Make path relative and ensure it starts correctly for AssetDatabase
				var relativePath = fullPath[projectRoot.Length..];
				if (relativePath.StartsWith("/"))
				{
					relativePath = relativePath[1..];
				}

				return relativePath;
			}

			Debug.LogWarning($"Path '{fullPath}' seems outside the project root '{projectRoot}'. Cannot convert to relative path.");
			return fullPath;
		}
	}
}