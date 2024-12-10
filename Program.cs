﻿using Crayon;
using Terminal;
using FAT;
using System;
using System.Data;
using System.Diagnostics;
using static Formatter.CommFormatter;
using static Crayon.Output;

#pragma warning disable CS0162
const bool TEST_FAT = false; //SET TO TRUE FOR TESTING THE FAT

Console.OutputEncoding = System.Text.Encoding.UTF8;

/* Este es el "main" de la aplicacion
 * Las clases y librerias se encuentran en 'src' separadas por contexto de utilidad
 * El programa consta de un bucle principal y una funcion que gestiona el input y lo envia al ConsoleManager
 */

#region FAT_TEST
//FAT TEST
if (TEST_FAT)
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
#endregion

#region MAIN_MENU
//MAIN MENU

print(format("<rainbow> ________ ___  ___       _______           ________  ___       ___       ________  ________  ________  _________  ___  ________  ________           _________  ________  ________  ___       _______      </rainbow>"));
print(format("<rainbow displacement=2>|\\  _____\\\\  \\|\\  \\     |\\  ___ \\         |\\   __  \\|\\  \\     |\\  \\     |\\   __  \\|\\   ____\\|\\   __  \\|\\___   ___\\\\  \\|\\   __  \\|\\   ___  \\        |\\___   ___\\\\   __  \\|\\   __  \\|\\  \\     |\\  ___ \\     </rainbow>"));
print(format("<rainbow displacement=4>\\ \\  \\__/\\ \\  \\ \\  \\    \\ \\   __/|        \\ \\  \\|\\  \\ \\  \\    \\ \\  \\    \\ \\  \\|\\  \\ \\  \\___|\\ \\  \\|\\  \\|___ \\  \\_\\ \\  \\ \\  \\|\\  \\ \\  \\\\ \\  \\       \\|___ \\  \\_\\ \\  \\|\\  \\ \\  \\|\\ /\\ \\  \\    \\ \\   __/|    </rainbow>"));
print(format("<rainbow displacement=6> \\ \\   __\\\\ \\  \\ \\  \\    \\ \\  \\_|/__       \\ \\   __  \\ \\  \\    \\ \\  \\    \\ \\  \\\\\\  \\ \\  \\    \\ \\   __  \\   \\ \\  \\ \\ \\  \\ \\  \\\\\\  \\ \\  \\\\ \\  \\           \\ \\  \\ \\ \\   __  \\ \\   __  \\ \\  \\    \\ \\  \\_|/__  </rainbow>"));
print(format("<rainbow displacement=8>  \\ \\  \\_| \\ \\  \\ \\  \\____\\ \\  \\_|\\ \\       \\ \\  \\ \\  \\ \\  \\____\\ \\  \\____\\ \\  \\\\\\  \\ \\  \\____\\ \\  \\ \\  \\   \\ \\  \\ \\ \\  \\ \\  \\\\\\  \\ \\  \\\\ \\  \\           \\ \\  \\ \\ \\  \\ \\  \\ \\  \\|\\  \\ \\  \\____\\ \\  \\_|\\ \\ </rainbow>"));
print(format("<rainbow displacement=10>   \\ \\__\\   \\ \\__\\ \\_______\\ \\_______\\       \\ \\__\\ \\__\\ \\_______\\ \\_______\\ \\_______\\ \\_______\\ \\__\\ \\__\\   \\ \\__\\ \\ \\__\\ \\_______\\ \\__\\\\ \\__\\           \\ \\__\\ \\ \\__\\ \\__\\ \\_______\\ \\_______\\ \\_______\\</rainbow>"));
print(format("<rainbow displacement=12>    \\|__|    \\|__|\\|_______|\\|_______|        \\|__|\\|__|\\|_______|\\|_______|\\|_______|\\|_______|\\|__|\\|__|    \\|__|  \\|__|\\|_______|\\|__| \\|__|            \\|__|  \\|__|\\|__|\\|_______|\\|_______|\\|_______|</rainbow>"));

#endregion

#region CONSOLE_ENVIRONMENT
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
    userName = output.Split('\\')[1].Substring(0, output.Split('\\')[1].Length-2);
}

while (true) // Bucle principal
{
    //TODO: agregar colores
    Console.WriteLine(Red().Text("┌─[") + Green().Text(userName) + Yellow().Text("@") + Rgb(43, 91, 156).Text(computerName) + Red().Text("]─[") + Rgb(52, 117, 27).Text(workingDirectory) + Red().Text("]"));
    Console.Write(Red().Text("└───■") + Yellow().Text(" $ "));

    command = Console.ReadLine();
    if (command != null && command != "") CommandManager(command);
}
#endregion

#region AUXILIARY_METHODS
void CommandManager(string s)
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
#endregion