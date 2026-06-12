using System;

namespace ArcadiaDevs.Viora.Platform.Agronomic.Domain.Model.ValueObjects
{
    /// <summary>
    /// User identifier value object.
    /// </summary>
    public readonly record struct UserId
    {
        public int Value { get; }

        public UserId(int value)
        {
            if (value <= 0)
            {
                throw new ArgumentException("UserId must be a positive number.", nameof(value));
            }

            Value = value;
        }
    }
}
