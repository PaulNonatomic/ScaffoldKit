using Newtonsoft.Json.Linq; 

namespace ScaffoldKit.Editor.Exporters 
{
	/// <summary>
	/// Interface for classes that can read an existing file and export its content
	/// as a JToken suitable for inclusion in a Scaffold file's FileData.
	/// </summary>
	public interface IFileContentExporter
	{
		/// <summary>
		/// Determines if this exporter can handle reading the content of the given file path.
		/// </summary>
		/// <param name="filePath">The full path to the file.</param>
		/// <returns>True if this exporter can handle the file, false otherwise.</returns>
		bool CanExport(string filePath);

		/// <summary>
		/// Reads the file content and returns it as a JToken.
		/// </summary>
		/// <param name="filePath">The full path to the file to read.</param>
		/// <returns>A JToken representing the file content (e.g. JValue for string, JObject/JArray for JSON), or null if content should not be exported or an error occurred.</returns>
		JToken ExportContent(string filePath);
	}
}