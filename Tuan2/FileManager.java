import java.io.*;
import java.math.BigInteger;
import java.nio.charset.StandardCharsets;

public class FileManager {

    /**
     * Lưu khóa ra file
     */
    public static void saveKey(
            String fileName,
            BigInteger p,
            BigInteger g,
            BigInteger value) throws IOException {

        BufferedWriter writer =
                new BufferedWriter(
                        new OutputStreamWriter(
                                new FileOutputStream(fileName),
                                StandardCharsets.UTF_8
                        )
                );

        writer.write(p.toString());
        writer.newLine();

        writer.write(g.toString());
        writer.newLine();

        writer.write(value.toString());

        writer.close();
    }

    /**
     * Đọc khóa từ file
     */
    public static BigInteger[] loadKey(
            String fileName) throws IOException {

        BufferedReader reader =
                new BufferedReader(
                        new InputStreamReader(
                                new FileInputStream(fileName),
                                StandardCharsets.UTF_8
                        )
                );

        BigInteger p =
                new BigInteger(reader.readLine());

        BigInteger g =
                new BigInteger(reader.readLine());

        BigInteger value =
                new BigInteger(reader.readLine());

        reader.close();

        return new BigInteger[]{
                p,
                g,
                value
        };
    }

    /**
     * Lưu bản mã
     */
    public static void saveCipher(
            String fileName,
            String cipherText)
            throws IOException {

        BufferedWriter writer =
                new BufferedWriter(
                        new OutputStreamWriter(
                                new FileOutputStream(fileName),
                                StandardCharsets.UTF_8
                        )
                );

        writer.write(cipherText);

        writer.close();
    }

    /**
     * Đọc bản mã
     */
    public static String loadCipher(
            String fileName)
            throws IOException {

        BufferedReader reader =
                new BufferedReader(
                        new InputStreamReader(
                                new FileInputStream(fileName),
                                StandardCharsets.UTF_8
                        )
                );

        StringBuilder sb =
                new StringBuilder();

        String line;

        while ((line = reader.readLine()) != null) {

            sb.append(line)
              .append("\n");
        }

        reader.close();

        return sb.toString();
    }
}