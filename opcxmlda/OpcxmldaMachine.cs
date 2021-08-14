using System.Collections.Generic;
using l99.driver.@base;
using OpcLabs.BaseLib.DataTypeModel.ComTypes;
using OpcLabs.EasyOpc.DataAccess;

namespace l99.driver.opcxmlda
{
    public class OpcxmldaMachine: Machine
    {
        public override string ToString()
        {
            return new
            {
                Id,
                _opcxmldaEndpoint.URI,
                _opcxmldaEndpoint.ConnectionTimeout
            }.ToString();
        }

        public override dynamic Info
        {
            get
            {
                return new
                {
                    _id = id,
                    _opcxmldaEndpoint.URI,
                    _opcxmldaEndpoint.ConnectionTimeout
                };
            }
        }

        public OpcxmldaEndpoint OpcxmldaEndpoint
        {
            get => _opcxmldaEndpoint;
        }
        
        private OpcxmldaEndpoint _opcxmldaEndpoint;

        public EasyDAClient Client
        {
            get => _client;
        }

        private EasyDAClient _client;

        public OpcxmldaMachine(Machines machines, bool enabled, string id, object config) : base(machines, enabled, id, config)
        {
            dynamic cfg = (dynamic) config;
            
            this["cfg"] = cfg;
            this["data"] = cfg.type["data"];
            this["platform"] = new Platform(this);
            
            _opcxmldaEndpoint = new OpcxmldaEndpoint(cfg.type["net_uri"], (short)cfg.type["net_timeout_s"]);
            
            _client = new EasyDAClient();
        }
    }
}