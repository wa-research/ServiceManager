using NUnit.Framework;

namespace ServiceManager.ServiceSupport.Tests
{
    [TestFixture]
    public class ConstructorFinderTests
    {
        private ServiceProxy _proxy;

        [SetUp]
        public void BeforeEachTest()
        {
            _proxy = new ServiceProxy() { Name = "TestProxy"};
        }

        [Test]
        public void GetStartMethod_FindsSimpleStartMethod()
        {
            Assert.IsNotNull(_proxy.GetStartMethod(typeof(SimpleStartOnly)));
        }

        [Test]
        public void GetStartMethod_FindsStartWithContext()
        {
            Assert.IsNotNull(_proxy.GetStartMethod(typeof(StartWithContextOnly)));
        }

        [Test]
        public void GetStartMethod_FindsMostSpecificMethod()
        {
            var s = _proxy.GetStartMethod(typeof(BothVersionsOfStartWithExtras));
            Assert.IsNotNull(s);
            Assert.IsTrue(s.GetParameters()[0].ParameterType == typeof(ServiceContext));
        }

        [Test]
        public void GetStartMethod_DoesNotFindWrongMethod()
        {
            Assert.IsNull(_proxy.GetStartMethod(typeof(WrongStart)));
        }

        #region Test support classes
        class SimpleStartOnly
        {
            public void StartService() { }
        }

        class StartWithContextOnly
        {
            public void StartService(ServiceContext ctx) {}
        }

        class WrongStart
        {
            public void StartService(object o1) {}
        }

        class BothVersionsOfStartWithExtras
        {
            public void StartService() {}
            public void StartService(object o) {}
            public void StartService(ServiceContext ctx) {}
            public void StartService(ServiceContext ctx, object fakeParam) {}
        }
        #endregion
    }
}
