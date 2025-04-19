using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace ScaffoldKit.Editor.Core
{
	/// <summary>
	///     Defines the type of UI control to generate for a placeholder.
	/// </summary>
	public enum PlaceholderType
	{
		String, // Default text input
		Boolean, // Checkbox / Toggle

		Enum // Dropdown list
		// Future: Integer, Float, FilePath, etc.
	}

	/// <summary>
	///     Defines the structure for specifying placeholder details in a .skt template.
	/// </summary>
	[Serializable]
	public class PlaceholderDefinition
	{
		[JsonProperty("key", Required = Required.Always)]
		public string Key;

		[JsonProperty("label", Required = Required.Always)]
		public string Label;

		[JsonProperty("description")] public string Description = "";

		[JsonProperty("type", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)] 
		[JsonConverter(typeof(StringEnumConverter))]
		public PlaceholderType Type = PlaceholderType.String;

		[JsonProperty("options", NullValueHandling = NullValueHandling.Ignore)]
		public List<string> Options;

		[JsonProperty("defaultValue", NullValueHandling = NullValueHandling.Ignore)]
		public string DefaultValue = "";

		[JsonProperty("order", DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
		public int Order = int.MaxValue;

		public bool GetDefaultBoolean()
		{
			return bool.TryParse(DefaultValue, out var result) && result;
		}

		public string GetValidatedEnumDefault()
		{
			if (Type != PlaceholderType.Enum || Options == null || Options.Count == 0)
			{
				return null;
			}

			// Use the first option as fallback if default is invalid or empty
			if (!string.IsNullOrEmpty(DefaultValue) && Options.Contains(DefaultValue))
			{
				return DefaultValue;
			}

			return Options[0];
		}
	}


	[Serializable]
	public class FileData
	{
		[JsonProperty("name")] public string Name;
		[JsonProperty("extension")] public string Extension;
		[JsonProperty("content")] public JToken Content;
	}

	[Serializable]
	public class DirectoryData
	{
		[JsonProperty("name")] 
		public string Name;
		
		[JsonProperty("contributesToNamespace", DefaultValueHandling = DefaultValueHandling.Ignore)]
		public bool ContributesToNamespace;

		[JsonProperty("subDirectories")] 
		public List<DirectoryData> SubDirectories = new();

		[JsonProperty("files")] 
		public List<FileData> Files = new();
	}

	[Serializable]
	public class ScaffoldData
	{
		[JsonProperty("templateName")] 
		public string TemplateName;

		[JsonProperty("templateVersion")] 
		public string TemplateVersion;

		[JsonProperty("placeholderDefinitions")]
		public List<PlaceholderDefinition> PlaceholderDefinitions = new();

		[JsonProperty("subDirectories")] 
		public List<DirectoryData> SubDirectories = new();

		[JsonProperty("files")] 
		public List<FileData> Files = new();
	}

	[Serializable]
	public class ScaffoldFile
	{
		public string FileName;
		public string FilePath;
		public ScaffoldData TemplateData;
	}
}