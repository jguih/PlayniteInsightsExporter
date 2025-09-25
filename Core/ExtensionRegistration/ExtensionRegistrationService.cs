using Core.Models;
using Core.Models.Error;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Core.ExtensionRegistration
{
    public class ExtensionRegistrationService : IExtensionRegistrationService
    {
        private IKeyManager keyManager { get; set; }
        private IPlayAtlasWebServerService webServer { get; set; }
        private IPlayAtlasExporterContext context { get; set; }
        private IFileSystemService fileSystemService { get; set; }

        public string RegistrationId { get; set; }

        public ExtensionRegistrationService(
            IKeyManager keyManager,
            IPlayAtlasWebServerService webServer,
            IPlayAtlasExporterContext context,
            IFileSystemService fileSystemService
        )
        {
            this.keyManager = keyManager;
            this.webServer = webServer;
            this.context = context;
            this.fileSystemService = fileSystemService;
        }

        private async Task<bool> IsAlreadyRegistered()
        {
            string regId;

            try
            {
                regId = GetRegistrationId();
            } catch (ExtensionException)
            {
                return false; // No local registration id found
            }

            try
            {
                var response = await webServer.CheckHealth();
                return response.IsSuccessStatusCode;
            }
            catch (ExtensionException)
            {
                return true; // Server not reachable, don't try to register again
            }
        }

        public async Task<bool> RegisterAsync()
        {
            if (await IsAlreadyRegistered())
                return false;

            string hostname = Environment.MachineName;
            string os = Environment.OSVersion.ToString();
            string id = context.GetExtensionId();
            string version = context.GetExtensionVersion();
            string key = keyManager.GetPublicKeyAsPem();

            RegisterExtensionCommand command = new RegisterExtensionCommand()
            {
                Hostname = hostname,
                Os = os,
                ExtensionId = id,
                ExtensionVersion = version,
                PublicKey = key
            };

            var response = await webServer.RegisterAsync(command);
            bool newRegistration = true;

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                newRegistration = false; // Attempted to register extension twice
            } else if (response.StatusCode != System.Net.HttpStatusCode.Created)
            {
                throw new ExtensionException("Unexpected response from server during extension registration");
            }

            var jsonString = await response.Content.ReadAsStringAsync();
            var registrationDto = JsonConvert.DeserializeObject<RegisterExtensionResponseDTO>(jsonString);
            if (registrationDto == null)
            {
                throw new ExtensionException("Server responded with success status code during extension registration, but did not return a valid body");
            }
            var registrationIdPath = fileSystemService.PathCombine(
                context.GetSecurityDirectoryPath(), "registrationId.txt");
            fileSystemService.FileWriteAllText(registrationIdPath, jsonString);
            RegistrationId = registrationDto.RegistrationId;
            return newRegistration;
        }

        public string GetRegistrationId()
        {
            if (!string.IsNullOrEmpty(RegistrationId))
            {
                return RegistrationId;
            }
            var registrationIdPath = fileSystemService.PathCombine(
                context.GetSecurityDirectoryPath(), "registrationId.txt");
            if (!fileSystemService.FileExists(registrationIdPath))
            {
                throw new ExtensionException("Extension is not registered.");
            }
            var jsonString = fileSystemService.FileReadAllText(registrationIdPath);
            var dto = JsonConvert.DeserializeObject<RegisterExtensionResponseDTO>(jsonString);
            RegistrationId = dto.RegistrationId;
            return RegistrationId;
        }
    }
}
