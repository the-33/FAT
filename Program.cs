using Crayon;
using Terminal;
using FAT;
using FAT.Data;
using FAT.MetaData;
using System;
using System.Data;
using System.Diagnostics;
using System.Windows;
using static Formatter.CommFormatter;
using static Crayon.Output;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Drawing;
using static Program;
using System.Management;
using System.Text.Json;

class Program
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
    #endregion

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
        Fat fat = new(CLUSTER_SIZE, ""); // Memoria FAT
        #endregion

        if (TEST_FAT) { fatTest(fat); }
        else { mainMenu(fat); }
    }

    #region MAIN_METHODS
    static void fatTest(Fat testFat)
    {
        testFat = loadFat("../../../tests/", "test");

        //testFat.addDirectory("peliculas", "C://");
        //testFat.addFile("shrek.mp4", "C://peliculas/");
        //testFat.moveFile("C://peliculas/", "shrek.mp4", "C://");
        //testFat.addDirectory("peliculas de animacion", "C://peliculas/");
        //testFat.removeFile("shrek.mp4", "C://");
        //testFat.addFile("el padrino.mp4", "C://peliculas/peliculas de animacion/");
        //testFat.moveFile("C://peliculas/peliculas de animacion/", "el padrino.mp4", "C://peliculas");
        //testFat.addDirectory("peliculas buenas", "C://");
        //testFat.moveDirectory("C://", "peliculas buenas", "C://peliculas/");
        //testFat.copyFile("C://peliculas/", "el padrino.mp4", "C://peliculas/peliculas buenas/");

        //saveFat(testFat, "../../../tests/", "test");
        return;
    }

    static void mainMenu(Fat fat, int selected = 0)
    {
        Console.Clear();
        Console.CursorVisible = false;
        Console.SetCursorPosition(Console.CursorLeft, Console.WindowTop + TITLE_VERTICAL_OFFSET);

        foreach (string s in TITLE)
        {
            print(format(s), true);
        }

        PrintMenu(MENU_OPTIONS, selected, MENU_MESSAGE, MENU_HORIZONTAL_OFFSET, MENU_VERTICAL_OFFSET, MENU_BOTTOM_MESSAGE); // Para cambiar lo que muestra el menu hacerlo desde la region 'CONSTANTS'

        ConsoleKey? keyPress = null;
        int mousePress = 0;

        bool enableMouseInteraction = true;
        bool exit = false;

        do
        {
            if(enableMouseInteraction)
            {
                Console.SetCursorPosition(Console.WindowLeft, Console.WindowTop);
                GetCursorPos(out POINT point);
                Console.Write(Dim().Text($"Mouse Position: X={point.X}, Y={point.Y}          "));
                mousePress = (GetAsyncKeyState(VK_LBUTTON) & 0x8000);
            }

            if (System.Console.KeyAvailable) keyPress = System.Console.ReadKey(true).Key;

            if(keyPress == ConsoleKey.DownArrow)
            {
                if (selected+1 < MENU_OPTIONS.Length)
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

        switch(selected)
        {
            case 0: consoleEnvironment(fat); break;
            case 1: showFatMetadata(fat); break;
            case 2: saveFat(fat, "", ""); break;
            default: Console.Clear(); Environment.Exit(0);  break;
        }
        Console.Clear();
    }

    static void consoleEnvironment(Fat fat)
    {
        Console.Clear();
        Console.CursorVisible = true;

        // CONSOLE ENVIRONMENT
        string? command; // String para guardar el comando que el usuario escriba
        ConsoleManager cM = new(COMMAND_DESCRIPTION_PATH, COMMAND_DESCRIPTION_EXTENSION); // Clase para gestionar los comandos

        string? workingDirectory = "C:/", userName, computerName;

        using (Process process = new Process()) // Obtiene el nombre de usuario y el nombre del equipo
        {
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = @"/c whoami";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            StreamReader reader = process.StandardOutput;
            string output = reader.ReadToEnd();

            computerName = output.Split('\\')[0];
            userName = output.Split('\\')[1].Substring(0, output.Split('\\')[1].Length - 2);
        }

        while (true) // Bucle principal
        {
            //TODO: agregar colores
            Console.WriteLine(Red().Text("┌─[") + Green().Text(userName) + Yellow().Text("@") + Rgb(43, 91, 156).Text(computerName) + Red().Text("]─[") + Rgb(52, 117, 27).Text(workingDirectory) + Red().Text("]"));
            Console.Write(Red().Text("└───■") + Yellow().Text(" $ "));

            command = Console.ReadLine();
            if (command != null && command != "") CommandManager(command, cM, fat);
        }
    }
    #endregion

    static void showFatMetadata(Fat fat)
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
        mainMenu(fat);
    }

    #region AUXILIARY METHODS
    static void CommandManager(string s, ConsoleManager cM, Fat fat)
    {
        //Un comando tendra el formato "COMANDO ARGUMENTOS FLAGS donde PARAMETROS sera una lista de parametros variable del tipo -FLAG ARGUMENTOS"
        string command = s.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        string?[] args = { "" };
        if (s.Length > command.Length)
        {
            args = (s.Substring(command.Length)).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }
        cM.execute(command, args, fat);
    }

    static void PrintMenu(string[] options, int selected, string message, int hOffset, int vOffset, string bottomMessage)
    {
        if(selected < 0 || selected >= options.Length)
        {
            Console.WriteLine(Red().Bold().Text("Cannot select an option that does not exist, selection will be set to default value (0)"));
            selected = 0;
        }

        int height = 5 + options.Length;
        int width = 0;
        foreach (string s in options) if(width < s.Length) width = s.Length;
        width += 8;
        if(width < message.Length) width = message.Length+10;

        int leftStartingPoint = ((Console.WindowWidth - width) / 2) + hOffset;
        Console.SetCursorPosition(leftStartingPoint, ((Console.WindowHeight - height) / 2) + vOffset);

        for (int i = -1; i<height; i++)
        {
            string line = "";
            for (int j = 0; j<width; j++)
            {
                if (i == -1)
                {
                    if (j == 2) line += "┌";
                    else if ( j == message.Length + 3) line += "┐";
                    else if(j > 2 && j<message.Length + 3) line += '─';
                    else line += ' ';
                }
                else if (i == 0)
                {
                    if (j == 0) line += '╔';
                    else if (j == 2) { line += "│" + Bold().Text(message) + "│"; j += message.Length + 1; }
                    else if (j == width - 1) line += '╗';
                    else line += '═';
                }
                else if(i == 1)
                {
                    if (j == 0 || j == width - 1) line += '║';
                    else if (j == 2) line += "└";
                    else if (j == message.Length + 3) line += "┘";
                    else if (j > 2 && j < message.Length + 3) line += '─';
                    else line += ' ';
                }
                else if (i == height-1)
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
            Console.SetCursorPosition(leftStartingPoint, Console.CursorTop+1);
        }

        Console.SetCursorPosition(Console.CursorLeft, ((Console.WindowHeight - height) / 2) + vOffset + 3);

        for (int i = 0; i<options.Length; i++)
        {
            Console.SetCursorPosition(((Console.WindowWidth - (width-6)) / 2) + hOffset, Console.CursorTop + 1);
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
                            string[] rgbStrings = color.Split( ',', ' ', StringSplitOptions.RemoveEmptyEntries);
                            int[] rgb = new int[] { int.Parse(rgbStrings[0]), int.Parse(rgbStrings[1]), int.Parse(rgbStrings[2]) };
                            Console.Write(Rgb((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]).Bold().Text("> ") + Reversed().Text(options[i]));
                        }
                        break;
                }
            }
        }

        Console.SetCursorPosition(((Console.WindowWidth - bottomMessage.Length)/2) + hOffset, Console.CursorTop + 4);
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

                switch(configuration[0])
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
            if(MENU_TITLE_FORMAT == "rainbow")
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

    static void saveFat(Fat fat, string path, string fileName)
    {
        Console.WriteLine();
        var options = new JsonSerializerOptions { WriteIndented = true };
        string jsonString = JsonSerializer.Serialize(fat, options);

        System.IO.File.WriteAllText(path + fileName + ".json", jsonString);
    }

    static Fat loadFat(string path, string fileName)
    {
        string jsonString = System.IO.File.ReadAllText(path + fileName + ".json");
        return JsonSerializer.Deserialize<Fat>(jsonString);
    }
    #endregion
}