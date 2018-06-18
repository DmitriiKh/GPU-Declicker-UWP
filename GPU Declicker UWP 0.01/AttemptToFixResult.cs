using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    public class FixResult
    {
        public bool Success { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public float ErrSum { get; set; }

        public bool BetterThan(FixResult anotherResult)
        {
            if ((Success && !anotherResult.Success) ||
                (Length < anotherResult.Length) || 
                (ErrSum / anotherResult.ErrSum < 0.01))
                return true;
            else
                return false;
        }
    }
}
