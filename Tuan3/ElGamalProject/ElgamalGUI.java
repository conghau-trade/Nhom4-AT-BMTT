import javax.swing.*;
import javax.swing.border.EmptyBorder;
import javax.swing.filechooser.FileNameExtensionFilter;
import java.awt.*;
import java.awt.datatransfer.StringSelection;
import java.io.File;
import java.math.BigInteger;
import java.util.List;

public class ElgamalGUI extends JFrame {

    private JComboBox<String> cbBits;

    private JTextArea txtPublicKey;
    private JTextArea txtPrivateKey;

    private JTextArea txtPlainText;
    private JTextArea txtCipherText;
    private JTextArea txtCipherInput;
    private JTextArea txtResult;

    private JButton btnGenerate;
    private JButton btnEncrypt;
    private JButton btnDecrypt;

    private JButton btnSavePublic;
    private JButton btnSavePrivate;

    private JButton btnLoadPublic;
    private JButton btnLoadPrivate;

    private JButton btnSaveCipher;
    private JButton btnLoadCipher;

    private JButton btnSavePlainText;
    private JButton btnLoadPlainText;
    private JButton btnSaveResult;

    private JButton btnClearPublicKey;
    private JButton btnClearPrivateKey;
    private JButton btnClearPlainText;
    private JButton btnClearCipherText;
    private JButton btnClearCipherInput;
    private JButton btnClearResult;
    private JButton btnClearAll;

    private JButton btnCopyPlainText;
    private JButton btnCopyCipherText;
    private JButton btnCopyCipherInput;
    private JButton btnCopyResult;

    private BigInteger p;
    private BigInteger g;
    private BigInteger x;
    private BigInteger y;

    public ElgamalGUI() {

        initNimbus();

        setTitle("ElGamal Encryption Tool");
        setSize(1000, 750);
        setLocationRelativeTo(null);
        setDefaultCloseOperation(EXIT_ON_CLOSE);

        buildUI();

        registerEvents();
    }

    private void initNimbus() {

        try {

            for (UIManager.LookAndFeelInfo info : UIManager.getInstalledLookAndFeels()) {

                if ("Nimbus".equals(info.getName())) {

                    UIManager.setLookAndFeel(
                            info.getClassName());

                    break;
                }
            }

        } catch (Exception ignored) {
        }
    }

    private void buildUI() {

        JPanel root = new JPanel(new BorderLayout());

        root.setBorder(
                new EmptyBorder(
                        10,
                        10,
                        10,
                        10));

        setContentPane(root);

        JLabel title = new JLabel(
                "ELGAMAL ENCRYPTION TOOL",
                SwingConstants.CENTER);

        title.setFont(
                new Font(
                        "Segoe UI",
                        Font.BOLD,
                        28));

        root.add(title, BorderLayout.NORTH);

        JPanel main = new JPanel(new BorderLayout(10, 10));
        main.add(createKeyPanel(), BorderLayout.NORTH);

        JSplitPane splitPane = new JSplitPane(
                JSplitPane.HORIZONTAL_SPLIT,
                createEncryptPanel(),
                createDecryptPanel());
        splitPane.setResizeWeight(0.5);
        splitPane.setContinuousLayout(true);
        splitPane.setBorder(null);

        main.add(splitPane, BorderLayout.CENTER);

        root.add(main, BorderLayout.CENTER);
    }

    private JPanel createKeyPanel() {

        JPanel panel = new JPanel(new BorderLayout(10, 10));

        JPanel top = new JPanel();

        cbBits = new JComboBox<>(
                new String[] {
                        "512",
                        "1024",
                        "2048"
                });

        btnGenerate = new JButton("Sinh khóa");

        top.add(
                new JLabel("Kích thước khóa"));

        top.add(cbBits);

        top.add(btnGenerate);

        panel.add(
                top,
                BorderLayout.NORTH);

        JPanel center = new JPanel(
                new GridLayout(
                        1,
                        2,
                        10,
                        10));

        txtPublicKey = new JTextArea();

        txtPrivateKey = new JTextArea();

        txtPublicKey.setLineWrap(true);
        txtPrivateKey.setLineWrap(true);

        center.add(
                new JScrollPane(
                        txtPublicKey));

        center.add(
                new JScrollPane(
                        txtPrivateKey));

        panel.add(
                center,
                BorderLayout.CENTER);

        JPanel bottom = new JPanel();

        btnSavePublic = new JButton("Lưu Public Key");

        btnSavePrivate = new JButton("Lưu Private Key");

        btnLoadPublic = new JButton("Đọc Public Key");

        btnLoadPrivate = new JButton("Đọc Private Key");

        btnClearPublicKey = new JButton("Xóa Public Key");
        btnClearPrivateKey = new JButton("Xóa Private Key");
        btnClearAll = new JButton("Xóa Tất Cả");

        bottom.add(btnSavePublic);
        bottom.add(btnSavePrivate);
        bottom.add(btnLoadPublic);
        bottom.add(btnLoadPrivate);
        bottom.add(btnClearPublicKey);
        bottom.add(btnClearPrivateKey);
        bottom.add(btnClearAll);

        panel.add(
                bottom,
                BorderLayout.SOUTH);

        return panel;
    }

