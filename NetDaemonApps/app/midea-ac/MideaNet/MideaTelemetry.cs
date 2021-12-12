using System.Linq;

namespace MideaAcIntegration.MideaNet
{
    public class MideaTelemetry
    {
        private readonly int[] _data;

        public MideaTelemetry(int[] data)
        {
            // The response data from the appliance includes a packet header which we don't want
            //_data = data.Skip(0xa).ToArray();
            _data = data.Skip(0x32).ToArray();
        }

        public string Test => "";

        // Byte 0x01
        public bool PowerState => (_data[0x01] & 0x1) > 0;

        public bool ImodeResume => (_data[0x01] & 0x4) > 0;

        public bool TimerMode => (_data[0x01] & 0x10) > 0;

        public bool ApplianceError => (_data[0x01] & 0x80) > 0;

        // Byte 0x02
        public int OperationalMode => (_data[0x02] & 0xe0) >> 5;

        // Byte 0x03
        public int FanSpeed => _data[0x03] & 0x7f;

        // Byte 0x04 + 0x06
        // get onTimer(): any {
        //     const on_timer_value = this.data[0x04];
        //     const on_timer_minutes = this.data[0x06];
        //     return {
        //         status: (on_timer_value & 0x80) >> 7 > 0,
        //         hour: (on_timer_value & 0x7c) >> 2,
        //         minutes: (on_timer_value & 0x3) | ((on_timer_minutes & 0xf0) >> 4),
        //     };
        // }

        // Byte 0x05 + 0x06
        // get offTimer(): any {
        //     const off_timer_value = this.data[0x05];
        //     const off_timer_minutes = this.data[0x06];
        //     return {
        //         status: (off_timer_value & 0x80) >> 7 > 0,
        //         hour: (off_timer_value & 0x7c) >> 2,
        //         minutes: (off_timer_value & 0x3) | (off_timer_minutes & 0xf),
        //     };
        // }

        // Byte 0x07
        // get swingMode(): MideaSwingMode {
        //     return this.data[0x07] & 0x0f;
        // }

        // Byte 0x08
        public int CozySleep => _data[0x08] & 0x03;

        // This needs a better name
        public bool Save => (_data[0x08] & 0x08) > 0;

        public bool LowFrequencyFan => (_data[0x08] & 0x10) > 0;

        public bool SuperFan => (_data[0x08] & 0x20) > 0;

        // This needs a better name
        public bool FeelOwn => (_data[0x08] & 0x80) > 0;

        // Byte 0x09
        public bool ChildSleepMode => (_data[0x09] & 0x01) > 0;

        public bool ExchangeAir => (_data[0x09] & 0x02) > 0;

        // This actually means 13°C(55°F)~35°C(95°F) according to my manual. Also dehumidifying.
        public bool DryClean => (_data[0x09] & 0x04) > 0;

        public bool EcoMode => (_data[0x09] & 0x10) > 0;

        // This needs a better name
        public bool CleanUp => (_data[0x09] & 0x20) > 0;

        // Byte 0x0a
        public bool SleepFunction => (_data[0x0a] & 0x01) > 0;

        public bool TurboMode => (_data[0x0a] & 0x02) > 0;

        // This needs a better name
        public bool NightLight => (_data[0x0a] & 0x10) > 0;

        // This needs a better name
        public bool PeakElec => (_data[0x0a] & 0x20) > 0;

        // This needs a better name
        public bool NaturalFan => (_data[0x0a] & 0x40) > 0;

        // Byte 0x0d
        public int Humidity => _data[0x0d] & 0x7f;
        
        
        // Byte 0x02
        public int TargetTemperature => (_data[0x02] & 0xf) + 16;
        // Byte 0x09
        public bool AuxHeat => (_data[0x09] & 0x08) > 0;
        // Byte 0x0a // This needs a better name
        public bool CatchCold => (_data[0x0a] & 0x08) > 0;
        // Byte 0x0b
        public double IndoorTemperature => (_data[0x0b] - 50) / 2.0;
        // Byte 0x0c
        public double OutdoorTemperature => (_data[0x0c] - 50) / 2.0;

        public bool CelsiusUnit => (_data[0x09] & 0x80) > 0; 

        public bool FahrenheitUnit => _data[23] > 0; // FAHRENHEIT - True
    }
}