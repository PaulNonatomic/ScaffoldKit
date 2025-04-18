using System;
using System.Collections.Generic;
using System.IO;
using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace ScaffoldKit.Editor.Importers
{
	/// <summary>
	/// Generates final content for .asmdef files, directly using calculated namespace
	/// and domain name placeholder, then applying other placeholders.
	/// </summary>
	public class AsmdefContentProvider : IFileContentProvider
	{
		private const string DomainNamePlaceholderKey = "{{DOMAIN_NAME}}";
		
		public bool CanProcess(string targetFileNameWithExt, FileData fileData)
		{
			return targetFileNameWithExt.EndsWith(".asmdef", StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Generates the final processed string content for the file, including placeholder replacements.
		/// Uses calculatedNamespace directly and attempts to use {{DOMAIN_NAME}} placeholder for the name.
		/// </summary>
		public string GetContent(FileData fileData, Dictionary<string, string> placeholderValues, string calculatedNamespace)
		{
			JObject templateContent = null;
			if (fileData.Content?.Type == JTokenType.Object)
			{
				templateContent = (JObject)fileData.Content.DeepClone();
			}

			// Create a base/default asmdef structure.
			var asmdef = new JObject
			{
				// Set name and rootNamespace initially to null or empty, they will be explicitly set later
				["name"] = "", // Will be replaced by DOMAIN_NAME placeholder value or default
				["rootNamespace"] = "", // Will be replaced by calculatedNamespace
				["references"] = new JArray(),
				["includePlatforms"] = new JArray(),
				["excludePlatforms"] = new JArray(),
				["allowUnsafeCode"] = false,
				["overrideReferences"] = false,
				["precompiledReferences"] = new JArray(),
				["autoReferenced"] = true,
				["defineConstraints"] = new JArray(),
				["versionDefines"] = new JArray(),
				["noEngineReferences"] = false
			};

			// Merge settings from the template over the defaults
			// This will bring in name/rootNamespace from the template if they exist
			if (templateContent != null)
			{
				asmdef.Merge(templateContent, new JsonMergeSettings
				{
					MergeArrayHandling = MergeArrayHandling.Replace
				});
			}

			// Explicitly set essential calculated/placeholder values
			// 1. Set name - Prioritize {{DOMAIN_NAME}} placeholder value if available
			if (placeholderValues.TryGetValue(DomainNamePlaceholderKey, out var domainNameValue) && !string.IsNullOrWhiteSpace(domainNameValue))
			{
				asmdef["name"] = domainNameValue;
			}
			else if (string.IsNullOrWhiteSpace(asmdef["name"]?.ToString() ?? ""))
			{
				// Fallback if placeholder wasn't provided and merge didn't set it
				var fallbackName = ScaffoldPathUtils.SanitizeFileName(Path.GetFileNameWithoutExtension(fileData.Name ?? "DefaultAsmdefName"));
				Debug.LogWarning($"[AsmdefContentProvider] '{DomainNamePlaceholderKey}' not found or empty in placeholders, and 'name' field was empty for {fileData.Name}. Using fallback name: '{fallbackName}'.");
				asmdef["name"] = fallbackName;
			}
			
			// Else: Keep the name that came from the merged templateContent if it was valid
			// 2. Set rootNamespace - Use the calculated namespace directly
			asmdef["rootNamespace"] = calculatedNamespace ?? "";

			
			var initialContent = asmdef.ToString(Formatting.Indented);
			return PlaceholderUtils.ApplyPlaceholders(initialContent, placeholderValues);
		}
	}
}
