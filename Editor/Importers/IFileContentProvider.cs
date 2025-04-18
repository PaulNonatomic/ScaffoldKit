using System.Collections.Generic;
using ScaffoldKit.Editor.Core;

namespace ScaffoldKit.Editor.Importers
{
	/// <summary>
	/// Interface for classes that can provide the final processed string content for a generated file
	/// based on its definition in the Scaffold template and contextual information.
	/// </summary>
	public interface IFileContentProvider
	{
		/// <summary>
		/// Determines if this provider can handle generating content for the given file.
		/// </summary>
		/// <param name="targetFileNameWithExt">The final calculated file name including extension (after placeholder replacement).</param>
		/// <param name="fileData">The original FileData object from the template.</param>
		/// <returns>True if this provider can process the file, false otherwise.</returns>
		bool CanProcess(string targetFileNameWithExt, FileData fileData);

		/// <summary>
		/// Generates the final processed string content for the file, including placeholder replacements.
		/// </summary>
		/// <param name="fileData">The FileData object from the template.</param>
		/// <param name="placeholderValues">The dictionary of globally collected placeholder values.</param>
		/// <param name="calculatedNamespace">The namespace calculated based on directory structure and root namespace placeholder.</param>
		/// <returns>The final processed string content for the file.</returns>
		string GetContent(FileData fileData, Dictionary<string, string> placeholderValues, string calculatedNamespace);
	}
}