namespace Estreya.BlishHUD.ArcDPSLogManager.Models;

using Newtonsoft.Json;
using System;

public class TagInfo : IEquatable<TagInfo>
{
    /// <summary>
    /// The name of this tag.
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; }

    [JsonConstructor]
    public TagInfo(string name)
    {
        this.Name = name;
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as TagInfo);
    }

    public bool Equals(TagInfo other)
    {
        return other != null &&
               this.Name == other.Name;
    }

    public override int GetHashCode()
    {
        return this.Name.GetHashCode();
    }
}
