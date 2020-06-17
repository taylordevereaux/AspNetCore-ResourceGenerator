using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.ResourceGenerator
{
    public class ResourceFileLanguage
    {
        public string Encoding { get; set; }
        public bool IsPrimary { get; set; }
        public string NonPrimaryResourceValueSuffix { get; set; }
        public string NonPrimaryResourceValuePrefix { get; set; }
        public ResourceFileLanguage()
        {

        }
        public ResourceFileLanguage(string encoding, bool isPrimary =  false)
        {
            Encoding = encoding;
            IsPrimary = isPrimary;
        }
        public ResourceFileLanguage(string encoding, string nonPrimaryResourceValuePrefix, string nonPrimaryResourceValueSuffix)
        {
            Encoding = encoding;
            IsPrimary = false;
            NonPrimaryResourceValueSuffix = nonPrimaryResourceValueSuffix;
            NonPrimaryResourceValuePrefix = nonPrimaryResourceValuePrefix;
        }
    }
}
