using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Xml;
using System.IO;
using System.Data;
using System.Collections;
using System.Resources;
using System.Windows.Media;

namespace RandomizerTracker
{
    // Implementing Translation as in the ManicJamie thingy
    public class Translation
    {
        public static Dictionary<string, string> TranslateRoomToDescriptive;

        public static void ProcessTranslationXML()
        {
            TranslateRoomToDescriptive = new Dictionary<string, string>();

            if (!Properties.Settings.Default.usingTranslator)
            {
                return;
            }
            if (!File.Exists(Properties.Settings.Default.translatorfilepath))
            {
                return;
            }


            XmlDocument TranslatorXML = new XmlDocument();
            TranslatorXML.Load(Properties.Settings.Default.translatorfilepath);

            foreach (XmlNode node in TranslatorXML.SelectNodes("Dictionary/Entry"))
            {
                TranslateRoomToDescriptive.Add(node["oldName"].InnerText, node["newName"].InnerText);
            }
        }

        public static string TranslateRoomName(string oldName)
        {
            if (oldName.Contains('-'))
            {
                if (TranslateRoomToDescriptive.TryGetValue(oldName, out string newName_special))
                {
                    return newName_special;
                }
                string[] words = oldName.Split('-');
                if (!TranslateRoomToDescriptive.TryGetValue(words[0], out string newName))
                {
                    newName = words[0];
                }
                return newName + '-' + words[1];
            }
            else
            {
                if (!TranslateRoomToDescriptive.TryGetValue(oldName, out string newName))
                {
                    newName = oldName;
                }
                return newName;
            }
        }

        public static string TranslateTransitionName(string oldName)
        {
            string[] words = oldName.Split('[');
            if (words.Count() < 2)
            {
                return oldName;
            }
            return TranslateRoomName(words[0]) + '[' + words[1];
        }

    }
}
