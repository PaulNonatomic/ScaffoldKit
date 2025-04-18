using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScaffoldKit.Editor.Exporters
{
	/// <summary>
	/// Exports content for JSON-based files (.json, .asmdef, .asmref)
	/// by parsing them into JObject or JArray.
	/// </summary>
	public class JsonExporter : IFileContentExporter
	{
		private static readonly HashSet<string> _jsonExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			".json", ".asmdef", ".asmref"
		};

		public bool CanExport(string filePath)
		{
			var extension = Path.GetExtension(filePath);
			return !string.IsNullOrEmpty(extension) && _jsonExtensions.Contains(extension);
		}

		public JToken ExportContent(string filePath)
		{
			try
			{
				var content = File.ReadAllText(filePath);
				var parsedToken = JToken.Parse(content);
				return parsedToken;
			}
			catch (JsonReaderException jsonEx)
			{
				Debug.LogWarning($"[JsonExporter] Failed to parse JSON content for '{Path.GetFileName(filePath)}'. Invalid JSON. Error: {jsonEx.Message}"); 
				return null;
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[JsonExporter] Could not read or parse file content for '{Path.GetFileName(filePath)}'. Error: {ex.Message}");
				return null;
			}
		}
	}
}