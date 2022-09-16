using System.Collections.Generic;
using UnityEngine;

/// <summary> Provides more methods for <see cref="string"/>s. </summary>
public static class ParseMethods
{
    /// <summary> Converts a <see cref="string"/> to a different type of object. </summary>
    /// <param name="str">The <see cref="string"/> to convert. </param>
    /// <param name="output"> The result of the conversion that <paramref name="str"/>.</param>
    /// <returns> Whether or not the operation was sucessful. </returns>
    public static bool TryParse(string str, out List<int> output)
    {
        output = new();
        if (str == "[]")
            return true;
        if (str[0] != '[')
            return false;
        int index = 1;
        while (index < str.Length)
        {
            int comma = str.IndexOf(", ", index);
            if (comma == -1)
                comma = str.Length - 1;
            if (!int.TryParse(str.Substring(index, comma - index), out int value))
            {
                output = new();
                return false;
            }
            output.Add(value);
            index = comma + 2;
        }
        if (str[index - 2] == ']')
            return true;
        output = new();
        return false;
    }
    public static bool TryParse(string str, out List<float> output)
    {
        output = new();
        if (str == "[]")
            return true;
        if (str[0] != '[')
            return false;
        int index = 1;
        while (index < str.Length)
        {
            int comma = str.IndexOf(", ", index);
            if (comma == -1)
                comma = str.Length - 1;
            if (!float.TryParse(str.Substring(index, comma - index), out float value))
            {
                output = new();
                return false;
            }
            output.Add(value);
            index = comma + 2;
        }
        if (str[index - 2] == ']')
            return true;
        output = new();
        return false;
    }
    public static bool TryParse(string str, out List<double> output)
    {
        output = new();
        if (str == "[]")
            return true;
        if (str[0] != '[')
            return false;
        int index = 1;
        while (index < str.Length)
        {
            int comma = str.IndexOf(", ", index);
            if (comma == -1)
                comma = str.Length - 1;
            if (!double.TryParse(str.Substring(index, comma - index), out double value))
            {
                output = new();
                return false;
            }
            output.Add(value);
            index = comma + 2;
        }
        if (str[index - 2] == ']')
            return true;
        output = new();
        return false;
    }
    public static bool TryParse(string str, out ObjectType output)
    {
        output = ObjectType.None;
        switch (str)
        {
            case "None":
                break;
            case "Ground":
                output = ObjectType.Ground;
                break;
            default:
                return false;
        }
        return true;
    }
    public static bool TryParse(string str, out List<ObjectType> output)
    {
        output = new();
        if (str == "[]")
            return true;
        if (str[0] != '[')
            return false;
        int index = 1;
        while (index < str.Length)
        {
            int comma = str.IndexOf(", ", index);
            if (comma == -1)
                comma = str.Length - 1;
            if (TryParse(str.Substring(index, comma - index), out ObjectType value))
            {
                output = new();
                return false;
            }
            output.Add(value);
            index = comma + 2;
        }
        if (str[index - 2] == ']')
            return true;
        output = new();
        return false;
    }
    public static bool TryParse(string str, out Vector3 output)
    {
        output = Vector3.zero;
        string strBracket = str.Replace('(', '[').Replace(')', ']');
        bool b = TryParse(strBracket, out List<float> value);
        if (!b || value.Count != 3)
            return false;
        output = new(value[0], value[1], value[2]);
        return true;
    }
    public static bool TryParse(string str, out Quaternion output)
    {
        output = Quaternion.identity;
        string strBracket = str.Replace('(', '[').Replace(')', ']');
        bool b = TryParse(strBracket, out List<float> value);
        if (!b || value.Count != 4)
            return false;
        output = new(value[0], value[1], value[2], value[3]);
        return true;
    }
}