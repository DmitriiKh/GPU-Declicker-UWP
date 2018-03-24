using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPU_Declicker_UWP_0._01
{
    class AttemptToFixResult
    {
        public bool Success { get; set; }
        public int Position { get; set; }
        public int Length { get; set; }
        public float ErrSum { get; set; }

        public bool BetterThan(AttemptToFixResult anotherResult)
        {
            if (!anotherResult.Success)
                return true;
            if (Length < anotherResult.Length)
                return true;
            if (ErrSum < anotherResult.ErrSum)
                return true;

            return false;
        }
    }
}
