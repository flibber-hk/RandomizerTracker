using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static RandomizerTracker.XmlData;

namespace RandomizerTracker
{
    public static class RandomizerData
    {
        public static bool areaRandomizer;
        public static bool roomRandomizer;

        public static Dictionary<string, string> FoundItemsToLocations;
        public static Dictionary<string, string> FoundTransitions;
        public static Dictionary<string, string> FoundOneWay;
        public static List<string> randomizedItems;
        public static Dictionary<string, bool> randomizedPools;
        public static HashSet<string> helperLocations;
        
        public static HashSet<string> checkedLocations;
        public static HashSet<string> exploredTransitions;
        public static HashSet<string> exploredRooms;

        public static string startLocation;

        public static void GetRandomizerData()
        {
            FoundItemsToLocations = new Dictionary<string, string>();
            FoundTransitions = new Dictionary<string, string>();
            FoundOneWay = new Dictionary<string, string>();

            if (!File.Exists(Properties.Settings.Default.filepath))
            {
                System.Windows.Forms.MessageBox.Show("Error: No tracker log found at the specified filepath.");
                return;
            }

            string[] data = File.ReadAllLines(Properties.Settings.Default.filepath);
            if (data.FirstOrDefault(line => line.StartsWith("Mode")) is string mode)
            {
                areaRandomizer = mode.Contains("Area Randomizer");
                roomRandomizer = mode.Contains("Room Randomizer");
            }
            (string, string)[] pools =
            {
                ("Dreamer", "Dreamers: "),
                ("Skill", "Skills: "),
                ("Charm", "Charms: "),
                ("Key", "Keys: "),
                ("Mask", "Mask shards: "),
                ("Vessel", "Vessel fragments: "),
                ("Ore", "Pale ore: "),
                ("Notch", "Charm notches: "),
                ("Geo", "Geo chests: "),
                ("Egg", "Rancid eggs: "),
                ("Relic", "Relics: "),
                ("Map", "Maps: "),
                ("Stag", "Stags: "),
                ("Grub", "Grubs: "),
                ("Root", "Whispering roots: "),
                ("Rock", "Geo rocks: "),
                ("Soul", "Soul totems: "),
                ("Lore", "Lore tablets: "),
                ("Cocoon", "Lifeblood cocoons: "),
                ("PalaceSoul", "Palace totems: "),
                ("Flame", "Grimmkin flames: "),
                ("Essence_Boss", "Boss essence: "),
                ("Cursed", "Cursed: "),
                ("Cursed", "Focus: "),
                ("CustomClaw", "Broken claw: ")
            };
            randomizedPools = new Dictionary<string, bool>();
            foreach (var pair in pools)
            {
                if (data.FirstOrDefault(line => line.StartsWith(pair.Item2)) is string poolBool)
                {
                    randomizedPools[pair.Item1] = poolBool.Contains(true.ToString());
                }
                else randomizedPools[pair.Item1] = false;
            }
            randomizedItems = Items.Where(item => randomizedPools.ContainsKey(itemToPool[item]) && randomizedPools[itemToPool[item]]).ToList();
            if (randomizedPools["CustomClaw"] && randomizedPools["Skill"])
            {
                randomizedItems.Remove("Mantis Claw");
            }

            exploredRooms = new HashSet<string>();
            exploredTransitions = new HashSet<string>();
            checkedLocations = new HashSet<string>();

            foreach (string line in data)
            {
                if (line.StartsWith("Start location: "))
                {
                    startLocation = string.Empty;
                    if (areaRandomizer)
                    {
                        startLocationArea.TryGetValue(line.Remove(0, 16).Trim(), out startLocation);
                    }
                    else if (roomRandomizer)
                    {
                        startLocationRoom.TryGetValue(line.Remove(0, 16).Trim(), out startLocation);
                    }
                }
                if (line.StartsWith("ITEM"))
                {
                    string[] words = line.Split('{', '}');
                    if (words.Length < 4)
                    {
                        continue;
                    }
                    FoundItemsToLocations[words[1]] = words[3];
                    //listBox1.Items.Add($"{words[1]}, {words[3]}");
                    if (randomizedItems.Contains(words[3]))
                    {
                        checkedLocations.Add(words[3]);
                    }
                }
                if (line.StartsWith("TRANSITION"))
                {
                    string[] words = line.Split('{', '}');
                    if (words.Length < 4)
                    {
                        continue;
                    }
                    FoundTransitions[words[1]] = words[3];

                    exploredTransitions.Add(words[1]);
                    exploredTransitions.Add(words[3]);
                    if (areaRandomizer)
                    {
                        if (transitionToArea.TryGetValue(words[1], out string val1))
                        {
                            exploredRooms.Add(val1);
                        }
                        if (transitionToArea.TryGetValue(words[3], out string val3))
                        {
                            exploredRooms.Add(val3);
                        }
                    }
                    else if (roomRandomizer)
                    {
                        if (transitionToRoom.TryGetValue(words[1], out string val1))
                        {
                            exploredRooms.Add(val1);
                        }
                        if (transitionToRoom.TryGetValue(words[3], out string val3))
                        {
                            exploredRooms.Add(val3);
                        }
                    }

                    if (isOneWay.TryGetValue(words[1], out bool oneWay) && !oneWay)
                    {
                        FoundTransitions[words[3]] = words[1];
                    }
                    else
                    {
                        FoundOneWay[words[3]] = words[1];
                    }
                }
            }


            // helper log
            helperLocations = new HashSet<string>();

            if (!File.Exists(Properties.Settings.Default.helperfilepath))
            {
                System.Windows.Forms.MessageBox.Show("Error: No helper log found at the specified filepath.");
                return;
            }

            string[] helperdata = File.ReadAllLines(Properties.Settings.Default.helperfilepath);

            int helpreadmode = 0;
            foreach (string line in helperdata)
            {
                if (line.StartsWith("REACHABLE ITEM LOCATIONS"))
                {
                    helpreadmode = 1;
                }
                else if (line.StartsWith("REACHABLE TRANSITIONS"))
                {
                    helpreadmode = 2;
                }
                else if (line.StartsWith(" - "))
                {
                    if (helpreadmode == 1)
                    {
                        if (areaRandomizer)
                        {
                            if (itemToArea.TryGetValue(line.Remove(0, 3).Trim().Replace(' ', '_'), out String val))
                            {
                                helperLocations.Add(val);
                            }
                            else if (ShopToArea.TryGetValue(line.Remove(0, 3).Trim(), out String valS))
                            {
                                helperLocations.Add(valS);
                            }
                        }
                        else if (roomRandomizer)
                        {
                            if (itemToRoom.TryGetValue(line.Remove(0, 3).Trim().Replace(' ', '_'), out String val))
                            {
                                helperLocations.Add(val);
                            }
                            else if (ShopToRoom.TryGetValue(line.Remove(0, 3).Trim(), out String valS))
                            {
                                helperLocations.Add(valS);
                            }
                        }
                    }
                    else if (helpreadmode == 2)
                    {
                        if (areaRandomizer)
                        {
                            if (transitionToArea.TryGetValue(line.Remove(0, 3).Trim().Replace(' ', '_'), out String val))
                            {
                                helperLocations.Add(val);
                            }
                        }
                        else if (roomRandomizer)
                        {
                            if (transitionToRoom.TryGetValue(line.Remove(0, 3).Trim().Replace(' ', '_'), out String val))
                            {
                                helperLocations.Add(val);
                            }
                        }
                    }
                }

                else if (line.StartsWith("CHECKED"))
                {
                    helpreadmode = 0;
                    break;
                }
            }

            // helperLocations.Add("Tutorial_01");
        }

        public static bool CheckLocationFound(string location)
        {
            return FoundItemsToLocations.ContainsValue(location);
        }
    }
}
