﻿using Terminal;
using FAT;
using System.Diagnostics;
using System.Windows;
using static Formatter.CommFormatter;
using static Crayon.Output;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json.Linq;

public static class Program
{
    #region CONSTANTS

    /*
     * Estas son las constantes que modifican los parametros del programa
     * Aqui estan los valores por defecto, para cambiar estos valores ir a la carpeta 'conf'
     */

    private const string CONF_PATH = "../../../conf/"; // Ubicacion de la carpeta de configuraciones

    // General
#pragma warning disable CS0162
    private static bool TEST_FAT = false; // Para testear la FAT
    // FAT
    private static int CLUSTER_SIZE = 1024;
    private static string FAT_COPY_PATH = "";
    // Console Manager
    private static string COMMAND_DESCRIPTION_PATH = "../../../commands/"; // Carpeta donde estan los archivos de descripcion de los comandos
    private static string COMMAND_DESCRIPTION_EXTENSION = ".comm"; // Extension de archivo de descripcion de los comandos

    // Cosas para que se abra a pantalla completa, la aplicacion pide permisos de administrador NO SE PUEDEN CAMBIAR
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")]
    private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_MAXIMIZE = 3;

    // Menu principal
    private static string[] TITLE = Array.Empty<string>();
    private static int TITLE_VERTICAL_OFFSET = 0;
    private static int TITLE_RAINBOW_DISPLACEMENT_INCREMENT = 0;
    private static string[] MENU_OPTIONS = Array.Empty<string>();
    private static string MENU_MESSAGE = "";
    private static int MENU_HORIZONTAL_OFFSET = 0;
    private static int MENU_VERTICAL_OFFSET = 0;
    private static string MENU_BOTTOM_MESSAGE = "";
    private static string MENU_SELECTION_ARROW_COLOR = "WHITE";
    private static string MENU_TITLE_FORMAT = "white";

    // Trackear el raton
    [DllImport("user32.dll")]
    public static extern bool GetCursorPos(out POINT lpPoint);

    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    const int VK_LBUTTON = 0x01;

    // Desactivar el modo quick edit
    [DllImport("kernel32.dll")]
    static extern IntPtr GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll")]
    static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

    [DllImport("kernel32.dll")]
    static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

    const int STD_INPUT_HANDLE = -10;
    const uint ENABLE_QUICK_EDIT_MODE = 0x0040;
    const uint ENABLE_EXTENDED_FLAGS = 0x0080;

    // Desactivar los botones
    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

