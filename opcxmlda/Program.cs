using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using l99.driver.@base;
using l99.driver.@base.mqtt;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using NLog;
using NLog.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace l99.driver.opcxmlda
{
    partial class Program
    {
        private static ILogger _logger;
        
        static async Task Main(string[] args)
        {
            detectArch();
            string nlog_file = getArgument(args, "--nlog", "nlog.config");
            _logger = setupLogger(nlog_file);
            string config_file = getArgument(args, "--config", "config.yml");
            dynamic config = readConfig(config_file);
            Machines machines = await createMachines(config);
            await machines.RunAsync();
            LogManager.Shutdown();
        }

        static void detectArch()
        {
            Console.WriteLine($"Bitness: {(IntPtr.Size == 8 ? "64-bit" : "32-bit")}");
        }
        
        static string getArgument(string[] args, string option_name, string defaultValue)
        {
            var value = args.SkipWhile(i => i != option_name).Skip(1).Take(1).FirstOrDefault();
            var option_value = string.IsNullOrEmpty(value) ? defaultValue : value;
            Console.WriteLine($"Argument '{option_name}' = '{option_value}'");
            return option_value;
        }
        
        static Logger setupLogger(string config_file)
        {
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(config_file);

            var config = new ConfigurationBuilder().Build();

            return LogManager.Setup()
                .SetupExtensions(ext => ext.RegisterConfigSettings(config))
                .GetCurrentClassLogger();
        }

        static dynamic readConfig(string config_file)
        {
            var input = new StreamReader(config_file);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var config = deserializer.Deserialize(input);
            _logger.Trace($"Deserialized configuration:\n{JObject.FromObject(config).ToString()}");
            return config;
        }

        static async Task<Machines> createMachines(dynamic config)
        {
            var machine_confs = new List<dynamic>();

            foreach (dynamic machine_conf in config["machines"])
            {
                var prebuilt_config = new
                {
                    machine = new {
                        enabled = machine_conf.ContainsKey("enabled") ? machine_conf["enabled"] : false,
                        type = machine_conf.ContainsKey("type") ? machine_conf["type"] : "l99.driver.opcxmlda.OpcxmldaMachine, opcxmlda",
                        id = machine_conf.ContainsKey("id") ? machine_conf["id"] : Guid.NewGuid().ToString(),
                        uri = machine_conf.ContainsKey("net_uri") ? machine_conf["net_uri"] : "http://opcxml.demo-this.com/XmlDaSampleServer/Service.asmx",
                        timeout = machine_conf.ContainsKey("net_timeout_s") ? machine_conf["net_timeout_s"] : 2,
                        data = machine_conf.ContainsKey("data") ? machine_conf["data"] : new List<dynamic>(),
                        collector = machine_conf.ContainsKey("strategy_type") ? machine_conf["strategy_type"] : "l99.driver.opcxmlda.collectors.Basic01, opcxmlda",
                        collector_lua = string.Empty,
                        collector_sweep_ms = machine_conf.ContainsKey("sweep_ms") ? machine_conf["sweep_ms"] : 2000,
                        handler = machine_conf.ContainsKey("handler_type") ? machine_conf["handler_type"] : "l99.driver.opcxmlda.handlers.Native, opcxmlda",
                        shdr_port = machine_conf.ContainsKey("shdr_port") ? machine_conf["shdr_port"] : 7878,
                        shdr_verbose = machine_conf.ContainsKey("shdr_verbose") ? machine_conf["shdr_verbose"] : false,
                    },
                    broker = new
                    {
                        enabled = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("enabled")) ? machine_conf["broker"]["enabled"] : false,
                        pub_status = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("publish_status")) ? machine_conf["broker"]["publish_status"] : false,
                        pub_arrivals = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("publish_arrivals")) ? machine_conf["broker"]["publish_arrivals"] : false,
                        pub_changes = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("publish_changes")) ? machine_conf["broker"]["publish_changes"] : false,
                        pub_disco = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("publish_disco")) ? machine_conf["broker"]["publish_disco"] : false,
                        disco_base_topic = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("disco_base_topic")) ? machine_conf["broker"]["disco_base_topic"] : "fanuc",
                        ip = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("net_ip")) ? machine_conf["broker"]["net_ip"] : "127.0.0.1", 
                        port = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("net_port")) ? machine_conf["broker"]["net_port"] : 1883,
                        auto_connect = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("enabled")) ? machine_conf["broker"]["auto_connect"] : false,
                        anonymous = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("anonymous")) ? machine_conf["broker"]["anonymous"] : true,
                        user = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("user")) ? machine_conf["broker"]["user"] : "user",
                        password = (machine_conf.ContainsKey("broker") && machine_conf["broker"].ContainsKey("password")) ? machine_conf["broker"]["password"] : "password"
                    }
                };

                var built_config = new
                {
                    prebuilt_config.machine,
                    prebuilt_config.broker,
                    handler = machine_conf.ContainsKey(prebuilt_config.machine.handler)
                        ? machine_conf[prebuilt_config.machine.handler]
                        : null
                };

                _logger.Trace($"Machine configuration built:\n{JObject.FromObject(built_config).ToString()}");
                
                machine_confs.Add(built_config);
            }

            Brokers brokers = new Brokers();
            Machines machines = new Machines();
            
            foreach (var cfg in machine_confs)
            {
                _logger.Trace($"Creating machine from config:\n{JObject.FromObject(cfg).ToString()}");
                
                Broker broker = await brokers.AddAsync(cfg.machine, cfg.broker);
                Machine machine = machines.Add(cfg.machine, broker);
                machine.AddCollector(Type.GetType(cfg.machine.collector), cfg.machine.collector_sweep_ms, cfg.machine.collector_lua);
                await machine.AddHandlerAsync(Type.GetType(cfg.machine.handler), cfg.handler);
            }

            return machines;
        }
    }
}