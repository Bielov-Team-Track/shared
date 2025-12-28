using NUnit.Framework;

namespace Shared.Testing.Base;

[TestFixture]
[Category("Unit")]
public abstract class UnitTestBase
{
    [SetUp]
    public virtual void SetUp()
    {
        // Override in derived classes for common setup
    }

    [TearDown]
    public virtual void TearDown()
    {
        // Override in derived classes for common cleanup
    }
}