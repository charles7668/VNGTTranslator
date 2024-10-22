using Microsoft.Extensions.DependencyInjection;
using NAudio.Wave;
using System.ComponentModel.Composition;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using VNGTTranslator.Configs;
using VNGTTranslator.Models;
using VNGTTranslator.Network;

namespace VNGTTranslator.TTSProviders.Microsoft
{
    [Export(typeof(ITTSProvider))]
    internal class EdgeTTSProvider : ITTSProvider
    {
        public EdgeTTSProvider()
        {
            _appConfigProvider = Program.ServiceProvider.GetRequiredService<IAppConfigProvider>();
            _networkService = Program.ServiceProvider.GetRequiredService<INetworkService>();
            Dictionary<string, object> configs = null!;
            Task.Run(async () =>
            {
                configs = await _appConfigProvider.GetTTSProviderConfigAsync(ProviderName);
            }).Wait();
            _configs = configs;
            if (configs.TryGetValue(SELECTED_VOICE_NAME_KEY, out object? selectedVoice))
            {
                SelectedVoice = selectedVoice.ToString() ?? "";
            }

            if (configs.TryGetValue(VOLUME_KEY, out object? volumeProp))
            {
                Volume = ((JsonElement?)volumeProp).Value.GetInt16();
            }

            if (configs.TryGetValue(RATE_KEY, out object? rateProp))
            {
                Rate = ((JsonElement?)rateProp).Value.GetInt16();
            }
        }

        private const string SELECTED_VOICE_NAME_KEY = "SelectedVoice";
        private const string VOLUME_KEY = "Volume";
        private const string RATE_KEY = "Rate";

        private const string CHROMIUM_MAJOR_VERSION = "129";

        private const string TRUSTED_TOKEN = "6A5AA1D4EAFF4E9FB37E23D68491D6F4";

        private readonly IAppConfigProvider _appConfigProvider;
        private readonly Dictionary<string, object> _configs;
        private readonly object _lock = new();
        private readonly INetworkService _networkService;

        private readonly string _ttsUrl =
            $"wss://speech.platform.bing.com/consumer/speech/synthesize/readaloud/edge/v1?TrustedClientToken={TRUSTED_TOKEN}";

        private readonly string _voiceListUrl =
            $"https://speech.platform.bing.com/consumer/speech/synthesize/readaloud/voices/list?trustedclienttoken={TRUSTED_TOKEN}";

        private int _rate = 1;
        private CancellationTokenSource _speakCts = new();
        private List<string>? _voiceList;
        private int _volume = 100;

        private ClientWebSocket? _webSocketClient;

        public string ProviderName { get; } = "Edge TTS";
        public string SelectedVoice { get; private set; } = string.Empty;

        public int Rate
        {
            get => _rate;
            set
            {
                _configs[RATE_KEY] = value;
                _rate = value;
            }
        }

        public int Volume
        {
            get => _volume;
            set
            {
                _configs[VOLUME_KEY] = value;
                _volume = value;
            }
        }

        public IEnumerable<string> GetSupportedVoiceList()
        {
            if (_voiceList != null)
                return _voiceList;
            try
            {
                HttpClient httpClient = _networkService.DefaultHttpClient;
                if (_appConfigProvider.GetAppConfig().IsUseProxy)
                {
                    if (_networkService.ProxyHttpClient != null)
                        httpClient = _networkService.ProxyHttpClient;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, _voiceListUrl);
                request.Headers.Add("Authority", "speech.platform.bing.com");
                request.Headers.Add("Sec-CH-UA",
                    $"\" Not;A Brand\";v=\"99\", \"Microsoft Edge\";v=\"{CHROMIUM_MAJOR_VERSION}\", \"Chromium\";v=\"{CHROMIUM_MAJOR_VERSION}\"");
                request.Headers.Add("Sec-CH-UA-Mobile", "?0");
                request.Headers.Add("Sec-Fetch-Site", "none");
                request.Headers.Add("Sec-Fetch-Mode", "cors");
                request.Headers.Add("Sec-Fetch-Dest", "empty");
                HttpResponseMessage responseMessage = httpClient.SendAsync(request).Result;

                string responseString = responseMessage.Content.ReadAsStringAsync().Result;
                var jsonDocument = JsonDocument.Parse(responseString);
                JsonElement jsonRoot = jsonDocument.RootElement;
                var result = new List<string>();
                foreach (JsonElement jsonElement in jsonRoot.EnumerateArray())
                {
                    if (!jsonElement.TryGetProperty("Name", out JsonElement nameProp))
                        continue;
                    string? name = nameProp.GetString();
                    if (!string.IsNullOrWhiteSpace(name))
                        result.Add(name);
                }

                _voiceList = result;
                return result;
            }
            catch (Exception)
            {
                return [];
            }
        }

