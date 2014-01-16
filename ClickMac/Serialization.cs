using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization.Json;
using System.Xml;
using System.Xml.Serialization;

namespace Kamahl.Common
{
    /// <summary>
    /// Easy Generic Serializer methods.
    /// </summary>
    public static class Serialization
    {
        /// <summary>
        /// Load using an XMLSerializer.  Recommend using ReadObject instead.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file"></param>
        /// <returns></returns>
        public static T Load<T>(string file)
        {
            T ret;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            XmlReader xmlReader = null;
            try
            {
                xmlReader = XmlReader.Create(file);
                ret = (T)(xmlSerializer.Deserialize(xmlReader));
            }
            finally
            {
                if (xmlReader != null)
                {
                    xmlReader.Close();
                }
            }
            return ret;
        }

        [Obsolete("Use the newer DataContract Serializers Instead",true)]
        public static void Save<T>(string file, T obj)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            XmlWriter xmlWriter = XmlWriter.Create(file);
            xmlSerializer.Serialize(xmlWriter, obj);
            xmlWriter.Close();
        }

        public enum SerializationType { xml, json, xmlz, jsonz, binary, soap };
        private static SerializationType ImplyType(string fileName)
        {
            SerializationType type = SerializationType.xml;
            if (Path.GetExtension(fileName) == ".json")
                type = SerializationType.json;
            if (Path.GetExtension(fileName) == ".jzon")
                type = SerializationType.jsonz;
            if (Path.GetExtension(fileName) == ".zxml")
                type = SerializationType.xmlz;
            if (Path.GetExtension(fileName) == ".bin")
                type = SerializationType.binary;
            if (Path.GetExtension(fileName) == ".dat")
                type = SerializationType.soap;

            return type;
        }

        public static void WriteObject<T>(string fileName, T obj)
        {
            SerializationType type = ImplyType(fileName);
            WriteObject(fileName, obj, type);
        }

  

        public static void WriteObject<T>(string fileName, T obj, SerializationType type)
        {
            if (type == SerializationType.binary || type == SerializationType.soap)
            {
                WriteBinaryObject(fileName, obj, type);
                return;
            }
            FileStream writer = new FileStream(fileName + ".tmp", FileMode.Create);
            dynamic ser = null;
            if (type == SerializationType.xml || type == SerializationType.xmlz)
                ser = new DataContractSerializer(typeof(T));
            else if (type == SerializationType.json || type == SerializationType.jsonz)
                ser = new DataContractJsonSerializer(typeof(T));
            if (type == SerializationType.jsonz || type == SerializationType.xmlz)
            {
                GZipStream zwriter = new GZipStream(writer, CompressionMode.Compress);
                ser.WriteObject(zwriter, obj);
                zwriter.Close();
            }
            else
            {
                ser.WriteObject(writer, obj);
                writer.Close();
            }
            if (File.Exists(fileName))
                File.Delete(fileName);
            File.Move(fileName + ".tmp", fileName);
        }

        public static void WriteBinaryObject<T>(string fileName, T obj, SerializationType type)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, obj);
            stream.Close();

        }

        /// <summary>
        /// Try to Read the file, and return null if it throws a SerializationException.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fileName"></param>
        /// <returns>Deserialized value or null</returns>
        public static T TryReadObject<T>(string fileName)
        {
            if (!File.Exists(fileName))
            {
                if (File.Exists(Path.ChangeExtension(fileName, "xml")))
                    fileName = Path.ChangeExtension(fileName, "xml");
                else if (File.Exists(Path.ChangeExtension(fileName, "json")))
                    fileName = Path.ChangeExtension(fileName, "json");
                else if (File.Exists(Path.ChangeExtension(fileName, "bin")))
                    fileName = Path.ChangeExtension(fileName, "bin");
                else if (File.Exists(Path.ChangeExtension(fileName, "dat")))
                    fileName = Path.ChangeExtension(fileName, "dat");
            }
            try
            {
                return ReadObject<T>(fileName);
            }
            catch (SerializationException)
            {
                return default(T);
            }
        }

        public static T ReadObject<T>(string fileName)
        {
            SerializationType type = ImplyType(fileName);
            return ReadObject<T>(fileName, type);
        }

        public static T ReadObject<T>(string fileName, SerializationType type)
        {
            if (!File.Exists(fileName))
                return default(T);
            if (type == SerializationType.binary || type == SerializationType.soap)
            {
                return ReadBinaryObject<T>(fileName);
            }
            FileStream fs = new FileStream(fileName, FileMode.Open);
            GZipStream gs = null;
            if (type == SerializationType.jsonz || type == SerializationType.xmlz)
                gs = new GZipStream(fs, CompressionMode.Decompress);
            XmlDictionaryReader reader = null;
            T deserializedobj = default(T);
            try
            {
                dynamic ser = null;
                if (type == SerializationType.xml || type == SerializationType.xmlz)
                {
                    reader = XmlDictionaryReader.CreateTextReader(gs == null ? (Stream)fs : gs, new XmlDictionaryReaderQuotas());
                    ser = new DataContractSerializer(typeof(T));
                }
                else if (type == SerializationType.json || type == SerializationType.jsonz)
                {
                    ser = new DataContractJsonSerializer(typeof(T));
                    reader = JsonReaderWriterFactory.CreateJsonReader(gs == null ? (Stream)fs : gs, new XmlDictionaryReaderQuotas());
                }
                deserializedobj = (T)ser.ReadObject(reader, true);
            }
            catch (XmlException)
            {
                deserializedobj = default(T);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                else if (gs != null)
                    gs.Close();
                else
                    fs.Close();
            }
            return deserializedobj;
        }
                
        private static T ReadBinaryObject<T>(string fileName)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(fileName , FileMode.Open, FileAccess.Read, FileShare.Read);
            T obj = (T)formatter.Deserialize(stream);
            stream.Close();
            return obj;
        }
        /// <summary>
        /// Converts <paramref name="obj"/> into it's JSON representation.
        /// </summary>
        /// <typeparam name="T">A serializable type.</typeparam>
        /// <param name="obj">The object to be serialized.</param>
        /// <param name="KnownTypes">A list of known types to be serialized.</param>
        /// <returns>JSON representation of <paramref name="obj"/>.</returns>
        public static string DumpToJson<T>(T obj, params Type[] KnownTypes)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(typeof(T), KnownTypes);
                ser.WriteObject(ms, obj);
                return System.Text.Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static T LoadFromJson<T>(string json)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var ser = new DataContractJsonSerializer(typeof(T));
            var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, new XmlDictionaryReaderQuotas());
            return (T)ser.ReadObject(reader, true);
        }
        public static object LoadFromJson(string json, Type type)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var ser = new DataContractJsonSerializer(type);
            var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, new XmlDictionaryReaderQuotas());
            return ser.ReadObject(reader, true);
        }
    }
}