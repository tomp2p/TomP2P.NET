using NUnit.Framework;
using System.Collections.Generic;

namespace TomP2P.Tests.Utils
{
    [TestFixture]
    public class UtilsTest
    {
        [Test]
        public void TestIsSameCollectionsSet()
        {
            IList<int> list1 = new List<int>();
            list1.Add(1);
            list1.Add(2);
            list1.Add(3);

            IList<int> list2 = new List<int>();
            list2.Add(4);
            list2.Add(5);
            list2.Add(6);

            IList<int> list3 = new List<int>();
            list3.Add(4);
            list3.Add(5);
            list3.Add(6);

            ISet<int> set1 = new HashSet<int>();
            set1.Add(1);
            set1.Add(2);
            set1.Add(3);

            ISet<int> set2 = new HashSet<int>();
            set2.Add(4);
            set2.Add(5);
            set2.Add(6);

            ISet<int> set3 = new HashSet<int>();
            set3.Add(7);
            set3.Add(8);
            set3.Add(9);

            ICollection<IList<int>> collection1 = new List<IList<int>>();
            collection1.Add(list1);
            collection1.Add(list2);

            ICollection<ISet<int>> collection2 = new HashSet<ISet<int>>();
            collection2.Add(set1);
            collection2.Add(set2);

            ICollection<ISet<int>> collection3 = new HashSet<ISet<int>>();
            collection3.Add(set2);
            collection3.Add(set3);

            // collection1 == collection2
            Assert.IsTrue(Core.Utils.Utils.IsSameCollectionSets(collection1, collection2));

            // collection1 != collection3
            Assert.IsFalse(Core.Utils.Utils.IsSameCollectionSets(collection1, collection3));
        }
    }
}
