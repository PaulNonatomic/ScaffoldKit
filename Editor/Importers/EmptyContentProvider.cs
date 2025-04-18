using System.Collections.Generic;
using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Utils;

namespace ScaffoldKit.Editor.Importers
{
	/// <summary>
	/// A fallback provider that generates empty content. Applies placeholders (which does nothing on empty string).
	/// </summary>
	public class EmptyContentProvider : IFileContentProvider
	{
		public bool CanProcess(string targetFileNameWithExt, FileData fileData)
		{
			return true;
		}

		public string GetContent(FileData fileData, Dictionary<string, string> placeholderValues, string calculatedNamespace)
		{
			var initialContent = string.Empty;
			return PlaceholderUtils.ApplyPlaceholders(initialContent, placeholderValues);
		}
	}
}