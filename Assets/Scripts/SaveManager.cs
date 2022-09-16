using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary> A class for handling save files. </summary>
public class SaveManager : MonoBehaviour
{
    private static SaveManager s_singleton;
    private const float s_saveInterval = 5;
    [HideInInspector]
    public string Path;
    private double timer;

    /// <summary> The singleton instance of the save manager. </summary>
    public static SaveManager Singleton
    {
        get => s_singleton;
        private set
        {
            if (s_singleton == null)
                s_singleton = value;
            else if (s_singleton != value)
                Destroy(value);
        }
    }

    private void Awake()
    {
        Singleton = this;
    }

    // Saves the world every so often.
    private void Update()
    {
        if (!NetworkManager.Singleton.Server.IsRunning)
            return;
        timer += Time.deltaTime;
        if (timer > s_saveInterval)
        {
            timer = 0;
            WorldToFile();
        }
    }

    /// <summary> Reads the text file for a world at <see cref="Path"/> and spawns the corresponding world objects. </summary>
    /// <returns> Whether or not the operation was successful. </returns>
    public bool FileToWorld()
    {
        if (!File.Exists(Path))
            return false;
        using (StreamReader reader = new StreamReader(Path))
        {
            string line = reader.ReadLine();
            while (line != null)
            {
                List<string> values = SplitLine(line);
                if (values.Count < 7)
                    return false;
                bool isValid = true;
                isValid &= ParseMethods.TryParse(values[0], out ObjectType type);
                isValid &= ParseMethods.TryParse(values[1], out Vector3 position);
                isValid &= ParseMethods.TryParse(values[2], out Quaternion rotation);
                isValid &= double.TryParse(values[3], out double health);
                isValid &= ParseMethods.TryParse(values[4], out List<double> statusEffects);
                isValid &= ParseMethods.TryParse(values[5], out List<ObjectType> objects);
                isValid &= ParseMethods.TryParse(values[6], out List<int> amounts);
                if (isValid)
                    WorldObject.Create(type, position, rotation, health, statusEffects.ToArray(), objects.ToArray(), amounts.ToArray());
                else
                    return false;
                line = reader.ReadLine();
            }
            reader.Close();
        }
        return true;
    }

    /// <summary> Writes to the text file at <see cref="Path"/> and saves the current world objects.. </summary>
    public void WorldToFile()
    {
        if (Path == Application.dataPath + "/SavedWorlds/default.txt")
            return;
        List<string> playerStrings = new();
        if (File.Exists(Path))
        {
            using (StreamReader reader = new StreamReader(Path))
            {
                string line = reader.ReadLine();
                bool inPlayerSection = false;
                while (line != null)
                {
                    if (inPlayerSection)
                        playerStrings.Add(line);
                    inPlayerSection |= line == "";
                    line = reader.ReadLine();
                }
                reader.Close();
            }
        }
        File.WriteAllText(Path, "");
        using (StreamWriter writer = new StreamWriter(Path))
        {
            List<PlayerObject> players = new();
            foreach (WorldObject worldObject in WorldObject.WorldObjectDict.Values)
            {
                if (worldObject.Type == ObjectType.Player)
                {
                    players.Add((PlayerObject)worldObject);
                    continue;
                }
                StringBuilder builder = new StringBuilder();
                builder.Append(worldObject.Type + ", ");
                builder.Append(worldObject.transform.position + ", ");
                builder.Append(worldObject.transform.rotation + ", ");
                builder.Append(worldObject.Health + ", ");
                builder.Append(worldObject.StatusEffects.ToString<double>() + ", ");
                builder.Append(worldObject.Objects.ToString<ObjectType>() + ", ");
                builder.Append(worldObject.Amounts.ToString<int>());
                writer.WriteLine(builder.ToString());
            }
            writer.WriteLine();
            foreach (string playerString in playerStrings)
            {
                List<string> values = SplitLine(playerString);
                PlayerObject player = null;
                foreach (PlayerObject playerObject in players)
                {
                    if (values[7] == playerObject.Username)
                    {
                        player = playerObject;
                        break;
                    }
                }
                if (player is null)
                {
                    writer.WriteLine(playerString);
                    continue;
                }
                StringBuilder builder = new StringBuilder();
                builder.Append(player.Type + ", ");
                builder.Append(player.transform.position + ", ");
                builder.Append(player.transform.rotation + ", ");
                builder.Append(player.Health + ", ");
                builder.Append(player.StatusEffects.ToString<double>() + ", ");
                builder.Append(player.Objects.ToString<ObjectType>() + ", ");
                builder.Append(player.Amounts.ToString<int>());
                builder.Append(player.Username);
                builder.Append(values[8]); // TODO: Where do we initially store encText, salt, IV?
                builder.Append(values[9]);
                builder.Append(values[10]);
                writer.WriteLine(builder.ToString());
            }
            writer.Close();
        }
    }

    // Splits each line into separate strings.
    private static List<string> SplitLine(string line)
    {
        List<string> result = new();
        bool unmatchedBracket = false;
        int start = 0;
        for (int i = 0; i <= line.Length; i++)
        {
            if (i == line.Length || line[i] == ',' && !unmatchedBracket)
            {
                result.Add(line.Substring(start, i - start));
                i++;
                start = i + 1;
                continue;
            }
            unmatchedBracket |= line[i] == '(' || line[i] == '[';
            unmatchedBracket &= line[i] != ')' && line[i] != ']';
        }
        return result;
    }
}