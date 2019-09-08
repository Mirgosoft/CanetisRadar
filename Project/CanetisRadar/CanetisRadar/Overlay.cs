using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio;
using NAudio.CoreAudioApi;
using IniParser;
using IniParser.Model;
using System.Globalization;

namespace CanetisRadar
{
    public partial class Overlay : Form
    {
        // -------------------------------------------------------
        // Variables
        // -------------------------------------------------------
        MMDeviceEnumerator enumerator;
        MMDevice device;

        float sizeMaxMultiplier = 1;        // Множитель допустимой громкости в процентах (от радиуса радара)
        float toPxMultiplier = 1;           // Итоговый множитель, на кот. умножается значение Громкости канала для получения значения в пикселях (рассчитывается динамически)
        int deadzoneVal = 0;                // Мертвая зона, начиная с которой показывается направление звука
        float maxChannelVal = 0;            // Максимально зарегистрированная громкость канала
        float temp_TopLeft = 0;
        float temp_TopRight = 0;
        float temp_BotLeft = 0;
        float temp_BotRight = 0;
        float sound_X = 0;
        float sound_Y = 0;
        string infotext = "";
        int channel_val = 0;
        bool debug = false;

        Pen linePen_Colored = new Pen(Color.Lime, 4);
        Pen linePen_Black = new Pen(Color.Black, 8);
        Pen radarRect_border = new Pen(Color.DarkCyan, 2);
        Pen radarRect_Cross = new Pen(Color.DarkCyan, 2);

        // -------------------------------------------------------
        // Dll Imports
        // -------------------------------------------------------
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public Overlay()
        {
            InitializeComponent();
        }

        private void Overlay_Load(object sender, EventArgs e)
        {
            this.TransparencyKey = Color.Turquoise;
            this.BackColor = Color.Turquoise;

            int initialStyle = GetWindowLong(this.Handle, -20);
            SetWindowLong(this.Handle, -20, initialStyle | 0x80000 | 0x20);

            this.WindowState = FormWindowState.Maximized;
            this.Opacity = 0.5;

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile("C:\\oPH64RlpL.ini");
            // Множитель допустимой громкости в процентах (от радиуса радара)
            // (Задайте более 1, если нужно отчетливее видеть изменения на малой громкости)
            string m = data["basic"]["fSizeMaxMultiplier"];
            sizeMaxMultiplier = float.Parse(m, CultureInfo.InvariantCulture);
            m = data["basic"]["fStartMaxChannelVal"];               // Стартовое значение максимально зарегистрированной громкости канала
            // Обновляем сразу и Итоговый множитель, на кот. умножается значение Громкости канала для получения значения в пикселях
            UpdateMaxChannelVal(float.Parse(m, CultureInfo.InvariantCulture));
            m = data["basic"]["bDebug"];                            // Включена ли дебаг инфа
            debug = m == "0" ? false : true;
            m = data["basic"]["iDeadzoneInPx"];                     // Мертвая зона, начиная с которой показывается направление звука
            deadzoneVal = Int32.Parse(m);

            Thread t = new Thread(Loop);
            t.Start();
        }

        // -------------------------------------------------------
        // Main Loop
        // -------------------------------------------------------
        public void Loop()
        {
            enumerator = new MMDeviceEnumerator();
            device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Console);

            if (device.AudioMeterInformation.PeakValues.Count < 8)
            {
                MessageBox.Show("Use7.1dev!", " ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(-1);
            }

            while (true)
            {
                // Текущий максимум среди всех каналов
                if (maxChannelVal < device.AudioMeterInformation.PeakValues[0]) 
                    UpdateMaxChannelVal(device.AudioMeterInformation.PeakValues[0]);
                if (maxChannelVal < device.AudioMeterInformation.PeakValues[1])
                    UpdateMaxChannelVal(device.AudioMeterInformation.PeakValues[1]);
                if (maxChannelVal < device.AudioMeterInformation.PeakValues[4])
                    UpdateMaxChannelVal(device.AudioMeterInformation.PeakValues[4]);
                if (maxChannelVal < device.AudioMeterInformation.PeakValues[5])
                    UpdateMaxChannelVal(device.AudioMeterInformation.PeakValues[5]);
                
                temp_TopLeft = device.AudioMeterInformation.PeakValues[0] * toPxMultiplier;
                temp_TopRight = device.AudioMeterInformation.PeakValues[1] * toPxMultiplier;
                temp_BotLeft = device.AudioMeterInformation.PeakValues[4] * toPxMultiplier;
                temp_BotRight = device.AudioMeterInformation.PeakValues[5] * toPxMultiplier;
                
                sound_X = 75 - temp_TopLeft + temp_TopRight;
                sound_Y = 75 - temp_TopLeft - temp_TopRight;

                sound_X = sound_X - temp_BotLeft + temp_BotRight;
                sound_Y = sound_Y + temp_BotLeft + temp_BotRight;

                if (sound_X < 5)        sound_X = 5;
                else if (sound_X > 145) sound_X = 145;
                if (sound_Y < 5)        sound_Y = 5;
                else if (sound_Y > 145) sound_Y = 145;

                //CreateRadar_Dot((int)sound_X, (int)sound_Y); // Render sound pos
                CreateRadar_Line((int)sound_X, (int)sound_Y); // Render sound pos

                // Debug info -------------------------------
                if (debug)
                {
                    infotext = "";
                    for (int i = 0; i < device.AudioMeterInformation.PeakValues.Count; i++)
                    {
                        if (maxChannelVal == 0)
                            break;

                        channel_val = (int)Math.Ceiling(device.AudioMeterInformation.PeakValues[i] / maxChannelVal * 100.0);

                        infotext += i + " -> " + channel_val + "\n";
                    }
                    infotext += "maxChannelVal -> " + maxChannelVal + "\n";
                    infotext += "toPxMultiplier -> " + toPxMultiplier + "\n";
                    label2.Invoke((MethodInvoker)delegate {
                        label2.Text = infotext;
                    });
                }
                // Debug info (END) --------------------------

                Thread.Sleep(10);
            }
        }

        public void UpdateMaxChannelVal(float max_channel_val) {
            maxChannelVal = max_channel_val;
            toPxMultiplier = 70 * sizeMaxMultiplier / maxChannelVal;
        }
        
        public void CreateRadar_Line(int x, int y)
        {
            Bitmap radar = new Bitmap(150, 150);
            Graphics grp = Graphics.FromImage(radar);
            grp.DrawRectangle(radarRect_border, 1, 1, 148, 148);
            grp.DrawLine(radarRect_Cross, 0, 75, radar.Width, 75);
            grp.DrawLine(radarRect_Cross, 75, 0, 75, radar.Height);

            if (Math.Abs(x - 75) < deadzoneVal && Math.Abs(y - 75) < deadzoneVal)
                grp.DrawLine(linePen_Black, 72, 72, 77, 77);
            else {
                grp.DrawLine(linePen_Black, 75, 75, x, y);
                grp.DrawLine(linePen_Colored, 75, 75, x, y);
            }

            pictureBox1.Invoke((MethodInvoker)delegate {
                pictureBox1.Image = radar;
            });
        }
        public void CreateRadar_Dot(int x, int y)
        {
            Bitmap radar = new Bitmap(150, 150);
            Graphics grp = Graphics.FromImage(radar);
            grp.FillRectangle(Brushes.Black, 0, 0, radar.Width, radar.Height);

            grp.FillRectangle(Brushes.Red, x - 5, y - 5, 10, 10);

            pictureBox1.Invoke((MethodInvoker)delegate {
                pictureBox1.Image = radar;
            });
        }
    }
}
