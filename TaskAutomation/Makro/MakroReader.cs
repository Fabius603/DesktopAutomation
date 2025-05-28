using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TaskAutomation.Makro
{
    public class MakroReader
    {
        public static MakroList LadeMakroDatei(string pfad)
        {
            if (!File.Exists(pfad))
                throw new FileNotFoundException("Datei nicht gefunden", pfad);

            string json = File.ReadAllText(pfad);

            var options = new JsonSerializerOptions
            {
                Converters = { new MakroBefehlConverter() },
                PropertyNameCaseInsensitive = true
            };
            return JsonSerializer.Deserialize<MakroList>(json, options);
        }
    }
}
