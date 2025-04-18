using System.Collections.Generic;
using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Utils;
using Newtonsoft.Json.Linq;

namespace ScaffoldKit.Editor.Importers
{
	/// <summary>
	/// Generates final string content directly from a JToken of type String, applying placeholders.
	/// </summary>
	public class StringTokenContentProvider : IFileContentProvider
	{
		public bool CanProcess(string targetFileNameWithExt, FileData fileData)
		{
			return fileData.Content?.Type == JTokenType.String;
		}

		public string GetContent(FileData fileData, Dictionary<string, string> placeholderValues, string calculatedNamespace)
		{
			var initialContent = fileData.Content.Value<string>() ?? string.Empty;
			return PlaceholderUtils.ApplyPlaceholders(initialContent, placeholderValues);
		}
	}
}