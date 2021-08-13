using System.Collections.Generic;
using System.Threading.Tasks;
using l99.driver.@base;
using System.Linq;
using System.Text;
using MTConnect;
using NLua;

namespace l99.driver.opcxmlda.handlers
{
    public class SHDR: Handler
    {
        private dynamic _handlerConfig;
        
        private MTConnect.Adapter _adapter;
        
        private Lua _luaState;
        private LuaTable _luaTable;
        private Dictionary<string, LuaFunction> _luaFunctions;
        private Dictionary<string, string> _tempFunctions;

        private string _luaModuleTemplate =
@"
{0}

user =  {{}}
";
        
        private string _luaFunctionTemplate =
@"
function user:{0}(this, name, current_value, new_value, dataitem, dataitems)
    {1}
end
";

        public SHDR(Machine machine) : base(machine)
        {
            
        }

        private void addDataItem(string category, string name)
        {
            switch (category.ToLower())
            {
                case "sample":
                    _adapter.AddDataItem(new Sample(name));
                    break;
                    
                case "event":
                    _adapter.AddDataItem(new Event(name));
                    break;
                    
                case "message":
                    _adapter.AddDataItem(new Message(name));
                    break;
                    
                case "condition":
                    _adapter.AddDataItem(new Condition(name, true));
                    break;
            }
        }

        private string getShdrEvalText(Dictionary<object, object> shdr)
        {
            if (shdr.ContainsKey("eval"))
                return string.Format(_luaFunctionTemplate, (string)shdr["name"], (string)shdr["eval"]);

            return null;
        }

        private bool hasShdrSection(Dictionary<object,object> descriptor)
        {
            string descriptor_id = (string)descriptor.Keys.ElementAt(0);
            
            if (descriptor[descriptor_id]==null)
                return false;
                
            if (!((Dictionary<object, object>)descriptor[descriptor_id]).ContainsKey("shdr"))
                return false;

            return true;
        }

        private string getShdrSectionName(Dictionary<object, object> descriptor)
        {
            if (!hasShdrSection(descriptor))
                return null;
            
            string descriptor_id = (string)descriptor.Keys.ElementAt(0);

            if (!((Dictionary<object, object>)((Dictionary<object, object>)descriptor[descriptor_id])["shdr"]).ContainsKey("name"))
                return null;
            
            return (string)((Dictionary<object, object>)((Dictionary<object, object>)descriptor[descriptor_id])["shdr"])["name"];
        }

        public Dictionary<object, object> getShdrSection(Dictionary<object,object> descriptor)
        {
            string descriptor_id = (string)descriptor.Keys.ElementAt(0);
            return ((Dictionary<object, object>)((Dictionary<object, object>)descriptor[descriptor_id])["shdr"]);
        }

        public void addDataItems(dynamic section)
        {
            foreach (Dictionary<object,object> descriptor in section)
            {
                if (!hasShdrSection(descriptor))
                    continue;

                var shdr = getShdrSection(descriptor);

                var eval_text = getShdrEvalText(shdr);
                if(!string.IsNullOrEmpty(eval_text))
                    _tempFunctions.Add((string)shdr["name"], eval_text);
                
                addDataItem((string)shdr["category"], (string)shdr["name"]);
            }
        }
        
        public override async Task InitializeAsync(dynamic config)
        {
            _handlerConfig = config;
            _adapter = new Adapter(config["port"], config["verbose"]);

            _tempFunctions = new Dictionary<string, string>();
            _luaFunctions = new Dictionary<string, LuaFunction>();
            _luaState = new Lua();
            _luaState.LoadCLRPackage();

            StringBuilder temp_sb = new StringBuilder();
            temp_sb.AppendFormat(_luaModuleTemplate, config["lua_head"]);
            temp_sb.AppendLine();

            if(config.ContainsKey("data"))
                addDataItems(config["data"]);
            
            if(machine["data"]!=null)
                addDataItems(machine["data"]);
            
            foreach (var kv in _tempFunctions)
            {
                temp_sb.AppendLine(kv.Value);
                temp_sb.AppendLine();
            }
            
            _luaState.DoString(temp_sb.ToString());
            _luaTable = _luaState["user"] as LuaTable;
            
            foreach (var kv in _tempFunctions)
            {
                var function = _luaTable?[kv.Key] as LuaFunction;
                _luaFunctions.Add(kv.Key, function);
            }
            
            _adapter.Start();
        }

        private bool processIncomingData(Veneer veneer, dynamic section)
        {
            foreach (Dictionary<object, object> descriptor in section)
            {
                string descriptor_id = (string)descriptor.Keys.ElementAt(0);
                if (descriptor_id.Equals(veneer.Name))
                {
                    var di_name = getShdrSectionName(descriptor);

                    if (string.IsNullOrEmpty(di_name))
                        continue;

                    var new_value = veneer.LastArrivedValue.value;
                    
                    if (_luaFunctions.ContainsKey(di_name))
                    {
                        object[] temp_value = _luaFunctions[di_name] 
                            .Call(null, 
                                _luaTable, 
                                di_name, 
                                _adapter.GetDataItemValue(di_name), 
                                new_value, 
                                _adapter.GetDataItem(di_name),
                                _adapter.DataItemsDictionary);
                        
                        new_value = temp_value.Length > 0 ? temp_value[0] : "UNAVAILABLE";
                    }
                    
                    _adapter.UpdateDataItem(di_name, new_value);
                    
                    return true;
                }
            }

            return false;
        }

        public override async Task<dynamic?> OnDataChangeAsync(Veneers veneers, Veneer veneer, dynamic? beforeChange)
        {
            _adapter.Begin();

            bool processed = false;
            
            if(_handlerConfig.ContainsKey("data"))
                processed = processIncomingData(veneer, _handlerConfig["data"]);
            
            if(!processed && machine["data"]!=null)
                processIncomingData(veneer, machine["data"]);
            
            return null;
        }
        
        protected override async Task afterSweepCompleteAsync(Machine machine, dynamic? onSweepComplete)
        {
            var sb = _adapter.SendChanged();
            
            if (sb.Length == 0)
                return;
            
            var topic = $"opcxmlda/{machine.Id}/shdr";
            string payload = sb.ToString();
            await machine.Broker.PublishChangeAsync(topic, payload);
        }
    }
}