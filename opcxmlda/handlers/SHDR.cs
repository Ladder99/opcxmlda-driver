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
        private MTConnect.Adapter _adapter;
        
        private Lua _luaState;

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

        private LuaTable _luaTable;
        private Dictionary<string, LuaFunction> _luaFunctions;

        public SHDR(Machine machine) : base(machine)
        {
            
        }

        public override async Task InitializeAsync(dynamic config)
        {
            _adapter = new Adapter(config["port"], config["verbose"]);

            _luaFunctions = new Dictionary<string, LuaFunction>();
            _luaState = new Lua();
            _luaState.LoadCLRPackage();

            Dictionary<string, string> temp_functions = new Dictionary<string, string>();
            StringBuilder temp_sb = new StringBuilder();
            temp_sb.AppendFormat(_luaModuleTemplate, config["lua_head"]);
            temp_sb.AppendLine();
            
            _adapter.AddDataItem(new Event("avail"));
            _adapter.UpdateDataItem("avail", "AVAILABLE");
            
            foreach (Dictionary<object,object> descriptor in machine["data"])
            {
                string descriptor_id = (string)descriptor.Keys.ElementAt(0);

                if (descriptor[descriptor_id]==null)
                    continue;
                
                if (!((Dictionary<object, object>)descriptor[descriptor_id]).ContainsKey("shdr"))
                    continue;
                
                var shdr = ((Dictionary<object, object>)((Dictionary<object, object>)descriptor[descriptor_id])["shdr"]);

                var di_name = (string)shdr["name"];
                var di_cat = (string)shdr["category"];
                
                if (shdr.ContainsKey("eval"))
                {
                    var di_eval = (string)shdr["eval"];
                    string function_text = string.Format(_luaFunctionTemplate, di_name, di_eval);
                    temp_functions.Add(di_name, function_text);
                }

                switch (di_cat)
                {
                    case "sample":
                        _adapter.AddDataItem(new Sample(di_name));
                        break;
                    
                    case "event":
                        _adapter.AddDataItem(new Event(di_name));
                        break;
                    
                    case "message":
                        _adapter.AddDataItem(new Message(di_name));
                        break;
                    
                    case "condition":
                        _adapter.AddDataItem(new Condition(di_name, true));
                        break;
                }
            }

            foreach (var kv in temp_functions)
            {
                temp_sb.AppendLine(kv.Value);
                temp_sb.AppendLine();
            }
            
            _luaState.DoString(temp_sb.ToString());
            _luaTable = _luaState["user"] as LuaTable;
            
            foreach (var kv in temp_functions)
            {
                var function = _luaTable?[kv.Key] as LuaFunction;
                _luaFunctions.Add(kv.Key, function);
            }
            
            _adapter.Start();
        }

        public override async Task<dynamic?> OnDataChangeAsync(Veneers veneers, Veneer veneer, dynamic? beforeChange)
        {
            _adapter.Begin();

            foreach (Dictionary<object, object> descriptor in machine["data"])
            {
                string descriptor_id = (string)descriptor.Keys.ElementAt(0);
                if (descriptor_id.Equals(veneer.Name))
                {
                    if (descriptor[descriptor_id]==null)
                        continue;
                
                    if (!((Dictionary<object, object>)descriptor[descriptor_id]).ContainsKey("shdr"))
                        continue;
                    
                    if (!((Dictionary<object, object>)((Dictionary<object, object>)descriptor[descriptor_id])["shdr"]).ContainsKey("name"))
                        continue;
                    
                    var di_name = (string)((Dictionary<object, object>)((Dictionary<object, object>)descriptor[descriptor_id])["shdr"])["name"];

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
                    
                    break;
                }
            }

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