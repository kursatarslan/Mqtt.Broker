using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mqtt.Domain.Models;

namespace Mqtt.Data.Contracts
{
    public interface IMqttRepository
    {
        MqttUser GetUser(string username);
        MqttUser AddUser(string username, string password);
        bool SaveChanges();
        Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);
        Guid AddMessage(MqttMessage message);
        Task<bool> AddLogAsync(Log log);
        int AddAudit(Audit audit);
        void UpdatePlatoon(Platoon platoon);
        void DeletePlatoon(Platoon platoon);
        void DeletePlatoonRange(Platoon[] platoons);
        int AddPlatoon(Platoon platoon);
        int AddSubscribe(Subscribe sub);
        Connection AddConnection(Connection con);
        IEnumerable<MqttMessage> GetMessages();
        IEnumerable<Connection> GetConnection();
        IEnumerable<Platoon> GetPlatoon();
        Platoon GetPlatoonById(string id);
        Subscribe GetSubscribeByTopic(string clientId, string topic, string qos);
        IEnumerable<Subscribe> GetSubscribeById(string clientId);
        IEnumerable<Subscribe> GetSubscribe();
        Platoon GetPlatoon(string vehicleId);
    }
}