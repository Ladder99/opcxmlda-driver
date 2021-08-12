namespace l99.driver.opcxmlda
{
    public class OpcxmldaEndpoint
    {
        public string URI => _uri;

        private string _uri = "http://opcxml.demo-this.com/XmlDaSampleServer/Service.asmx";
        
        public short ConnectionTimeout => _connectionTimeout;

        private short _connectionTimeout = 3;

        public OpcxmldaEndpoint(string uri, short connectionTimeout)
        {
            _uri = uri;
            _connectionTimeout = connectionTimeout;
        }
    }
}