using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ScaffoldKit.Editor.Core
{
	/// <summary>
	/// Defines the structure for specifying placeholder details in a .skt template.
	/// </summary>
	[Serializable]
	public class PlaceholderDefinition
	{
		[JsonProperty("key", Required = Required.Always)]
		public string Key;

		[JsonProperty("label", Required = Required.Always)]
		public string Label;

		[JsonProperty("description")]
		public string Description = "";

		[JsonProperty("defaultValue")]
		public string DefaultValue = ""; 

		[JsonProperty("order")]
		public int Order = int.MaxValue;
	}
	
	[Serializable]
	public class FileData
	{
		[JsonProperty("name")]
		public string Name;

		[JsonProperty("extension")]
		public string Extension;

		[JsonProperty("content")]
		public JToken Content;
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

		[JsonProperty("files")] public List<FileData> Files = new ();
	}

	[Serializable]
	public class ScaffoldData
	{
		[JsonProperty("templateName")]
		public string TemplateName;

		[JsonProperty("templateVersion")]
		public string TemplateVersion;

		[JsonProperty("subDirectories")]
		public List<DirectoryData> SubDirectories = new();

		[JsonProperty("files")]
		public List<FileData> Files = new();
		
		[JsonProperty("placeholderDefinitions")]
		public List<PlaceholderDefinition> PlaceholderDefinitions = new ();
	}
	
	[Serializable]
	public class ScaffoldFile
	{
		public string FileName;
		public string FilePath;
		public ScaffoldData TemplateData;
	}
}