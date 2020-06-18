using Microsoft.Office.Interop.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.ResourceGenerator
{
    public class ResourceExporter
    {
        public string ResourcesDirectory { get; set; }
        public List<ResourceFileLanguage> LanguageEncodings { get; set; }
        public ResourceExporter(string resourcesDirectory, List<ResourceFileLanguage> languageEncodings)
        {
            ResourcesDirectory = resourcesDirectory;
            LanguageEncodings = languageEncodings;
        }

        public void ExportResourcesToExcel()
        {
            var excel = new Application();
            var workbook = excel.Workbooks.Add();

            var data = ParseResourceFiles();

            foreach (System.Data.DataTable table in data.Tables)
            {
                var worksheet = (Worksheet)excel.Worksheets.Add();
                try
                {
                    worksheet.Name = table.TableName;

                    for (int i = 1; i < table.Columns.Count + 1; ++i)
                    {
                        worksheet.Cells[1, i] = table.Columns[i - 1].ColumnName;
                    }

                    for (int row = 2; row < table.Rows.Count + 2; ++row)
                    {
                        worksheet.Cells[row, 1] = (string)table.Rows[row - 2]["Key"];
                        int languageIndex = 1;
                        foreach (var language in LanguageEncodings.OrderBy(x => x.IsPrimary ? 0 : 1))
                        {
                            worksheet.Cells[row, languageIndex + 1] = table.Rows[row - 2][language.Encoding];
                            languageIndex += 1;
                        }
                    }
                }
                catch(Exception ex)
                {

                }
            }
            workbook.SaveAs($"ERI_Resources_Export_{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}.xlsx");

            excel.Quit();
        }

        private DataSet ParseResourceFiles()
        {
            DataSet data = new DataSet();
            foreach (var language in LanguageEncodings.OrderBy(x => x.IsPrimary ? 0 : 1))
            {
                var files = new System.IO.DirectoryInfo(ResourcesDirectory).GetFiles($"*.{language.Encoding}.resx", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (language.IsPrimary)
                    {
                        System.Data.DataTable table = new System.Data.DataTable();
                        string name = file.Name
                            .Replace(file.Extension, "")
                            .Replace($".{language.Encoding}", "")
                            .Replace("_", "");

                        if (name.Length > 30)
                            name = name.Substring(0, 30);
                        table.TableName = name;
                        table.Columns.Add("Key");
                        table.Columns.Add(language.Encoding);
                        using (ResXResourceReader resx = new ResXResourceReader(file.FullName))
                        {
                            foreach (DictionaryEntry entry in resx)
                            {
                                var row = table.Rows.Add(entry.Key);
                                row[language.Encoding] = entry.Value;
                            }
                        }
                        data.Tables.Add(table);
                    }
                }
            }

            return data;
        }
    }
}
