import java.math.BigInteger;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.List;

public class Elgamal {

    public static class CipherPair {
        private BigInteger c1;
        private BigInteger c2;

        public CipherPair(BigInteger c1, BigInteger c2) {
            this.c1 = c1;
            this.c2 = c2;
        }

        public BigInteger getC1() {
            return c1;
        }

        public BigInteger getC2() {
            return c2;
        }

        @Override
        public String toString() {
            return c1 + "," + c2;
        }
    }

    /**
     * Mã hóa chuỗi Unicode
     */
    public static List<CipherPair> encrypt(String plainText, BigInteger p, BigInteger g, BigInteger y) {
        List<CipherPair> encrypted = new ArrayList<>();
        byte[] data = plainText.getBytes(StandardCharsets.UTF_8);
        for (byte b : data) {
            int value = b & 0xFF;
            BigInteger m = BigInteger.valueOf(value);
            BigInteger k = KeyGenerator.randomK(p);
            BigInteger c1 = g.modPow(k, p);
            BigInteger c2 =
                    m.multiply(y.modPow(k, p))
                            .mod(p);
            encrypted.add(new CipherPair(c1, c2));
        }
        return encrypted;
    }

    /**
     * Giải mã chuỗi Unicode
     */
    public static String decrypt(List<CipherPair> cipher, BigInteger p, BigInteger x) {
        byte[] data = new byte[cipher.size()];
        for (int i = 0; i < cipher.size(); i++) {
            CipherPair pair = cipher.get(i);
            BigInteger s =
                    pair.getC1().modPow(x, p);
            BigInteger sInverse =
                    s.modInverse(p);
            BigInteger m =
                    pair.getC2()
                            .multiply(sInverse)
                            .mod(p);
            data[i] = (byte) m.intValue();
        }
        return new String(data, StandardCharsets.UTF_8);
    }

    /**
     * Chuyển List Cipher -> String
     */
    public static String cipherToText(
            List<CipherPair> cipher) {

        StringBuilder sb = new StringBuilder();

        for (CipherPair pair : cipher) {

            sb.append(pair.getC1())
                    .append(":")
                    .append(pair.getC2())
                    .append("\n");
        }

        return sb.toString();
    }

    /**
     * Chuyển String -> List Cipher
     */
    public static List<CipherPair> textToCipher(
            String text) {

        List<CipherPair> result =
                new ArrayList<>();

        String[] lines =
                text.split("\\n");

        for (String line : lines) {

            if (line.trim().isEmpty())
                continue;

            String[] parts =
                    line.split(":");

            BigInteger c1 =
                    new BigInteger(parts[0]);

            BigInteger c2 =
                    new BigInteger(parts[1]);

            result.add(
                    new CipherPair(c1, c2)
            );
        }

        return result;
    }
}