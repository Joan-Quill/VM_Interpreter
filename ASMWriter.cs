using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static VM_Interpreter.ConsoleWriter;

namespace VM_Interpreter
{
    internal class ASMWriter
    {
        private enum Command
        {
            none,
            push,
            pop,
            add,
            sub,
            neg,
            eq,
            gt,
            lt,
            and,
            or,
            not
        }

        private enum Segment 
        {
            none,
            local,
            argument,
            @this,
            that,
            constant,
            @static,
            pointer,
            temp
        }

        

        public bool OperationComplete { get; private set; }
        public int Errors { get; private set; } = 0;
        private int Labels { get; set; } = 0;

        //private List<string> WorkingFile { get; set; } = new();

        public void CreateASM(List<string> workingFile, string filePath)
        {
            OperationComplete = false;
            Labels = 0;
            Errors = 0;
            StreamWriter wr = new StreamWriter(filePath);

            for (int i = 0; i < workingFile.Count; i++)
            {
                string[] lineSegments = workingFile[i].Split(' ');

                if (lineSegments.Length == 0 ) 
                {
                    ConsoleWriter.Write(new string[] { $"Write error on line {i + 1} in {Path.GetFileName(filePath)}.",
                                                        "Zero length command encountered. Skipping file."}, ConsoleCode.ERROR);
                    Errors++;
                    break;
                }
                else if (lineSegments.Length >= 1)
                {
                    int value = 0;

                    //Read command
                    Command workingCommand = FindCommand(lineSegments[0]);
                    if (workingCommand == Command.none)
                    {
                        ConsoleWriter.Write(new string[] { $"Write error on line {i + 1} in {Path.GetFileName(filePath)}.",
                                                            "Error reading command. Skipping file."}, ConsoleCode.ERROR);
                        Errors++;
                        break;
                    }

                    //Read segment
                    Segment workingSegment = Segment.none;
                    if (lineSegments.Length >= 2)
                    {
                        workingSegment = FindSegment(lineSegments[1]);
                        value = Convert.ToInt32(lineSegments[2]);
                        if (workingSegment == Segment.none)
                        {
                            ConsoleWriter.Write(new string[] { $"Write error on line {i + 1} in {Path.GetFileName(filePath)}.",
                                                                "Error reading segment. Skipping file."}, ConsoleCode.ERROR);
                            Errors++;
                            break;
                        }
                    }

                    CommandBuilder(wr, workingCommand, workingSegment, value);

                }
                else
                {
                    ConsoleWriter.Write(new string[] { $"Write error on line {i + 1} in {Path.GetFileName(filePath)}.",
                                                        "Too many segments. Skipping file."}, ConsoleCode.ERROR);
                    wr.Close();
                    Errors++;
                    break;
                }
            }

            wr.Close();
            OperationComplete = true;
            ConsoleWriter.Write(new string[] { $"Successfully wrote to {Path.GetFileName(filePath)}" }, ConsoleCode.SUCCESS );
        }


        private static Command FindCommand(string command)
        {
            if (Enum.IsDefined(typeof(Command), command))
            {
                return (Command)Enum.Parse(typeof(Command), command);
            }
            else
            {
                return Command.none;
            }
        }

        private static Segment FindSegment(string segment)
        {
            if (Enum.IsDefined(typeof(Segment), segment))
            {
                return (Segment)Enum.Parse(typeof(Segment), segment);
            }
            else
            {
                return Segment.none;
            }
        }

