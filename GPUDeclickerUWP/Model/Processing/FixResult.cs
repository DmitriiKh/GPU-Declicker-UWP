namespace GPUDeclickerUWP.Model.Processing
{
    public class FixResult
    {
        public bool Success { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public float ErrSum { get; set; }

        public bool BetterThan(FixResult anotherResult)
        {
            if (Success && !anotherResult.Success ||
                Length < anotherResult.Length ||
                ErrSum / anotherResult.ErrSum < 0.01)
                return true;
            return false;
        }
    }
}