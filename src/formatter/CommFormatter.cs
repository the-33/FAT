using Crayon;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Crayon.Output;

namespace Formatter
{
    // No es necesario explicar nada, funciona y punto. Para detalles sobre como hacer los .comm abrir 'doc/FORMAT.html'

    public static class CommFormatter
    {
        public static string format(string line)
        {
            string result = line;

            result = result.Split("##")[0];
            result = result.Replace("<tab>", "\t");
            result = result.Replace("<br>", "\n");
            result = result.Replace("#<#", "$lowerthan$");
            result = result.Replace("#>#", "$greaterthan$");

            return result + "\n";
        }

        public static void print(string formatted, bool centered = false)
        {
            if (centered)
            {
                string s = formatted.Replace("$lowerthan$", "$").Replace("$greaterthan$", "$");

                int length = 0;
                bool countChar = true;

                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '<':
                            countChar = false;
                            break;
                        case '>':
                            countChar = true;
                            break;
                        default:
                            if (countChar) length++;
                            break;
                    }
                }

                Console.SetCursorPosition((Console.WindowWidth - length) / 2, Console.CursorTop);
            }

            formatted = formatted.Replace("<", "<##");
            formatted = formatted.Replace(">", "##>");

            string[] parts = formatted.Split(new char[] { '<', '>' }, StringSplitOptions.RemoveEmptyEntries);

            bool hasColor = false;
            string colorString = "";
            bool isBold = false;
            bool isDim = false;
            bool isUnderlined = false;
            bool isReversed = false;
            bool isRainbow = false;
            int rainbowDisplacement = 0;

            foreach (string part in parts)
            {
                if(part.StartsWith("##") && part.EndsWith("##"))
                {
                    if (part.StartsWith("##color"))
                    {
                        colorString = part.Split('=')[1];
                        colorString = colorString.Replace("##", "");
                        hasColor = true;
                    }
                    else if (part == "##b##") isBold = true;
                    else if (part == "##d##") isDim = true;
                    else if (part == "##u##") isUnderlined = true;
                    else if (part == "##r##") isReversed = true;
                    else if (part.StartsWith("##rainbow"))
                    {
                        isRainbow = true;
                        if(part.Contains("displacement="))
                        {
                            try { rainbowDisplacement = int.Parse(part.Split("=", StringSplitOptions.RemoveEmptyEntries)[1].Replace("##", "")); }
                            catch (Exception e) { Console.WriteLine(Red().Bold().Text(e.Message)); }
                        }
                    }
                    else if (part == "##/color##") hasColor = false;
                    else if (part == "##/b##") isBold = false;
                    else if (part == "##/d##") isDim = false;
                    else if (part == "##/u##") isUnderlined = false;
                    else if (part == "##/r##") isReversed = false;
                    else if (part == "##/rainbow##") isRainbow = false;
                }
                else
                {
                    string s = part;
                    s = s.Replace("$lowerthan$", "<");
                    s = s.Replace("$greaterthan$", ">");

                    if (!isRainbow)
                    {
                        IOutput? format = null;

                        if (isBold) format = format == null ? Bold() : format.Bold();
                        if (isDim) format = format == null ? Dim() : format.Dim();
                        if (isUnderlined) format = format == null ? Underline() : format.Underline();
                        if (isReversed) format = format == null ? Reversed() : format.Reversed();

                        if (hasColor)
                        {
                            switch (colorString)
                            {
                                case "BLACK":
                                    format = format == null ? Black() : format.Black();
                                    break;
                                case "RED":
                                    format = format == null ? Red() : format.Red();
                                    break;
                                case "GREEN":
                                    format = format == null ? Green() : format.Green();
                                    break;
                                case "YELLOW":
                                    format = format == null ? Yellow() : format.Yellow();
                                    break;
                                case "BLUE":
                                    format = format == null ? Blue() : format.Blue();
                                    break;
                                case "MAGENTA":
                                    format = format == null ? Magenta() : format.Magenta();
                                    break;
                                case "CYAN":
                                    format = format == null ? Cyan() : format.Cyan();
                                    break;
                                default:
                                case "WHITE":
                                    format = format == null ? White() : format.White();
                                    break;
                            }

                            if (colorString.StartsWith("RGB("))
                            {
                                colorString = colorString.Replace("RGB(", "").Replace(")##", "");
                                string[] rgbStrings = colorString.Split(',', ' ', StringSplitOptions.RemoveEmptyEntries);
                                int[] rgb = new int[] { int.Parse(rgbStrings[0]), int.Parse(rgbStrings[1]), int.Parse(rgbStrings[2]) };
                                format = format == null ? Rgb((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]) : format.Rgb((byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
                            }
                        }

                        if (format == null) Console.Write(s);
                        else Console.Write(format.Text(s));
                    }
                    else
                    {
                        var rainbow = new Rainbow(0.5);
                        IOutput format;
                        for (int i = rainbowDisplacement; i > 0; i--) { format = rainbow.Next(); }

                        foreach (char c in s)
                        {
                            //if (c == ' ') Console.Write(" ");
                            //else
                            //{
                                format = rainbow.Next();

                                if (isDim) format = format.Dim();
                                if (isUnderlined) format = format.Underline();
                                if (isReversed) format = format.Reversed();

                                Console.Write(format.Text(c.ToString()));
                            //}
                        }
                    }
                }
            }
        }
    }
}
