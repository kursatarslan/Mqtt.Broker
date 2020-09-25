namespace Mqtt.Application.Contracts
{
    public interface ISecretProvider
    {
         string GetSecret(string key);
    }
}