    private JPanel createEncryptPanel() {

        JPanel panel = new JPanel(
                new BorderLayout(
                        10,
                        10));

        txtPlainText = new JTextArea();
        txtCipherText = new JTextArea();

        btnEncrypt = new JButton("Mã hóa");
        btnSavePlainText = new JButton("Lưu bản rõ");
        btnLoadPlainText = new JButton("Đọc bản rõ");

        JPanel plainPanel = new JPanel(new BorderLayout(5, 5));
        plainPanel.add(new JLabel("Bản rõ:"), BorderLayout.NORTH);
        plainPanel.add(new JScrollPane(txtPlainText), BorderLayout.CENTER);

        JPanel plainButtons = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 5));
        plainButtons.add(btnLoadPlainText);
        plainButtons.add(btnSavePlainText);
        btnClearPlainText = new JButton("Xóa bản rõ");
        btnCopyPlainText = new JButton("Copy bản rõ");
        plainButtons.add(btnClearPlainText);
        plainButtons.add(btnCopyPlainText);
        plainButtons.add(btnEncrypt);
        plainPanel.add(plainButtons, BorderLayout.SOUTH);

        JPanel cipherPanel = new JPanel(new BorderLayout(5, 5));
        cipherPanel.add(new JLabel("Bản mã:"), BorderLayout.NORTH);
        cipherPanel.add(new JScrollPane(txtCipherText), BorderLayout.CENTER);

        JPanel cipherButtons = new JPanel();
        btnSaveCipher = new JButton("Lưu bản mã");
        btnLoadCipher = new JButton("Đọc bản mã");
        btnClearCipherText = new JButton("Xóa bản mã");
        btnCopyCipherText = new JButton("Copy bản mã");
        cipherButtons.add(btnLoadCipher);
        cipherButtons.add(btnSaveCipher);
        cipherButtons.add(btnClearCipherText);
        cipherButtons.add(btnCopyCipherText);
        cipherPanel.add(cipherButtons, BorderLayout.SOUTH);

        JSplitPane encryptSplit = new JSplitPane(
                JSplitPane.VERTICAL_SPLIT,
                plainPanel,
                cipherPanel);
        encryptSplit.setResizeWeight(0.5);
        encryptSplit.setContinuousLayout(true);
        encryptSplit.setBorder(null);

        panel.add(encryptSplit, BorderLayout.CENTER);

        return panel;
    }

    private JPanel createDecryptPanel() {

        JPanel panel = new JPanel(
                new BorderLayout(
                        10,
                        10));

        txtCipherInput = new JTextArea();
        txtResult = new JTextArea();

        btnDecrypt = new JButton("Giải mã");

        JPanel cipherPanel = new JPanel(new BorderLayout(5, 5));
        cipherPanel.add(new JLabel("Bản mã:"), BorderLayout.NORTH);
        cipherPanel.add(new JScrollPane(txtCipherInput), BorderLayout.CENTER);

        JPanel cipherButtons = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 5));
        JButton loadCipherButton = new JButton("Đọc bản mã");
        loadCipherButton.addActionListener(e -> {
            try {
                JFileChooser chooser = createTextFileChooser("Đọc bản mã");
                if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {
                    File file = chooser.getSelectedFile();
                    txtCipherInput.setText(FileManager.loadCipher(file.getAbsolutePath()));
                    JOptionPane.showMessageDialog(this, "Đã đọc bản mã từ file: " + file.getName());
                }
            } catch (Exception ex) {
                JOptionPane.showMessageDialog(this, "Đọc bản mã thất bại: " + ex.getMessage());
            }
        });
        cipherButtons.add(loadCipherButton);
        btnClearCipherInput = new JButton("Xóa bản mã");
        btnCopyCipherInput = new JButton("Copy bản mã");
        cipherButtons.add(btnClearCipherInput);
        cipherButtons.add(btnCopyCipherInput);
        cipherButtons.add(btnDecrypt);
        cipherPanel.add(cipherButtons, BorderLayout.SOUTH);

        JPanel resultPanel = new JPanel(new BorderLayout(5, 5));
        resultPanel.add(new JLabel("Kết quả:"), BorderLayout.NORTH);
        resultPanel.add(new JScrollPane(txtResult), BorderLayout.CENTER);

        JPanel bottom = new JPanel(new FlowLayout(FlowLayout.LEFT, 5, 5));
        btnSaveResult = new JButton("Lưu kết quả");
        btnClearResult = new JButton("Xóa kết quả");
        btnCopyResult = new JButton("Copy kết quả");
        bottom.add(btnSaveResult);
        bottom.add(btnClearResult);
        bottom.add(btnCopyResult);
        resultPanel.add(bottom, BorderLayout.SOUTH);

        JSplitPane decryptSplit = new JSplitPane(
                JSplitPane.VERTICAL_SPLIT,
                cipherPanel,
                resultPanel);
        decryptSplit.setResizeWeight(0.5);
        decryptSplit.setContinuousLayout(true);
        decryptSplit.setBorder(null);

        panel.add(decryptSplit, BorderLayout.CENTER);

        return panel;
    }

    private void registerEvents() {

        btnGenerate.addActionListener(e -> generateKeys());

        btnEncrypt.addActionListener(e -> encryptText());

        btnDecrypt.addActionListener(e -> decryptText());

        btnSavePublic.addActionListener(e -> savePublicKey());

        btnSavePrivate.addActionListener(e -> savePrivateKey());

        btnLoadPublic.addActionListener(e -> loadPublicKey());

        btnLoadPrivate.addActionListener(e -> loadPrivateKey());

        btnSavePlainText.addActionListener(e -> savePlainText());

        btnLoadPlainText.addActionListener(e -> loadPlainText());

        btnSaveCipher.addActionListener(e -> saveCipher());

        btnLoadCipher.addActionListener(e -> loadCipher());

        btnSaveResult.addActionListener(e -> saveResult());

        btnClearPublicKey.addActionListener(e -> {
            txtPublicKey.setText("");
        });

        btnClearPrivateKey.addActionListener(e -> {
            txtPrivateKey.setText("");
        });

        btnClearPlainText.addActionListener(e -> {
            txtPlainText.setText("");
        });

        btnClearCipherText.addActionListener(e -> {
            txtCipherText.setText("");
        });

        btnClearCipherInput.addActionListener(e -> {
            txtCipherInput.setText("");
        });

        btnClearResult.addActionListener(e -> {
            txtResult.setText("");
        });

        btnClearAll.addActionListener(e -> {
            txtPublicKey.setText("");
            txtPrivateKey.setText("");
            txtPlainText.setText("");
            txtCipherText.setText("");
            txtCipherInput.setText("");
            txtResult.setText("");
            p = null;
            g = null;
            x = null;
            y = null;
        });

        btnCopyPlainText.addActionListener(e -> copyToClipboard(txtPlainText.getText(), "bản rõ"));
        btnCopyCipherText.addActionListener(e -> copyToClipboard(txtCipherText.getText(), "bản mã"));
        btnCopyCipherInput.addActionListener(e -> copyToClipboard(txtCipherInput.getText(), "bản mã"));
        btnCopyResult.addActionListener(e -> copyToClipboard(txtResult.getText(), "kết quả"));
    }

    private void copyToClipboard(String text, String label) {
        if (text == null || text.trim().isEmpty()) {
            JOptionPane.showMessageDialog(this, "Không có " + label + " để copy.");
            return;
        }
        StringSelection stringSelection = new StringSelection(text);
        Toolkit.getDefaultToolkit().getSystemClipboard().setContents(stringSelection, null);
        JOptionPane.showMessageDialog(this, "Đã copy " + label + " vào clipboard.");
    }

    private void generateKeys() {
        generateKeys(true);
    }

    private void generateKeys(boolean showMessage) {

        try {

            int bits = Integer.parseInt(
                    (String) cbBits.getSelectedItem());

            KeyGenerator.KeyPair keyPair = KeyGenerator.generateKeys(bits);

            p = keyPair.getP();
            g = keyPair.getG();
            x = keyPair.getX();
            y = keyPair.getY();

            txtPublicKey.setText(
                    "p = " + p +
                            "\n\n" +
                            "g = " + g +
                            "\n\n" +
                            "y = " + y);

            txtPrivateKey.setText(
                    "x = " + x);

            if (showMessage) {
                JOptionPane.showMessageDialog(
                        this,
                        "Đã sinh khóa thành công.");
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    ex.getMessage());
        }
    }

    private void encryptText() {

        try {

            String plainText = txtPlainText.getText();

            if (plainText == null || plainText.trim().isEmpty()) {

                JOptionPane.showMessageDialog(
                        this,
                        "Vui lòng nhập văn bản để mã hóa.");

                return;
            }

            if (p == null || g == null || y == null) {

                generateKeys(false);
                JOptionPane.showMessageDialog(
                        this,
                        "Không tìm thấy khóa. Đã tự động sinh khóa mới.");
            }

            List<Elgamal.CipherPair> cipher =

                    Elgamal.encrypt(
                            plainText,
                            p,
                            g,
                            y);

            txtCipherText.setText(
                    Elgamal.cipherToText(cipher));

            JOptionPane.showMessageDialog(
                    this,
                    "Mã hóa thành công. Bản rõ: "
                            + plainText.length()
                            + " ký tự, bản mã: "
                            + cipher.size()
                            + " cặp.");

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    "Mã hóa thất bại: " + ex.getMessage());
        }
    }

    private void decryptText() {

        try {

            if (p == null || x == null) {

                JOptionPane.showMessageDialog(
                        this,
                        "Chưa có private key.");

                return;
            }

            String cipherText = txtCipherInput.getText();

            if (cipherText == null || cipherText.trim().isEmpty()) {

                JOptionPane.showMessageDialog(
                        this,
                        "Bản mã trống. Vui lòng đọc hoặc nhập bản mã.");

                return;
            }

            List<Elgamal.CipherPair> cipher = Elgamal.textToCipher(cipherText);

            String result = Elgamal.decrypt(
                    cipher,
                    p,
                    x);

            txtResult.setText(result);

            String plainText = txtPlainText.getText();
            if (plainText != null && !plainText.trim().isEmpty()) {
                if (plainText.equals(result)) {
                    JOptionPane.showMessageDialog(
                            this,
                            "Giải mã thành công và khớp với bản rõ.");
                } else {
                    JOptionPane.showMessageDialog(
                            this,
                            "Giải mã thành công nhưng không khớp với bản rõ.");
                }
            } else {
                JOptionPane.showMessageDialog(
                        this,
                        "Giải mã thành công.");
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    "Giải mã thất bại: Bản mã không hợp lệ hoặc đã bị sửa. "
                            + ex.getMessage());
        }
    }

    private void savePrivateKey() {

        try {

            JFileChooser chooser = new JFileChooser();

            if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {

                File file = chooser.getSelectedFile();

                FileManager.saveKey(
                        file.getAbsolutePath(),
                        p,
                        g,
                        x);

                JOptionPane.showMessageDialog(
                        this,
                        "Đã lưu Private Key");
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    ex.getMessage());
        }
    }

    private void loadPublicKey() {

        try {

            JFileChooser chooser = new JFileChooser();

            if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {

                File file = chooser.getSelectedFile();

                BigInteger[] key =

                        FileManager.loadKey(
                                file.getAbsolutePath());

                p = key[0];
                g = key[1];
                y = key[2];

                txtPublicKey.setText(
                        "p = " + p +
                                "\n\n" +
                                "g = " + g +
                                "\n\n" +
                                "y = " + y);
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    ex.getMessage());
        }
    }

    private void savePublicKey() {

        try {

            JFileChooser chooser = new JFileChooser();

            if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {

                File file = chooser.getSelectedFile();

                FileManager.saveKey(
                        file.getAbsolutePath(),
                        p,
                        g,
                        y);

                JOptionPane.showMessageDialog(
                        this,
                        "Đã lưu Public Key");
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    ex.getMessage());
        }
    }

    private void loadPrivateKey() {

        try {

            JFileChooser chooser = new JFileChooser();

            if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {

                File file = chooser.getSelectedFile();

                BigInteger[] key =

                        FileManager.loadKey(
                                file.getAbsolutePath());

                p = key[0];
                g = key[1];
                x = key[2];

                txtPrivateKey.setText(
                        "x = " + x);
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    ex.getMessage());
        }
    }

    private void savePlainText() {

        try {

            String plainText = txtPlainText.getText();
            if (plainText == null || plainText.trim().isEmpty()) {
                JOptionPane.showMessageDialog(
                        this,
                        "Không có bản rõ để lưu.");
                return;
            }

            JFileChooser chooser = createTextFileChooser("Lưu bản rõ");
            if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
                File file = ensureTxtExtension(chooser.getSelectedFile());
                FileManager.saveCipher(
                        file.getAbsolutePath(),
                        plainText);
                JOptionPane.showMessageDialog(
                        this,
                        "Đã lưu bản rõ vào file: " + file.getName());
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    "Lưu bản rõ thất bại: " + ex.getMessage());
        }
    }

    private void loadPlainText() {

        try {

            JFileChooser chooser = createTextFileChooser("Đọc bản rõ");
            if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {
                File file = chooser.getSelectedFile();
                txtPlainText.setText(
                        FileManager.loadCipher(
                                file.getAbsolutePath()));
                JOptionPane.showMessageDialog(
                        this,
                        "Đã đọc bản rõ từ file: " + file.getName());
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    "Đọc bản rõ thất bại: " + ex.getMessage());
        }
    }

    private void saveResult() {

        try {

            String result = txtResult.getText();
            if (result == null || result.trim().isEmpty()) {
                JOptionPane.showMessageDialog(
                        this,
                        "Không có kết quả để lưu.");
                return;
            }

            JFileChooser chooser = createTextFileChooser("Lưu kết quả giải mã");
            if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {
                File file = ensureTxtExtension(chooser.getSelectedFile());
                FileManager.saveCipher(
                        file.getAbsolutePath(),
                        result);
                JOptionPane.showMessageDialog(
                        this,
                        "Đã lưu kết quả giải mã vào file: " + file.getName());
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    "Lưu kết quả thất bại: " + ex.getMessage());
        }
    }

    private JFileChooser createTextFileChooser(String title) {

        JFileChooser chooser = new JFileChooser();
        chooser.setDialogTitle(title);
        chooser.setFileFilter(
                new FileNameExtensionFilter(
                        "Text files (*.txt)",
                        "txt"));
        return chooser;
    }

    private File ensureTxtExtension(File file) {

        if (file.getName().contains(".")) {
            return file;
        }

        return new File(file.getAbsolutePath() + ".txt");
    }

    private void saveCipher() {

        try {

            JFileChooser chooser = createTextFileChooser("Lưu bản mã");

            if (chooser.showSaveDialog(this) == JFileChooser.APPROVE_OPTION) {

                File file = ensureTxtExtension(chooser.getSelectedFile());

                FileManager.saveCipher(
                        file.getAbsolutePath(),
                        txtCipherText.getText());

                JOptionPane.showMessageDialog(
                        this,
                        "Đã lưu bản mã vào file: " + file.getName());
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    "Lưu bản mã thất bại: " + ex.getMessage());
        }
    }

    private void loadCipher() {

        try {

            JFileChooser chooser = createTextFileChooser("Đọc bản mã");

            if (chooser.showOpenDialog(this) == JFileChooser.APPROVE_OPTION) {

                File file = chooser.getSelectedFile();

                txtCipherText.setText(
                        FileManager.loadCipher(
                                file.getAbsolutePath()));

                JOptionPane.showMessageDialog(
                        this,
                        "Đã đọc bản mã từ file: " + file.getName());
            }

        } catch (Exception ex) {

            JOptionPane.showMessageDialog(
                    this,
                    "Đọc bản mã thất bại: " + ex.getMessage());
        }
    }

    public static void main(String[] args) {

        SwingUtilities.invokeLater(() -> {

            new ElgamalGUI().setVisible(true);

        });
    }
}
