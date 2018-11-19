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

        public DeviceSettings()
        {
            ColorMatrix = new Sharp_SDK.COLOR_MATRIX();
            KeyColorAll = new Sharp_SDK.KEY_COLOR();
        }
    }
}
