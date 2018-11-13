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
        private System.Timers.Timer aSysInfoTimer;

        private bool ledControlEnabled = true;
        private Sharp_SDK.COLOR_MATRIX colorMatrix;

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
            colorMatrix = new Sharp_SDK.COLOR_MATRIX(keyColors);
            Sharp_SDK.SDK.SetControlDevice(Sharp_SDK.DEVICE_INDEX.DEV_MKeys_M_White);
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

        void GetSysInfo()
        {
            DateTime now = DateTime.Now;
            string dateTime = now.ToShortDateString() + " " + now.ToLongTimeString();
            tbSystemTime.Text = dateTime;

            // TODO: Implement CPU Usage Info

            // TODO: Implement RAM USage Info


            float volumePeekValue = Sharp_SDK.SDK.GetNowVolumePeekValue();
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
            if (ledControlEnabled)
            {
                btnLedControl.Content = "Disable";
                btnLedEffect.IsEnabled = false;
                btnLEDSingleKey.IsEnabled = true;
                btnLEDAllKeys.IsEnabled = true;
                btnLEDColor_All.IsEnabled = true;
                try
                {
                    Sharp_SDK.SDK.EnableLedControl(true);
                } catch (AccessViolationException ex)
                {
                    //MessageBox.Show(ex.Message);
                }
            } else
            {
                btnLedControl.Content = "Enable";
                btnLedEffect.IsEnabled = true;
                btnLEDSingleKey.IsEnabled = false;
                btnLEDAllKeys.IsEnabled = false;
                btnLEDColor_All.IsEnabled = false;
                try
                {
                    Sharp_SDK.SDK.EnableLedControl(false);
                }
                catch (AccessViolationException ex)
                {
                    //MessageBox.Show(ex.Message);
                }
            }
        }

        // TODO: Rename function to "ButtonLedEffect_Click"
        private void ButtonLedEffect_Click(object sender, RoutedEventArgs e)
        {
            Sharp_SDK.EFF_INDEX effIndex = (Sharp_SDK.EFF_INDEX)cbLEDEffectChoose.SelectedIndex;
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

            Sharp_SDK.SDK.SetLedColor(keyRowIndex, keyColumnIndex, keyColor.r, keyColor.g, keyColor.b);
        }

        // TODO: Rename function to "ButtonSetKeyColorMatrix_Click"
        private void ButtonSetKeyColorMatrix_Click(object sender, RoutedEventArgs e)
        {
            Sharp_SDK.SDK.SetAllLedColor(colorMatrix);
        }

        // TODO: Rename function to "ButtonSetFullKeyColor_Click"
        private void ButtonSetFullKeyColor_Click(object sender, RoutedEventArgs e)
        {
            byte redColor;
            byte greenColor;
            byte blueColor;
            Byte.TryParse(tbLEDRed_All.Text, out redColor);
            Byte.TryParse(tbLEDGreen_All.Text, out greenColor);
            Byte.TryParse(tbLEDBlue_All.Text, out blueColor);

            Sharp_SDK.SDK.SetFullLedColor(redColor, greenColor, blueColor);
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

            byte redColor;
            byte greenColor;
            byte blueColor;
            Byte.TryParse(tbLEDRed.Text, out redColor);
            Byte.TryParse(tbLEDGreen.Text, out greenColor);
            Byte.TryParse(tbLEDBlue.Text, out blueColor);

            keyColor.r = redColor;
            keyColor.g = greenColor;
            keyColor.b = blueColor;

            colorMatrix.KeyColor[row][column] = keyColor;
        }

        private void TextBoxLED_TextChanged_All(object sender, TextChangedEventArgs e)
        {

        }
    }
}
