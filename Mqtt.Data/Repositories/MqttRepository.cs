using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mqtt.Context;
using Mqtt.Data.Contracts;
using Mqtt.Domain.Models;

namespace Mqtt.Data.Repositories
{
    public class MqttRepository : IMqttRepository
    {
        private readonly DataContext _context;

        public MqttRepository(
            DataContext context)
        {
            _context = context;
        }

        public MqttUser GetUser(string username)
        {
            var user = _context.MqttUsers.FirstOrDefault((u => u.Username.Equals(username)));
            return user;
        }
        
        public void UpdatePlatoon(Platoon platoon)
        {
            _context.Entry(platoon).State = EntityState.Modified;
        }

        public MqttUser AddUser(string username, string password)
        {
            var user = _context.MqttUsers.Add(new MqttUser {Username = username, Password = password});
            return user.Entity;
        }

        public Connection AddConnection(Connection con)
        {
            var newCon = _context.Connection.Add(con);
            return newCon.Entity;
        }
        public int AddPlatoon(Platoon platoon)
        {
            var newCon = _context.Platoon.Add(platoon);
            return newCon.Entity.Id;
        }

        public bool SaveChanges()
            => _context.SaveChanges() > 0;

        public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken) > 0;

        public Guid AddMessage(MqttMessage message)
        {
            return _context.MqttMessages.Add(message).Entity.Id;
        }

        public int AddAudit(Audit audit)
        {
            return _context.Audit.Add(audit).Entity.Id;
        }

        public async Task<bool> AddLogAsync(Log log)
        {
             _context.Log.Add(log);
             return await SaveChangesAsync();
        }

        public int AddSubscribe(Subscribe subscribe)
        {
            return _context.Subscribe.Add(subscribe).Entity.Id;
        }

        public IEnumerable<MqttMessage> GetMessages()
        {
            return _context.MqttMessages.ToList();
        }
        public IEnumerable<Platoon> GetPlatoon()
        {
            return _context.Platoon.ToList();
        }
        public Subscribe GetSubscribeByTopic(string clientId, string topic, string qos) {
            return _context.Subscribe.FirstOrDefault(f => f.ClientId == clientId && f.Topic ==topic && f.QoS == qos );
        }
        public IEnumerable<Subscribe> GetSubscribeById(string clientId) {
            return _context.Subscribe.Where(f => f.ClientId == clientId ).ToList();
        }
        public Platoon GetPlatoonById(int id)
        {
            return _context.Platoon.FirstOrDefault(f => f.Id == id && f.Enable);
        }

        public Platoon GetPlatoon(string vehicleId)
        {
            return _context.Platoon.FirstOrDefault(f => f.VechicleId == vehicleId && f.Enable);
        }
        
    }
}