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

				// Validate if this is a valid Scaffold file
				if (parsedData != null && !string.IsNullOrEmpty(parsedData.TemplateName))
				{
					var sktFile = new ScaffoldFile
					{
						FileName = fileName,
						FilePath = filePath,
						TemplateData = parsedData
					};

					LoadedTemplates.Add(sktFile);
					Debug.Log($"Successfully loaded Scaffold file: {fileName}");
				}
				else
				{
					Debug.LogWarning($"File {fileName} doesn't match the expected Scaffold format");
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"Error loading Scaffold file {filePath}: {e.Message}");
			}
		}

		/// <summary>
		///     Gets an Scaffold file by its template name
		/// </summary>
		public ScaffoldFile GetTemplateByName(string templateName)
		{
			return LoadedTemplates.Find(template =>
				template.TemplateData.TemplateName.Equals(templateName, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		///     Gets an Scaffold file by filename
		/// </summary>
		public ScaffoldFile GetTemplateByFileName(string fileName)
		{
			return LoadedTemplates.Find(template =>
				template.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		///     Reload all Scaffold files (useful for runtime updates)
		/// </summary>
		public void ReloadAllTemplates()
		{
			FindAndLoadAllScaffoldFiles();
		}

		/// <summary>
		///     Check if a file exists and is a valid Scaffold format
		/// </summary>
		public bool IsValidScaffoldFile(string filePath)
		{
			if (!File.Exists(filePath))
			{
				return false;
			}

			if (!filePath.EndsWith(".skt", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			try
			{
				var jsonContent = File.ReadAllText(filePath);
				var parsedData = JsonUtility.FromJson<ScaffoldData>(jsonContent);
				return parsedData != null && !string.IsNullOrEmpty(parsedData.TemplateName);
			}
			catch
			{
				return false;
			}
		}
	}
}