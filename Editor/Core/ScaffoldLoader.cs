using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace ScaffoldKit.Editor.Core
{
	public class ScaffoldLoader
	{
		private const string SCAFFOLD_EXTENSION = "*.skt";
		public List<ScaffoldFile> LoadedTemplates = new();

		/// <summary>
		///     Finds and loads all Scaffold files in the project
		/// </summary>
		public void FindAndLoadAllScaffoldFiles()
		{
			LoadedTemplates.Clear();

			var assetsPath = Application.dataPath;
			var sktFiles = Directory.GetFiles(assetsPath, SCAFFOLD_EXTENSION, SearchOption.AllDirectories);

			foreach (var filePath in sktFiles)
			{
				LoadScaffoldFile(filePath);
			}
		}

		/// <summary>
		///     Loads a single Scaffold file from the given path
		/// </summary>
		public void LoadScaffoldFile(string filePath)
		{
			try
			{
				var jsonContent = File.ReadAllText(filePath);
				var fileName = Path.GetFileName(filePath);
				var parsedData = JsonConvert.DeserializeObject<ScaffoldData>(jsonContent);

				// Validate if this is a valid Scaffold file based on required fields
				if (parsedData != null && !string.IsNullOrEmpty(parsedData.TemplateName))
				{
					parsedData.PlaceholderDefinitions ??= new();
					parsedData.SubDirectories ??= new();
					parsedData.Files ??= new();

					var sktFile = new ScaffoldFile
					{
						FileName = fileName,
						FilePath = filePath,
						TemplateData = parsedData
					};

					LoadedTemplates.Add(sktFile);
				}
				else
				{
					Debug.LogWarning($"File '{fileName}' at path '{filePath}' doesn't match the expected Scaffold format or is missing required fields (e.g., templateName).");
				}
			}
			catch (JsonReaderException jsonEx)
			{
				Debug.LogError($"Error parsing JSON in Scaffold file '{filePath}': {jsonEx.Message} (Line: {jsonEx.LineNumber}, Pos: {jsonEx.LinePosition})");
			}
			catch (Exception e)
			{
				Debug.LogError($"Error loading Scaffold file '{filePath}': {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
			}
		}

		/// <summary>
		///     Gets an Scaffold file by its template name
		/// </summary>
		public ScaffoldFile GetTemplateByName(string templateName)
		{
			if (string.IsNullOrEmpty(templateName))
			{
				return null;
			}

			return LoadedTemplates.Find(template =>
				template.TemplateData != null &&
				template.TemplateData.TemplateName.Equals(templateName, StringComparison.OrdinalIgnoreCase));
		}


		/// <summary>
		///     Gets an Scaffold file by filename
		/// </summary>
		public ScaffoldFile GetTemplateByFileName(string fileName)
		{
			if (string.IsNullOrEmpty(fileName))
			{
				return null;
			}

			return LoadedTemplates.Find(template =>
				template.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
		}


		/// <summary>
		///     Reload all Scaffold files (useful for runtime updates)
		/// </summary>
		public void ReloadAllTemplates()
		{
			Debug.Log("Reloading all Scaffold templates...");
			FindAndLoadAllScaffoldFiles();
			Debug.Log($"Found and loaded {LoadedTemplates.Count} templates.");
		}

		/// <summary>
		///     Check if a file exists and is a valid Scaffold format based on basic parsing.
		///     Note: This is a basic check; full validation happens during loading.
		/// </summary>
		public bool IsValidScaffoldFile(string filePath)
		{
			if (!File.Exists(filePath))
			{
				return false;
			}

			// Check extension first
			if (!filePath.EndsWith(".skt", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			try
			{
				var jsonContent = File.ReadAllText(filePath);
				if (string.IsNullOrWhiteSpace(jsonContent))
				{
					return false;
				}

				// Attempt to parse and check for the presence of a key field (templateName)
				var parsedData = JsonConvert.DeserializeObject<ScaffoldData>(jsonContent);
				return parsedData != null && !string.IsNullOrEmpty(parsedData.TemplateName);
			}
			catch (JsonReaderException)
			{
				return false;
			}
			catch (Exception ex)
			{
				Debug.LogError($"Unexpected error validating scaffold file '{filePath}': {ex.Message}");
				return false;
			}
		}
	}
}