namespace MideaAcIntegration.MideaNet;

public static class Crc8
{
    public static int Calculate(int[] data)
    {
        var crcValue = 0;
        foreach (var m in data)
        {
            var k = crcValue ^ m;
            if (k > 256) k -= 256;
            if (k < 0) k += 256;
            crcValue = MideaConstants.Crc8Table854[k];
        }
        return crcValue;
    }
}