using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace Tycoon.Tests
{
    public sealed class CheckpointStringListTests
    {
        [Test]
        public void EmptyConsumersRemainSeparateFromLiveEvent()
        {
            var source = new TransactionEvent { id = "event", consumers = new List<string>() };
            var copy = Copy(source);
            Assert.That(copy.consumers, Is.Empty);
            Assert.That(copy.consumers, Is.Not.SameAs(source.consumers));
            source.consumers.Add("live");
            Assert.That(copy.consumers, Is.Empty);
            copy.consumers.Add("snapshot");
            Assert.That(source.consumers, Is.EqualTo(new[] { "live" }));
        }

        [Test]
        public void StringListsKeepNullDuplicateOrderAndTwoWayIsolation()
        {
            var source = new List<string> { "first", null, "first", "last" };
            var copy = Copy(source);
            Assert.That(copy, Is.TypeOf<List<string>>());
            Assert.That(copy, Is.EqualTo(source));
            source[0] = "changed";
            copy[3] = "snapshot";
            Assert.That(copy[0], Is.EqualTo("first"));
            Assert.That(source[3], Is.EqualTo("last"));
            Assert.That(copy[1], Is.Null);
            Assert.That(copy[2], Is.EqualTo("first"));
        }

        static T Copy<T>(T value)
        {
            var snapshot = typeof(TransactionCore).Assembly.GetType("Tycoon.CheckpointSnapshot");
            var method = snapshot.GetMethod("Copy", BindingFlags.Static | BindingFlags.NonPublic);
            return (T)method.MakeGenericMethod(typeof(T)).Invoke(null, new object[] { value });
        }
    }
}
