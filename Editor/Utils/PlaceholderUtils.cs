using System.Collections.Generic;

namespace ScaffoldKit.Editor.Utils
{
	/// <summary>
	/// Utility class for handling placeholder replacements.
	/// </summary>
	public static class PlaceholderUtils
	{
		/// <summary>
		/// Replaces known placeholder keys (e.g., "{{KEY}}") in a string with their values.
		/// </summary>
		/// <param name="input">The string potentially containing placeholders.</param>
		/// <param name="values">Dictionary mapping full placeholder keys to replacement values.</param>
		/// <returns>The string with placeholders replaced.</returns>
		public static string ApplyPlaceholders(string input, Dictionary<string, string> values)
		{
			if (string.IsNullOrEmpty(input) || values == null || values.Count == 0)
			{
				return input;
			}

			// Simple iterative replacement. Could use StringBuilder for performance on very large strings/many placeholders.
			// Consider Regex.Replace for more complex scenarios if needed later.
			var result = input;
			foreach (var kvp in values)
			{
				// Key is expected to be "{{PLACEHOLDER_NAME}}"
				if (!string.IsNullOrEmpty(kvp.Key)) // Basic check on key
				{
					result = result.Replace(kvp.Key, kvp.Value ?? ""); 
				}
			}
			return result;
		}
	}
}