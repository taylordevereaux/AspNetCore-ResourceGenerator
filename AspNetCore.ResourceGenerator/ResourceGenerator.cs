using ICSharpCode.Decompiler.Metadata;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;

namespace AspNetCore.ResourceGenerator
{
    public class ResourceGenerator
    {
        public string ProjectDirectory { get; set; }
        public string ResourcesDirectory { get; set; }
        //public bool SkipExistingResourceFiles { get; set; } = true;

        public List<ResourceFileLanguage> LanguageEncodings { get; set; }

        public ResourceGenerator(
            string projectDirectory
            , string resourcesDirectory
            , List<ResourceFileLanguage> languageEncodings)
        {
            ProjectDirectory = projectDirectory;
            ResourcesDirectory = resourcesDirectory.Contains(projectDirectory) ? resourcesDirectory : Path.Combine(projectDirectory, resourcesDirectory);
            LanguageEncodings = languageEncodings;
            //SkipExistingResourceFiles = skipExistingResourceFiles;
        }

        public void GenerateResourceFiles(List<ResourceFileData> resourceFiles)
        {

            foreach (var resourceFile in resourceFiles)
            {
#if DEBUG
                Console.WriteLine($"{resourceFile.ResourceFileDirectory}\\{resourceFile.FileName}");
                foreach (var key in resourceFile.ResourceKeys)
                {
                    Console.WriteLine($" - {key}");
                }
#endif
                List<ResXDataNode> primaryEntries = null;
                
                // Generate the resource files for each language
                foreach (var language in LanguageEncodings.OrderBy(x => x.IsPrimary ? 0 : 1))
                {
                    if (language.IsPrimary)
                    {
                        primaryEntries = GenerateResourceFile(resourceFile, language);
                    }
                    else
                    {
                        GenerateResourceFile(resourceFile, language, primaryEntries);
                    }
                }

            }
        }

        private List<ResXDataNode> GenerateResourceFile(
            ResourceFileData resourceFile
            , ResourceFileLanguage language
            , List<ResXDataNode> primaryEntries = null)
        {
            var fileName = $"{resourceFile.FileName}.{language.Encoding}.resx";
            var filePath = Path.Combine(ResourcesDirectory, resourceFile.ResourceFileDirectory, fileName);

            string prefix = language.IsPrimary ? null : language.NonPrimaryResourceValuePrefix;
            string suffix = language.IsPrimary ? null : language.NonPrimaryResourceValueSuffix;

            var resourceEntries = GenerateEntriesFromKeys(
                resourceFile.ResourceKeys, 
                prefix,
                suffix);

            if (!language.IsPrimary && primaryEntries != null)
            {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
                System.ComponentModel.Design.ITypeResolutionService typeres = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
                resourceEntries.AddRange(
                    primaryEntries.Select(x => new ResXDataNode(x.Name, $"{prefix}{x.GetValue(typeres)}{suffix}") { Comment = "AUTO" })
                );
            }

            if (File.Exists(filePath))
            {
                return MergeResourceFile(filePath, resourceEntries, language);
            }
            else
            {
                var directory = new DirectoryInfo(Path.Combine(ResourcesDirectory, resourceFile.ResourceFileDirectory)); 
                if (!directory.Exists)
                {
                    directory.Create();
                }

                return CreateResourceFile(filePath, resourceEntries);
            }
        }

        private List<ResXDataNode> CreateResourceFile(string filePath, List<ResXDataNode> entries)
        {
            using (ResXResourceWriter resx = new ResXResourceWriter(filePath))
            { 
                foreach (var entry in entries)
                {
                    resx.AddResource(entry);
                }
            }
            return entries;
        }

