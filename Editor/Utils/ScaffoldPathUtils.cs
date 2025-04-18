using System;
using System.IO;
using UnityEngine;

namespace ScaffoldKit.Editor.Utils
{
	/// <summary>
	/// Provides utility functions for path and name manipulation within the Scaffold context.
	/// </summary>
	public static class ScaffoldPathUtils
	{
		/// <summary>
		/// Determines the root path for generation based on the user's selection in the Project window.
		/// </summary>
		/// <param name="selectionPath">The relative path from AssetDatabase.GetAssetPath(Selection.activeObject).</param>
		/// <returns>The full path to the root folder for generation, or null if invalid.</returns>
		public static string GetGenerationRootPath(string selectionPath)
		{
			try
			{
				var projectRoot = Path.GetDirectoryName(Application.dataPath);

				// Handle case where nothing is selected or selection path is empty
				if (string.IsNullOrEmpty(selectionPath))
				{
					Debug.LogError("[ScaffoldPathUtils] No folder or file selected in the Project window. Cannot determine root path for generation.");
					return null;
				}

				// Calculate full path of the selection
				var fullSelectionPath = Path.GetFullPath(Path.Combine(projectRoot, selectionPath));

				// Check if selection is a valid directory
				if (Directory.Exists(fullSelectionPath))
				{
					return fullSelectionPath;
				}

				// Check if selection is a valid file
				if (File.Exists(fullSelectionPath))
				{
					return Path.GetDirectoryName(fullSelectionPath);
				}

				// If selection is not a valid file or directory
				Debug.LogError($"[ScaffoldPathUtils] Selection path '{selectionPath}' does not point to an existing file or folder. Cannot determine root path.");
				return null;
			}
			catch (Exception e)
			{
				Debug.LogError($"[ScaffoldPathUtils] Error determining generation root path from selection '{selectionPath}': {e.Message}.");
				return null;
			}
		}

		/// <summary>
		/// Converts a full system path back to a project-relative path used by AssetDatabase.
		/// </summary>
		/// <param name="fullPath">The full system path.</param>
		/// <returns>The path relative to the Unity project root, or the original path if outside the project.</returns>
		public static string GetRelativePath(string fullPath)
		{
			try
			{
				// Ensure consistent path separators
				fullPath = fullPath.Replace("\\", "/");
				if (string.IsNullOrEmpty(Application.dataPath)) return fullPath;

				var projectPath = Application.dataPath.Replace("\\", "/");
				var projectRoot = Path.GetDirectoryName(projectPath)?.Replace("\\", "/");

				// Handle case where project root couldn't be determined
				if (string.IsNullOrEmpty(projectRoot)) return fullPath;

				 if (!projectRoot.EndsWith("/"))
				 {
					 projectRoot += "/";
				 }

				if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
				{
					return fullPath.Substring(projectRoot.Length);
				}

				// If it's not within the project structure
				Debug.LogWarning($"[ScaffoldPathUtils] Full path '{fullPath}' seems outside the project root '{projectRoot}'. Returning original path.");
				return fullPath;
			}
			catch (Exception ex)
			{
				Debug.LogError($"[ScaffoldPathUtils] Error converting path '{fullPath}' to relative path: {ex.Message}");
				return fullPath;
			}
		}

		/// <summary>
		/// Removes characters that are invalid in folder names.
		/// </summary>
		public static string SanitizeFolderName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return string.Empty;
			
			var invalidChars = new string(Path.GetInvalidPathChars()) + new string(Path.GetInvalidFileNameChars());
			var sanitized = name;
			foreach (char c in invalidChars)
			{
				sanitized = sanitized.Replace(c.ToString(), "");
			}
			
			return sanitized.Trim();
		}

		/// <summary>
		/// Removes characters that are invalid in file names.
		/// </summary>
		public static string SanitizeFileName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return string.Empty;
			
			var invalidChars = new string(Path.GetInvalidFileNameChars());
			var sanitized = name;
			foreach (char c in invalidChars)
			{
				sanitized = sanitized.Replace(c.ToString(), "");
			}
			
			return sanitized.Trim();
		}
	}
}
