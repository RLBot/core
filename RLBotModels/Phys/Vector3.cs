namespace RLBotModels.Phys
{
    public struct Vector3
    {
        public float x,
            y,
            z;

        public float Magnitude()
        {
            return (float)Math.Sqrt(x * x + y * y + z * z);
        }
    }
}
