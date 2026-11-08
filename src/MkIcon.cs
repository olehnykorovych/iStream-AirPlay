namespace IStream
{
    static class MkIcon
    {
        static void Main(string[] args)
        {
            IconArt.SaveIco(args[0], new[] { 16, 20, 24, 32, 48, 64, 256 });
        }
    }
}
