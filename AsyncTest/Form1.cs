using VSOP87;

namespace AsyncTest
{
    public partial class Form1 : Form
    {
        private Calculator vsop;

        public Form1()
        {
            InitializeComponent();
            vsop = new Calculator();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label_time.Text = DateTime.Now.ToString() + "." + DateTime.Now.Millisecond;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            DateTime Tinput = DateTime.Now;
            VSOPTime vTime = new VSOPTime(Tinput);
            foreach (VSOPVersion iv in Enum.GetValues(typeof(VSOPVersion)))
            {
                foreach (VSOPBody ib in Utility.AvailableBody(iv))
                {
                    await CallAsync(ib, iv, vTime);
                    await Task.Delay(500);
                }
            }
        }

        private async Task CallAsync(VSOPBody ibody, VSOPVersion iver, VSOPTime time)
        {
            var results = await vsop.GetPlanetPositionAsync(ibody, iver, time);
            label_version.Text = iver.ToString();
            label_body.Text = ibody.ToString();
            label1.Text = results.Variables[0].ToString();
            label2.Text = results.Variables[1].ToString();
            label3.Text = results.Variables[2].ToString();
            label4.Text = results.Variables[3].ToString();
            label5.Text = results.Variables[4].ToString();
            label6.Text = results.Variables[5].ToString();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}