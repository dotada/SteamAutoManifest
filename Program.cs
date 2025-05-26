static int GetCharacterOccurancesInString(string str, char c)
{
    int count = 0;
    foreach (char ch in str)
    {
        if (ch == c)
        {
            count++;
        }
    }
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
foreach (string line in lines)
{
    if (line.Contains("addappid") && GetCharacterOccurancesInString(line, ',') >= 2)
    {
        string[] splitString = line.Split('(', ',', ')');
        keys.Add(splitString[1], splitString[3].Replace("\"", ""));
    }
}

string appid = lines.First().Split('(', ')')[1];
StreamWriter writer = File.AppendText(Path.Combine(applistpath, applistcount.ToString() + ".txt"));
writer.Write(appid);
writer.Close();
applistcount++;

Console.Write("Enter amount of DLCs to enter, if any: ");
string dlcinput = Console.ReadLine();
List<string> appIds = new();
if (!String.IsNullOrWhiteSpace(dlcinput) && int.Parse(dlcinput) > 0)
{
    for (int i = 0; i < int.Parse(dlcinput); i++)
    {
        Console.Write("Enter DLC AppID: ");
        string dlcAppId = Console.ReadLine().Replace("\n", "").Trim();
        if (String.IsNullOrWhiteSpace(dlcAppId)) break;
        if (!appIds.Contains(dlcAppId))
        {
            appIds.Add(dlcAppId);
        }
        else
        {
            Console.WriteLine("DLC AppID already exists, please enter a unique AppID.");
            i--;
        }
    }
}

for (int i = 0; i < appIds.Count; i++)
{
    using (StreamWriter sw = File.AppendText(Path.Combine(applistpath, applistcount.ToString() + ".txt")))
    {
        sw.Write(appIds[i].Replace("\n", "").Trim());
        sw.Close();
        applistcount++;
    }
}

Console.WriteLine("If the depots section in your config.vdf does not exist please add it under the \"Rate\" section.");
Console.WriteLine("Please copy paste the following into your config.vdf depots section: ");
foreach (KeyValuePair<string, string> key in keys)
{
    using (StreamWriter sw = File.AppendText(Path.Combine(applistpath, applistcount.ToString() + ".txt")))
    {
        sw.Write(key.Key);
        sw.Close();
        applistcount++;
    }
    Console.Write($"                    \"{key.Key}\"\n");
    Console.Write("                    {\n");
    Console.Write($"                        \"DecryptionKey\"   \"{key.Value}\"\n");
    Console.Write("                    }\n");
}

Console.WriteLine("Press any key to exit...");
Console.ReadLine();