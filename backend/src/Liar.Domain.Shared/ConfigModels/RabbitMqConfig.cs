﻿namespace Liar.Domain.Shared.ConfigModels
{
    public class RabbitMqConfig
    {
        public string VirtualHost { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Port { get; set; }
        public EventBus EventBus { get; set; }
    }

    public class EventBus
    {
        public string ClientName { get; set; }
        public string ExchangeName { get; set; }
    }
}
