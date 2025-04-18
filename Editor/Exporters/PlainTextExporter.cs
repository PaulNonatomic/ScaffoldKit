using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ScaffoldKit.Editor.Exporters
{
	/// <summary>
	/// Exports content for common plain text files (.cs, .md, .txt, etc.) as a JSON string value.
	/// </summary>
	public class PlainTextExporter : IFileContentExporter
	{
		private static readonly HashSet<string> _textExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			".cs", ".txt", ".md", ".xml", ".html", ".css", ".js", ".uss", ".shader", ".cginc", ".hlsl"
		};

		public bool CanExport(string filePath)
		{
			var extension = Path.GetExtension(filePath);
			return !string.IsNullOrEmpty(extension) && _textExtensions.Contains(extension);
		}

		public JToken ExportContent(string filePath)
		{
			try
			{
				var content = File.ReadAllText(filePath);
				return new JValue(content);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[PlainTextExporter] Could not read file content for '{Path.GetFileName(filePath)}'. Error: {ex.Message}");
				return null;
			}
		}
	}
}