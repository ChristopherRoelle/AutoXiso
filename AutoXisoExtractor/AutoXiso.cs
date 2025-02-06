using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoXisoExtractor
{
    internal class AutoXiso
    {
        private string inputPath = ".\\input\\";
        private string outputPath = ".\\output\\";
        private string xisoPath = ".\\dependents\\extract-xiso.exe";
        private string[] extensions = { ".iso", ".xiso" };

        private List<ROM> romList = new List<ROM>();
        private bool romsDetected = false;
        private List<MenuItem> menuItems = new List<MenuItem>();
        private string infoMessage = "";

        public AutoXiso()
        {
            romsDetected = false;
            menuItems.Add(new MenuItem("Detect", "Detects ROMs", false, DetectROMs));
            menuItems.Add(new MenuItem("List", "Lists detected ROMs", true, ListROMs));
            menuItems.Add(new MenuItem("Extract One", "Extracts a single ROM", true, ExtractROM));
            menuItems.Add(new MenuItem("Extract All", "Extracts all detected ROMs", true, ExtractROMs));
            menuItems.Add(new MenuItem("Clear Ext", "Removes any extension on output folders", false, ClearExtensions));
            menuItems.Add(new MenuItem("Exit", "", false, Exit));
        }

        public void Begin()
        {
            //TODO: Implement ini config.
            if (String.IsNullOrEmpty(inputPath))
            {
                Console.WriteLine("Input path is empty!");
                Exit();
            }
            if (String.IsNullOrEmpty(outputPath))
            {
                Console.WriteLine("Output path is empty!");
                Exit();
            }

            //Perform an initial Detect
            DetectROMs();
            Console.Clear();

            do
            {
                PrintHeader();
                PrintMainMenu();
                Console.Write("Select an option: ");
                string userInput = Console.ReadLine()?.ToLower().Trim() ?? "";

                //Remove any brackets the user may have typed
                userInput = userInput.Replace("[", "").Replace("]", "");

                //LINQ to find matching menu item
                var matchingItem = menuItems.FirstOrDefault(item => item.Name.ToLower() == userInput);

                if (matchingItem != null)
                {
                    infoMessage = "";
                    Console.Clear();
                    PrintHeader();
                    matchingItem.PerformAction();
                    AwaitUserInput();
                }
                else
                {
                    infoMessage = "Invalid option. Please try again.";
                }


                Console.Clear();
            }
            while (true);

        }

        /// <summary>
        /// Prints the header.
        /// </summary>
        private void PrintHeader()
        {
            Console.WriteLine("#===========================#");
            Console.WriteLine("#    Auto Xiso Extractor    #");
            Console.WriteLine("#    Christopher Roelle     #");
            Console.WriteLine("#===========================#");
            Console.WriteLine();
            if (romList.Count > 0)
            {
                Console.WriteLine($"Detected ROMs: {romList.Count}");
                Console.WriteLine();
            }
            if (!String.IsNullOrEmpty(infoMessage))
            {
                Console.WriteLine(infoMessage);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Prints all menu items depending on if ROMs have been detected.
        /// </summary>
        private void PrintMainMenu()
        {
            Console.WriteLine("===| MAIN MENU |===");
            
            foreach (MenuItem item in menuItems)
            {
                if (!item.RequiresROMs || item.RequiresROMs && romsDetected)
                {
                    Console.Write($"[{item.Name}]");
                    if (!String.IsNullOrEmpty(item.Descriptor))
                    {
                        Console.Write($" - {item.Descriptor}");
                    }
                    Console.Write("\n");
                }
            }

            Console.WriteLine("\nEnter bracketed text to make a selection.");
        }

        /// <summary>
        /// Detects ROMs located in the input path.
        /// </summary>
        public void DetectROMs()
        {
            Console.WriteLine("===| DETECT ROMS |===");

            //Reset detection
            romsDetected = false;

            if (romList.Count > 0)
            {
                romList.Clear();
            }

            //Check that the input path exists, if not try and generate it.
            if (!Directory.Exists(inputPath))
            {
                Console.WriteLine("Input directory does not exist!");

                //Try to generate the path if it doesnt exist.
                try
                {
                    var path = System.IO.Directory.CreateDirectory(inputPath).FullName;
                    Console.WriteLine($"Input directory has been created:\n{path}");
                }
                catch (Exception e)
                {
                    //Error, return to the menu safely and notify the user.
                    Console.WriteLine(e.ToString());
                    return;
                }
            }

            //Begin detecting
            string[] files = GetFilesByExtension(inputPath, extensions);

            //Check if the input path is empty
            if (files.Length == 0)
            {
                Console.WriteLine("No ROMs detected!");
                return;
            }

            Console.WriteLine("Scanning for ROMs...");
            foreach (string file in files)
            {
                string romName = RemoveAllExtensions(file);
                romList.Add(new ROM(romName, file));
            }

            romsDetected = true;

            //Sort the list by rom name
            romList = romList.OrderBy(rom => rom.Name).ToList();

            Console.WriteLine("\tComplete!");
            Console.WriteLine($"\nDetected {romList.Count} ROMs");
        }

        /// <summary>
        /// Lists all of the roms.
        /// </summary>
        public void ListROMs()
        {
            Console.WriteLine("===| LIST ROMS |===");
            foreach (ROM rom in romList)
            {
                Console.WriteLine($"[{romList.IndexOf(rom)}] - {rom.Name}");
            }

            Console.WriteLine($"\nListed {romList.Count} ROMs");

        }

        /// <summary>
        /// Extracts a single ROM given its index in the romList.
        /// </summary>
        public void ExtractROM()
        {
            if (romList.Count == 0)
            {
                Console.WriteLine("No ROMs detected!");
                return;
            }

            ListROMs();

            Console.WriteLine("\n===| EXTRACT SINGLE ROM |===");
            Console.WriteLine("Use the bracketed number. Type 'Back' to return.");

            int userInput = -1;
            while (true)
            {
                //Prompt for the ROM Index or 'back' to return
                Console.Write("Which ROM would you like to extract: ");
                string rawInput = Console.ReadLine()?.ToLower() ?? "";

                //Check for return command.
                if (rawInput == "back")
                {
                    Console.WriteLine("Returning to Main Menu...");
                    return; //Exit to main menu
                }

                //Parse for index
                if (!int.TryParse(rawInput, out userInput) || userInput < 0 || userInput >= romList.Count)
                {
                    Console.WriteLine("Please enter a valid ROM index!");
                    continue; //Loop again.
                }

                ROM selectedROM = romList[userInput];

                //Verify this is a valid rom index
                if (selectedROM != null)
                {
                    Console.WriteLine($"\nChosen ROM: {selectedROM.Name}");

                    if (ConfirmSelection())
                    {
                        ExtractROMUsingXISOExtractor(selectedROM);
                        return; //Return after extraction
                    }
                    else
                    {
                        Console.WriteLine("Returning to ROM selection...");
                        continue;
                    }
                }
                else
                {
                    //Return to get index prompt
                    Console.WriteLine("Invalid ROM index provided!");
                    continue;
                }
            }

        }

        public void ExtractROMs()
        {
            if (romList.Count == 0)
            {
                Console.WriteLine("No ROMs detected!");
                return;
            }

            Console.WriteLine("\n===| EXTRACT ALL ROMS |===");
            Console.WriteLine($"This will extract {romList.Count} ROMs");

            if (ConfirmSelection())
            {
                foreach (ROM rom in romList)
                {
                    ExtractROMUsingXISOExtractor(rom);
                }
            }
            else
            {
                Console.WriteLine("Returning to Main Menu...");
            }
        }

        /// <summary>
        /// Extracts a given ROM
        /// </summary>
        /// <param name="rom">The ROM we want to extract.</param>
        private void ExtractROMUsingXISOExtractor(ROM rom)
        {
            Console.WriteLine($"\nExtracting: {rom.Name}");
            Console.WriteLine($"Destination: {outputPath}");

            //Make sure XISO Extractor exists
            if (!File.Exists(xisoPath))
            {
                Console.WriteLine("extract-xiso is missing!");
                Console.WriteLine($"Expected here: {xisoPath}");
                return;
            }

            //Create the ROM extract destination path if it doesnt exist
            if (!Directory.Exists(outputPath))
            {
                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception e)
                {
                    //Uh-oh, error occured.
                    Console.WriteLine(e.ToString());
                    return;
                }
            }

            string romPath = Path.GetFullPath(rom.Path);
            string arguments = $"\"{romPath}\"";

            Console.WriteLine("Extracting...");
            InvokeXISOExtractor(xisoPath, arguments);

            return;
        }

        /// <summary>
        /// Exits the application.
        /// </summary>
        public void Exit()
        {
            Console.WriteLine("\nExiting AutoXiso...");
            Environment.Exit(0);
        }

        //Helpers

        /// <summary>
        /// Calls XISO Extractor
        /// </summary>
        /// <param name="appPath">Path to the application</param>
        /// <param name="args">The arguments, provided via the rom</param>
        private void InvokeXISOExtractor(string appPath, string args)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = outputPath
                };

                //Start the process
                Process process = Process.Start(startInfo);

                //Wait for exit
                process.WaitForExit();

                Console.WriteLine("\tComplete!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// Returns file paths based on given extensions.
        /// </summary>
        /// <param name="path">The path to look in.</param>
        /// <param name="extensions">Array of extensions to match on.</param>
        /// <returns></returns>
        private string[] GetFilesByExtension(string path, string[] extensions)
        {
            return Directory.GetFiles(path)
                .Where(file => extensions.Contains(Path.GetExtension(file).ToLower()))
                .ToArray();
        }

        /// <summary>
        /// Awaits user input of a single key.
        /// </summary>
        private void AwaitUserInput()
        {
            Console.Write("\nPress any key to continue...");
            Console.ReadKey();
        }

        /// <summary>
        /// Recursively removes multiple extensions.
        /// </summary>
        /// <param name="filename">The filename to remove extensions from.</param>
        /// <returns></returns>
        private string RemoveAllExtensions(string filename)
        {
            string newName = Path.GetFileNameWithoutExtension(filename);

            //Recursively call this for any further extensions
            if (newName.Contains(".")) { return RemoveAllExtensions(newName); }

            //No further extensions, return the name
            return newName;
        }

        /// <summary>
        /// Asks for confirmation with a (Y/N) answer.
        /// </summary>
        /// <returns></returns>
        private bool ConfirmSelection()
        {
            while (true)
            {
                Console.Write("Is this correct? (Y/N): ");
                string response = Console.ReadLine()?.ToLower() ?? "";

                switch (response)
                {
                    case "y":
                        return true;
                    case "n":
                        return false;
                    default:
                        Console.WriteLine("Please enter a valid response.");
                        break;
                }
            }
        }

        private void ClearExtensions()
        {
            //Create the ROM extract destination path if it doesnt exist
            if (!Directory.Exists(outputPath))
            {
                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception e)
                {
                    //Uh-oh, error occured.
                    Console.WriteLine(e.ToString());
                    return;
                }
            }

            String[] outputFolderPaths = Directory.GetDirectories(outputPath);
            int numChanges = 0;

            Console.WriteLine("\n===| CLEAR OUTPUT FOLDER EXTENSIONS |===");
            Console.WriteLine($"This will remove any extension from the output subdirectories.");

            if (ConfirmSelection())
            {
                foreach (string folderPath in outputFolderPaths)
                {
                    try
                    {
                        string folderName = Path.GetFileName(folderPath);
                        string newFolderName = folderName;

                        //Check for .
                        if (folderName.Contains("."))
                        {
                            //Remove the dot and anything after it
                            newFolderName = folderName.Substring(0, folderName.IndexOf("."));
                        }

                        //Combine the original dir path with the new folder name
                        string newFolderPath = Path.Combine(Path.GetDirectoryName(folderPath), newFolderName);

                        //Check if the folder exists and rename it
                        if (Directory.Exists(folderPath))
                        {
                            Directory.Move(folderPath, newFolderPath);
                            numChanges++;
                            Console.WriteLine($"Folder renamed from '{folderPath}' to '{newFolderPath}'");
                        }
                        else
                        {
                            Console.WriteLine($"The folder '{folderPath}' does not exist.");
                        }


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                Console.WriteLine($"{numChanges} changes made.");
            }
            else
            {
                Console.WriteLine("Returning to Main Menu...");
            }
        }
    }
}
