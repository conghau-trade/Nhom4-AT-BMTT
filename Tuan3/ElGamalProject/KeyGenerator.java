import java.math.BigInteger;
import java.security.SecureRandom;

public class KeyGenerator {

    private static final SecureRandom random =
            new SecureRandom();

    public static class KeyPair {

        private BigInteger p;
        private BigInteger g;
        private BigInteger x;
        private BigInteger y;

        public KeyPair(
                BigInteger p,
                BigInteger g,
                BigInteger x,
                BigInteger y) {

            this.p = p;
            this.g = g;
            this.x = x;
            this.y = y;
        }

        public BigInteger getP() {
            return p;
        }

        public BigInteger getG() {
            return g;
        }

        public BigInteger getX() {
            return x;
        }

        public BigInteger getY() {
            return y;
        }
    }

    /**
     * Sinh khóa ElGamal
     */
    public static KeyPair generateKeys(int bits) {
        BigInteger p =
                BigInteger.probablePrime(
                        bits,
                        random
                );
        BigInteger g = generateGenerator(p);
        BigInteger x =
                randomBetween(
                        BigInteger.TWO,
                        p.subtract(BigInteger.TWO)
                );
        BigInteger y =
                g.modPow(x, p);
        return new KeyPair(p, g, x, y);
    }

    /**
     * Sinh g
     */
    private static BigInteger generateGenerator(
            BigInteger p) {

        BigInteger g;

        do {

            g = randomBetween(
                    BigInteger.TWO,
                    p.subtract(BigInteger.TWO)
            );

        } while (
                g.equals(BigInteger.ZERO)
                        || g.equals(BigInteger.ONE)
        );

        return g;
    }

    /**
     * Sinh k ngẫu nhiên
     */
    public static BigInteger randomK(
            BigInteger p) {

        return randomBetween(
                BigInteger.TWO,
                p.subtract(BigInteger.TWO)
        );
    }

    /**
     * Sinh số trong đoạn [min,max]
     */
    private static BigInteger randomBetween(
            BigInteger min,
            BigInteger max) {

        BigInteger result;

        do {

            result =
                    new BigInteger(
                            max.bitLength(),
                            random
                    );

        } while (
                result.compareTo(min) < 0
                        || result.compareTo(max) > 0
        );

        return result;
    }
}