using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FAT;
using static Formatter.CommFormatter;
using static Crayon.Output;
using System.Runtime.CompilerServices;

namespace Terminal
{
    /*  Clase que representa un comando cualquiera.
     *  Las strings son para la descripcion del comando cuando se usa --help.
     *  Los archivos ".comm" usaran un formato especificado en el archivo FORMAT.html en la carpeta doc.
     *  El atributo execution guarda la funcion que se ejecutara cuando console manager llame al execute del objeto comando que corresponda.
     */

    public class Command
    {
        private string name { get; set; }
        public string format { get; set; }
        public string description { get; set; }
        public string explanation { get; set; }
        public string options { get; set; }
        public string notes { get; set; }
        private Func<string?[], Fat, string, string> execution { get; set; }

        public Command(string pathToDSC, Func<string?[], Fat, string, string> execution)
        {

            name = "";
            format = "";
            description = "";
            explanation = "";
            options = "";
            notes = "";

            /*  Parseador de formato ".comm" a los atributos de la clase ignorando lineas de comentarios.
             *  - Utiliza los encabezados de cada seccion de formato "#ESTO ES UN ENCABEZADO#" para identificar el atributo al que añadir la linea.
             *      * Las lineas en las secciones #NAME# y #FORMAT# no se suman a los atributos correspondientes sino que los sobreescriben.
             *  - Los comentarios son de formato "##esto es un comentario".
             *  - La omision de comentarios y demas cuestiones del formato las realiza la funcion "format" de CommFormatter la cual es aplicada a cada linea que se lee del ".comm".
             *  
             *  NOTE: este constructor devuelve excepciones tanto si no se encuentra el archivo especificado en la string pathToDSC como si en el parseo de
             *  los archivos ".comm" faltan las secciones obligatorias.
             */

            try
            {
                StreamReader sr = new StreamReader(pathToDSC);

                string? line = sr.ReadLine();

                while (line != null)
                {
                    if (line == "#NAME#") // Obligatoria
                    {
                        while ((line = sr.ReadLine()) != null && !(line.StartsWith('#') && line.EndsWith('#')))
                        {
                            if (!line.StartsWith("##") && line != "") name = format(line);
                        }
                    }
                    else if (line == "#FORMAT#") // Obligatoria
                    {
                        while ((line = sr.ReadLine()) != null && !(line.StartsWith('#') && line.EndsWith('#')))
                        {
                            if (!line.StartsWith("##") && line != "") format = format(line);
                        }
                    }
                    else if (line == "#DESCRIPTION#") // Obligatoria
                    {
                        while ((line = sr.ReadLine()) != null && !(line.StartsWith('#') && line.EndsWith('#')))
                        {
                            if (!line.StartsWith("##") && line != "") description += format(line);
                        }
                    }
                    else if (line == "#EXPLANATION#")
                    {
                        while ((line = sr.ReadLine()) != null && !(line.StartsWith('#') && line.EndsWith('#')))
                        {
                            if (!line.StartsWith("##") && line != "") explanation += format(line);
                        }
                    }
                    else if (line == "#OPTIONS#")
                    {
                        while ((line = sr.ReadLine()) != null && !(line.StartsWith('#') && line.EndsWith('#')))
                        {
                            if (!line.StartsWith("##") && line != "") options += format(line);
                        }
                    }
                    else if (line == "#NOTES#")
                    {
                        while ((line = sr.ReadLine()) != null && !(line.StartsWith('#') && line.EndsWith('#')))
                        {
                            if (!line.StartsWith("##") && line != "") notes += format(line);
                        }
                    }
                    else line = sr.ReadLine();
                }

                sr.Close();

                if (name == "") throw new Exception("#NAME# field missing");
                if (format == "") throw new Exception("#FORMAT# field missing");
                if (description == "") throw new Exception("#DESCRIPTION# field missing");
            }
            catch (Exception e)
            {
                Console.WriteLine(Bold().Red().Text("Error in " + pathToDSC + " file: " + e));
            }
            finally
            {
                this.execution = execution;
            }
        }

        public string execute(string?[] args, Fat fat, string wD)
        {
            if (args.Contains("--help")) { help(); return ""; }
            else
            {
                return execution(args, fat, wD);
            }
        }

        public void help()
        {
            print(name);
            print(format);
            print(description + "\n");
            if(explanation != "" && (options != "" || notes != "")) print(explanation + "\n");
            else print(explanation);
            if (options != "" && notes != "") print(options + "\n");
            else print(options);
            if (notes != "") print(notes);
            Console.WriteLine();
        }
    }
}
