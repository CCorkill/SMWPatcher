using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace SMWPatcher.html
{
    public class HackInfo
    {
        [JsonProperty("title")]
        public string title { get; set; }

        [JsonProperty("imageURLs")]
        public List<string> imageURLs { get; set; }

        [JsonProperty("rating")]
        public float rating { get; set; }

        [JsonProperty("author")]
        public string author { get; set; }

        [JsonProperty("type")]
        public string type { get; set; }

        [JsonProperty("exits")]
        public string exits { get; set; }

        [JsonProperty("description")]
        public string description { get; set; }

        [JsonProperty("downloadURL")]
        public string downloadURL { get; set; }
    }
}
