// ============================================================
//  CÔNG CỤ MÃ HÓA VÀ GIẢI MÃ ELGAMAL — C# WinForms
//  dotnet run  (net8.0-windows, UseWindowsForms=true)
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace ElGamalTool
{
    // ============================================================
    //  ElGamal Core
    // ============================================================
    public static class ElGamal
    {
        static readonly Random _rng = new Random();

        public static BigInteger ModPow(BigInteger b, BigInteger e, BigInteger m)
            => BigInteger.ModPow(b, e, m);

        public static BigInteger ModInverse(BigInteger a, BigInteger p)
            => BigInteger.ModPow(a, p - 2, p);

        public static bool IsPrime(long n)
        {
            if (n < 2) return false;
            if (n < 4) return true;
            if (n % 2 == 0 || n % 3 == 0) return false;
            for (long i = 5; i * i <= n; i += 6)
                if (n % i == 0 || n % (i + 2) == 0) return false;
            return true;
        }

        public static long PrimitiveRoot(long p)
        {
            long phi = p - 1;
            var factors = new List<long>();
            long n = phi;
            for (long i = 2; i * i <= n; i++)
                if (n % i == 0) { factors.Add(i); while (n % i == 0) n /= i; }
            if (n > 1) factors.Add(n);
            for (long g = 2; g < p; g++)
            {
                bool ok = true;
                foreach (var f in factors)
                    if (ModPow(g, phi / f, p) == 1) { ok = false; break; }
                if (ok) return g;
            }
            return 2;
        }

        public static long NearPrime(long n)
        {
            if (n < 3) n = 3; if (n % 2 == 0) n++;
            while (!IsPrime(n)) n += 2; return n;
        }

        public static (long p, long g, long x, long h) GenerateKeys()
        {
            long p = NearPrime(_rng.Next(500, 9999));
            long g = PrimitiveRoot(p);
            long x = _rng.Next(2, (int)(p - 2));
            long h = (long)ModPow(g, x, p);
            return (p, g, x, h);
        }

        public static List<(long c1, long c2)> Encrypt(long[] vals, long p, long g, long h)
        {
            var r = new List<(long, long)>();
            foreach (var m in vals)
            {
                long k  = _rng.Next(2, (int)(p - 2));
                long c1 = (long)ModPow(g, k, p);
                long c2 = (long)((long)ModPow(h, k, p) * m % p);
                r.Add((c1, c2));
            }
            return r;
        }

        public static long[] Decrypt(List<(long c1, long c2)> pairs, long p, long x)
        {
            var r = new long[pairs.Count];
            for (int i = 0; i < pairs.Count; i++)
            {
                var (c1, c2) = pairs[i];
                var s    = ModPow(c1, x, p);
                var sInv = ModInverse(s, p);
                r[i] = (long)(c2 * sInv % p);
            }
            return r;
        }

        public static string PairsToString(List<(long c1, long c2)> pairs)
            => string.Join("|", pairs.Select(p => $"{p.c1},{p.c2}"));

        public static List<(long c1, long c2)> ParsePairs(string s)
        {
            var list = new List<(long, long)>();
            foreach (var tok in s.Trim().Split('|'))
            {
                var pts = tok.Split(',');
                if (pts.Length != 2) throw new FormatException($"Cặp \"{tok}\" sai định dạng c1,c2");
                if (!long.TryParse(pts[0].Trim(), out long c1) ||
                    !long.TryParse(pts[1].Trim(), out long c2))
                    throw new FormatException($"Giá trị không phải số: \"{tok}\"");
                list.Add((c1, c2));
            }
            return list;
        }

        public static long[] ToNumbers(string input, string fmt)
        {
            switch (fmt)
            {
                case "Số":
                    return input.Trim().Split(new[] { ' ', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => long.TryParse(t, out var n) ? n : throw new FormatException($"Không phải số: \"{t}\"")).ToArray();
                case "Hex":
                    var h = input.Replace(" ","").Replace("\n","").Replace("\r","");
                    if (h.Length % 2 != 0) throw new FormatException("Hex phải có độ dài chẵn.");
                    return Enumerable.Range(0, h.Length/2).Select(i => (long)Convert.ToInt32(h.Substring(i*2,2),16)).ToArray();
                case "Base64":
                    return Convert.FromBase64String(input.Trim()).Select(b => (long)b).ToArray();
                default: // Văn bản
                    return Encoding.UTF8.GetBytes(input).Select(b => (long)b).ToArray();
            }
        }

        public static string FromNumbers(long[] nums, string fmt)
        {
            switch (fmt)
            {
                case "Hex":    return string.Concat(nums.Select(n => n.ToString("x2")));
                case "Base64": return Convert.ToBase64String(nums.Select(n => (byte)(n & 0xFF)).ToArray());
                default:       return Encoding.UTF8.GetString(nums.Select(n => (byte)(n & 0xFF)).ToArray());
            }
        }
    }

    // ============================================================
    //  Main Form
    // ============================================================
    public class MainForm : Form
    {
        static readonly Color cBG   = Color.FromArgb(245,247,252);
        static readonly Color cHDR  = Color.FromArgb(25,80,190);
        static readonly Color cACC  = Color.FromArgb(25,80,190);
        static readonly Color cOK   = Color.FromArgb(22,163,74);
        static readonly Color cERR  = Color.FromArgb(220,38,38);
        static readonly Color cWARN = Color.FromArgb(170,110,0);
        static readonly Color cLBL  = Color.FromArgb(40,55,80);
        static readonly Color cGRAY = Color.FromArgb(95,108,130);
        static readonly Font  fUI   = new Font("Segoe UI", 9.5f);
        static readonly Font  fBold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        static readonly Font  fMono = new Font("Consolas", 9.5f);

        // Keys
        long _p=2357, _g=2, _x=1751, _h;
        string _lastEnc = "";
        string _inFmt  = "Văn bản";
        string _outFmt = "Văn bản";

        // Controls
        TextBox txtP=null!, txtG=null!, txtX=null!;
        Label   lblH=null!, lblPG=null!, lblPX=null!;
        TextBox txtInput=null!, txtResult=null!;
        TextBox txtOrig=null!, txtSusp=null!;
        Label   lblStatus=null!, lblCmpRes=null!;
        Panel   pnlStatus=null!, pnlCmpRes=null!;
        TabControl tabs=null!;

        public MainForm()
        {
            SuspendLayout();
            Text          = "CÔNG CỤ MÃ HÓA VÀ GIẢI MÃ ELGAMAL";
            Size          = new Size(1000, 820);
            MinimumSize   = new Size(860, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = cBG;
            Font          = fUI;

            // Header
            var hdr = new Panel { Dock=DockStyle.Top, Height=52, BackColor=cHDR };
            hdr.Controls.Add(new Label {
                Text="CÔNG CỤ MÃ HÓA VÀ GIẢI MÃ ELGAMAL",
                Font=new Font("Segoe UI",14,FontStyle.Bold), ForeColor=Color.White,
                AutoSize=true, Location=new Point(18,13)
            });
            Controls.Add(hdr);

            // Status bar
            pnlStatus = new Panel { Dock=DockStyle.Bottom, Height=26, BackColor=Color.FromArgb(218,228,245) };
            lblStatus  = new Label { Dock=DockStyle.Fill, TextAlign=ContentAlignment.MiddleLeft,
                Padding=new Padding(8,0,0,0), Font=new Font("Segoe UI",8.5f), ForeColor=cLBL, Text="Sẵn sàng." };
            pnlStatus.Controls.Add(lblStatus);
            Controls.Add(pnlStatus);

            // Tabs
            tabs = new TabControl { Dock=DockStyle.Fill, Font=fUI, Padding=new Point(14,5) };
            tabs.TabPages.Add(TabEncrypt());
            tabs.TabPages.Add(TabCompare());
            tabs.TabPages.Add(TabAbout());
            var wrap = new Panel { Dock=DockStyle.Fill, Padding=new Padding(10,6,10,4) };
            wrap.Controls.Add(tabs);
            Controls.Add(wrap);
            Controls.SetChildIndex(wrap, 0);

            ResumeLayout();
            RecalcH();
        }

        // ============================================================
        //  TAB 1: Mã hóa / Giải mã
        // ============================================================
        TabPage TabEncrypt()
        {
            var tab = new TabPage("🔐  Mã hóa / Giải mã") { BackColor=cBG, UseVisualStyleBackColor=false };

            // Outer scroll panel
            var scroll = new Panel { Dock=DockStyle.Fill, AutoScroll=true, Padding=new Padding(12,8,12,8) };
            tab.Controls.Add(scroll);

            // We build a vertical FlowLayoutPanel so everything stacks properly
            var flow = new FlowLayoutPanel {
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Dock          = DockStyle.Top,
                Padding       = new Padding(0)
            };
            scroll.Controls.Add(flow);

            // ── 1. Tham số ──────────────────────────────────────────
            flow.Controls.Add(SecLabel("Tham số hệ thống (p, g, x)"));
            var secParam = SecPanel(flow, 950);

            R(secParam, "Số nguyên tố p:", 10, 10); txtP = TX(secParam, "2357", 130, 10, 110); txtP.TextChanged += (_,__)=>RecalcH();
            R(secParam, "Phần tử sinh g:", 260, 10); txtG = TX(secParam, "2",    380, 10, 90);  txtG.TextChanged += (_,__)=>RecalcH();
            R(secParam, "Khóa bí mật x:",  490, 10); txtX = TX(secParam, "1751", 610, 10, 110); txtX.TextChanged += (_,__)=>RecalcH();

            R(secParam, "h = g^x mod p:", 10, 48, cLBL);
            lblH  = IL(secParam, "—", 140, 48, cACC);
            R(secParam, "Khóa công khai (p,g):", 260, 48, cLBL);
            lblPG = IL(secParam, "—", 420, 48, cACC);
            R(secParam, "Khóa bí mật x:", 570, 48, cLBL);
            lblPX = IL(secParam, "—", 680, 48, Color.Crimson);

            Btn(secParam, "🎲 Tạo khóa ngẫu nhiên", cACC,                    10, 82, 175, _=>GenerateKeys());
            Btn(secParam, "✔ Kiểm tra tham số",      Color.FromArgb(70,85,110), 194, 82, 160, _=>ValidateParams());
            secParam.Height = 120;

            // ── 2. Kiểu dữ liệu đầu vào ─────────────────────────────
            flow.Controls.Add(SecLabel("Kiểu dữ liệu đầu vào"));
            var secInFmt = SecPanel(flow, 950);
            AddRadioRow(secInFmt, new[]{"Văn bản","Số","Hex","Base64"}, 10, 10, v => _inFmt = v, "Văn bản");
            secInFmt.Height = 38;

            // ── 3. Dữ liệu đầu vào ───────────────────────────────────
            flow.Controls.Add(SecLabel("Dữ liệu đầu vào"));
            var secIn = SecPanel(flow, 950);
            txtInput = new TextBox {
                Multiline=true, ScrollBars=ScrollBars.Vertical,
                Location=new Point(10,10), Size=new Size(910,110),
                BorderStyle=BorderStyle.FixedSingle, Font=fMono,
                BackColor=Color.FromArgb(252,252,255)
            };
            secIn.Controls.Add(txtInput);
            int bx=10;
            Btn(secIn,"📋 Dán",   Color.FromArgb(70,85,110), bx,128, 75, _=>PasteIn()); bx+=83;
            Btn(secIn,"📁 Tải tệp",Color.FromArgb(70,85,110),bx,128, 85, _=>LoadIn());  bx+=93;
            Btn(secIn,"🗑 Xóa",   Color.FromArgb(185,28,28), bx,128, 70, _=>txtInput.Clear());
            secIn.Height = 166;

            // ── 4. Định dạng đầu ra ──────────────────────────────────
            flow.Controls.Add(SecLabel("Định dạng đầu ra"));
            var secOutFmt = SecPanel(flow, 950);
            AddRadioRow(secOutFmt, new[]{"Văn bản","Hex","Base64"}, 10, 10, v => _outFmt = v, "Văn bản");
            secOutFmt.Height = 38;

            // ── 5. Thao tác ──────────────────────────────────────────
            flow.Controls.Add(SecLabel("Thao tác"));
            var secAct = SecPanel(flow, 950);
            bx=10;
            Btn(secAct,"🔐 Mã hóa",            cACC,                       bx, 10,115, _=>DoEncrypt()); bx+=123;
            Btn(secAct,"🔓 Giải mã",            Color.FromArgb(109,40,217), bx, 10,115, _=>DoDecrypt()); bx+=123;
            Btn(secAct,"📋 Sao chép kết quả",   Color.FromArgb(70,85,110),  bx, 10,145, _=>CopyResult()); bx+=153;
            Btn(secAct,"💾 Lưu tệp",            Color.FromArgb(21,128,61),  bx, 10,100, _=>SaveResult()); bx+=108;
            Btn(secAct,"🗑 Xóa tất cả",         Color.FromArgb(185,28,28),  bx, 10,110, _=>ClearAll());
            Btn(secAct,"🔬 Chi tiết thuật toán", Color.FromArgb(15,118,110), 10, 48,165, _=>ShowDetail());
            secAct.Height = 86;

            // ── 6. Kết quả ───────────────────────────────────────────
            flow.Controls.Add(SecLabel("Kết quả"));
            var secRes = SecPanel(flow, 950);
            txtResult = new TextBox {
                Multiline=true, ScrollBars=ScrollBars.Vertical, ReadOnly=true,
                Location=new Point(10,10), Size=new Size(910,130),
                BorderStyle=BorderStyle.FixedSingle, Font=fMono,
                BackColor=Color.FromArgb(250,252,255),
                Text="Kết quả sẽ hiển thị ở đây..."
            };
            secRes.Controls.Add(txtResult);
            secRes.Height = 150;

            // Resize inputs when form resizes
            scroll.Resize += (_,__) => {
                int w = scroll.ClientSize.Width - 30;
                flow.Width = w;
                foreach (Panel sec in flow.Controls.OfType<Panel>()) {
                    sec.Width = w;
                    foreach (TextBox tb in sec.Controls.OfType<TextBox>().Where(t=>t.Multiline))
                        tb.Width = w - 20;
                }
            };

            return tab;
        }

        // ============================================================
        //  TAB 2: So sánh
        // ============================================================
        TabPage TabCompare()
        {
            var tab = new TabPage("🔍  So sánh bản mã") { BackColor=cBG };
            var pnl = new Panel { Dock=DockStyle.Fill, AutoScroll=true, Padding=new Padding(14,10,14,10) };
            tab.Controls.Add(pnl);

            int y=8;
            pnl.Controls.Add(new Label {
                Text="Dán bản mã gốc và bản mã cần kiểm tra để phát hiện sửa đổi, thiếu hoặc thêm ký tự.",
                AutoSize=false, Width=900, Height=26, Location=new Point(0,y),
                ForeColor=cGRAY, Font=fUI
            }); y+=32;

            R(pnl, "Bản mã gốc (Original):",         0,   y, cACC, true);
            R(pnl, "Bản mã cần kiểm tra (Suspect):", 455, y, cACC, true); y+=22;

            txtOrig = new TextBox { Multiline=true, ScrollBars=ScrollBars.Both, Location=new Point(0,y),
                Size=new Size(430,170), BorderStyle=BorderStyle.FixedSingle, Font=fMono };
            txtSusp = new TextBox { Multiline=true, ScrollBars=ScrollBars.Both, Location=new Point(455,y),
                Size=new Size(430,170), BorderStyle=BorderStyle.FixedSingle, Font=fMono };
            pnl.Controls.AddRange(new Control[]{txtOrig,txtSusp}); y+=180;

            int bx=0;
            Btn(pnl,"🔍 So sánh ngay",              cACC,                       bx,y,145, _=>CompareTexts()); bx+=153;
            Btn(pnl,"📌 Dùng kết quả mã hóa cuối",  Color.FromArgb(70,85,110),  bx,y,195, _=>UseLastEnc());  bx+=203;
            Btn(pnl,"🗑 Xóa",                       Color.FromArgb(185,28,28),  bx,y,80,  _=>{txtOrig.Clear();txtSusp.Clear();pnlCmpRes.Visible=false;}); y+=48;

            pnlCmpRes = new Panel { Location=new Point(0,y), Size=new Size(890,200), Visible=false };
            lblCmpRes = new Label { Dock=DockStyle.Fill, AutoSize=false, TextAlign=ContentAlignment.TopLeft,
                Padding=new Padding(12), Font=fUI, BackColor=Color.White, BorderStyle=BorderStyle.FixedSingle };
            pnlCmpRes.Controls.Add(lblCmpRes);
            pnl.Controls.Add(pnlCmpRes);
            return tab;
        }

        // ============================================================
        //  TAB 3: Về ElGamal
        // ============================================================
        TabPage TabAbout()
        {
            var tab = new TabPage("📖  Về ElGamal") { BackColor=cBG };
            var rtb = new RichTextBox {
                Dock=DockStyle.Fill, ReadOnly=true, BackColor=cBG,
                BorderStyle=BorderStyle.None, Font=fUI,
                ScrollBars=RichTextBoxScrollBars.Vertical,
                Text=
"THUẬT TOÁN ELGAMAL\r\n" +
"─────────────────────────────────────────────────────────\r\n\r\n" +
"ElGamal là hệ mật mã khoá công khai do Taher Elgamal đề xuất năm 1985.\r\n" +
"Dựa trên bài toán logarithm rời rạc (DLP - Discrete Logarithm Problem).\r\n\r\n" +
"TẠO KHÓA:\r\n" +
"  1. Chọn số nguyên tố lớn p và phần tử sinh g của Z*p\r\n" +
"  2. Chọn khóa bí mật x ngẫu nhiên: 1 < x < p-1\r\n" +
"  3. Tính khóa công khai: h = g^x mod p\r\n" +
"  • Khóa công khai: (p, g, h)     • Khóa bí mật: x\r\n\r\n" +
"MÃ HÓA thông điệp m  (0 ≤ m < p):\r\n" +
"  1. Chọn số ngẫu nhiên k: 1 < k < p-1\r\n" +
"  2. c1 = g^k mod p\r\n" +
"  3. c2 = m · h^k mod p    → Bản mã: cặp (c1, c2)\r\n\r\n" +
"GIẢI MÃ bản mã (c1, c2):\r\n" +
"  1. s     = c1^x   mod p\r\n" +
"  2. s_inv = s^(p-2) mod p   (nghịch đảo Fermat)\r\n" +
"  3. m     = c2 · s_inv mod p\r\n\r\n" +
"ĐỊNH DẠNG BẢN MÃ:\r\n" +
"  c1,c2|c1,c2|c1,c2|...   (mỗi byte/ký tự = 1 cặp)\r\n\r\n" +
"LƯU Ý:\r\n" +
"  • Giá trị mỗi phần tử đầu vào phải < p.\r\n" +
"  • k ngẫu nhiên → cùng bản rõ cho bản mã khác nhau mỗi lần.\r\n" +
"  • Tính an toàn dựa trên độ khó của DLP với p đủ lớn.\r\n"
            };
            tab.Controls.Add(rtb);
            return tab;
        }

        // ============================================================
        //  UI HELPERS
        // ============================================================
        Label SecLabel(string t)
        {
            return new Label {
                Text=t, AutoSize=false, Width=950, Height=22,
                ForeColor=cACC, Font=fBold, Margin=new Padding(0,8,0,0)
            };
        }

        Panel SecPanel(Control parent, int w)
        {
            var p = new Panel {
                Width=w, Height=50, BackColor=Color.White,
                BorderStyle=BorderStyle.FixedSingle,
                Margin=new Padding(0,0,0,2)
            };
            if (parent is FlowLayoutPanel fl) fl.Controls.Add(p);
            return p;
        }

        void R(Control p, string t, int x, int y, Color? c=null, bool bold=false)
        {
            p.Controls.Add(new Label {
                Text=t, Location=new Point(x,y), AutoSize=true,
                ForeColor=c??cLBL, Font=bold?fBold:fUI
            });
        }

        Label IL(Control p, string t, int x, int y, Color c)
        {
            var l = new Label { Text=t, Location=new Point(x,y), AutoSize=true, ForeColor=c, Font=fBold };
            p.Controls.Add(l); return l;
        }

        TextBox TX(Control p, string def, int x, int y, int w)
        {
            var tb = new TextBox { Text=def, Location=new Point(x,y), Width=w, BorderStyle=BorderStyle.FixedSingle };
            p.Controls.Add(tb); return tb;
        }

        void Btn(Control p, string text, Color bg, int x, int y, int w, Action<object> click)
        {
            var b = new Button {
                Text=text, Location=new Point(x,y), Width=w, Height=30,
                BackColor=bg, ForeColor=Color.White,
                FlatStyle=FlatStyle.Flat, Cursor=Cursors.Hand, Font=fUI
            };
            b.FlatAppearance.BorderSize=0;
            b.Click += (s,_)=>click(s!);
            p.Controls.Add(b);
        }

        void AddRadioRow(Panel sec, string[] opts, int startX, int y, Action<string> setter, string def)
        {
            int x=startX;
            foreach (var opt in opts)
            {
                string cap=opt;
                var rb = new RadioButton { Text=opt, Location=new Point(x,y), AutoSize=true, ForeColor=cLBL, Checked=opt==def };
                rb.CheckedChanged += (_,__)=>{ if(rb.Checked) setter(cap); };
                sec.Controls.Add(rb); x+=100;
            }
        }

        // ============================================================
        //  LOGIC
        // ============================================================
        void RecalcH()
        {
            if (txtP==null) return;
            if (!long.TryParse(txtP.Text,out long p)||
                !long.TryParse(txtG.Text,out long g)||
                !long.TryParse(txtX.Text,out long x)) return;
            try {
                _p=p;_g=g;_x=x;
                _h=(long)ElGamal.ModPow(g,x,p);
                if(lblH!=null){ lblH.Text=_h.ToString(); lblPG.Text=$"({p}, {g})"; lblPX.Text=x.ToString(); }
            } catch {}
        }

        void GenerateKeys()
        {
            var (p,g,x,h)=ElGamal.GenerateKeys();
            txtP.Text=p.ToString(); txtG.Text=g.ToString(); txtX.Text=x.ToString();
            _p=p;_g=g;_x=x;_h=h;
            lblH.Text=h.ToString(); lblPG.Text=$"({p}, {g})"; lblPX.Text=x.ToString();
            Stat($"✅ Tạo khóa ngẫu nhiên: p={p}, g={g}, x={x}, h={h}", cOK);
        }

        void ValidateParams()
        {
            var errs=new List<string>();
            if (!long.TryParse(txtP.Text,out long p)||!ElGamal.IsPrime(p)) errs.Add($"p={txtP.Text} không phải số nguyên tố");
            else _p=p;
            if (!long.TryParse(txtG.Text,out long g)||g<2||g>=p) errs.Add("g phải thỏa 2 ≤ g < p");
            else _g=g;
            if (!long.TryParse(txtX.Text,out long x)||x<2||x>=p-1) errs.Add($"x phải thỏa 2 ≤ x ≤ {p-2}");
            else _x=x;
            if (errs.Count>0) Popup("❌ Tham số không hợp lệ:\n• "+string.Join("\n• ",errs),true);
            else { RecalcH(); Popup($"✅ Tham số hợp lệ!\nh = g^x mod p = {_h}",false); }
        }

        void ChkOrThrow()
        {
            var errs=new List<string>();
            if (!long.TryParse(txtP.Text,out long p)||!ElGamal.IsPrime(p)) errs.Add("p không phải số nguyên tố");
            else _p=p;
            if (!long.TryParse(txtG.Text,out long g)||g<2||g>=p) errs.Add("g không hợp lệ");
            else _g=g;
            if (!long.TryParse(txtX.Text,out long x)||x<2||x>=p-1) errs.Add($"x không hợp lệ (cần 2 ≤ x ≤ {p-2})");
            else { _x=x; RecalcH(); }
            if (errs.Count>0) throw new Exception("Tham số không hợp lệ:\n• "+string.Join("\n• ",errs));
        }

        void DoEncrypt()
        {
            try {
                ChkOrThrow();
                string raw=txtInput.Text;
                if (string.IsNullOrWhiteSpace(raw)) throw new Exception("Vui lòng nhập dữ liệu đầu vào.");
                long[] nums=ElGamal.ToNumbers(raw,_inFmt);
                var big=nums.Where(n=>n>=_p).ToArray();
                if (big.Length>0) throw new Exception($"{big.Length} giá trị ≥ p ({_p}): [{string.Join(",",big.Take(5))}]\nHãy tăng p hoặc đổi định dạng.");
                var pairs=ElGamal.Encrypt(nums,_p,_g,_h);
                _lastEnc=ElGamal.PairsToString(pairs);
                txtResult.Text=_lastEnc;
                Stat($"✅ Mã hóa thành công — {nums.Length} phần tử → {pairs.Count} cặp (c1,c2)", cOK);
            } catch(Exception ex){ Stat("❌ "+ex.Message.Split('\n')[0],cERR); Popup("❌ "+ex.Message,true); }
        }

        void DoDecrypt()
        {
            try {
                ChkOrThrow();
                string raw=txtInput.Text.Trim();
                if (string.IsNullOrWhiteSpace(raw)) throw new Exception("Vui lòng dán bản mã cần giải mã.");

                // Basic format validation
                if (!raw.Contains(","))
                    throw new Exception("Bản mã không đúng định dạng.\nĐịnh dạng chuẩn: c1,c2|c1,c2|...\nKiểm tra lại bản mã.");

                List<(long c1,long c2)> pairs;
                try { pairs=ElGamal.ParsePairs(raw); }
                catch(FormatException ex) {
                    throw new Exception($"Lỗi phân tích bản mã: {ex.Message}\n\nĐịnh dạng chuẩn: c1,c2|c1,c2|...\nKiểm tra bản mã có bị thiếu/thêm ký tự không?");
                }

                if (pairs.Count==0) throw new Exception("Không tìm thấy cặp mã hợp lệ.");

                var bad=pairs.Select((pr,i)=>new{i,pr}).Where(e=>e.pr.c1<=0||e.pr.c1>=_p||e.pr.c2<=0||e.pr.c2>=_p).ToList();
                if (bad.Count>0) {
                    var sample=string.Join("\n  ",bad.Take(4).Select(e=>$"Cặp {e.i+1}: c1={e.pr.c1}, c2={e.pr.c2}"));
                    throw new Exception($"⚠️ {bad.Count} cặp ngoài phạm vi (0 < c < p={_p}):\n  {sample}\n\nBản mã có thể đã bị sửa hoặc dùng p khác.");
                }

                long[] dec=ElGamal.Decrypt(pairs,_p,_x);
                txtResult.Text=ElGamal.FromNumbers(dec,_outFmt);
                Stat($"✅ Giải mã thành công — {pairs.Count} cặp → {dec.Length} phần tử", cOK);
            } catch(Exception ex){ Stat("❌ "+ex.Message.Split('\n')[0],cERR); Popup("❌ "+ex.Message,true); }
        }

        void CompareTexts()
        {
            string o=txtOrig.Text.Trim(), s=txtSusp.Text.Trim();
            if (string.IsNullOrWhiteSpace(o)||string.IsNullOrWhiteSpace(s))
            { ShowCmp("⚠️ Vui lòng nhập cả hai bản mã.",cWARN); return; }

            if (o==s) { ShowCmp($"✅ HAI BẢN MÃ GIỐNG NHAU HOÀN TOÀN\r\n\r\nĐộ dài: {o.Length} ký tự\r\nBản mã chưa bị chỉnh sửa. An toàn.",cOK); return; }

            int minLen=Math.Min(o.Length,s.Length), diff=0, firstDiff=-1;
            for(int i=0;i<minLen;i++) if(o[i]!=s[i]){diff++;if(firstDiff<0)firstDiff=i;}
            int lenDiff=Math.Abs(o.Length-s.Length);
            var issues=new List<string>();
            if(lenDiff>0) issues.Add(s.Length<o.Length?$"Bị THIẾU {lenDiff} ký tự (gốc: {o.Length}, nghi vấn: {s.Length})":$"Bị THÊM {lenDiff} ký tự (gốc: {o.Length}, nghi vấn: {s.Length})");
            if(diff>0) issues.Add($"{diff} ký tự bị THAY ĐỔI (vị trí đầu tiên: {firstDiff})");
            var sb=new StringBuilder();
            sb.AppendLine("🚨 CẢNH BÁO: BẢN MÃ ĐÃ BỊ CHỈNH SỬA!\r\n");
            sb.AppendLine("Chi tiết:"); foreach(var iss in issues) sb.AppendLine("  • "+iss);
            sb.AppendLine("\r\nKhuyến nghị: KHÔNG giải mã bản mã này — kết quả sẽ sai hoàn toàn.");
            sb.AppendLine("Gợi ý: Lấy lại bản mã từ nguồn gốc đáng tin cậy.");
            ShowCmp(sb.ToString(),cERR);
        }

        void ShowCmp(string msg,Color c)
        {
            pnlCmpRes.Visible=true; lblCmpRes.Text=msg; lblCmpRes.ForeColor=c;
            Stat(msg.Split('\r')[0],c);
        }

        void UseLastEnc()
        {
            if(string.IsNullOrEmpty(_lastEnc)){MessageBox.Show("Chưa có bản mã. Hãy mã hóa trước.","Thông báo",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
            txtOrig.Text=_lastEnc; tabs.SelectedIndex=1;
        }

        void ShowDetail()
        {
            MessageBox.Show(
                $"Tham số hiện tại:\r\n  p = {_p}\r\n  g = {_g}\r\n  x = {_x}  (bí mật)\r\n  h = {_h}  (= g^x mod p)\r\n\r\n"+
                "Mã hóa: k ngẫu nhiên → c1=g^k mod p, c2=m·h^k mod p\r\n"+
                "Giải mã: s=c1^x mod p, m=c2·s^(p-2) mod p\r\n\r\n"+
                "Định dạng bản mã: c1,c2|c1,c2|...",
                "Chi tiết thuật toán ElGamal",MessageBoxButtons.OK,MessageBoxIcon.Information);
        }

        void ClearAll(){txtInput.Clear();txtResult.Text="Kết quả sẽ hiển thị ở đây...";Stat("Đã xóa.",cLBL);}

        void CopyResult()
        {
            if(!string.IsNullOrEmpty(txtResult.Text)&&!txtResult.Text.StartsWith("Kết quả"))
            {Clipboard.SetText(txtResult.Text);Stat("📋 Đã sao chép!",cOK);}
            else Popup("Chưa có kết quả để sao chép.",false);
        }

        void SaveResult()
        {
            if(string.IsNullOrEmpty(txtResult.Text)||txtResult.Text.StartsWith("Kết quả")){Popup("Chưa có kết quả.",false);return;}
            using var d=new SaveFileDialog{Filter="Text (*.txt)|*.txt|All|*.*",FileName="elgamal_result.txt"};
            if(d.ShowDialog()==DialogResult.OK){File.WriteAllText(d.FileName,txtResult.Text,Encoding.UTF8);Stat($"💾 Đã lưu: {d.FileName}",cOK);}
        }

        void PasteIn(){if(Clipboard.ContainsText()){txtInput.Text=Clipboard.GetText();Stat("📋 Đã dán.",cLBL);}else Stat("⚠️ Clipboard trống.",cWARN);}

        void LoadIn(){
            using var d=new OpenFileDialog{Filter="Text (*.txt)|*.txt|All|*.*"};
            if(d.ShowDialog()==DialogResult.OK){txtInput.Text=File.ReadAllText(d.FileName,Encoding.UTF8);Stat($"📁 Đã tải: {d.FileName}",cLBL);}
        }

        void Stat(string msg,Color c)
        {
            lblStatus.Text=msg; lblStatus.ForeColor=c;
            pnlStatus.BackColor=c==cERR?Color.FromArgb(254,226,226):c==cOK?Color.FromArgb(220,252,231):c==cWARN?Color.FromArgb(254,249,195):Color.FromArgb(218,228,245);
        }

        void Popup(string msg,bool err)=>MessageBox.Show(msg,err?"Lỗi":"Thông báo",MessageBoxButtons.OK,err?MessageBoxIcon.Error:MessageBoxIcon.Information);
    }

    // ============================================================
    //  Entry point
    // ============================================================
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
