﻿using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(System.IO.File))]
[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Gameloop.Vdf.VdfSerializer))]
[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Gameloop.Vdf.VdfConvert))]
static int GetCharacterOccurancesInString(string str, char c)
{
    int count = 0;
    foreach (char ch in str) if (ch == c) count++;

    return count;
}
Console.Write("Enter Steam path (where steam.exe is located): ");
string steampath = Console.ReadLine();
if (steampath[0].ToString() == "\"")
{
    steampath = steampath.Remove(0, 1);
}
if (steampath[steampath.Length - 1].ToString() == "\"")
{
    steampath = steampath.Remove(steampath.Length - 1, 1);
}
Console.Write("Enter manifest file location (where .lua and .manifest files are located): ");
string manifestpath = Console.ReadLine();
if (manifestpath[0].ToString() == "\"")
{
    manifestpath = manifestpath.Remove(0, 1);
}
if (manifestpath[manifestpath.Length - 1].ToString() == "\"")
{
    manifestpath = manifestpath.Remove(manifestpath.Length - 1, 1);
}
string depotcache = Path.Combine(steampath, "depotcache");
string applistpath = Path.Combine(steampath, "AppList");
if (!Directory.Exists(depotcache))
{
    Directory.CreateDirectory(depotcache);
}

if (!Directory.Exists(applistpath))
{
    Directory.CreateDirectory(applistpath);
}

int applistcount = Directory.GetFiles(applistpath, "*.txt").Length;
string[] manifestfiles = Directory.GetFiles(manifestpath, "*.manifest");
string[] luafiles = Directory.GetFiles(manifestpath, "*.lua");
foreach (string file in manifestfiles)
{
    File.Copy(file, Path.Combine(depotcache, Path.GetFileName(file)), true);
}

Dictionary<string, string> keys = new();
IEnumerable<string> lines = File.ReadLines(luafiles[0]);
string appid = lines.First().Split('(', ')')[1];
JObject DLCInfo = JObject.Parse(await new HttpClient().GetStringAsync($"https://store.steampowered.com/api/appdetails?appids={appid}"));
List<string> DLCs = new List<string>();
if (DLCInfo[$"{appid}"]["data"]["dlc"] != null)
{
    DLCs = DLCInfo[$"{appid}"]["data"]["dlc"].Values<string>().ToList();
}
foreach (string line in lines)
{
    if (line.Contains("addappid") && GetCharacterOccurancesInString(line, ',') >= 2)
    {
        string[] splitString = line.Split('(', ',', ')');
        keys.Add(splitString[1], splitString[3].Replace("\"", ""));
    }
}

StreamWriter writer = File.AppendText(Path.Combine(applistpath, applistcount.ToString() + ".txt"));
writer.Write(appid);
writer.Close();
applistcount++;
List<string> appIds = new();
foreach (KeyValuePair<string, string> key in keys)
{
    appIds.Add(key.Key);
}
foreach (string dlc in DLCs)
{
    if (!appIds.Contains(dlc))
    {
        appIds.Add(dlc);
    }
}
List<string> existingAppIds = new();

foreach (string file in Directory.GetFiles(applistpath))
{
    existingAppIds.Add(File.ReadAllLines(file).First().Trim());
}

for (int i = 0; i < appIds.Count; i++)
{
    if (!existingAppIds.Contains(appIds[i]))
    {
        using (StreamWriter sw = File.AppendText(Path.Combine(applistpath, applistcount.ToString() + ".txt")))
        {
            sw.Write(appIds[i].Replace("\n", "").Trim());
            sw.Close();
            applistcount++;
        }
    }
}

try
{
    dynamic config = VdfConvert.Deserialize(File.ReadAllText(Path.Combine(steampath, "config", "config.vdf")), new VdfSerializerSettings() { MaximumTokenSize = 32768, UsesEscapeSequences = true });
    VObject root = config.Value;
    VObject software = root["Software"] as VObject;
    VObject valve = (software.ContainsKey("valve") ? software["valve"] : software["Valve"]) as VObject;
    VObject steam = valve["Steam"] as VObject;
    if (!steam.ContainsKey("depots"))
    {
        steam["depots"] = new VObject();
    }
    VObject depots = steam["depots"] as VObject;
    foreach (KeyValuePair<string, string> key in keys)
    {
        depots[key.Key] = new VObject
        {
            ["DecryptionKey"] = new VValue(key.Value)
        };
    }
    File.WriteAllText(Path.Combine(steampath, "config", "config.vdf"), VdfConvert.Serialize(config));
    Console.WriteLine("Finished. Press any key to exit...");
    Console.ReadLine();
} catch (Exception ex)
{
    Console.WriteLine("An error occurred while processing the config.vdf file: " + ex.Message);
    Console.WriteLine("Please ensure that the file exists and is not corrupted.");
    Console.WriteLine("Press any key to exit...");
    Console.ReadLine();
}