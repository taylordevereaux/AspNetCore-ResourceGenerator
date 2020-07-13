using Microsoft.Office.Interop.Excel;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace AspNetCore.ResourceGenerator
{
    public delegate string Conflict(string existingValue, string newValue);
    public class ResourceImporter
    {
        private const string Key = "Key";
        private const string Import = "IMPORT";
        private const string SKIP_IMPORT = "SKIP_IMPORT";

        public string ResourcesDirectory { get; set; }
        public List<ResourceImportLanguage> LanguageEncodings { get; set; }
        public bool ImportCommonResourcesSheet { get; set; } = false;

        public bool ExportConflictResults { get; set; } = false;

        protected Dictionary<string, ResourceImportLanguage> LanguageDictionary { get => this.LanguageEncodings.ToDictionary(x => x.Encoding, x => x); }

        private System.Data.DataTable _conflicts = new System.Data.DataTable();


        public ResourceImporter(string resourcesDirectory, List<ResourceImportLanguage> languageEncodings, bool importCommonResourcesSheet = false)
        {
            ResourcesDirectory = resourcesDirectory;
            LanguageEncodings = languageEncodings;

            if (LanguageEncodings.Count == 0)
                throw new ArgumentException("At least one language is required");

            ImportCommonResourcesSheet = importCommonResourcesSheet;
        }



        public void ImportResourcesFromExcel(string excelFilePath)
        {
            ImportResourcesFromExcel(excelFilePath, null);
        }
        public void ImportResourcesFromExcel(string excelFilePath, Conflict conflictHandler)
        {
            System.Data.DataSet data = ParseExcel(excelFilePath);


            if (ExportConflictResults)
            {
                GenerateConflictsDataTable();
            }

            ImportData(data, conflictHandler);

            if (ExportConflictResults)
            {
                ExportConflicts();
            }
        }

        private void ImportData(DataSet data, Conflict conflictHandler)
        {
            System.Data.DataTable commonData = null;
            if (ImportCommonResourcesSheet && data.Tables["Common"] != null)
            {
                commonData = data.Tables["Common"];
            }

            var resourceFiles = GetResourceFiles();

            foreach (var resourceGroup in resourceFiles)
            {
                string tableName = resourceGroup.Key.StartsWith("_")
                    ? resourceGroup.Key.Substring(1, resourceGroup.Key.Length - 1)
                    : resourceGroup.Key;
                var table = data.Tables[tableName];

                if (table == null)
                    continue;

                Dictionary<string, List<ResXDataNode>> resources = new Dictionary<string, List<ResXDataNode>>();
                foreach (var resourceFile in resourceGroup.Value)
                {
                    string encoding = GetResourceEncoding(resourceFile);

                    List<ResXDataNode> entries = GetResourceEntries(resourceFile);

                    resources.Add(encoding, entries);
                }

                foreach (var language in LanguageEncodings)
                {
                    if (!table.Columns.Contains(language.Encoding))
                        continue;

                    if (!resources.ContainsKey(language.Encoding))
                        continue;

                    var entries = resources[language.Encoding];

                    List<ResXDataNode> newEntries = new List<ResXDataNode>();
                    foreach (var node in entries)
                    {
                        ResXDataNode newNode = node;

                        if (node.Comment != SKIP_IMPORT)
                        {
                            if (ImportCommonResourcesSheet && commonData != null)
                            {
                                newNode = MergeDataTableResource(commonData, newNode, language, conflictHandler);
                            }

                            newNode = MergeDataTableResource(table, newNode, language, conflictHandler);
                        }
                        newEntries.Add(newNode);
                    }

                    resources[language.Encoding] = newEntries;
                }

                foreach (var resourceFile in resourceGroup.Value)
                {
                    string encoding = GetResourceEncoding(resourceFile);

                    List<ResXDataNode> entries = resources[encoding];

                    using (ResXResourceWriter resx = new ResXResourceWriter(resourceFile.FullName))
                    {
                        foreach (var entry in entries)
                        {
                            resx.AddResource(entry);
                        }
                    }
                }
            }
        }

        private ResXDataNode MergeDataTableResource(System.Data.DataTable table, ResXDataNode node, ResourceImportLanguage language, Conflict conflictHandler)
        {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            System.ComponentModel.Design.ITypeResolutionService typeres = null;
#pragma warning restore CS0219 // Variable is assigned but its value is never used

            // Merge Data to node
            for (int r = 0; r < table.Rows.Count; ++r)
            {
                string key = table.Rows[r][Key].ToString();
                string value = table.Rows[r][language.Encoding].ToString();

                if (String.IsNullOrWhiteSpace(value))
                    continue;

                if (key == node.Name)
                {
                    if (language.UpdateType == UpdateType.Overwrite)
                    {
                        string existingValue = node.GetValue(typeres).ToString();
                        if (existingValue != value)
                        {
                            var overwriteNode = new ResXDataNode(key, value);
                            overwriteNode.Comment = Import;

                            return overwriteNode;
                        }
                    }
                    else if (language.UpdateType == UpdateType.Prompt)
                    {

                        string existingValue = node.GetValue(typeres).ToString();
                        if (existingValue != value
                            && conflictHandler != null)
                        {
                            Console.WriteLine($"Conflict: {table.TableName}.{key}");
                            string newValue = conflictHandler(existingValue, value);

                            if (ExportConflictResults)
                            {
                                LogConflict(table.TableName, key, existingValue, newValue, newValue == existingValue ? "EXISTING" : "NEW");
                            }

                            if (newValue == existingValue)
                                return node;

                            var promptNode = new ResXDataNode(key, newValue);
                            promptNode.Comment = Import;

                            return promptNode;
                        }
                    }
                }
            }
            return node;
        }

        private static List<ResXDataNode> GetResourceEntries(FileInfo resource)
        {
            List<ResXDataNode> entries = new List<ResXDataNode>();
            using (ResXResourceReader resx = new ResXResourceReader(resource.FullName))
            {
                resx.UseResXDataNodes = true;
                foreach (DictionaryEntry entry in resx)
                {
                    ResXDataNode node = (ResXDataNode)entry.Value;
                    entries.Add(node);
                }
            }

            return entries;
        }

        private static string GetResourceEncoding(FileInfo file)
        {
            int firstDot = file.Name.IndexOf('.') + 1;
            string encoding = file.Name.Substring(firstDot, file.Name.IndexOf('.', firstDot) - firstDot);
            return encoding;
        }
        private static string GetResourceName(FileInfo file)
        {
            int firstDot = file.Name.IndexOf('.');
            string name = file.Name.Substring(0, firstDot);
            return name;
        }

        private Dictionary<string, List<FileInfo>> GetResourceFiles()
        {
            var files = new System.IO.DirectoryInfo(ResourcesDirectory).GetFiles($"*.resx", SearchOption.AllDirectories);
            return files
                .GroupBy(x => GetResourceName(x))
                .OrderBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.ToList());
        }

        private System.Data.DataSet ParseExcel(string excelFilePath)
        {
            System.Data.DataSet data = new System.Data.DataSet();

            String excelConnectionString = $"Provider=Microsoft.ACE.OLEDB.16.0;Data Source={excelFilePath};Extended Properties=Excel 12.0;";

            using (OleDbConnection connection = new OleDbConnection(excelConnectionString))
            {
                connection.Open();

                var tables = connection.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);

                string[] sheetNames = new string[tables.Rows.Count];

                int i = 0;
                foreach (System.Data.DataRow row in tables.Rows)
                {
                    string sheetName = row["TABLE_NAME"].ToString();
                    sheetNames[i++] = sheetName;
                }

                foreach (var sheetName in sheetNames)
                {
                    using (OleDbCommand command = new OleDbCommand($"SELECT * FROM [{sheetName}]", connection))
                    {
                        OleDbDataAdapter adapter = new OleDbDataAdapter();
                        adapter.SelectCommand = command;

                        // Fill the DataSet with the information from the worksheet.
                        if (sheetName.EndsWith("$"))
                            adapter.Fill(data, sheetName.Substring(0, sheetName.Length - 1));
                        else
                            adapter.Fill(data, sheetName);
                    }
                }
            }

            return data;
        }

        #region Conflicts Export
        private void GenerateConflictsDataTable()
        {
            _conflicts = new System.Data.DataTable();
            _conflicts.TableName = "Conflicts";
            _conflicts.Columns.Add("Source");
            _conflicts.Columns.Add("Key");
            _conflicts.Columns.Add("ExistingText");
            _conflicts.Columns.Add("NewText");
            _conflicts.Columns.Add("Choice");
        }

        private void LogConflict(string source, string key, string existingText, string newText, string choice)
        {
            _conflicts.Rows.Add(source, key, existingText, newText, choice); 
        }


        private void ExportConflicts()
        {
            try
            {
                StringBuilder sb = new StringBuilder();

                IEnumerable<string> columnNames = _conflicts.Columns.Cast<DataColumn>().
                                                  Select(column => column.ColumnName);
                sb.AppendLine(string.Join(",", columnNames));


                foreach (DataRow row in _conflicts.Rows)
                {
                    IEnumerable<string> fields = row.ItemArray
                        .Select(
                            field => string.Concat("\"", field.ToString().Replace("\"", "\"\""), "\"")
                        );
                    sb.AppendLine(string.Join(",", fields));
                }

                File.WriteAllText($"ERI_Resources_Conflicts_{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}.csv", sb.ToString());

                //var excel = new ApplicationClass();
                //var workbook = excel.Workbooks.Add();

                //var worksheet = (Worksheet)excel.Worksheets.Add();
                //worksheet.Name = _conflicts.TableName;

                //for (int i = 1; i < _conflicts.Columns.Count + 1; ++i)
                //{
                //    worksheet.Cells[1, i] = _conflicts.Columns[i - 1].ColumnName;
                //}

                //for (int row = 2; row < _conflicts.Rows.Count + 2; ++row)
                //{
                //    for (int col = 1; col < _conflicts.Columns.Count + 1; ++col)
                //    {
                //        worksheet.Cells[row, col] = (string)_conflicts.Rows[row - 2][col - 1];
                //    }
                //}

                //workbook.SaveAs($"ERI_Resources_Conflicts_{DateTime.Now.ToString("yyyy-MM-dd-hh-mm-ss")}.xlsx");

                //excel.Quit();
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion
    }
}
