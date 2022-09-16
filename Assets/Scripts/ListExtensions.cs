using System.Collections.Generic;
using System.Text;

/// <summary> Provides more methods for <see cref="IList{T}"/>s. </summary>
public static class ListExtensions
{
    /// <summary>Finds the first index of <paramref name="value"/>.</summary>
    /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
    /// <param name="list">The list to be searched through.</param>
    /// <param name="value">The value to be searched for.</param>
    /// <returns>The first index of <paramref name="value"/>, or -1 if not present.</returns>
    public static int IndexOf<T>(this IList<T> list, T value)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(value))
                return i;
        }
        return -1;
    }

    /// <summary>Finds the last index of <paramref name="value"/>.</summary>
    /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
    /// <param name="list">The list to be searched through.</param>
    /// <param name="value">The value to be searched for.</param>
    /// <returns>The last index of <paramref name="value"/>, or -1 if not present.</returns>
    public static int LastIndexOf<T>(this IList<T> list, T value)
    {
        for (int i = list.Count - 1; i >= 0; i++)
        {
            if (list[i].Equals(value))
                return i;
        }
        return -1;
    }

    /// <summary>Finds all indices of <paramref name="value"/>.</summary>
    /// <typeparam name="T">The type of <paramref name="value"/>.</typeparam>
    /// <param name="list">The list to be searched through.</param>
    /// <param name="value">The value to be searched for.</param>
    /// <returns>A <see cref="List{T}"/> of all the indices of <paramref name="value"/>.</returns>
    public static List<int> IndicesOf<T>(this IList<T> list, T value)
    {
        List<int> indices = new();
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(value))
                indices.Add(i);
        }
        return indices;
    }

    /// <summary> Converts a <see cref="List{T}"/> to a <see cref="string"/>.</summary>
    /// <typeparam name="T"> The type of object in <paramref name="list"/>. </typeparam>
    /// <param name="list"> The list to be converted. </param>
    /// <returns> The string that the list is converted to. </returns>
    public static string ToString<T>(this IList<T> list)
    {
        if (list.Count == 0)
            return "[]";
        StringBuilder builder = new();
        builder.Append("[");
        foreach (T value in list)
        {
            builder.Append(value.ToString());
            builder.Append(", ");
        }
        builder.Replace(", ", "]", builder.ToString().Length - 2, 2);
        return builder.ToString();
    }
}