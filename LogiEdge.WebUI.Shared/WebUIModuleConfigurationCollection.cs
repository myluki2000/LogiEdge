namespace LogiEdge.WebUI.Shared
{
    public class WebUIModuleConfigurationCollection : List<IWebUIModuleConfiguration>
    {
        public WebUIModuleConfigurationCollection()
        {
        }

        public WebUIModuleConfigurationCollection(IEnumerable<IWebUIModuleConfiguration> collection) : base(collection)
        {
        }

        public WebUIModuleConfigurationCollection(int capacity) : base(capacity)
        {
        }
    }
}
