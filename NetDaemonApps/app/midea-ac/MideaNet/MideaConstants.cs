namespace MideaAcIntegration.MideaNet
{
    public struct MideaConstants
    {
        public static readonly int[] GetTelemetryCommand =
        {
            170, // 0         - Sync header
            32, // 1         - Message length request
            172, // 2         - Device type (172 for Air Conditioner)
            0, // 3		 - Frame sync check (not used, 0x00)
            0, // 4		 - Reserved 0x00
            0, // 5		 - Reserved 0x00
            0, // 6		 - Message Id
            0, // 7    	 - Framework protocol version
            0, // 8         - Device Agreement Version
            3, // 9         - Message type request identification
            // Command Header End
            // Data Start
            65, // 10		- Data request/response: check status
            129, // 11		- Power state
            0, // 12		- Operational mode
            255, // 13
            3, // 14
            255, // 15
            0, // 16
            2, // 17	 	- Room temperature request: 0x02 - indoor temperature, 0x03 - outdoor temperature
            0, // 18
            0, // 19
            0, // 20
            // Padding
            0, 0, 0, 0, 0, 0, 0, 0, 0,
            3, // Message ID
            205, 156, 16, 184, 113, 186, 162, 129, 39, 12, 160, 157, 100, 102, 118, 15, 154, 166
        };

        public const string DeviceTypeAc = "0xAC";

        public const string UserAgent = "Dalvik/2.1.0 (Linux; U; Android 7.0; SM-G935F Build/NRD90M)";
        public const string AppKey = "ff0cf6f5f0c3471de36341cab3f7a9af";
        public const string ClientType = "1"; // 0: PC, 1: Android, 2: IOS
        public const string RequestSource = "17";
        public const string AppId = "1117";
        public const string RequestFormat = "2"; //JSON
        public const string Language = "en-US";
    }
}