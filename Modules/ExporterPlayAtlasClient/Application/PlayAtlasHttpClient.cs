using ExporterCommon.Application;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Dtos;
using ExporterPlayAtlasClient.Infra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public class PlayAtlasHttpClient : IPlayAtlasHttpClientPort
    {
        private readonly IAppLoggerPort appLogger;
        private readonly IExporterPluginContextPort pluginContext;
        private readonly ISystemConfigPort systemConfig;
        private readonly ISignatureServicePort signatureService;
        private readonly IHashServicePort hashService;
        private readonly ISyncGamesHttpContentBuilderPort syncGamesHttpContentBuilder;
        private readonly ISyncMediaFilesHttpContentBuilderPort syncMediaFilesHttpContentBuilder;
        private readonly ISyncGamesDtoMapperPort syncGamesDtoMapper;
        private readonly IOpenGameSessionHttpContentBuilderPort openGameSessionHttpContentBuilder;
        private readonly ICloseGameSessionHttpContentBuilderPort closeGameSessionHttpContentBuilder;
        private readonly IStaleGameSessionHttpContentBuilderPort staleGameSessionHttpContentBuilder;
        private readonly HttpClient httpClient;

        public PlayAtlasHttpClient(
            IAppLoggerPort appLogger,
            IExporterPluginContextPort pluginContext,
            ISystemConfigPort systemConfig,
            ISignatureServicePort signatureService,
            IHashServicePort hashService,
            ISyncGamesHttpContentBuilderPort syncGamesHttpContentBuilder,
            ISyncMediaFilesHttpContentBuilderPort syncMediaFilesHttpContentBuilder,
            ISyncGamesDtoMapperPort syncGamesDtoMapper,
            IOpenGameSessionHttpContentBuilderPort openGameSessionHttpContentBuilder,
            ICloseGameSessionHttpContentBuilderPort closeGameSessionHttpContentBuilder,
            IStaleGameSessionHttpContentBuilderPort staleGameSessionHttpContentBuilder
        )
        {
            this.appLogger = appLogger;
            this.pluginContext = pluginContext;
            this.systemConfig = systemConfig;
            this.signatureService = signatureService;
            this.hashService = hashService;
            this.syncGamesHttpContentBuilder = syncGamesHttpContentBuilder;
            this.syncMediaFilesHttpContentBuilder = syncMediaFilesHttpContentBuilder;
            this.syncGamesDtoMapper = syncGamesDtoMapper;
            this.openGameSessionHttpContentBuilder = openGameSessionHttpContentBuilder;
            this.closeGameSessionHttpContentBuilder = closeGameSessionHttpContentBuilder;
            this.staleGameSessionHttpContentBuilder = staleGameSessionHttpContentBuilder;

            httpClient = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        private string ParseUrl(string endpoint = "")
        {
            var webAppUrl = pluginContext.GetWebServerURL();
            if (string.IsNullOrEmpty(endpoint))
            {
                return webAppUrl;
            }
            return $"{webAppUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";
        }

        private HttpRequestMessage CreateSignedRequest(
            HttpMethod method,
            string endpoint,
            HttpContent content = null,
            string bodyHash = null
        )
        {
            string serverUrl = pluginContext.GetWebServerURL();
            string requestUrl = ParseUrl(endpoint);
            string registrationId = systemConfig.ExtensionRegistrationId;
            string extensionId = pluginContext.GetExtensionId();

            byte[] canonicalBytes = signatureService.BuildRequestCanonicalString(
                method: method,
                endpoint: endpoint,
                bodyHash: bodyHash
            );

            string signatureBase64 = signatureService.Sign(canonicalBytes);

            var request = new HttpRequestMessage(method, requestUrl);

            if (content != null)
            {
                request.Content = content;
            }

            request.Headers.Add("Origin", serverUrl);
            request.Headers.Add("Referer", serverUrl);
            request.Headers.Add("X-Signature", signatureBase64);
            request.Headers.Add("X-ExtensionId", extensionId);
            request.Headers.Add("X-RegistrationId", registrationId);

            if (bodyHash != null)
            {
                request.Headers.Add("X-ContentHash", bodyHash);
            }

            return request;
        }


        public async Task<PlayAtlasLibraryManifest> GetManifestAsync()
        {
            try
            {
                string endpoint = SendGetManifestRequestDto.ENDPOINT;
                using (var request = CreateSignedRequest(HttpMethod.Get, endpoint))
                using (var response = await httpClient.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();

                    var body = await response.Content.ReadAsStringAsync();
                    var manifest = JsonConvert.DeserializeObject<PlayAtlasLibraryManifest>(body);

                    return manifest;
                }
            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas manifest request failed", ex);
                throw;
            }
        }

        public async Task SyncGamesAsync(SyncGamesCommand command)
        {
            try
            {
                string endpoint = SyncGamesRequestDto.ENDPOINT;
                var requestDto = syncGamesDtoMapper.Map(command);
                var jsonString = requestDto.ToJsonString();
                var contentHash = hashService.ComputeSHA256Base64(jsonString);

                using (
                    var jsonContent = syncGamesHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest))
                {
                    response.EnsureSuccessStatusCode();
                }

            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas request to sync games failed", ex);
                throw;
            }
        }

        public async Task SyncMediaFilesAsync(SyncMediaFilesCommand command)
        {
            try
            {
                string endpoint = SyncMediaFilesRequestDto.ENDPOINT;
                var canonicalHash = hashService.ComputeCanonicalSHA256ForGameMediaFiles(
                        gameId: command.GameId,
                        contentHash: command.ContentHash,
                        mediaFolderPath: command.MediaFolderPath
                    );
                var requestDto = new SyncMediaFilesRequestDto
                {
                    CanonicalHash = canonicalHash,
                    ContentHash = command.ContentHash,
                    GameId = command.GameId,
                    MediaFiles = command.MediaFiles,
                };

                using (
                    var multipartContent = syncMediaFilesHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        multipartContent,
                        canonicalHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest))
                {
                    response.EnsureSuccessStatusCode();
                }

            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas request to sync game media files failed", ex);
                throw;
            }
        }

        public async Task OpenGameSessionAsync(OpenGameSessionCommand command)
        {
            try
            {
                string endpoint = OpenGameSessionRequestDto.ENDPOINT;
                var requestDto = new OpenGameSessionRequestDto(
                    sessionId: command.GameSession.SessionId,
                    gameId: command.GameSession.GameId,
                    startTime: command.GameSession.StartTime
                );
                var jsonString = requestDto.ToJsonString();
                var contentHash = hashService.ComputeSHA256Base64(jsonString);

                using (
                    var jsonContent = openGameSessionHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest))
                {
                    response.EnsureSuccessStatusCode();
                }

            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas request to open game session failed", ex);
                throw;
            }
        }

        public async Task CloseGameSessionAsync(CloseGameSessionCommand command)
        {
            try
            {
                string endpoint = CloseGameSessionRequestDto.ENDPOINT;
                var requestDto = new CloseGameSessionRequestDto(
                    sessionId: command.GameSession.SessionId,
                    gameId: command.GameSession.GameId,
                    startTime: command.GameSession.StartTime,
                    endTime: command.GameSession.EndTime.Value,
                    duration: command.GameSession.Duration.Value
                );
                var jsonString = requestDto.ToJsonString();
                var contentHash = hashService.ComputeSHA256Base64(jsonString);

                using (
                    var jsonContent = closeGameSessionHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest))
                {
                    response.EnsureSuccessStatusCode();
                }

            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas request to close game session failed", ex);
                throw;
            }
        }

        public async Task StaleGameSessionAsync(StaleGameSessionCommand command)
        {
            try
            {
                string endpoint = StaleGameSessionRequestDto.ENDPOINT;
                var requestDto = new StaleGameSessionRequestDto(
                    sessionId: command.GameSession.SessionId,
                    gameId: command.GameSession.GameId,
                    startTime: command.GameSession.StartTime
                );
                var jsonString = requestDto.ToJsonString();
                var contentHash = hashService.ComputeSHA256Base64(jsonString);

                using (
                    var jsonContent = staleGameSessionHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest))
                {
                    response.EnsureSuccessStatusCode();
                }

            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas request to stale game session failed", ex);
                throw;
            }
        }
    }
}
