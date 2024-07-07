using Kingmaker.Blueprints;
using System;
using System.IO;

namespace AAModContentExporter;
public static class GuidSerialization
{
    public static void SerializeGuids(BlueprintGuid[] guids, string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var binaryWriter = new BinaryWriter(fileStream);
        // Write the length of the array first
        binaryWriter.Write(guids.Length);
        // Write each GUID
        foreach (var guid in guids)
        {
            byte[] guidBytes = guid.ToByteArray();
            binaryWriter.Write(guidBytes);
        }
    }
    public static BlueprintGuid[] DeserializeGuids(string filePath)
    {
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var binaryReader = new BinaryReader(fileStream);
        // Read the length of the array first
        int length = binaryReader.ReadInt32();
        var guids = new BlueprintGuid[length];

        // Read each GUID
        for (int i = 0; i < length; i++)
        {
            byte[] guidBytes = binaryReader.ReadBytes(16); // GUIDs are 16 bytes
            guids[i] = new BlueprintGuid(new Guid(guidBytes));
        }

        return guids;
    }
}
