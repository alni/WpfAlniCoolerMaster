using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        private Sharp_SDK.KEY_COLOR keyColorAll;
        private string boundExe = null;
        private string lastActiveProcess = null;

        private bool initialized = false;

        private Sharp_SDK.DEVICE_INDEX currDevice;
        private Sharp_SDK.EFF_INDEX currEffect;

        private Sharp_SDK.SDK.KEY_CALLBACK keyCallback;
        private Sharp_SDK.SDK.KEY_CALLBACK getKeyRowColumnCallback;

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
            lastActiveProcess = (ActiveProcess.GetActiveProcessFileName() + ".exe").ToLower();
            this.keyCallback = KeyCallback;
            this.getKeyRowColumnCallback = GetKeyRowColumnKeyCallback;
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
            Console.WriteLine(String.IsNullOrWhiteSpace(boundExe) == false);
            if (String.IsNullOrWhiteSpace(boundExe) == false) {
                string activeProcessFileName = (ActiveProcess.GetActiveProcessFileName() + ".exe").ToLower();
                if (activeProcessFileName != lastActiveProcess)
                {
                    if (boundExe != null && this.ledControlEnabled == true)
                    {
                        // Only do this when "this.ledControlEnabled" is True
                        Console.WriteLine(activeProcessFileName + " | " + boundExe.ToLower() + " (" + (activeProcessFileName == boundExe.ToLower()) + ")");
                        if (activeProcessFileName == boundExe.ToLower())
                        {
                            Sharp_SDK.SDK.EnableLedControl(true);
                            SetKeyColorMatrix(currDevice);
                        }
                        else
                        {
                            Sharp_SDK.SDK.EnableLedControl(false);
                        }
                    }
                    lastActiveProcess = activeProcessFileName + "";
                }
            }
            //Console.WriteLine(ActiveProcess.GetActiveProcessFileName());
        }

        [HandleProcessCorruptedStateExceptions]
        void GetSysInfo()
        {
            DateTime now = DateTime.Now;
            string dateTime = now.ToShortDateString() + " " + now.ToLongTimeString();
            tbSystemTime.Text = dateTime;

            //string timeNow = Sharp_SDK.SDK.GetNowTime();
            //tbSystemTime.Text = timeNow;

            // CPU Usage Info
            long cpuUsage = Sharp_SDK.SDK.GetNowCPUUsage();
            tbCPUUsage.Text = ((int)cpuUsage).ToString();
            //ToolTip toolTip = new ToolTip();
            //toolTip.Content = cpuUsage.ToString();
            //tbCPUUsage.ToolTip = toolTip;

            // RAM USage Info
            int ramUsage = Sharp_SDK.SDK.GetRamUsage();
            tbRAMUsage.Text = ramUsage.ToString();

            float volumePeekValue = Sharp_SDK.SDK.GetNowVolumePeekValue() * 100;
            tbVolumePeek.Text = volumePeekValue.ToString("F0");
        }

        // Check if Device is plugged in
        private void ButtonDevicePlug_Click(object sender, RoutedEventArgs e)
        {
            bool isPluggedIn = Sharp_SDK.SDK.IsDevicePlug();
            Sharp_SDK.LAYOUT_KEYOBARD kbl = Sharp_SDK.SDK.GetDeviceLayout();
            isPluggedIn = isPluggedIn && (kbl != Sharp_SDK.LAYOUT_KEYOBARD.LAYOUT_UNINIT);
            MessageBox.Show(isPluggedIn ? "Connected!" : "Removed!");
        }

        // Setting the Current Device
        private void ButtonSetDevice_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = cbDeviceSelect.SelectedIndex; // Selected Device Index

            // Try to convert the index to a device enum and set it as the currently controlled
            Sharp_SDK.DEVICE_INDEX selectedDevice = (Sharp_SDK.DEVICE_INDEX)selectedIndex; // convert to enum

            SetDevice(selectedDevice);
        }

        private void SetDevice(Sharp_SDK.DEVICE_INDEX selectedDevice)
        { 
            Sharp_SDK.SDK.SetControlDevice(selectedDevice); // Try to set it as controlled
            Sharp_SDK.SDK.EnableLedControl(ledControlEnabled); // Enable/Disable LED Control
            if (ledControlEnabled)
            {
                // Refresh the LEDs if LED Control is enabled
                Sharp_SDK.SDK.RefreshLed(true);
            }

            // Change the color setting states based on if it is a RGB Device or not (single color LEDs)
            bool isSingleColor = IsSingleColorLed(selectedDevice);

            if (isSingleColor)
            {
                // Change the LED color labels to Brightness content with single color LEDs
                lblLed.Content = "BRT:"; // Brightness
                lblLed_All.Content = "BRT:"; // Brightness
            }
            else
            {
                // Revert toe Red color labels to Red content with RGB LEDs
                lblLed.Content = "Color:";
                lblLed_All.Content = "Color:";
            }

            currDevice = selectedDevice;
        }

        // Get Device Layout
        private void ButtonDeviceLayout_Click(object sender, RoutedEventArgs e)
        {
            // Get the current keyboard layout of the device
            Sharp_SDK.LAYOUT_KEYOBARD kbdLayout = Sharp_SDK.SDK.GetDeviceLayout();
            string strKbdLayout = kbdLayout.ToString();
            tbLayout.Text = strKbdLayout;
        }

        // Toggle LED Control
        [HandleProcessCorruptedStateExceptions]
        private void ButtonLEDControlToggle_Click(object sender, RoutedEventArgs e)
        {
            // Toggle LED Control
            ledControlEnabled = !ledControlEnabled;
            try
            {
                Sharp_SDK.SDK.EnableLedControl(ledControlEnabled);
                if (ledControlEnabled)
                {
                    // Refresh the LEDs if LED Control is enabled
                    Sharp_SDK.SDK.RefreshLed(true);
                }
            }
            catch (AccessViolationException ex)
            {
                //MessageBox.Show(ex.Message);
                Console.WriteLine(ex.Message);
            }


            // Update the LED Control button content based on if LED Control is currently enabled or disabled
            if (ledControlEnabled == true)
            {
                // LED Control is currently enabled
                btnLedControl.Content = "Disable";
            }
            else
            {
                // LED Control is currently disabled
                btnLedControl.Content = "Enable";
            }

            // Update the state of other controls based on if LED Control is currently enabled or disabled
            btnLedEffect.IsEnabled = !ledControlEnabled;

            btnLEDSingleKey.IsEnabled = ledControlEnabled;
            btnLEDAllKeys.IsEnabled = ledControlEnabled;
            btnLEDColor_All.IsEnabled = ledControlEnabled;
        }

        // Set the current LED Effect
        private void ButtonLedEffect_Click(object sender, RoutedEventArgs e)
        {
            // Setting the current LED Effect (only available when LED Control is disabled)
            Sharp_SDK.EFF_INDEX effIndex = (Sharp_SDK.EFF_INDEX)cbLEDEffectChoose.SelectedIndex;
            SetLedEffect(effIndex);
        }

        private void SetLedEffect(Sharp_SDK.EFF_INDEX effIndex)
        {
            if (this.ledControlEnabled == false)
            {
                bool success = Sharp_SDK.SDK.SwitchLedEffect(effIndex);
                if (success == false)
                {
                    MessageBox.Show("No Effect or Fail");
                }
            }
            currEffect = effIndex;
        }

        /// <summary>
        /// Only allow numbers in TextBox
        /// https://stackoverflow.com/a/12721673
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        // Set the color for a single key
        private void ButtonSetSingleKeyColor_Click(object sender, RoutedEventArgs e)
        {
            // Setting the current color for a single key (only available when LED Control is enabled)
            int keyRowIndex = cbLEDRow.SelectedIndex;
            int keyColumnIndex = cbLEDColumn.SelectedIndex;

            Sharp_SDK.KEY_COLOR keyColor = colorMatrix.KeyColor[keyRowIndex][keyColumnIndex];
            keyColor = NormalizeColors(keyColor);

            Sharp_SDK.SDK.SetLedColor(keyRowIndex, keyColumnIndex, keyColor.r, keyColor.g, keyColor.b);
        }

        // Set the color for each key as specified in the ColorMatrix
        private void ButtonSetKeyColorMatrix_Click(object sender, RoutedEventArgs e)
        {
            int selectedIndex = cbDeviceSelect.SelectedIndex;
            Sharp_SDK.DEVICE_INDEX selectedDevice = (Sharp_SDK.DEVICE_INDEX)selectedIndex;
            SetKeyColorMatrix(selectedDevice);
        }

        private void SetKeyColorMatrix(Sharp_SDK.DEVICE_INDEX deviceIndex)
        {
            // Setting the current colors set for each keys (only available when LED Control is enabled)
            //Sharp_SDK.SDK.SetAllLedColor(colorMatrix);
            Sharp_SDK.KEY_COLOR[][] keyColors = colorMatrix.KeyColor;
            for (int i = 0; i < keyColors.Length; i++)
            {
                for (int j = 0; j < keyColors[i].Length; j++)
                {
                    Sharp_SDK.KEY_COLOR keyColor = colorMatrix.KeyColor[i][j];
                    keyColor = NormalizeColors(keyColor, deviceIndex);

                    Sharp_SDK.SDK.SetLedColor(i, j, keyColor.r, keyColor.g, keyColor.b);
                }
            }
        }

        // Set all the keys to the same color
        private void ButtonSetFullKeyColor_Click(object sender, RoutedEventArgs e)
        {
            // Setting the same color for all the each keys (only available when LED Control is enabled)
            byte redColor = keyColorAll.r;
            byte greenColor = keyColorAll.g;
            byte blueColor = keyColorAll.b;

            Sharp_SDK.KEY_COLOR keyColor = new Sharp_SDK.KEY_COLOR(redColor, greenColor, blueColor);
            keyColor = NormalizeColors(keyColor);

            var success = Sharp_SDK.SDK.SetFullLedColor(keyColor.r, keyColor.g, keyColor.b);
            if (success == false)
            {
                MessageBox.Show("SetFullLedColor Failed!");
            }
        }

        private void CheckboxKeyEffect_Checked(object sender, RoutedEventArgs e)
        {
            Sharp_SDK.SDK.SetKeyCallBack(this.keyCallback);
            Sharp_SDK.SDK.EnableKeyInterrupt(true);
        }

        private void CheckboxKeyEffect_Unchecked(object sender, RoutedEventArgs e)
        {
            Sharp_SDK.SDK.SetKeyCallBack(null);
            Sharp_SDK.SDK.EnableKeyInterrupt(false);
        }

        void KeyCallback(int iRow, int iColumn, bool bPressed)
        {
            Console.WriteLine(iRow + ":" + iColumn + " | " + bPressed);

            if (this.ledControlEnabled == true)
            {
                Sharp_SDK.DEVICE_INDEX devIndex = (Sharp_SDK.DEVICE_INDEX)currDevice;
                if (devIndex == Sharp_SDK.DEVICE_INDEX.DEV_MMouse_L)
                {
                    if (iColumn > 3)
                    {
                        iColumn = 3;
                    }
                }
                else if (devIndex == Sharp_SDK.DEVICE_INDEX.DEV_MM520 || devIndex == Sharp_SDK.DEVICE_INDEX.DEV_MM530)
                {
                    if (iColumn > 2)
                    {
                        iColumn = 2;
                    }
                }
                if (bPressed)
                {
                    Sharp_SDK.SDK.SetLedColor(iRow, iColumn, 255, 0, 0);
                }
                else
                {
                    Sharp_SDK.SDK.SetLedColor(iRow, iColumn, 0, 0, 0);
                }
            }
        }

        // TODO: Implement CPU Status on button click
        private void ButtonCPUStatus_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine(tbCPUUsage.Text);
        }

        private Sharp_SDK.KEY_COLOR SetCurrentKeyColor(string strRedColor, string strGreenColor, string strBlueColor)
        {
            // Convert the text of the LED color text-boxes to a valid color value
            byte redColor = GetColorValue(strRedColor); // Convert the Red Color
            byte greenColor = GetColorValue(strGreenColor); // Convert the Green Color
            byte blueColor = GetColorValue(strBlueColor); // Convert the Blue Color

            return SetCurrentKeyColor(redColor, greenColor, blueColor);
        }

        private Sharp_SDK.KEY_COLOR SetCurrentKeyColor(byte redColor, byte greenColor, byte blueColor)
        {
            // Get the currently selected key row and column
            int row = cbLEDRow.SelectedIndex;
            int column = cbLEDColumn.SelectedIndex;

            // Get the current Key color of the selected key
            Sharp_SDK.KEY_COLOR keyColor = colorMatrix.KeyColor[row][column];

            // Update the Key Color with the new values
            keyColor.r = redColor; // Update the red channel
            keyColor.g = greenColor; // Update the green channel
            keyColor.b = blueColor; // Update the blue channel
            // Normalize the colors in-case of a non-RGB LED device
            keyColor = NormalizeColors(keyColor);

            // Set the Color of the key with the new Key Color
            colorMatrix.KeyColor[row][column] = keyColor;

            return keyColor;
        }

        /// <summary>
        /// Check if the device only supports a Single LED Colors or RGB Colors
        /// </summary>
        /// <param name="device"></param>
        /// <returns>True when Single Color LED. False if supports RGB Colors</returns>
        private bool IsSingleColorLed(Sharp_SDK.DEVICE_INDEX device)
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

        /// <summary>
        /// Normalize the Key Color channels
        /// 
        /// For Single Color LEDs, sets the Green and Blue channels to the same as the Red channel.
        /// 
        /// Otherwise, for RGB LEDs, just returns the supplied Key Color
        /// </summary>
        /// <param name="keyColor">The Key Color to normalize</param>
        /// <returns>Normalized Key Color</returns>
        private Sharp_SDK.KEY_COLOR NormalizeColors(Sharp_SDK.KEY_COLOR keyColor)
        {
            int selectedIndex = cbDeviceSelect.SelectedIndex;
            Sharp_SDK.DEVICE_INDEX selectedDevice = (Sharp_SDK.DEVICE_INDEX)selectedIndex;
            return NormalizeColors(keyColor, selectedDevice);
        }

        /// <summary>
        /// Normalize the Key Color channels
        /// 
        /// For Single Color LEDs, sets the Green and Blue channels to the same as the Red channel.
        /// 
        /// Otherwise, for RGB LEDs, just returns the supplied Key Color
        /// </summary>
        /// <param name="keyColor">The Key Color to normalize</param>
        /// <param name="selectedDevice">The device to check against (if is Single Color LEDs or RGB LEDs)</param>
        /// <returns>Normalized Key Color</returns>
        private Sharp_SDK.KEY_COLOR NormalizeColors(Sharp_SDK.KEY_COLOR keyColor, Sharp_SDK.DEVICE_INDEX selectedDevice)
        {
            if (IsSingleColorLed(selectedDevice))
            {
                // Set the other channels to the same as the Red channel
                keyColor.g = keyColor.r; // Green channel
                keyColor.b = keyColor.r; // Blue channel
            }
            return keyColor;
        }

        /// <summary>
        /// Tries to get and clamp the string color value within correct values
        /// </summary>
        /// <param name="strColor">The string color value</param>
        /// <returns>A correct color value</returns>
        private byte GetColorValue(string strColor)
        {
            // Declare _out_ variable inlined (see: https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7#out-variables)
            int.TryParse(strColor, out int iColor);
            if (iColor > MAX_COLOR_VALUE)
            {
                // Clamp color if greater than MAX_COLOR_VALUE
                iColor = MAX_COLOR_VALUE; // Set to MAX_COLOR_VALUE
            }
            else if (iColor < 0)
            {
                // Set the color to 0 if less than that
                iColor = 0; // Set to 0
            }
            return (byte)iColor; // Return as byte
        }

        /// <summary>
        /// Update the Key Color for all keys with the value a new value
        /// </summary>
        private void UpdateAllLEDColor()
        {
            UpdateAllLEDColor(keyColorAll, keyColorAll.r, keyColorAll.g, keyColorAll.b);
        }

        /// <summary>
        /// Update the Key Color for all keys with the value a new value
        /// </summary>
        /// <param name="redColor">Color value for Red channel</param>
        /// <param name="greenColor">Color value for Green channel</param>
        /// <param name="blueColor">Color value for Blue channel</param>
        private void UpdateAllLEDColor(byte redColor, byte greenColor, byte blueColor)
        {
            UpdateAllLEDColor(keyColorAll, redColor, greenColor, blueColor);
        }

        /// <summary>
        /// Update the Key Color for all keys with the value a new value
        /// </summary>
        /// <param name="keyColor">The Key Color to update the values from</param>
        /// <param name="redColor">Color value for Red channel</param>
        /// <param name="greenColor">Color value for Green channel</param>
        /// <param name="blueColor">Color value for Blue channel</param>
        private void UpdateAllLEDColor(Sharp_SDK.KEY_COLOR keyColor, byte redColor, byte greenColor, byte blueColor)
        {
            // Update the Key Color with the new values
            keyColor.r = redColor; // Update the red channel
            keyColor.g = greenColor; // Update the green channel
            keyColor.b = blueColor; // Update the red channel
            // Normalize the colors in-case of a non-RGB LED device
            keyColor = NormalizeColors(keyColor);

            // Update the LED Color Picker value
            System.Windows.Media.Color currColor = (System.Windows.Media.Color)clrPickerLED_All.SelectedColor;
            currColor.R = keyColor.r;
            currColor.G = keyColor.g;
            currColor.B = keyColor.b;
            clrPickerLED_All.SelectedColor = currColor;

            // Set the Key Color for all keys with the new Key Color
            keyColorAll = keyColor;
        }

        // Changes to the selected index of either LED Row or LED Column
        private void ComboBoxLED_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update the Key Color text-boxes with the new values
            UpdateSelectedLEDColor();
        }

        /// <summary>
        /// Update the Key Color text-boxes with the new values based on the selected Key Row and Column
        /// </summary>
        private void UpdateSelectedLEDColor()
        {
            int row = 0; // Default to 0
            int column = 0; // Default to 0
            if (initialized == true)
            {
                // Only update the Key Color text-boxes if the window has actually been initialized and loaded
                if (cbLEDRow != null && cbLEDColumn != null)
                {
                    // Only update the LED row & column index when both the LED Row & Column Combo boxes has been loaded
                    row = cbLEDRow.SelectedIndex;
                    column = cbLEDColumn.SelectedIndex;
                }
                // Get the current Key Color from the currently selected Key row and column
                Sharp_SDK.KEY_COLOR keyColor = colorMatrix.KeyColor[row][column];
                // Normalize the color in-case of Single Color LED device
                keyColor = NormalizeColors(keyColor);

                // Update the Key LED Color Picker value
                System.Windows.Media.Color currColor = (System.Windows.Media.Color)clrPickerLED.SelectedColor;
                currColor.R = keyColor.r;
                currColor.G = keyColor.g;
                currColor.B = keyColor.b;
                clrPickerLED.SelectedColor = currColor;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Disable the LED Control when closing the window (revert changes to the LED colors)
            Sharp_SDK.SDK.EnableLedControl(false);
            Sharp_SDK.SDK.SwitchLedEffect(Sharp_SDK.EFF_INDEX.EFF_FULL_ON); // Revert to "Full On"
        }

        // Save the Default Settings
        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            // Create new Device Settings object
            DeviceSettings deviceSettings = new DeviceSettings(currDevice)
            {
                // Simplified initialization
                ColorMatrix = colorMatrix, // Store the current Colors set for each key
                KeyColorAll = keyColorAll, // Store the current Key Color for All keys
                SelectedEffect = currEffect, // Store the current Effect
                BoundExe = null // No bound process/EXE for default Device Settings
            };

            // Convert the Device Settings to a JSON object string
            string output = JsonConvert.SerializeObject(deviceSettings);
            // Store the JSON string in User (Default) Settings, as the Default Device Settings
            Properties.Settings.Default.DefaultDeviceSettingsJson = output + "";
            Properties.Settings.Default.Save(); // Save the User (Default) Settings
        }

        // Load the Default Settings
        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            // Get the Default Device Settings JSON object string
            string output = Properties.Settings.Default.DefaultDeviceSettingsJson + "";
            // Convert the JSON object string to a Device Settings object
            DeviceSettings deviceSettings = JsonConvert.DeserializeObject<DeviceSettings>(output);
            if (deviceSettings != null)
            {
                if (deviceSettings.ColorMatrix.KeyColor != null)
                {
                    colorMatrix = deviceSettings.ColorMatrix; // Load the current Colors set for each key
                }
                keyColorAll = deviceSettings.KeyColorAll; // Load the current Key Color for All keys
                currDevice = deviceSettings.SelectedDevice;
                currEffect = deviceSettings.SelectedEffect;

                cbDeviceSelect.SelectedIndex = (int)currDevice;
                cbLEDEffectChoose.SelectedIndex = (int)currEffect;
            }

            // No bound process/EXE for default Device Settings
            boundExe = null;
            tbProfileExe.Text = "";

            // Update the value for the currently selected key
            // Not needed to update each key at this time (the others will be updated the LED Row or Column selection changes)
            UpdateSelectedLEDColor();

            // Update the current value for the All Keys
            UpdateAllLEDColor();

            SetDevice(currDevice);
            SetLedEffect(currEffect);
        }

        private void ButtonProfileSave_Click(object sender, RoutedEventArgs e)
        {
            int selectedProfile = cbProfile.SelectedIndex;

            // Create new Device Settings object
            DeviceSettings deviceSettings = new DeviceSettings(currDevice)
            {
                // Simplified initialization
                ColorMatrix = colorMatrix, // Store the current Colors set for each key
                KeyColorAll = keyColorAll, // Store the current Key Color for All keys
                SelectedEffect = currEffect // Store the current Effect
            };
            
            if (String.IsNullOrWhiteSpace(boundExe))
            {
                deviceSettings.BoundExe = null;
            } else
            {
                deviceSettings.BoundExe = boundExe + "";
            }

            // Convert the Device Settings to a JSON object string
            string output = JsonConvert.SerializeObject(deviceSettings);
            // Store the JSON string in User (Default) Settings, as the Default Device Settings
            Properties.Settings.Default.ProfilesDeviceSettingsJson[selectedProfile] = output + "";
            Properties.Settings.Default.Save(); // Save the User (Default) Settings
        }

        private void ButtonProfileLoad_Click(object sender, RoutedEventArgs e)
        {
            int selectedProfile = cbProfile.SelectedIndex;

            // Get the Default Device Settings JSON object string
            string output = Properties.Settings.Default.ProfilesDeviceSettingsJson[selectedProfile] + "";

            LoadDeviceSettings(output);
        }

        private void LoadDeviceSettings(string settings)
        {
            // Convert the JSON object string to a Device Settings object
            DeviceSettings deviceSettings = JsonConvert.DeserializeObject<DeviceSettings>(settings);
            LoadDeviceSettings(deviceSettings);
        }

        private void LoadDeviceSettings(DeviceSettings deviceSettings)
        {
            if (deviceSettings != null)
            {
                if (deviceSettings.ColorMatrix.KeyColor != null)
                {
                    colorMatrix = deviceSettings.ColorMatrix; // Load the current Colors set for each key
                }
                keyColorAll = deviceSettings.KeyColorAll; // Load the current Key Color for All keys
                currDevice = deviceSettings.SelectedDevice;
                currEffect = deviceSettings.SelectedEffect;

                cbDeviceSelect.SelectedIndex = (int)currDevice;
                cbLEDEffectChoose.SelectedIndex = (int)currEffect;

                if (String.IsNullOrWhiteSpace(deviceSettings.BoundExe))
                {
                    boundExe = null;
                }
                else
                {
                    boundExe = deviceSettings.BoundExe + "";
                }
            }

            // Update the value for the currently selected key
            // Not needed to update each key at this time (the others will be updated the LED Row or Column selection changes)
            UpdateSelectedLEDColor();

            // Update the current value for the All Keys
            UpdateAllLEDColor();

            SetDevice(currDevice);
            SetLedEffect(currEffect);

            if (String.IsNullOrWhiteSpace(boundExe))
            {
                tbProfileExe.Text = "";
            }
            else
            {
                tbProfileExe.Text = boundExe + "";
            }
        }

        // Bind a Profile to a process/EXE
        private void ButtonProfileBind_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(tbProfileExe.Text))
            {
                boundExe = null;
            }
            else
            {
                boundExe = tbProfileExe.Text + "";
            }
        }

        // Export Device Settings to File
        private void ButtonExportFile_Click(object sender, RoutedEventArgs e)
        {
            // Create new Device Settings object
            DeviceSettings deviceSettings = new DeviceSettings(currDevice)
            {
                // Simplified initialization
                ColorMatrix = colorMatrix, // Store the current Colors set for each key
                KeyColorAll = keyColorAll, // Store the current Key Color for All keys
                SelectedEffect = currEffect // Store the current Effect
            };
            
            if (String.IsNullOrWhiteSpace(boundExe))
            {
                deviceSettings.BoundExe = null;
            }
            else
            {
                deviceSettings.BoundExe = boundExe + "";
            }

            JsonSerializer serializer = new JsonSerializer()
            {
                // Simplified initialization
                Formatting = Formatting.Indented
            };


            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                // Simplified initialization
                Filter = "JSON Device Settings File|*.json",
                Title = "Save a Device Settings/Profile"
            };
            
            saveFileDialog.ShowDialog();

            // If the file name is not an empty string, open it for saving.
            if (String.IsNullOrWhiteSpace(saveFileDialog.FileName) == false)
            {
                FileStream fs = (FileStream)saveFileDialog.OpenFile();
                using (StreamWriter sw = new StreamWriter(fs))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, deviceSettings);
                }
                fs.Close();
            }
        }

        // Import Device Settings from File
        private void ButtonImportFile_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("#TODO: Please Implement Me :)", "Not Yet Implemented...");
            DeviceSettings deviceSettings = null;

            JsonSerializer serializer = new JsonSerializer()
            {
                // Simplified initialization
                Formatting = Formatting.Indented
            };

            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                // Simplified initialization
                Filter = "JSON Device Settings File|*.json",
                Title = "Open a Device Settings/Profile"
            };
            
            openFileDialog.ShowDialog();

            if (String.IsNullOrWhiteSpace(openFileDialog.FileName) == false)
            {
                FileStream fs = (FileStream)openFileDialog.OpenFile();
                using (StreamReader sr = new StreamReader(fs))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    // Convert the JSON object string to a Device Settings object
                    deviceSettings = serializer.Deserialize<DeviceSettings>(reader);
                }
                fs.Close();
            }

            if (deviceSettings != null)
            {
                LoadDeviceSettings(deviceSettings);
            }
        }

        private void ColorPickerLED_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            if (this.IsInitialized == true)
            {
                System.Windows.Media.Color currColor = clrPickerLED.SelectedColor.Value;
                bool isSingleColor = IsSingleColorLed(this.currDevice);
                if (isSingleColor)
                {
                    currColor.G = currColor.R;
                    currColor.B = currColor.R;
                    currColor.A = 128;

                    clrPickerLED.SelectedColor = currColor;
                }

                Sharp_SDK.KEY_COLOR keyColor = SetCurrentKeyColor(currColor.R, currColor.G, currColor.B);
            }
        }

        private void ColorPickerLED_All_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<System.Windows.Media.Color?> e)
        {
            if (this.IsInitialized == true)
            {
                System.Windows.Media.Color currColor = clrPickerLED_All.SelectedColor.Value;
                bool isSingleColor = IsSingleColorLed(this.currDevice);
                if (isSingleColor)
                {
                    currColor.G = currColor.R;
                    currColor.B = currColor.R;
                    currColor.A = 128;

                    clrPickerLED_All.SelectedColor = currColor;
                }

                // Update the Key Color for all keys with the new values
                UpdateAllLEDColor(currColor.R, currColor.G, currColor.B);
            }
        }

        void GetKeyRowColumnKeyCallback(int iRow, int iColumn, bool bPressed)
        {
            Console.WriteLine(iRow + ":" + iColumn + " | " + bPressed);
            if (bPressed == false)
            {
                // Key Up
                Dispatcher.BeginInvoke(new Action(() => SetSelectedLEDRowAndColumn(iRow, iColumn)));
            }
        }

        private void SetSelectedLEDRowAndColumn(int row, int column)
        {
            cbLEDRow.SelectedIndex = row + 0;
            cbLEDColumn.SelectedIndex = column + 0;
        }

        private void CheckboxGetKeyRowColumn_Checked(object sender, RoutedEventArgs e)
        {
            Sharp_SDK.SDK.SetKeyCallBack(this.getKeyRowColumnCallback);
            Sharp_SDK.SDK.EnableKeyInterrupt(true);
        }

        private void CheckboxGetKeyRowColumn_Unchecked(object sender, RoutedEventArgs e)
        {
            Sharp_SDK.SDK.SetKeyCallBack(null);
            Sharp_SDK.SDK.EnableKeyInterrupt(false);
        }
    }
}
