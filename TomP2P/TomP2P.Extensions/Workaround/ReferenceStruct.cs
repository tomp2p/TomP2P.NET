
namespace TomP2P.Extensions.Workaround
{
    /// <summary>
    /// Helper class to wrap .NET structs into a reference type.
    /// </summary>
    public class ReferenceStruct<T> where T : struct
    {
        private T _value;

        public ReferenceStruct()
            : this(default(T))
        { }

        public ReferenceStruct(T initialValue)
        {
            _value = initialValue;
        }

        public void SetValue(T value)
        {
            _value = value;
        }

        public T GetValue()
        {
            return _value;
        }
    }
}
