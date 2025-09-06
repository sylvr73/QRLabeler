using QRLabeler.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace QRLabeler.Utilities
{
    public static class FileHelpers
    {
        public static List<LabelData> ReadExportFile(string fname)
        {
            var entries = new List<LabelData>();
            var regex = new Regex(@"(\d+)");

            using (var reader = new StreamReader(fname))
            {
                var columnIndices = new Dictionary<string, int>();
                string text = reader.ReadToEnd();
                var lines = text.SplitConsideringQuotes();
                foreach (var line in lines)
                {
                    try
                    {
                        if (0 == columnIndices.Count)
                        {
                            for (var i = 0; i < line.Count; i++)
                            {
                                if (columnIndices.ContainsKey(line[i]))
                                    continue;
                                columnIndices.Add(line[i], i);
                            }
                            continue;
                        }

                        var tableName = GetValue(columnIndices, line, "Table");
                        //var idx = tableName.IndexOf("T", StringComparison.OrdinalIgnoreCase);
                        //if (-1 != idx)
                        //    tableName = tableName.Substring(idx);
                        //else
                        //    Trace.Flush();
                        tableName = tableName.Replace("Specialty", "Spec.");
                        tableName = tableName.Replace("table", "Table");

                        //var paid = GetValue(columnIndices, line, "Paid");
                        //var received = GetValue(columnIndices, line, "Received");
                        //if (paid != "1" || received != "1")
                        //    continue;

                        var style = GetValue(columnIndices, line, "Style");
                        style = style.Replace(" Beer", string.Empty);
                        style = style.Replace("Vegetable", "Veg");

                        var entry = new LabelData
                        {
                            Carbonation = GetValue(columnIndices, line, "Sweetness"),
                            Category = GetValue(columnIndices, line, "Category"),
                            EntryNumber = GetValue(columnIndices, line, "Entry Number"),
                            JudgingNumber = GetValue(columnIndices, line, "Judging Number"),
                            RequiredInfo = GetValue(columnIndices, line, "Required Info"),
                            Strength = GetValue(columnIndices, line, "Strength"),
                            Subcategory = GetValue(columnIndices, line, "Subcategory"),
                            Sweetness = GetValue(columnIndices, line, "Carbonation"),
                            Style = style,
                            TableName = tableName,
                        };
                        
                        // one report had the column spelled this way
                        if (string.IsNullOrWhiteSpace(entry.Subcategory))
                        {
                            entry.Subcategory = GetValue(columnIndices, line, "Sub Category");
                        }

                        var match = regex.Match(entry.TableName);
                        if(match.Success)
                        {
                            entry.TableNumber = int.Parse(match.Captures[0].Value);
                        }
                        else
                        {
                            Console.WriteLine("Couldn't find table number!");
                        }

                        entries.Add(entry);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex}");
                    }
                }
            }

            return entries;
        }

        private static string GetValue(Dictionary<string, int> columnIndices, List<string> rowValues, string title)
        {
            if (false == columnIndices.TryGetValue(title, out int index) || index >= rowValues.Count)
                return string.Empty;
            return rowValues[index];
       }

        public static List<List<string>> SplitConsideringQuotes(this string input, bool removeEmpty = false, bool trim = true, char[] splitBy = null)
        {
            var allLines = new List<List<string>>();
            if (null == splitBy || 0 == splitBy.Length)
            {
                splitBy = new[] { ',', '\t' };
            }
            var start = 0;
            var inQuotes = false;
            var currentLine = new List<string>();
            for (var current = 0; current < input.Length; current++)
            {
                if (input[current] == '\n')
                {
                    if (false == inQuotes)
                    {
                        if (current > start)
                        {
                            var value = input.Substring(start, current - start);
                            if (trim)
                            {
                                value = value.Trim();
                            }
                            if (false == (removeEmpty && string.IsNullOrWhiteSpace(value)))
                            {
                                currentLine.Add(value);
                            }
                            start = current + 1;
                        }
                        allLines.Add(currentLine);
                        currentLine = new List<string>();
                    }  
                }

                if (input[current] == '\"')
                {
                    inQuotes = !inQuotes;
                }

                if (current == input.Length - 1)
                {
                    if (input[current] != ',')
                    {
                        currentLine.Add(input.Substring(start));
                    }
                    else
                    {
                        currentLine.Add(input.Substring(start, current - start));
                    }
                }
                else if (Array.IndexOf(splitBy, input[current]) != -1 && !inQuotes)
                {
                    var value = input.Substring(start, current - start);
                    if (trim)
                    {
                        value = value.Trim(new[] { ' ', '\n', '\t', '"' });
                    }
                    if (false == (removeEmpty && string.IsNullOrWhiteSpace(value)))
                    {
                        currentLine.Add(value);
                    }
                    start = current + 1;
                }
            }

            var cleaned = new List<List<string>>();
            foreach (var line in allLines)
                cleaned.Add(line.ConvertAll(l => l.Replace("\"", string.Empty)).ConvertAll(l=>l.Replace("\n", " / ")));

            return cleaned;
        }










    }
}
