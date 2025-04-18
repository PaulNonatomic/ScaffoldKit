<div align=center>   

<p align="center">
  <img src="Readme~/logo.png" width="600" alt="ScaffoldKit Logo">
</p>

# Generate Unity project structures instantly.

Define reusable templates for any directory structure, including files, content, and placeholders, directly within Unity. Use ScaffoldKit to instantly generate consistent scaffolding for new features, packages, or projects, saving time and reducing setup errors.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![PullRequests](https://img.shields.io/badge/PRs-welcome-blueviolet)](http://makeapullrequest.com)
[![Releases](https://img.shields.io/github/v/release/PaulNonatomic/ScaffoldKit)](https://github.com/PaulNonatomic/ScaffoldKit/releases)
[![Unity](https://img.shields.io/badge/Unity-2022.3+-black.svg)](https://unity3d.com/pt/get-unity/download/archive)

</div>

---

## Installation
To install Scaffold Kit in your Unity project, add the package from the git URL: https://github.com/PaulNonatomic/ScaffoldKit.git using the Unity package manager.

## Overview

Setting up consistent folder structures, assembly definitions, and basic scripts for new features or modules in Unity can be repetitive and error-prone. ScaffoldKit streamlines this process by allowing you to define **Scaffold Templates** (`.skt` files). These templates capture directory layouts, file contents, and use placeholders for customization. With a few clicks, you can generate complex structures anywhere in your project, ensuring consistency and accelerating development.

## Features

* **Create Templates from Scratch:** Define `.skt` templates manually or via a simple editor action.
* **Export Existing Folders:** Convert any existing folder structure within your Unity project into a reusable `.skt` template.
* **Editor Window Generation:** A user-friendly editor window (`Assets/Create/ScaffoldKit/Generate Scaffold`) guides you through selecting a template, filling in placeholders, and generating the structure.
* **Dynamic Placeholder System:** Define custom placeholders for flexible template generation.
* **Automatic C# Namespace Calculation:** Ensures correct namespaces in generated `.cs` and `.asmdef` files based on folder structure.
* **File Content Embedding:** Include default content for various file types within your templates.
* **Custom Asset Handling:** `.skt` files have a unique icon and a custom inspector preview.
* **Refresh Templates:** Easily reload templates if they are modified or added while the generator window is open.

## Core Concepts

* **Scaffold Templates (`.skt` files):** These are JSON files that define the desired directory and file structure. They specify folder names, file names, file extensions, and even the initial content for files. `.skt` files have a custom icon and inspector preview within Unity.
* **Placeholders:** Templates can include placeholders using the `{{PLACEHOLDER_NAME}}` syntax within folder names, file names, extensions, and file content. When generating from a template, ScaffoldKit prompts you to provide values for these placeholders, allowing for dynamic customization.
    * **Special Placeholders:**
        * `{{NAMESPACE}}`: Used to define the root C# namespace for the generated structure.
        * `{{DOMAIN_NAME}}`: Often used as the primary name for the generated feature or module, commonly used for the `.asmdef` file's `name` property.
* **Automatic Namespace Generation:** For C# scripts (`.cs`) and Assembly Definitions (`.asmdef`), Scaffold Kit automatically calculates the appropriate namespace based on the folder structure relative to the root generation path and the `{{NAMESPACE}}` placeholder value. You can control which folders contribute to the namespace using the `contributesToNamespace` flag within the template's directory definitions.
* **Content Handling:** Templates can store initial file content. Scaffold Kit handles different types:
    * **Plain Text:** (.cs, .txt, .md, .xml, etc.) stored as a JSON string.
    * **JSON:** (.json, .asmdef, .asmref) stored as a JSON object or array.
    * **Other/Binary:** Content can be omitted (stored as `null`) if only the file structure is needed.
  
## How to Use

### 1. Creating a Scaffold Template (`.skt`)

You have two ways to create templates:

**a) From Scratch:**

1.  Navigate to the desired folder in your Unity Project window.
2.  Right-click and select `Assets/Create/ScaffoldKit/New Scaffold Template`.
3.  This creates a `NewScaffold.skt` file with a basic structure.
4.  Select the file and click "Edit Template" in the Inspector (or open it in your preferred text/JSON editor).
5.  Define your structure using the JSON format:

    ```json
    {
      "templateName": "My Feature Template", // Display name in the Generator Window
      "templateVersion": "1.0.0",
      "subDirectories": [ // Folders directly under the root generation path
        {
          "name": "Scripts",
          "contributesToNamespace": true, // This folder adds 'Scripts' to the namespace
          "subDirectories": [],
          "files": [
            {
              "name": "{{SCRIPT_NAME}}", // Placeholder for the script name
              "extension": "cs",
              "content": "// Initial content for {{SCRIPT_NAME}}.cs\nusing UnityEngine;\n\nnamespace {{NAMESPACE}}.Scripts // Calculated namespace\n{\n    public class {{SCRIPT_NAME}} : MonoBehaviour\n    {\n        // ...\n    }\n}"
            }
          ]
        },
        {
          "name": "Prefabs",
          "contributesToNamespace": false, // This folder does NOT add to the namespace
          "subDirectories": [],
          "files": []
        }
      ],
      "files": [ // Files directly under the root generation path
        {
          "name": "{{DOMAIN_NAME}}", // Placeholder, often used for asmdef
          "extension": "asmdef",
          "content": { // JSON content is stored as an object/array
            "name": "{{DOMAIN_NAME}}", // Will be replaced
            "rootNamespace": "", // Will be replaced by calculated namespace
            "references": [],
            "includePlatforms": [],
            "excludePlatforms": [],
            "allowUnsafeCode": false,
            "overrideReferences": false,
            "precompiledReferences": [],
            "autoReferenced": true,
            "defineConstraints": [],
            "versionDefines": [],
            "noEngineReferences": false
          }
        }
      ]
    }
    ```

**b) From an Existing Folder:**

1.  Select the folder in your Unity Project window that you want to use as a base for your template.
2.  Right-click and select `Assets/Create/ScaffoldKit/New Scaffold Template From Folder`.
3.  Scaffold Kit will analyze the folder's structure and contents, creating a new `.skt` file (e.g., `FolderName.skt`) in the *parent* directory of the selected folder.
4.  This generated template will capture the subdirectories and files, attempting to export content appropriately (text as strings, JSON as objects/arrays). You may need to edit the `.skt` file afterwards to add placeholders or adjust content.

### 2. Understanding Template Structure

* `templateName`: The name displayed in the Scaffold Generator window's dropdown.
* `templateVersion`: Version number for your template.
* `subDirectories`: An array of `DirectoryData` objects representing folders to create.
    * `name`: The name of the directory (can contain placeholders).
    * `contributesToNamespace` (boolean, optional, defaults to `false`): If `true`, this directory's sanitized name will be appended to the C# namespace for files within it and its subdirectories.
    * `subDirectories`: Nested array for subfolders.
    * `files`: An array of `FileData` objects within this directory.
* `files`: An array of `FileData` objects to create directly within the current directory level (either root or within a `DirectoryData` object).
    * `name`: The base name of the file (can contain placeholders).
    * `extension`: The file extension *without* the leading dot (can contain placeholders).
    * `content`: The initial content of the file. Can be:
        * A JSON string (for text files like `.cs`, `.txt`, `.md`).
        * A JSON object or array (for `.json`, `.asmdef`, `.asmref`).
        * A JSON primitive (`true`, `false`, number).
        * `null` (if no content should be generated, only the empty file).

### 3. Using Placeholders

Placeholders (`{{PLACEHOLDER_NAME}}`) can be used in:
* Directory `name` fields.
* File `name` fields.
* File `extension` fields.
* File `content` (when it's a string or within string values inside JSON content).

When you load a template in the Generator window, Scaffold Kit scans it and creates input fields for each unique placeholder found.

**Key Placeholders:**

* `{{NAMESPACE}}`: **Crucial.** Defines the root namespace. The final namespace for a C# file will be `{{NAMESPACE}}` plus any segments added by folders with `contributesToNamespace: true`.
* `{{DOMAIN_NAME}}`: **Recommended.** Use this for the primary identifier of your scaffolded element (e.g., the feature name). It's commonly used for the `.asmdef` file name and its `name` property.
* `{{SCRIPT_NAME}}`, `{{FEATURE_NAME}}`, etc.: Define any other placeholders you need for customization.

### 4. Generating Structures

1.  **Open the Generator Window:** Go to `Assets > Create > ScaffoldKit > Generate Scaffold`.
2.  **Select Template:** Choose your desired `.skt` template from the dropdown. If you don't see your template, ensure it's saved in your `Assets` folder and click the refresh button.
3.  **Load Template:** Click "Load Template".
4.  **Fill Placeholders:** The window will switch to Page 2, showing input fields for all placeholders detected in the template (like `{{NAMESPACE}}`, `{{DOMAIN_NAME}}`, `{{SCRIPT_NAME}}`, etc.). Fill these in carefully.
5.  **Select Target Location:** **Important:** In the *Unity Project window*, select the folder where you want the root of the generated structure to be created.
6.  **Generate:** Click "Generate Structure".

Scaffold Kit will create the directories and files defined in the template at the selected location, replacing placeholders and calculating namespaces as configured.

## Example `.skt` Template (Simple Feature)

```json
{
  "templateName": "Simple Feature Module",
  "templateVersion": "1.1.0",
  "subDirectories": [
    {
      "name": "Scripts",
      "contributesToNamespace": true,
      "subDirectories": [
        {
          "name": "Runtime",
          "contributesToNamespace": true,
          "subDirectories": [],
          "files": [
            {
              "name": "{{FEATURE_NAME}}Controller",
              "extension": "cs",
              "content": "using UnityEngine;\n\nnamespace {{NAMESPACE}}.Scripts.Runtime // Calculated: Root.Scripts.Runtime\n{\n    public class {{FEATURE_NAME}}Controller : MonoBehaviour\n    {\n        public void Initialize()\n        {\n            Debug.Log(\"{{FEATURE_NAME}} Initialized!\");\n        }\n    }\n}"
            }
          ]
        },
        {
          "name": "Editor",
          "contributesToNamespace": true,
          "subDirectories": [],
          "files": [
            {
                "name": "{{FEATURE_NAME}}Editor",
                "extension": "cs",
                "content": "using UnityEditor;\nusing UnityEngine;\n\nnamespace {{NAMESPACE}}.Scripts.Editor // Calculated: Root.Scripts.Editor\n{\n    [CustomEditor(typeof({{NAMESPACE}}.Scripts.Runtime.{{FEATURE_NAME}}Controller))]\n    public class {{FEATURE_NAME}}Editor : Editor\n    {\n        public override void OnInspectorGUI()\n        {\n            DrawDefaultInspector();\n            // Add custom editor logic here\n        }\n    }\n}"
            }
          ]
        }
      ],
      "files": []
    },
    {
      "name": "Tests",
      "contributesToNamespace": true, // {{NAMESPACE}}.Tests
      "subDirectories": [
         {
            "name": "Runtime",
            "contributesToNamespace": true, // {{NAMESPACE}}.Tests.Runtime
            "files": [],
            "subDirectories": []
         }
      ],
      "files": [
         {
             "name": "{{DOMAIN_NAME}}.Tests.Runtime",
             "extension": "asmdef",
             "content": {
                "name": "{{DOMAIN_NAME}}.Tests.Runtime", // Placeholder used
                "rootNamespace": "", // Will be calculated: {{NAMESPACE}}.Tests.Runtime
                "references": [
                    "GUID:{{GUID:UnityEngine.TestRunner}}", // Example: Reference Test Runner
                    "GUID:{{GUID:UnityEditor.TestRunner}}", // Example: Reference Test Runner
                    "GUID:{{GUID:{{DOMAIN_NAME}}.Runtime}}" // Example: Reference the main runtime asmdef
                ],
                "includePlatforms": [],
                "excludePlatforms": [],
                "allowUnsafeCode": false,
                "overrideReferences": true, // Important for tests
                "precompiledReferences": [
                    "nunit.framework.dll"
                ],
                "autoReferenced": false, // Test assemblies usually aren't auto-referenced
                "defineConstraints": [
                    "UNITY_INCLUDE_TESTS"
                ],
                "versionDefines": [],
                "noEngineReferences": false
             }
         }
      ]
    }
  ],
  "files": [
    {
      "name": "{{DOMAIN_NAME}}.Runtime", // Use DOMAIN_NAME for the main asmdef
      "extension": "asmdef",
      "content": {
        "name": "{{DOMAIN_NAME}}.Runtime", // Placeholder used
        "rootNamespace": "", // Will be calculated: {{NAMESPACE}}
        "references": [],
        "includePlatforms": [],
        "excludePlatforms": [],
        "allowUnsafeCode": false,
        "overrideReferences": false,
        "precompiledReferences": [],
        "autoReferenced": true,
        "defineConstraints": [],
        "versionDefines": [],
        "noEngineReferences": false
      }
    }
  ]
}
(Note: For GUID: references in .asmdef content, you'll need to either hardcode known GUIDs or potentially develop a more advanced placeholder system if you need dynamic GUID lookups during generation.):ContributingPull requests are welcome! For major changes, please open an issue first to discuss what you would like to change.Please make sure to update tests as appropriate.