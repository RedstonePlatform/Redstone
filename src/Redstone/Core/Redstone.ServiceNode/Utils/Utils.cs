namespace Redstone.ServiceNode.Utils
{
    public class Utils
    {
    }

    public class ConfigurationOptionWrapper<T>
    {
        public string Name { get; set; }
        public T Value { get; set; }

        public ConfigurationOptionWrapper(string name, T configValue)
        {
            this.Name = name;
            this.Value = configValue;
        }
    }
}
