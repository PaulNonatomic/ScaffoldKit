using System;
using System.Collections.Generic;
using System.Text;
using ScaffoldKit.Editor.Importers;
using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Utils;
using Newtonsoft.Json.Linq;

namespace ScaffoldKit.Editor.Importers
{
	/// <summary>
	/// Generates final content for C# (.cs) files, handling boilerplate, applying placeholders, and using calculated namespace.
	/// </summary>
	public class CSharpContentProvider : IFileContentProvider
	{
		public bool CanProcess(string targetFileNameWithExt, FileData fileData)
		{
			return targetFileNameWithExt.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Gets the final C# content, applying placeholders and using calculated namespace.
		/// </summary>
		public string GetContent(FileData fileData, Dictionary<string, string> placeholderValues, string calculatedNamespace)
		{
			string initialContent;

			// Option 1: Template provides the full content as a string
			if (fileData.Content?.Type == JTokenType.String)
			{
				initialContent = fileData.Content.Value<string>() ?? string.Empty;
			}
			// Option 2: Generate default boilerplate
			else
			{
				var className = ScaffoldPathUtils.SanitizeFileName(fileData.Name);
				if (string.IsNullOrWhiteSpace(className)) className = "DefaultClassName";

				var sb = new StringBuilder();
				sb.AppendLine("using UnityEngine;");
				sb.AppendLine("using System.Collections;");
				sb.AppendLine("using System.Collections.Generic;");
				sb.AppendLine();
				sb.Append("namespace ").Append(calculatedNamespace); // Use calculated namespace
				if(string.IsNullOrWhiteSpace(calculatedNamespace)) {
					sb.Append(" // Warning: Namespace was empty"); // Add warning if calc'd ns is empty
				}
				sb.AppendLine();
				sb.AppendLine("{");
				sb.Append("\tpublic class ").Append(className).Append(" : MonoBehaviour"); // Default base class
				sb.AppendLine();
				sb.AppendLine("\t{");
				sb.AppendLine("\t\t// Start is called before the first frame update");
				sb.AppendLine("\t\tvoid Start()");
				sb.AppendLine("\t\t{");
				sb.AppendLine("\t\t\t");
				sb.AppendLine("\t\t}");
				sb.AppendLine();
				sb.AppendLine("\t\t// Update is called once per frame");
				sb.AppendLine("\t\tvoid Update()");
				sb.AppendLine("\t\t{");
				sb.AppendLine("\t\t\t");
				sb.AppendLine("\t\t}");
				sb.AppendLine("\t}");
				sb.AppendLine("}");
				initialContent = sb.ToString();
			}

			return PlaceholderUtils.ApplyPlaceholders(initialContent, placeholderValues);
		}
	}
}
