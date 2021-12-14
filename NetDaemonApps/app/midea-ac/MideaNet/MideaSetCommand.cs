namespace MideaAcIntegration.MideaNet;

public class MideaSetCommand
{
    private readonly List<int> _data;

    public MideaSetCommand()
    {
        _data = MideaConstants.BaseSetCommand.ToList();
    }

    public bool AudibleFeedback
    {
        get => (_data[0x0b] & 0x42) > 0;
        set
        {
            _data[0x0b] &= ~0x42; // Clear the audible bits
            _data[0x0b] |= value ? 0x42 : 0;
        }
    }
    
    public bool PowerState
    {
        get => (_data[0x0b] & 0x01) > 0;
        set
        {
            _data[0x0b] &= ~0x01; // Clear the power bits
            _data[0x0b] |= value ? 0x01 : 0;
        }
    }
    
    public int OperationalMode
    {
        get => (_data[0x0c] & 0xe0) >> 5;
        set
        {
            _data[0x0c] &= ~0xe0; // Clear the mode bits bits
            _data[0x0c] |= (value << 5) & 0xe0;
        }
    }

    public int TargetTemperature
    {
        get => _data[0x0c] & 0x1f;
        set
        {
            _data[0x0c] &= ~0x0f; // Clear the temperature bits bits
            _data[0x0c] |= (value & 0xf) | ((value << 4) & 0x10);
        }
    }

    public int[] GetData()
    {
        // Add the CRC8
        _data[^1] = Crc8.Calculate(_data.Skip(16).ToArray());
        // Set the length of the command data
        _data[0x01] = _data.Count;
        return _data.ToArray();
    }
}