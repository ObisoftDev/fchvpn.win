using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace ObisoftNet.Encoders
{
    public class S6Encoder
    {
        public static char CryptoChar(char value)
        {
            string map = "0123456789abcd3fghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ@./=#$%&:,;_-|";
            if (map.Contains(value))
            {
               int i = map.IndexOf(value);
               return map[map.Length - 1 - i];
            }
            return value;
        }
        public static string _enc(string text)
        {
            string result = "";
            foreach(var ch in text)
            {
                result += CryptoChar(ch);
            }
            return result;
        }
        public static string Decrypt(string text){
        	string result="";
        	for(int i = 0;i<text.Length;i+=2){
        		result += CryptoChar(text[i]);
        	}
        	return result;
        }
        public string Enc(string text) => _enc(text);
        public static string encrypt(string text, string key = "keytoenc")
        {
            if (key.Length > 10)
                throw new Exception("LA KEY SOLO PUEDE TENER 9 CARACTERES");
            string result = _enc(text);
            string keyenc = _enc(key);
            string keylenenc = _enc(keyenc.Length.ToString());
            return keylenenc + result + keyenc;
        }
        public static string decrypt(string text, string key = "keytoenc")
        {
            if (key.Length > 10)
                throw new Exception("LA KEY SOLO PUEDE TENER 9 CARACTERES");
            try
            {
                int keylen = int.Parse(CryptoChar(text[0]).ToString());
                int indexkey = text.Length - keylen;
                string keyintext = _enc(text.Substring(indexkey));
                if (keyintext == key)
                {
                    return _enc(text.Remove(indexkey).Substring(1));
                }
                else
                {
                    throw new Exception("Key No Valida!");
                }
            }
            catch { }
            return text;
        }
    }
}
