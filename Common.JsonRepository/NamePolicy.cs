using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Common.JsonRepository
{
    public static class NamePolicy
    {
        private const int MaxLen = 30;

        // Erlaubt: Buchstaben, Ziffern, Leerzeichen, _.- 
        // (Passe an, wenn du Umlaute/weitere Zeichen erlauben willst)
        public static string Sanitize(string? raw)
        {
            var s = (raw ?? "").Trim();
            if (string.IsNullOrEmpty(s)) s = "Unnamed";

            s = Regex.Replace(s, @"\s+", " ");                 // Whitespaces bündeln
            s = Regex.Replace(s, @"[^A-Za-z0-9 _\.-]", "_");   // Sonderzeichen -> _
            s = s.Trim(' ', '.', '-', '_');                     // Ränder säubern
            if (s.Length == 0) s = "Unnamed";
            if (s.Length > MaxLen) s = s[..MaxLen];
            return s;
        }

        public static string MakeUnique(string baseName, ISet<string> taken)
        {
            if (!taken.Contains(baseName)) return baseName;
            // Stil: "Name (2)", "Name (3)", ...
            for (int i = 2; ; i++)
            {
                var cand = $"{baseName} ({i})";
                if (!taken.Contains(cand)) return cand;
            }
        }
    }
}
