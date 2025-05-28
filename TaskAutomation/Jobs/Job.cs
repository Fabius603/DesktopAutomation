using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using OpenCvSharp;

namespace TaskAutomation.Jobs
{
    public class Job
    {
        [JsonPropertyName("name")] 
        public string Name { get; set; }

        [JsonPropertyName("repeating")] 
        public bool Repeating { get; set; } 

        public List<object> Steps { get; set; }
    }
}
