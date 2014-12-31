using Newtonsoft.Json;
using System;
using System.IO;

namespace Lokad.Cloud.Storage
{
    public class JsonDataSerializer : IDataSerializer
    {
        public void Serialize(object instance, System.IO.Stream destinationStream, Type type)
        {
            string json = JsonConvert.SerializeObject(instance);
            StreamWriter writer = new StreamWriter(destinationStream);
            writer.Write(json);
            writer.Flush();
        }

        public object Deserialize(System.IO.Stream sourceStream, Type type)
        {
            StreamReader reader = new StreamReader(sourceStream);
            string json = reader.ReadToEnd();
            return JsonConvert.DeserializeObject(json, type);
        }
    }
}
