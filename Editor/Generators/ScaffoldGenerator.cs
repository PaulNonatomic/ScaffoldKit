using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Importers;
using ScaffoldKit.Editor.Utils;
using UnityEditor;
using UnityEngine;

namespace ScaffoldKit.Editor.Generators
{
	/// <summary>
	/// Handles the generation of directory structures and files based on Scaffold templates,
	/// replacing placeholders with user-provided values. Uses the selected project path as the root.
	/// Delegates file content generation to IFileContentProvider implementations and calculates contextual namespaces.
	/// </summary>
	public static class ScaffoldGenerator
	{
		private static readonly List<IFileContentProvider> _fileContentProviders;
		private const string RootNamespaceKey = "{{NAMESPACE}}";

		static ScaffoldGenerator()
		{
			_fileContentProviders = new List<IFileContentProvider>
			{
				new AsmdefContentProvider(),
				new CSharpContentProvider(),
				new JsonTokenContentProvider(),
				new StringTokenContentProvider(),
				new ValueTokenContentProvider(),
				new EmptyContentProvider() // Fallback last
			};
		}

		public static void Generate(ScaffoldFile template, string selectionPath, Dictionary<string, string> placeholderValues)
		{
			if (template?.TemplateData == null)
			{
				Debug.LogError("[ScaffoldGenerator] Template data is null.");
				EditorUtility.DisplayDialog("Generation Error", "Selected template data is invalid or null.", "OK");
				return;
			}
			
			if (placeholderValues == null) placeholderValues = new Dictionary<string, string>();

			// Get Root Namespace from placeholders (allow it to be empty)
			placeholderValues.TryGetValue(RootNamespaceKey, out var rootNamespaceValue);
			rootNamespaceValue = rootNamespaceValue ?? "";

			var rootDirectoryPath = ScaffoldPathUtils.GetGenerationRootPath(selectionPath);
			if (string.IsNullOrEmpty(rootDirectoryPath))
			{
				EditorUtility.DisplayDialog("Generation Error", "Could not determine root path. Select folder/file.", "OK");
				return;
			}

			Debug.Log($"[ScaffoldGenerator] Using generation root path: {rootDirectoryPath}");
			Debug.Log($"[ScaffoldGenerator] Using root namespace: '{rootNamespaceValue}' (from {RootNamespaceKey})");

			try
			{
				// Start recursion with the root namespace value as the base namespace segment
				GenerateDirectoryContents(template.TemplateData, rootDirectoryPath, placeholderValues, rootNamespaceValue);
			}
			catch (Exception e)
			{
				 Debug.LogError($"[ScaffoldGenerator] Error during generation: {e.Message}\n{e.StackTrace}");
				 EditorUtility.DisplayDialog("Generation Error", $"Error:\n{e.Message}", "OK");
				 AssetDatabase.Refresh();
				 return;
			}

			AssetDatabase.Refresh();
			var relativeRootPath = ScaffoldPathUtils.GetRelativePath(rootDirectoryPath);
			Debug.Log($"[ScaffoldGenerator] Generated structure in '{relativeRootPath}'. Refreshed.");

			var rootAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativeRootPath);
			if (rootAsset != null)
			{
				EditorGUIUtility.PingObject(rootAsset);
			}
			else {
				rootAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets");
				if (rootAsset != null) EditorGUIUtility.PingObject(rootAsset);
			}
		}