        public Result SetVoice(string voiceName)
        {
            if (string.IsNullOrWhiteSpace(voiceName) || _voiceList == null)
                return Result.Success();
            if(!_voiceList.Contains(voiceName))
                return Result.Success();
            SelectedVoice = voiceName;
            _configs[SELECTED_VOICE_NAME_KEY] = voiceName;
            return Result.Success();
        }

        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(SelectedVoice))
                throw new Exception("Please select a voice first");
            if (_webSocketClient == null)
            {
                lock (_lock)
                {
                    _webSocketClient ??= new ClientWebSocket();
                }

                _speakCts = new CancellationTokenSource();
                try
                {
                    AppConfig appConfig = _appConfigProvider.GetAppConfig();
                    if (appConfig.IsUseProxy)
                    {
                        try
                        {
                            if (appConfig.IsUseSystemProxy)
                            {
                                _webSocketClient.Options.Proxy = WebRequest.DefaultWebProxy;
                            }
                            else
                            {
                                string proxyAddress = "http://" + appConfig.ProxyAddress + ":" + appConfig.ProxyPort;
                                var proxy = new WebProxy(proxyAddress);
                                _webSocketClient.Options.Proxy = proxy;
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    _webSocketClient.Options.SetRequestHeader("Pragma", "no-cache");
                    _webSocketClient.Options.SetRequestHeader("Cache-Control", "no-cache");
                    _webSocketClient.Options.SetRequestHeader("Origin",
                        "chrome-extension://jdiccldimpdaibmpdkjnbmckianbfold");
                    var serverUri = new Uri(_ttsUrl + $"&ConnectionId={GenerateConnectId()}");
                    await _webSocketClient.ConnectAsync(serverUri, _speakCts.Token);
                    await SendWebSocketCommandAsync(_webSocketClient, _speakCts.Token);
                    await SendSsmlRequestAsync(_webSocketClient, text, _speakCts.Token);
                    List<byte> voiceBinary = await ReceiveMessagesAsync(_webSocketClient);
                    await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "close",
                        _speakCts.Token);
                    _webSocketClient = null;
                    _ = SpeakInternalAsync(voiceBinary, _speakCts.Token);
                }
                catch (TaskCanceledException)
                {
                }
                catch
                {
                    // ignore
                }
                finally
                {
                    if (_webSocketClient is { State: WebSocketState.Open })
                    {
                        try
                        {
                            await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "close",
                                CancellationToken.None);
                        }
                        catch
                        {
                            // ignore
                        }
                    }

                    _webSocketClient = null;
                }
            }
        }

        public Task<Result> StoreSettingsAsync()
        {
            return _appConfigProvider.SaveTTSProviderConfigAsync(ProviderName, _configs);
        }

        public void StopSpeak()
        {
            _speakCts.Cancel();
        }

        private static string DateToString()
        {
            DateTime utcNow = DateTime.UtcNow;
            return utcNow.ToString("ddd MMM dd yyyy HH:mm:ss 'GMT+0000 (Coordinated Universal Time)'",
                CultureInfo.InvariantCulture) ?? throw new ArgumentNullException();
        }

        private static string GenerateConnectId()
        {
            return Guid.NewGuid().ToString().Replace("-", "");
        }

        private string MakeSsml(string text)
        {
            string voiceName = SelectedVoice;
            string rate;
            if (Rate >= 1)
            {
                int percent = (Rate - 1) * 100;
                rate = $"+{percent}%";
            }
            else
            {
                int percent = (1 - Rate) * 100;
                rate = $"-{percent}%";
            }

            string volume = $"-{(100 - Volume).ToString()}%";
            if (Volume == 100)
                volume = "+0%";

            return $@"
            <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
                <voice name='{voiceName}'>
                    <prosody pitch='+0Hz' rate='{rate}' volume='{volume}'>
                        {text}
                    </prosody>
                </voice>
            </speak>";
        }

        private static async Task<List<byte>> ReceiveMessagesAsync(ClientWebSocket client)
        {
            var resultBinary = new List<byte>();
            byte[] buffer = new byte[1024 * 4]; // 4KB buffer
            while (client.State == WebSocketState.Open)
            {
                WebSocketReceiveResult result =
                    await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    int headerLength = receivedMessage.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                    string header = receivedMessage.Substring(0, headerLength);
                    string[] lines = header.Split("\r\n");
                    bool breakWhile = false;
                    foreach (string line in lines)
                    {
                        if (!line.StartsWith("Path:"))
                            continue;
                        string path = line.Substring(5);
                        if (path.Trim() == "turn.end")
                        {
                            breakWhile = true;
                            break;
                        }
                    }

                    if (breakWhile)
                        break;
                    continue;
                }

                {
                    byte[] headerLengthBytes = { buffer[1], buffer[0] };
                    short headerLength = BitConverter.ToInt16(headerLengthBytes);
                    resultBinary.AddRange(buffer[(headerLength + 2)..result.Count]);
                }
            }

            return resultBinary;
        }

        private async Task SendSsmlRequestAsync(ClientWebSocket webSocket, string text,
            CancellationToken cancellationToken)
        {
            string message = SsmlHeadersPlusData(GenerateConnectId(), DateToString(), MakeSsml(text));
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
                cancellationToken);
        }

        private static async Task SendWebSocketCommandAsync(ClientWebSocket webSocket,
            CancellationToken cancellationToken)
        {
            string message =
                $"X-Timestamp:{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\r\n" +
                "Content-Type:application/json; charset=utf-8\r\n" +
                "Path:speech.config\r\n\r\n" +
                "{\"context\":{\"synthesis\":{\"audio\":{\"metadataoptions\":{" +
                "\"sentenceBoundaryEnabled\":false,\"wordBoundaryEnabled\":true}," +
                "\"outputFormat\":\"audio-24khz-48kbitrate-mono-mp3\"" +
                "}}}}\r\n";

            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true,
                cancellationToken);
        }

        private async Task SpeakInternalAsync(List<byte> audioData, CancellationToken cancellationToken)
        {
            try
            {
                using var ms = new MemoryStream(audioData.ToArray());
                await using var waveReader = new Mp3FileReader(ms);
                using var waveOut = new WaveOutEvent();
                waveOut.Init(waveReader);
                waveOut.Play();

                while (waveOut.PlaybackState == PlaybackState.Playing && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            catch
            {
                // ignore
            }
        }

        private string SsmlHeadersPlusData(string requestId, string timestamp, string ssml)
        {
            return
                $"X-RequestId:{requestId}\r\n" +
                "Content-Type:application/ssml+xml\r\n" +
                $"X-Timestamp:{timestamp}Z\r\n" +
                "Path:ssml\r\n\r\n" +
                ssml;
        }
    }
}