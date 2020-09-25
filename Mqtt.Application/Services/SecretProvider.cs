using System;
using System.IO;
using Mqtt.Application.Contracts;
using Microsoft.Extensions.FileProviders;

namespace Mqtt.Application.Services
{
    public class SecretProvider : ISecretProvider
    {
        public string GetSecret(
            string key)
        {
            const string DOCKER_SECRET_PATH = "/run/secrets/";
            if (!Directory.Exists(DOCKER_SECRET_PATH)) 
                return Environment.GetEnvironmentVariable(key);
            using var provider = new PhysicalFileProvider(DOCKER_SECRET_PATH);
            var fileInfo = provider.GetFileInfo(key);
            if (!fileInfo.Exists) 
                return Environment.GetEnvironmentVariable(key);
            using var stream = fileInfo.CreateReadStream();
            using var streamReader = new StreamReader(stream);
            return streamReader.ReadToEnd();
        }
    }
}