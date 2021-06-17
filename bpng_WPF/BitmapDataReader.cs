using System.IO;

namespace bpng_WPF
{
    public class BitmapDataReader : BinaryReader
    {
        public BitmapDataReader(Stream str) : base(str)
        {

        }

		public override int ReadInt32()
		{
			byte[] b = new byte[4];

			for (int i = 0; i < b.Length; i++)
				b[i] = ReadByte();

			return (b[0] << 24) | (b[1] << 16) | (b[2] << 8) | b[3];
		}
	}
}