        private void CommandBuilder(StreamWriter wr, Command command, Segment segment, int value)
        {
            if (segment != Segment.none)
            {
                switch (command) 
                {
                    case Command.push:
                        wr.WriteLine($"//Push {segment} {value}");
                        SegmentBuilder(wr, segment, value);
                        if (segment == Segment.constant) wr.WriteLine("D=A");
                        else wr.WriteLine("D=M");
                        PushStack(wr);
                        break;

                    case Command.pop:
                        wr.WriteLine($"//Pop {segment} {value}");
                        SegmentBuilder(wr, segment, value);
                        wr.WriteLine("D=A");
                        wr.WriteLine("@R13");
                        wr.WriteLine("M=D");
                        PopStack(wr);
                        wr.WriteLine("@R13");
                        wr.WriteLine("A=M");
                        wr.WriteLine("M=D");
                        break;
                }

            }
            else
            {
                wr.WriteLine($"//{command}");
                switch (command)
                {
                    case Command.add:
                        LogicInit(wr);
                        wr.WriteLine("M=D+M");
                        IncrementSP(wr);
                        break;

                    case Command.sub:
                        LogicInit(wr);
                        wr.WriteLine("M=M-D");
                        IncrementSP(wr);
                        break;

                    case Command.neg:
                        DecrementSP(wr);
                        SPtoA(wr);
                        wr.WriteLine("M=-M");
                        IncrementSP(wr);
                        break;

                    case Command.eq:
                        JumpBuilder(wr, "JEQ");
                        break;

                    case Command.gt:
                        JumpBuilder(wr, "JGT");
                        break;

                    case Command.lt:
                        JumpBuilder(wr, "JLT");
                        break;

                    case Command.and:
                        LogicInit(wr);
                        wr.WriteLine("M=D&M");
                        IncrementSP(wr);
                        break;

                    case Command.or:
                        LogicInit(wr);
                        wr.WriteLine("M=D|M");
                        IncrementSP(wr);
                        break;

                    case Command.not:
                        DecrementSP(wr);
                        SPtoA(wr);
                        wr.WriteLine("M=!M");
                        IncrementSP(wr);
                        break;
                }
            }
        }

        private static void SegmentBuilder(StreamWriter wr, Segment segment, int value)
        {
            switch (segment)
            {
                case Segment.local:
                    SegmentPart(wr, "LCL", value);
                    break;

                case Segment.argument:
                    SegmentPart(wr, "ARG", value);
                    break;

                case Segment.@this:
                    SegmentPart(wr, "THIS", value);
                    break;

                case Segment.that:
                    SegmentPart(wr, "THAT", value);
                    break;

                case Segment.constant:
                    wr.WriteLine("@" + value);
                    break;

                case Segment.@static:
                    wr.WriteLine("@static" + value);
                    break;

                case Segment.pointer:
                    int pointer = value + 3;
                    wr.WriteLine("@R" + pointer);
                    break;

                case Segment.temp:
                    int temp = value + 5;
                    wr.WriteLine("@R" + temp);
                    break;
            }
        }

        private static void SegmentPart(StreamWriter wr, string segment, int value)
        {
            wr.WriteLine("@" + segment);
            wr.WriteLine("D=M");
            wr.WriteLine("@" + value);
            wr.WriteLine("A=D+A");
        }

        private static void IncrementSP(StreamWriter wr)
        {
            wr.WriteLine("@SP");
            wr.WriteLine("M=M+1");
        }

        private static void DecrementSP(StreamWriter wr)
        {
            wr.WriteLine("@SP");
            wr.WriteLine("M=M-1");
        }

        private static void SPtoA(StreamWriter wr)
        {
            wr.WriteLine("@SP");
            wr.WriteLine("A=M");
        }

        private static void PushStack(StreamWriter wr)
        {
            SPtoA(wr);
            wr.WriteLine("M=D");
            IncrementSP(wr);
        }

        private static void PopStack(StreamWriter wr)
        {
            DecrementSP(wr);
            wr.WriteLine("A=M");
            wr.WriteLine("D=M");
        }

        private static void LogicInit(StreamWriter wr)
        {
            PopStack(wr);
            DecrementSP(wr);
            SPtoA(wr);
        }

        private void JumpBuilder(StreamWriter wr, string jump)
        {
            LogicInit(wr);
            wr.WriteLine("D=M-D");
            wr.WriteLine("@LABEL_" + Labels);
            wr.WriteLine("D;" +  jump);
            SPtoA(wr);
            wr.WriteLine("M=0");
            wr.WriteLine("@ENDLABEL_" + Labels);
            wr.WriteLine("0;JMP");
            wr.WriteLine("(LABEL_" + Labels + ")");
            SPtoA(wr);
            wr.WriteLine("M=-1");
            wr.WriteLine("(ENDLABEL_" + Labels + ")");
            IncrementSP(wr);
            Labels++;
        }
    }
}
