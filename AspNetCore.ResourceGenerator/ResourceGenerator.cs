using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;

namespace AspNetCore.ResourceGenerator
{
    public class ResourceGenerator
    {
        public string ProjectDirectory { get; set; }
        public string ResourcesDirectory { get; set; }
        public bool SkipExistingResourceFiles { get; set; } = true;

        public List<string> LanguageEncodings { get; set; }

        public ResourceGenerator(
            string projectDirectory
            , string resourcesDirectory
            , List<string> languageEncodings
            , bool skipExistingResourceFiles)
        {
            ProjectDirectory = projectDirectory;
            ResourcesDirectory = resourcesDirectory.Contains(projectDirectory) ? resourcesDirectory : Path.Combine(projectDirectory, resourcesDirectory);
            LanguageEncodings = languageEncodings;
            SkipExistingResourceFiles = skipExistingResourceFiles;
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

                // Generate the resource files for each language
                foreach (var language in LanguageEncodings)
                {
                    GenerateResourceFile(resourceFile, language);
                }

            }
        }

        private void GenerateResourceFile(ResourceFileData resourceFile, string language)
        {
            var fileName = $"{resourceFile.FileName}.{language}.resx";
            var filePath = Path.Combine(ResourcesDirectory, resourceFile.ResourceFileDirectory, fileName);

            if (File.Exists(filePath))
            {
                MergeResourceFile(filePath, resourceFile.ResourceKeys);
            }
            else
            {
                var directory = new DirectoryInfo(Path.Combine(ResourcesDirectory, resourceFile.ResourceFileDirectory)); 
                if (!directory.Exists)
                {
                    directory.Create();
                }
#if DEBUG
                filePath = Path.Combine(ResourcesDirectory, resourceFile.ResourceFileDirectory, $"TEST_{fileName}");
#endif
                CreateResourceFile(filePath, resourceFile.ResourceKeys);
            }
        }

        private void CreateResourceFile(string filePath, List<string> resourceKeys)
        {
            using (System.Resources.ResXResourceWriter resx = new System.Resources.ResXResourceWriter(filePath))
            { 
                foreach (var key in resourceKeys)
                {
                    resx.AddResource(key, key);
                }
            }
        }

        private void MergeResourceFile(string filePath, List<string> resourceKeys)
        {
            //using (ResXResourceReader resxReader = new ResXResourceReader(resxFile))
            //{
            //}
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
                    yield return localizedMatch.Value;
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
