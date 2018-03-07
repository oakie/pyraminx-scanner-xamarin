using System.Collections.Generic;

namespace Pyraminx.Core
{
    public class FaceColor
    {
        public int Value { get; protected set; }
        public string Name { get; protected set; }

        public static FaceColor Undefined = new FaceColor { Value = -1, Name = "Undefined" };
        public static FaceColor Yellow = new FaceColor { Value = 0, Name = "Yellow" };
        public static FaceColor Blue = new FaceColor { Value = 1, Name = "Blue" };
        public static FaceColor Green = new FaceColor { Value = 2, Name = "Green" };
        public static FaceColor Orange = new FaceColor { Value = 3, Name = "Orange" };

        public static IEnumerable<FaceColor> AsEnumerable = new[] { Yellow, Blue, Green, Orange };

        protected FaceColor() { }

        public override string ToString()
        {
            return Value != -1 ? Name.Substring(0, 1) : ".";
        }

        public static bool operator ==(FaceColor lhs, FaceColor rhs)
        {
            if (lhs is null && rhs is null)
                return true;
            if (lhs is null || rhs is null)
                return false;
            return lhs.Value == rhs.Value;
        }

        public static bool operator !=(FaceColor lhs, FaceColor rhs)
        {
            return !(lhs == rhs);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != typeof(FaceColor))
                return false;
            return (FaceColor)obj == this;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}