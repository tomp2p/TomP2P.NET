using System;

namespace TomP2P.Utils
{
    public class Pair<TZero, TOne> : IEquatable<Pair<TZero, TOne>> 
        where TZero : class 
        where TOne : class
    {
        public TZero Element0 { get; private set; }
        public TOne Element1 { get; private set; }

        public static Pair<TZero, TOne> Create(TZero element0, TOne element1)
        {
            return new Pair<TZero, TOne>(element0, element1);
        }

        public Pair(TZero element0, TOne element1)
        {
            Element0 = element0;
            Element1 = element1;
        }

        public Pair<TZero, TOne> SetElement0(TZero element0)
        {
            return new Pair<TZero, TOne>(element0, Element1);
        }

        public Pair<TZero, TOne> SetElement1(TOne element1)
        {
            return new Pair<TZero, TOne>(Element0, Element1);
        }

        public bool IsEmpty
        {
            get { return Element0 == null && Element1 == null; }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            if (GetType() != obj.GetType())
            {
                return false;
            }
            return Equals(obj as Pair<TZero, TOne>);
        }

        public bool Equals(Pair<TZero, TOne> other)
        {
            return Element0.Equals(other.Element0) && Element1.Equals(other.Element1);
        }

        public override int GetHashCode()
        {
            return (Element0 == null ? 0 : Element0.GetHashCode()) ^ (Element1 == null ? 0 : Element1.GetHashCode());
        }
    }
}