    [DllImport("user32.dll")]
    private static extern bool DeleteMenu(IntPtr hMenu, uint uPosition, uint uFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleDisplayMode(IntPtr hConsoleOutput, uint dwFlags, out COORD lpNewScreenBufferDimensions);

    private const int STD_OUTPUT_HANDLE = -11;
    private const uint SC_CLOSE = 0xF060;
    private const uint MF_BYCOMMAND = 0x00000000;
    private const uint SC_MINIMIZE = 0xF020;
    private const uint SC_MAXIMIZE = 0xF030;
    private const uint CONSOLE_FULLSCREEN_MODE = 1;

    [StructLayout(LayoutKind.Sequential)]
    public struct COORD
    {
        public short X;
        public short Y;
    }

    // Previous font to restore
    static string? previousFont = null;
    #endregion

    public static Fat fat { get; set; } = new(CLUSTER_SIZE);
    public static ConsoleManager cM = new(COMMAND_DESCRIPTION_PATH, COMMAND_DESCRIPTION_EXTENSION); // Clase para gestionar los comandos

    [STAThread]
    public static void Main(string[] args)
    {
        /* Este es el "main" de la aplicacion
         * Las clases y librerias se encuentran en 'src' separadas por contexto de utilidad
         * El programa consta de un bucle principal y una funcion que gestiona el input y lo envia al ConsoleManager
         */

        #region SETUP
        // No tocar esta region, importante para que la aplicacion funcione bien

        Console.OutputEncoding = System.Text.Encoding.UTF8; // Cambia el encoding de texto para admitir mas caracteres, permite que se dibujen los emojis

        // Iniciar la aplicacion en pantalla completa
        IntPtr consoleHandle = GetConsoleWindow();
        if (consoleHandle != IntPtr.Zero)
        {
            ShowWindow(consoleHandle, SW_MAXIMIZE);
        }

        // Desactivar los botones de cerrar, minimizar y poner en ventana
        IntPtr consoleWindow = GetConsoleWindow();
        IntPtr systemMenu = GetSystemMenu(consoleWindow, false);
        DeleteMenu(systemMenu, SC_CLOSE, MF_BYCOMMAND);
        DeleteMenu(systemMenu, SC_MINIMIZE, MF_BYCOMMAND);
        //DeleteMenu(systemMenu, SC_MAXIMIZE, MF_BYCOMMAND);

        // Desactivar el modo quick edit
        IntPtr handle = GetStdHandle(STD_INPUT_HANDLE);
        uint mode;
        if (GetConsoleMode(handle, out mode))
        {
            mode &= ~ENABLE_QUICK_EDIT_MODE;
            mode |= ENABLE_EXTENDED_FLAGS;
            SetConsoleMode(handle, mode);
        }

        loadConfiguration();
        fat = new(CLUSTER_SIZE); // Memoria FAT
        cM = new(COMMAND_DESCRIPTION_PATH, COMMAND_DESCRIPTION_EXTENSION);
        #endregion

        if (TEST_FAT) { fatTest(); }
        else { mainMenu(); }
    }

    #region MAIN_METHODS
    static void fatTest()
    {
        fat.showMetadata();
        fat.addDirectory("peliculasNoVistas", "C:/");
        fat.addFile("exmachina.mp4", "C:/peliculasNoVistas");
        fat.showMetadata();
        fat.addDirectory("peliculasVistas", "C:/");
        fat.moveFile("C:/peliculasNoVistas", "exmachina.mp4", "C:/peliculasvistas");
        fat.showMetadata();
        fat.removeDirectory("C:/", "peliculasVistas");
        fat.showMetadata();
        //listar procesos en ejecucion
        fat.addDirectory("tmp", "C:/");
        fat.addFile("gattaca.mp4", "C:/tmp");
        fat.addFile("memento.mp4", "C:/tmp");
        //esperar un minuto
        Console.WriteLine(fat.listDirectory("C:/tmp"));
        //lanzar proceso borratmpcada5s
        //listar procesos
        //esperar 5 segundos
        Console.WriteLine(fat.listDirectory("C:/tmp"));
    }

    static void mainMenu(int selected = 0)
    {
        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
        };

        Console.Clear();
        Console.CursorVisible = false;
        Console.SetCursorPosition(Console.CursorLeft, Console.WindowTop + TITLE_VERTICAL_OFFSET);

        while (System.Console.KeyAvailable) System.Console.ReadKey(true);

        foreach (string s in TITLE)
        {
            print(format(s) + "\n", true);
        }

        PrintMenu(MENU_OPTIONS, selected, MENU_MESSAGE, MENU_HORIZONTAL_OFFSET, MENU_VERTICAL_OFFSET, MENU_BOTTOM_MESSAGE); // Para cambiar lo que muestra el menu hacerlo desde la region 'CONSTANTS'

        ConsoleKey? keyPress = null;
        int mousePress = 0;

        bool enableMouseInteraction = true;
        bool exit = false;

        do
        {
            if (enableMouseInteraction)
            {
                Console.SetCursorPosition(Console.WindowLeft, Console.WindowTop);
                GetCursorPos(out POINT point);
                Console.Write(Dim().Text($"Mouse Position: X={point.X}, Y={point.Y}          "));
                mousePress = (GetAsyncKeyState(VK_LBUTTON) & 0x8000);
            }

            if (System.Console.KeyAvailable) keyPress = System.Console.ReadKey(true).Key;

            if (keyPress == ConsoleKey.DownArrow)
            {
                if (selected + 1 < MENU_OPTIONS.Length)
                {
                    selected++;
                }
                keyPress = null;
                PrintMenu(MENU_OPTIONS, selected, MENU_MESSAGE, MENU_HORIZONTAL_OFFSET, MENU_VERTICAL_OFFSET, MENU_BOTTOM_MESSAGE);
            }
            else if (keyPress == ConsoleKey.UpArrow)
            {
                if (selected - 1 >= 0)
                {
                    selected--;
                }
                keyPress = null;
                PrintMenu(MENU_OPTIONS, selected, MENU_MESSAGE, MENU_HORIZONTAL_OFFSET, MENU_VERTICAL_OFFSET, MENU_BOTTOM_MESSAGE);
            }
            else if (keyPress == ConsoleKey.Enter) exit = true;
        }
        while (!exit);

        switch (selected)
        {
            case 0: consoleEnvironment(); break;
            case 1: showFatMetadata(); break;
            case 2: newFat(); break;
            case 3: saveFat(); break;
            case 4: loadFat(); break;
            default: exitProgram(selected); break;
        }
    }

