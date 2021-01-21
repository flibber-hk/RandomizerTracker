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
using static RandomizerTracker.Translation;

namespace RandomizerTracker
{
    public static class XmlData
    {
        public static List<string> Items;
        public static Dictionary<string, string> itemToArea;
        public static Dictionary<string, string> itemToRoom;
        public static Dictionary<string, string> itemToPool;
        public static Dictionary<string, bool> checkIfShopItem;

        public static List<string> areaTransitions;
        public static Dictionary<string, string> transitionToArea;
        public static Dictionary<string, bool> hasBenchArea;
        public static HashSet<string> areas;

        public static List<string> roomTransitions;
        public static Dictionary<string, string> transitionToRoom;
        public static Dictionary<string, string> roomToArea;
        public static Dictionary<string, bool> isOneWay;
        public static Dictionary<string, bool> hasBench;
        public static HashSet<string> rooms;

        public static Dictionary<string, Color> AreaToColor;
        public static Dictionary<string, string> AltRoomNames;

        public static Dictionary<string, string> ShopToArea;
        public static Dictionary<string, string> ShopToRoom;
        public static Dictionary<string, string> startLocationArea;
        public static Dictionary<string, string> startLocationRoom;

        public static void ProcessXmls()
        {
            ProcessTranslationXML();

            XmlDocument itemXml = LoadEmbeddedXml("RandomizerTracker.Resources.items.xml");
            Items = new List<string>();
            itemToArea = new Dictionary<string, string>();
            itemToRoom = new Dictionary<string, string>();
            itemToPool = new Dictionary<string, string>();
            checkIfShopItem = new Dictionary<string, bool>();
            foreach (XmlNode node in itemXml.SelectNodes("randomizer/item"))
            {
                string itemName = node.Attributes?["name"].InnerText;
                Items.Add(itemName);
                itemToArea.Add(itemName, node["areaName"].InnerText);
                itemToRoom.Add(itemName, TranslateRoomName(node["sceneName"].InnerText));
                itemToPool.Add(itemName, node["pool"].InnerText);
                checkIfShopItem.Add(itemName, node["type"].InnerText == "Shop");
            }

            XmlDocument areaXml = LoadEmbeddedXml("RandomizerTracker.Resources.areas.xml");
            areaTransitions = new List<string>();
            transitionToArea = new Dictionary<string, string>();
            hasBenchArea = new Dictionary<string, bool>();
            areas = new HashSet<string>();
            foreach (XmlNode node in areaXml.SelectNodes("randomizer/transition"))
            {
                string areaTransitionName = TranslateTransitionName(node.Attributes?["name"].InnerText);
                string areaName = node["areaName"].InnerText;
                areaTransitions.Add(areaTransitionName);
                transitionToArea.Add(areaTransitionName, areaName);
                hasBenchArea[areaName] = node["hasBench"].InnerText == "true";
                areas.Add(areaName);
            }

            XmlDocument roomXml = LoadEmbeddedXml("RandomizerTracker.Resources.rooms.xml");
            roomTransitions = new List<string>();
            transitionToRoom = new Dictionary<string, string>();
            roomToArea = new Dictionary<string, string>();
            isOneWay = new Dictionary<string, bool>();
            hasBench = new Dictionary<string, bool>();
            rooms = new HashSet<string>();
            foreach (XmlNode node in roomXml.SelectNodes("randomizer/transition"))
            {
                string roomTransitionName = TranslateTransitionName(node.Attributes?["name"].InnerText);
                string roomName = TranslateRoomName(node["sceneName"].InnerText);
                roomTransitions.Add(roomTransitionName);
                transitionToRoom.Add(roomTransitionName, roomName);
                roomToArea[roomName] = node["areaName"].InnerText;
                isOneWay.Add(roomTransitionName, node?["oneWay"]?.InnerText?.Any(c => c == '1' || c == '2') == true);
                hasBench[roomName] = node["hasBench"].InnerText == "true";
                rooms.Add(roomName);
            }

            XmlDocument colorsXml = LoadFolderXml("colors.xml");
            AreaToColor = new Dictionary<string, Color>();
            foreach (XmlNode node in colorsXml.SelectNodes("randomizer/area"))
            {
                byte r = byte.Parse(node["r"].InnerText, System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(node["g"].InnerText, System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(node["b"].InnerText, System.Globalization.NumberStyles.HexNumber);
                AreaToColor.Add(node.Attributes["name"].InnerText, Color.FromRgb(r, g, b));
            }


            XmlDocument shopHelperXml = LoadEmbeddedXml("RandomizerTracker.Resources.shopHelper.xml");
            ShopToArea = new Dictionary<string, string>();
            ShopToRoom = new Dictionary<string, string>();
            foreach (XmlNode node in shopHelperXml.SelectNodes("randomizer/item"))
            {
                ShopToArea[node.Attributes["name"].InnerText] = node["areaName"].InnerText;
                ShopToRoom[node.Attributes["name"].InnerText] = TranslateRoomName(node["sceneName"].InnerText);
            }

            XmlDocument startLocationXml = LoadEmbeddedXml("RandomizerTracker.Resources.startlocations.xml");
            startLocationArea = new Dictionary<string, string>();
            startLocationRoom = new Dictionary<string, string>();
            foreach (XmlNode node in startLocationXml.SelectNodes("randomizer/start"))
            {
                startLocationArea[node.Attributes["name"].InnerText] = node["areaName"].InnerText;
                startLocationRoom[node.Attributes["name"].InnerText] = TranslateRoomName(node["sceneName"].InnerText);
            }

            AltRoomNames = new Dictionary<string, string>();
            XmlDocument altroomsXml = LoadFolderXml("altroomnames.xml");
            if (!Properties.Settings.Default.usingTranslator)
            {
                foreach (XmlNode node in altroomsXml.SelectNodes("randomizer/room"))
                {
                    AltRoomNames[node.Attributes["name"].InnerText] = node["altName"].InnerText;
                }
            }
        }

        private static XmlDocument LoadEmbeddedXml(string filepath)
        {
            Stream stream = RandomizerTracker.instance.GetType().Assembly.GetManifestResourceStream(filepath);
            XmlDocument doc = new XmlDocument();
            doc.Load(stream);
            stream.Dispose();
            return doc;
        }

        private static XmlDocument LoadFolderXml(string name)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load($"Resources/{name}");
            return doc;
        }
    }
}
