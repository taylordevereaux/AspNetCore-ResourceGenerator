using System;
using System.Collections.Generic;
using System.Text;

namespace AspNetCore.ResourceGenerator
{
    public class ResourceFileData
    {
        public string ResourceFileDirectory { get; set; }
        public string FileName { get; set; }
        public List<string> ResourceKeys { get; set; }
        public ResourceFileData()
        {

        }
        public ResourceFileData(string resourceFileDirectory, string fileName, List<string> resourceKeys)
        {
            ResourceFileDirectory = resourceFileDirectory;
            FileName = fileName;
            ResourceKeys = resourceKeys;
        }

    }
}