    static void consoleEnvironment()
    {
        Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
        };

        Console.Clear();
        Console.CursorVisible = true;

        // CONSOLE ENVIRONMENT
        cM.exit = false;
        string? command; // String para guardar el comando que el usuario escriba
        bool exit = false;

        while (!exit) // Bucle principal
        {
            //TODO: agregar colores
            Console.WriteLine(Red().Text("┌─[") + Green().Text(cM.envVars["USER"]) + Yellow().Text("@") + Rgb(43, 91, 156).Text(cM.envVars["HOSTNAME"]) + Red().Text("]─[") + Rgb(52, 117, 27).Text(cM.envVars["PWD"]) + Red().Text("]"));
            Console.Write(Red().Text("└───■") + Yellow().Text(" $ "));

            if (!exit)
            {
                command = Console.ReadLine();
                if (!exit && command != null && command != "") cM.execute(command, cM.envVars["PWD"]);
                while (System.Console.KeyAvailable) System.Console.ReadKey(true);

                Console.WriteLine();

                exit = cM.exit;
            }
        }

        Console.Clear();
        mainMenu();
    }

    static void showFatMetadata()
    {
        Console.Clear();
        fat.showMetadata();
        Console.WriteLine("Pulsa enter para volver...");

        ConsoleKey? keyPress = null;
        do
        {
            if (System.Console.KeyAvailable) keyPress = System.Console.ReadKey(true).Key;
        }
        while (keyPress != ConsoleKey.Enter);
        Console.Clear();
        mainMenu(1);
    }

    static void newFat()
    {
        DialogResult result = System.Windows.Forms.MessageBox.Show(
            "The current fat state will be overwritten. Do you want to continue?",
            "WARNING",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (result == DialogResult.Yes) fat = new(CLUSTER_SIZE);
        mainMenu(2);
    }

    static void saveFat()
    {
        string path = askForFile();

        if (path != "")
        {
            if (File.Exists(path))
            {
                while (System.Console.KeyAvailable) System.Console.ReadKey(true);
                DialogResult result = System.Windows.Forms.MessageBox.Show(
                    $"The file {path} already exists. Do you want to overwrite it?",
                    "WARNING",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result == DialogResult.No)
                {
                    mainMenu(3);
                }
            }

            Fat.Metadata m = fat.metadata;
            m.fatCopyPath = path + "cpy";
            fat.metadata = m;

            fat.metadata.bootCode.recalculateMagicNumber(fat);

            string jsonString = JsonSerializer.Serialize(fat, new JsonSerializerOptions { WriteIndented = true });

            System.IO.File.WriteAllText(path, jsonString);

            if (File.Exists(path + "cpy"))
            {
                File.SetAttributes(
                   path + "cpy",
                   FileAttributes.Normal
                );
            }

            jsonString = JsonSerializer.Serialize(fat, new JsonSerializerOptions { WriteIndented = false });
            System.IO.File.WriteAllText(path + "cpy", Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString)));
            File.SetAttributes(
               path + "cpy",
               FileAttributes.Archive |
               FileAttributes.Hidden |
               FileAttributes.ReadOnly
            );
        }

        mainMenu(3);
    }

    static void loadFat()
    {
        string path = askForFile(true);

        if (path != "")
        {
            while (System.Console.KeyAvailable) System.Console.ReadKey(true);
            DialogResult result = System.Windows.Forms.MessageBox.Show(
                "The current fat state will be overwritten. Do you want to continue?",
                "WARNING",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result == DialogResult.Yes)
            {
                string jsonString = System.IO.File.ReadAllText(path);
                Fat? loadedFat;
                try
                {
                    loadedFat = JsonSerializer.Deserialize<Fat>(jsonString);
                }
                catch (Exception e)
                {
                    System.Windows.Forms.MessageBox.Show(
                        e.Message,
                        "EXCEPTION",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    loadedFat = null;
                }

                if (loadedFat != null && loadedFat.metadata.bootCode.boot(loadedFat))
                {
                    fat = loadedFat;

                    jsonString = JsonSerializer.Serialize(fat, new JsonSerializerOptions { WriteIndented = true });

                    if (File.Exists(path + "cpy"))
                    {
                        File.SetAttributes(
                           path + "cpy",
                           FileAttributes.Normal
                        );
                    }

                    jsonString = JsonSerializer.Serialize(fat, new JsonSerializerOptions { WriteIndented = false });
                    System.IO.File.WriteAllText(path + "cpy", Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString)));
                    File.SetAttributes(
                       path + "cpy",
                       FileAttributes.Archive |
                       FileAttributes.Hidden |
                       FileAttributes.ReadOnly
                    );
                }
                else
                {
                    result = System.Windows.Forms.MessageBox.Show(
                        $"Could not load {path} , the file is corrupted or not formatted properly.\nPress OK to recover the data from the fat copy",
                        "ERROR",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Error
                    );

                    if (result == DialogResult.OK)
                    {
                        try
                        {
                            jsonString = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(System.IO.File.ReadAllText(path + "cpy")));
                            loadedFat = JsonSerializer.Deserialize<Fat>(jsonString);
                        }
                        catch (Exception e)
                        {
                            System.Windows.Forms.MessageBox.Show(
                                e.Message,
                                "EXCEPTION",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            loadedFat = null;
                        }

                        if (loadedFat != null && loadedFat.metadata.bootCode.boot(loadedFat))
                        {
                            fat = loadedFat;

                            jsonString = JsonSerializer.Serialize(fat, new JsonSerializerOptions { WriteIndented = true });

                            System.IO.File.WriteAllText(path, jsonString);

                            if (File.Exists(path + "cpy"))
                            {
                                File.SetAttributes(
                                   path + "cpy",
                                   FileAttributes.Normal
                                );
                            }

                            jsonString = JsonSerializer.Serialize(fat, new JsonSerializerOptions { WriteIndented = false });
                            System.IO.File.WriteAllText(path + "cpy", Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonString)));
                            File.SetAttributes(
                               path + "cpy",
                               FileAttributes.Archive |
                               FileAttributes.Hidden |
                               FileAttributes.ReadOnly
                            );
                        }
                        else
                        {
                            System.Windows.Forms.MessageBox.Show(
                                $"Could not recover the data from the fat copy.",
                                "ERROR",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }
                }
            }
        }

        mainMenu(4);
    }

    static void exitProgram(int prevSelected)
    {
        DialogResult result = System.Windows.Forms.MessageBox.Show(
            "Are you sure you want to exit?\nAll unsaved data will be lost.",
            "WARNING",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        );

        if (result == DialogResult.Yes)
        {
            Console.Clear();
            Console.WriteLine(Dim().Text("Exiting..."));
            Environment.Exit(0);
        }

        mainMenu(prevSelected);
    }
    #endregion

    #region AUXILIARY METHODS
    static void PrintMenu(string[] options, int selected, string message, int hOffset, int vOffset, string bottomMessage)
    {
        if (selected < 0 || selected >= options.Length)
        {
            selected = 0;
        }

        int height = 5 + options.Length;
        int width = options.Max(x => x.Length);
        width += 8;
        if (width < message.Length) width = message.Length + 10;

        int leftStartingPoint = ((Console.WindowWidth - width) / 2) + hOffset;
        Console.SetCursorPosition(leftStartingPoint, ((Console.WindowHeight - height) / 2) + vOffset);

        for (int i = -1; i < height; i++)
        {
            string line = "";
            for (int j = 0; j < width; j++)
            {
                if (i == -1)
                {
                    if (j == 2) line += "┌";
                    else if (j == message.Length + 3) line += "┐";
                    else if (j > 2 && j < message.Length + 3) line += '─';
                    else line += ' ';
                }
                else if (i == 0)
                {
                    if (j == 0) line += '╔';
                    else if (j == 2) { line += "│" + Bold().Text(message) + "│"; j += message.Length + 1; }
                    else if (j == width - 1) line += '╗';
                    else line += '═';
                }
                else if (i == 1)
                {
                    if (j == 0 || j == width - 1) line += '║';
                    else if (j == 2) line += "└";
                    else if (j == message.Length + 3) line += "┘";
                    else if (j > 2 && j < message.Length + 3) line += '─';
                    else line += ' ';
                }
                else if (i == height - 1)
                {
                    if (j == 0) line += '╚';
                    else if (j == width - 1) line += '╝';
                    else line += '═';
                }
                else
                {
                    if (j == 0 || j == width - 1) line += '║';
                    else line += ' ';
                }
            }
            Console.Write(line);
            Console.SetCursorPosition(leftStartingPoint, Console.CursorTop + 1);
        }

        Console.SetCursorPosition(Console.CursorLeft, ((Console.WindowHeight - height) / 2) + vOffset + 3);

        for (int i = 0; i < options.Length; i++)
        {
            Console.SetCursorPosition(((Console.WindowWidth - (width - 6)) / 2) + hOffset, Console.CursorTop + 1);
            if (i != selected) Console.Write("  " + options[i]);
            else
            {
                string color = MENU_SELECTION_ARROW_COLOR;
                switch (color)
                {
                    case "BLACK": Console.Write(Black().Bold().Text("> ") + Reversed().Text(options[i])); break;
                    case "RED": Console.Write(Red().Bold().Text("> ") + Reversed().Text(options[i])); break;
                    case "GREEN": Console.Write(Green().Bold().Text("> ") + Reversed().Text(options[i])); break;
                    case "YELLOW": Console.Write(Yellow().Bold().Text("> ") + Reversed().Text(options[i])); break;
                    case "BLUE": Console.Write(Blue().Bold().Text("> ") + Reversed().Text(options[i])); break;
                    case "MAGENTA": Console.Write(Magenta().Bold().Text("> ") + Reversed().Text(options[i])); break;
                    case "CYAN": Console.Write(Cyan().Bold().Text("> ") + Reversed().Text(options[i])); break;
                    case "WHITE": Console.Write(White().Bold().Text("> ") + Reversed().Text(options[i])); break;
                    default:
                        if (color.StartsWith("RGB("))
                        {
                            color = color.Replace("RGB(", "").Replace(")", "");
                            string[] rgbStrings = color.Split(',', ' ', StringSplitOptions.RemoveEmptyEntries);
                            int[] rgb = new int[] { int.Parse(rgbStrings[0]), int.Parse(rgbStrings[1]), int.Parse(rgbStrings[2]) };
                            Console.Write(Rgb((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]).Bold().Text("> ") + Reversed().Text(options[i]));
                        }
                        break;
                }
            }
        }

        Console.SetCursorPosition(((Console.WindowWidth - bottomMessage.Length) / 2) + hOffset, Console.CursorTop + 4);
        Console.Write(Dim().Text(bottomMessage));
    }

    static void loadConfiguration()
    {
        // General
        using (StreamReader sr = new StreamReader(CONF_PATH + "GENERAL"))
        {
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] configuration = line.Split(" : ");

                switch (configuration[0])
                {
                    case "TEST_FAT":
                        TEST_FAT = (configuration[1] == "true");
                        break;
                    case "CLUSTER_SIZE":
                        CLUSTER_SIZE = int.Parse(configuration[1]);
                        break;
                    case "FAT_COPY_PATH":
                        FAT_COPY_PATH = configuration[1];
                        break;
                    case "COMMAND_DESCRIPTION_PATH":
                        COMMAND_DESCRIPTION_PATH = configuration[1];
                        break;
                    case "COMMAND_DESCRIPTION_EXTENSION":
                        COMMAND_DESCRIPTION_EXTENSION = configuration[1];
                        break;
                }
            }
            sr.Close();
        }

        // Main menu general
        using (StreamReader sr = new StreamReader(CONF_PATH + "MAIN_MENU_GENERAL"))
        {
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                string[] configuration = line.Split(" : ");

                switch (configuration[0])
                {
                    case "TITLE_VERTICAL_OFFSET":
                        TITLE_VERTICAL_OFFSET = int.Parse(configuration[1]);
                        break;
                    case "MENU_MESSAGE":
                        MENU_MESSAGE = configuration[1];
                        break;
                    case "MENU_HORIZONTAL_OFFSET":
                        MENU_HORIZONTAL_OFFSET = int.Parse(configuration[1]);
                        break;
                    case "MENU_VERTICAL_OFFSET":
                        MENU_VERTICAL_OFFSET = int.Parse(configuration[1]);
                        break;
                    case "MENU_BOTTOM_MESSAGE":
                        MENU_BOTTOM_MESSAGE = configuration[1];
                        break;
                    case "TITLE_RAINBOW_DISPLACEMENT_INCREMENT":
                        TITLE_RAINBOW_DISPLACEMENT_INCREMENT = int.Parse(configuration[1]);
                        break;
                    case "MENU_SELECTION_ARROW_COLOR":
                        MENU_SELECTION_ARROW_COLOR = configuration[1];
                        break;
                    case "MENU_TITLE_FORMAT":
                        MENU_TITLE_FORMAT = configuration[1];
                        break;

                }
            }
            sr.Close();
        }

        // Main menu title
        using (StreamReader sr = new StreamReader(CONF_PATH + "MAIN_MENU_TITLE"))
        {
            string? line;
            List<string> title = new();
            if (MENU_TITLE_FORMAT == "rainbow")
            {
                int displacement = 0;
                while ((line = sr.ReadLine()) != null)
                {
                    title.Add("<rainbow displacement=" + displacement + ">" + line + "</rainbow>");
                    displacement += TITLE_RAINBOW_DISPLACEMENT_INCREMENT;
                }
            }
            else
            {
                while ((line = sr.ReadLine()) != null)
                {
                    title.Add("<" + MENU_TITLE_FORMAT + ">" + line + "</" + MENU_TITLE_FORMAT + ">");
                }
            }

            TITLE = title.ToArray();
            sr.Close();
        }

        // Main menu options
        using (StreamReader sr = new StreamReader(CONF_PATH + "MAIN_MENU_OPTIONS"))
        {
            string? line;
            List<string> options = new();
            while ((line = sr.ReadLine()) != null)
            {
                options.Add(line);
            }
            MENU_OPTIONS = options.ToArray();
            sr.Close();
        }
    }

    private static string askForFile(bool fileShouldExist = false)
    {
        System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog
        {
            Filter = "FAT files (*.fat)|*.fat",
            Title = "Select a FAT File",
            Multiselect = false,
            InitialDirectory = "../../../saveData/fats/",
            RestoreDirectory = true,
            CheckFileExists = fileShouldExist,
        };

        string filePath = "";
        DialogResult result = openFileDialog.ShowDialog();

        if (result == DialogResult.OK)
        {
            filePath = openFileDialog.FileName;
            return filePath;
        }

        return filePath;
    }
    #endregion
}