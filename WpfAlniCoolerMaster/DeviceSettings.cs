using Sharp_SDK;

namespace WpfAlniCoolerMaster
{
    class DeviceSettings
    {
        public COLOR_MATRIX ColorMatrix;
        public KEY_COLOR KeyColorAll;
        public DEVICE_INDEX SelectedDevice;
        public EFF_INDEX SelectedEffect;
        public string BoundExe = null;

        public DeviceSettings() : this(DEVICE_INDEX.DEV_DEFAULT)
        {
            // Use "DEV_DEFAULT" as default device with the overloaded constructor
            // https://stackoverflow.com/a/5555741
        }

        public DeviceSettings(DEVICE_INDEX device)
        {
            SelectedDevice = device;
            ColorMatrix = new COLOR_MATRIX();
            KeyColorAll = new KEY_COLOR();
            SelectedEffect = EFF_INDEX.EFF_FULL_ON; // Default to Full On

            int maxLEDRow = SDK.MAX_LED_ROW;
            int maxLEDColumn = SDK.MAX_LED_COLUMN;
            KEY_COLOR[][] keyColors = new KEY_COLOR[maxLEDRow][];
            for (int i = 0; i < keyColors.Length; i++)
            {
                keyColors[i] = new KEY_COLOR[maxLEDColumn];
            }
            for (int i = 0; i < keyColors.Length; i++)
            {
                for (int j = 0; j < keyColors[i].Length; j++)
                {
                    keyColors[i][j] = new KEY_COLOR(0, 0, 0);
                }
            }

            ColorMatrix.KeyColor = keyColors;
        }
    }
}
