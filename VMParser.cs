using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static VM_Interpreter.ConsoleWriter;

namespace VM_Interpreter
{
    internal class VMParser
    {
        public bool OperationComplete { get; private set; }
        public int Errors { get; private set; } = 0;
        public List<string> WorkingFile { get; private set; } = new();

        public void Parse(string filePath)
        {
            if (File.ReadLines(filePath) != null)
            {
                WorkingFile.Clear();
                OperationComplete = false;

                int totalLines = 0;
                bool isBlock = false;
                StreamReader sr = new StreamReader(filePath);

                string? workingLine;
                while ((workingLine = sr.ReadLine()) != null)
                {
                    totalLines++;
                    workingLine = workingLine.Trim();

                    /* Checks for comment blocks, empty lines, and trims comments 
                     * Not extremely thorough with block comments as none of the project files use block comment syntax
                     */
                    if (workingLine.Contains(@"/*") || isBlock)
                    {
                        string[] workLinesStart = workingLine.Split(@"/*");
                        if (workLinesStart.Count() > 2)
                        {
                            ConsoleWriter.Write(new string[] { $"Block comment error on line {totalLines} in {Path.GetFileName(filePath)}.",
                                                                "Skipping file and moving to next operation." }, ConsoleCode.ERROR, ConsoleOptions.ConsoleBar);
                            Errors++;
                            return;
                        }

                        isBlock = true;

                        if (workingLine.Contains(@"*/") && !workingLine.Contains(workLinesStart[0]))
                        {
                            string[] workLinesEnd = workingLine.Split(@"*/");
                            if (workLinesEnd.Count() > 2)
                            {
                                ConsoleWriter.Write(new string[] { $"Block comment error on line {totalLines} in {Path.GetFileName(filePath)}.",
                                                                    "Skipping file and moving to next operation." }, ConsoleCode.ERROR, ConsoleOptions.ConsoleBar);
                                Errors++;
                                return;
                            }
                            isBlock = false;
                        }
                        else
                        {
                            continue;
                        }

                    }
                    else if (IsWhiteSpace(workingLine))
                    {
                        continue;
                    }
                    else
                    {
                        WorkingFile.Add(TrimComments(workingLine));
                    }

                }

                sr.Close();

                if (WorkingFile.Count == 0)
                {
                    ConsoleWriter.Write(new string[] { $"No readable lines found in {Path.GetFileName(filePath)}" }, ConsoleCode.ERROR, ConsoleOptions.ConsoleBar);
                    Errors++;
                    return;
                }
                else
                {
                    ConsoleWriter.Write(new string[] { $"Successfully read {Path.GetFileName(filePath)}" }, ConsoleCode.SUCCESS);
                    OperationComplete = true;
                    return;
                }
            }
        }

        private static bool IsWhiteSpace(string line)
        {
            string workingLine = line.Trim();

            if (string.IsNullOrWhiteSpace(workingLine)) return true;
            if (workingLine.StartsWith("//")) return true;

            return false;
        }

        private static string TrimComments(string line)
        {
            string workingLine = line.Trim();

            if (workingLine.Contains("//"))
            {
                workingLine = workingLine.Split(@"//")[0];
            }

            return workingLine.Trim();
        }

    }
}
