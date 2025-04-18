using System.Collections.Generic;
using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Utils;
using Newtonsoft.Json.Linq;

namespace ScaffoldKit.Editor.Importers
{
	/// <summary>
	/// Generates final string content from simple JToken value types, applying placeholders.
	/// </summary>
	public class ValueTokenContentProvider : IFileContentProvider
	{
		public bool CanProcess(string targetFileNameWithExt, FileData fileData)
		{
			if (fileData.Content == null) return false;

			switch (fileData.Content.Type)
			{
				case JTokenType.Integer:
				case JTokenType.Float:
				case JTokenType.Boolean:
				case JTokenType.Date:
				case JTokenType.Guid:
				case JTokenType.Uri:
				case JTokenType.TimeSpan:
					return true;
				default:
					return false;
			}
		}

		public string GetContent(FileData fileData, Dictionary<string, string> placeholderValues, string calculatedNamespace)
		{
			var initialContent = fileData.Content.ToString();
			return PlaceholderUtils.ApplyPlaceholders(initialContent, placeholderValues);
		}
	}
}