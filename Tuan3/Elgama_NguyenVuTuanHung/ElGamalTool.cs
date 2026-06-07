// ============================================================
//  CÔNG CỤ MÃ HÓA VÀ GIẢI MÃ ELGAMAL — C# WinForms
//  UI: 2 panel song song (Mã hóa | Giải mã) cấu trúc giống DES MainUI
//  Logic: giữ nguyên 100% ElGamalTool.cs gốc
//  dotnet run  (net8.0-windows, UseWindowsForms=true)
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;

namespace ElGamalTool
{
    // ============================================================
    //  ElGamal Core  — giữ nguyên 100% từ bản gốc
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
            if (n < 3) n = 3;
            if (n % 2 == 0) n++;
            while (!IsPrime(n)) n += 2;
            return n;
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
                if (pts.Length != 2)
                    throw new FormatException($"Cặp \"{tok}\" sai định dạng c1,c2");
                if (!long.TryParse(pts[0].Trim(), out long c1) ||
                    !long.TryParse(pts[1].Trim(), out long c2))
                    throw new FormatException($"Giá trị không phải số: \"{tok}\"");
                list.Add((c1, c2));
            }
            if (list.Count == 0) throw new FormatException("Không tìm thấy cặp hợp lệ.");
            return list;
        }

        public static long[] ToNumbers(string input, string fmt)
        {
            switch (fmt)
            {
                case "Số":
                    return input.Trim()
                        .Split(new[] { ' ', ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(t => long.TryParse(t, out var n) ? n
                                     : throw new FormatException($"Không phải số: \"{t}\""))
                        .ToArray();
                case "Hex":
                    var h = input.Replace(" ", "").Replace("\n", "").Replace("\r", "");
                    if (h.Length % 2 != 0) throw new FormatException("Hex phải có độ dài chẵn.");
                    return Enumerable.Range(0, h.Length / 2)
                        .Select(i => (long)Convert.ToInt32(h.Substring(i * 2, 2), 16))
                        .ToArray();
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
                case "Số":    return string.Join(" ", nums);
                case "Hex":   return string.Concat(nums.Select(n => n.ToString("x2")));
                case "Base64":return Convert.ToBase64String(nums.Select(n => (byte)(n & 0xFF)).ToArray());
                default:      return Encoding.UTF8.GetString(nums.Select(n => (byte)(n & 0xFF)).ToArray());
            }
        }
    }

    // ============================================================
    //  MainForm — 2 panel song song giống DES MainUI
    // ============================================================
    public class MainForm : Form
    {
        // ── Màu ────────────────────────────────────────────────────
        static readonly Color cBG      = Color.FromArgb(240, 244, 248);
        static readonly Color cPanel   = Color.White;
        static readonly Color cHDR     = Color.FromArgb(25,  82, 190);
        static readonly Color cEnc     = Color.FromArgb(46,  125, 50);
        static readonly Color cDec     = Color.FromArgb(106, 27,  154);
        static readonly Color cClear   = Color.FromArgb(198, 40,  40);
        static readonly Color cOther   = Color.FromArgb(84,  110, 122);
        static readonly Color cKeyGen  = Color.FromArgb(25,  82,  190);
        static readonly Color cDetail  = Color.FromArgb(230, 81,   0);
        static readonly Color cCompare = Color.FromArgb(0,   105,  92);
        static readonly Color cBorder  = Color.FromArgb(207, 216, 220);
        static readonly Color cHdrEnc  = Color.FromArgb(232, 245, 233);
        static readonly Color cHdrDec  = Color.FromArgb(237, 231, 246);
        static readonly Color cResBG   = Color.FromArgb(248, 250, 251);
        static readonly Color cOK      = Color.FromArgb(22,  163,  74);
        static readonly Color cERR     = Color.FromArgb(220,  38,  38);
        static readonly Color cWARN    = Color.FromArgb(170, 110,   0);
        static readonly Color cLBL     = Color.FromArgb(40,   55,  80);
        static readonly Color cGRAY    = Color.FromArgb(95,  108, 130);
        static readonly Color cPrimary = Color.FromArgb(21,  101, 192);

        // ── Font ────────────────────────────────────────────────────
        static readonly Font fUI    = new Font("Segoe UI",  9.5f);
        static readonly Font fBold  = new Font("Segoe UI",  9.5f, FontStyle.Bold);
        static readonly Font fMono  = new Font("Consolas",  9.5f);
        static readonly Font fBtn   = new Font("Segoe UI",  9f,   FontStyle.Bold);
        static readonly Font fTitle = new Font("Segoe UI", 11f,   FontStyle.Bold);
        static readonly Font fHdr   = new Font("Segoe UI", 14f,   FontStyle.Bold);
        static readonly Font fSec   = new Font("Segoe UI",  9.5f, FontStyle.Bold);

        // ── State ────────────────────────────────────────────────────
        long   _p = 2357, _g = 2, _x = 1751, _h;
        string _lastEnc   = "";
        string _encInFmt  = "Văn bản";
        string _decOutFmt = "Văn bản";

        // ── Tham số chung ────────────────────────────────────────────
        TextBox txtP = null!, txtG = null!, txtX = null!;
        Label   lblH = null!, lblPub = null!, lblPriv = null!;

        // ── Panel Mã hóa ─────────────────────────────────────────────
        RadioButton encRbText = null!, encRbNum = null!, encRbHex = null!, encRbBase64 = null!;
        TextBox     encTxtInput  = null!;
        TextBox     encTxtResult = null!;

        // ── Panel Giải mã ─────────────────────────────────────────────
        RadioButton decRbOutText = null!, decRbOutNum = null!, decRbOutHex = null!, decRbOutBase64 = null!;
        TextBox     decTxtCipher = null!;
        TextBox     decTxtResult = null!;

        // ── Tab So sánh ──────────────────────────────────────────────
        TextBox cmpTxtOrig = null!, cmpTxtSusp = null!;
        Label   lblCmpRes  = null!;
        Panel   pnlCmpRes  = null!;

        // ── Status ────────────────────────────────────────────────────
        Label lblStatus = null!;
        Panel pnlStatus = null!;
        TabControl tabs = null!;

        // ============================================================
        //  Constructor
        // ============================================================
        public MainForm()
        {
            SuspendLayout();
            Text          = "CÔNG CỤ MÃ HÓA VÀ GIẢI MÃ ELGAMAL";
            Size          = new Size(1340, 920);
            MinimumSize   = new Size(1100, 780);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = cBG;
            Font          = fUI;
            BuildUI();
            ResumeLayout();
            RecalcH();
        }

        // ============================================================
        //  BuildUI
        // ============================================================
        void BuildUI()
        {
            // Header
            var hdr = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = cHDR };
            hdr.Controls.Add(new Label {
                Text = "CÔNG CỤ MÃ HÓA VÀ GIẢI MÃ ELGAMAL",
                Font = fHdr, ForeColor = Color.White,
                AutoSize = true, Location = new Point(18, 12)
            });
            Controls.Add(hdr);

            // Status bar
            pnlStatus = new Panel { Dock = DockStyle.Bottom, Height = 26,
                BackColor = Color.FromArgb(218, 228, 245) };
            lblStatus  = new Label {
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
                Font = new Font("Segoe UI", 8.5f), ForeColor = cLBL, Text = "Sẵn sàng."
            };
            pnlStatus.Controls.Add(lblStatus);
            Controls.Add(pnlStatus);

            // Panel tham số (dưới header, trên tab)
            var pnlParams = BuildParamsPanel();
            pnlParams.Dock = DockStyle.Top;
            Controls.Add(pnlParams);

            // TabControl
            tabs = new TabControl {
                Dock = DockStyle.Fill, Font = fUI, Padding = new Point(14, 5)
            };
            tabs.TabPages.Add(BuildTabMain());
            tabs.TabPages.Add(BuildTabCompare());
            tabs.TabPages.Add(BuildTabAbout());

            var wrap = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 6, 8, 4) };
            wrap.Controls.Add(tabs);
            Controls.Add(wrap);

            Controls.SetChildIndex(wrap,      0);
            Controls.SetChildIndex(pnlParams, 1);
            Controls.SetChildIndex(hdr,       2);
        }

        // ============================================================
        //  Panel tham số hệ thống
        // ============================================================
        Panel BuildParamsPanel()
        {
            var pnl = new Panel { Height = 112, BackColor = cPanel, Padding = new Padding(12, 6, 12, 6) };
            pnl.Paint += (s, e) => {
                using var pen = new Pen(cBorder);
                e.Graphics.DrawLine(pen, 0, pnl.Height - 1, pnl.Width, pnl.Height - 1);
            };

            // Tiêu đề
            pnl.Controls.Add(new Label {
                Text = "⚙  Tham số hệ thống (p, g, x)",
                Font = fSec, ForeColor = cPrimary, AutoSize = true, Location = new Point(12, 6)
            });

            // Row 1: inputs
            int y = 28, x = 12;
            Lbl(pnl, "Số nguyên tố p:", x, y); x += 118;
            txtP = TB(pnl, "2357", x, y, 100);  x += 110;
            Lbl(pnl, "Phần tử sinh g:", x, y);  x += 118;
            txtG = TB(pnl, "2",    x, y, 80);   x += 90;
            Lbl(pnl, "Khóa bí mật x:", x, y);   x += 110;
            txtX = TB(pnl, "1751", x, y, 100);  x += 114;
            Btn(pnl, "🎲 Tạo khóa ngẫu nhiên", cKeyGen, x, y - 2, 180, _ => DoGenerateKeys()); x += 188;
            Btn(pnl, "✔ Kiểm tra tham số",      cOther,  x, y - 2, 155, _ => DoValidateParams());

            txtP.TextChanged += (_, __) => RecalcH();
            txtG.TextChanged += (_, __) => RecalcH();
            txtX.TextChanged += (_, __) => RecalcH();

            // Row 2: h, pub, priv
            y = 68; x = 12;
            Lbl(pnl, "h = g^x mod p:", x, y);                x += 118;
            lblH    = ILbl(pnl, "—", x, y, cPrimary);        x += 110;
            Lbl(pnl, "Khóa công khai (p, g, h):", x, y);     x += 200;
            lblPub  = ILbl(pnl, "—", x, y, cPrimary);        x += 330;
            Lbl(pnl, "Khóa bí mật x:", x, y);                x += 118;
            lblPriv = ILbl(pnl, "—", x, y, Color.Crimson);

            return pnl;
        }

        // ============================================================
        //  Tab 1 – song song Mã hóa | Giải mã (giống DES MainUI)
        // ============================================================
        TabPage BuildTabMain()
        {
            var tab = new TabPage("🔐  Mã hóa / Giải mã") { BackColor = cBG };

            var tbl = new TableLayoutPanel {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = cBG,
                Padding = new Padding(4)
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

            var encScroll = WrapScroll(BuildEncPanel());
            var decScroll = WrapScroll(BuildDecPanel());

            tbl.Controls.Add(encScroll, 0, 0);
            tbl.Controls.Add(decScroll, 1, 0);
            tab.Controls.Add(tbl);
            return tab;
        }

        // ============================================================
        //  Panel Mã hóa
        // ============================================================
        Panel BuildEncPanel()
        {
            var flow = NewFlow();

            // ── Header ──
            flow.Controls.Add(PanelHeader("🔐  MÃ HÓA", cHdrEnc, cEnc));
            flow.Controls.Add(VGap(6));

            // ── Định dạng đầu vào ──
            flow.Controls.Add(SecLbl("Định dạng dữ liệu đầu vào"));
            var secInFmt = SecPnl(flow);
            encRbText   = RB(secInFmt, "Văn bản", 10,  10, true);
            encRbNum    = RB(secInFmt, "Số",      115, 10, false);
            encRbHex    = RB(secInFmt, "Hex",     195, 10, false);
            encRbBase64 = RB(secInFmt, "Base64",  280, 10, false);
            encRbText.CheckedChanged   += (_, __) => { if (encRbText.Checked)   _encInFmt = "Văn bản"; };
            encRbNum.CheckedChanged    += (_, __) => { if (encRbNum.Checked)    _encInFmt = "Số"; };
            encRbHex.CheckedChanged    += (_, __) => { if (encRbHex.Checked)    _encInFmt = "Hex"; };
            encRbBase64.CheckedChanged += (_, __) => { if (encRbBase64.Checked) _encInFmt = "Base64"; };
            secInFmt.Height = 38;

            // ── Bản rõ ──
            flow.Controls.Add(SecLbl("Dữ liệu đầu vào (bản rõ)"));
            var secIn = SecPnl(flow);
            encTxtInput = Memo(secIn, 10, 10, 110);
            int bx = 10;
            Btn(secIn, "📋 Dán",     cOther, bx, 128, 75,  _ => EncPaste());   bx += 83;
            Btn(secIn, "📁 Tải tệp", cOther, bx, 128, 90,  _ => EncLoad());    bx += 98;
            Btn(secIn, "🗑 Xóa",     cClear, bx, 128, 70,  _ => encTxtInput.Clear());
            secIn.Height = 166;

            // ── Thao tác ──
            flow.Controls.Add(SecLbl("Thao tác"));
            var secAct = SecPnl(flow);
            bx = 10;
            // Hàng 1: hành động chính
            Btn(secAct, "🔐 Mã hóa",           cEnc,    bx, 10, 110, _ => DoEncrypt());      bx += 118;
            Btn(secAct, "🗑 Xóa tất cả",        cClear,  bx, 10, 115, _ => EncClearAll());    bx += 123;
            Btn(secAct, "🎲 Tạo khóa mới",      cKeyGen, bx, 10, 130, _ => DoGenerateKeys()); bx = 10;
            // Hàng 2: sao chép
            Btn(secAct, "📋 Sao chép bản rõ",   cOther,  bx, 48, 145, _ => Copy(encTxtInput,  "bản rõ")); bx += 153;
            Btn(secAct, "📋 Sao chép bản mã",   cOther,  bx, 48, 145, _ => Copy(encTxtResult, "bản mã")); bx = 10;
            // Hàng 3: tải / lưu
            Btn(secAct, "📁 Tải bản rõ",        cOther,  bx, 86, 110, _ => EncLoad());        bx += 118;
            Btn(secAct, "💾 Lưu bản mã",        cEnc,    bx, 86, 110, _ => EncSaveCipher());  bx += 118;
            Btn(secAct, "💾 Lưu tham số",       cOther,  bx, 86, 140, _ => SaveParams());     bx = 10;
            // Hàng 4: chi tiết
            Btn(secAct, "🔬 Chi tiết thuật toán", cDetail,  bx, 124, 168, _ => ShowDetailEnc()); bx += 176;
            Btn(secAct, "📌 Gửi sang So sánh",    cCompare, bx, 124, 155, _ => SendToCompare());
            secAct.Height = 162;

            // ── Kết quả ──
            flow.Controls.Add(SecLbl("Bản mã (kết quả mã hóa)"));
            var secRes = SecPnl(flow);
            encTxtResult = Memo(secRes, 10, 10, 130, true);
            encTxtResult.Text = "Bản mã sẽ hiển thị ở đây...";
            secRes.Height = 150;

            HookResize(flow);
            return flow;
        }

        // ============================================================
        //  Panel Giải mã
        // ============================================================
        Panel BuildDecPanel()
        {
            var flow = NewFlow();

            // ── Header ──
            flow.Controls.Add(PanelHeader("🔓  GIẢI MÃ", cHdrDec, cDec));
            flow.Controls.Add(VGap(6));

            // ── Bản mã input ──
            flow.Controls.Add(SecLbl("Bản mã đầu vào  (định dạng: c1,c2|c1,c2|...)"));
            var secIn = SecPnl(flow);
            decTxtCipher = Memo(secIn, 10, 10, 110);
            int bx = 10;
            Btn(secIn, "📋 Dán",     cOther, bx, 128, 75, _ => DecPaste());   bx += 83;
            Btn(secIn, "📁 Tải tệp", cOther, bx, 128, 90, _ => DecLoad());    bx += 98;
            Btn(secIn, "🗑 Xóa",     cClear, bx, 128, 70, _ => decTxtCipher.Clear());
            secIn.Height = 166;

            // ── Định dạng kết quả ──
            flow.Controls.Add(SecLbl("Định dạng kết quả (bản rõ)"));
            var secOutFmt = SecPnl(flow);
            decRbOutText   = RB(secOutFmt, "Văn bản", 10,  10, true);
            decRbOutNum    = RB(secOutFmt, "Số",      115, 10, false);
            decRbOutHex    = RB(secOutFmt, "Hex",     195, 10, false);
            decRbOutBase64 = RB(secOutFmt, "Base64",  280, 10, false);
            decRbOutText.CheckedChanged   += (_, __) => { if (decRbOutText.Checked)   _decOutFmt = "Văn bản"; };
            decRbOutNum.CheckedChanged    += (_, __) => { if (decRbOutNum.Checked)    _decOutFmt = "Số"; };
            decRbOutHex.CheckedChanged    += (_, __) => { if (decRbOutHex.Checked)    _decOutFmt = "Hex"; };
            decRbOutBase64.CheckedChanged += (_, __) => { if (decRbOutBase64.Checked) _decOutFmt = "Base64"; };
            secOutFmt.Height = 38;

            // ── Thao tác ──
            flow.Controls.Add(SecLbl("Thao tác"));
            var secAct = SecPnl(flow);
            bx = 10;
            // Hàng 1: hành động chính
            Btn(secAct, "🔓 Giải mã",            cDec,    bx, 10, 110, _ => DoDecrypt());      bx += 118;
            Btn(secAct, "🗑 Xóa tất cả",         cClear,  bx, 10, 115, _ => DecClearAll());    bx += 123;
            Btn(secAct, "🎲 Tạo khóa mới",       cKeyGen, bx, 10, 130, _ => DoGenerateKeys()); bx = 10;
            // Hàng 2: sao chép
            Btn(secAct, "📋 Sao chép bản mã",    cOther,  bx, 48, 145, _ => Copy(decTxtCipher, "bản mã")); bx += 153;
            Btn(secAct, "📋 Sao chép bản rõ",    cOther,  bx, 48, 145, _ => Copy(decTxtResult, "bản rõ")); bx = 10;
            // Hàng 3: tải / lưu
            Btn(secAct, "📁 Tải bản mã",         cOther,  bx, 86, 110, _ => DecLoad());        bx += 118;
            Btn(secAct, "💾 Lưu bản rõ",         cDec,    bx, 86, 110, _ => DecSavePlain());   bx += 118;
            Btn(secAct, "📁 Tải tham số",        cOther,  bx, 86, 140, _ => LoadParams());     bx = 10;
            // Hàng 4: chi tiết
            Btn(secAct, "🔬 Chi tiết thuật toán", cDetail, bx, 124, 168, _ => ShowDetailDec());
            secAct.Height = 162;

            // ── Kết quả ──
            flow.Controls.Add(SecLbl("Bản rõ (kết quả giải mã)"));
            var secRes = SecPnl(flow);
            decTxtResult = Memo(secRes, 10, 10, 130, true);
            decTxtResult.Text = "Bản rõ sẽ hiển thị ở đây...";
            secRes.Height = 150;

            HookResize(flow);
            return flow;
        }

        // ============================================================
        //  Tab 2 – So sánh bản mã
        // ============================================================
        TabPage BuildTabCompare()
        {
            var tab = new TabPage("🔍  So sánh bản mã") { BackColor = cBG };
            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true,
                Padding = new Padding(14, 10, 14, 10) };
            tab.Controls.Add(pnl);

            int y = 8;
            pnl.Controls.Add(new Label {
                Text = "Dán bản mã gốc và bản mã cần kiểm tra để phát hiện sửa đổi, thiếu hoặc thêm ký tự.",
                AutoSize = false, Width = 980, Height = 22, Location = new Point(0, y),
                ForeColor = cGRAY, Font = fUI
            }); y += 28;

            Lbl(pnl, "Bản mã gốc (Original):",         0,   y, cPrimary, true);
            Lbl(pnl, "Bản mã cần kiểm tra (Suspect):", 475, y, cPrimary, true); y += 22;

            cmpTxtOrig = new TextBox {
                Multiline = true, ScrollBars = ScrollBars.Both,
                Location = new Point(0, y), Size = new Size(455, 170),
                BorderStyle = BorderStyle.FixedSingle, Font = fMono
            };
            cmpTxtSusp = new TextBox {
                Multiline = true, ScrollBars = ScrollBars.Both,
                Location = new Point(475, y), Size = new Size(455, 170),
                BorderStyle = BorderStyle.FixedSingle, Font = fMono
            };
            pnl.Controls.AddRange(new Control[] { cmpTxtOrig, cmpTxtSusp }); y += 180;

            int bx = 0;
            Btn(pnl, "🔍 So sánh ngay",             cCompare, bx, y, 155, _ => DoCompare());   bx += 163;
            Btn(pnl, "📌 Dùng bản mã vừa mã hóa",   cOther,   bx, y, 200, _ => UseLastEnc());  bx += 208;
            Btn(pnl, "🗑 Xóa",                       cClear,   bx, y, 80,
                _ => { cmpTxtOrig.Clear(); cmpTxtSusp.Clear(); pnlCmpRes.Visible = false; });
            y += 46;

            pnlCmpRes = new Panel { Location = new Point(0, y), Size = new Size(940, 220), Visible = false };
            lblCmpRes = new Label {
                Dock = DockStyle.Fill, AutoSize = false, TextAlign = ContentAlignment.TopLeft,
                Padding = new Padding(12), Font = fUI, BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            pnlCmpRes.Controls.Add(lblCmpRes);
            pnl.Controls.Add(pnlCmpRes);
            return tab;
        }

        // ============================================================
        //  Tab 3 – Về ElGamal
        // ============================================================
        TabPage BuildTabAbout()
        {
            var tab = new TabPage("📖  Về ElGamal") { BackColor = cBG };
            var rtb = new RichTextBox {
                Dock = DockStyle.Fill, ReadOnly = true, BackColor = cBG,
                BorderStyle = BorderStyle.None, Font = fUI,
                ScrollBars = RichTextBoxScrollBars.Vertical,
                Text =
"THUẬT TOÁN ELGAMAL\r\n" +
"───────────────────────────────────────────────────────────────────\r\n\r\n" +
"ElGamal là hệ mật mã khóa công khai do Taher Elgamal đề xuất năm 1985.\r\n" +
"Dựa trên bài toán logarithm rời rạc (DLP - Discrete Logarithm Problem).\r\n\r\n" +
"TẠO KHÓA:\r\n" +
"  1. Chọn số nguyên tố lớn p và phần tử sinh g của Z*p\r\n" +
"  2. Chọn khóa bí mật x ngẫu nhiên: 1 < x < p-1\r\n" +
"  3. Tính khóa công khai: h = g^x mod p\r\n" +
"  • Khóa công khai: (p, g, h)     • Khóa bí mật: x\r\n\r\n" +
"MÃ HÓA thông điệp m  (0 ≤ m < p):\r\n" +
"  1. Chọn số ngẫu nhiên k: 1 < k < p-1  (mới mỗi lần)\r\n" +
"  2. c1 = g^k mod p\r\n" +
"  3. c2 = m · h^k mod p    → Bản mã: cặp (c1, c2)\r\n\r\n" +
"GIẢI MÃ bản mã (c1, c2):\r\n" +
"  1. s     = c1^x   mod p\r\n" +
"  2. s_inv = s^(p-2) mod p   (nghịch đảo Fermat nhỏ)\r\n" +
"  3. m     = c2 · s_inv mod p\r\n\r\n" +
"ĐỊNH DẠNG BẢN MÃ:\r\n" +
"  c1,c2|c1,c2|c1,c2|...   (mỗi byte/ký tự = 1 cặp)\r\n\r\n" +
"TÍNH CHẤT:\r\n" +
"  • Probabilistic: k ngẫu nhiên → cùng bản rõ cho bản mã khác nhau mỗi lần.\r\n" +
"  • IND-CPA secure: không phân biệt được hai bản mã từ cùng bản rõ.\r\n" +
"  • Mỗi giá trị đầu vào phải < p.\r\n" +
"  • Tính an toàn phụ thuộc vào kích thước p (thực tế cần p ≥ 2048-bit).\r\n\r\n" +
"LƯU Ý BẢO MẬT:\r\n" +
"  • Không bao giờ tái sử dụng k cho hai thông điệp khác nhau (lộ x).\r\n" +
"  • Không có xác thực — cần kết hợp chữ ký số để đảm bảo toàn vẹn.\r\n"
            };
            tab.Controls.Add(rtb);
            return tab;
        }

        // ============================================================
        //  Logic – Khóa
        // ============================================================
        void RecalcH()
        {
            if (txtP == null) return;
            if (!long.TryParse(txtP.Text, out long p) ||
                !long.TryParse(txtG.Text, out long g) ||
                !long.TryParse(txtX.Text, out long x)) return;
            try {
                _p = p; _g = g; _x = x;
                _h = (long)ElGamal.ModPow(g, x, p);
                lblH.Text    = _h.ToString();
                lblPub.Text  = $"({p}, {g}, {_h})";
                lblPriv.Text = x.ToString();
            } catch { }
        }

        void DoGenerateKeys()
        {
            var (p, g, x, h) = ElGamal.GenerateKeys();
            txtP.Text = p.ToString(); txtG.Text = g.ToString(); txtX.Text = x.ToString();
            _p = p; _g = g; _x = x; _h = h;
            lblH.Text = h.ToString(); lblPub.Text = $"({p}, {g}, {h})"; lblPriv.Text = x.ToString();
            Stat($"✅ Tạo khóa ngẫu nhiên: p={p}  g={g}  x={x}  h={h}", cOK);
        }

        void DoValidateParams()
        {
            var errs = new List<string>();
            if (!long.TryParse(txtP.Text, out long p) || !ElGamal.IsPrime(p))
                errs.Add($"p={txtP.Text} không phải số nguyên tố");
            else _p = p;
            if (!long.TryParse(txtG.Text, out long g) || g < 2 || g >= _p)
                errs.Add("g phải thỏa 2 ≤ g < p");
            else _g = g;
            if (!long.TryParse(txtX.Text, out long x) || x < 2 || x > _p - 2)
                errs.Add($"x phải thỏa 2 ≤ x ≤ {_p - 2}");
            else _x = x;

            if (errs.Count > 0)
                Popup("❌ Tham số không hợp lệ:\n• " + string.Join("\n• ", errs), true);
            else { RecalcH(); Popup($"✅ Tham số hợp lệ!\nh = g^x mod p = {_h}", false); }
        }

        void ChkOrThrow()
        {
            var errs = new List<string>();
            if (!long.TryParse(txtP.Text, out long p) || !ElGamal.IsPrime(p))
                errs.Add("p không phải số nguyên tố");
            else _p = p;
            if (!long.TryParse(txtG.Text, out long g) || g < 2 || g >= _p)
                errs.Add("g không hợp lệ");
            else _g = g;
            if (!long.TryParse(txtX.Text, out long x) || x < 2 || x > _p - 2)
                errs.Add($"x không hợp lệ (cần 2 ≤ x ≤ {_p - 2})");
            else { _x = x; RecalcH(); }
            if (errs.Count > 0)
                throw new Exception("Tham số không hợp lệ:\n• " + string.Join("\n• ", errs));
        }

        // ============================================================
        //  Logic – Mã hóa
        // ============================================================
        void DoEncrypt()
        {
            try {
                ChkOrThrow();
                string raw = encTxtInput.Text;
                if (string.IsNullOrWhiteSpace(raw))
                    throw new Exception("Vui lòng nhập dữ liệu đầu vào.");

                long[] nums = ElGamal.ToNumbers(raw, _encInFmt);
                var big = nums.Where(n => n >= _p).ToArray();
                if (big.Length > 0)
                    throw new Exception(
                        $"{big.Length} giá trị ≥ p ({_p}): [{string.Join(",", big.Take(5))}]\n" +
                        "Hãy tăng p hoặc đổi định dạng đầu vào.");

                var pairs = ElGamal.Encrypt(nums, _p, _g, _h);
                _lastEnc = ElGamal.PairsToString(pairs);
                encTxtResult.Text = _lastEnc;
                Stat($"✅ Mã hóa thành công — {nums.Length} phần tử → {pairs.Count} cặp (c1,c2)", cOK);
            }
            catch (Exception ex) {
                Stat("❌ " + ex.Message.Split('\n')[0], cERR);
                Popup("❌ " + ex.Message, true);
            }
        }

        void EncClearAll()
        {
            encTxtInput.Clear();
            encTxtResult.Text = "Bản mã sẽ hiển thị ở đây...";
            encRbText.Checked = true;
            _lastEnc = "";
            Stat("Đã xóa panel mã hóa.", cLBL);
        }

        void EncPaste()
        {
            if (Clipboard.ContainsText()) { encTxtInput.Text = Clipboard.GetText(); Stat("📋 Đã dán.", cLBL); }
            else Stat("⚠️ Clipboard trống.", cWARN);
        }

        void EncLoad()
        {
            var s = LoadFile("Tải bản rõ");
            if (s != null) { encTxtInput.Text = s; Stat("📁 Đã tải bản rõ.", cLBL); }
        }

        void EncSaveCipher()
        {
            if (string.IsNullOrEmpty(encTxtResult.Text) || encTxtResult.Text.StartsWith("Bản mã"))
            { Popup("Chưa có bản mã để lưu.", false); return; }
            SaveFile(encTxtResult.Text, "Lưu bản mã", "ban_ma_elgamal.txt");
        }

        void SaveParams()
        {
            SaveFile($"p={_p}\ng={_g}\nx={_x}\nh={_h}", "Lưu tham số khóa", "tham_so_elgamal.txt");
        }

        // ============================================================
        //  Logic – Giải mã
        // ============================================================
        void DoDecrypt()
        {
            try {
                ChkOrThrow();
                string raw = decTxtCipher.Text.Trim();
                if (string.IsNullOrWhiteSpace(raw))
                    throw new Exception("Vui lòng dán bản mã cần giải mã.");
                if (!raw.Contains(","))
                    throw new Exception(
                        "Bản mã không đúng định dạng.\n" +
                        "Định dạng chuẩn: c1,c2|c1,c2|...\nKiểm tra lại bản mã.");

                List<(long c1, long c2)> pairs;
                try { pairs = ElGamal.ParsePairs(raw); }
                catch (FormatException ex) {
                    throw new Exception(
                        $"Lỗi phân tích bản mã: {ex.Message}\n\n" +
                        "Định dạng chuẩn: c1,c2|c1,c2|...\n" +
                        "Kiểm tra bản mã có bị thiếu/thêm ký tự không?");
                }

                var bad = pairs.Select((pr, i) => new { i, pr })
                               .Where(e => e.pr.c1 <= 0 || e.pr.c1 >= _p ||
                                           e.pr.c2 <= 0 || e.pr.c2 >= _p).ToList();
                if (bad.Count > 0) {
                    var sample = string.Join("\n  ",
                        bad.Take(4).Select(e => $"Cặp {e.i + 1}: c1={e.pr.c1}, c2={e.pr.c2}"));
                    throw new Exception(
                        $"⚠️ {bad.Count} cặp ngoài phạm vi (0 < c < p={_p}):\n  {sample}\n\n" +
                        "Bản mã có thể đã bị sửa hoặc dùng p khác.");
                }

                long[] dec = ElGamal.Decrypt(pairs, _p, _x);
                decTxtResult.Text = ElGamal.FromNumbers(dec, _decOutFmt);
                Stat($"✅ Giải mã thành công — {pairs.Count} cặp → {dec.Length} phần tử", cOK);
            }
            catch (Exception ex) {
                Stat("❌ " + ex.Message.Split('\n')[0], cERR);
                Popup("❌ " + ex.Message, true);
            }
        }

        void DecClearAll()
        {
            decTxtCipher.Clear();
            decTxtResult.Text = "Bản rõ sẽ hiển thị ở đây...";
            decRbOutText.Checked = true;
            Stat("Đã xóa panel giải mã.", cLBL);
        }

        void DecPaste()
        {
            if (Clipboard.ContainsText()) { decTxtCipher.Text = Clipboard.GetText(); Stat("📋 Đã dán.", cLBL); }
            else Stat("⚠️ Clipboard trống.", cWARN);
        }

        void DecLoad()
        {
            var s = LoadFile("Tải bản mã");
            if (s != null) { decTxtCipher.Text = s.Trim(); Stat("📁 Đã tải bản mã.", cLBL); }
        }

        void DecSavePlain()
        {
            if (string.IsNullOrEmpty(decTxtResult.Text) || decTxtResult.Text.StartsWith("Bản rõ"))
            { Popup("Chưa có bản rõ để lưu.", false); return; }
            SaveFile(decTxtResult.Text, "Lưu bản rõ", "ban_ro_elgamal.txt");
        }

        void LoadParams()
        {
            var s = LoadFile("Tải tham số khóa");
            if (s == null) return;
            foreach (var line in s.Split('\n'))
            {
                var kv = line.Split('=');
                if (kv.Length < 2) continue;
                switch (kv[0].Trim()) {
                    case "p": txtP.Text = kv[1].Trim(); break;
                    case "g": txtG.Text = kv[1].Trim(); break;
                    case "x": txtX.Text = kv[1].Trim(); break;
                }
            }
            RecalcH();
            Stat("📁 Đã tải tham số khóa.", cLBL);
        }

        // ============================================================
        //  Logic – Sao chép
        // ============================================================
        void Copy(TextBox tb, string label)
        {
            if (string.IsNullOrWhiteSpace(tb.Text) ||
                tb.Text.StartsWith("Bản mã") || tb.Text.StartsWith("Bản rõ"))
            { Popup("Không có dữ liệu để sao chép.", false); return; }
            Clipboard.SetText(tb.Text);
            Stat($"📋 Đã sao chép {label}.", cOK);
        }

        // ============================================================
        //  Logic – Chi tiết thuật toán
        // ============================================================
        void ShowDetailEnc()
        {
            try {
                ChkOrThrow();
                string raw = encTxtInput.Text;
                if (string.IsNullOrWhiteSpace(raw)) throw new Exception("Chưa nhập dữ liệu đầu vào.");
                long[] nums  = ElGamal.ToNumbers(raw, _encInFmt);
                var    pairs = ElGamal.Encrypt(nums, _p, _g, _h);
                ShowDetailDialog(BuildDetailText(nums, pairs, true));
            }
            catch (Exception ex) { Popup("❌ " + ex.Message, true); }
        }

        void ShowDetailDec()
        {
            try {
                ChkOrThrow();
                string raw = decTxtCipher.Text.Trim();
                if (string.IsNullOrWhiteSpace(raw)) throw new Exception("Chưa nhập bản mã.");
                var    pairs = ElGamal.ParsePairs(raw);
                long[] dec   = ElGamal.Decrypt(pairs, _p, _x);
                ShowDetailDialog(BuildDetailText(dec, pairs, false));
            }
            catch (Exception ex) { Popup("❌ " + ex.Message, true); }
        }

        string BuildDetailText(long[] nums, List<(long c1, long c2)> pairs, bool isEnc)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("    CHI TIẾT THUẬT TOÁN ELGAMAL");
            sb.AppendLine("═══════════════════════════════════════════════════════════════\n");
            sb.AppendLine($"  p (số nguyên tố)  = {_p}");
            sb.AppendLine($"  g (phần tử sinh)  = {_g}");
            sb.AppendLine($"  x (khóa bí mật)  = {_x}");
            sb.AppendLine($"  h = g^x mod p    = {_h}\n");
            sb.AppendLine($"  Khóa công khai : (p={_p}, g={_g}, h={_h})");
            sb.AppendLine($"  Khóa bí mật    : x={_x}\n");

            if (isEnc) {
                sb.AppendLine("─── MÃ HÓA từng phần tử ────────────────────────────────────────");
                sb.AppendLine($"  {"m",-10}  {"c1",-14}  {"c2",-14}  Ghi chú");
                sb.AppendLine("  " + new string('─', 60));
                for (int i = 0; i < Math.Min(nums.Length, 40); i++) {
                    string note = (nums[i] >= 32 && nums[i] <= 126)
                        ? $"ký tự '{(char)nums[i]}'"
                        : $"0x{nums[i]:x2}";
                    sb.AppendLine($"  {nums[i],-10}  {pairs[i].c1,-14}  {pairs[i].c2,-14}  {note}");
                }
                if (nums.Length > 40) sb.AppendLine($"  ... ({nums.Length - 40} phần tử còn lại)");
            } else {
                sb.AppendLine("─── GIẢI MÃ từng cặp ───────────────────────────────────────────");
                sb.AppendLine($"  {"STT",-5}  {"c1",-14}  {"c2",-14}  {"m",-10}  Ký tự");
                sb.AppendLine("  " + new string('─', 60));
                for (int i = 0; i < Math.Min(pairs.Count, 40); i++) {
                    long m = nums[i];
                    string ch = (m >= 32 && m <= 126) ? $"'{(char)m}'" : $"0x{m:x2}";
                    sb.AppendLine($"  {i+1,-5}  {pairs[i].c1,-14}  {pairs[i].c2,-14}  {m,-10}  {ch}");
                }
                if (pairs.Count > 40) sb.AppendLine($"  ... ({pairs.Count - 40} cặp còn lại)");
            }

            sb.AppendLine("\n─── CÔNG THỨC ──────────────────────────────────────────────────");
            sb.AppendLine("  Mã hóa : c1 = g^k mod p   |   c2 = m · h^k mod p");
            sb.AppendLine("  Giải mã: s  = c1^x mod p  |   s_inv = s^(p-2) mod p  |   m = c2·s_inv mod p");
            sb.AppendLine("  (Nghịch đảo Fermat: a^(p-1) ≡ 1 mod p  ⇒  a^(-1) ≡ a^(p-2) mod p)");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            return sb.ToString();
        }

        void ShowDetailDialog(string content)
        {
            var dlg = new Form {
                Text = "Chi tiết thuật toán ElGamal", Size = new Size(720, 580),
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(250, 250, 252)
            };
            var ta = new TextBox {
                Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill, Font = fMono, Text = content,
                BackColor = Color.FromArgb(250, 250, 252), BorderStyle = BorderStyle.None
            };
            var btmPnl = new Panel { Dock = DockStyle.Bottom, Height = 44, BackColor = cBG };
            var btnClose = new Button {
                Text = "Đóng", Width = 90, Height = 30,
                BackColor = cOther, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (_, __) => dlg.Close();
            btmPnl.Controls.Add(btnClose);
            btmPnl.Resize += (_, __) => btnClose.Location = new Point(btmPnl.Width - 100, 7);
            dlg.Controls.Add(ta);
            dlg.Controls.Add(btmPnl);
            dlg.ShowDialog(this);
        }

        // ============================================================
        //  Logic – So sánh bản mã
        // ============================================================
        void DoCompare()
        {
            string o = cmpTxtOrig.Text.Trim(), s = cmpTxtSusp.Text.Trim();
            if (string.IsNullOrWhiteSpace(o) || string.IsNullOrWhiteSpace(s))
            { ShowCmp("⚠️ Vui lòng nhập cả hai bản mã.", cWARN); return; }

            if (o == s) {
                ShowCmp($"✅ HAI BẢN MÃ GIỐNG NHAU HOÀN TOÀN\r\n\r\n" +
                        $"Độ dài: {o.Length} ký tự\r\nBản mã chưa bị chỉnh sửa. An toàn.", cOK);
                return;
            }

            int minLen = Math.Min(o.Length, s.Length), diff = 0, firstDiff = -1;
            for (int i = 0; i < minLen; i++)
                if (o[i] != s[i]) { diff++; if (firstDiff < 0) firstDiff = i; }
            int lenDiff = Math.Abs(o.Length - s.Length);
            var issues  = new List<string>();
            if (lenDiff > 0)
                issues.Add(s.Length < o.Length
                    ? $"Bị THIẾU {lenDiff} ký tự (gốc: {o.Length}, nghi vấn: {s.Length})"
                    : $"Bị THÊM {lenDiff} ký tự (gốc: {o.Length}, nghi vấn: {s.Length})");
            if (diff > 0)
                issues.Add($"{diff} ký tự bị THAY ĐỔI (vị trí đầu tiên: {firstDiff})");

            var sb = new StringBuilder();
            sb.AppendLine("🚨 CẢNH BÁO: BẢN MÃ ĐÃ BỊ CHỈNH SỬA!\r\n");
            sb.AppendLine("Chi tiết:");
            foreach (var iss in issues) sb.AppendLine("  • " + iss);
            sb.AppendLine("\r\nKhuyến nghị: KHÔNG giải mã bản mã này — kết quả sẽ sai hoàn toàn.");
            sb.AppendLine("Gợi ý: Lấy lại bản mã từ nguồn gốc đáng tin cậy.");
            ShowCmp(sb.ToString(), cERR);
        }

        void ShowCmp(string msg, Color c)
        {
            pnlCmpRes.Visible = true; lblCmpRes.Text = msg; lblCmpRes.ForeColor = c;
            Stat(msg.Split('\r')[0], c);
        }

        void UseLastEnc()
        {
            if (string.IsNullOrEmpty(_lastEnc))
            { MessageBox.Show("Chưa có bản mã. Hãy mã hóa trước.", "Thông báo",
                              MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            cmpTxtOrig.Text = _lastEnc;
            tabs.SelectedIndex = 1;
            Stat("📌 Đã điền bản mã vừa mã hóa vào ô gốc.", cOK);
        }

        void SendToCompare()
        {
            if (string.IsNullOrEmpty(encTxtResult.Text) || encTxtResult.Text.StartsWith("Bản mã"))
            { Popup("Chưa có bản mã để gửi sang So sánh.", false); return; }
            cmpTxtOrig.Text = encTxtResult.Text;
            tabs.SelectedIndex = 1;
            Stat("📌 Đã gửi bản mã sang tab So sánh.", cOK);
        }

        // ============================================================
        //  Tiện ích File
        // ============================================================
        string? LoadFile(string title)
        {
            using var d = new OpenFileDialog {
                Title = title, Filter = "Text (*.txt)|*.txt|All|*.*"
            };
            if (d.ShowDialog() != DialogResult.OK) return null;
            try { return File.ReadAllText(d.FileName, Encoding.UTF8); }
            catch (Exception ex) { Popup("Lỗi đọc tệp: " + ex.Message, true); return null; }
        }

        void SaveFile(string content, string title, string defName)
        {
            if (string.IsNullOrWhiteSpace(content)) { Popup("Không có dữ liệu để lưu.", false); return; }
            using var d = new SaveFileDialog {
                Title = title, Filter = "Text (*.txt)|*.txt|All|*.*", FileName = defName
            };
            if (d.ShowDialog() != DialogResult.OK) return;
            try { File.WriteAllText(d.FileName, content, Encoding.UTF8); Stat($"💾 Đã lưu: {d.FileName}", cOK); }
            catch (Exception ex) { Popup("Lỗi lưu tệp: " + ex.Message, true); }
        }

        // ============================================================
        //  Tiện ích Status / Popup
        // ============================================================
        void Stat(string msg, Color c)
        {
            lblStatus.Text      = msg;
            lblStatus.ForeColor = c;
            pnlStatus.BackColor = c == cERR  ? Color.FromArgb(254, 226, 226)
                                : c == cOK   ? Color.FromArgb(220, 252, 231)
                                : c == cWARN ? Color.FromArgb(254, 249, 195)
                                             : Color.FromArgb(218, 228, 245);
        }

        void Popup(string msg, bool err) =>
            MessageBox.Show(msg, err ? "Lỗi" : "Thông báo",
                            MessageBoxButtons.OK,
                            err ? MessageBoxIcon.Error : MessageBoxIcon.Information);

        // ============================================================
        //  UI Builder Helpers
        // ============================================================
        FlowLayoutPanel NewFlow() => new FlowLayoutPanel {
            FlowDirection = FlowDirection.TopDown, WrapContents = false,
            AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top, BackColor = cBG, Padding = new Padding(0)
        };

        Panel WrapScroll(Control inner)
        {
            var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true,
                BackColor = cBG, Padding = new Padding(4) };
            p.Controls.Add(inner);
            return p;
        }

        void HookResize(FlowLayoutPanel flow)
        {
            flow.Resize += (_, __) => {
                int w = flow.Width - 24;
                if (w < 100) return;
                foreach (Panel sec in flow.Controls.OfType<Panel>()) {
                    sec.Width = w;
                    foreach (TextBox tb in sec.Controls.OfType<TextBox>().Where(t => t.Multiline))
                        tb.Width = sec.Width - 20;
                }
            };
        }

        Panel PanelHeader(string title, Color bg, Color fg)
        {
            var p = new Panel {
                Height = 44, BackColor = bg,
                BorderStyle = BorderStyle.FixedSingle,
                Margin = new Padding(0, 0, 0, 4), Width = 600
            };
            p.Controls.Add(new Label {
                Text = title,
                Font = fTitle, ForeColor = fg, AutoSize = true, Location = new Point(10, 11)
            });
            return p;
        }

        Label SecLbl(string t) => new Label {
            Text = t, AutoSize = false, Width = 900, Height = 22,
            ForeColor = cPrimary, Font = fSec, Margin = new Padding(0, 6, 0, 0)
        };

        Panel SecPnl(FlowLayoutPanel parent)
        {
            var p = new Panel {
                Width = 900, Height = 50, BackColor = cPanel,
                BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 0, 0, 2)
            };
            parent.Controls.Add(p);
            return p;
        }

        TextBox Memo(Panel parent, int x, int y, int h, bool ro = false)
        {
            var tb = new TextBox {
                Multiline = true, ScrollBars = ScrollBars.Vertical,
                Location = new Point(x, y), Size = new Size(parent.Width - 20, h),
                BorderStyle = BorderStyle.FixedSingle, Font = fMono,
                ReadOnly = ro,
                BackColor = ro ? cResBG : Color.FromArgb(252, 252, 255)
            };
            parent.Controls.Add(tb);
            return tb;
        }

        void Lbl(Control p, string t, int x, int y, Color? c = null, bool bold = false) =>
            p.Controls.Add(new Label {
                Text = t, Location = new Point(x, y), AutoSize = true,
                ForeColor = c ?? cLBL, Font = bold ? fBold : fUI
            });

        Label ILbl(Control p, string t, int x, int y, Color c)
        {
            var l = new Label { Text = t, Location = new Point(x, y), AutoSize = true,
                ForeColor = c, Font = fBold };
            p.Controls.Add(l); return l;
        }

        TextBox TB(Control p, string def, int x, int y, int w)
        {
            var tb = new TextBox { Text = def, Location = new Point(x, y), Width = w,
                BorderStyle = BorderStyle.FixedSingle, Font = fMono };
            p.Controls.Add(tb); return tb;
        }

        void Btn(Control p, string text, Color bg, int x, int y, int w, Action<object> click)
        {
            var b = new Button {
                Text = text, Location = new Point(x, y), Width = w, Height = 30,
                BackColor = bg, ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Font = fBtn
            };
            b.FlatAppearance.BorderSize = 0;
            b.Click += (s, _) => click(s!);
            p.Controls.Add(b);
        }

        RadioButton RB(Control p, string text, int x, int y, bool chk)
        {
            var rb = new RadioButton {
                Text = text, Location = new Point(x, y), AutoSize = true,
                Checked = chk, ForeColor = cLBL, Font = fUI
            };
            p.Controls.Add(rb); return rb;
        }

        Control VGap(int h) => new Panel {
            Height = h, Width = 1, BackColor = Color.Transparent
        };
    }

    // ============================================================
    //  Entry Point
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
