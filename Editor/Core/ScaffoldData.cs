using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ScaffoldKit.Editor.Core
{
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
		public List<DirectoryData> SubDirectories;

		[JsonProperty("files")]
		public List<FileData> Files;
	}

	[Serializable]
	public class ScaffoldData
	{
		[JsonProperty("templateName")]
		public string TemplateName;

		[JsonProperty("templateVersion")]
		public string TemplateVersion;

		[JsonProperty("subDirectories")]
		public List<DirectoryData> SubDirectories;

		[JsonProperty("files")]
		public List<FileData> Files;
	}
	
	[Serializable]
	public class ScaffoldFile
	{
		public string FileName;
		public string FilePath;
		public ScaffoldData TemplateData;
	}
}