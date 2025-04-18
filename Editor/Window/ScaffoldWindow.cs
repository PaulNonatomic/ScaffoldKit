using ScaffoldKit.Editor.Core;
using ScaffoldKit.Editor.Generators;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScaffoldKit.Editor.Window
{
	public class ScaffoldWindow : EditorWindow
	{
		private static readonly Regex PlaceholderRegex = new Regex(@"\{\{(.*?)\}\}", RegexOptions.Compiled);
		private static ScaffoldWindow _window;

		private VisualElement _root;
		private VisualElement _page1Container;
		private VisualElement _page2Container;
		private VisualElement _placeholderFieldsContainer;
		private DropdownField _templateDropdown;
		private Button _loadTemplateButton;
		private Button _generateButton;
		private Button _backButton;
		private Button _refreshButton;
		private ScaffoldLoader _scaffoldLoader;
		private ScaffoldFile _selectedTemplateFile;
		private Dictionary<string, TextField> _dynamicInputFields = new Dictionary<string, TextField>();

		[MenuItem("Assets/Create/ScaffoldKit/Generate Scaffold", false, 100)]
		public static void ShowEditor()
		{
			_window = GetWindow<ScaffoldWindow>();
			_window.titleContent = new GUIContent("Skaffold Generator");
			_window.minSize = new Vector2(450, 350);
		}

		public void CreateGUI()
		{
			_root = rootVisualElement;

			var baseStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.nonatomic.scaffoldkit/Editor/Window/ScaffoldWindowStyle.uss");
			if (baseStyleSheet != null)
			{
				_root.styleSheets.Add(baseStyleSheet);
			}
			else
			{
				Debug.LogWarning("[AssemblyBuilderWindow] Stylesheet not found.");
			}

			_scaffoldLoader = new ScaffoldLoader();
			_scaffoldLoader.FindAndLoadAllScaffoldFiles();

			_page1Container = new VisualElement { name = "Page1" };
			_page1Container.AddToClassList("page-container");
			_root.Add(_page1Container);

			_page2Container = new VisualElement { name = "Page2" };
			_page2Container.AddToClassList("page-container");
			_root.Add(_page2Container);

			BuildPage1();
			BuildPage2();

			ShowPage(1);
			UpdateTemplateDropdown();
		}

		private void BuildPage1()
		{
			_page1Container.Clear();
			
			var logoImg = new Image();
			logoImg.AddToClassList("logo");
			logoImg.image = Resources.Load<Texture2D>("logo");
			_page1Container.Add(logoImg);

			var lineBreak = new VisualElement();
			lineBreak.AddToClassList("line-break");
			_page1Container.Add(lineBreak);

			var templateSection = new VisualElement();
			templateSection.AddToClassList("section");
			_page1Container.Add(templateSection);

			var templateLabel = new Label("Select Template:");
			templateLabel.AddToClassList("section-label");
			templateSection.Add(templateLabel);

			_templateDropdown = new DropdownField();
			_templateDropdown.AddToClassList("template-dropdown");
			_templateDropdown.RegisterValueChangedCallback(evt => _selectedTemplateFile = _scaffoldLoader.GetTemplateByName(evt.newValue));
			templateSection.Add(_templateDropdown);

			var buttonContainer = new VisualElement();
			buttonContainer.AddToClassList("button-container");
			_page1Container.Add(buttonContainer);

			_loadTemplateButton = new Button(OnLoadTemplateClicked) { text = "Load Template" };
			_loadTemplateButton.AddToClassList("primary-btn");
			buttonContainer.Add(_loadTemplateButton);

			_refreshButton = new Button(RefreshTemplates);
			_refreshButton.AddToClassList("refresh-btn");
			buttonContainer.Add(_refreshButton);
			
			var icon = new Image();
			icon.AddToClassList("button-icon");
			icon.image = Resources.Load<Texture2D>("Icons/refresh");
			_refreshButton.Add(icon);
		}

		private void BuildPage2()
		{
			_page2Container.Clear();

			var logoImg = new Image();
			logoImg.AddToClassList("logo");
			logoImg.image = Resources.Load<Texture2D>("logo");
			_page2Container.Add(logoImg);
			
			var lineBreak = new VisualElement();
			lineBreak.AddToClassList("line-break");
			_page2Container.Add(lineBreak);

			var placeholderSection = new VisualElement();
			placeholderSection.AddToClassList("section");
			_page2Container.Add(placeholderSection);

			var placeholderLabel = new Label("Enter Placeholder Values:");
			placeholderLabel.AddToClassList("section-label");
			placeholderSection.Add(placeholderLabel);

			var scrollView = new ScrollView(ScrollViewMode.Vertical);
			scrollView.AddToClassList("placeholder-scrollview");
			placeholderSection.Add(scrollView);

			_placeholderFieldsContainer = new VisualElement { name = "PlaceholderFields" };
			_placeholderFieldsContainer.AddToClassList("placeholder-fields-container");
			scrollView.Add(_placeholderFieldsContainer);

			var buttonContainer2 = new VisualElement();
			buttonContainer2.AddToClassList("button-container");
			_page2Container.Add(buttonContainer2);

			_backButton = new Button(OnBackClicked);
			_backButton.AddToClassList("secondary-button");
			buttonContainer2.Add(_backButton);
			
			var icon = new Image();
			icon.AddToClassList("button-icon");
			icon.image = Resources.Load<Texture2D>("Icons/back");
			_backButton.Add(icon);

			_generateButton = new Button(GenerateAssembly) { text = "Generate Structure" };
			_generateButton.AddToClassList("primary-btn");
			buttonContainer2.Add(_generateButton);
		}

		private void ShowPage(int pageNumber)
		{
			_page1Container.style.display = (pageNumber == 1) ? DisplayStyle.Flex : DisplayStyle.None;
			_page2Container.style.display = (pageNumber == 2) ? DisplayStyle.Flex : DisplayStyle.None;
			_root.MarkDirtyRepaint();
		}

		private void UpdateTemplateDropdown()
		{
			var templateChoices = _scaffoldLoader.LoadedTemplates
				.Select(t => t.TemplateData?.TemplateName ?? "Invalid Template")
				.OrderBy(name => name)
				.ToList();

			var hasValidTemplates = templateChoices.Any(name => name != "Invalid Template") && templateChoices.Count > 0;

			if (!hasValidTemplates)
			{
				templateChoices.Clear();
				templateChoices.Add("No valid templates found");
				_templateDropdown.index = -1;
				_templateDropdown.choices = templateChoices;
				_templateDropdown.SetEnabled(false);
				_loadTemplateButton?.SetEnabled(false);
			}
			else
			{
				_templateDropdown.choices = templateChoices;
				_templateDropdown.SetEnabled(true);
				_loadTemplateButton?.SetEnabled(true);
				
				var currentSelection = _templateDropdown.value;
				_templateDropdown.index = (!string.IsNullOrEmpty(currentSelection) && templateChoices.Contains(currentSelection))
					? templateChoices.IndexOf(currentSelection)
					: 0;

				_selectedTemplateFile = _templateDropdown.index >= 0 
					? _scaffoldLoader.GetTemplateByName(_templateDropdown.choices[_templateDropdown.index]) 
					: null;
			}
		}

		private void RefreshTemplates()
		{
			_scaffoldLoader.ReloadAllTemplates();
			UpdateTemplateDropdown();
		}

		private HashSet<string> ScanTemplateForPlaceholders(ScaffoldFile template)
		{
			var uniquePlaceholders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (template?.TemplateData == null) return uniquePlaceholders;
			
			FindPlaceholdersRecursive(template.TemplateData, uniquePlaceholders);
			return uniquePlaceholders;
		}

		/// <summary>
		/// Recursively finds placeholders in ScaffoldData, DirectoryData, and FileData nodes.
		/// </summary>
		private void FindPlaceholdersRecursive(object dataNode, HashSet<string> foundPlaceholders)
		{
			if (dataNode == null) return;

			List<DirectoryData> subDirectories = null;
			List<FileData> files = null;

			switch (dataNode)
			{
				case ScaffoldData rootData:
					subDirectories = rootData.SubDirectories;
					files = rootData.Files;
					break;
				case DirectoryData dirData:
					ScanStringForPlaceholders(dirData.Name, foundPlaceholders);
					subDirectories = dirData.SubDirectories;
					files = dirData.Files;
					break;
				case FileData fileData:
				{
					ScanStringForPlaceholders(fileData.Name, foundPlaceholders);
					ScanStringForPlaceholders(fileData.Extension, foundPlaceholders);

					if (fileData.Content != null)
					{
						ScanJTokenForPlaceholders(fileData.Content, foundPlaceholders);
					}
					break;
				}
			}

			// Recursively scan files (if it was ScaffoldData or DirectoryData)
			if (files != null)
			{
				foreach (var file in files)
				{
					FindPlaceholdersRecursive(file, foundPlaceholders); // Recurse into FileData node itself
				}
			}

			// Recursively scan subdirectories (if it was ScaffoldData or DirectoryData)
			if (subDirectories != null)
			{
				foreach (var subDir in subDirectories)
				{
					FindPlaceholdersRecursive(subDir, foundPlaceholders); // Recurse into DirectoryData node
				}
			}
		}

		/// <summary>
		/// Scans a JToken structure recursively for placeholders within string values.
		/// </summary>
		private void ScanJTokenForPlaceholders(JToken token, HashSet<string> foundPlaceholders)
		{
			if (token == null) return;

			switch (token.Type)
			{
				case JTokenType.Object:
					
					foreach (var property in ((JObject)token).Properties())
					{
						ScanJTokenForPlaceholders(property.Value, foundPlaceholders); // Recurse into property value
					}
					break;

				case JTokenType.Array:
					
					foreach (var item in (JArray)token)
					{
						ScanJTokenForPlaceholders(item, foundPlaceholders);
					}
					break;

				case JTokenType.String:
					
					ScanStringForPlaceholders(token.Value<string>(), foundPlaceholders);
					break;

				default:
					break;
			}
		}


		/// <summary>
		/// Scans a single string for placeholder patterns using Regex.
		/// </summary>
		private void ScanStringForPlaceholders(string input, HashSet<string> foundPlaceholders)
		{
			if (string.IsNullOrEmpty(input)) return;

			var matches = PlaceholderRegex.Matches(input);
			foreach (Match match in matches)
			{
				if (!match.Success || match.Groups.Count <= 1) continue;
				
				var placeholderName = match.Groups[1].Value.Trim();
				if (string.IsNullOrWhiteSpace(placeholderName)) continue;
				
				foundPlaceholders.Add(placeholderName);
			}
		}
		
		private void PopulatePlaceholderFields(HashSet<string> placeholderNames)
		{
			_placeholderFieldsContainer.Clear();
			_dynamicInputFields.Clear();

			if (placeholderNames.Count == 0)
			{
				var noPlaceholdersLabel = new Label("No placeholders found in this template.");
				noPlaceholdersLabel.AddToClassList("info-label");
				_placeholderFieldsContainer.Add(noPlaceholdersLabel);
				return;
			}

			var sortedNames = placeholderNames.ToList();
			sortedNames.Sort(StringComparer.OrdinalIgnoreCase);

			foreach (var name in sortedNames)
			{
				var fieldContainer = new VisualElement();
				fieldContainer.AddToClassList("field-container");

				var labelText = FormatPlaceholderName(name) + ":";
				var label = new Label(labelText);
				label.AddToClassList("field-label");
				fieldContainer.Add(label);

				var textField = new TextField();
				textField.AddToClassList("text-field");
				textField.name = name;
				fieldContainer.Add(textField);

				_placeholderFieldsContainer.Add(fieldContainer);
				_dynamicInputFields.Add(name, textField);
			}
		}

		private string FormatPlaceholderName(string placeholderName)
		{
			if (string.IsNullOrWhiteSpace(placeholderName)) return "Invalid Placeholder";
			
			var parts = placeholderName.Split(new[] { '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
			for (var i = 0; i < parts.Length; i++)
			{
				if (parts[i].Length <= 0) continue;

				if (parts[i].All(char.IsUpper) && parts[i].Length > 1)
				{
					parts[i] = parts[i];
				}
				else
				{
					var partA = char.ToUpperInvariant(parts[i][0]);
					var partB = parts[i][1..].ToLowerInvariant();
					parts[i] = partA + partB;
				}
			}
			
			return string.Join(" ", parts);
		}

		private void OnLoadTemplateClicked()
		{
			if (_templateDropdown.index < 0 || _templateDropdown.index >= _scaffoldLoader.LoadedTemplates.Count)
			{
				EditorUtility.DisplayDialog("Error", "Please select a valid template.", "OK");
				return;
			}

			var templateName = _templateDropdown.value;
			_selectedTemplateFile = _scaffoldLoader.GetTemplateByName(templateName);

			if (_selectedTemplateFile == null)
			{
				EditorUtility.DisplayDialog("Error", $"Could not load template: {templateName}", "OK");
				ShowPage(1);
				return;
			}

			var placeholders = ScanTemplateForPlaceholders(_selectedTemplateFile);
			PopulatePlaceholderFields(placeholders);
			ShowPage(2);
		}

		private void OnBackClicked()
		{
			ShowPage(1);
		}

		private void GenerateAssembly()
		{
			if (_selectedTemplateFile == null)
			{
				EditorUtility.DisplayDialog("Error", "No template loaded.", "OK");
				ShowPage(1);
				return;
			}

			var placeholderValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var missingValues = new List<string>();

			foreach (var kvp in _dynamicInputFields)
			{
				var placeholderKey = $"{{{{{kvp.Key}}}}}";
				var value = kvp.Value.value;
				if (string.IsNullOrWhiteSpace(value)) missingValues.Add(FormatPlaceholderName(kvp.Key));
				placeholderValues[placeholderKey] = value ?? "";
			}

			if (missingValues.Count > 0)
			{
				EditorUtility.DisplayDialog("Missing Values", $"Please provide values for:\n\n- {string.Join("\n- ", missingValues)}", "OK");
				return;
			}

			var selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
			ScaffoldGenerator.Generate(_selectedTemplateFile, selectionPath, placeholderValues);
			
			Debug.Log($"Generation process initiated for template: {_selectedTemplateFile.TemplateData.TemplateName}");
			EditorUtility.DisplayDialog("Success", $"Skaffold generation initiated.", "OK");
		}
	}
}
