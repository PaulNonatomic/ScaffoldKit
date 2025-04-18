using System.Collections.Generic;
using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ScaffoldKit.Editor.Importers
{
	/// <summary>
	/// Generates final string content from JObject or JArray tokens, applying placeholders.
	/// Note: Placeholders within nested JSON values are NOT currently replaced.
	/// </summary>
	public class JsonTokenContentProvider : IFileContentProvider
	{
		public bool CanProcess(string targetFileNameWithExt, FileData fileData)
		{
			return fileData.Content?.Type == JTokenType.Object || fileData.Content?.Type == JTokenType.Array;
		}

		public string GetContent(FileData fileData, Dictionary<string, string> placeholderValues, string calculatedNamespace)
		{
			var initialContent = fileData.Content.ToString(Formatting.Indented);
			return PlaceholderUtils.ApplyPlaceholders(initialContent, placeholderValues);
		}
	}
}