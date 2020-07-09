using Microsoft.Office.Interop.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AspNetCore.ResourceGenerator
{
    public class ResourceExporter
    {
        private const string Key = "Key";

        public string ResourcesDirectory { get; set; }
        public List<ResourceExportLanguage> LanguageEncodings { get; set; }
        public bool GroupCommonResourcesToSharedSheet { get; set; } = false;
        public ResourceExporter(string resourcesDirectory, List<ResourceExportLanguage> languageEncodings, bool groupCommonResourcesToSharedSheet = false)
        {
            ResourcesDirectory = resourcesDirectory;
            LanguageEncodings = languageEncodings;

            if (LanguageEncodings.Count == 0)
                throw new ArgumentException("At least one language is required");

            GroupCommonResourcesToSharedSheet = groupCommonResourcesToSharedSheet;
        }

        public void ExportResourcesToExcel()
        {
            var excel = new Application();
            var workbook = excel.Workbooks.Add();

            var data = ParseResourceFiles();

            if (GroupCommonResourcesToSharedSheet)
            {
                GroupCommonResources(data);
            }

            foreach (System.Data.DataTable table in data.Tables)
            {
                GenerateExcelSheet(excel, table);
            }
            workbook.SaveAs($"ERI_Resources_Export_{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}.xlsx");

            excel.Quit();
        }

        private void GenerateExcelSheet(Application excel, System.Data.DataTable table)
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
                    worksheet.Cells[row, 1] = (string)table.Rows[row - 2][Key];
                    int languageIndex = 1;
                    foreach (var language in LanguageEncodings.OrderBy(x => x.IsPrimary ? 0 : 1))
                    {
                        worksheet.Cells[row, languageIndex + 1] = table.Rows[row - 2][language.Encoding];
                        languageIndex += 1;
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void GroupCommonResources(DataSet data)
        {
            const string Common = "Common";
            System.Data.DataTable table = data.Tables.Add(Common);
            table.Columns.Add(Key);
            foreach (var language in LanguageEncodings.OrderBy(x => x.IsPrimary ? 0 : 1))
            {
                table.Columns.Add(language.Encoding);
            }


            var primaryLanguage = LanguageEncodings.FirstOrDefault(x => x.IsPrimary);
            if (primaryLanguage == null)
                primaryLanguage = LanguageEncodings.FirstOrDefault();

            var commonEntries = GetQueryableDataSet(data, primaryLanguage)
                .GroupBy(x => x)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            foreach (var entry in commonEntries)
            {
                var row = table.Rows.Add(entry.Key);
                row[primaryLanguage.Encoding] = entry.Value;
            }

            // We now remove all common entries from the other tables.

            foreach (System.Data.DataTable t in data.Tables)
            {
                if (t.TableName != Common)
                {
                    for (int i = 0; i < t.Rows.Count; ++i)
                    {
                        if (commonEntries.Exists(x => (string)x.Key == (string)t.Rows[i][Key] 
                            && (string)x.Value == (string)t.Rows[i][primaryLanguage.Encoding]))
                        {
                            t.Rows.RemoveAt(i--);
                        }
                    }
                }
            }
        }

        private IEnumerable<DictionaryEntry> GetQueryableDataSet(DataSet data, ResourceExportLanguage language)
        {
            foreach (System.Data.DataTable t in data.Tables)
            {
                foreach (System.Data.DataRow row in t.Rows)
                {
                    yield return new DictionaryEntry(row[Key], row[language.Encoding]);
                }
            }
        }

        private DataSet ParseResourceFiles()
        {

            //var primaryLanguage = LanguageEncodings.FirstOrDefault(x => x.IsPrimary);
            //if (primaryLanguage == null)
            //    primaryLanguage = LanguageEncodings.FirstOrDefault();

            DataSet data = new DataSet();
            foreach (var language in LanguageEncodings.OrderBy(x => x.IsPrimary ? 0 : 1))
            {
                var files = new System.IO.DirectoryInfo(ResourcesDirectory).GetFiles($"*.{language.Encoding}.resx", SearchOption.AllDirectories)
                    // Ordering them d5escending because as they're added to the excel they're added to the front.
                    .OrderByDescending(x => x.Name.Replace("_", ""))
                    .ToList();
                foreach (var file in files)
                {
                    string tableName = file.Name
                           .Replace(file.Extension, "")
                           .Replace($".{language.Encoding}", "")
                           .Replace("_", "");
                    if (tableName.Length > 30)
                        tableName = tableName.Substring(0, 30);

                    if (language.IsPrimary)
                    {
                        CreateAndPopulateDataTable(data, language, file, tableName);
                    }
                    else
                    {
                        if (data.Tables[tableName] == null)
                        {
                            CreateAndPopulateDataTable(data, language, file, tableName);
                        }
                        else
                        {
                            System.Data.DataTable table = data.Tables[tableName];
                            table.Columns.Add(language.Encoding);

                            if (language.IncludedResourceValues)
                            {
                                PopulateDataTable(language, file, table);
                            }
                        }
                    }
                }
            }

            return data;
        }

        private static void CreateAndPopulateDataTable(DataSet data, ResourceExportLanguage language, FileInfo file, string tableName)
        {
            System.Data.DataTable table = CreateDataTable(language, tableName);

            PopulateDataTable(language, file, table);

            data.Tables.Add(table);
        }

        private static System.Data.DataTable CreateDataTable(ResourceExportLanguage language, string tableName)
        {
            System.Data.DataTable table = new System.Data.DataTable();
            table.TableName = tableName;
            table.Columns.Add(Key);
            table.Columns.Add(language.Encoding);
            return table;
        }

        private static void PopulateDataTable(ResourceExportLanguage language, FileInfo file, System.Data.DataTable table)
        {
            using (ResXResourceReader resx = new ResXResourceReader(file.FullName))
            {
                foreach (DictionaryEntry entry in resx)
                {
                    var row = table.Rows.Add(entry.Key);
                    row[language.Encoding] = entry.Value;
                }
            }
        }
    }
}
