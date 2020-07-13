using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.ResourceGenerator
{
    public enum UpdateType
    {
        Overwrite,
        Prompt
    }

    public class ResourceImportLanguage
    {
        public string Encoding { get; set; }
        public UpdateType UpdateType { get; set; }

        public ResourceImportLanguage(string encoding, UpdateType updateType)
        {
            Encoding = encoding;
            UpdateType = updateType;
        }
    }
}
