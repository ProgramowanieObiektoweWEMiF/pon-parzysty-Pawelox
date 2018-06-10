using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Data.SqlClient;

namespace turbodiodpol
{

    public partial class Form1 : Form
    {
        int port;
        dane datas = new dane();
        dane datas2 = new dane();
        string log_text;
        string has_text;
        string log_odb;
        string has_odb;
        string connectionString = @"Data Source = LAPTOP-NI7C0AEB; Initial Catalog = UserRegistrationDB; Integrated Security = True;";
        string arduino_port;
        int arduino_baud_rate;
        double barwa = 0;
        double nasycenie = 0;
        double jasnosc = 0;
        HSV data = new HSV(0, 0, 0);
        HSV data2 = new HSV(0, 0, 0);
        RGB value = new RGB();
        RGB value2 = new RGB();
        BinaryWriter writer;
        public TcpClient klient;
        MemoryStream ms = new MemoryStream();
        BinaryFormatter bf = new BinaryFormatter();
        byte[] userDataBytes;
        byte[] userDataLen;
        //odtąd dodane z serwera
        BinaryReader reader;
        IPAddress adresIP = null;
        public TcpListener serwer = null;
        byte[] readMsgLen = new byte[4];

      public Form1()
      {
           InitializeComponent();
           lista();
      }

       public void lista()
       {
            string[] ports = SerialPort.GetPortNames();
           foreach (string port in ports)
             {
                comboBox1.Items.Insert(0, port);
             }
           if (ports.Length > 0)
           {
               comboBox1.SelectedIndex = 0;
           }
        }

        [Serializable]
        public struct dane
        {
            private string login;
            private string haslo;

            public dane(string login, string haslo)
            {
                this.login = login;
                this.haslo = haslo;
            }

            public string LOGIN
            {
                get { return this.login; }
                set { this.login = value; }
            }

            public string HASLO
            {
                get { return this.haslo; }
                set { this.haslo = value; }
            }
        }

        [Serializable]
        public struct RGB
        {
            public byte _r;
            public byte _g;
            public byte _b;

            public RGB(byte r, byte g, byte b)
            {
                this._r = r;
                this._g = g;
                this._b = b;
            }

            public byte R
            {
                get { return this._r; }
                set { this._r = value; }
            }

            public byte G
            {
                get { return this._g; }
                set { this._g = value; }
            }

            public byte B
            {
                get { return this._b; }
                set { this._b = value; }
            }

            public bool Equals(RGB rgb)
            {
                return (this.R == rgb.R) && (this.G == rgb.G) && (this.B == rgb.B);
            }
        }

        public struct HSV
        {
            private double _h;
            private double _s;
            private double _v;

            public HSV(double h, double s, double v)
            {
                this._h = h;
                this._s = s;
                this._v = v;
            }

            public double H
            {
                get { return this._h; }
                set { this._h = value; }
            }

            public double S
            {
                get { return this._s; }
                set { this._s = value; }
            }

            public double V
            {
                get { return this._v; }
                set { this._v = value; }
            }

            public bool Equals(HSV hsv)
            {
                return (this.H == hsv.H) && (this.S == hsv.S) && (this.V == hsv.V);
            }
        }

