using System.Text;
using System.Xml.Serialization;
using System;
using System.Runtime.InteropServices;
using OldWorldTools.Models.WFRPCharacter;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace OldWorldTools.API
{
    public static class HelperMethods
    {
        static Random random = new Random();

        public static List<string> SeparateCSV(string csvContents)
        {
            var strings = csvContents.Split(',');

            List<string> result = new List<string>();

            foreach (var s in strings)
            {
                result.Add(s);
            }

            return result;
        }

        public static List<string> SeparateAndFormatCSV(string csvContents)
        {
            var strings = csvContents.Split(',');

            List<string> result = new List<string>();

            foreach (var s in strings)
            {
                result.Add(AddSpacesToSentence(s));
            }

            return result;
        }

        public static string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ' && text[i - 1] != '(' && text[i - 1] != '/' || text[i] == '(')
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        public static T RandomEnumValue<T>()
        {
            var v = Enum.GetValues(typeof(T));
            return (T)v.GetValue(random.Next(v.Length));
        }

        public static T DeserializeXMLFileToObject<T>(string XmlFilename)
        {
            T returnObject = default(T);
            if (string.IsNullOrEmpty(XmlFilename)) return default(T);

            try
            {
                StreamReader xmlStream = new StreamReader(XmlFilename);
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                returnObject = (T)serializer.Deserialize(xmlStream);
            }
            catch (Exception ex)
            {
                //handle exceptions!
            }
            return returnObject;
        }
    }
}
