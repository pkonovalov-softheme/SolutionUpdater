using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.Xml.Linq;

namespace ProjectUpdater
{

    class Program
    {
        //const string buildConfigurationDir = @"F:\gitrepos2\common\BuildConfiguration\";
        //const string trunkDir = @"F:\gitrepos2\common\test\ConsoleApplication15\";
        private static string buildConfigurationDir;
        private static string trunkDir;

        const bool Verbose = false;
        private static uint replacedN;
        private static uint errorsN;
        private static uint projectsCount;

        static void Main(string[] args)
        {

            Console.WriteLine("Please enter absolute path to your local 'Common' repository directory");
            buildConfigurationDir = Console.ReadLine() + "\\BuildConfiguration\\";

            if (!Directory.Exists(buildConfigurationDir))
                throw new ArgumentException("Directory {0} does not exists!", buildConfigurationDir);

            Console.WriteLine("Please enter absolute path to target directory where you want to update projects");
            trunkDir = Console.ReadLine();

            if (!Directory.Exists(trunkDir))
                throw new ArgumentException("Directory {0} does not exists!", trunkDir);

            Console.WriteLine("Searching *.sln files in {0} ...", trunkDir);
            string[] solutions = Directory.GetFiles(trunkDir, "*.sln", SearchOption.AllDirectories);

            foreach (string solution in solutions)
            {
                Console.WriteLine("Processing " + solution);
                UpdateSolution(solution);
            }

            Console.WriteLine("All done");
            Console.WriteLine("Total project count:{0}, Errors number:{1}, Succesufuly processed:{2}, Not found:{3} ", projectsCount, errorsN, replacedN, projectsCount - errorsN - replacedN);
            Console.ReadLine();
        }

        private static void UpdateSolution(string solutionFile)
        {
            StreamReader streamReader = File.OpenText(solutionFile);
            string solutionContents = streamReader.ReadToEnd();

            AddImportElement(solutionContents, "\"(?<word1>\\S+.csproj)", solutionFile, "Replay.CSharp.props", "<Import Project=\"$(MSBuildBinPath)\\Microsoft.CSharp.targets\" />");
            AddImportElement(solutionContents, "\"(?<word1>\\S+.vcxproj)", solutionFile, "Replay.Cpp.props", "<Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.targets\" />");
        }

        private static void AddImportElement(string solutionContents, string searchPattern, string solutionFile, string importElementType, string oldImportString)
        {
             var m = Regex.Match(solutionContents, searchPattern, RegexOptions.IgnoreCase);
            while (m.Success)
            {
                projectsCount++;
                string projectFileAbsPath = string.Empty;
                try
                {
                    projectFileAbsPath = Path.Combine(Path.GetDirectoryName(solutionFile), m.Groups[1].ToString());
                    if (Verbose)
                        Console.WriteLine("Patching " + projectFileAbsPath);
                    var streamReader = File.OpenText(projectFileAbsPath);
                    string projectContents = streamReader.ReadToEnd();
                    streamReader.Close();
                    string goodRelativePath = MakeRelativePath(projectFileAbsPath, buildConfigurationDir);
                    string repImport = "<Import Project=\"" + goodRelativePath + importElementType + "\" Condition=\"'$(DoNotImportReplayCommonSettings)'!='true'\"/>";
                    string fixedContent = projectContents.Replace(oldImportString, repImport + Environment.NewLine + "  " + oldImportString);
                    oldImportString = "<Import Project=\"$(MSBuildBinPath)\\Microsoft.CSharp.targets\" />";
                    fixedContent = fixedContent.Replace(oldImportString, repImport + Environment.NewLine + "  " + oldImportString);

                    if (fixedContent != projectContents)
                        replacedN++;

                    if (projectContents.Contains(importElementType))
                    {
                        if (Verbose)
                            Console.WriteLine("Allready contains Replay Import element");
                        m = m.NextMatch();
                        continue;
                    }

                    StreamWriter streamWriter = File.CreateText(projectFileAbsPath);
                    streamWriter.Write(fixedContent);
                    streamWriter.Close();
                }
                catch (Exception ex)
                {
                    errorsN++;
                    Console.WriteLine("Exception catched:{0} in progect {1}, solution {2}", ex.Message, projectFileAbsPath, solutionFile);
                }

                m = m.NextMatch();
            }
        }

        /// <summary>
        /// Creates a relative path from one file or folder to another.
        /// </summary>
        /// <param name="fromPath">Contains the directory that defines the start of the relative path.</param>
        /// <param name="toPath">Contains the path that defines the endpoint of the relative path.</param>
        /// <param name="dontEscape">Boolean indicating whether to add uri safe escapes to the relative path</param>
        /// <returns>The relative path from the start directory to the end path.</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static String MakeRelativePath(String fromPath, String toPath)
        {
            if (String.IsNullOrEmpty(fromPath)) throw new ArgumentNullException("fromPath");
            if (String.IsNullOrEmpty(toPath)) throw new ArgumentNullException("toPath");

            Uri fromUri = new Uri(fromPath);
            Uri toUri = new Uri(toPath);

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            String relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

    }

}
