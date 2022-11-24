using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace GliderView.Service
{
    public class FaaDatabaseProvider : IFaaDatabaseProvider
    {
        public class Aircraft
        {
            public string TypeAircraft { get; set; }

            public const string TYPE_GLIDER = "Glider";
        }

        private readonly IHttpClientFactory _httpClient;

        public static string FaaNNumberLookupUrl = "https://registry.faa.gov/aircraftinquiry/Search/NNumberResult?NNumbertxt=";


        public FaaDatabaseProvider(IHttpClientFactory httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Aircraft?> Lookup(string nNumber)
        {
            string url = FaaNNumberLookupUrl + nNumber;

            string html = null;

            using (var client = _httpClient.CreateClient())
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();

                html = await response.Content.ReadAsStringAsync();
            }

            var document = new HtmlDocument();
            document.LoadHtml(html);

            var aircraftType = document.DocumentNode.SelectSingleNode("//td[@data-label='Aircraft Type']");

            if (aircraftType == null)
                throw new Exception("Could not parse HTML page.");

            var aircraft = new Aircraft()
            {
                TypeAircraft = aircraftType.InnerText
            };

            return aircraft;
        }
    }
}
