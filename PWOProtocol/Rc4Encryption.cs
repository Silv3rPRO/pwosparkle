using System.Text;

namespace PWOProtocol
{
    public static class Rc4Encryption
    {
        private static int[] _key = new int[]
        {
            0x0A, 0x6A, 0x36, 0x0E, 0x47, 0x70, 0x1F, 0x25, 0x41, 0x55, 0x1D, 0x0C, 0x23,
            0x10, 0x10, 0x3B, 0x5C, 0x68, 0x58, 0x63, 0x39, 0x57, 0x60, 0x16, 0x11, 0x14,
            0x48, 0x04, 0x35, 0x69, 0x63, 0x1A
        };

        public static void Encrypt(byte[] input)
        {
            int x, y, j = 0;
            int[] box = new int[256];

            for (int i = 0; i < 256; i++)
            {
                box[i] = i;
            }

            for (int i = 0; i < 256; i++)
            {
                j = (_key[(i + 1) % _key.Length] + box[i] + j) % 256;
                x = box[i];
                box[i] = box[j];
                box[j] = x;
            }

            j = 0;
            
            for (int i = 0; i < input.Length; i++)
            {
                y = (i + 1) % 256;
                j = (box[y] + j) % 256;
                x = box[y];
                box[y] = box[j];
                box[j] = x;

                input[i] = (byte)(input[i] ^ box[(box[y] + box[j]) % 256]);
            }
        }
    }
}
