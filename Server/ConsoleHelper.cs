using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WindowsSshServer
{
    public static class ConsoleHelper
    {
        // Replaces any control characters (except tab, carriage return, and newline) with safe sequences
        // to avoid attacks by sending terminal control characters.
        public static string FilterControlChars(string text)
        {
            var textBuilder = new StringBuilder(text);
            char curChar;

            for(int i = 0; i < textBuilder.Length; i++)
            {
                curChar = textBuilder[i];

                // Ignore tab, carriage return, and newline.
                if (curChar == '\t' || curChar == '\r' || curChar == '\n') continue;

                // Check if char is control char.
                if (char.IsControl(curChar))
                {
                    // Replace char with safe sequence.
                    textBuilder.Remove(i, 1);
                    textBuilder.Insert(i, "\\" + (int)curChar);
                }
            }

            // Return filtered text.
            return textBuilder.ToString();
        }
    }
}
