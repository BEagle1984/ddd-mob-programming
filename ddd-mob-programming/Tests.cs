using System;
using ddd_mob_programming.Model.ValueObjects;
using FluentAssertions;
using Xunit;

namespace ddd_mob_programming
{
    public class Tests
    {
        [Fact]
        public void Test1()
        {
            true.Should().BeTrue();
        }
    }
}