        private List<ResXDataNode> MergeResourceFile(string filePath, List<ResXDataNode> resourceEntries, ResourceFileLanguage language)
        {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            System.ComponentModel.Design.ITypeResolutionService typeres = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used

            List<ResXDataNode> entries = new List<ResXDataNode>();
            using (ResXResourceReader resx = new ResXResourceReader(filePath))
            {
                resx.UseResXDataNodes = true;
                foreach (DictionaryEntry entry in resx)
                {
                    ResXDataNode node = (ResXDataNode)entry.Value;
                    entries.Add(node);
                }
            }

            // Add the Resources Entries from the parse that don't already exist.
            foreach (var entry in resourceEntries)
            {
                if (!entries.Exists(x => x.Name == entry.Name))
                {
                    entries.Add(entry);
                }
            }
            
            // Update Entries that are not found from the regex.
            foreach (var entry in entries)
            {
                if (!resourceEntries.Exists(x => x.Name == entry.Name))
                {
                    entry.Comment = "Resource Key not found from Auto Generation";
                }
            }

            using (ResXResourceWriter resx = new ResXResourceWriter(filePath))
            {
                foreach (var entry in entries)
                {
                    resx.AddResource(entry);
                }
            }

            return entries;
        }

        public List<ResourceFileData> ParseViews()
        {
            List<ResourceFileData> results = new List<ResourceFileData>();
            System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(ProjectDirectory);

            var viewsPath = Path.Combine(ProjectDirectory, "Views");

            if (System.IO.Directory.Exists(viewsPath))
            {
                var files = new System.IO.DirectoryInfo(viewsPath).GetFiles("*.cshtml", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    if (ParseView(file, out ResourceFileData result))
                    {
                        results.Add(result);
                    }
                }
            }
            return results;
        }

        private List<ResXDataNode> GenerateEntriesFromKeys(List<string> resourceKeys, string valuePrefix, string valueSuffix)
        {
            return resourceKeys.Select(x => new ResXDataNode(x, $"{valuePrefix}{x}{valueSuffix}") { Comment = "AUTO" }).ToList();
        }

        private bool ParseView(FileInfo file, out ResourceFileData result)
        {
            result = new ResourceFileData();

            result.ResourceFileDirectory = GetPathFromProjectRoot(file);

            //if (SkipExistingResourceFiles && File.Exists(filePath))
            //    return;

            result.FileName = file.Name.Replace(file.Extension, "");

            var text = File.ReadAllText(file.FullName);

            var localizerVariable = Regex.Match(text, "(?<=@inject IViewLocalizer )[\\w ]+");

            if (String.IsNullOrWhiteSpace(localizerVariable.Value))
                return false;

            var localizedMatch = Regex.Match(text, $"(?<=@{localizerVariable}\\[\")[\\w ]+");

            result.ResourceKeys = GetLocalizedStringKeys(localizedMatch);

            return result.ResourceKeys.Count > 0;
        }

        private List<string> GetLocalizedStringKeys(Match localizedMatch)
        {
            return GetLocalizedStringKeysEnumerator(localizedMatch).Distinct().ToList();
        }

        private IEnumerable<string> GetLocalizedStringKeysEnumerator(Match localizedMatch)
        {
            while (localizedMatch.Success)
            {
                if (!String.IsNullOrWhiteSpace(localizedMatch.Value))
                {
                    yield return localizedMatch.Value.Trim();
                }
                localizedMatch = localizedMatch.NextMatch();
            }
        }

        private string GetResourceFilePath(FileInfo file, string languageEncoding)
        {
            // First check if the resource file already exists.
            var pathFromDirectory = GetPathFromProjectRoot(file);

            var resourcefilePath = Path.Combine(ResourcesDirectory, pathFromDirectory);

            var resourceFileName = $"{file.Name.Replace(file.Extension, "")}.{languageEncoding}.resx";

            var filePath = Path.Combine(resourcefilePath, resourceFileName);

            return filePath;
        }

        private string GetPathFromProjectRoot(FileInfo file)
        {
            string path = "";
            var directory = file.Directory;
            const int maxCount = 256;
            int count = 0;
            while (!ProjectDirectory.EndsWith(directory.Name) && count++ < maxCount)
            {
                path = Path.Combine(directory.Name, path);
                directory = directory.Parent;
            }

            return path;
        }
    }
}
