using Crayon;
using Terminal;
using FAT;
using System;
using System.Data;
using System.Diagnostics;
using static Formatter.CommFormatter;
using static Crayon.Output;
using System.Runtime.InteropServices;
using System.Reflection;

class Program
{
    #region CONSTANTS
    #pragma warning disable CS0162
    const bool TEST_FAT = false; // Poner a verdadero para hacer tests a la fat

    // Cosas para que se abra a pantalla completa, la aplicacion pide permisos de administrador
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetConsoleWindow();
    [DllImport("user32.dll")]
    private static extern int ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_MAXIMIZE = 3;

    // Menu principal
    static public string[] TITLE = 
    {
    "<rainbow> ________ ___  ___       _______           ________  ___       ___       ________  ________  ________  _________  ___  ________  ________           _________  ________  ________  ___       _______      </rainbow>",
    "<rainbow displacement=2>|\\  _____\\\\  \\|\\  \\     |\\  ___ \\         |\\   __  \\|\\  \\     |\\  \\     |\\   __  \\|\\   ____\\|\\   __  \\|\\___   ___\\\\  \\|\\   __  \\|\\   ___  \\        |\\___   ___\\\\   __  \\|\\   __  \\|\\  \\     |\\  ___ \\     </rainbow>",
    "<rainbow displacement=4>\\ \\  \\__/\\ \\  \\ \\  \\    \\ \\   __/|        \\ \\  \\|\\  \\ \\  \\    \\ \\  \\    \\ \\  \\|\\  \\ \\  \\___|\\ \\  \\|\\  \\|___ \\  \\_\\ \\  \\ \\  \\|\\  \\ \\  \\\\ \\  \\       \\|___ \\  \\_\\ \\  \\|\\  \\ \\  \\|\\ /\\ \\  \\    \\ \\   __/|    </rainbow>",
    "<rainbow displacement=6> \\ \\   __\\\\ \\  \\ \\  \\    \\ \\  \\_|/__       \\ \\   __  \\ \\  \\    \\ \\  \\    \\ \\  \\\\\\  \\ \\  \\    \\ \\   __  \\   \\ \\  \\ \\ \\  \\ \\  \\\\\\  \\ \\  \\\\ \\  \\           \\ \\  \\ \\ \\   __  \\ \\   __  \\ \\  \\    \\ \\  \\_|/__  </rainbow>",
    "<rainbow displacement=8>  \\ \\  \\_| \\ \\  \\ \\  \\____\\ \\  \\_|\\ \\       \\ \\  \\ \\  \\ \\  \\____\\ \\  \\____\\ \\  \\\\\\  \\ \\  \\____\\ \\  \\ \\  \\   \\ \\  \\ \\ \\  \\ \\  \\\\\\  \\ \\  \\\\ \\  \\           \\ \\  \\ \\ \\  \\ \\  \\ \\  \\|\\  \\ \\  \\____\\ \\  \\_|\\ \\ </rainbow>",
    "<rainbow displacement=10>   \\ \\__\\   \\ \\__\\ \\_______\\ \\_______\\       \\ \\__\\ \\__\\ \\_______\\ \\_______\\ \\_______\\ \\_______\\ \\__\\ \\__\\   \\ \\__\\ \\ \\__\\ \\_______\\ \\__\\\\ \\__\\           \\ \\__\\ \\ \\__\\ \\__\\ \\_______\\ \\_______\\ \\_______\\</rainbow>",
    "<rainbow displacement=12>    \\|__|    \\|__|\\|_______|\\|_______|        \\|__|\\|__|\\|_______|\\|_______|\\|_______|\\|_______|\\|__|\\|__|    \\|__|  \\|__|\\|_______|\\|__| \\|__|            \\|__|  \\|__|\\|__|\\|_______|\\|_______|\\|_______|</rainbow>"
    };

    public const int TITLE_VERTICAL_OFFSET = 10;

    static public string[] MENU_OPTIONS =
    {
        "START CONSOLE",
        "SHOW FAT METADATA",
        "EXIT"
    };

    public const string MENU_MESSAGE = "SELECT OPTION USING THE ARROW KEYS";

    public const int MENU_HORIZONTAL_OFFSET = 0;
    public const int MENU_VERTICAL_OFFSET = -5;

