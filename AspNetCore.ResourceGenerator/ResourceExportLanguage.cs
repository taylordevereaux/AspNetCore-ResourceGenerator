using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.ResourceGenerator
{
    public class ResourceExportLanguage
    {
        public string Encoding { get; set; }
        public bool IsPrimary { get; set; }
        public bool IncludedResourceValues { get; set; }
        public ResourceExportLanguage()
        {

        }
        public ResourceExportLanguage(string encoding, bool isPrimary, bool includeResourceValues = true)
        {
            Encoding = encoding;
            IsPrimary = isPrimary;
            IncludedResourceValues = includeResourceValues;
        }
    }
}
