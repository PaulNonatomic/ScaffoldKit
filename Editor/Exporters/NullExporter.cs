using Newtonsoft.Json.Linq;

namespace ScaffoldKit.Editor.Exporters
{
	/// <summary>
	/// A fallback exporter that exports null content for any file type it's asked to process.
	/// Use this for binary files or file types where content export is not desired.
	/// </summary>
	public class NullExporter : IFileContentExporter
	{
		/// <summary>
		/// Can handle any file type (acts as a fallback).
		/// </summary>
		public bool CanExport(string filePath)
		{
			return true;
		}

		/// <summary>
		/// Always returns null, indicating no content should be included in the Scaffold file.
		/// </summary>
		public JToken ExportContent(string filePath)
		{
			return null;
		}
	}
}