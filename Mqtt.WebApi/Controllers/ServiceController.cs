using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Mqtt.Data.Contracts;
using Mqtt.Domain.Models;

namespace Mqtt.WebApi.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class ServiceController : ControllerBase
    {
        private readonly IMqttRepository _repo;

        private readonly ILogger<Controllers.ServiceController> _logger;

        public ServiceController(ILogger<Controllers.ServiceController> logger,IMqttRepository repo)
        {
            _logger = logger;
            _repo = repo;
        }

        [HttpGet("GetPlatoon")]
        public IEnumerable<Platoon> GetPlatoon()
        { 
            _logger.LogInformation(" getting all platoon");
            return _repo.GetPlatoon().ToArray();
        }
        
        [HttpGet("GetSubscribe")]
        public IEnumerable<Subscribe> GetSubscribe()
        {
            _logger.LogInformation(" getting all subscribe");
            return _repo.GetSubscribe().ToArray();
        }
        
        [HttpGet("GetMessage")]
        public IEnumerable<MqttMessage> GetMessage()
        {
            _logger.LogInformation(" getting all message");
            return _repo.GetMessages().ToArray();
        }
        
        [HttpGet("GetConnection")]
        public IEnumerable<Connection> GetConnection()
        {
            _logger.LogInformation(" getting all connection");
            return _repo.GetConnection().ToArray();
        }
    }
}