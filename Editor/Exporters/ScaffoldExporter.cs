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
			_fileExporters = new List<IFileContentExporter>
			{
				new JsonExporter(),
				new PlainTextExporter(),
				new NullExporter() // Fallback MUST be last
			};
		}


		[MenuItem(MENU_ITEM_PATH, false, 81)]
		private static void GenerateScaffoldFromFolder()
		{
			if (Selection.assetGUIDs.Length != 1) return;
			var guid = Selection.assetGUIDs[0];
			var selectedDirectoryPath_Relative = AssetDatabase.GUIDToAssetPath(guid);

			if (string.IsNullOrEmpty(selectedDirectoryPath_Relative) || !AssetDatabase.IsValidFolder(selectedDirectoryPath_Relative))
			{
				Debug.LogError("[ScaffoldExporter] Invalid selection.");
				return;
			}

			var projectRoot = Path.GetDirectoryName(Application.dataPath);
			var selectedDirectoryPathFull = Path.GetFullPath(Path.Combine(projectRoot, selectedDirectoryPath_Relative));
			var selectedFolderName = Path.GetFileName(selectedDirectoryPathFull);

			try
			{
				var sktData = new ScaffoldData
				{
					TemplateName = $"{selectedFolderName} Contents",
					TemplateVersion = "1.0.0",
					SubDirectories = new (),
					Files = new (),
					PlaceholderDefinitions = new (),
				};

				// Process Files Directly Under Selected Folder
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
					Debug.LogWarning($"[ScaffoldExporter] Error reading subdirs in root '{selectedDirectoryPathFull}'. Error: {ex.Message}");
				 }

				// Determine Output Path
				var parentPathRelative = Path.GetDirectoryName(selectedDirectoryPath_Relative);
				var outputFileName = $"{selectedFolderName}.skt";
				var outputFilePathRelative = Path.Combine(parentPathRelative, outputFileName);
				var outputFilePathFull = Path.GetFullPath(Path.Combine(projectRoot, outputFilePathRelative));

				// Serialize and Write File
				var jsonSettings = new JsonSerializerSettings
				{
					Formatting = Formatting.Indented,
					NullValueHandling = NullValueHandling.Ignore
				};
				var jsonContent = JsonConvert.SerializeObject(sktData, jsonSettings);

				File.WriteAllText(outputFilePathFull, jsonContent);
				Debug.Log($"[ScaffoldExporter] Generated Scaffold file with content at: {outputFilePathRelative}");

				// Refresh and Select
				AssetDatabase.Refresh();
				var createdAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(outputFilePathRelative);
				if (createdAsset == null) return;
			
				Selection.activeObject = createdAsset;
				EditorGUIUtility.PingObject(createdAsset);
			}
			catch (System.Exception e)
			{
				Debug.LogError($"[ScaffoldExporter] Failed Scaffold generation for '{selectedFolderName}'. Error: {e.Message}\n{e.StackTrace}");
				EditorUtility.DisplayDialog("Scaffold Export Error", $"Failed.\nError: {e.Message}", "OK");
			}
		}

		[MenuItem(MENU_ITEM_PATH, true)]
		private static bool GenerateScaffoldFromFolderValidation()
		{
			var guids = Selection.assetGUIDs;
			if (guids.Length != 1) return false;
			
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

			// Explicitly ensure lists are initialized as a safeguard
			if (directoryData.Files == null) directoryData.Files = new List<FileData>();
			if (directoryData.SubDirectories == null) directoryData.SubDirectories = new List<DirectoryData>();

			// Process Files
			try
			{
				foreach (var filePath in Directory.GetFiles(directoryPath))
				{
					var fileName = Path.GetFileName(filePath);
					if (fileName.EndsWith(".meta", System.StringComparison.OrdinalIgnoreCase)) continue;

					var exporter = _fileExporters.FirstOrDefault(e => e.CanExport(filePath));
					JToken fileContent = exporter?.ExportContent(filePath);

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
					if (subDirectoryData != null)
					{
						directoryData.SubDirectories.Add(subDirectoryData);
					}
				}
			}
			catch (System.Exception ex)
			{
				Debug.LogWarning($"[ScaffoldExporter Recurse] Error reading subdirs in '{directoryPath}'. ErrorType: {ex.GetType().Name}, Message: {ex.Message}");
			}

			return directoryData;
		}
	}
}
