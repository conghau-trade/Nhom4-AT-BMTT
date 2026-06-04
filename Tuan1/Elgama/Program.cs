using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows.Forms;
using System.Text;
using System.Drawing;

namespace ElgamalApp
{
    // --- LỚP CHẠY CHÍNH ---
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }

    // --- LỚP GIAO DIỆN VÀ LOGIC ---
    public partial class Form1 : Form
    {
        private RichTextBox txtBanRoTrai = new RichTextBox();
        private RichTextBox txtBanMaTrai = new RichTextBox();
        private RichTextBox txtBanMaPhai = new RichTextBox();
        private RichTextBox txtBanRoPhai = new RichTextBox();
        private Button btnMaHoa = new Button();
        private Button btnGiaiMa = new Button();
        private Button btnChuyen = new Button();

        BigInteger p = 263, a = 2, x = 6, y;

        public Form1()
        {
            y = BigInteger.ModPow(a, x, p);
            InitializeCustomComponent();
        }

        private void InitializeCustomComponent()
        {
            this.Text = "He ma Elgamal - Hung 29";
            this.Size = new Size(820, 500);

            txtBanRoTrai.Bounds = new Rectangle(20, 50, 300, 100);
            btnMaHoa.Bounds = new Rectangle(20, 160, 100, 30);
            btnMaHoa.Text = "Ma hoa";
            btnMaHoa.Click += btnMaHoa_Click;

            txtBanMaTrai.Bounds = new Rectangle(20, 200, 300, 100);
            btnChuyen.Bounds = new Rectangle(345, 200, 100, 35);
            btnChuyen.Text = "CHUYEN >>>";
            btnChuyen.Click += btnChuyen_Click;

            txtBanMaPhai.Bounds = new Rectangle(460, 50, 300, 100);
            btnGiaiMa.Bounds = new Rectangle(460, 160, 100, 30);
            btnGiaiMa.Text = "Giai ma";
            btnGiaiMa.Click += btnGiaiMa_Click;

            txtBanRoPhai.Bounds = new Rectangle(460, 200, 300, 100);

            this.Controls.AddRange(new Control[] { txtBanRoTrai, btnMaHoa, txtBanMaTrai, btnChuyen, txtBanMaPhai, btnGiaiMa, txtBanRoPhai });
        }

        private void btnMaHoa_Click(object? sender, EventArgs e)
        {
            try {
                byte[] bytes = Encoding.UTF8.GetBytes(txtBanRoTrai.Text);
                StringBuilder sb = new StringBuilder();
                BigInteger k = 3; 
                foreach (byte b in bytes) {
                    BigInteger M = new BigInteger(b);
                    BigInteger K = BigInteger.ModPow(y, k, p); 
                    BigInteger c1 = BigInteger.ModPow(a, k, p);
                    BigInteger c2 = (K * M) % p;
                    sb.Append(c1 + "-" + c2 + " ");
                }
                txtBanMaTrai.Text = sb.ToString().Trim();
            } catch { MessageBox.Show("Loi ma hoa!"); }
        }

        private void btnChuyen_Click(object? sender, EventArgs e) => txtBanMaPhai.Text = txtBanMaTrai.Text;

        private void btnGiaiMa_Click(object? sender, EventArgs e)
        {
            try {
                string[] pairs = txtBanMaPhai.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                List<byte> decodedBytes = new List<byte>();
                foreach (string pair in pairs) {
                    string[] parts = pair.Split('-');
                    BigInteger c1 = BigInteger.Parse(parts[0]);
                    BigInteger c2 = BigInteger.Parse(parts[1]);
                    BigInteger K_dec = BigInteger.ModPow(c1, x, p); 
                    BigInteger K_inv = ModInverse(K_dec, p);
                    BigInteger M_res = (c2 * K_inv) % p;
                    if (M_res < 0) M_res += p;
                    decodedBytes.Add((byte)M_res);
                }
                txtBanRoPhai.Text = Encoding.UTF8.GetString(decodedBytes.ToArray());
            } catch { MessageBox.Show("Loi giai ma!"); }
        }

        public static BigInteger ModInverse(BigInteger a, BigInteger n)
        {
            BigInteger t = 0, nt = 1, r = n, nr = a;
            while (nr != 0) {
                BigInteger q = r / nr;
                (t, nt) = (nt, t - q * nt);
                (r, nr) = (nr, r - q * nr);
            }
            return t < 0 ? t + n : t;
        }
    }
}