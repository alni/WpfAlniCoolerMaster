using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfAlniCoolerMaster
{
    class DeviceSettings
    {
        public Sharp_SDK.COLOR_MATRIX ColorMatrix;
        public Sharp_SDK.KEY_COLOR KeyColorAll;
        public string BoundExe = null;

        public DeviceSettings()
        {
            ColorMatrix = new Sharp_SDK.COLOR_MATRIX();
            KeyColorAll = new Sharp_SDK.KEY_COLOR();

            int maxLEDRow = Sharp_SDK.SDK.MAX_LED_ROW;
            int maxLEDColumn = Sharp_SDK.SDK.MAX_LED_COLUMN;
            Sharp_SDK.KEY_COLOR[][] keyColors = new Sharp_SDK.KEY_COLOR[maxLEDRow][];
            for (int i = 0; i < keyColors.Length; i++)
            {
                keyColors[i] = new Sharp_SDK.KEY_COLOR[maxLEDColumn];
            }
            for (int i = 0; i < keyColors.Length; i++)
            {
                for (int j = 0; j < keyColors[i].Length; j++)
                {
                    keyColors[i][j] = new Sharp_SDK.KEY_COLOR(0, 0, 0);
                }
            }

            ColorMatrix.KeyColor = keyColors;
        }
    }
}