    public const string MENU_BOTTOM_MESSAGE = "press ENTER to confirm option";
    #endregion

    public static void Main(string[] args)
    {
        /* Este es el "main" de la aplicacion
         * Las clases y librerias se encuentran en 'src' separadas por contexto de utilidad
         * El programa consta de un bucle principal y una funcion que gestiona el input y lo envia al ConsoleManager
         */

        #region SETUP
        // No tocar esta region, importante para que la aplicacion funcione bien
        Console.OutputEncoding = System.Text.Encoding.UTF8; // Cambia el encoding de texto para admitir mas caracteres, ermite que se dibujen los emojis

        // Cosas inicia la aplicacion en pantalla completa
        IntPtr consoleHandle = GetConsoleWindow();
        if (consoleHandle != IntPtr.Zero)
        {
            ShowWindow(consoleHandle, SW_MAXIMIZE);
        }
        #endregion

        if (TEST_FAT) { fatTest(); }
        else { mainMenu(); }
    }

    #region MAIN_METHODS
    static void fatTest()
    {
        Fat testFat = new();

        testFat.addDirectory("holamundo", "C://");
        testFat.addFile("jsjs.txt", "C://holamundo");
        testFat.writeToFile("jsjs.txt", "C://holamundo", "aksdfjslkfjslfkjsflksjflksjdflsdkjfsfjsfl\nsdfksfksjflsjdfsjfslfjsJAJAJAJ");
        testFat.writeToFile("jsjs.txt", "C://holamundo", "hoola", false);
        testFat.writeToFile("jsjs.txt", "C://holamundo", "hoola");
        Console.WriteLine(testFat.catFile("jsjs.txt", "C://holamundo"));
        testFat.copyFile("C://holamundo", "jsjs.txt", "C://", "jjj.bat");
        testFat.addDirectory("hey", "C://holamundo");
        Console.WriteLine(testFat.listDirectory("C://holamundo"));
        Console.WriteLine(testFat.listDirectory("C://"));
        testFat.moveFile("C://holamundo", "jsjs.txt", "C://");
        testFat.showMetadata();

        return;
    }

    static void mainMenu(int selected = 0)
    {
        Console.Clear();
        Console.CursorVisible = false;
        Console.SetCursorPosition(Console.CursorLeft, Console.WindowTop + TITLE_VERTICAL_OFFSET);

        foreach (string s in TITLE)
        {
            print(format(s), true);
        }

        PrintMenu(MENU_OPTIONS, selected, MENU_MESSAGE, MENU_HORIZONTAL_OFFSET, MENU_VERTICAL_OFFSET, MENU_BOTTOM_MESSAGE); // Para cambiar lo que muestra el menu hacerlo desde la region 'CONSTANTS'

        ConsoleKey keyPress;

        do
        {
            keyPress = System.Console.ReadKey(true).Key;

            if(keyPress == ConsoleKey.DownArrow)
            {
                if (selected+1 < MENU_OPTIONS.Length) selected++;
                PrintMenu(MENU_OPTIONS, selected, MENU_MESSAGE, MENU_HORIZONTAL_OFFSET, MENU_VERTICAL_OFFSET, MENU_BOTTOM_MESSAGE);
            }
            else if (keyPress == ConsoleKey.UpArrow)
            {
                if (selected-1 >= 0) selected--;
                PrintMenu(MENU_OPTIONS, selected, MENU_MESSAGE, MENU_HORIZONTAL_OFFSET, MENU_VERTICAL_OFFSET, MENU_BOTTOM_MESSAGE);
            }
        }
        while (keyPress != ConsoleKey.Enter);

        switch(selected)
        {
            default: break;
        }

        Console.Clear();
    }

    static void consoleEnvironment()
    {
        Console.CursorVisible = true;

        // CONSOLE ENVIRONMENT
        string? command; // String para guardar el comando que el usuario escriba
        ConsoleManager cM = new(); // Clase para gestionar los comandos
        Fat fat = new(); // Memoria FAT

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
            else Console.Write(Bold().Red().Text("> ") + Reversed().Text(options[i]));
        }

        Console.SetCursorPosition(((Console.WindowWidth - bottomMessage.Length)/2) + hOffset, Console.CursorTop + 4);
        Console.Write(Dim().Text(bottomMessage));
    }
    #endregion
}