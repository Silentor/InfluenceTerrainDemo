namespace TerrainDemo.Tools
{
    public struct Tuple<T1, T2>
    {
        public readonly T1 First;
        public readonly T2 Second;

        public Tuple(T1 first, T2 second)
        {
            First = first;
            Second = second;
        }
    }
}
