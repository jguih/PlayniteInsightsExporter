using ExporterCommon.Application;
using ExporterCommon.Common;
using ExporterCommon.Infra;
using ExporterPlayAtlasClient.Dtos;
using ExporterPlayAtlasClient.Error;
using ExporterPlayAtlasClient.Infra;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ExporterPlayAtlasClient.Application
{
    public class PlayAtlasHttpClient : IPlayAtlasHttpClientPort
    {
        private readonly IAppLoggerPort appLogger;
        private readonly IHashServicePort hashService;
        private readonly ISyncMediaFilesHttpContentBuilderPort syncMediaFilesHttpContentBuilder;
        private readonly ISyncGamesDtoMapperPort syncGamesDtoMapper;
        private readonly IJsonHttpContentBuilderPort jsonHttpContentBuilder;
        private readonly IHttpRequestSignerPort requestSigner;
        private readonly HttpClient httpClient;

        public PlayAtlasHttpClient(
            IAppLoggerPort appLogger,
            IHashServicePort hashService,
            ISyncMediaFilesHttpContentBuilderPort syncMediaFilesHttpContentBuilder,
            ISyncGamesDtoMapperPort syncGamesDtoMapper,
            IJsonHttpContentBuilderPort jsonHttpContentBuilder,
            IHttpRequestSignerPort requestSigner,
            HttpClient httpClient
        )
        {
            this.appLogger = appLogger;
            this.hashService = hashService;
            this.syncMediaFilesHttpContentBuilder = syncMediaFilesHttpContentBuilder;
            this.syncGamesDtoMapper = syncGamesDtoMapper;
            this.jsonHttpContentBuilder = jsonHttpContentBuilder;
            this.httpClient = httpClient;
            this.requestSigner = requestSigner;
        }

        public async Task<PlayAtlasLibraryManifest> GetManifestAsync()
        {
            try
            {
                string endpoint = SendGetManifestRequestDto.ENDPOINT;
                using (var request = requestSigner.CreateSignedRequest(HttpMethod.Get, endpoint))
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

        public async Task SyncGamesAsync(
            SyncGamesCommand command, 
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                string endpoint = SyncGamesRequestDto.ENDPOINT;
                var requestDto = syncGamesDtoMapper.Map(command);
                var jsonString = requestDto.ToJsonString();
                var contentHash = hashService.ComputeSHA256Base64(jsonString);

                using (
                    var jsonContent = jsonHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = requestSigner.CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest, cancellationToken))
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

        public async Task SyncMediaFilesAsync(
            SyncMediaFilesCommand command, 
            CancellationToken cancellationToken = default
        )
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
                    var signedRequest = requestSigner.CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        multipartContent,
                        canonicalHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest, cancellationToken))
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

        public async Task OpenGameSessionAsync(
            OpenGameSessionCommand command, 
            CancellationToken cancellationToken = default
        )
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
                    var jsonContent = jsonHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = requestSigner.CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest, cancellationToken))
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

        public async Task CloseGameSessionAsync(
            CloseGameSessionCommand command, 
            CancellationToken cancellationToken = default
        )
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
                    var jsonContent = jsonHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = requestSigner.CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest, cancellationToken))
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

        public async Task StaleGameSessionAsync(
            StaleGameSessionCommand command, 
            CancellationToken cancellationToken = default
        )
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
                    var jsonContent = jsonHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = requestSigner.CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest, cancellationToken))
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

        public async Task<ExtensionRegistration> RegisterExtensionAsync(
            RegisterExtensionCommand command,
            CancellationToken cancellationToken = default
        )
        {
            try
            {
                string endpoint = RegisterExtensionRequestDto.ENDPOINT;
                var requestDto = new RegisterExtensionRequestDto(
                    extensionId: command.ExtensionId,
                    publicKey: command.PublicKeyPem,
                    hostname: command.Hostname,
                    os: command.Os,
                    extensionVersion: command.ExtensionVersion
                );
                var jsonString = requestDto.ToJsonString();
                var contentHash = hashService.ComputeSHA256Base64(jsonString);

                using (
                    var jsonContent = jsonHttpContentBuilder.Build(requestDto)
                )
                using (
                    var signedRequest = requestSigner.CreateSignedRequest(
                        HttpMethod.Post,
                        endpoint,
                        jsonContent,
                        contentHash,
                        includeRegistrationId: false
                    )
                )
                using (var response = await httpClient.SendAsync(signedRequest, cancellationToken))
                {
                    if (response.StatusCode == HttpStatusCode.Conflict)
                        throw new ExtensionAlreadyRegisteredException();

                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsStringAsync();
                    var registration = JsonConvert.DeserializeObject<ExtensionRegistration>(content) ?? throw new InvalidDataException("Failed to parse extension registration response from PlayAtlas server");
                    return registration;
                }

            }
            catch (Exception ex)
            {
                appLogger.Error("PlayAtlas request to register extension failed", ex);
                throw;
            }
        }
    }
}