        public static RGB HSVToRGB(HSV hsv)
        {
            double r = 0, g = 0, b = 0;

            if (hsv.S == 0)
            {
                r = hsv.V;
                g = hsv.V;
                b = hsv.V;
            }
            else
            {
                int i;
                double f, p, q, t;

                if (hsv.H == 360)
                    hsv.H = 0;
                else
                    hsv.H = hsv.H / 60;

                i = (int)Math.Truncate(hsv.H);
                f = hsv.H - i;

                p = hsv.V * (1.0 - hsv.S);
                q = hsv.V * (1.0 - (hsv.S * f));
                t = hsv.V * (1.0 - (hsv.S * (1.0 - f)));

                switch (i)
                {
                    case 0:
                        r = hsv.V;
                        g = t;
                        b = p;
                        break;

                    case 1:
                        r = q;
                        g = hsv.V;
                        b = p;
                        break;

                    case 2:
                        r = p;
                        g = hsv.V;
                        b = t;
                        break;

                    case 3:
                        r = p;
                        g = q;
                        b = hsv.V;
                        break;

                    case 4:
                        r = t;
                        g = p;
                        b = hsv.V;
                        break;

                    default:
                        r = hsv.V;
                        g = p;
                        b = q;
                        break;
                }

            }

            return new RGB((byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
        }

        private void startButton_Click(object sender, EventArgs e)
        {
            if (!arduino.IsOpen)
            {
                if (comboBox1.SelectedItem != null)
                {
                    this.arduino_port = comboBox1.SelectedItem.ToString();
                    this.arduino_baud_rate = 9600;
                    arduino.PortName = this.arduino_port;
                    arduino.BaudRate = this.arduino_baud_rate;
                }
                else System.Windows.Forms.MessageBox.Show("Musisz wybrać wartość z listy!");

                try
                {
                    arduino.Open();
                    ListaStanu.Items.Clear();
                    ListaStanu.Items.Insert(0, "Połączono");
                }
                catch
                {
                    MessageBox.Show("Nie udało się zainicjować połączenia!");
                }
            }
            else
            {
                arduino.Write(new[] { (Byte)0, (Byte)0, (Byte)0 }, 0, 3);
                suwak1.Value = suwak1.Minimum;
                suwak2.Value = suwak2.Minimum;
                suwak3.Value = suwak3.Minimum;
                arduino.Close();
                ListaStanu.Items.Clear();
                ListaStanu.Items.Insert(0, "Rozłączono");
                KolorDiody.BackColor = Color.FromArgb(0, 0, 0);
                KolorR.BackColor = Color.FromArgb(0, 0, 0);
                KolorG.BackColor = Color.FromArgb(0, 0, 0);
                KolorB.BackColor = Color.FromArgb(0, 0, 0);
            }
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            lista();
        }

        
        private void button2_Click(object sender, EventArgs e) //przycisk do połączenia się TCP
        {
            string host = textBox1.Text;
            int port = System.Convert.ToInt16(numericUpDown1.Value);
            try
            {
                klient = new TcpClient(host, port);
                NetworkStream clientStream = klient.GetStream();
                writer = new BinaryWriter(clientStream);
                backgroundWorker2.WorkerSupportsCancellation = true;
                listBox1.Items.Add("Nawiązano połączenie z " + host + " na porcie:" + port);
                button5.Enabled = false;
                button6.Enabled = false;
                button4.Enabled = true;
                button2.Enabled = false;
                button7.Enabled = true;
            }
            catch(Exception ex)
            {
                listBox1.Items.Add("Błąd: Nie udało się nawiązać połączenia!");
                MessageBox.Show("Nie udało się znaleźć serwera o podanej nazwie!");
            }

        }

        private void button4_Click(object sender, EventArgs e) //przycisk do wysyłania kolorów
        {
            backgroundWorker2.RunWorkerAsync();
        }

        private void backgroundWorker2_DoWork_1(object sender, DoWorkEventArgs e)// wątek do wysłania kolorów
        {
            if(klient.Connected)
            {
                this.suwak1.Invoke(new MethodInvoker(delegate() { barwa = (double)suwak1.Value; }));
                this.suwak2.Invoke(new MethodInvoker(delegate() { nasycenie = ((double)suwak2.Value) / 100; }));
                this.suwak3.Invoke(new MethodInvoker(delegate() { jasnosc = ((double)suwak3.Value) / 100; }));
                data = new HSV(barwa, nasycenie, jasnosc);
                value = HSVToRGB(data);
                bf.Serialize(ms, value);
                ms.Seek(0, SeekOrigin.Begin);
                userDataBytes = ms.ToArray();
                ms.Flush();
                userDataLen = BitConverter.GetBytes((Int32)userDataBytes.Length);
                writer.Write(userDataLen, 0, 4);
                writer.Write(userDataBytes, 0, (Int32)userDataBytes.Length);
            }
            else
            {
                MessageBox.Show("Wysyłanie nieudane!");
            }
            backgroundWorker2.CancelAsync();
        }

        private void button5_Click(object sender, EventArgs e) //start serwera
        {
            button4.Enabled = false;
            button2.Enabled = false;
            backgroundWorker4.WorkerSupportsCancellation = true;
            backgroundWorker4.RunWorkerAsync();
        }

        private void button6_Click(object sender, EventArgs e)//stop serwera
        {
            try
            {
                serwer.Stop();
                if (!klient.Connected)
                {
                    klient.Close();
                }
                
                listBox1.Items.Add("Zakończono pracę serwera ...");
                button5.Enabled = true;
                button6.Enabled = false;
                button4.Enabled = true;
                button2.Enabled = true;
                KolorR.BackColor = Color.FromArgb(0, 0, 0);
                KolorG.BackColor = Color.FromArgb(0, 0, 0);
                KolorB.BackColor = Color.FromArgb(0, 0, 0);
                KolorDiody.BackColor = Color.FromArgb(0, 0, 0);
            }
            catch
            {
                listBox1.Items.Add("Zakończono pracę serwera ...");
                //button5.Enabled = true;
                button6.Enabled = false;
                button4.Enabled = true;
                button2.Enabled = true;
                KolorR.BackColor = Color.FromArgb(0, 0, 0);
                KolorG.BackColor = Color.FromArgb(0, 0, 0);
                KolorB.BackColor = Color.FromArgb(0, 0, 0);
                KolorDiody.BackColor = Color.FromArgb(0, 0, 0);
                button6.Enabled = false;
                DialogResult dr;
                dr = MessageBox.Show("Nie jesteś połączony!");
                if (dr == DialogResult.OK)
                   button5.Enabled = true;
            }
        }

        private void backgroundWorker3_DoWork(object sender, DoWorkEventArgs e)//wątek do odczytu
        {
            while (klient.Connected)
            {
                try
                {
                    reader.Read(readMsgLen, 0, 4);
                }
                catch
                {
                    MessageBox.Show("Klient został odłączony!");
                    serwer.Stop();
                    if (!klient.Connected)
                    {
                        klient.Close();
                    }
                    this.listBox1.Invoke(new MethodInvoker(delegate() { listBox1.Items.Add("Zakończono pracę serwera ..."); }));
                    this.button5.Invoke(new MethodInvoker(delegate() { button5.Enabled = true; }));
                    this.button6.Invoke(new MethodInvoker(delegate() { button6.Enabled = false; }));
                    this.button4.Invoke(new MethodInvoker(delegate() { button4.Enabled = true; }));
                    this.button2.Invoke(new MethodInvoker(delegate() { button2.Enabled = true; }));
                    this.KolorR.Invoke(new MethodInvoker(delegate() { KolorR.BackColor = Color.FromArgb(0, 0, 0); }));
                    this.KolorG.Invoke(new MethodInvoker(delegate() { KolorG.BackColor = Color.FromArgb(0, 0, 0); }));
                    this.KolorB.Invoke(new MethodInvoker(delegate() { KolorB.BackColor = Color.FromArgb(0, 0, 0); }));
                    this.KolorDiody.Invoke(new MethodInvoker(delegate() { KolorDiody.BackColor = Color.FromArgb(0, 0, 0); }));
                    break;
                }
                int dataLen = BitConverter.ToInt32(readMsgLen, 0);
                byte[] readMsgData = new byte[dataLen];
                reader.Read(readMsgData, 0, dataLen);
                MemoryStream ms = new MemoryStream(readMsgData);
                BinaryFormatter bf1 = new BinaryFormatter();
                ms.Position = 0;
                ms.Seek(0, SeekOrigin.Begin);

                try
                {
                    datas2 = (dane)bf1.Deserialize(ms);
                    log_odb = datas2.LOGIN;
                    has_odb = datas2.HASLO;
                    this.textBox2.Invoke(new MethodInvoker(delegate () { textBox2.AppendText("To jest login : " + log_odb + "\n To jest haslo : " + has_odb); }));

                    if (log_odb != "" && has_odb != "")
                    {
                        using (SqlConnection sqlCon = new SqlConnection(connectionString))
                        {
                            sqlCon.Open();
                            SqlCommand sqlCmd = new SqlCommand("UserAdd", sqlCon);
                            sqlCmd.CommandType = CommandType.StoredProcedure;
                            sqlCmd.Parameters.AddWithValue("@Username", log_odb.Trim());
                            sqlCmd.Parameters.AddWithValue("@Password", has_odb.Trim());
                            sqlCmd.ExecuteNonQuery();
                            MessageBox.Show("Rejestracja kompletna");
                            Clear();
                            log_odb = "";
                            has_odb = "";
                        }
                    }
                }
                catch
                {
                    ms.Position = 0;
                    value2 = (RGB)bf1.Deserialize(ms);
                    backgroundWorker7.WorkerSupportsCancellation = true;
                    backgroundWorker5.RunWorkerAsync();
                    backgroundWorker7.RunWorkerAsync();
                }
            }
        }

        private void backgroundWorker4_DoWork(object sender, DoWorkEventArgs e)//wątek do serwera
        {
            try
            {
                adresIP = IPAddress.Parse(textBox1.Text);
            }
            catch
            {
                MessageBox.Show("Błędny format adresu IP!", "Błąd");
                this.textBox1.Invoke(new MethodInvoker(delegate() { textBox1.Text = String.Empty; }));
                return;
            }
            port = System.Convert.ToInt16(numericUpDown1.Value);
            try
            {
                serwer = new TcpListener(adresIP, port);
                this.listBox1.Invoke(new MethodInvoker(delegate() { listBox1.Items.Add("Oczekuję na połączenie ..."); }));
                serwer.Start();
                this.button5.Invoke(new MethodInvoker(delegate() { button5.Enabled = false; }));
                this.button6.Invoke(new MethodInvoker(delegate() { button6.Enabled = true; }));
                this.button7.Invoke(new MethodInvoker(delegate () { button7.Enabled = false; }));
                this.startButton.Invoke(new MethodInvoker(delegate () { startButton.Enabled = true; }));
                klient = serwer.AcceptTcpClient();
                this.listBox1.Invoke(new MethodInvoker(delegate() { listBox1.Items.Add("Nawiązano połączenie"); }));
                NetworkStream stream = klient.GetStream();
                reader = new BinaryReader(stream);

                backgroundWorker3.RunWorkerAsync(); //Uruchamia wątek do odbierania wiadomości
                backgroundWorker4.CancelAsync();

            }
            catch (Exception ex)
            {
                this.listBox1.Invoke(new MethodInvoker(delegate() { listBox1.Items.Add("Błąd inicjacji serwera!"); }));
                this.button5.Invoke(new MethodInvoker(delegate () { button5.Enabled = false; }));
                DialogResult xd;
                xd = MessageBox.Show("Nie udało się zainicjalizować serwera!");
                if (xd == DialogResult.OK)
                    this.button5.Invoke(new MethodInvoker(delegate () { button1.Enabled = true; }));
                    this.button2.Invoke(new MethodInvoker(delegate () { button4.Enabled = true; }));
            }
        }

        private void backgroundWorker5_DoWork(object sender, DoWorkEventArgs e)
        {
            this.KolorR.Invoke(new MethodInvoker(delegate() { KolorR.BackColor = Color.FromArgb(value.R, 0, 0); }));
            this.KolorG.Invoke(new MethodInvoker(delegate() { KolorG.BackColor = Color.FromArgb(0, value.G, 0); }));
            this.KolorB.Invoke(new MethodInvoker(delegate() { KolorB.BackColor = Color.FromArgb(0, 0, value.B); }));
            this.KolorDiody.Invoke(new MethodInvoker(delegate() { KolorDiody.BackColor = Color.FromArgb(value.R, value.G, value.B); }));
        }

        private void button7_Click(object sender, EventArgs e)
        {
            if (textBox4.Text == "" || textBox5.Text == "")
                MessageBox.Show("Wypełnij pola!");
            else
            {
                log_text = textBox4.Text.Trim();
                has_text = textBox5.Text.Trim();
                datas = new dane(log_text, has_text);
                backgroundWorker6.WorkerSupportsCancellation = true;
                backgroundWorker6.RunWorkerAsync();
            }
        }

        void Clear()
        {
            textBox4.Text = textBox5.Text = "";
        }

        private void backgroundWorker6_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                bf.Serialize(ms, datas);
                ms.Seek(0, SeekOrigin.Begin);
                userDataBytes = ms.ToArray();
                ms.Flush();
                userDataLen = BitConverter.GetBytes((Int32)userDataBytes.Length);
                writer.Write(userDataLen, 0, 4);
                writer.Write(userDataBytes, 0, (Int32)userDataBytes.Length);
                Clear();
            }
            catch
            {
                this.button7.Invoke(new MethodInvoker(delegate () { button7.Enabled = false; }));
                DialogResult dr;
                dr = MessageBox.Show("Nie jesteś połączony!");
                if (dr == DialogResult.OK)
                    this.button7.Invoke(new MethodInvoker(delegate () { button7.Enabled = true; }));
            }
            backgroundWorker6.CancelAsync();
        }

        private void backgroundWorker7_DoWork(object sender, DoWorkEventArgs e)
        {
            if (arduino.IsOpen)
            {
                arduino.Write(new[] { (Byte)value2.R, (Byte)value2.G, (Byte)value2.B }, 0, 3);
                this.KolorR.Invoke(new MethodInvoker(delegate () { KolorR.BackColor = Color.FromArgb(value2.R, 0, 0); }));
                this.KolorG.Invoke(new MethodInvoker(delegate () { KolorG.BackColor = Color.FromArgb(0, value2.G, 0); }));
                this.KolorB.Invoke(new MethodInvoker(delegate () { KolorB.BackColor = Color.FromArgb(0, 0, value2.B); }));
                this.KolorDiody.Invoke(new MethodInvoker(delegate () { KolorDiody.BackColor = Color.FromArgb(value2.R, value2.G, value2.B); }));
            }
            else
            {
                backgroundWorker7.CancelAsync();
            }
        }
    }
}