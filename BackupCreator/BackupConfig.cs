using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;



namespace BackupCreator
{
   [XmlRoot(ElementName = ConfigInfo.CONFIG_ELEMENT)]
   public class Config
   {
      [XmlElement("ConnectionDetails")]
      public ConnectionDetails ConnectionDetails { get; set; }

      [XmlElement("Database")]
      public string Database { get; set; }
      [XmlElement("BackupPath")]
      public string BackupPath { get; set; }
   }

   public class ConnectionDetails
   {
      [XmlElement("ServerInstance")]
      public string ServerInstance { get; set; }
      [XmlElement("ConnectTimeout")]
      public int ConnectTimeout { get; set; }
   }



   public static class ConfigManager
   {
      public static Config GetDefaultConfig()
      {
         var config = new Config
         {
            Database = "Mini",
            BackupPath = @"D:\Autobackup",
            ConnectionDetails = new ConnectionDetails
            {
               ServerInstance = @"(local)\SECOND",
               ConnectTimeout = 15
            }
         };
         return config;
      }

      public static void CreateDefaultConfigFile()
      {
         var xml = new XDocument(GetDefaultConfig().ToXElement<Config>());
         xml.Save(ConfigInfo.CONFIG_PATH);
      }

      public static Config GetConfig()
      {
         if (!File.Exists(ConfigInfo.CONFIG_PATH))
         {
            string msg = $@"'{ConfigInfo.CONFIG_PATH}' not found. For create new launch program with  key `{Arguments.Init}`";
            throw new FileNotFoundException(msg);
         }

         XDocument doc = XDocument.Load(ConfigInfo.CONFIG_PATH);
         XElement elem = doc.Element(ConfigInfo.CONFIG_ELEMENT);
         return elem.FromXElement<Config>();
      }
   }



   public static class XmlHelper
   {
      public static XElement ToXElement<T>(this object obj)
      {
         if (obj == null)
            return null;
         using (var memoryStream = new MemoryStream())
         using (TextWriter streamWriter = new StreamWriter(memoryStream))
         {
            var xmlSerializer = new XmlSerializer(typeof(T));
            xmlSerializer.Serialize(streamWriter, obj);
            return XElement.Parse(Encoding.ASCII.GetString(memoryStream.ToArray()));
         }
      }

      public static T FromXElement<T>(this XElement xElement)
      {
         if (xElement == null)
            return default(T);
         var xmlSerializer = new XmlSerializer(typeof(T));
         return (T) xmlSerializer.Deserialize(xElement.CreateReader());
      }
   }

}
