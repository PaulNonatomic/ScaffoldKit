using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Exporters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ScaffoldKit.Editor.Exporters
{
	/// <summary>
	/// Provides editor functionality to export a folder structure to a Scaffold file,
	/// including reading content from source files using IFileContentExporter implementations.
	/// </summary>
	public static class ScaffoldExporter
	{
		private const string MENU_ITEM_PATH = "Assets/Create/ScaffoldKit/New Scaffold Template From Folder";
		private static readonly List<IFileContentExporter> _fileExporters;

		static ScaffoldExporter()
		{
			_fileExporters = new ()
			{
				new JsonExporter(),
				new PlainTextExporter(),
				new NullExporter()
			};
		}

		[MenuItem(MENU_ITEM_PATH, false, 81)]
		private static void GenerateScaffoldFromFolder()
		{
			if (Selection.assetGUIDs.Length != 1)
			{
				Debug.LogError("[ScaffoldExporter] Please select exactly one folder to export.");
				return;
			}

			var guid = Selection.assetGUIDs[0];
			var selectedDirectoryPathRelative = AssetDatabase.GUIDToAssetPath(guid);

			if (string.IsNullOrEmpty(selectedDirectoryPathRelative) || !AssetDatabase.IsValidFolder(selectedDirectoryPathRelative))
			{
				Debug.LogError("[ScaffoldExporter] Selection is not a valid folder.");
				return;
			}

			var projectRoot = Path.GetDirectoryName(Application.dataPath);
			var selectedDirectoryPathFull = Path.GetFullPath(Path.Combine(projectRoot, selectedDirectoryPathRelative)).Replace("\\", "/");
			var selectedFolderName = Path.GetFileName(selectedDirectoryPathFull);

			if (string.IsNullOrEmpty(selectedFolderName)) {
				Debug.LogError("[ScaffoldExporter] Could not determine folder name from path.");
				return;
			}

			try
			{
				var sktData = new ScaffoldData
				{
					TemplateName = $"{selectedFolderName} Export",
					TemplateVersion = "1.0.0",
					PlaceholderDefinitions = new (),
					SubDirectories = new (),
					Files = new ()
				};

				// Process files in the selected folder
				try
				{
					foreach (var filePath in Directory.GetFiles(selectedDirectoryPathFull))
					{
						var fileName = Path.GetFileName(filePath);
						if (fileName.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;

						var exporter = _fileExporters.FirstOrDefault(e => e.CanExport(filePath));
						var fileContent = exporter?.ExportContent(filePath);

						var fileData = new FileData
						{
							Name = Path.GetFileNameWithoutExtension(fileName),
							Extension = Path.GetExtension(fileName).TrimStart('.'),
							Content = fileContent 
						};
						sktData.Files.Add(fileData);
					}
				}
				catch (System.Exception ex) {
					Debug.LogWarning($"[ScaffoldExporter] Error reading files in root '{selectedDirectoryPathFull}'. Error: {ex.Message}");
				}

				// Process Subdirectories Directly Under Selected Folder
				try
				{
					foreach (var subDirPath in Directory.GetDirectories(selectedDirectoryPathFull))
					{
						var subDirectoryData = BuildDirectoryDataRecursive(subDirPath);
						if (subDirectoryData != null)
						{
							sktData.SubDirectories.Add(subDirectoryData);
						}
					}
				}
				catch (System.Exception ex) {
					Debug.LogWarning($"[ScaffoldExporter] Error reading subdirectories in root '{selectedDirectoryPathFull}'. Error: {ex.Message}");
				}

				// Output Path
				var parentPathRelative = Path.GetDirectoryName(selectedDirectoryPathRelative);
				var outputFileName = $"{selectedFolderName}.skt";
				var outputFilePathRelative = Path.Combine(parentPathRelative, outputFileName).Replace("\\", "/");
				var outputFilePathFull = Path.GetFullPath(Path.Combine(projectRoot, outputFilePathRelative)).Replace("\\", "/");

				// Serialize and Write File
				var jsonSettings = new JsonSerializerSettings
				{
					Formatting = Formatting.Indented,
					NullValueHandling = NullValueHandling.Ignore
				};
				var jsonContent = JsonConvert.SerializeObject(sktData, jsonSettings);

				File.WriteAllText(outputFilePathFull, jsonContent);
				Debug.Log($"[ScaffoldExporter] Generated Scaffold template from folder '{selectedFolderName}' at: {outputFilePathRelative}");

				// Refresh and Select
				AssetDatabase.Refresh();
				var createdAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(outputFilePathRelative);
				
				if (createdAsset != null) 
				{
					Selection.activeObject = createdAsset;
					EditorGUIUtility.PingObject(createdAsset);
				}
				else 
				{
					 Debug.LogWarning($"[ScaffoldExporter] Could not select the created asset at '{outputFilePathRelative}'. It might be outside the Assets folder or AssetDatabase needs more time.");
				}

			}
			catch (System.Exception e)
			{
				Debug.LogError($"[ScaffoldExporter] Failed Scaffold generation for folder '{selectedFolderName}'. Error: {e.Message}\n{e.StackTrace}");
				EditorUtility.DisplayDialog("Scaffold Export Error", $"Failed to export folder '{selectedFolderName}'.\nError: {e.Message}", "OK");
			}
		}

		[MenuItem(MENU_ITEM_PATH, true)]
		private static bool GenerateScaffoldFromFolderValidation()
		{
			// Enable the menu item only if exactly one folder is selected
			var guids = Selection.assetGUIDs;
			if (guids is not { Length: 1 }) return false;

			var path = AssetDatabase.GUIDToAssetPath(guids[0]);
			if (string.IsNullOrEmpty(path)) return false;

			return AssetDatabase.IsValidFolder(path);
		}

		/// <summary>
		/// Recursively builds DirectoryData, using exporters to get file content.
		/// Includes explicit list initialization checks as a safeguard.
		/// </summary>
		private static DirectoryData BuildDirectoryDataRecursive(string directoryPath)
		{
			var dirName = Path.GetFileName(directoryPath);
			var directoryData = new DirectoryData { Name = dirName, ContributesToNamespace = false };

			// Explicitly ensure lists are initialized (redundant if constructor does it, but safe)
			directoryData.Files ??= new List<FileData>();
			directoryData.SubDirectories ??= new List<DirectoryData>();

			// Process Files
			try
			{
				foreach (var filePath in Directory.GetFiles(directoryPath))
				{
					var fileName = Path.GetFileName(filePath);
					if (fileName.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;

					var exporter = _fileExporters.FirstOrDefault(e => e.CanExport(filePath));
					var fileContent = exporter?.ExportContent(filePath);

					var fileData = new FileData
					{
						Name = Path.GetFileNameWithoutExtension(fileName),
						Extension = Path.GetExtension(fileName).TrimStart('.'),
						Content = fileContent
					};
					directoryData.Files.Add(fileData);
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"[ScaffoldExporter Recurse] Error reading files in '{directoryPath}'. ErrorType: {ex.GetType().Name}, Message: {ex.Message}");
			}

			// Process Subdirectories
			try
			{
				foreach (var subDirPath in Directory.GetDirectories(directoryPath))
				{
					var subDirectoryData = BuildDirectoryDataRecursive(subDirPath);
					if (subDirectoryData != null) // Check if recursion returned valid data
					{
						directoryData.SubDirectories.Add(subDirectoryData);
					}
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"[ScaffoldExporter Recurse] Error reading subdirectories in '{directoryPath}'. ErrorType: {ex.GetType().Name}, Message: {ex.Message}");
			}

			// Return null if the directory is effectively empty (no files or subdirs processed)
			return directoryData;
		}
	}
}
