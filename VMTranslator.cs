using System;
using System.Collections.Generic;
using System.IO;
using VM_Interpreter;

using static VM_Interpreter.ConsoleWriter;

internal class VMTranslator
{
    private static bool _debug = false;
    private static int Operations = 0;
    private static int Errors = 0;
    private static void Main(string[] args)
    {
        //ConsoleWriter.Write(new string[] { "This is an application to convert VM files to ASM files.", 
        //                                   "Press any key to continue..."}, ConsoleCode.MESSAGE, ConsoleOptions.Wait);

        string? read = args[0];
        
        List<string> fileNames = new();
        //foreach (string file in Directory.GetFiles(Directory.GetCurrentDirectory(), "*.vm", SearchOption.TopDirectoryOnly))
        //{
        //    fileNames.Add(file);
        //    ConsoleOptions options = ConsoleOptions.ConsoleBar;
        //    if (fileNames.IndexOf(file) > 0) options = ConsoleOptions.None;
        //    ConsoleWriter.Write(new string[] { $"File captured at: {file}" }, ConsoleCode.MESSAGE, options);
        //}

        if (read != null)
        {
            fileNames.Add(Directory.GetFiles(Directory.GetCurrentDirectory(), read, SearchOption.TopDirectoryOnly)[0]);
            ConsoleWriter.Write(new string[] { $"File captured at: {fileNames[0]}" }, ConsoleCode.MESSAGE, ConsoleOptions.ConsoleBar);

            VMParser parser = new();
            ASMWriter writer = new ASMWriter();
            foreach (string file in fileNames)
            {
                ConsoleWriter.Write(new string[] { $"Working on {Path.GetFileName(file)}" }, ConsoleCode.MESSAGE, ConsoleOptions.ConsoleBar);

                //Parse file into command list
                parser.Parse(file);
                if (_debug)
                {
                    string[] array = new string[1 + parser.WorkingFile.Count];
                    array[0] = $"Captured contents of {Path.GetFileName(file)}";
                    parser.WorkingFile.ToArray().CopyTo(array, 1);
                    ConsoleWriter.Write(array, ConsoleCode.DEBUG);
                }

                //Take command list and convert to ASM file
                writer.CreateASM(parser.WorkingFile, file.Replace(".vm", ".asm"));

                if (writer.OperationComplete && parser.OperationComplete) Operations++;
                else Errors++;

            }

            ConsoleWriter.Write(new string[] { $"Completed {Operations} operations with {Errors} errors" }, ConsoleCode.FINISH, ConsoleOptions.ConsoleBar);

        }

    }

}