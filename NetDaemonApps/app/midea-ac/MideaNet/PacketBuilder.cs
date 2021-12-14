namespace MideaAcIntegration.MideaNet;

public static class PacketBuilder
{
    public static int[] Build(MideaSetCommand command)
    {
        var commandData = command.GetData();
        var packet = MideaConstants.InitPacketData.ToList();

        // Append the command data to the packet
        packet.AddRange(commandData);
        // Append a basic checksum of the command to the packet (This is apart from the CRC8 that was added in the command)
        packet.Add(Checksum(commandData.Skip(1).ToArray()));
        //Padding with 0's
        var padding = new int[49 - commandData.Length];
        Array.Fill(padding, 0);
        packet.AddRange(padding);
        // Set the packet length in the packet!
        packet[0x04] = packet.Count;
        return packet.ToArray();
    }

    private static int Checksum(int[] data)
    {
        return 255 - data.Aggregate(0, (a, b) => a + b) % 256 + 1;
    }
}