using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters;

using System.IO;

namespace ObisoftNet.Encoders
{
    public class ObjectEncoder
    {
        public string KeyEnc = "keyenc";

        public static void EncFile(string file,ObjectEncoder obj)
        {
            using(Stream stream = File.Create(file))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    foreach (var input in obj.GetType().GetProperties())
                    {
                        try
                        {
                            string key = S6Encoder.encrypt(input.Name, obj.KeyEnc);
                            string value = S6Encoder.encrypt(input.GetValue(obj).ToString(), obj.KeyEnc);
                            writer.Write(key);
                            writer.Write(value);
                        }
                        catch { }
                    }
                    foreach (var input in obj.GetType().GetFields())
                    {
                        try
                        {
                            string key = S6Encoder.encrypt(input.Name, obj.KeyEnc);
                            string value = S6Encoder.encrypt(input.GetValue(obj).ToString(), obj.KeyEnc);
                            writer.Write(key);
                            writer.Write(value);
                        }
                        catch { }
                    }
                }
            }
        }
        private static object _parse(string value,object input)
        {
            try
            {
                if (input.GetType() == typeof(int))
                    return int.Parse(value);
                if (input.GetType() == typeof(double))
                    return double.Parse(value);
                if (input.GetType() == typeof(long))
                    return long.Parse(value);
                if (input.GetType() == typeof(string))
                    return value;
            }
            catch { }
            return null;
        }
        public static T DecFile<T>(string file,string keyenc="keyenc")
        {
            object result = typeof(T).Assembly.CreateInstance(typeof(T).FullName);
            using (Stream stream = File.OpenRead(file))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    foreach (var input in result.GetType().GetProperties())
                    {
                        try
                        {
                            string key = S6Encoder.decrypt(reader.ReadString(), keyenc);
                            string value = S6Encoder.decrypt(reader.ReadString(), keyenc);
                            object valdec = _parse(value, input.GetValue(result));
                            input.SetValue(result, valdec);
                        }
                        catch { }
                    }
                    foreach (var input in result.GetType().GetFields())
                    {
                        try
                        {
                            string key = S6Encoder.decrypt(reader.ReadString(), keyenc);
                            string value = S6Encoder.decrypt(reader.ReadString(), keyenc);
                            object valdec = _parse(value, input.GetValue(result));
                            input.SetValue(result, valdec);
                        }
                        catch { }
                    }
                }
            }
            return (T)result;
        }
    }
}