		/// <summary>
		/// Recursively processes directory contents, calculating namespace segments.
		/// </summary>
		/// <param name="directoryDataNode">ScaffoldData or DirectoryData node.</param>
		/// <param name="currentPath">Full disk path of the current directory.</param>
		/// <param name="placeholderValues">Global placeholder values.</param>
		/// <param name="currentNamespace">The fully qualified namespace up to this directory level.</param>
		private static void GenerateDirectoryContents(object directoryDataNode, string currentPath, Dictionary<string, string> placeholderValues, string currentNamespace)
		{
			List<DirectoryData> subDirectories = null;
			List<FileData> files = null;
			
			if (directoryDataNode is ScaffoldData rootData)
			{
				subDirectories = rootData.SubDirectories;
				files = rootData.Files;
			}
			else if (directoryDataNode is DirectoryData dirData)
			{
				subDirectories = dirData.SubDirectories;
				files = dirData.Files;
			}
			else
			{
				return;
			}

			// Process Files within the currentPath
			if (files != null)
			{
				foreach (var fileData in files)
				{
					if (fileData == null || string.IsNullOrWhiteSpace(fileData.Name)) continue;
					// Pass the final calculated namespace for this directory level
					GenerateFile(fileData, currentPath, placeholderValues, currentNamespace);
				}
			}

			// Process Subdirectories Recursively
			if (subDirectories == null) return;
			
			foreach (var subDirData in subDirectories)
			{
				if (subDirData == null || string.IsNullOrWhiteSpace(subDirData.Name)) continue;

				// Apply placeholders before sanitizing and path calculation
				var processedSubDirName = PlaceholderUtils.ApplyPlaceholders(subDirData.Name, placeholderValues);
				var sanitizedSubDirName = ScaffoldPathUtils.SanitizeFolderName(processedSubDirName);

				if (string.IsNullOrWhiteSpace(sanitizedSubDirName)) continue;

				var subDirPath = Path.Combine(currentPath, sanitizedSubDirName);
				try
				{
					Directory.CreateDirectory(subDirPath);

					var nextNamespace = currentNamespace; // Start with parent's namespace
					if (subDirData.ContributesToNamespace) // Check the flag on the subDirData
					{
						// Append sanitized name if flag is true
						if (!string.IsNullOrEmpty(nextNamespace))
						{
							nextNamespace += ".";
						}
						// Use the sanitized name for the namespace segment
						nextNamespace += sanitizedSubDirName;
					}
					
					GenerateDirectoryContents(subDirData, subDirPath, placeholderValues, nextNamespace);
				}
				catch (Exception e)
				{
					Debug.LogError($"[ScaffoldGenerator] Failed subdirectory '{sanitizedSubDirName}': {e.Message}");
					continue;
				}
			}
		}

		/// <summary>
		/// Generates a single file, delegating content generation to providers.
		/// </summary>
		/// <param name="fileData">FileData definition.</param>
		/// <param name="targetDirectoryPath">Full path to containing directory.</param>
		/// <param name="placeholderValues">Global placeholder values.</param>
		/// <param name="calculatedNamespace">The fully calculated namespace for this file's location.</param>
		private static void GenerateFile(FileData fileData, string targetDirectoryPath, Dictionary<string, string> placeholderValues, string calculatedNamespace)
		{
			try
			{
				// Process and sanitize file name components AFTER applying placeholders
				var baseFileName = PlaceholderUtils.ApplyPlaceholders(fileData.Name, placeholderValues);
				baseFileName = ScaffoldPathUtils.SanitizeFileName(baseFileName);
				if (string.IsNullOrWhiteSpace(baseFileName)) return; // Skip invalid names

				var fileNameWithExt = baseFileName;
				if (!string.IsNullOrWhiteSpace(fileData.Extension))
				{
					var processedExtension = PlaceholderUtils.ApplyPlaceholders(fileData.Extension, placeholderValues);
					var cleanExtension = "." + ScaffoldPathUtils.SanitizeFileName(processedExtension.TrimStart('.'));
					fileNameWithExt = $"{baseFileName}{cleanExtension}";
				}

				var filePath = Path.Combine(targetDirectoryPath, fileNameWithExt);

				// Get Final Content From Provider (includes internal placeholder replacement)
				var finalContent = string.Empty;
				var provider = _fileContentProviders.FirstOrDefault(p => p.CanProcess(fileNameWithExt, fileData));

				if (provider != null)
				{
					try
					{
						// Pass calculated namespace and global placeholders to provider
						finalContent = provider.GetContent(fileData, placeholderValues, calculatedNamespace);
					}
					catch (Exception ex)
					{
						Debug.LogError($"[ScaffoldGenerator] Error getting content from '{provider.GetType().Name}' for '{fileNameWithExt}': {ex.Message}");
						finalContent = $"// Error generating content: {ex.Message}";
					}
				}
				else
				{
					Debug.LogError($"[ScaffoldGenerator] CRITICAL: No provider for '{fileNameWithExt}'.");
				}

				// Write to file
				// Placeholder replacement is now done *inside* the provider's GetContent method
				File.WriteAllText(filePath, finalContent ?? string.Empty);
			}
			catch (Exception e)
			{
				var originalName = $"{fileData.Name}{(string.IsNullOrWhiteSpace(fileData.Extension) ? "" : "." + fileData.Extension.TrimStart('.'))}";
				Debug.LogError($"[ScaffoldGenerator] Failed file '{originalName}': {e.Message}");
			}
		}

		// Removed ReplacePlaceholders method - logic moved into providers via PlaceholderUtils
	}
}
