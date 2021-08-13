# opcxmlda-driver

## Overview

Fully configurable OPC XML-DA driver built on top of [base-driver](https://github.com/Ladder99/base-driver), [QuickOPC](https://www.opclabs.com/products/picoopc/227-products/quickopc), and [MTConnect .NET SDK](https://github.com/mtconnect/dot_net_sdk) libraries.  
Perfect for pulling data from Simotion drives running on Simatic Step 7 before OPC-UA was made available.

## Output

### SHDR

Handler `l99.driver.opcxmlda.handlers.SHDR, opcxmlda` outputs data in SHDR format to an MQTT broker and also is available at the TCP port defined in configuration (default: 7878).

![SHDR MQTT](docs/shdr_mqtt.gif)

![SHDR TCP](docs/shdr_tcp.gif)

### Splunk Metric

Handler `l99.driver.opcxmlda.handlers.SplunkMetric, opcxmlda` outputs data in Splunk metric format to an MQTT broker.

![SPLUNK MQTT](docs/splunk_mqtt.gif)

## Configuration

Driver configuration is maintained in the `config.yml` file.  You can read more about YAML structure [here](https://www.cloudbees.com/blog/yaml-tutorial-everything-you-need-get-started).  Multiple machines can be added and are differentiated by their `id` key.  

`enabed` - enables or disables collection and processing of machine data  
`type` - machine type maintains the OPC client  
`strategy_type` - collection strategy manages collection from the OPC server and presentation of data once its received  
`handler_type` - handler manages data formatting and output to another system  
`sweep_ms` - frequency of data collection from OPC server  
`net_uri` - path to the OPC server  
  
```yml
machines:
  - id: demo
    enabled: !!bool true
    type: l99.driver.opcxmlda.OpcxmldaMachine, opcxmlda
    strategy_type: l99.driver.opcxmlda.collectors.Basic01, opcxmlda
    handler_type: l99.driver.opcxmlda.handlers.SHDR, opcxmlda
    sweep_ms: !!int 1000
    net_uri: http://opcxml.demo-this.com/XmlDaSampleServer/Service.asmx
    net_timeout_s: !!int 3
```

The `data` section identifies individual OPC XML-DA objects.  The `shdr` section is specific to the SHDR handler to assist with transmitting the data to an MTConnect Agent.  

`shdr.name` - MTConnect DataItem name  
`shdr.category` - MTConnect DataItem category (sample,event,message,condition)  
`shdr.eval` - Lua evaluation context and pre-processor

```yml
    data:
      - Dynamic/Analog Types/Double:
          shdr:
            name: analog_double
            category: sample
            eval: |
              --print(JObject.FromObject(dataitems):ToString());
              
              if current_value == 'UNAVAILABLE' then
                print(name .. ' is available')
                return new_value
              else
                print(name .. ' = ' .. current_value .. ' + ' .. new_value)
                return current_value + new_value
              end
      - Dynamic/Analog Types/Int:
          shdr:
            name: analog_int
            category: sample
            eval: |
              print('analog_int value: ' .. dataitems["analog_int"].Value);
              return new_value;
      - Dynamic/Analog Types/Double[]:
      - Static/Simple Types/String:
          shdr:
            name: simple_string
            category: message
            eval: |
              random_code = math.random(100,999);
              dataitem.Code = tostring(random_code);
              return new_value;
      - Static/Simple Types/DateTime:
          shdr:
            name: simple_datetime
            category: event
      - Static/ArrayTypes/Object[]:
      - Dynamic/Analog Types/Fools/Guildenstern:
      - Dynamic/Enumerated Types/Gems:
      - SomeUnknownItem:
```

The `broker` section defines the MQTT broker settings.  

```yml
    broker:
      enabled: !!bool true
      net_ip: 10.20.30.102
      net_port: !!int 1883
      auto_connect: !!bool true
      publish_status: !!bool true
      publish_arrivals: !!bool true
      publish_changes: !!bool true
      publish_disco: !!bool true
      disco_base_topic: opcxmlda
```

Handler specific sections provide additional configuration that might be handler specific.  In this case the SHDR handler identifies the MTConnect Adapter to listen on as well as additional MTConnect DataItems that might not be available at the OPC server.

```yml
    l99.driver.opcxmlda.handlers.SHDR, opcxmlda:
      port: !!int 7878
      verbose: !!bool true
      lua_head: |
        luanet.load_assembly 'System';
        luanet.load_assembly 'Newtonsoft.Json';
        JObject = luanet.import_type 'Newtonsoft.Json.Linq.JObject';
      data:
        - avail:
            shdr:
              name: avail
              category: event
              eval: |
                return "AVAILABLE";
```

## Lua Evaluation Context

The Lua script generated from the `config.yml` file is shown below.  A function is defined for every MTConnect DataItem.  

### Function Signature

`this` - Lua table  
`name` - DataItem name  
`current_value` - DataItem current value  
`new_value` - new value arriving from OPC server  
`dataitem` - reference to the `MTCDataItem` object  
`dataitems` - reference to the `MTCDataItem` collection  

```lua
luanet.load_assembly 'System';
luanet.load_assembly 'Newtonsoft.Json';
JObject = luanet.import_type 'Newtonsoft.Json.Linq.JObject';

user =  {}

function user:avail(this, name, current_value, new_value, dataitem, dataitems)
    return \"AVAILABLE\";
end

function user:analog_double(this, name, current_value, new_value, dataitem, dataitems)
    --print(JObject.FromObject(dataitems):ToString());
    
    if current_value == 'UNAVAILABLE' then
      print(name .. ' is available')
      return new_value
    else
      print(name .. ' = ' .. current_value .. ' + ' .. new_value)
      return current_value + new_value
    end
end

function user:analog_int(this, name, current_value, new_value, dataitem, dataitems)
    print('analog_int value: ' .. dataitems[\"analog_int\"].Value);
    return new_value;
end

function user:simple_string(this, name, current_value, new_value, dataitem, dataitems)
    random_code = math.random(100,999);
    dataitem.Code = tostring(random_code);
    return new_value;
end
```