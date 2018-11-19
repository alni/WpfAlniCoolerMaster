using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfAlniCoolerMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static readonly byte MAX_COLOR_VALUE = 255;
        private System.Timers.Timer aSysInfoTimer;

        private bool ledControlEnabled = true;
        private Sharp_SDK.COLOR_MATRIX colorMatrix;

        private bool initialized = false;

        public MainWindow()
        {
            InitializeComponent();

            SetSysInfoTimer();

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
            colorMatrix = new Sharp_SDK.COLOR_MATRIX(keyColors);
            //Sharp_SDK.SDK.SetControlDevice(Sharp_SDK.DEVICE_INDEX.DEV_MKeys_M_White);
            initialized = true;
        }

        private void SetSysInfoTimer()
        {
            aSysInfoTimer = new System.Timers.Timer(1000);
            aSysInfoTimer.Elapsed += ASysInfoTimer_Elapsed;
            aSysInfoTimer.AutoReset = true;
            aSysInfoTimer.Enabled = true;
        }


        private void ASysInfoTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => GetSysInfo() ));
        }

        [HandleProcessCorruptedStateExceptions]
        void GetSysInfo()
        {
            DateTime now = DateTime.Now;
            string dateTime = now.ToShortDateString() + " " + now.ToLongTimeString();
            tbSystemTime.Text = dateTime;

            //string timeNow = Sharp_SDK.SDK.GetNowTime();
            //tbSystemTime.Text = timeNow;

            // TODO: Implement CPU Usage Info
            long cpuUsage = Sharp_SDK.SDK.GetNowCPUUsage();
            tbCPUUsage.Text = cpuUsage.ToString();
            //ToolTip toolTip = new ToolTip();
            //toolTip.Content = cpuUsage.ToString();
            //tbCPUUsage.ToolTip = toolTip;

            // TODO: Implement RAM USage Info
            int ramUsage = Sharp_SDK.SDK.GetRamUsage();
            tbRAMUsage.Text = ramUsage.ToString();

            float volumePeekValue = Sharp_SDK.SDK.GetNowVolumePeekValue() * 100;
            tbVolumePeek.Text = volumePeekValue.ToString("F0");
        }

        // TODO: Rename function to "ButtonDevicePlug_Click"
        private void ButtonDevicePlug_Click(object sender, RoutedEventArgs e)
        {
            bool isPluggedIn = Sharp_SDK.SDK.IsDevicePlug();
            Sharp_SDK.LAYOUT_KEYOBARD kbl = Sharp_SDK.SDK.GetDeviceLayout();
            MessageBox.Show(isPluggedIn ? "Connected!" : "Removed!");
        }

        // TODO: Rename function to "ButtonSetDevice_Click"
        private void ButtonSetDevice_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = cbDeviceSelect.SelectedIndex;

            Sharp_SDK.DEVICE_INDEX selectedDevice = (Sharp_SDK.DEVICE_INDEX)selectedIndex;
            Sharp_SDK.SDK.SetControlDevice(selectedDevice);
            Sharp_SDK.SDK.EnableLedControl(ledControlEnabled);
            if (ledControlEnabled)
            {
                Sharp_SDK.SDK.RefreshLed(true);
            }
            bool isSingleColor = isSingleColorLed(selectedDevice);
            tbLEDGreen.IsEnabled = !isSingleColor;
            tbLEDBlue.IsEnabled = !isSingleColor;
            tbLEDGreen_All.IsEnabled = !isSingleColor;
            tbLEDBlue_All.IsEnabled = !isSingleColor;
            if (isSingleColor)
            {
                lblLedRed.Content = "BRT:"; // Brightness
                lblLedRed_All.Content = "BRT:"; // Brightness
            }
            else
            {
                lblLedRed.Content = "R:";
                lblLedRed_All.Content = "R:";
            }
        }

        // TODO: Rename function to "ButtonDeviceLayout_Click"
        private void ButtonDeviceLayout_Click(object sender, RoutedEventArgs e)
        {
            Sharp_SDK.LAYOUT_KEYOBARD kbdLayout = Sharp_SDK.SDK.GetDeviceLayout();
            string strKbdLayout = kbdLayout.ToString();
            tbLayout.Text = strKbdLayout;
        }

        // TODO: Rename function to "ButtonLEDControlToggle_Click"
        [HandleProcessCorruptedStateExceptions]
        private void ButtonLEDControlToggle_Click(object sender, RoutedEventArgs e)
        {
            ledControlEnabled = !ledControlEnabled;
            try
            {
                Sharp_SDK.SDK.EnableLedControl(ledControlEnabled);
                if (ledControlEnabled)
                {
                    Sharp_SDK.SDK.RefreshLed(true);
                }
            }
            catch (AccessViolationException ex)
            {
                //MessageBox.Show(ex.Message);
            }
            

            if (ledControlEnabled == true)
            {
                btnLedControl.Content = "Disable";
            }
            else
            {
                btnLedControl.Content = "Enable";
            }
            
            btnLedEffect.IsEnabled = !ledControlEnabled;

            btnLEDSingleKey.IsEnabled = ledControlEnabled;
            btnLEDAllKeys.IsEnabled = ledControlEnabled;
            btnLEDColor_All.IsEnabled = ledControlEnabled;
        }

        // TODO: Rename function to "ButtonLedEffect_Click"
        private void ButtonLedEffect_Click(object sender, RoutedEventArgs e)
        {
            Sharp_SDK.EFF_INDEX effIndex = (Sharp_SDK.EFF_INDEX)cbLEDEffectChoose.SelectedIndex;
            bool success = Sharp_SDK.SDK.SwitchLedEffect(effIndex);
            if (success == false)
            {
                MessageBox.Show("No Effect or Fail");
            }
        }

        /// <summary>
        /// https://stackoverflow.com/a/12721673
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // TODO: Rename function to "ButtonSetSingleKeyColor_Click"
        private void ButtonSetSingleKeyColor_Click(object sender, RoutedEventArgs e)
        {
            int keyRowIndex = cbLEDRow.SelectedIndex;
            int keyColumnIndex = cbLEDColumn.SelectedIndex;

            Sharp_SDK.KEY_COLOR keyColor = colorMatrix.KeyColor[keyRowIndex][keyColumnIndex];
            keyColor = normalizeColors(keyColor);

            Sharp_SDK.SDK.SetLedColor(keyRowIndex, keyColumnIndex, keyColor.r, keyColor.g, keyColor.b);
        }

        // TODO: Rename function to "ButtonSetKeyColorMatrix_Click"
        private void ButtonSetKeyColorMatrix_Click(object sender, RoutedEventArgs e)
        {
            //Sharp_SDK.SDK.SetAllLedColor(colorMatrix);
            Sharp_SDK.KEY_COLOR[][] keyColors = colorMatrix.KeyColor;
            for (int i = 0; i < keyColors.Length; i++)
            {
                for (int j = 0; j < keyColors[i].Length; j++)
                {
                    Sharp_SDK.KEY_COLOR keyColor = colorMatrix.KeyColor[i][j];
                    keyColor = normalizeColors(keyColor);

                    Sharp_SDK.SDK.SetLedColor(i, j, keyColor.r, keyColor.g, keyColor.b);
                }
            }
        }

        // TODO: Rename function to "ButtonSetFullKeyColor_Click"
        private void ButtonSetFullKeyColor_Click(object sender, RoutedEventArgs e)
        {
            byte redColor = GetColorValue(tbLEDRed_All.Text);
            byte greenColor = GetColorValue(tbLEDGreen_All.Text);
            byte blueColor = GetColorValue(tbLEDBlue_All.Text);

            Sharp_SDK.KEY_COLOR keyColor = new Sharp_SDK.KEY_COLOR(redColor, greenColor, blueColor);
            keyColor = normalizeColors(keyColor);

            var success = Sharp_SDK.SDK.SetFullLedColor(keyColor.r, keyColor.g, keyColor.b);
            if (success == false)
            {
                MessageBox.Show("SetFullLedColor Failed!");
            }
        }

        private void CheckboxKeyEffect_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckboxKeyEffect_Unchecked(object sender, RoutedEventArgs e)
        {

        }

        // TODO: Rename function to "ButtonCPUStatus_Click"
        private void ButtonCPUStatus_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TextBoxLED_TextChanged(object sender, TextChangedEventArgs e)
        {
            int row = cbLEDRow.SelectedIndex;
            int column = cbLEDColumn.SelectedIndex;

            Sharp_SDK.KEY_COLOR keyColor = colorMatrix.KeyColor[row][column];

            byte redColor = GetColorValue(tbLEDRed.Text);
            byte greenColor = GetColorValue(tbLEDGreen.Text);
            byte blueColor = GetColorValue(tbLEDBlue.Text);

            keyColor.r = redColor;
            keyColor.g = greenColor;
            keyColor.b = blueColor;
            keyColor = normalizeColors(keyColor);

            tbLEDRed.Text = keyColor.r.ToString();
            tbLEDGreen.Text = keyColor.g.ToString();
            tbLEDBlue.Text = keyColor.b.ToString();

            colorMatrix.KeyColor[row][column] = keyColor;
        }

        private bool isSingleColorLed(Sharp_SDK.DEVICE_INDEX device)
        {
            bool isSingleColorLed = false; // Default to RGB
            switch (device)
            {
                case Sharp_SDK.DEVICE_INDEX.DEV_MKeys_L_White:
                case Sharp_SDK.DEVICE_INDEX.DEV_MKeys_M_White:
                case Sharp_SDK.DEVICE_INDEX.DEV_MKeys_S_White:
                    isSingleColorLed = true;
                    break;
            }
            return isSingleColorLed;
        }

        private Sharp_SDK.KEY_COLOR normalizeColors(Sharp_SDK.KEY_COLOR keyColor)
        {
            int selectedIndex = cbDeviceSelect.SelectedIndex;
            Sharp_SDK.DEVICE_INDEX selectedDevice = (Sharp_SDK.DEVICE_INDEX)selectedIndex;
            if (isSingleColorLed(selectedDevice))
            {
                keyColor.g = keyColor.r;
                keyColor.b = keyColor.r;
            }
            return keyColor;
        }

        private byte GetColorValue(string strColor)
        {
            byte color;
            int iColor;
            int.TryParse(strColor, out iColor);
            if (iColor > MAX_COLOR_VALUE)
            {
                iColor = MAX_COLOR_VALUE;
            }
            else if (iColor < 0)
            {
                iColor = 0;
            }
            return (byte)iColor;
        }

        private void TextBoxLED_TextChanged_All(object sender, TextChangedEventArgs e)
        {
            Sharp_SDK.KEY_COLOR keyColor = new Sharp_SDK.KEY_COLOR();

            byte redColor = GetColorValue(tbLEDRed_All.Text);
            byte greenColor = GetColorValue(tbLEDGreen_All.Text);
            byte blueColor = GetColorValue(tbLEDBlue_All.Text);

            keyColor.r = redColor;
            keyColor.g = greenColor;
            keyColor.b = blueColor;
            keyColor = normalizeColors(keyColor);

            tbLEDRed_All.Text = keyColor.r.ToString();
            tbLEDGreen_All.Text = keyColor.g.ToString();
            tbLEDBlue_All.Text = keyColor.b.ToString();
        }

        private void ComboBoxLED_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int row = 0;
            int column = 0;
            if (initialized == true)
            {
                if (cbLEDRow != null && cbLEDColumn != null)
                {
                    row = cbLEDRow.SelectedIndex;
                    column = cbLEDColumn.SelectedIndex;
                }
                Sharp_SDK.KEY_COLOR keyColor = colorMatrix.KeyColor[row][column];
                keyColor = normalizeColors(keyColor);

                tbLEDRed.Text = keyColor.r.ToString("F0");
                tbLEDGreen.Text = keyColor.g.ToString("F0");
                tbLEDBlue.Text = keyColor.b.ToString("F0");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Sharp_SDK.SDK.EnableLedControl(false);
        }
    }
}
