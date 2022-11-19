using System.Net.WebSockets;

namespace GliderView.Service
{
    public class FlightBookClient
    {
        private readonly string _url;
        private readonly IHttpClientFactory _httpClient;

        public int ReceiveBufferSize { get; set; } = 8192;

        public FlightBookClient(string url, IHttpClientFactory httpClient)
        {
            _url = url;
            _httpClient = httpClient;
        }

        // api/live/igc/{trackerId}/{start}/{end} - https://gitlab.com/lemoidului/ogn-flightbook/-/blob/master/web/service.py#L83
        // https://gitlab.com/lemoidului/ogn-flightbook/-/blob/master/core/live.py#L252
        // start and end are unix timestamps (seconds) indicating the waypoints to include in the IGC file
        // start of 0 means return all waypoints since last takeoff

        public async Task Connect()
        {
            var token = new CancellationTokenSource();

            var socket = new ClientWebSocket();
            await socket.ConnectAsync(new Uri(_url), token.Token);
            var buffer = new byte[ReceiveBufferSize];
            var result = await socket.ReceiveAsync(buffer, token.Token);
            if (result.MessageType != WebSocketMessageType.Close)
            {

            }
        }

        public async Task<Stream> DownloadIgcFile(string trackerId, DateTime? start = null, DateTime? end = null)
        {
            long startTimestamp = 0;
            if (start != null)
                startTimestamp = GetUnixTimeSeconds(start.Value);

            var url = $"{_url}/live/igc/{trackerId}/{startTimestamp}";
            if (end != null)
                url = url + "/" + GetUnixTimeSeconds(end.Value);

            using (var client = _httpClient.CreateClient())
            using (var response = await client.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();

                // Copy to memory stream because we can't reset the stream from the Http content
                var memoryStream = new MemoryStream();
                using (var stream = await response.Content.ReadAsStreamAsync())
                    stream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);

                return memoryStream;
            }
        }

        private long GetUnixTimeSeconds(DateTime date)
        {
            return ((DateTimeOffset)date).ToUnixTimeSeconds();
        }
    }
}
